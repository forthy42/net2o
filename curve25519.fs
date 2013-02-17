\ Interface to the Curve25519 primitive of the NaCL library

\ for ease to deploy, there is just the crypto_scalarmult primitive
\ everything else is done with Wurstkessel, anyways

c-library curve25519
    \c /* Copyright 2008, Google Inc.
    \c  * All rights reserved.
    \c  *
    \c  * Code released into the public domain.
    \c  *
    \c  * curve25519-donna: Curve25519 elliptic curve, public key function
    \c  *
    \c  * http://code.google.com/p/curve25519-donna/
    \c  *
    \c  * Adam Langley <agl@imperialviolet.org>
    \c  *
    \c  * Derived from public domain C code by Daniel J. Bernstein <djb@cr.yp.to>
    \c  *
    \c  * More information about curve25519 can be found here
    \c  *   http://cr.yp.to/ecdh.html
    \c  *
    \c  * djb's sample implementation of curve25519 is written in a special assembly
    \c  * language called qhasm and uses the floating point registers.
    \c  *
    \c  * This is, almost, a clean room reimplementation from the curve25519 paper. It
    \c  * uses many of the tricks described therein. Only the crecip function is taken
    \c  * from the sample implementation.
    \c  */
    \c 
    \c #include <string.h>
    \c #include <stdint.h>
    \c 
    \c #if INTPTR_MAX == INT32_MAX
    \c /*
    \c version 20081011
    \c Matthew Dempsky
    \c Public domain.
    \c Derived from public domain code by D. J. Bernstein.
    \c */
    \c 
    \c static void add(unsigned int out[32],const unsigned int a[32],const unsigned int b[32])
    \c {
    \c   unsigned int j;
    \c   unsigned int u;
    \c   u = 0;
    \c   for (j = 0;j < 31;++j) { u += a[j] + b[j]; out[j] = u & 255; u >>= 8; }
    \c   u += a[31] + b[31]; out[31] = u;
    \c }
    \c 
    \c static void sub(unsigned int out[32],const unsigned int a[32],const unsigned int b[32])
    \c {
    \c   unsigned int j;
    \c   unsigned int u;
    \c   u = 218;
    \c   for (j = 0;j < 31;++j) {
    \c     u += a[j] + 65280 - b[j];
    \c     out[j] = u & 255;
    \c     u >>= 8;
    \c   }
    \c   u += a[31] - b[31];
    \c   out[31] = u;
    \c }
    \c 
    \c static void squeeze(unsigned int a[32])
    \c {
    \c   unsigned int j;
    \c   unsigned int u;
    \c   u = 0;
    \c   for (j = 0;j < 31;++j) { u += a[j]; a[j] = u & 255; u >>= 8; }
    \c   u += a[31]; a[31] = u & 127;
    \c   u = 19 * (u >> 7);
    \c   for (j = 0;j < 31;++j) { u += a[j]; a[j] = u & 255; u >>= 8; }
    \c   u += a[31]; a[31] = u;
    \c }
    \c 
    \c static const unsigned int minusp[32] = {
    \c  19, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 128
    \c } ;
    \c 
    \c static void freeze(unsigned int a[32])
    \c {
    \c   unsigned int aorig[32];
    \c   unsigned int j;
    \c   unsigned int negative;
    \c 
    \c   for (j = 0;j < 32;++j) aorig[j] = a[j];
    \c   add(a,a,minusp);
    \c   negative = -((a[31] >> 7) & 1);
    \c   for (j = 0;j < 32;++j) a[j] ^= negative & (aorig[j] ^ a[j]);
    \c }
    \c 
    \c static void mult(unsigned int out[32],const unsigned int a[32],const unsigned int b[32])
    \c {
    \c   unsigned int i;
    \c   unsigned int j;
    \c   unsigned int u;
    \c 
    \c   for (i = 0;i < 32;++i) {
    \c     u = 0;
    \c     for (j = 0;j <= i;++j) u += a[j] * b[i - j];
    \c     for (j = i + 1;j < 32;++j) u += 38 * a[j] * b[i + 32 - j];
    \c     out[i] = u;
    \c   }
    \c   squeeze(out);
    \c }
    \c 
    \c static void mult121665(unsigned int out[32],const unsigned int a[32])
    \c {
    \c   unsigned int j;
    \c   unsigned int u;
    \c 
    \c   u = 0;
    \c   for (j = 0;j < 31;++j) { u += 121665 * a[j]; out[j] = u & 255; u >>= 8; }
    \c   u += 121665 * a[31]; out[31] = u & 127;
    \c   u = 19 * (u >> 7);
    \c   for (j = 0;j < 31;++j) { u += out[j]; out[j] = u & 255; u >>= 8; }
    \c   u += out[j]; out[j] = u;
    \c }
    \c 
    \c static void square(unsigned int out[32],const unsigned int a[32])
    \c {
    \c   unsigned int i;
    \c   unsigned int j;
    \c   unsigned int u;
    \c 
    \c   for (i = 0;i < 32;++i) {
    \c     u = 0;
    \c     for (j = 0;j < i - j;++j) u += a[j] * a[i - j];
    \c     for (j = i + 1;j < i + 32 - j;++j) u += 38 * a[j] * a[i + 32 - j];
    \c     u *= 2;
    \c     if ((i & 1) == 0) {
    \c       u += a[i / 2] * a[i / 2];
    \c       u += 38 * a[i / 2 + 16] * a[i / 2 + 16];
    \c     }
    \c     out[i] = u;
    \c   }
    \c   squeeze(out);
    \c }
    \c 
    \c static void select(unsigned int p[64],unsigned int q[64],const unsigned int r[64],const unsigned int s[64],unsigned int b)
    \c {
    \c   unsigned int j;
    \c   unsigned int t;
    \c   unsigned int bminus1;
    \c 
    \c   bminus1 = b - 1;
    \c   for (j = 0;j < 64;++j) {
    \c     t = bminus1 & (r[j] ^ s[j]);
    \c     p[j] = s[j] ^ t;
    \c     q[j] = r[j] ^ t;
    \c   }
    \c }
    \c 
    \c static void mainloop(unsigned int work[64],const unsigned char e[32])
    \c {
    \c   unsigned int xzm1[64];
    \c   unsigned int xzm[64];
    \c   unsigned int xzmb[64];
    \c   unsigned int xzm1b[64];
    \c   unsigned int xznb[64];
    \c   unsigned int xzn1b[64];
    \c   unsigned int a0[64];
    \c   unsigned int a1[64];
    \c   unsigned int b0[64];
    \c   unsigned int b1[64];
    \c   unsigned int c1[64];
    \c   unsigned int r[32];
    \c   unsigned int s[32];
    \c   unsigned int t[32];
    \c   unsigned int u[32];
    \c   unsigned int i;
    \c   unsigned int j;
    \c   unsigned int b;
    \c   int pos;
    \c 
    \c   for (j = 0;j < 32;++j) xzm1[j] = work[j];
    \c   xzm1[32] = 1;
    \c   for (j = 33;j < 64;++j) xzm1[j] = 0;
    \c 
    \c   xzm[0] = 1;
    \c   for (j = 1;j < 64;++j) xzm[j] = 0;
    \c 
    \c   for (pos = 254;pos >= 0;--pos) {
    \c     b = e[pos / 8] >> (pos & 7);
    \c     b &= 1;
    \c     select(xzmb,xzm1b,xzm,xzm1,b);
    \c     add(a0,xzmb,xzmb + 32);
    \c     sub(a0 + 32,xzmb,xzmb + 32);
    \c     add(a1,xzm1b,xzm1b + 32);
    \c     sub(a1 + 32,xzm1b,xzm1b + 32);
    \c     square(b0,a0);
    \c     square(b0 + 32,a0 + 32);
    \c     mult(b1,a1,a0 + 32);
    \c     mult(b1 + 32,a1 + 32,a0);
    \c     add(c1,b1,b1 + 32);
    \c     sub(c1 + 32,b1,b1 + 32);
    \c     square(r,c1 + 32);
    \c     sub(s,b0,b0 + 32);
    \c     mult121665(t,s);
    \c     add(u,t,b0);
    \c     mult(xznb,b0,b0 + 32);
    \c     mult(xznb + 32,s,u);
    \c     square(xzn1b,c1);
    \c     mult(xzn1b + 32,r,work);
    \c     select(xzm,xzm1,xznb,xzn1b,b);
    \c   }
    \c 
    \c   for (j = 0;j < 64;++j) work[j] = xzm[j];
    \c }
    \c 
    \c static void recip(unsigned int out[32],const unsigned int z[32])
    \c {
    \c   unsigned int z2[32];
    \c   unsigned int z9[32];
    \c   unsigned int z11[32];
    \c   unsigned int z2_5_0[32];
    \c   unsigned int z2_10_0[32];
    \c   unsigned int z2_20_0[32];
    \c   unsigned int z2_50_0[32];
    \c   unsigned int z2_100_0[32];
    \c   unsigned int t0[32];
    \c   unsigned int t1[32];
    \c   int i;
    \c 
    \c   /* 2 */ square(z2,z);
    \c   /* 4 */ square(t1,z2);
    \c   /* 8 */ square(t0,t1);
    \c   /* 9 */ mult(z9,t0,z);
    \c   /* 11 */ mult(z11,z9,z2);
    \c   /* 22 */ square(t0,z11);
    \c   /* 2^5 - 2^0 = 31 */ mult(z2_5_0,t0,z9);
    \c 
    \c   /* 2^6 - 2^1 */ square(t0,z2_5_0);
    \c   /* 2^7 - 2^2 */ square(t1,t0);
    \c   /* 2^8 - 2^3 */ square(t0,t1);
    \c   /* 2^9 - 2^4 */ square(t1,t0);
    \c   /* 2^10 - 2^5 */ square(t0,t1);
    \c   /* 2^10 - 2^0 */ mult(z2_10_0,t0,z2_5_0);
    \c 
    \c   /* 2^11 - 2^1 */ square(t0,z2_10_0);
    \c   /* 2^12 - 2^2 */ square(t1,t0);
    \c   /* 2^20 - 2^10 */ for (i = 2;i < 10;i += 2) { square(t0,t1); square(t1,t0); }
    \c   /* 2^20 - 2^0 */ mult(z2_20_0,t1,z2_10_0);
    \c 
    \c   /* 2^21 - 2^1 */ square(t0,z2_20_0);
    \c   /* 2^22 - 2^2 */ square(t1,t0);
    \c   /* 2^40 - 2^20 */ for (i = 2;i < 20;i += 2) { square(t0,t1); square(t1,t0); }
    \c   /* 2^40 - 2^0 */ mult(t0,t1,z2_20_0);
    \c 
    \c   /* 2^41 - 2^1 */ square(t1,t0);
    \c   /* 2^42 - 2^2 */ square(t0,t1);
    \c   /* 2^50 - 2^10 */ for (i = 2;i < 10;i += 2) { square(t1,t0); square(t0,t1); }
    \c   /* 2^50 - 2^0 */ mult(z2_50_0,t0,z2_10_0);
    \c 
    \c   /* 2^51 - 2^1 */ square(t0,z2_50_0);
    \c   /* 2^52 - 2^2 */ square(t1,t0);
    \c   /* 2^100 - 2^50 */ for (i = 2;i < 50;i += 2) { square(t0,t1); square(t1,t0); }
    \c   /* 2^100 - 2^0 */ mult(z2_100_0,t1,z2_50_0);
    \c 
    \c   /* 2^101 - 2^1 */ square(t1,z2_100_0);
    \c   /* 2^102 - 2^2 */ square(t0,t1);
    \c   /* 2^200 - 2^100 */ for (i = 2;i < 100;i += 2) { square(t1,t0); square(t0,t1); }
    \c   /* 2^200 - 2^0 */ mult(t1,t0,z2_100_0);
    \c 
    \c   /* 2^201 - 2^1 */ square(t0,t1);
    \c   /* 2^202 - 2^2 */ square(t1,t0);
    \c   /* 2^250 - 2^50 */ for (i = 2;i < 50;i += 2) { square(t0,t1); square(t1,t0); }
    \c   /* 2^250 - 2^0 */ mult(t0,t1,z2_50_0);
    \c 
    \c   /* 2^251 - 2^1 */ square(t1,t0);
    \c   /* 2^252 - 2^2 */ square(t0,t1);
    \c   /* 2^253 - 2^3 */ square(t1,t0);
    \c   /* 2^254 - 2^4 */ square(t0,t1);
    \c   /* 2^255 - 2^5 */ square(t1,t0);
    \c   /* 2^255 - 21 */ mult(out,t1,z11);
    \c }
    \c 
    \c int crypto_scalarmult(unsigned char *q,
    \c   const unsigned char *n,
    \c   const unsigned char *p)
    \c {
    \c   unsigned int work[96];
    \c   unsigned char e[32];
    \c   unsigned int i;
    \c   for (i = 0;i < 32;++i) e[i] = n[i];
    \c   e[0] &= 248;
    \c   e[31] &= 127;
    \c   e[31] |= 64;
    \c   for (i = 0;i < 32;++i) work[i] = p[i];
    \c   mainloop(work,e);
    \c   recip(work + 32,work + 32);
    \c   mult(work + 64,work,work + 32);
    \c   freeze(work + 64);
    \c   for (i = 0;i < 32;++i) q[i] = work[64 + i];
    \c   return 0;
    \c }
    \c #elif INTPTR_MAX == INT64_MAX
    \c typedef uint8_t u8;
    \c typedef uint64_t felem;
    \c // This is a special gcc mode for 128-bit integers. It's implemented on 64-bit
    \c // platforms only as far as I know.
    \c typedef unsigned uint128_t __attribute__((mode(TI)));
    \c 
    \c /* Sum two numbers: output += in */
    \c static void fsum(felem *output, const felem *in) {
    \c   unsigned i;
    \c   for (i = 0; i < 5; ++i) output[i] += in[i];
    \c }
    \c 
    \c /* Find the difference of two numbers: output = in - output
    \c  * (note the order of the arguments!)
    \c  */
    \c static void fdifference_backwards(felem *ioutput, const felem *iin) {
    \c   static const int64_t twotothe51 = (1l << 51);
    \c   const int64_t *in = (const int64_t *) iin;
    \c   int64_t *out = (int64_t *) ioutput;
    \c 
    \c   out[0] = in[0] - out[0];
    \c   out[1] = in[1] - out[1];
    \c   out[2] = in[2] - out[2];
    \c   out[3] = in[3] - out[3];
    \c   out[4] = in[4] - out[4];
    \c 
    \c   // An arithmetic shift right of 63 places turns a positive number to 0 and a
    \c   // negative number to all 1's. This gives us a bitmask that lets us avoid
    \c   // side-channel prone branches.
    \c   int64_t t;
    \c 
    \c #define NEGCHAIN(a,b) \
    \c   t = out[a] >> 63; \
    \c   out[a] += twotothe51 & t; \
    \c   out[b] -= 1 & t;
    \c 
    \c #define NEGCHAIN19(a,b) \
    \c   t = out[a] >> 63; \
    \c   out[a] += twotothe51 & t; \
    \c   out[b] -= 19 & t;
    \c 
    \c   NEGCHAIN(0, 1);
    \c   NEGCHAIN(1, 2);
    \c   NEGCHAIN(2, 3);
    \c   NEGCHAIN(3, 4);
    \c   NEGCHAIN19(4, 0);
    \c   NEGCHAIN(0, 1);
    \c   NEGCHAIN(1, 2);
    \c   NEGCHAIN(2, 3);
    \c   NEGCHAIN(3, 4);
    \c }
    \c 
    \c /* Multiply a number by a scalar: output = in * scalar */
    \c static void fscalar_product(felem *output, const felem *in, const felem scalar) {
    \c   uint128_t a;
    \c 
    \c   a = ((uint128_t) in[0]) * scalar;
    \c   output[0] = a & 0x7ffffffffffff;
    \c 
    \c   a = ((uint128_t) in[1]) * scalar + (a >> 51);
    \c   output[1] = a & 0x7ffffffffffff;
    \c 
    \c   a = ((uint128_t) in[2]) * scalar + (a >> 51);
    \c   output[2] = a & 0x7ffffffffffff;
    \c 
    \c   a = ((uint128_t) in[3]) * scalar + (a >> 51);
    \c   output[3] = a & 0x7ffffffffffff;
    \c 
    \c   a = ((uint128_t) in[4]) * scalar + (a >> 51);
    \c   output[4] = a & 0x7ffffffffffff;
    \c 
    \c   output[0] += (a >> 51) * 19;
    \c }
    \c 
    \c /* Multiply two numbers: output = in2 * in
    \c  *
    \c  * output must be distinct to both inputs. The inputs are reduced coefficient
    \c  * form, the output is not.
    \c  */
    \c static void fmul(felem *output, const felem *in2, const felem *in) {
    \c   uint128_t t[9];
    \c 
    \c   t[0] = ((uint128_t) in[0]) * in2[0];
    \c   t[1] = ((uint128_t) in[0]) * in2[1] +
    \c          ((uint128_t) in[1]) * in2[0];
    \c   t[2] = ((uint128_t) in[0]) * in2[2] +
    \c          ((uint128_t) in[2]) * in2[0] +
    \c          ((uint128_t) in[1]) * in2[1];
    \c   t[3] = ((uint128_t) in[0]) * in2[3] +
    \c          ((uint128_t) in[3]) * in2[0] +
    \c          ((uint128_t) in[1]) * in2[2] +
    \c          ((uint128_t) in[2]) * in2[1];
    \c   t[4] = ((uint128_t) in[0]) * in2[4] +
    \c          ((uint128_t) in[4]) * in2[0] +
    \c          ((uint128_t) in[3]) * in2[1] +
    \c          ((uint128_t) in[1]) * in2[3] +
    \c          ((uint128_t) in[2]) * in2[2];
    \c   t[5] = ((uint128_t) in[4]) * in2[1] +
    \c          ((uint128_t) in[1]) * in2[4] +
    \c          ((uint128_t) in[2]) * in2[3] +
    \c          ((uint128_t) in[3]) * in2[2];
    \c   t[6] = ((uint128_t) in[4]) * in2[2] +
    \c          ((uint128_t) in[2]) * in2[4] +
    \c          ((uint128_t) in[3]) * in2[3];
    \c   t[7] = ((uint128_t) in[3]) * in2[4] +
    \c          ((uint128_t) in[4]) * in2[3];
    \c   t[8] = ((uint128_t) in[4]) * in2[4];
    \c 
    \c   t[0] += t[5] * 19;
    \c   t[1] += t[6] * 19;
    \c   t[2] += t[7] * 19;
    \c   t[3] += t[8] * 19;
    \c 
    \c   t[1] += t[0] >> 51;
    \c   t[0] &= 0x7ffffffffffff;
    \c   t[2] += t[1] >> 51;
    \c   t[1] &= 0x7ffffffffffff;
    \c   t[3] += t[2] >> 51;
    \c   t[2] &= 0x7ffffffffffff;
    \c   t[4] += t[3] >> 51;
    \c   t[3] &= 0x7ffffffffffff;
    \c   t[0] += 19 * (t[4] >> 51);
    \c   t[4] &= 0x7ffffffffffff;
    \c   t[1] += t[0] >> 51;
    \c   t[0] &= 0x7ffffffffffff;
    \c   t[2] += t[1] >> 51;
    \c   t[1] &= 0x7ffffffffffff;
    \c 
    \c   output[0] = t[0];
    \c   output[1] = t[1];
    \c   output[2] = t[2];
    \c   output[3] = t[3];
    \c   output[4] = t[4];
    \c }
    \c 
    \c static void
    \c fsquare(felem *output, const felem *in) {
    \c   uint128_t t[9];
    \c 
    \c   t[0] = ((uint128_t) in[0]) * in[0];
    \c   t[1] = ((uint128_t) in[0]) * in[1] * 2;
    \c   t[2] = ((uint128_t) in[0]) * in[2] * 2 +
    \c          ((uint128_t) in[1]) * in[1];
    \c   t[3] = ((uint128_t) in[0]) * in[3] * 2 +
    \c          ((uint128_t) in[1]) * in[2] * 2;
    \c   t[4] = ((uint128_t) in[0]) * in[4] * 2 +
    \c          ((uint128_t) in[3]) * in[1] * 2 +
    \c          ((uint128_t) in[2]) * in[2];
    \c   t[5] = ((uint128_t) in[4]) * in[1] * 2 +
    \c          ((uint128_t) in[2]) * in[3] * 2;
    \c   t[6] = ((uint128_t) in[4]) * in[2] * 2 +
    \c          ((uint128_t) in[3]) * in[3];
    \c   t[7] = ((uint128_t) in[3]) * in[4] * 2;
    \c   t[8] = ((uint128_t) in[4]) * in[4];
    \c 
    \c   t[0] += t[5] * 19;
    \c   t[1] += t[6] * 19;
    \c   t[2] += t[7] * 19;
    \c   t[3] += t[8] * 19;
    \c 
    \c   t[1] += t[0] >> 51;
    \c   t[0] &= 0x7ffffffffffff;
    \c   t[2] += t[1] >> 51;
    \c   t[1] &= 0x7ffffffffffff;
    \c   t[3] += t[2] >> 51;
    \c   t[2] &= 0x7ffffffffffff;
    \c   t[4] += t[3] >> 51;
    \c   t[3] &= 0x7ffffffffffff;
    \c   t[0] += 19 * (t[4] >> 51);
    \c   t[4] &= 0x7ffffffffffff;
    \c   t[1] += t[0] >> 51;
    \c   t[0] &= 0x7ffffffffffff;
    \c 
    \c   output[0] = t[0];
    \c   output[1] = t[1];
    \c   output[2] = t[2];
    \c   output[3] = t[3];
    \c   output[4] = t[4];
    \c }
    \c 
    \c /* Take a little-endian, 32-byte number and expand it into polynomial form */
    \c static void
    \c fexpand(felem *output, const u8 *in) {
    \c   output[0] = *((const uint64_t *)(in)) & 0x7ffffffffffff;
    \c   output[1] = (*((const uint64_t *)(in+6)) >> 3) & 0x7ffffffffffff;
    \c   output[2] = (*((const uint64_t *)(in+12)) >> 6) & 0x7ffffffffffff;
    \c   output[3] = (*((const uint64_t *)(in+19)) >> 1) & 0x7ffffffffffff;
    \c   output[4] = (*((const uint64_t *)(in+25)) >> 4) & 0x7ffffffffffff;
    \c }
    \c 
    \c /* Take a fully reduced polynomial form number and contract it into a
    \c  * little-endian, 32-byte array
    \c  */
    \c static void
    \c fcontract(u8 *output, const felem *input) {
    \c   uint128_t t[5];
    \c 
    \c   t[0] = input[0];
    \c   t[1] = input[1];
    \c   t[2] = input[2];
    \c   t[3] = input[3];
    \c   t[4] = input[4];
    \c 
    \c   t[1] += t[0] >> 51; t[0] &= 0x7ffffffffffff;
    \c   t[2] += t[1] >> 51; t[1] &= 0x7ffffffffffff;
    \c   t[3] += t[2] >> 51; t[2] &= 0x7ffffffffffff;
    \c   t[4] += t[3] >> 51; t[3] &= 0x7ffffffffffff;
    \c   t[0] += 19 * (t[4] >> 51); t[4] &= 0x7ffffffffffff;
    \c 
    \c   t[1] += t[0] >> 51; t[0] &= 0x7ffffffffffff;
    \c   t[2] += t[1] >> 51; t[1] &= 0x7ffffffffffff;
    \c   t[3] += t[2] >> 51; t[2] &= 0x7ffffffffffff;
    \c   t[4] += t[3] >> 51; t[3] &= 0x7ffffffffffff;
    \c   t[0] += 19 * (t[4] >> 51); t[4] &= 0x7ffffffffffff;
    \c 
    \c   /* now t is between 0 and 2^255-1, properly carried. */
    \c   /* case 1: between 0 and 2^255-20. case 2: between 2^255-19 and 2^255-1. */
    \c 
    \c   t[0] += 19;
    \c 
    \c   t[1] += t[0] >> 51; t[0] &= 0x7ffffffffffff;
    \c   t[2] += t[1] >> 51; t[1] &= 0x7ffffffffffff;
    \c   t[3] += t[2] >> 51; t[2] &= 0x7ffffffffffff;
    \c   t[4] += t[3] >> 51; t[3] &= 0x7ffffffffffff;
    \c   t[0] += 19 * (t[4] >> 51); t[4] &= 0x7ffffffffffff;
    \c 
    \c   /* now between 19 and 2^255-1 in both cases, and offset by 19. */
    \c 
    \c   t[0] += 0x8000000000000 - 19;
    \c   t[1] += 0x8000000000000 - 1;
    \c   t[2] += 0x8000000000000 - 1;
    \c   t[3] += 0x8000000000000 - 1;
    \c   t[4] += 0x8000000000000 - 1;
    \c 
    \c   /* now between 2^255 and 2^256-20, and offset by 2^255. */
    \c 
    \c   t[1] += t[0] >> 51; t[0] &= 0x7ffffffffffff;
    \c   t[2] += t[1] >> 51; t[1] &= 0x7ffffffffffff;
    \c   t[3] += t[2] >> 51; t[2] &= 0x7ffffffffffff;
    \c   t[4] += t[3] >> 51; t[3] &= 0x7ffffffffffff;
    \c   t[4] &= 0x7ffffffffffff;
    \c 
    \c   *((uint64_t *)(output)) = t[0] | (t[1] << 51);
    \c   *((uint64_t *)(output+8)) = (t[1] >> 13) | (t[2] << 38);
    \c   *((uint64_t *)(output+16)) = (t[2] >> 26) | (t[3] << 25);
    \c   *((uint64_t *)(output+24)) = (t[3] >> 39) | (t[4] << 12);
    \c }
    \c 
    \c /* Input: Q, Q', Q-Q'
    \c  * Output: 2Q, Q+Q'
    \c  *
    \c  *   x2 z3: long form
    \c  *   x3 z3: long form
    \c  *   x z: short form, destroyed
    \c  *   xprime zprime: short form, destroyed
    \c  *   qmqp: short form, preserved
    \c  */
    \c static void
    \c fmonty(felem *x2, felem *z2, /* output 2Q */
    \c        felem *x3, felem *z3, /* output Q + Q' */
    \c        felem *x, felem *z,   /* input Q */
    \c        felem *xprime, felem *zprime, /* input Q' */
    \c        const felem *qmqp /* input Q - Q' */) {
    \c   felem origx[5], origxprime[5], zzz[5], xx[5], zz[5], xxprime[5],
    \c         zzprime[5], zzzprime[5];
    \c 
    \c   memcpy(origx, x, 5 * sizeof(felem));
    \c   fsum(x, z);
    \c   fdifference_backwards(z, origx);  // does x - z
    \c 
    \c   memcpy(origxprime, xprime, sizeof(felem) * 5);
    \c   fsum(xprime, zprime);
    \c   fdifference_backwards(zprime, origxprime);
    \c   fmul(xxprime, xprime, z);
    \c   fmul(zzprime, x, zprime);
    \c   memcpy(origxprime, xxprime, sizeof(felem) * 5);
    \c   fsum(xxprime, zzprime);
    \c   fdifference_backwards(zzprime, origxprime);
    \c   fsquare(x3, xxprime);
    \c   fsquare(zzzprime, zzprime);
    \c   fmul(z3, zzzprime, qmqp);
    \c 
    \c   fsquare(xx, x);
    \c   fsquare(zz, z);
    \c   fmul(x2, xx, zz);
    \c   fdifference_backwards(zz, xx);  // does zz = xx - zz
    \c   fscalar_product(zzz, zz, 121665);
    \c   fsum(zzz, xx);
    \c   fmul(z2, zz, zzz);
    \c }
    \c 
    \c // -----------------------------------------------------------------------------
    \c // Maybe swap the contents of two felem arrays (@a and @b), each @len elements
    \c // long. Perform the swap iff @swap is non-zero.
    \c //
    \c // This function performs the swap without leaking any side-channel
    \c // information.
    \c // -----------------------------------------------------------------------------
    \c static void
    \c swap_conditional(felem *a, felem *b, unsigned len, felem iswap) {
    \c   unsigned i;
    \c   const felem swap = -iswap;
    \c 
    \c   for (i = 0; i < len; ++i) {
    \c     const felem x = swap & (a[i] ^ b[i]);
    \c     a[i] ^= x;
    \c     b[i] ^= x;
    \c   }
    \c }
    \c 
    \c /* Calculates nQ where Q is the x-coordinate of a point on the curve
    \c  *
    \c  *   resultx/resultz: the x coordinate of the resulting curve point (short form)
    \c  *   n: a little endian, 32-byte number
    \c  *   q: a point of the curve (short form)
    \c  */
    \c static void
    \c cmult(felem *resultx, felem *resultz, const u8 *n, const felem *q) {
    \c   felem a[5] = {0}, b[5] = {1}, c[5] = {1}, d[5] = {0};
    \c   felem *nqpqx = a, *nqpqz = b, *nqx = c, *nqz = d, *t;
    \c   felem e[5] = {0}, f[5] = {1}, g[5] = {0}, h[5] = {1};
    \c   felem *nqpqx2 = e, *nqpqz2 = f, *nqx2 = g, *nqz2 = h;
    \c 
    \c   unsigned i, j;
    \c 
    \c   memcpy(nqpqx, q, sizeof(felem) * 5);
    \c 
    \c   for (i = 0; i < 32; ++i) {
    \c     u8 byte = n[31 - i];
    \c     for (j = 0; j < 8; ++j) {
    \c       const felem bit = byte >> 7;
    \c 
    \c       swap_conditional(nqx, nqpqx, 5, bit);
    \c       swap_conditional(nqz, nqpqz, 5, bit);
    \c       fmonty(nqx2, nqz2,
    \c              nqpqx2, nqpqz2,
    \c              nqx, nqz,
    \c              nqpqx, nqpqz,
    \c              q);
    \c       swap_conditional(nqx2, nqpqx2, 5, bit);
    \c       swap_conditional(nqz2, nqpqz2, 5, bit);
    \c 
    \c       t = nqx;
    \c       nqx = nqx2;
    \c       nqx2 = t;
    \c       t = nqz;
    \c       nqz = nqz2;
    \c       nqz2 = t;
    \c       t = nqpqx;
    \c       nqpqx = nqpqx2;
    \c       nqpqx2 = t;
    \c       t = nqpqz;
    \c       nqpqz = nqpqz2;
    \c       nqpqz2 = t;
    \c 
    \c       byte <<= 1;
    \c     }
    \c   }
    \c 
    \c   memcpy(resultx, nqx, sizeof(felem) * 5);
    \c   memcpy(resultz, nqz, sizeof(felem) * 5);
    \c }
    \c 
    \c // -----------------------------------------------------------------------------
    \c // Shamelessly copied from djb's code
    \c // -----------------------------------------------------------------------------
    \c static void
    \c crecip(felem *out, const felem *z) {
    \c   felem z2[5];
    \c   felem z9[5];
    \c   felem z11[5];
    \c   felem z2_5_0[5];
    \c   felem z2_10_0[5];
    \c   felem z2_20_0[5];
    \c   felem z2_50_0[5];
    \c   felem z2_100_0[5];
    \c   felem t0[5];
    \c   felem t1[5];
    \c   int i;
    \c 
    \c   /* 2 */ fsquare(z2,z);
    \c   /* 4 */ fsquare(t1,z2);
    \c   /* 8 */ fsquare(t0,t1);
    \c   /* 9 */ fmul(z9,t0,z);
    \c   /* 11 */ fmul(z11,z9,z2);
    \c   /* 22 */ fsquare(t0,z11);
    \c   /* 2^5 - 2^0 = 31 */ fmul(z2_5_0,t0,z9);
    \c 
    \c   /* 2^6 - 2^1 */ fsquare(t0,z2_5_0);
    \c   /* 2^7 - 2^2 */ fsquare(t1,t0);
    \c   /* 2^8 - 2^3 */ fsquare(t0,t1);
    \c   /* 2^9 - 2^4 */ fsquare(t1,t0);
    \c   /* 2^10 - 2^5 */ fsquare(t0,t1);
    \c   /* 2^10 - 2^0 */ fmul(z2_10_0,t0,z2_5_0);
    \c 
    \c   /* 2^11 - 2^1 */ fsquare(t0,z2_10_0);
    \c   /* 2^12 - 2^2 */ fsquare(t1,t0);
    \c   /* 2^20 - 2^10 */ for (i = 2;i < 10;i += 2) { fsquare(t0,t1); fsquare(t1,t0); }
    \c   /* 2^20 - 2^0 */ fmul(z2_20_0,t1,z2_10_0);
    \c 
    \c   /* 2^21 - 2^1 */ fsquare(t0,z2_20_0);
    \c   /* 2^22 - 2^2 */ fsquare(t1,t0);
    \c   /* 2^40 - 2^20 */ for (i = 2;i < 20;i += 2) { fsquare(t0,t1); fsquare(t1,t0); }
    \c   /* 2^40 - 2^0 */ fmul(t0,t1,z2_20_0);
    \c 
    \c   /* 2^41 - 2^1 */ fsquare(t1,t0);
    \c   /* 2^42 - 2^2 */ fsquare(t0,t1);
    \c   /* 2^50 - 2^10 */ for (i = 2;i < 10;i += 2) { fsquare(t1,t0); fsquare(t0,t1); }
    \c   /* 2^50 - 2^0 */ fmul(z2_50_0,t0,z2_10_0);
    \c 
    \c   /* 2^51 - 2^1 */ fsquare(t0,z2_50_0);
    \c   /* 2^52 - 2^2 */ fsquare(t1,t0);
    \c   /* 2^100 - 2^50 */ for (i = 2;i < 50;i += 2) { fsquare(t0,t1); fsquare(t1,t0); }
    \c   /* 2^100 - 2^0 */ fmul(z2_100_0,t1,z2_50_0);
    \c 
    \c   /* 2^101 - 2^1 */ fsquare(t1,z2_100_0);
    \c   /* 2^102 - 2^2 */ fsquare(t0,t1);
    \c   /* 2^200 - 2^100 */ for (i = 2;i < 100;i += 2) { fsquare(t1,t0); fsquare(t0,t1); }
    \c   /* 2^200 - 2^0 */ fmul(t1,t0,z2_100_0);
    \c 
    \c   /* 2^201 - 2^1 */ fsquare(t0,t1);
    \c   /* 2^202 - 2^2 */ fsquare(t1,t0);
    \c   /* 2^250 - 2^50 */ for (i = 2;i < 50;i += 2) { fsquare(t0,t1); fsquare(t1,t0); }
    \c   /* 2^250 - 2^0 */ fmul(t0,t1,z2_50_0);
    \c 
    \c   /* 2^251 - 2^1 */ fsquare(t1,t0);
    \c   /* 2^252 - 2^2 */ fsquare(t0,t1);
    \c   /* 2^253 - 2^3 */ fsquare(t1,t0);
    \c   /* 2^254 - 2^4 */ fsquare(t0,t1);
    \c   /* 2^255 - 2^5 */ fsquare(t1,t0);
    \c   /* 2^255 - 21 */ fmul(out,t1,z11);
    \c }
    \c 
    \c int
    \c crypto_scalarmult(u8 *mypublic, const u8 *secret, const u8 *basepoint) {
    \c   felem bp[5], x[5], z[5], zmone[5];
    \c   unsigned char e[32];
    \c   int i;
    \c   for (i = 0;i < 32;++i) e[i] = secret[i];
    \c   e[0] &= 248;
    \c   e[31] &= 127;
    \c   e[31] |= 64;
    \c   fexpand(bp, basepoint);
    \c   cmult(x, z, e, bp);
    \c   crecip(zmone, z);
    \c   fmul(z, x, zmone);
    \c   fcontract(mypublic, z);
    \c   return 0;
    \c }
    \c #else
    \c #error "INTPTR_MAX neither 32 or 64 bit integer type, don't know what to do"
    \c #endif

    c-function crypto_scalarmult crypto_scalarmult a a a -- void ( s pk sk -- )
end-c-library

32 Constant KEYBYTES

KEYBYTES buffer: base9
base9 KEYBYTES erase 9 base9 c!


