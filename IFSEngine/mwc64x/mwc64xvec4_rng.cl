/*
Part of MWC64X by David Thomas, dt10@imperial.ac.uk
This is provided under BSD, full license is with the main package.
See http://www.doc.ic.ac.uk/~dt10/research
*/
#ifndef dt10_mwc64xvec4_rng_cl
#define dt10_mwc64xvec4_rng_cl

#include "skip_mwc.cl"

//! Represents the state of a particular generator
typedef struct{ uint4 x; uint4 c; } mwc64xvec4_state_t;

enum{ MWC64XVEC4_A = 4294883355U };
enum{ MWC64XVEC4_M = 18446383549859758079UL };

void MWC64XVEC4_Step(mwc64xvec4_state_t *s)
{
	uint4 X=s->x, C=s->c;
	
	uint4 Xn=MWC64XVEC4_A*X+C;
	// Note that vector comparisons return -1 for true, so we have to do this odd negation
	// I would hope that the compiler would do something sensible if possible...
	uint4 carry=as_uint4(-(Xn<C));		
	uint4 Cn=mad_hi((uint4)MWC64XVEC4_A,X,carry);
	
	s->x=Xn;
	s->c=Cn;
}

void MWC64XVEC4_Skip(mwc64xvec4_state_t *s, ulong distance)
{
	uint2 tmp0=MWC_SkipImpl_Mod64((uint2)(s->x.s0,s->c.s0), MWC64XVEC4_A, MWC64XVEC4_M, distance);
	uint2 tmp1=MWC_SkipImpl_Mod64((uint2)(s->x.s1,s->c.s1), MWC64XVEC4_A, MWC64XVEC4_M, distance);
	uint2 tmp2=MWC_SkipImpl_Mod64((uint2)(s->x.s2,s->c.s2), MWC64XVEC4_A, MWC64XVEC4_M, distance);
	uint2 tmp3=MWC_SkipImpl_Mod64((uint2)(s->x.s3,s->c.s3), MWC64XVEC4_A, MWC64XVEC4_M, distance);
	s->x=(uint4)(tmp0.x, tmp1.x, tmp2.x, tmp3.x);
	s->c=(uint4)(tmp0.y, tmp1.y, tmp2.y, tmp3.y);
}

void MWC64XVEC4_SeedStreams(mwc64xvec4_state_t *s, ulong baseOffset, ulong perStreamOffset)
{
	uint2 tmp0=MWC_SeedImpl_Mod64(MWC64XVEC4_A, MWC64XVEC4_M, 4, 0, baseOffset, perStreamOffset);
	uint2 tmp1=MWC_SeedImpl_Mod64(MWC64XVEC4_A, MWC64XVEC4_M, 4, 1, baseOffset, perStreamOffset);
	uint2 tmp2=MWC_SeedImpl_Mod64(MWC64XVEC4_A, MWC64XVEC4_M, 4, 2, baseOffset, perStreamOffset);
	uint2 tmp3=MWC_SeedImpl_Mod64(MWC64XVEC4_A, MWC64XVEC4_M, 4, 3, baseOffset, perStreamOffset);
	s->x=(uint4)(tmp0.x, tmp1.x, tmp2.x, tmp3.x);
	s->c=(uint4)(tmp0.y, tmp1.y, tmp2.y, tmp3.y);
}

//! Return a 32-bit integer in the range [0..2^32)
uint4 MWC64XVEC4_NextUint4(mwc64xvec4_state_t *s)
{
	uint4 res=s->x ^ s->c;
	MWC64XVEC4_Step(s);
	return res;
}

#endif
