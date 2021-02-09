#version 430
#extension GL_ARB_explicit_attrib_location : enable

layout(location = 0) out vec4 color;

uniform int width = 1920;
uniform int height = 1080;
uniform uint max_density = 1;
uniform float brightness = 1.0;
uniform float inv_gamma = 1.0;
uniform float gamma_threshold = 0.0;
uniform float vibrancy = 1.0;
uniform vec3 bg_color = vec3(0.0, 0.0, 0.0);

layout(std140, binding = 0) buffer histogram_buffer
{
	vec4 histogram[];
};
layout(binding = 7) coherent buffer filter_acc_buffer
{
	float filter_acc[];
};

float log10(float x)
{
	return 0.30102999565 * log(x);
}
//log density scaling
vec4 logscale(vec4 acc)
{
	float ls = log10(1.0 + brightness * acc.w / max_density) / acc.w;
	vec4 lh = acc * ls;
	return lh;
}

//tonemapping algo based on Apophysis
vec4 tonemap(vec4 fp)
{
	//xyz: accumulated color (log)
	//w: how many times this pixel was hit (log)

	//gamma linearization
	float funcval = 0.0;
	if (gamma_threshold != 0.0)
	{
		funcval = pow(gamma_threshold, inv_gamma - 1);
	}
	float alpha;
	if (fp.w < gamma_threshold)
	{
		float frac = fp.w / gamma_threshold;
		alpha = (1.0 - frac) * fp.w * funcval + frac * pow(fp.w, inv_gamma);
	}
	else
		alpha = pow(fp.w, inv_gamma);

	float ls = vibrancy * alpha / fp.w;
	alpha = clamp(alpha, 0.0, 1.0);

	vec3 o = ls * fp.rgb + (1.0 - vibrancy) * pow(fp.rgb, vec3(inv_gamma));
	o = clamp(o, vec3(0.0), vec3(1.0));

	o += (1.0 - alpha) * bg_color;
	o = clamp(o, vec3(0.0), vec3(1.0));

	return vec4(o, alpha);
}

void main(void)
{
	int px = int(gl_FragCoord.x);
	int py = int(gl_FragCoord.y);
	vec2 uv = vec2(gl_FragCoord.x/float(width),gl_FragCoord.y/float(height));
	int pxi = px+py*width;

	vec4 c = histogram[pxi];
	float filter_norm = filter_acc[pxi] / histogram[pxi].w;
	c *= filter_norm;
	c = logscale(c);
	c = tonemap(c);
	color = c;
}
