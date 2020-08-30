#version 430

layout(location = 0) out vec4 color;

uniform sampler2D histogram_tex;//in logarithmic scale
uniform int width = 1920;
uniform int height = 1080;
uniform float de_max_radius = 9.0;
uniform float de_power = 0.2;
uniform float de_threshold = 0.4;
uniform uint max_density = 1;

//similar to flame, but uses cone filter
//ref: https://github.com/scottdraves/flam3/wiki/Density-Estimation
vec4 density_estimation()
{
	int px = int(gl_FragCoord.x);
	int py = int(gl_FragCoord.y);
	vec2 uv = vec2(gl_FragCoord.x / float(width), gl_FragCoord.y / float(height));


	float w = de_max_radius / de_threshold * clamp(de_threshold - texture(histogram_tex, uv).w, 0.0, 1.0);

	//experiment:
	//float w = de_max_radius / de_threshold * clamp(de_threshold - texture(histogram_tex, uv).w, 0.0, 1.0);
	//float w = de_max_radius * clamp(1.0 / pow(texture(histogram_tex, uv).w*max_density, de_power), 0.0, 1.0);
	//experiment:
	//float d = texture(histogram_tex, uv).w;//actual density
	//if (d > de_threshold)
	//	return texture(histogram_tex, uv);//do not estimate dense enough areas
	//float mult = de_max_radius / pow(de_threshold, 1.0 / de_power);//TODO: calc on cpu
	//float w = mult * pow(de_threshold - d, 1.0 / de_power);

	w = clamp(w, 0.0, de_max_radius);
	const int kSize = int(w);
	if (kSize == 0)
		discard;//spare a histogram_tex read/write 

	vec4 de = vec4(0.0);
	float wnorm = 0.0;
	for (int i = -kSize; i <= kSize; i++)
	{
		if (px + i < 0 || px + i > width - 1)
			continue;

		for (int j = -kSize; j <= kSize; j++)
		{
			if (py + j < 0 || py + j > height - 1)
				continue;

			float cw = clamp(1.0 - sqrt(float(i * i + j * j)) / float(kSize), 0.0, 1.0);
			de += cw * texture(histogram_tex, uv + vec2(float(i), float(j)) / vec2(float(width), float(height)));
			wnorm += cw;
		}
	}
	de /= wnorm;

	return de;
}

void main(void)
{
	//float de = density_estimation();
	//de_color = vec4(c.xyz / c.w * de, de);
	color = density_estimation();
}