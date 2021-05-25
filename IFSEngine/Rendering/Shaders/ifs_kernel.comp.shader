#version 430
//#version 430 compatibility
//#extension GL_ARB_compute_shader : enable
//#extension GL_ARB_shader_storage_buffer_object : enable
#extension GL_NV_shader_atomic_float : enable

//precision highp float;

layout(local_size_x = 64) in;

#define PI		3.14159265358f
#define TWOPI	6.28318530718f

#define MAX_ITERATORS 100
#define MAX_PALETTE_COLORS 150
#define MAX_PARAMS (2 * MAX_ITERATORS)
#define MAX_XAOS (MAX_ITERATORS * MAX_ITERATORS)

struct camera_params
{
	mat4 view_proj_mat;
	vec4 position;
	vec4 forward;
	vec4 focus_point;
	float depth_of_field;
	float focus_distance;
	float focus_area;
	float padding0;
};

struct Iterator
{
	float color_speed;
	float color_index;//color index, 0 - 1
	float opacity;
	float reset_prob;
	int reset_alias;
	int tfId;
	int tfParamsStart;
	int shading_mode;//0: default, 1: delta_p
};

struct p_state
{
	vec4 pos;
	float color_index;
	float dummy0;
	int iterator_index;
	int warmup_cnt;
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
	int pass_iters;//iterations per pass
	int palettecnt;

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

layout(binding = 6) uniform transform_params_ubo
{
	float tfParams[MAX_PARAMS];//parameters of all transforms
};

/*layout(binding = 7) uniform xaos_ubo
{
	float xaos[MAX_XAOS];//xaos matrix: weights to Iterators
};*/

uniform int width;
uniform int height;
uniform int dispatch_cnt;
uniform int reset_points_state;

//pcg: https://www.reedbeta.com/blog/hash-functions-for-gpu-rendering/
uint pcg_hash(uint x)
{
	uint state = x * 747796405u + 2891336453u;
	uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
	return (word >> 22u) ^ word;
}
//random hash without sin: http://amindforeverprogramming.blogspot.com/2013/07/random-floats-in-glsl-330.html
uint no_sin_hash(uint x) {
	x += (x << 10u);
	x ^= (x >> 6u);
	x += (x << 3u);
	x ^= (x >> 11u);
	x += (x << 15u);
	return x;
}
uint hash(uint x)
{
	return pcg_hash(x);
}
uint hash(uvec2 v) {
	return hash(v.x ^ hash(v.y));
}

uint hash(uvec3 v) {
	return hash(v.x ^ hash(v.y) ^ hash(v.z));
}

uint hash(uvec4 v) {
	return hash(v.x ^ hash(v.y) ^ hash(v.z) ^ hash(v.w));
}
float f_hash(float f) {
	const uint mantissaMask = 0x007FFFFFu;
	const uint one = 0x3F800000u;

	uint h = hash(floatBitsToUint(f));
	h &= mantissaMask;
	h |= one;

	float  r2 = uintBitsToFloat(h);
	return r2 - 1.0;
}
float f_hash(float f1, float f2, uint nextSample) {
	const uint mantissaMask = 0x007FFFFFu;
	const uint one = 0x3F800000u;

	uint h = hash(uvec3(floatBitsToUint(f1), floatBitsToUint(f2), nextSample));
	h &= mantissaMask;
	h |= one;

	float  r2 = uintBitsToFloat(h);
	return r2 - 1.0;
}
float f_hash(float f1, float f2, float f3) {
	const uint mantissaMask = 0x007FFFFFu;
	const uint one = 0x3F800000u;

	uint h = hash(uvec3(floatBitsToUint(f1), floatBitsToUint(f2), floatBitsToUint(f3)));
	h &= mantissaMask;
	h |= one;

	float  r2 = uintBitsToFloat(h);
	return r2 - 1.0;
}

float random(inout uint nextSample)
{
	return f_hash(gl_GlobalInvocationID.x, dispatch_cnt, nextSample++);
}

vec2 Project(camera_params c, vec4 p, inout uint next)
{
	vec3 pointDir = normalize(p.xyz - c.position.xyz);
	if (dot(pointDir, c.forward.xyz) < 0.0)
		return ivec2(-2, -2);

	vec4 normalizedPoint = c.view_proj_mat * vec4(p.xyz, 1.0f);
	normalizedPoint /= normalizedPoint.w;

	//dof
	float ratio = width / float(height);
	float dof = c.depth_of_field * max(0, abs(dot(p.xyz - c.focus_point.xyz, -c.forward.xyz)) - c.focus_area); //use focalplane normal
	float ra = random(next);
	float rl = random(next);
	normalizedPoint.xy += pow(rl, 0.5f) * dof * vec2(cos(ra * TWOPI), sin(ra * TWOPI));

	return vec2(
		(normalizedPoint.x + 1) * width / 2.0f,
		(normalizedPoint.y * ratio + 1) * height / 2.0f);
}

vec3 apply_transform(Iterator iter, vec3 p_input, inout uint next)
{
	vec3 p = p_input;
	int p_cnt = iter.tfParamsStart;

	//snippets inserted on initialization
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

	color_index = fract ( speed * it.color_index + (1.0f - speed) * in_color );
}

vec3 getPaletteColor(float pos)
{
	float palettepos = pos * (settings.palettecnt - 1);
	int index = int(floor(palettepos));
	vec3 c1 = palette[index].xyz;
	vec3 c2 = palette[index+1].xyz;
	float a = fract(palettepos);
	//TODO: interpolate in a different color space?
	return mix(c1, c2, a);//lerp
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
	float workgroup_random = f_hash(gl_WorkGroupID.x, dispatch_cnt, next);
	//p.iterator_index = int(/*random(next)*/workgroup_random * settings.itnum);
	p.iterator_index = alias_sample(workgroup_random);
	p.color_index = iterators[p.iterator_index].color_index;
	p.warmup_cnt = 0;
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

	uint next = 34567;//TODO: option to change this seed by animation frame number

	p_state p;
	if (reset_points_state == 1)//after invalidation
		p = reset_state(next);
	else
		p = state[gid];

	for (int i = 0; i < settings.pass_iters; i++)
	{
		//pick a random xaos weighted Transform index
		int r_index = -1;
		float r = f_hash(gl_WorkGroupID.x, dispatch_cnt, i);//random(next);
		r_index = alias_sample_xaos(p.iterator_index, r);
		if (r_index == -1 || //no outgoing weight
			random(next) < settings.entropy || //chance to reset by entropy
			any(isinf(p.pos)) || (p.pos.x == 0 && p.pos.y == 0 && p.pos.z == 0))//TODO: optional/remove
		{//reset if invalid
			p = reset_state(next);
		}
		else
			p.iterator_index = r_index;

		Iterator selected_iterator = iterators[p.iterator_index];

		vec4 p0_pos = p.pos;
		p.pos.xyz = apply_transform(selected_iterator, p.pos.xyz, next);//transform here
		apply_coloring(selected_iterator, p0_pos, p.pos, p.color_index);
		p.warmup_cnt++;

		if (selected_iterator.opacity == 0.0)
			continue;//avoid useless projection and histogram writes

		//perspective project
		vec2 projf = Project(settings.camera, p.pos, next);
		ivec2 proj = ivec2(int(round(projf.x)), int(round(projf.y)));
		vec2 proj_offset = projf - vec2(proj);
	
		//lands on the histogram && warmup
		if (proj.x >= 0 && proj.x < width && proj.y >= 0 && proj.y < height && (settings.warmup < p.warmup_cnt))
		{
			vec4 color = vec4(getPaletteColor(p.color_index), selected_iterator.opacity);

			//TODO: this is the same as dof
			float defocus = max(0, abs(dot(p.pos.xyz - settings.camera.focus_point.xyz, -settings.camera.forward.xyz)) - settings.camera.focus_area);

			if (settings.fog_effect > 0.0f)
			{//optional fog effect
				float fog_mask = 2.0*(1.0 - 1.0 / (1.0 + pow(1.0 + settings.fog_effect, - defocus + settings.camera.focus_area)));
				fog_mask = clamp(fog_mask, 0.0, 1.0);
				color.w *= fog_mask;
			}
			if (color.w == 0.0)
				continue;//avoid useless histogram writes

			//mark plane of focus
			//if (defocus == 0.0)
				//color.rgb = vec3(2.0, 0.0, 0.0);

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
						vec2 nb = vec2(proj + ivec2(ax, ay));
						float pd = distance(nb, projf);

						//TODO: use settings.filter_method to pick one
						//float aw = max(0.0, 1.0-pd);
						//float aw = max(0.0, Lanczos(pd, 2));
						float aw = max(0.0, Mitchell_Netravali(pd)) * filter_radius * filter_radius * 2 * 2;
						if (nb.x >= 0 && nb.x < width && nb.y >= 0 && nb.y < height)
							accumulate_hit(ivec2(nb), aw * color);
					}
				}
			}
			else
			{
				accumulate_hit(proj, color);
			}

		}
	
	}

	state[gid] = p;

}