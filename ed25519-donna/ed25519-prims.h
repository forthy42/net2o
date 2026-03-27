#ifndef ED25519_PRIMS_H
#define ED25519_PRIMS_H

#include <stdlib.h>
#include <ed25519-donna-portable-identify.h>

#if defined(ED25519_SSE2)
typedef uint32_t bignum25519[12];
typedef uint64_t bignum256modm_element_t;
typedef bignum256modm_element_t bignum256modm[5];
#elif defined(HAVE_UINT128)
typedef uint64_t bignum256modm_element_t;
typedef bignum256modm_element_t bignum256modm[5];
typedef uint64_t bignum25519[5];
#else
typedef uint32_t bignum256modm_element_t;
typedef bignum256modm_element_t bignum256modm[9];
typedef uint32_t bignum25519[10];
#endif

typedef struct ge25519_t {
        bignum25519 x, y, z, t;
} ge25519;
typedef struct ge25519_niels_t {
	bignum25519 ysubx, xaddy, t2d;
} ge25519_niels;

extern void ge25519_pack(unsigned char r[32], const ge25519 *p);
extern int ge25519_unpack_negative_vartime(ge25519 *r, const unsigned char p[32]);
extern void ge25519_add(ge25519 *r, const ge25519 *p,  const ge25519 *q);
extern void ge25519_double_scalarmult_vartime(ge25519 *r, const ge25519 *p1, const bignum256modm s1, const bignum256modm s2);
extern void ge25519_scalarmult_vartime(ge25519 *r, const ge25519 *p1, const bignum256modm s1);
extern void ge25519_scalarmult(ge25519 *r, const ge25519 *p1, const bignum256modm s1);
extern void ge25519_scalarmult_base(ge25519 *r, const bignum256modm s);
extern void expand256_modm(bignum256modm out, const unsigned char *in, size_t len);
extern void expand_raw256_modm(bignum256modm out, const unsigned char in[32]);
extern void contract256_modm(unsigned char out[32], const bignum256modm in);
extern void add256_modm(bignum256modm r, const bignum256modm x, const bignum256modm y);
extern void mul256_modm(bignum256modm r, const bignum256modm x, const bignum256modm y);
extern void sub256_modm_batch(bignum256modm out, const bignum256modm a, const bignum256modm b, size_t limbsize);
extern void invert256_modm(bignum256modm recip, const bignum256modm s);
extern int lt256_modm_batch(const bignum256modm a, const bignum256modm b, size_t limbsize);
extern int lte256_modm_batch(const bignum256modm a, const bignum256modm b, size_t limbsize);
extern int iszero256_modm_batch(const bignum256modm a);
extern int isone256_modm_batch(const bignum256modm a);

extern const ge25519 ge25519_basepoint;
extern const bignum25519 ge25519_ecd;
extern const bignum25519 ge25519_ec2d;
extern const bignum25519 ge25519_sqrtneg1;
extern const ge25519_niels ge25519_niels_sliding_multiples[32];

#endif
