#version 430
//#version 430 compatibility
//#extension GL_ARB_compute_shader : enable
//#extension GL_ARB_shader_storage_buffer_object : enable
#extension GL_NV_shader_atomic_float : enable
#extension GL_ARB_shader_precision : require

//precision highp float;

layout(local_size_x = 64) in;

#define PI			3.14159265358f
#define TWOPI		6.28318530718f
#define DEGTORAD	0.0174532925f

#define MAX_ITERATORS		100
#define MAX_PALETTE_COLORS	150
#define MAX_PARAMS			(2 * MAX_ITERATORS)
#define MAX_XAOS			(MAX_ITERATORS * MAX_ITERATORS)

struct camera_params
{
	mat4 view_proj_mat;
	vec4 position;
	vec4 forward;
	vec4 focus_point;
	float aperture;
	float focus_distance;
	float depth_of_field;
	int projection_type;//0: persp, 1: equirect
};

struct Iterator
{
	float color_speed;
	float color_index;//color index, 0 - 1
	float opacity;
	float reset_prob;
	int reset_alias;
	int tfId;
	int real_params_index;
	int vec3_params_index;
	int shading_mode;//0: default, 1: delta_p
	float tf_mix;
	float tf_add;
	int padding2;
};

struct p_state
{
	vec4 pos;
	float color_index;
	float dummy0;
	int iterator_index;
	int iteration_depth;
};

//Shader Storage Buffer Objects
//read and write, dynamic size

layout(std140, binding = 0) coherent buffer histogram_buffer
{
	vec4 histogram[];
};

layout(std140, binding = 1) buffer points_buffer
{
	//per invocation
	p_state state[];
};

//Uniform Buffer Objects
//read-only, fixed size

layout(std140, binding = 2) uniform settings_ubo
{
	camera_params camera;

	float fog_effect;
	int itnum;//number of iterators
	int palettecnt;
	int mark_area_in_focus;

	int warmup;
	float entropy;
	int max_filter_radius;
	int padding0;

	int filter_method;
	float filter_param0;
	float filter_param1;
	float filter_param2;
} settings;

layout(std140, binding = 3) uniform iterators_ubo
{
	Iterator iterators[MAX_ITERATORS];
};

layout(std140, binding = 4) uniform alias_tables_ubo
{
	vec4 alias_tables[MAX_ITERATORS];//x: probability, y: alias
};

layout(std140, binding = 5) uniform palette_ubo
{
	vec4 palette[MAX_PALETTE_COLORS];
};

layout(std140, binding = 6) uniform real_params_ubo
{
	float real_params[MAX_PARAMS];//real transform parameters of all iterators
};

layout(std140, binding = 7) uniform vec3_params_ubo
{
	vec4 vec3_params[MAX_PARAMS];//vec3 transform parameters of all iterators
};

uniform uint seed;
uniform int width;
uniform int height;
uniform int dispatch_cnt;
uniform int reset_points_state;
uniform int invocation_iters;

mat3 rotmat(vec3 v, float arad)
{
	float c = cos(arad);
	float s = sin(arad);
	return mat3(
		c + (1.0 - c) * v.x * v.x, (1.0 - c) * v.x * v.y - s * v.z, (1.0 - c) * v.x * v.z + s * v.y,
		(1.0 - c) * v.x * v.y + s * v.z, c + (1.0 - c) * v.y * v.y, (1.0 - c) * v.y * v.z - s * v.x,
		(1.0 - c) * v.x * v.z - s * v.y, (1.0 - c) * v.y * v.z + s * v.x, c + (1.0 - c) * v.z * v.z
	);
}
mat3 rotate_euler(vec3 euler_angles)
{
	return rotmat(vec3(1.0, 0.0, 0.0), euler_angles.x) * rotmat(vec3(0.0, 1.0, 0.0), euler_angles.y) * rotmat(vec3(0.0, 0.0, 1.0), euler_angles.z);
}

