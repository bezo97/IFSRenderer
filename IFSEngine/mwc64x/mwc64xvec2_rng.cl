/*
Part of MWC64X by David Thomas, dt10@imperial.ac.uk
This is provided under BSD, full license is with the main package.
See http://www.doc.ic.ac.uk/~dt10/research
*/
#ifndef dt10_mwc64xvec2_rng_cl
#define dt10_mwc64xvec2_rng_cl

#include "skip_mwc.cl"

//! Represents the state of a particular generator
typedef struct{ uint2 x; uint2 c; } mwc64xvec2_state_t;

enum{ MWC64XVEC2_A = 4294883355U };
enum{ MWC64XVEC2_M = 18446383549859758079UL };

void MWC64XVEC2_Step(mwc64xvec2_state_t *s)
{
	uint2 X=s->x, C=s->c;
	
	uint2 Xn=MWC64XVEC2_A*X+C;
	// Note that vector comparisons return -1 for true, so we have to do this negation
	// I would hope that the compiler would do something sensible if possible...
	uint2 carry=as_uint2(-(Xn<C));		
	uint2 Cn=mad_hi((uint2)MWC64XVEC2_A,X,carry);
	
	s->x=Xn;
	s->c=Cn;
}

void MWC64XVEC2_Skip(mwc64xvec2_state_t *s, ulong distance)
{
	uint2 tmp0=MWC_SkipImpl_Mod64((uint2)(s->x.s0,s->c.s0), MWC64XVEC2_A, MWC64XVEC2_M, distance);
	uint2 tmp1=MWC_SkipImpl_Mod64((uint2)(s->x.s1,s->c.s1), MWC64XVEC2_A, MWC64XVEC2_M, distance);
	s->x=(uint2)(tmp0.x, tmp1.x);
	s->c=(uint2)(tmp0.y, tmp1.y);
}

void MWC64XVEC2_SeedStreams(mwc64xvec2_state_t *s, ulong baseOffset, ulong perStreamOffset)
{
	uint2 tmp0=MWC_SeedImpl_Mod64(MWC64XVEC2_A, MWC64XVEC2_M, 2, 0, baseOffset, perStreamOffset);
	uint2 tmp1=MWC_SeedImpl_Mod64(MWC64XVEC2_A, MWC64XVEC2_M, 2, 1, baseOffset, perStreamOffset);
	s->x=(uint2)(tmp0.x, tmp1.x);
	s->c=(uint2)(tmp0.y, tmp1.y);
}

//! Return a 32-bit integer in the range [0..2^32)
uint2 MWC64XVEC2_NextUint2(mwc64xvec2_state_t *s)
{
	uint2 res=s->x ^ s->c;
	MWC64XVEC2_Step(s);
	return res;
}

#endif
