#version 430

layout(location = 0) out vec4 color;

uniform sampler2D t1;//log hist

uniform int width = 1920;
uniform int height = 1080;

uniform float Brightness = 1.0;
uniform float InvGamma = 1.0;
uniform float GammaThreshold = 0.0;
uniform float Vibrancy = 1.0;
uniform vec3 BackgroundColor = vec3(0.0, 0.0, 0.0);
//tmp option to enable wip density estimation
uniform bool EnableDE = false;

//TODO: maybe do Brightness before DE, so zooming in on low density areas wont be blurry. Or use the focus distance??

//uses cone filter
vec4 DensityEstimation()
{
	int px = int(gl_FragCoord.x);
	int py = int(gl_FragCoord.y);
	vec2 uv = vec2(gl_FragCoord.x / float(width), gl_FragCoord.y / float(height));

	float de_threshold = 0.25;//TODO: param
	float w = clamp(de_threshold - texture(t1, uv).w, 0.0, 1.0);

	vec4 de = vec4(0.0);
	const int kSize = int(w * 20.0);//TODO: param
	if (kSize == 0)
		return texture(t1, uv);

	float wnorm = 0.0;
	for (int i = -kSize; i <= kSize; i++)
	{
		for (int j = -kSize; j <= kSize; j++)
		{
			float cw = clamp(1.0 - sqrt(float(i * i + j * j)) / float(kSize), 0.0, 1.0);
			if (px + i<0 || px + i>width - 1 || py + j < 0 || py + j>height - 1)
				break;

			de += cw * texture(t1, uv + vec2(float(i), float(j)) / vec2(float(width), float(height)));
			wnorm += cw;
		}
	}
	de /= wnorm;

	return de;
}


//tonemapping algo based on Apophysis
vec4 Tonemap(vec4 fp)
{
	//xyz: accumulated color (log)
	//w: how many times this pixel was hit (log)

	float ls = Brightness * fp.w;
	fp = fp * Brightness;

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

void main(void)
{
	int px = int(gl_FragCoord.x);
	int py = int(gl_FragCoord.y);
	vec2 uv = vec2(gl_FragCoord.x / float(width), gl_FragCoord.y / float(height));

	vec4 logc;
	if (EnableDE)
	{
		//float de = DensityEstimation();
		//logc = vec4(logc.xyz / logc.w * de, de);
		logc = DensityEstimation();
	}
	else
		logc = texture(t1, uv);

	color = Tonemap(logc);
}