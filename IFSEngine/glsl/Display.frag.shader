#version 450
#extension GL_ARB_explicit_attrib_location : enable
uniform int width = 1920;
uniform int height = 1080;

layout(location = 0) out vec4 color;
layout(std140, binding = 1) buffer histogrambuf
{
	vec4 histogram[];
};

uniform int IterAcc = 0;
uniform float Brightness = 1.0;
uniform float InvGamma = 1.0;
uniform float GammaThreshold = 0.0;
uniform float Vibrancy = 1.0;
uniform vec3 BackgroundColor = vec3(0.0,0.0,0.0);

float log10(float x)
{
	return 0.30102999565 * log(x);
}

//tonemapping algo based on Apophysis
vec4 Tonemap(vec4 histogram)
{
	vec3 acc_c = histogram.xyz;//accumulated color
	float acc_h = histogram.w;//how many times this pixel was hit

	if(acc_h < 1.0)
		return vec4(BackgroundColor, 1.0);//TODO: transparent bg

	float act_density = float(IterAcc)*0.0000001;//apo:0.001

	float ls = Brightness * log10(1.0 + acc_h / act_density) / acc_h;

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
	o += (1.0-alpha) * BackgroundColor;

	o = clamp(o, vec3(0.0), vec3(1.0));
	return vec4(o, alpha);
}


void main(void)
{
	int px = int(gl_FragCoord.x);
	int py = int(gl_FragCoord.y);
	vec2 uv = vec2(gl_FragCoord.x/float(width),gl_FragCoord.y/float(height));
	int pxi = px+py*width;

	color = Tonemap(histogram[pxi]);
}