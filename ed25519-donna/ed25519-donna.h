/*
	Public domain by Andrew M. <liquidsun@gmail.com>
	Modified from the amd64-51-30k implementation by
		Daniel J. Bernstein
		Niels Duif
		Tanja Lange
		Peter Schwabe
		Bo-Yin Yang
*/

#include <assert.h>
#include "ed25519-donna-portable.h"

#if defined(ED25519_SSE2)
#else
	#if defined(HAVE_UINT128) && !defined(ED25519_FORCE_32BIT)
		#define ED25519_64BIT
	#else
		#define ED25519_32BIT
	#endif
#endif

#if !defined(ED25519_NO_INLINE_ASM)
	/* detect extra features first so un-needed functions can be disabled throughout */
	#if defined(ED25519_SSE2)
		#if defined(COMPILER_GCC) && defined(CPU_X86)
			#define ED25519_GCC_32BIT_SSE_CHOOSE
		#elif defined(COMPILER_GCC) && defined(CPU_X86_64)
			#define ED25519_GCC_64BIT_SSE_CHOOSE
		#endif
	#else
		#if defined(CPU_X86_64)
			#if defined(COMPILER_GCC) 
				#if defined(ED25519_64BIT)
					#define ED25519_GCC_64BIT_X86_CHOOSE
				#else
					#define ED25519_GCC_64BIT_32BIT_CHOOSE
				#endif
			#endif
		#endif
	#endif
#endif

#if defined(ED25519_SSE2)
	#include "curve25519-donna-sse2.h"
#elif defined(ED25519_64BIT)
	#include "curve25519-donna-64bit.h"
#else
	#include "curve25519-donna-32bit.h"
#endif

#include "curve25519-donna-helpers.h"

/* separate uint128 check for 64 bit sse2 */
#if defined(HAVE_UINT128) && !defined(ED25519_FORCE_32BIT)
	#include "modm-donna-64bit.h"
#else
	#include "modm-donna-32bit.h"
#endif
#include "modm-invert.h"

typedef unsigned char hash_512bits[64];

/*
	Timing safe memory compare
*/
static inline int ed25519_verify(const long* a, const long* b, size_t len) {
  long diff=0;
  assert(len == 32);
  switch(sizeof(long)) {
    case 4:
      diff|=((a[4]^b[4])|(a[5]^b[5])|(a[6]^b[6])|(a[7]^b[7]));
    case 8:
      diff|=((a[0]^b[0])|(a[1]^b[1])|(a[2]^b[2])|(a[3]^b[3]));
  }
  return -(diff==0);
}

/*
 * Arithmetic on the twisted Edwards curve -x^2 + y^2 = 1 + dx^2y^2
 * with d = -(121665/121666) = 37095705934669439343138083508754565189542113879843219016388785533085940283555
 * Base point: (15112221349535400772501151409588531511454012693041857206046113283949847762202,46316835694926478169428394003475163141307993866256225615783033603165251855960);
 */

typedef struct ge25519_t {
	bignum25519 x, y, z, t;
} ge25519;

typedef struct ge25519_p1p1_t {
	bignum25519 x, y, z, t;
} ge25519_p1p1;

typedef struct ge25519_niels_t {
	bignum25519 ysubx, xaddy, t2d;
} ge25519_niels;

typedef struct ge25519_pniels_t {
	bignum25519 ysubx, xaddy, z, t2d;
} ge25519_pniels;

#include "ed25519-donna-basepoint-table.h"

#if defined(ED25519_64BIT)
	#include "ed25519-donna-64bit-tables.h"
	#include "ed25519-donna-64bit-x86.h"
#else
	#include "ed25519-donna-32bit-tables.h"
	#include "ed25519-donna-64bit-x86-32bit.h"
#endif


#if defined(ED25519_SSE2)
	#include "ed25519-donna-32bit-sse2.h"
	#include "ed25519-donna-64bit-sse2.h"
	#include "ed25519-donna-impl-sse2.h"
#else
	#include "ed25519-donna-impl-base.h"
#endif

