//ezt compile elott beirjuk:
//#version 450
//#extension GL_ARB_explicit_attrib_location : enable
//uniform int width=...;
//uniform int height=...;

layout(location = 0) out vec4 color;
//uniform sampler2D renderedTexture;
layout(std140, binding = 1) buffer histogrambuf
{
	vec4 histogram[];
};
uniform int framestep = 0;
uniform float Brightness = 1.0;
uniform float Gamma = 4.0;


vec3 RgbToHsv(vec3 rgb)
{
	float r = rgb.x;
	float g = rgb.y;
	float b = rgb.z;
	float h, s, v;

	float cmax, cmin, del, rc, gc, bc;
	cmax = max(max(r, g), b);//Compute maximum of r, g, b.
	cmin = min(min(r, g), b);//Compute minimum of r, g, b.
	del = cmax - cmin;
	v = cmax;
	s = (cmax != 0.0f) ? (del / cmax) : 0.0f;
	h = 0.0f;

	if (s != 0.0f)
	{
		rc = (cmax - r) / del;
		gc = (cmax - g) / del;
		bc = (cmax - b) / del;

		if (r == cmax)
			h = bc - gc;
		else if (g == cmax)
			h = 2.0f + rc - bc;
		else if (b == cmax)
			h = 4.0f + gc - rc;

		if (h < 0.0f)
			h += 6.0f;
	}

	return vec3(h, s, v);
}

vec3 HsvToRgb(vec3 hsv)
{
	float h = hsv.x;
	float s = hsv.y;
	float v = hsv.z;
	float r, g, b;

	int j;
	float f, p, q, t;

	while (h >= 6.0f)
		h -= 6.0f;

	while (h < 0.0f)
		h += 6.0f;

	j = int(floor(h));
	f = h - j;
	p = v * (1.0f - s);
	q = v * (1.0f - (s * f));
	t = v * (1.0f - (s * (1.0f - f)));

	switch (j)
	{
	case 0:  r = v;  g = t;  b = p;  break;

	case 1:  r = q;  g = v;  b = p;  break;

	case 2:  r = p;  g = v;  b = t;  break;

	case 3:  r = p;  g = q;  b = v;  break;

	case 4:  r = t;  g = p;  b = v;  break;

	case 5:  r = v;  g = p;  b = q;  break;

	default: r = v;  g = t;  b = p;  break;
	}

	return vec3(r, g, b);
}

//linrange: gammathresholdhoz
float alpha_magic_gamma(float density, float gamma, float linrange)
{
	float frac, alpha;
	float funcval = pow(linrange, gamma);

	if (density > 0.0f)
	{
		if (density < linrange)
		{
			frac = density / linrange;
			alpha = (1.0f - frac) * density * (funcval / linrange) + frac * pow(density, gamma);
		}
		else
			alpha = pow(density, gamma);
	}
	else
		alpha = 0.0f;

	return alpha;
}

vec3 CalcNewRgb(vec3 cBuf, float ls, float highPow)
{
	vec3 newRgb;

	int rgbi;
	float lsratio;
	vec3 newhsv;
	float maxa, maxc, newls;
	float adjustedHighlight;

	if (ls == 0 || (cBuf.x == 0 && cBuf.y == 0 && cBuf.z == 0))
	{
		newRgb.x = 0;
		newRgb.y = 0;
		newRgb.z = 0;
		return newRgb;
	}

	//Identify the most saturated channel.
	maxc = max(max(cBuf.x, cBuf.y), cBuf.z);
	maxa = ls * maxc;
	newls = 1.0f / maxc;

	//If a channel is saturated and highlight power is non-negative
	//modify the color to prevent hue shift.
	if (maxa > 1.0f && highPow >= 0.0f)
	{
		lsratio = pow(newls / ls, highPow);

		//Calculate the max-value color (ranged 0 - 1).
		newRgb = newls * cBuf;

		//Reduce saturation by the lsratio.
		newhsv = RgbToHsv(newRgb);
		newhsv.y *= lsratio;/*reduce saturation*/
		newRgb = HsvToRgb(newhsv);
	}
	else
	{
		adjustedHighlight = -highPow;

		if (adjustedHighlight > 1.0f)
			adjustedHighlight = 1.0f;

		if (maxa <= 1.0f)
			adjustedHighlight = 1.0f;

		//Calculate the max-value color (ranged 0 - 1) interpolated with the old behavior.
		newRgb = ((1.0f - adjustedHighlight) * newls + adjustedHighlight * ls) * cBuf;
	}

	return newRgb;
}

