//width
//height

//#pragma OPENCL EXTENSION cl_khr_fp64 : enable

#pragma OPENCL EXTENSION CL_APPLE_gl_sharing : enable
#pragma OPENCL EXTENSION CL_KHR_gl_sharing : enable

#include "mwc64x.cl"

//0-1
float randhash(mwc64x_state_t *state)
{
	//lol hack:
	uint maxint = 0;
	maxint--;
	uint r = MWC64X_NextUint(state);
	return ((float) r) / ((float)maxint);
}

typedef struct
{
    float ox;//pos
    float oy;
    float oz;

    float dx;//dir
    float dy;
    float dz;

	//dir
    float theta;//0-pi
    float phi;//0-2pi
	//helpers
	float sin_theta;
	float cos_theta;
	float sin_phi;
	float cos_phi;
    float sin_phi_hack;
    float cos_phi_hack;

    float focallength;//for perspective

	float focusdistance;//for dof
	float dof_effect;
} Camera;

typedef struct
{
	int itnum;//length of its - 1 (last one is finalit)
	int pass_iters;//do this many iterations
	int rendersteps;
	Camera camera;
	int enable_depthfog;
} Settings;

typedef struct
{
	//Origin
	float ox;
	float oy;
	float oz;
	//X axis
	float xx;
	float xy;
	float xz;
	//Y axis
	float yx;
	float yy;
	float yz;
	//Z axis
	float zx;
	float zy;
	float zz;

} Affine;

typedef struct
{
	Affine aff;
	int tfID;
	float w;//weight
	float cs;//color speed, -1 - 1
	float ci;//color index, 0 - 1
	float op;//opacity
} Iterator;

float2 Project(Camera c, float3 p, float ra, float rl)
{
	int r1 = 0.0f;
	int r2 = 0.0f;
	int r3 = 0.0f;

	float3 r;//ray from camera to point
	r.x = (p.x-c.ox);
	r.y = (p.y-c.oy);
	r.z = (p.z-c.oz);

	//float hack=c.phi;//miert vetitunk a kamera jobb oldala iranyaba???
	//c.phi-=3.1415926f/2.0f;

	float subject_distance = - r.x*c.sin_phi_hack*c.sin_theta + r.y*c.cos_phi_hack*c.sin_theta - r.z*c.cos_theta + r3 -/*ez miert nem +*/ c.focallength;
	if(subject_distance<0.0f)//discard if behind camera
		return (float2)(-2,-2);

	float model_x = r.x*c.cos_phi_hack + r.y*c.sin_phi_hack + r1;
	float model_y = - r.x*c.sin_phi_hack*c.cos_theta + r.y*c.cos_phi_hack*c.cos_theta + r.z*c.sin_theta - r2;

	//c.phi=hack;

	//dof pr
	float3 co = (float3)(c.ox, c.oy, c.oz);
	float3 cd = (float3)(c.dx, c.dy, c.dz);
	float3 rd = normalize(p-co);
	float3 focuspoint = co + (rd * c.focusdistance);// / dot(rd, cd));
	float3 fpn = -cd;//focal plane normal
	float D = dot(fpn,focuspoint);
	float dd = fabs(dot(fpn,p)+D);
	float dof = c.dof_effect*dd;
	model_x+=pow(rl,0.5f)*dof*cos(ra*6.28318530718f);
	model_y+=pow(rl,0.5f)*dof*sin(ra*6.28318530718f);

	float2 o;
	o.x = model_x * c.focallength / subject_distance;//project
	o.y = model_y * c.focallength / subject_distance;
	return o;

}

float3 affine(Affine aff, float3 input){
 float px = aff.xx * input.x + aff.xy * input.y + aff.xz * input.z + aff.ox;
 float py = aff.yx * input.x + aff.yy * input.y + aff.yz * input.z + aff.oy;
 float pz = aff.zx * input.x + aff.zy * input.y + aff.zz * input.z + aff.oz;
 return (float3)(px, py, pz);
}

float3 Apply(Iterator it, float3 input)
{
	float3 p = affine(it.aff, input.xyz);
	//transform here: TODO
	if(it.tfID==0)
	{
		//linear
	}
	else if(it.tfID==1)
	{//spherical
		float r = length(p);
		p = p/(r*r);
	}

	return p;
}

float2 ApplyShader(Iterator it, float2 input/*(color,opacity)*/)
{
	//float c = (input.x + it.ci) / it.cs;//color//ez hibas a paperben
	float c = it.cs * it.ci + (1.0f - it.cs) * input.x;
	float o = it.op;//opacity
	return (float2)(c-floor(c),o);//(color,opacity)
}