//pcg: https://www.reedbeta.com/blog/hash-functions-for-gpu-rendering/
uint pcg_hash(uint x)
{
	uint state = x * 747796405u + 2891336453u;
	uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
	return (word >> 22u) ^ word;
}
uvec2 pcg_hash_2d(uvec2 v)
{
    v = v * 1664525u + 1013904223u;

    v.x += v.y * 1664525u;
    v.y += v.x * 1664525u;

    v = v ^ (v>>16u);

    v.x += v.y * 1664525u;
    v.y += v.x * 1664525u;

    v = v ^ (v>>16u);

    return v;
}
//https://www.jcgt.org/published/0009/03/02/
uvec3 pcg_hash_3d(uvec3 v) {

    v = v * 1664525u + 1013904223u;

    v.x += v.y*v.z;
    v.y += v.z*v.x;
    v.z += v.x*v.y;

    v ^= v >> 16u;

    v.x += v.y*v.z;
    v.y += v.z*v.x;
    v.z += v.x*v.y;

    return v;
}
uvec4 pcg_hash_4d(uvec4 v)
{
    v = v * 1664525u + 1013904223u;
    
    v.x += v.y*v.w;
    v.y += v.z*v.x;
    v.z += v.x*v.y;
    v.w += v.y*v.z;
    
    v ^= v >> 16u;
    
    v.x += v.y*v.w;
    v.y += v.z*v.x;
    v.z += v.x*v.y;
    v.w += v.y*v.z;
    
    return v;
}
// 128-bit xorshift - Marsaglia, Xorshift RNGs, Journal of Statistical Software, v8n14, 2003
uint xorshift128(uvec4 v)
{
    v.w ^= v.w << 11u;
    v.w ^= v.w >> 8u;
    v = v.wxyz;
    v.x ^= v.y;
    v.x ^= v.y >> 19u;
    return v.x;
}
//uint hash -> float 0-1
float f_hash(uint h) {
	h &= 0x007FFFFFu;//mantissa mask
	h |= 0x3F800000u;//one mask
	float  r2 = uintBitsToFloat(h);
	return r2 - 1.0;
}

//float variants of hash functions
float f_hash1(float f) {
	return f_hash(pcg_hash(floatBitsToUint(f)));
}
float f_hash2(float f1, float f2) {
	return f_hash(pcg_hash_2d(uvec2(floatBitsToUint(f1), floatBitsToUint(f2))).x);    
}
float f_hash3(float f1, float f2, float f3) {
	return f_hash(pcg_hash_3d(uvec3(floatBitsToUint(f1), floatBitsToUint(f2), floatBitsToUint(f3))).x);
}
float f_hash4(uint u1, uint u2, uint u3, uint u4) {
	return f_hash(xorshift128(pcg_hash_4d(uvec4(u1, u2, u3, u4))));
}

//This may be used by plugins like this: random(next++);
float random(inout uint nextSample)
{
	return f_hash4(seed, gl_GlobalInvocationID.x, uint(dispatch_cnt), nextSample++);
}


//consts available in transforms
//using ARB_shader_precision extension, this should reliably produce NaN for all vendors
const vec3 discarded_point = vec3(intBitsToFloat(0x7fc00000));

//additional include snippets inserted on initialization
@includes

vec3 apply_transform(Iterator iter, p_state _p_input, inout uint next)
{
	//variables available in transforms:
    vec3 p = _p_input.pos.xyz;
	int iter_depth = _p_input.iteration_depth;

	//transform snippets inserted on initialization
	@transforms

	return p;
}

void apply_coloring(Iterator it, vec4 p0, vec4 p, inout float color_index)
{
	float in_color = color_index;
	float speed = it.color_speed;
	if (it.shading_mode == 1)
	{
		float p_delta = length(p - p0);
		speed *= (1.0 - 1.0 / (1.0 + p_delta));
	}

    float new_index = mix(in_color, it.color_index, speed);
    if(new_index != 0.0 && fract(new_index) == 0.0)
        color_index = new_index;
    else
	    color_index = fract(new_index);
}

vec3 getPaletteColor(float pos)
{
	float palettepos = pos * (settings.palettecnt - 1);
	int index = int(floor(palettepos));
	vec3 c1 = palette[index].xyz;
    if (index + 1 == settings.palettecnt)
        return c1;
	vec3 c2 = palette[index+1].xyz;
	float a = fract(palettepos);
	//TODO: interpolate in a different color space?
	return mix(c1, c2, a);//lerp
}

vec2 ProjectPerspective(camera_params c, vec3 p, inout uint next)
{
    vec4 p_norm = c.view_proj_mat * vec4(p, 1.0f);

    //discard behind camera
    vec3 dir = normalize(p_norm.xyz);
    if (dot(dir, vec3(0.0,0.0,-1.0)) < 0.0)
        return vec2(-2.0, -2.0);

    if (any(isinf(p_norm)) || p_norm.w == 0.0)
    {//may be projected to infinity, or w is sometimes 0, not sure why
        return vec2(-2.0, -2.0);
    }

    p_norm /= p_norm.w;

    //dof
    float blur = c.aperture * max(0, abs(dot(p - c.focus_point.xyz, -c.forward.xyz)) - c.depth_of_field); //use focalplane normal
    float ra = random(next);
    float rl = random(next);
    p_norm.xy += pow(rl, 0.5f) * blur * vec2(cos(ra * TWOPI), sin(ra * TWOPI));

    //discard at edges
    vec2 cl = clamp(p_norm.xy, vec2(-1.0), vec2(1.0));
    if (length(p_norm.xy - cl) != 0.0)
        return vec2(-2.0, -2.0);

    float ratio = width / float(height);
    return vec2(
        (p_norm.x + 1) * 0.5 * width - 0.5,
        (p_norm.y * ratio + 1) * 0.5 * height - 0.5);
}

