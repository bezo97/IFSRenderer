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
	float wsum;//outgoing xaos weights sum
	float color_speed;
	float color_index;//color index, 0 - 1
	float opacity;
	int tfId;
	int tfParamsStart;
	int shading_mode;//0: default, 1: delta_p
	int padding0;
};

struct p_state
{
	vec4 pos;
	float color_index;
	float dummy0;
	int last_tf_index;
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
	int padding0;
	int padding1;
} settings;

layout(std140, binding = 3) uniform iterators_ubo
{
	Iterator iterators[MAX_ITERATORS];
};

layout(std140, binding = 4) uniform palette_ubo
{
	vec4 palette[MAX_PALETTE_COLORS];
};

layout(binding = 5) uniform transform_params_ubo
{
	float tfParams[MAX_PARAMS];//parameters of all transforms
};

layout(binding = 6) uniform xaos_ubo
{
	float xaos[MAX_XAOS];//xaos matrix: weights to Iterators
};

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

ivec2 Project(CameraParameters c, vec4 p, inout uint next)
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

	ivec2 o = ivec2(//image center
		int((normalizedPoint.x + 1) * width / 2.0f),
		int((normalizedPoint.y * ratio + 1) * height / 2.0f)
	);
	
	return o;

}

vec3 apply_transform(Iterator iter, vec3 input)
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
	float sin_phi = sin(phi);
	p.pos = vec4(
		rho * sin_phi * cos(theta),
		rho * sin_phi * sin(theta),
		rho * cos(phi),
		0.0//unused
	);
	p.color_index = random(next);
	p.last_tf_index = int(/*random(next)*/f_hash(gl_WorkGroupID.x, settings.dispatchCnt, next) * settings.itnum);
	p.warmup_cnt = 0;
	return p;
}

void main() {
	const uint gid = gl_GlobalInvocationID.x;

	uint next = 34567;//TODO: option to change this seed by animation frame number

	p_state p;
	if (settings.resetPointsState == 1)
		p = reset_state(next);
	else
		p = state[gid];

	for (int i = 0; i < settings.pass_iters; i++)
	{
		//pick a random xaos weighted Transform index
		int r_index = -1;
		float r = f_hash(gl_WorkGroupID.x, settings.dispatchCnt, i);//random(next);
		r *= iterators[p.last_tf_index].wsum;//sum outgoing xaos weight
		float w_acc = 0.0f;//accumulate previous iterator xaos weights until r randomly chosen iterator reached
		for (int j = 0; j < settings.itnum; j++)
			if (w_acc < r) {
				w_acc += xaos[p.last_tf_index * settings.itnum + j];
				r_index = j;
			}
			else
				break;
		p.last_tf_index = r_index;
		
		if (r_index == -1 || 
			any(isinf(p.pos)) || (p.pos.x == 0 && p.pos.y == 0 && p.pos.z == 0))//TODO: optional/remove
		{//reset if invalid
			//TODO: idea: detect if the only next iterator is self, reset after x iterations if
			p = reset_state(next);
		}

		Iterator selected_iterator = iterators[r_index];

		vec4 p0_pos = p.pos;
		p.pos.xyz = apply_transform(selected_iterator, p.pos.xyz);//transform here
		apply_coloring(selected_iterator, p0_pos, p.pos, p.color_index);

		//perspective project
		ivec2 proj = Project(settings.camera, p.pos, next);
	
		//lands on the histogram && warmup
		if (proj.x >= 0 && proj.x < width && proj.y >= 0 && proj.y < height && !(i < settings.warmup && settings.resetPointsState == 1))
		{
			vec4 color = vec4(getPaletteColor(p.color_index), selected_iterator.opacity);
			if (settings.fog_effect > 0.0f)
			{//optional fog effect
				float pr1 = 1.0f / pow(1.0f + length(settings.focuspoint.xyz - p.pos.xyz), settings.fog_effect);
				pr1 = clamp(pr1, 0.0f, 1.0f);
				color.w *= pr1;
			}
			color.xyz *= color.w;

			int ipx = proj.x + proj.y * width;//pixel index

			//accumulate hit
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
	
	}

	state[gid] = p;

}
