#define it_num (3)

//max_iters
//width
//height

//#pragma OPENCL EXTENSION cl_khr_fp64 : enable

#pragma OPENCL EXTENSION CL_APPLE_gl_sharing : enable
#pragma OPENCL EXTENSION CL_KHR_gl_sharing : enable

typedef struct
{
	float2 o;
	float2 x;
	float2 y;
} Affine;

typedef struct
{
	Affine aff;
	int tfID;
	float w;
	float cs;
} Iterator;
/*
__constant Iterator its[it_num] = {
{
	{{0.0,1.0} , {0.5,0.0} , {0.0,0.5}},
	0,
	0.33,
	0.5
},{
	{{1.0,1.0} , {0.5,0.0} , {0.0,0.5}},
	0,
	0.33,
	0.5
},{
	{{0.5,0.133975} , {0.5,0.0} , {0.0,0.5}},
	0,
	0.33,
	0.5
}

};
*/
__constant Iterator its[it_num] = {
{
	{{0.0,-0.5} , {-0.4,0.33} , {0.33,-0.8}},
	0,
	0.25,
	0.5
},{
	{{0.2,-1.0} , {0.8,0.5} , {0.1,0.8}},
	0,
	0.5,
	0.5
},{
	{{0.6,0.133975} , {0.56,0.0} , {0.0,0.4}},
	0,
	0.25,
	0.5
}

};

__constant Iterator finalit = {
	{{0.0,0.0} , {0.5,0.0} , {0.0,0.5}},
	0,
	1.0,
	0.5
};

float2 affine(Affine aff, float2 input){
 float px = aff.x.x * input.x + aff.x.y * input.y + aff.o.x;
 float py = aff.y.x * input.y + aff.y.y * input.y + aff.o.y;
 return (float2)(px, py);
}

float4 Apply(Iterator it, float4 input)
{
	float2 p = affine(it.aff, input.xy);
	//transform here: TODO
	float c = (input.z + it.cs) / 2.0f;//color
	float o = input.w;//opacity
	return (float4)(p, c, o);
}

float4 getPaletteColor(float lerp)
{
	return (float4)(1.0f, 1.0f, 1.0f, 1.0f);
}

__kernel void Main(__global float* output, __global float* rnd)
{
	int gid = get_global_id(0);
	int next = gid*(max_iters+2);//for rnd

	float startx = rnd[next++]*2.0f-1.0f;
	float starty = rnd[next++]*2.0f-1.0f;
	float4 p = (float4)(startx, starty, 0.1f,1.0f);//x,y,c,o

	for (int i = 0; i < max_iters; i++)
	{//pick a random weighted Transform index
		int r_index = 0;
		float r = rnd[next++];
		float w_acc = 0.0f;
		for (int j = 0; j < it_num; j++)
			if (w_acc < r) {
				w_acc += its[j].w;
				r_index = j;
			}
			else
				break;

		p = Apply(its[r_index], p);//transform here

		float4 finalp = Apply(finalit, p);

		finalp.xy = (float2)(0.5f,0.5f) + finalp.xy * (0.5f);//to window center

		//printf("f2 = %2.2v2hlf\n", index);
		int x_index = (int)(round(finalp.x*width));
		int y_index = (int)(round(finalp.y*width));

		float4 color = (float4)(1.0f,1.0f,1.0f,1.0f);//TODO: calc by Palette(p.z)

		if (x_index >= 0 && x_index < width && y_index >= 0 && y_index < height && i>16)
		{//point lands on picture
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x;//r
			output[ipx+1] += color.y;//g
			output[ipx+2] += color.z;//b
			output[ipx+3] += 1.0f;//a
			//accepted_iters++;
		}
		else
			continue;
		
		//aa
		float dx = finalp.x*width - floor(finalp.x*width);//finalp.x*width % 1;
		float dy = finalp.y*height - floor(finalp.y*height);//finalp.y*height % 1;
		float avg = 1.0/8.0;
		
		x_index=floor(finalp.x*width);
		y_index=floor(finalp.y*height);
		if(x_index>=0 && x_index<width && y_index>=0 && y_index<height)
		{//point lands on picture
			double dd = 2.0 - dx-dy;
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x * dd * avg;
			output[ipx+1] += color.y * dd * avg;
			output[ipx+2] += color.z * dd * avg;
			output[ipx+3] += 1.0;

		};
      
		x_index=ceil(finalp.x*width);
		y_index=ceil(finalp.y*height);
		if(x_index>=0 && x_index<width && y_index>=0 && y_index<height)
		{//point lands on picture
			double dd = dx+dy;
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x * dd * avg;
			output[ipx+1] += color.y * dd * avg;
			output[ipx+2] += color.z * dd * avg;
			output[ipx+3] += 1.0;
		};
      
		x_index=floor(finalp.x*width);
		y_index=ceil(finalp.y*height);
		if(x_index>=0 && x_index<width && y_index>=0 && y_index<height)
		{//point lands on picture
			double dd = (1.0-dx)+dy;
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x * dd * avg;
			output[ipx+1] += color.y * dd * avg;
			output[ipx+2] += color.z * dd * avg;
			output[ipx+3] += 1.0;
		};
      
		x_index=ceil(finalp.x*width);
		y_index=floor(finalp.y*height);
		if(x_index>=0 && x_index<width && y_index>=0 && y_index<height)
		{//point lands on picture
			double dd = dx+(1.0-dy);
			int ipx = x_index*4 + y_index*4 * width;//pixel index
			output[ipx+0] += color.x * dd * avg;
			output[ipx+1] += color.y * dd * avg;
			output[ipx+2] += color.z * dd * avg;
			output[ipx+3] += 1.0;
		};
		


	}

}

__kernel void Display(__global float* calc, __global float* disp, __write_only image2d_t img, __global float* settings)
{
	int gid = get_global_id(0);

	//double steps = settings[0];//int
	//double brightness = settings[1];
	//double gamma = settings[2];

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