vec2 ProjectEquirectangular(camera_params c, vec3 p, inout uint next)
{
    vec4 p_norm = c.view_proj_mat * vec4(p, 1.0f);

    //TODO: dof?

    vec3 dir = normalize(p_norm.xyz);
    float a1 = atan(dir.y, dir.x);
    float a2 = asin(dir.z);
    float x_px = (a1 / PI + 1.0) / 2.0 * width;
    float y_px = (a2 / PI + 0.5) * height;
    return vec2(x_px, y_px);
}

vec2 Project(camera_params c, vec3 p, inout uint next)
{
    if (c.projection_type == 0)
        return ProjectPerspective(c, p, next);
    else //if(c.projection_type == 1)
        return ProjectEquirectangular(c, p, next);
}

//alias method sampling in O(1)
//input: uniform random 0-1
//output: sampled iterator's index
int alias_sample(float r01)
{
	int i = int(floor(settings.itnum * r01));
	float y = fract(settings.itnum * r01);
	//biased coin flip
	if (y < iterators[i].reset_prob)
		return i;
	return iterators[i].reset_alias;
}

int alias_sample_xaos(int iterator_index, float r01)
{
	int i = int(floor(settings.itnum * r01));
	float y = fract(settings.itnum * r01);
	vec4 t = alias_tables[iterator_index * settings.itnum + i];
	float prob = t.x;
	int alias = int(t.y);
	if (y < prob)
		return i;
	return alias;
}

//from [0,1] uniform to [0,inf] ln
float startingDistribution(float uniformR)
{
	float a = pow(uniformR, 1.0/3.0);//avoid center of sphere
	float curve = 1.578425;//1.578425: half of the values are < 0.5	//TODO: parameter?
	//float curve = 0.5 + 10.0 * pow(1.001, -dispatch_cnt);
	return -1.0 / curve * log(1.0 - a);
}

p_state reset_state(inout uint next)
{
	p_state p;
	//init points into a starting distribution
	float theta = TWOPI * random(next);
	float phi = acos(2.0 * random(next) - 1.0);
	float rho = startingDistribution(random(next));//[0,inf] ln
	//experiment: rho dependent on camera distance from origo
	rho *= 2.0 * length(settings.camera.position);
	float sin_phi = sin(phi);
	p.pos = vec4(
		rho * sin_phi * cos(theta),
		rho * sin_phi * sin(theta),
		rho * cos(phi),
		0.0//unused
	);
	float workgroup_random = f_hash4(seed, gl_WorkGroupID.x, uint(dispatch_cnt), next++);
	//p.iterator_index = int(/*random(next)*/workgroup_random * settings.itnum);
	p.iterator_index = alias_sample(workgroup_random);
	p.color_index = iterators[p.iterator_index].color_index;
	p.iteration_depth = 0;
	return p;
}

//Based on: https://pixinsight.com/doc/docs/InterpolationAlgorithms/InterpolationAlgorithms.html
float sinc(float x)
{
	if (x==0.0)
		return 1.0;
	return sin(PI * x) / (PI * x);
}
float Lanczos(float x, int n)
{
	//n>0
	if (abs(x) <= float(n))
		return sinc(x) * sinc(x / float(n));
	return 0.0;
}

float Mitchell_Netravali(float x /*,B, C*/)
{
	//const float B = 1.0 / 3.0;
	//const float C = 1.0 / 3.0;
	//best when B + 2*C = 1
	const float B = 0.35f;
	const float C = 0.325f;

	float a = abs(x);
	if (a < 1.0)
		return ((12.0 - 9.0 * B - 6.0 * C) * (a * a * a) + (-18.0 + 12.0 * B + 6.0 * C) * (a * a) + 6.0 - 2.0 * B) / 6.0;
	else if (1.0 <= a && a < 2.0)
		return ((-B - 6.0 * C) * (a * a * a) + (6.0 * B + 30.0 * C) * (a * a) + (-12.0 * B - 48.0 * C) * a + 8.0 * B + 24.0 * C) / 6.0;
	else
		return 0.0;
}