const float upScaleFactor = 1.0;//pr
const float scaleConstant = 1.0;//pr
const float vibrancy = 1.0;//pr
vec4 fractron9000_tonemap(vec4 rawPix)
{
	float invGamma = 1.0/Gamma;//TODO: pass invGamma

	float z, ka, lg10, gammaFactor;
	vec4 logPix;
	vec4 result;

	rawPix *= upScaleFactor;
  if(rawPix.a <= 0.5) //bail if alpha is too small to avoid dividing by zero
    return vec4(0.0, 0.0, 0.0, 0.0);

  logPix.a = Brightness * log(1.0 + rawPix.a*scaleConstant/framestep);
  ka = logPix.a / rawPix.a;
  
	logPix.r = ka * rawPix.r;
	logPix.g = ka * rawPix.g;
	logPix.b = ka * rawPix.b;
	z = pow(logPix.a, invGamma); //TODO: use invGamma
  gammaFactor = z / logPix.a;
  
	result.r = clamp(mix(pow(logPix.r,invGamma), gammaFactor*logPix.r, vibrancy), 0.0, 1.0);
	result.g = clamp(mix(pow(logPix.g,invGamma), gammaFactor*logPix.g, vibrancy), 0.0, 1.0);
	result.b = clamp(mix(pow(logPix.b,invGamma), gammaFactor*logPix.b, vibrancy), 0.0, 1.0);
	result.a = clamp(z, 0.0, 1.0);
  
	return result;
}

float log10(float x)
{
	return 0.30102999565 * log(x);
}

vec4 Apophysis(vec4 histogram)
{
	vec3 acc_c = histogram.xyz;//accumulated color
	float acc_h = histogram.w;//how many times this pixel was hit

	//todo
	float act_density = 1.0;//apo:0.001
	float g_thresh = 0.0;
	float vib = 1.1;
	vec3 bg = vec3(0.0,0.0,0.0);


	float ls = Brightness * log10(1.0 + acc_h / act_density) / acc_h;

	vec4 fp = histogram*ls;

	//gamma linearization
	float g_inv = 1.0 / Gamma;
	float funcval = 0.0;
	if(g_thresh != 0.0)
	{
		funcval = pow(g_thresh, g_inv - 1);
	}
	float alpha;
	if(fp.w < -1.0/*g_thresh*/)
	{
		float frac = fp.w / -1.0/*g_thresh*/;
		alpha = (1.0 - frac) * fp.w * funcval + frac * pow(fp.w, g_inv);
	}
	else
		alpha = pow(fp.w, g_inv);

	ls = vib * alpha / fp.w;
	alpha = clamp(alpha, 0.0, 1.0);

	vec3 o = ls * fp.rgb + (1.0-vib) * pow(fp.rgb, vec3(g_inv));
	o += (1.0-alpha) * bg;

	return vec4(o, alpha);

}


void main(void)
{
	int px = int(gl_FragCoord.x);
	int py = int(gl_FragCoord.y);
	vec2 uv = vec2(gl_FragCoord.x/float(width),gl_FragCoord.y/float(height));
	int pxi = px+py*width;

	vec3 acc_color = histogram[pxi].xyz;//accumulated color
	float acc_histogram = histogram[pxi].w;//how many times this pixel was hit

	//apo:
	if(gl_FragCoord.x > 950)
		color = Apophysis(histogram[pxi]);
	else
	{

	float logscale = Brightness * log(1.0f + acc_histogram / framestep);
	//vec3 logscaled_color = logscale * acc_color;
	
	float ls = /*vibrancy*/1.0f * alpha_magic_gamma(logscale, 1.0f / Gamma, 0.0/*g_thresh*/);
	ls = clamp(ls, 0.0f, 1.0f);
	vec3 logscaled_color = CalcNewRgb(acc_color, ls, /*high pow*/-1.0);
	logscaled_color = clamp(logscaled_color, 0.0f, 1.0f);
	
	color = vec4(logscaled_color, 1.0);//TODO: calc alpha channel, blend background
	}
}