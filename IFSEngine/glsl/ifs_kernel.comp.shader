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

struct CameraParameters
{
	mat4 viewProjectionMatrix;
	vec4 position;
	vec4 forward;
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
	//current view:
	CameraParameters camera;
	vec4 focuspoint;//from camera,focusdistance
	float fog_effect;
	float depth_of_field;
	float focusdistance;
	float focusarea;//focal plane -> focal 'area'
	//current frame:
	int itnum;//length of iterators
	int pass_iters;//iterations per pass
	int dispatchCnt;
	int palettecnt;

	int resetPointsState;
	int warmup;
	float entropy;
	int max_filter_radius;

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

//random hash without sin: http://amindforeverprogramming.blogspot.com/2013/07/random-floats-in-glsl-330.html
uint hash(uint x) {
	x += (x << 10u);
	x ^= (x >> 6u);
	x += (x << 3u);
	x ^= (x >> 11u);
	x += (x << 15u);
	return x;
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

float random(inout uint nextSample)
{
	return f_hash(gl_GlobalInvocationID.x, settings.dispatchCnt, nextSample++);
}

vec2 Project(CameraParameters c, vec4 p, inout uint next)
{
	vec3 pointDir = normalize(p.xyz - c.position.xyz);
	if (dot(pointDir, c.forward.xyz) < 0.0)
		return ivec2(-2, -2);

	vec4 normalizedPoint = c.viewProjectionMatrix * vec4(p.xyz, 1.0f);
	normalizedPoint /= normalizedPoint.w;

	//dof
	float ratio = width / float(height);
	float dof = settings.depth_of_field * max(0, abs(dot(p.xyz - settings.focuspoint.xyz, -c.forward.xyz)) - settings.focusarea); //use focalplane normal
	float ra = random(next);
	float rl = random(next);
	normalizedPoint.xy += pow(rl, 0.5f) * dof * vec2(cos(ra * TWOPI), sin(ra * TWOPI));

	return vec2(
		(normalizedPoint.x + 1) * width / 2.0f,
		(normalizedPoint.y * ratio + 1) * height / 2.0f);
}

vec3 apply_transform(Iterator iter, vec3 input, inout uint next)
{
	vec3 p = input;
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
	//float curve = 0.5 + 10.0 * pow(1.001, -settings.dispatchCnt);
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
	p.color_index = random(next);
	float workgroup_random = f_hash(gl_WorkGroupID.x, settings.dispatchCnt, next);
	//p.iterator_index = int(/*random(next)*/workgroup_random * settings.itnum);
	p.iterator_index = alias_sample(workgroup_random);
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
	float B = 1.0 / 3.0;
	float C = 1.0 / 3.0;

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
	if (settings.resetPointsState == 1)//after invalidation
		p = reset_state(next);
	else
		p = state[gid];

	for (int i = 0; i < settings.pass_iters; i++)
	{
		//pick a random xaos weighted Transform index
		int r_index = -1;
		float r = f_hash(gl_WorkGroupID.x, settings.dispatchCnt, i);//random(next);
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
		ivec2 proj = ivec2(int(projf.x), int(projf.y));
		vec2 proj_offset = (fract(projf) - vec2(0.5))*2.0;
	
		//lands on the histogram && warmup
		if (proj.x >= 0 && proj.x < width && proj.y >= 0 && proj.y < height && (settings.warmup < p.warmup_cnt))
		{
			vec4 color = vec4(getPaletteColor(p.color_index), selected_iterator.opacity);

			//lightsource experiment
			//vec3 surface_normal = normalize(p.pos.xyz - p0_pos.xyz);
			//vec3 light_pos = vec3(1.0, 1.0 + settings.focusarea, 1.0);
			//vec4 light_col = vec4(1.0, 1.0, 1.0, 1.0);
			//vec3 light_vec = light_pos - p.pos.xyz;
			//float light_falloff = clamp(1.0 / (1.0 + 1.0 * settings.focusdistance * dot(light_vec, light_vec)), 0.0, 1.0);
			//color = color * (1.0-dot(surface_normal, normalize(light_vec))) * light_col * light_falloff;

			if (settings.fog_effect > 0.0f)
			{//optional fog effect
				float fog_mask = 1.0 / (1.0 + pow(length(settings.focuspoint.xyz - p.pos.xyz) / settings.focusarea, settings.fog_effect));
				fog_mask = clamp(fog_mask, 0.0, 1.0);
				color.w *= fog_mask;
			}
			if (color.w == 0.0)
				continue;//avoid useless histogram writes

			color.xyz *= color.w;

			if (settings.max_filter_radius > 1/* && settings.dispatchCnt > 3*/)
			{
				const int filter_radius = 2 + int(settings.max_filter_radius / pow(1.0+(histogram[proj.x+proj.y*width].w), 0.4));
				//float w_acc = 0.0;
				for (int i = -filter_radius + 1; i < filter_radius; i++)
				{
					for (int j = -filter_radius + 1; j < filter_radius; j++)
					{
						//float wx = Lanczos(float(i) - projf.x + floor(projf.x), filter_radius);
						//float wy = Lanczos(float(j) - projf.y + floor(projf.y), filter_radius);
						float wx = Mitchell_Netravali((float(2*i) / filter_radius - projf.x + floor(projf.x)));
						float wy = Mitchell_Netravali((float(2*j) / filter_radius - projf.y + floor(projf.y)));
						//w_acc += wx * wy;//clamp
						ivec2 filter_proj = ivec2(int(floor(proj.x)) + i, int(floor(projf.y)) + j);
						accumulate_hit(filter_proj, color * clamp(wx * wy, 0.0, 1.0));
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