float3 getPaletteColor(float pos)
{
  //define two constant colors, like red and blue
  float3 red   = (float3)(1.0f, 0.0f, 0.0f);
  float3 green = (float3)(0.0f, 1.0f, 0.0f);
  float3 blue  = (float3)(0.0f, 0.0f, 1.0f);
  //interpolate linearly between the two, according to pos
  float3 result; //= (1.0f-pos) * red + pos * blue;//mix(red, blue, pos);

  if (pos <= 0.5f)
  {
	  result = (green * pos * 2.0f) + red * (0.5f - pos) * 2.0f;
  }
  else
  {
	  result = blue * (pos - 0.5f) * 2.0f + green * (1.0f - pos) * 2.0f;
  }

  //return that color
  return result;
}

__kernel void Main(
	__global float* output,
	__global Iterator* its,
	__global Settings* settings,
	__global float* pointsstate,
	__global mwc64x_state_t* rng_state
)
{
	int gid = get_global_id(0);

	const int pass_iters = settings[0].pass_iters;
	const int rendersteps = settings[0].rendersteps;

	//rnd index: cpu rol jott randomok
	//int next = (gid*(pass_iters+3) + /*hash()*/rendersteps) % /*randbuf meret*/(1500*(10000+2));

	//init mwc64x random hash
	mwc64x_state_t rng = rng_state[gid];
	if(rendersteps==0)
	{
		ulong perStream=3*10000*100;//ez miert tobb?
		MWC64X_SeedStreams(&rng, gid, perStream); 
	}
	

	/*float startx = rnd[next++]*2.0f-1.0f;
	float starty = rnd[next++]*2.0f-1.0f;
	float startz = rnd[next++]*2.0f-1.0f;
	float3 p = (float3)(startx, starty, startz);//x,y,z
	float2 p_shader = (float2)(0.0f, 1.0f);//c,o*/
	float3 p = (float3)(
		pointsstate[gid*4+0],
		pointsstate[gid*4+1],
		pointsstate[gid*4+2]
	);
	float2 p_shader = (float2)(
		pointsstate[gid*4+3],
		1.0f
	);

	for (int i = 0; i < pass_iters; i++)
	{//pick a random weighted Transform index
		int r_index = 0;
		float r = randhash(&rng);//rnd[next++];
		float w_acc = 0.0f;
		for (int j = 0; j < settings[0].itnum; j++)
			if (w_acc < r) {
				w_acc += its[j].w;
				r_index = j;
			}
			else
				break;

		//?? ha elso iter, akkor allitsuk be a shadert az elsore??
		if(i==0)
			p_shader.x = its[r_index].ci;

		//ha elozo iteracioban tul messze ment, akkor reset
		/*if(p.x==INFINITY||p.y==INFINITY||p.z==INFINITY||p.x==-INFINITY||p.y==-INFINITY||p.z==-INFINITY || p.x==0||p.y==0||p.z==0)
		{
			p.x = rnd[(53*next++)%(gid*(pass_iters+3))]*2.0f-1.0f;
			p.y = rnd[(67*next++)%(gid*(pass_iters+3))]*2.0f-1.0f;
			p.z = rnd[(71*next++)%(gid*(pass_iters+3))]*2.0f-1.0f;
		}*/

		p = Apply(its[r_index], p);//transform here
		p_shader = ApplyShader(its[r_index], p_shader);/*color,opacity*/

		float3 finalp = Apply(/*finalit*/its[settings[0].itnum], p);
		float2 finalp_shader = ApplyShader(/*finalit*/its[settings[0].itnum], p_shader);/*color,opacity*/
		finalp_shader.y=p_shader.y;//opacity=1 a finalon

		//printf("f2 = %2.2v2hlf\n", index);
		
		//perspective project
		float ra1 = randhash(&rng);
		float ra2 = randhash(&rng);
		float2 proj = Project(settings[0].camera, finalp, /*rnd[i%pass_iters]*/ra1,/*rnd[(i+422)%pass_iters]*/ra2);
		//window center
		proj.x = width/2.0f + proj.x * width/2.0f;
		proj.y = height/2.0f + proj.y * height/2.0f;
		
		int x_index = round(proj.x);
		int y_index = round(proj.y);


		float4 color = (float4)(getPaletteColor(finalp_shader.x), finalp_shader.y);//TODO: calc by Palette(p_shader.color)

		//depth fog option
		float depthfog = 1.0f;
		if(settings[0].enable_depthfog)
		{
			color.xyz*=color.w;//?
			depthfog = 1.0f / (1.0f + pow(max(0.0f,length(finalp-(float3)(settings[0].camera.ox,settings[0].camera.oy,settings[0].camera.oz))-settings[0].camera.focusdistance-settings[0].camera.focallength),2));
		}
		color.xyzw*=depthfog;
		
		//ha kamera mogott van, akkor az egyik sarok utan van, kilog
		if (x_index >= 0 && x_index < width && y_index >= 0 && y_index < height && i>16)
		{//point lands on picture
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x;//r
			output[ipx+1] += color.y;//g
			output[ipx+2] += color.z;//b
			output[ipx+3] += color.w;//hany db, histogramhoz
			//accepted_iters++;
		}
		else
			continue;

		continue;//no aa
		
		//aa
		float dx = proj.x - floor(proj.x);//proj.x % 1;
		float dy = proj.y - floor(proj.y);//proj.y % 1;

		float avg = 1.0/8.0;
		color *= avg;
		
		x_index=floor(proj.x);
		y_index=floor(proj.y);
		if(x_index>=0 && x_index<width && y_index>=0 && y_index<height)
		{//point lands on picture
			float dd = 2.0f - dx-dy;
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x * dd;
			output[ipx+1] += color.y * dd;
			output[ipx+2] += color.z * dd;
			output[ipx+3] += color.w * dd;//hany db, histogramhoz

		};
      
		x_index=ceil(proj.x);
		y_index=ceil(proj.y);
		if(x_index>=0 && x_index<width && y_index>=0 && y_index<height)
		{//point lands on picture
			float dd = dx+dy;
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x * dd;
			output[ipx+1] += color.y * dd;
			output[ipx+2] += color.z * dd;
			output[ipx+3] += color.w * dd;//hany db, histogramhoz
		};
      
		x_index=floor(proj.x);
		y_index=ceil(proj.y);
		if(x_index>=0 && x_index<width && y_index>=0 && y_index<height)
		{//point lands on picture
			float dd = (1.0f-dx)+dy;
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x * dd;
			output[ipx+1] += color.y * dd;
			output[ipx+2] += color.z * dd;
			output[ipx+3] += color.w * dd;//hany db, histogramhoz
		};
      
		x_index=ceil(proj.x);
		y_index=floor(proj.y);
		if(x_index>=0 && x_index<width && y_index>=0 && y_index<height)
		{//point lands on picture
			float dd = dx+(1.0f-dy);
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x * dd;
			output[ipx+1] += color.y * dd;
			output[ipx+2] += color.z * dd;
			output[ipx+3] += color.w * dd;//hany db, histogramhoz
		};
		
	}

	//save point state for next pass
	pointsstate[gid*4+0] = p.x;
	pointsstate[gid*4+1] = p.y;
	pointsstate[gid*4+2] = p.z;
	pointsstate[gid*4+3] = p_shader.x;//color

	rng_state[gid] = rng;

}

