/*
Part of MWC64X by David Thomas, dt10@imperial.ac.uk
This is provided under BSD, full license is with the main package.
See http://www.doc.ic.ac.uk/~dt10/research
*/
#ifndef dt10_mwc64xvec8_rng_cl
#define dt10_mwc64xvec8_rng_cl

#include "skip_mwc.cl"

//! Represents the state of a particular generator
typedef struct{ uint8 x; uint8 c; } mwc64xvec8_state_t;

enum{ MWC64XVEC8_A = 4294883355U };
enum{ MWC64XVEC8_M = 18446383549859758079UL };

void MWC64XVEC8_Step(mwc64xvec8_state_t *s)
{
	uint8 X=s->x, C=s->c;
	
	uint8 Xn=MWC64XVEC8_A*X+C;
	// Note that vector comparisons return -1 for true, so we have to do this odd negation
	// I would hope that the compiler would do something sensible if possible...
	uint8 carry=as_uint8(-(Xn<C));		
	uint8 Cn=mad_hi((uint8)MWC64XVEC8_A,X,carry);
	
	s->x=Xn;
	s->c=Cn;
}

void MWC64XVEC8_Skip(mwc64xvec8_state_t *s, ulong distance)
{
	uint2 tmp0=MWC_SkipImpl_Mod64((uint2)(s->x.s0,s->c.s0), MWC64XVEC8_A, MWC64XVEC8_M, distance);
	uint2 tmp1=MWC_SkipImpl_Mod64((uint2)(s->x.s1,s->c.s1), MWC64XVEC8_A, MWC64XVEC8_M, distance);
	uint2 tmp2=MWC_SkipImpl_Mod64((uint2)(s->x.s2,s->c.s2), MWC64XVEC8_A, MWC64XVEC8_M, distance);
	uint2 tmp3=MWC_SkipImpl_Mod64((uint2)(s->x.s3,s->c.s3), MWC64XVEC8_A, MWC64XVEC8_M, distance);
	uint2 tmp4=MWC_SkipImpl_Mod64((uint2)(s->x.s4,s->c.s4), MWC64XVEC8_A, MWC64XVEC8_M, distance);
	uint2 tmp5=MWC_SkipImpl_Mod64((uint2)(s->x.s5,s->c.s5), MWC64XVEC8_A, MWC64XVEC8_M, distance);
	uint2 tmp6=MWC_SkipImpl_Mod64((uint2)(s->x.s6,s->c.s6), MWC64XVEC8_A, MWC64XVEC8_M, distance);
	uint2 tmp7=MWC_SkipImpl_Mod64((uint2)(s->x.s7,s->c.s7), MWC64XVEC8_A, MWC64XVEC8_M, distance);
	s->x=(uint8)(tmp0.x, tmp1.x, tmp2.x, tmp3.x, tmp4.x, tmp5.x, tmp6.x, tmp7.x);
	s->c=(uint8)(tmp0.y, tmp1.y, tmp2.y, tmp3.y, tmp4.y, tmp5.y, tmp6.y, tmp7.y);
}

void MWC64XVEC8_SeedStreams(mwc64xvec8_state_t *s, ulong baseOffset, ulong perStreamOffset)
{
	uint2 tmp0=MWC_SeedImpl_Mod64(MWC64XVEC8_A, MWC64XVEC8_M, 8, 0, baseOffset, perStreamOffset);
	uint2 tmp1=MWC_SeedImpl_Mod64(MWC64XVEC8_A, MWC64XVEC8_M, 8, 1, baseOffset, perStreamOffset);
	uint2 tmp2=MWC_SeedImpl_Mod64(MWC64XVEC8_A, MWC64XVEC8_M, 8, 2, baseOffset, perStreamOffset);
	uint2 tmp3=MWC_SeedImpl_Mod64(MWC64XVEC8_A, MWC64XVEC8_M, 8, 3, baseOffset, perStreamOffset);
	uint2 tmp4=MWC_SeedImpl_Mod64(MWC64XVEC8_A, MWC64XVEC8_M, 8, 4, baseOffset, perStreamOffset);
	uint2 tmp5=MWC_SeedImpl_Mod64(MWC64XVEC8_A, MWC64XVEC8_M, 8, 5, baseOffset, perStreamOffset);
	uint2 tmp6=MWC_SeedImpl_Mod64(MWC64XVEC8_A, MWC64XVEC8_M, 8, 6, baseOffset, perStreamOffset);
	uint2 tmp7=MWC_SeedImpl_Mod64(MWC64XVEC8_A, MWC64XVEC8_M, 8, 7, baseOffset, perStreamOffset);
	s->x=(uint8)(tmp0.x, tmp1.x, tmp2.x, tmp3.x, tmp4.x, tmp5.x, tmp6.x, tmp7.x);
	s->c=(uint8)(tmp0.y, tmp1.y, tmp2.y, tmp3.y, tmp4.y, tmp5.y, tmp6.y, tmp7.y);
}

//! Return a 32-bit integer in the range [0..2^32)
uint8 MWC64XVEC8_NextUint8(mwc64xvec8_state_t *s)
{
	uint8 res=s->x ^ s->c;
	MWC64XVEC8_Step(s);
	return res;
}

#endif
