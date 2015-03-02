/*
 * threefish.c
 * Copyright 2010 Jonathan Bowman
 * macro-based unrolled version (c) 2014 Bernd Paysan
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
 * implied. See the License for the specific language governing
 * permissions and limitations under the License.
*/

#include <string.h>
#include <stdio.h>
#include "threefish.h"
#include "skein.h"

static const uint8_t tf_rot_consts[] = {
  46, 36, 19, 37,
  33, 27, 14, 42,
  17, 49, 36, 39,
  44, 9, 54, 56,
  39, 30, 34, 24,
  13, 50, 10, 17,
  25, 29, 39, 43,
  8, 35, 56, 22
};

static const uint8_t tf_permut[] = {
  0,1,2,3,4,5,6,7,
  2,1,4,7,6,5,0,3,
  4,1,6,3,0,5,2,7,
  6,1,0,7,2,5,4,3
};

/* 64-bit rotate left */
static inline uint64_t rot_l64(uint64_t x, uint16_t N)
{
  return (x << N) ^ (x >> (64-N));
}

/* 64-bit rotate right */
static inline uint64_t rot_r64(uint64_t x, uint16_t N) {
  return (x >> N) ^ (x << (64-N));
}

static inline void tf_prep(struct tf_ctx *ctx)
{
  ctx->key[8] = ctx->key[0] ^ ctx->key[1] ^ ctx->key[2] ^
    ctx->key[3] ^ ctx->key[4] ^ ctx->key[5] ^
    ctx->key[6] ^ ctx->key[7] ^ SKEIN_KS_PARITY;
}

static inline void tf_tweak(struct tf_ctx *ctx)
{
  ctx->tweak[2] = ctx->tweak[0] ^ ctx->tweak[1];
}

#define PERMUTE(i)					\
  m = tf_permut[2*i];					\
  n = tf_permut[2*i+1];					\
  X[m] += X[n];						\
  X[n] = X[m] ^ rot_l64(X[n], tf_rot_consts[i+s]);
#define TWEAKE(r)		 \
  for (y=0;y<8;y++)		 \
    X[y] += ctx->key[(r+y) % 9]; \
  X[5] += ctx->tweak[(r) % 3];	 \
  X[6] += ctx->tweak[(r+1) % 3]; \
  X[7] += r
#define ROUNDE(r)					\
  PERMUTE(0); PERMUTE(1); PERMUTE(2); PERMUTE(3);	\
  PERMUTE(4); PERMUTE(5); PERMUTE(6); PERMUTE(7);	\
  PERMUTE(8); PERMUTE(9); PERMUTE(10); PERMUTE(11);	\
  PERMUTE(12); PERMUTE(13); PERMUTE(14); PERMUTE(15);	\
  TWEAKE(r); s ^= 16

void tf_encrypt(struct tf_ctx *ctx, const uint64_t *p,
		uint64_t *out, int flags)
{
  uint64_t X[8];
  int8_t i,m,n,s=0,y;
  
  if(flags & 8) {
    tf_prep(ctx);
  }
  if(flags & 4) {
    tf_tweak(ctx);
  }

  for(i=0;i<8;i++) {
    X[i] = p[i] + ctx->key[i];
  }
  X[5] += ctx->tweak[0];
  X[6] += ctx->tweak[1];
  
  /* The rounds: */
  ROUNDE(1); ROUNDE(2); ROUNDE(3); ROUNDE(4); ROUNDE(5); ROUNDE(6);
  ROUNDE(7); ROUNDE(8); ROUNDE(9); ROUNDE(10); ROUNDE(11); ROUNDE(12);
  ROUNDE(13); ROUNDE(14); ROUNDE(15); ROUNDE(16); ROUNDE(17); ROUNDE(18);
  
  switch(flags & 3) {
  case 2: // Bernd mode
    for (i=0; i<8; i++) {
      ctx->key[i] ^= X[i] ^ p[i];
    }
  case 0: // ECB mode
    memcpy(out, X, 64);
    break;
  case 1: // SKEIN mode
    for (i=0; i<8; i++) {
      out[i] = X[i] ^ p[i];
    } break;
  default: break;
  }
}

#define PERMUTD(i)					\
  m = tf_permut[2*i];					\
  n = tf_permut[2*i+1];					\
  X[n] = rot_r64(X[m]^X[n], tf_rot_consts[i+s]);	\
  X[m] -= X[n]
#define TWEAKD(r)				\
  for (y=0;y<8;y++) {				\
    X[y] -= ctx->key[(r+y) % 9];		\
  }						\
  X[5] -= ctx->tweak[(r) % 3];			\
  X[6] -= ctx->tweak[(r+1) % 3];		\
  X[7] -= r;
#define ROUNDD(r)				      \
  TWEAKD(r);					      \
  PERMUTD(15); PERMUTD(14); PERMUTD(13); PERMUTD(12); \
  PERMUTD(11); PERMUTD(10); PERMUTD(9); PERMUTD(8);   \
  PERMUTD(7); PERMUTD(6); PERMUTD(5); PERMUTD(4);     \
  PERMUTD(3); PERMUTD(2); PERMUTD(1); PERMUTD(0);     \
  s ^= 16;

void tf_decrypt(struct tf_ctx *ctx, const uint64_t *c, uint64_t *out, int flags)
{
  uint64_t X[8];
  int8_t i,m,n,s=16,y;

  if(flags & 8) {
    tf_prep(ctx);
  }
  if(flags & 4) {
    tf_tweak(ctx);
  }
  
  memcpy(X, c, 64);
  
  /* The rounds: */
  ROUNDD(18); ROUNDD(17); ROUNDD(16); ROUNDD(15); ROUNDD(14); ROUNDD(13);
  ROUNDD(12); ROUNDD(11); ROUNDD(10); ROUNDD(9); ROUNDD(8); ROUNDD(7);
  ROUNDD(6); ROUNDD(5); ROUNDD(4); ROUNDD(3); ROUNDD(2); ROUNDD(1);
  
  for (i=0; i<8; i++) {
    X[i] -= ctx->key[i];
  }
  X[5] -= ctx->tweak[0];
  X[6] -= ctx->tweak[1];
  
  switch(flags & 3) {
  case 2: // Bernd mode
    for (i=0; i<8; i++) {
      ctx->key[i] ^= X[i] ^ c[i];
    }
  case 0: // ECB mode
    memcpy(out, X, 64);
    break;
  default: break;
  }
}
