#version 430
#extension GL_ARB_explicit_attrib_location : enable

uniform int width = 1920;
uniform int height = 1080;
uniform uint ActualDensity = 1;

layout(location = 0) out vec4 ls;
layout(std140, binding = 1) buffer histogrambuf
{
	vec4 histogram[];
};


float log10(float x)
{
	return 0.30102999565 * log(x);
}
//log density scaling
vec4 logscale(vec4 acc)
{
	if (acc.w < 1.0)
		reutrn vec4(0.0);
	float ls = log10(1.0 + acc.w / ActualDensity) / acc.w;
	vec4 lh = acc * ls;
	return clamp(lh, vec4(0.0), vec4(1.0));
}

void main(void)
{
	int px = int(gl_FragCoord.x);
	int py = int(gl_FragCoord.y);
	vec2 uv = vec2(gl_FragCoord.x/float(width),gl_FragCoord.y/float(height));
	int pxi = px+py*width;

	ls = logscale(histogram[pxi]);
}