void accumulate_hit(ivec2 proj, vec4 color)
{
	int ipx = proj.x + proj.y * width;//pixel index
#ifdef GL_NV_shader_atomic_float
	//Use atomic float add if available. Slower but more accurate.
	atomicAdd(histogram[ipx].r, color.r);
	atomicAdd(histogram[ipx].g, color.g);
	atomicAdd(histogram[ipx].b, color.b);
	atomicAdd(histogram[ipx].w, color.w);//db
#else
	histogram[ipx].rgb += color.rgb;
	histogram[ipx].w += color.w;//db
#endif

}

void main() {
	const uint gid = gl_GlobalInvocationID.x;

	uint next = pcg_hash(54321 + seed);

	p_state p;
	if (reset_points_state == 1)//after invalidation
		p = reset_state(next);
	else
		p = state[gid];
	
	for (int i = 0; i < invocation_iters; i++)
	{
		//pick a random xaos weighted Transform index
		int r_index = -1;
		float r = f_hash4(seed, gl_WorkGroupID.x, uint(dispatch_cnt), i);//random(next);
		r_index = alias_sample_xaos(p.iterator_index, r);
		if (r_index == -1 || //no outgoing weight
            p.iteration_depth == -1 || //invalid point position
			f_hash4(seed, gl_WorkGroupID.x, uint(dispatch_cnt), next++) < settings.entropy) //chance to reset by entropy
		{//reset if invalid
			p = reset_state(next);
		}
		else
			p.iterator_index = r_index;

		Iterator selected_iterator = iterators[p.iterator_index];

		vec4 p0_pos = p.pos;
		vec3 p_ret = apply_transform(selected_iterator, p, next);//transform here
		p.pos.xyz = mix(p0_pos.xyz, p_ret + p0_pos.xyz * selected_iterator.tf_add, selected_iterator.tf_mix);

        if (any(isinf(p.pos.xyz)) || //at infinity
            any(isnan(p.pos.xyz)) || //reset by plugin
            dot(p.pos.xyz, p.pos.xyz) == 0.0) //at origo
        {//check if position is invalid
            p.iteration_depth = -1;//means invalid
            continue;
        }

		apply_coloring(selected_iterator, p0_pos, p.pos, p.color_index);
		p.iteration_depth++;

		if (p.iteration_depth < settings.warmup || selected_iterator.opacity == 0.0)
			continue;//avoid useless projection and histogram writes

		//perspective project
		vec2 projf = Project(settings.camera, p.pos.xyz, next);
        if (projf.x == -2.0)
            continue;//out of frame
        ivec2 proj = ivec2(int(round(projf.x)), int(round(projf.y)));

		vec4 color = vec4(getPaletteColor(p.color_index), selected_iterator.opacity);

		//TODO: this is the same as dof
		float defocus = max(0, abs(dot(p.pos.xyz - settings.camera.focus_point.xyz, -settings.camera.forward.xyz)) - settings.camera.depth_of_field);

		if (settings.fog_effect > 0.0f)
		{//optional fog effect
			float fog_mask = 2.0*(1.0 - 1.0 / (1.0 + pow(1.0 + settings.fog_effect, - defocus + settings.camera.depth_of_field)));
			fog_mask = clamp(fog_mask, 0.0, 1.0);
			color.w *= fog_mask;
		}
		if (color.w == 0.0)
			continue;//avoid useless histogram writes

		//mark area in focus with red
		if (settings.mark_area_in_focus != 0 && defocus < 0.01)
			color = vec4(1.0, 0.0, 0.0, 2.0);

		color.xyz *= color.w;

		if (settings.max_filter_radius > 0/* && proj.x>width/2*/)
		{
			//TODO: determine filter_radius based on settings.filter_method
			const int filter_radius = int(settings.max_filter_radius);

			//for (int ax = -filter_radius; ax <= filter_radius; ax++)
			int ax = -filter_radius + int(random(next) * 2 * filter_radius);
			{
				//for (int ay = -filter_radius; ay <= filter_radius; ay++)
				int ay = -filter_radius + int(random(next) * 2 * filter_radius);
				{
					ivec2 nb = proj + ivec2(ax, ay);
					float pd = distance(vec2(nb), projf);

					//TODO: use settings.filter_method to pick one
					//float aw = max(0.0, 1.0-pd);
					//float aw = max(0.0, Lanczos(pd, 2));
					float aw = max(0.0, Mitchell_Netravali(pd)) * filter_radius * filter_radius * 2 * 2;
                    if (settings.camera.projection_type == 1)
                        nb.x = nb.x % width;
					if (nb.x >= 0 && nb.x < width && nb.y >= 0 && nb.y < height)
						accumulate_hit(nb, aw * color);
				}
			}
		}
		else
		{
			accumulate_hit(proj, color);
		}
	
	}

	state[gid] = p;

}