float3 RgbToHsv(float3 rgb)
{
	float r = rgb.x;
	float g = rgb.y;
	float b = rgb.z;
	float h,s,v;

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

	return (float3)(h,s,v);
}

float3 HsvToRgb(float3 hsv)
{
	float h = hsv.x;
	float s = hsv.y;
	float v = hsv.z;
	float r,g,b;

	int j;
	float f, p, q, t;

	while (h >= 6.0f)
		h -= 6.0f;

	while (h <  0.0f)
		h += 6.0f;

	j = floor(h);
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

	return (float3)(r,g,b);
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

float3 CalcNewRgb(float3 cBuf, float ls, float highPow)
{
	float3 newRgb;

	int rgbi;
	float lsratio;
	float3 newhsv;
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

__kernel void Display(__global float* calc, __global float* disp, __write_only image2d_t img, __global float* settings)
{
	int gid = get_global_id(0);

	//float  = settings[0];//int //quality
	//float brightness = settings[1];
	//float gamma = settings[2];

	float3 acc_color = (float3)(calc[gid*4+0], calc[gid*4+1], calc[gid*4+2]);//accumulated color

	float acc_histogram = calc[gid*4+3];//how many times this pixel was hit

	float logscale = (settings[1] * log(1.0f + acc_histogram / (settings[0]/settings[2]))) / acc_histogram;
	//float logscale = settings[1] * exp(log(acc_histogram/settings[0]) / settings[2]); //ez vmi mas
	float3 logscaled_color = logscale * acc_color;
	//float3 linearscaled_color = acc_color / settings[0];

	float ls = /*vibrancy*/1.0f * alpha_magic_gamma(acc_histogram / settings[0], 1.0f/settings[2], 0.0f/*g_thresh*/);// / (acc_histogram / settings[0]);
	ls = clamp(ls, 0.0f, 1.0f);
	logscaled_color = CalcNewRgb(logscaled_color, ls, /*high pow*/2.0f);
	logscaled_color = clamp(logscaled_color, 0.0f, 1.0f);

	int2 coords = (int2)(gid%width, gid / width);
	//format: //image2d_t, int2, float4
	write_imagef(img, coords, (float4)(logscaled_color,1.0f));
	//write_imagef(img, coords, linear);//ez nem jo mert nem fer bele a gl 0-1 rangejebe

	disp[gid*4+0] = logscaled_color.x;
	disp[gid*4+1] = logscaled_color.y;
	disp[gid*4+2] = logscaled_color.z;
	disp[gid*4+3] = 1.0f;
}