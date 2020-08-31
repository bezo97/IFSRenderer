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

//TODO: remove
struct Affine
{
	float ox;
	float oy;
	float oz;

	float xx;
	float xy;
	float xz;

	float yx;
	float yy;
	float yz;

	float zx;
	float zy;
	float zz;
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

//Shader Storage Buffer Objects
//read and write, dynamic size

layout(std140, binding = 0) coherent buffer histogram_buffer
{
	vec4 histogram[];
};
layout(std140, binding = 1) buffer points_buffer
{
	vec4 pointsstate[];//for each thread
};

layout(std430, binding = 2) buffer last_transform_index_buffer
{
	int last_tf_index[];//remember previous tranform index for each thread, needed for xaos
};

//Uniform Buffer Objects
//read-only, fixed size

layout(std140, binding = 3) uniform settings_ubo
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

layout(std140, binding = 4) uniform iterators_ubo
{
	Iterator iterators[MAX_ITERATORS];
};

layout(std140, binding = 5) uniform palette_ubo
{
	vec4 palette[MAX_PALETTE_COLORS];
};

layout(binding = 6) uniform transform_params_ubo
{
	float tfParams[MAX_PARAMS];//parameters of all transforms
};

layout(binding = 7) uniform xaos_ubo
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
float random(float f) {
	const uint mantissaMask = 0x007FFFFFu;
	const uint one = 0x3F800000u;

	uint h = hash(floatBitsToUint(f));
	h &= mantissaMask;
	h |= one;

	float  r2 = uintBitsToFloat(h);
	return r2 - 1.0;
}
float random(float f1, float f2, uint nextSample) {
	const uint mantissaMask = 0x007FFFFFu;
	const uint one = 0x3F800000u;

	uint h = hash(uvec3(floatBitsToUint(f1), floatBitsToUint(f2), nextSample));
	h &= mantissaMask;
	h |= one;

	float  r2 = uintBitsToFloat(h);
	return r2 - 1.0;
}

float randhash(uint nextSample)
{
	return random(gl_GlobalInvocationID.x, settings.dispatchCnt, nextSample);
}

ivec2 Project(CameraParameters c, vec3 p, float ra, float rl)
{

	vec3 pointDir = normalize(p - c.position.xyz);
	if (dot(pointDir, c.forward.xyz) < 0.0)
		return ivec2(-2, -2);

	vec4 normalizedPoint = c.viewProjectionMatrix * vec4(p.xyz, 1.0f);
	normalizedPoint /= normalizedPoint.w;

	//dof
	float ratio = width / float(height);
	float dof = settings.depth_of_field * max(0, abs(dot(p - settings.focuspoint.xyz, -c.forward.xyz)) - settings.focusarea); //use focalplane normal
	normalizedPoint.xy += pow(rl, 0.5f) * dof * vec2(cos(ra * TWOPI), sin(ra * TWOPI));

	ivec2 o = ivec2(//image center
		int((normalizedPoint.x + 1) * width / 2.0f),
		int((normalizedPoint.y * ratio + 1) * height / 2.0f)
	);
	
	return o;

}

vec3 affine(Affine aff, vec3 input) {
	float px = aff.xx * input.x + aff.xy * input.y + aff.xz * input.z + aff.ox;
	float py = aff.yx * input.x + aff.yy * input.y + aff.yz * input.z + aff.oy;
	float pz = aff.zx * input.x + aff.zy * input.y + aff.zz * input.z + aff.oz;
	return vec3(px, py, pz);
}

//TODO: Compile this based on TransformFunction.ShaderCode, alphabetical order, add iterator hash
//Affine
#define OX (tfParams[p_cnt+0])
#define OY (tfParams[p_cnt+1])
#define OZ (tfParams[p_cnt+2])
#define XX (tfParams[p_cnt+3])
#define XY (tfParams[p_cnt+4])
#define XZ (tfParams[p_cnt+5])
#define YX (tfParams[p_cnt+6])
#define YY (tfParams[p_cnt+7])
#define YZ (tfParams[p_cnt+8])
#define ZX (tfParams[p_cnt+9])
#define ZY (tfParams[p_cnt+10])
#define ZZ (tfParams[p_cnt+11])

//Waves
#define Waves_Weight (tfParams[p_cnt+6]) //remove?
#define FreqX (tfParams[p_cnt+0])
#define FreqY (tfParams[p_cnt+1])
#define FreqZ (tfParams[p_cnt+2])
#define ScaleX (tfParams[p_cnt+3])
#define ScaleY (tfParams[p_cnt+4])
#define ScaleZ (tfParams[p_cnt+5])

//Moebius
#define Alpha (tfParams[p_cnt+0])
#define AX (tfParams[p_cnt+1])
#define AY (tfParams[p_cnt+2])
#define AZ (tfParams[p_cnt+3])
#define BX (tfParams[p_cnt+4])
#define BY (tfParams[p_cnt+5])
#define BZ (tfParams[p_cnt+6])
#define M11 (tfParams[p_cnt+7])
#define M12 (tfParams[p_cnt+8])
#define M13 (tfParams[p_cnt+9])
#define M21 (tfParams[p_cnt+10])
#define M22 (tfParams[p_cnt+11])
#define M23 (tfParams[p_cnt+12])
#define M31 (tfParams[p_cnt+13])
#define M32 (tfParams[p_cnt+14])
#define M33 (tfParams[p_cnt+15])
#define Yaw (tfParams[p_cnt+16])
#define Pitch (tfParams[p_cnt+17])
#define Roll (tfParams[p_cnt+18])

vec3 Apply(Iterator iter, vec3 input)
{
	vec3 p = input;
	int p_cnt = iter.tfParamsStart;

	if (iter.tfId == 0)
	{//affine

		//get affine transform parameters
		int p_cnt = iter.tfParamsStart;
		Affine aff;
		aff.ox = OX;
		aff.oy = OY;
		aff.oz = OZ;
		aff.xx = XX;
		aff.xy = XY;
		aff.xz = XZ;
		aff.yx = YX;
		aff.yy = YY;
		aff.yz = YZ;
		aff.zx = ZX;
		aff.zy = ZY;
		aff.zz = ZZ;

		p = affine(aff, p.xyz);
	}
	else if (iter.tfId == 1)
	{//spherical
		float r = length(p);
		p = 1.0f/*weight*/ * p / (r * r);
	}
	else if (iter.tfId == 2)
	{//waves

		/*
		vOut.x = parVars[WEIGHT_0_0] * fma(parVars[WAVES2_SCALEX_0], sin(vIn.y * parVars[WAVES2_FREQX_0]), vIn.x);
		vOut.y = parVars[WEIGHT_0_0] * fma(parVars[WAVES2_SCALEY_0], sin(vIn.x * parVars[WAVES2_FREQY_0]), vIn.y);
		vOut.z = parVars[WEIGHT_0_0] * fma(parVars[WAVES2_SCALEZ_0], sin(precalcSqrtSumSquares * parVars[WAVES2_FREQZ_0]), vIn.z);
		*/
		vec3 o;

		o.x = Waves_Weight * fma(ScaleX, sin(p.y * FreqX), p.x);
		o.y = Waves_Weight * fma(ScaleY, sin(p.z * FreqY), p.y);
		o.z = Waves_Weight * fma(ScaleZ, sin(p.x * FreqZ), p.z);

		p = o;
	}
	else if (iter.tfId == 3)
	{//foci
		float exp_x = exp(p.x);
		float exp_x_2 = exp_x * 0.5;
		float exp_nz = 0.5 / exp_x;
		float cos_y = cos(p.y);
		float sin_y = sin(p.y);
		float cos_z = cos(p.z);
		float sin_z = sin(p.z);
		float r = 1.0/*weight*/ / (exp_x_2 + exp_nz - cos_y - cos_z);
		float fx = (exp_x_2 - exp_nz);
		p = vec3(fx, sin_y, sin_z) * r;
	}
	else if (iter.tfId == 4)
	{//loonie
		float weight = 1.0;
		float w2 = weight * weight;
		float r2 = dot(p, p);
		float r;
		if (r2 < w2)
			r = weight * sqrt(w2 / r2 - 1.0);
		else
			r = weight;
		p = r * p;
	}
	else if (iter.tfId == 5)
	{//moebius
		vec3 a = vec3(AX, AY, AZ);
		vec3 b = vec3(BX, BY, BZ);
		mat3 m = mat3(
			M11, M21, M31, // first column 
			M12, M22, M32, // second column
			M13, M23, M33  // third column
		);
		//https://en.wikipedia.org/wiki/M%C3%B6bius_transformation#Higher_dimensions
		return b + (Alpha * m * (p - a)) / pow(length(p - a), 2.0);
	}

	return p;
}

void ApplyShader(inout vec2 p_shader, Iterator it, vec3 p0, vec3 p)
{
	float in_color = p_shader.x;
	float in_opacity = p_shader.y;
	float speed = it.color_speed;
	if (it.shading_mode == 1)
	{
		float p_delta = length(p - p0);
		speed *= (1.0 - 1.0 / (1.0 + p_delta));
	}
	float out_color = fract ( speed * it.color_index + (1.0f - speed) * in_color );
	float out_opacity = it.opacity;
	p_shader = vec2(out_color, out_opacity);
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
	return -1.0 / curve * log(1.0 - a);
}

void main() {
	uint gid = gl_GlobalInvocationID.x;

	int next = 34567;//TODO: option to change this seed by animation frame number

	if (settings.resetPointsState == 1)
	{//usually on first dispatch, or when number of threads changes
		//randomize starting iterator
		last_tf_index[gid] = int(randhash(next++) * settings.itnum);
		
		//init points into a starting distribution
		float theta = TWOPI * randhash(next++);
		float phi = acos(2.0 * randhash(next++) - 1.0);
		float rho = startingDistribution(randhash(next++));//[0,inf] ln
		float sin_phi = sin(phi);
		pointsstate[gid] = vec4(
			rho * sin_phi * cos(theta),
			rho * sin_phi * sin(theta),
			rho * cos(phi),
			randhash(next++)
		);
	}

	vec3 p = pointsstate[gid].xyz;
	vec2 p_shader = vec2(pointsstate[gid].w, 1.0);


	for (int i = 0; i < settings.pass_iters; i++)
	{
		//pick a random xaos weighted Transform index
		int r_index = -1;
		float r = randhash(next++);
		r *= iterators[last_tf_index[gid]].wsum;//sum outgoing xaos weight
		float w_acc = 0.0f;//accumulate previous iterator xaos weights until r randomly chosen iterator reached
		for (int j = 0; j < settings.itnum; j++)
			if (w_acc < r) {
				w_acc += xaos[last_tf_index[gid] * settings.itnum + j];
				r_index = j;
			}
			else
				break;
		
		//TODO: option: reset position if no change for n iterations
		if (r_index == -1)
		{//reset position if no weight out
			//idea: place next to another point instead of random reset
			p = pointsstate[(gid + 1)].xyz;
			p_shader.x = pointsstate[(gid + 1)].w;
			p_shader.y = 1.0;
			r_index = last_tf_index[(gid + 1)];
		}
		if (isinf(p.x) || isinf(p.y) || isinf(p.z) || (p.x == 0 && p.y == 0 && p.z == 0))
		{//reset position if too far
			//TODO: make this optional?
			//isinf: For each element i of the result, isinf returns true if x[i] is posititve or negative floating point infinity and false otherwise.
			//idea: place next to another point instead of random reset
			p = pointsstate[(gid + 1)].xyz;
			p_shader.x = pointsstate[(gid + 1)].w;
			p_shader.y = 1.0;
			r_index = last_tf_index[(gid + 1)];
		}
		last_tf_index[gid] = r_index;


		vec3 p0 = p;
		p = Apply(iterators[r_index], p);//transform here
		ApplyShader(p_shader, iterators[r_index], p0, p);

		//perspective project
		float ra1 = randhash(next++);
		float ra2 = randhash(next++);
		ivec2 proj = Project(settings.camera, p, ra1, ra2);
	
		//lands on the histogram && warmup
		if (proj.x >= 0 && proj.x < width && proj.y >= 0 && proj.y < height && !(i < settings.warmup && settings.resetPointsState == 1))
		{
			vec4 color = vec4(getPaletteColor(p_shader.x), p_shader.y);
			if (settings.fog_effect > 0.0f)
			{//optional fog effect
				float pr1 = 1.0f / pow(1.0f + length(settings.focuspoint.xyz - p), settings.fog_effect);
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
	
	pointsstate[gid] = vec4(p, p_shader.x);

}
