//width
//height

//#pragma OPENCL EXTENSION cl_khr_fp64 : enable

#pragma OPENCL EXTENSION CL_APPLE_gl_sharing : enable
#pragma OPENCL EXTENSION CL_KHR_gl_sharing : enable

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
	int max_iters;//do this many iterations
	Camera camera;
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
	float w;
	float cs;
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

//TODO: color es opacity megint
float3 Apply(Iterator it, float3 input/*, float2 shader (color,opacity)*/)
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

	//float c = (input.z + it.cs) / 2.0f;//color//ez hibas a paperben
	//float c = it.cs * /*it.color_index*/0.5 + (1.0f - it.cs) * shader.x;//TODO: add color_index to Iterators
	//float o = shader.y;//opacity
	return p;//(float4)(p, c, o);
}

float4 getPaletteColor(float lerp)
{
	return (float4)(1.0f, 1.0f, 1.0f, 1.0f);
}

__kernel void Main(
	__global float* output,
	__global float* rnd,
	__global Iterator* its,
	__global Settings* settings
)
{
	int gid = get_global_id(0);

	const int max_iters = settings[0].max_iters;

	int next = gid*(max_iters+3);//for rnd

	float startx = rnd[next++]*2.0f-1.0f;
	float starty = rnd[next++]*2.0f-1.0f;
	float startz = rnd[next++]*2.0f-1.0f;
	float3 p = (float3)(startx, starty, startz);//x,y,c,o
	//TODO: p_shader (color01, opacity01)

	for (int i = 0; i < max_iters; i++)
	{//pick a random weighted Transform index
		int r_index = 0;
		float r = rnd[next++];
		float w_acc = 0.0f;
		for (int j = 0; j < settings[0].itnum; j++)
			if (w_acc < r) {
				w_acc += its[j].w;
				r_index = j;
			}
			else
				break;

		p = Apply(its[r_index], p);//transform here
		//TODO: p_shader=

		float3 finalp = Apply(/*finalit*/its[settings[0].itnum], p);
		//TODO: finalp_shader=

		//printf("f2 = %2.2v2hlf\n", index);
		
		//perspective project
		float2 proj = Project(settings[0].camera, finalp, rnd[i%max_iters],rnd[(i+422)%max_iters]);
		//window center
		proj.x = width/2.0f + proj.x * width/2.0f;
		proj.y = width/2.0f + proj.y * width/2.0f;
		
		int x_index = round(proj.x);
		int y_index = round(proj.y);


		float4 color = (float4)(1.0f,1.0f,1.0f,1.0f);//TODO: calc by Palette(p_shader.color)

		//depth fog pr
		//if(x_index<width/2)
			color.w /= 1.0f + pow(max(0.0f,length(finalp-(float3)(settings[0].camera.ox,settings[0].camera.oy,settings[0].camera.oz))-settings[0].camera.focusdistance-settings[0].camera.focallength),8)*0.5f;

		//opacityvel szoroz
		color.xyz*=color.w;
		
		//ha kamera mogott van, akkor az egyik sarok utan van, kilog
		if (x_index >= 0 && x_index < width && y_index >= 0 && y_index < height && i>16)
		{//point lands on picture
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x;//r
			output[ipx+1] += color.y;//g
			output[ipx+2] += color.z;//b
			output[ipx+3] += color.w;//a
			//accepted_iters++;
		}
		else
			continue;
		
		//aa
		float dx = proj.x - floor(proj.x);//proj.x % 1;
		float dy = proj.y - floor(proj.y);//proj.y % 1;
		float avg = 1.0/8.0;
		
		x_index=floor(proj.x);
		y_index=floor(proj.y);
		if(x_index>=0 && x_index<width && y_index>=0 && y_index<height)
		{//point lands on picture
			float dd = 2.0f - dx-dy;
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x * dd * avg;
			output[ipx+1] += color.y * dd * avg;
			output[ipx+2] += color.z * dd * avg;
			output[ipx+3] += color.w * dd * avg;

		};
      
		x_index=ceil(proj.x);
		y_index=ceil(proj.y);
		if(x_index>=0 && x_index<width && y_index>=0 && y_index<height)
		{//point lands on picture
			float dd = dx+dy;
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x * dd * avg;
			output[ipx+1] += color.y * dd * avg;
			output[ipx+2] += color.z * dd * avg;
			output[ipx+3] += color.w * dd * avg;
		};
      
		x_index=floor(proj.x);
		y_index=ceil(proj.y);
		if(x_index>=0 && x_index<width && y_index>=0 && y_index<height)
		{//point lands on picture
			float dd = (1.0f-dx)+dy;
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x * dd * avg;
			output[ipx+1] += color.y * dd * avg;
			output[ipx+2] += color.z * dd * avg;
			output[ipx+3] += color.w * dd * avg;
		};
      
		x_index=ceil(proj.x);
		y_index=floor(proj.y);
		if(x_index>=0 && x_index<width && y_index>=0 && y_index<height)
		{//point lands on picture
			float dd = dx+(1.0f-dy);
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x * dd * avg;
			output[ipx+1] += color.y * dd * avg;
			output[ipx+2] += color.z * dd * avg;
			output[ipx+3] += color.w * dd * avg;
		};
		


	}

}

__kernel void Display(__global float* calc, __global float* disp, __write_only image2d_t img, __global float* settings)
{
	int gid = get_global_id(0);

	//float steps = settings[0];//int
	//float brightness = settings[1];
	//float gamma = settings[2];

	float4 acc = (float4)(calc[gid*4+0], calc[gid*4+1], calc[gid*4+2], calc[gid*4+3]);

	float4 linear = acc/settings[0];
	float4 logscaled = settings[1]*exp(log(linear)/settings[2]);
	logscaled.w = 1.0f;//debug

	int2 coords = (int2)(gid%width, gid / width);

	//format: //image2d_t, int2, float4
	write_imagef(img, coords, logscaled);
	//write_imagef(img, coords, linear);//ez nem jo mert nem fer bele a gl 0-1 rangejebe

	disp[gid*4+0] = logscaled.x;
	disp[gid*4+1] = logscaled.y;
	disp[gid*4+2] = logscaled.z;
	disp[gid*4+3] = 1.0;
}