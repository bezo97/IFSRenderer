#version 450

uniform sampler2D t1;//new render frame
uniform sampler2D t2;//last aa frame

uniform int width = 1920;
uniform int height = 1080;

// Temporal AA based on Epic Games' implementation:
// https://de45xmedrsdbp.cloudfront.net/Resources/files/TemporalAA_small-59732822.pdf
// Based on https://www.shadertoy.com/view/4tcXD2
#define mip_blur 0.75
// YUV-RGB conversion routine from Hyper3D. Luminance (x) & Chroma (y,z)
vec3 encodePalYuv(vec3 rgb)
{
	rgb = pow(rgb, vec3(2.0)); // gamma correction
	return vec3(
		dot(rgb, vec3(0.299, 0.587, 0.114)),
		dot(rgb, vec3(-0.14713, -0.28886, 0.436)),
		dot(rgb, vec3(0.615, -0.51499, -0.10001))
	);
}
vec3 decodePalYuv(vec3 yuv)
{
	vec3 rgb = vec3(
		dot(yuv, vec3(1., 0., 1.13983)),
		dot(yuv, vec3(1., -0.39465, -0.58060)),
		dot(yuv, vec3(1., 2.03211, 0.))
	);
	return pow(rgb, vec3(1.0 / 2.0)); // gamma correction
}
vec4 TemporalAA()
{
	vec2 uv = gl_FragCoord.xy / vec2(float(width), float(height));
	vec2 off = 1.0 / vec2(float(width), float(height));

	//sample new render frame
	vec3 in0 = texture(t1, uv).xyz;
	vec3 in1 = texture(t1, uv + vec2(+off.x, 0.0), mip_blur).xyz;
	vec3 in2 = texture(t1, uv + vec2(-off.x, 0.0), mip_blur).xyz;
	vec3 in3 = texture(t1, uv + vec2(0.0, +off.y), mip_blur).xyz;
	vec3 in4 = texture(t1, uv + vec2(0.0, -off.y), mip_blur).xyz;
	vec3 in5 = texture(t1, uv + vec2(+off.x, +off.y), mip_blur).xyz;
	vec3 in6 = texture(t1, uv + vec2(-off.x, +off.y), mip_blur).xyz;
	vec3 in7 = texture(t1, uv + vec2(+off.x, -off.y), mip_blur).xyz;
	vec3 in8 = texture(t1, uv + vec2(-off.x, -off.y), mip_blur).xyz;

	//sample last aa frame
	vec3 lastColor = texture(t2, uv).xyz;
	float lastMixRate = texture(t2, uv).w;

	float mixRate = min(lastMixRate, 0.5);
	vec3 antialiased = mix(lastColor * lastColor, in0 * in0, mixRate);
	antialiased = sqrt(antialiased);

	antialiased = encodePalYuv(antialiased);
	in0 = encodePalYuv(in0);
	in1 = encodePalYuv(in1);
	in2 = encodePalYuv(in2);
	in3 = encodePalYuv(in3);
	in4 = encodePalYuv(in4);
	in5 = encodePalYuv(in5);
	in6 = encodePalYuv(in6);
	in7 = encodePalYuv(in7);
	in8 = encodePalYuv(in8);
	vec3 minColor = min(min(min(in0, in1), min(in2, in3)), in4);
	vec3 maxColor = max(max(max(in0, in1), max(in2, in3)), in4);
	minColor = mix(minColor,
		min(min(min(in5, in6), min(in7, in8)), minColor), 0.5);
	maxColor = mix(maxColor,
		max(max(max(in5, in6), max(in7, in8)), maxColor), 0.5);
	vec3 preclamping = antialiased;
	antialiased = clamp(antialiased, minColor, maxColor);

	mixRate = 1.0 / (1.0 / mixRate + 1.0);

	vec3 diff = antialiased - preclamping;
	float clampAmount = dot(diff, diff);

	mixRate += clampAmount * 4.0;
	mixRate = clamp(mixRate, 0.05, 0.5);

	antialiased = decodePalYuv(antialiased);

	return vec4(antialiased, mixRate);
}

//based on https://www.shadertoy.com/view/4tVSDm
float grayscale(vec3 image) {
	return dot(image, vec3(0.3, 0.59, 0.11));
}
float normpdf(in float x, in float sigma)
{
	return 0.39894 * exp(-0.5 * x * x / (sigma * sigma)) / sigma;
}
vec4 temporalResolve()
{
	vec2 uv = gl_FragCoord.xy / vec2(float(width), float(height));

	vec3 imageacc = max(texture(t2, uv).rgb, vec3(0.0));
	vec3 image = max(texture(t1, uv).rgb, vec3(0.0));

	const int mSize = 7;//param
	const int kSize = (mSize - 1) / 2;
	float kernel[mSize];
	vec3 imageblurred = vec3(0.0);

	//create the 1-D kernel
	float sigma = 2.;
	float Z = 0.0;
	for (int j = 0; j <= kSize; ++j)
	{
		kernel[kSize + j] = kernel[kSize - j] = normpdf(float(j), sigma);
	}

	//get the normalization factor (as the gaussian has been clamped)
	for (int j = 0; j < mSize; ++j)
	{
		Z += kernel[j];
	}

	//read out the texels
	for (int i = -kSize; i <= kSize; ++i)
	{
		for (int j = -kSize; j <= kSize; ++j)
		{
			imageblurred += kernel[kSize + j] * kernel[kSize + i] * texture(t1, (gl_FragCoord.xy + vec2(float(i), float(j))) / vec2(float(width), float(height))).rgb;
		}
	}
	imageblurred = imageblurred / (Z * Z);

	image = min(image, imageblurred * 1.25); // reduce fireflies 
	image = max(image, imageblurred * 0.75); // reduce darkflies

	imageacc = image;

	return vec4(imageacc, 1.0);
}

void main(void)
{
	vec2 uv = gl_FragCoord.xy / vec2(float(width), float(height));
	vec4 taa = TemporalAA();
	vec4 tr = temporalResolve();
	gl_FragColor = /*uv.x < 0.5 ? texture(t1, uv) : */vec4(max(taa.xyz, tr.xyz), taa.w);
}