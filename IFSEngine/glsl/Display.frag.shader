#version 430
#extension GL_ARB_explicit_attrib_location : enable
uniform int width = 1920;
uniform int height = 1080;

layout(location = 0) out vec4 color;
layout(std140, binding = 1) buffer histogrambuf
{
	vec4 histogram[];
};

uniform uint ActualDensity = 1;
uniform float Brightness = 1.0;
uniform float InvGamma = 1.0;
uniform float GammaThreshold = 0.0;
uniform float Vibrancy = 1.0;
uniform vec3 BackgroundColor = vec3(0.0,0.0,0.0);

//tmp option to enable wip density estimation
uniform bool EnableDE = false;

float log10(float x)
{
	return 0.30102999565 * log(x);
}

//tonemapping algo based on Apophysis
vec4 Tonemap(vec4 histogram)
{
	vec3 acc_c = histogram.xyz;//accumulated color
	float acc_h = histogram.w;//how many times this pixel was hit
	
	float ls = Brightness * log10(1.0 + acc_h / ActualDensity) / acc_h;
	vec4 fp = histogram*ls;

	//gamma linearization
	float funcval = 0.0;
	if(GammaThreshold != 0.0)
	{
		funcval = pow(GammaThreshold, InvGamma - 1);
	}
	float alpha;
	if(fp.w < GammaThreshold)
	{
		float frac = fp.w / GammaThreshold;
		alpha = (1.0 - frac) * fp.w * funcval + frac * pow(fp.w, InvGamma);
	}
	else
		alpha = pow(fp.w, InvGamma);

	ls = Vibrancy * alpha / fp.w;
	alpha = clamp(alpha, 0.0, 1.0);

	vec3 o = ls * fp.rgb + (1.0-Vibrancy) * pow(fp.rgb, vec3(InvGamma));
	o = clamp(o, vec3(0.0), vec3(1.0));

	o += (1.0-alpha) * BackgroundColor;
	o = clamp(o, vec3(0.0), vec3(1.0));

	return vec4(o, alpha);
}

//based on https://www.shadertoy.com/view/4tVSDm
float grayscale(vec3 image) {
	return dot(image, vec3(0.3, 0.59, 0.11));
}
float normpdf(in float x, in float sigma)
{
	return exp(-0.5 * x * x / sigma) / 1.0;
}
float DensityEstimation()
{
	int px = int(gl_FragCoord.x);
	int py = int(gl_FragCoord.y);
	vec2 uv = vec2(gl_FragCoord.x / float(width), gl_FragCoord.y / float(height));
	int pxi = px + py * width;

	const int mSize = 21;//TODO: param
	const int kSize = (mSize - 1) / 2;
	float kernel[mSize];
	float de = 0.0;

	//create the 1-D kernel
	float sigma = clamp((ActualDensity - histogram[pxi].w) / ActualDensity,0.0,1.0) * kSize / 2.0;
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

	for (int i = -kSize; i <= kSize; ++i)
	{
		for (int j = -kSize; j <= kSize; ++j)
		{
			if (px + i<0 || px + i>width - 1 || py + j < 0 || py + j>height - 1)
				break;//TODO: Z-=
			de += kernel[kSize + j] * kernel[kSize + i] * histogram[(px+i) + (py+j) * width].w;
		}
	}
	de /= Z * Z;

	return de;
}

void main(void)
{
	int px = int(gl_FragCoord.x);
	int py = int(gl_FragCoord.y);
	vec2 uv = vec2(gl_FragCoord.x/float(width),gl_FragCoord.y/float(height));
	int pxi = px+py*width;

	if (EnableDE)
	{
		float de = DensityEstimation();
		vec3 c = (histogram[pxi].rgb / histogram[pxi].w) * de;
		color = Tonemap(vec4(c, de));
	}
	else
		color = Tonemap(histogram[pxi]);
}
