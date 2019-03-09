/*
 * threefish.c
 * Copyright 2010 Jonathan Bowman
 * macro-based unrolled version (c) 2014,2018 Bernd Paysan
 * Added threefish-256 in 2018
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

static const uint8_t tf_rot_256[] = {
  14, 16,
  52, 57,
  23, 40,
  5, 37,
  25, 33,
  46, 12,
  58, 22,
  32, 32,
};

static const uint8_t tf_perm_256[] = {
  0, 1, 2, 3,
  0, 3, 2, 1,
  0, 1, 2, 3,
  0, 3, 2, 1,
};

static const uint8_t tf_rot_512[] = {
  46, 36, 19, 37,
  33, 27, 14, 42,
  17, 49, 36, 39,
  44, 9, 54, 56,
  39, 30, 34, 24,
  13, 50, 10, 17,
  25, 29, 39, 43,
  8, 35, 56, 22
};

static const uint8_t tf_perm_512[] = {
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

static inline void tf_prep_256(struct tf_ctx_256 *ctx)
{
  ctx->key[4] = ctx->key[0] ^ ctx->key[1] ^ ctx->key[2] ^
    ctx->key[3] ^ SKEIN_KS_PARITY;
}

static inline void tf_prep_512(struct tf_ctx_512 *ctx)
{
  ctx->key[8] = ctx->key[0] ^ ctx->key[1] ^ ctx->key[2] ^
    ctx->key[3] ^ ctx->key[4] ^ ctx->key[5] ^
    ctx->key[6] ^ ctx->key[7] ^ SKEIN_KS_PARITY;
}

static inline void tf_tweak_256(struct tf_ctx_256 *ctx)
{
  ctx->tweak[2] = ctx->tweak[0] ^ ctx->tweak[1];
}

static inline void tf_tweak_512(struct tf_ctx_512 *ctx)
{
  ctx->tweak[2] = ctx->tweak[0] ^ ctx->tweak[1];
}

#define PERMUTE_256(i)					\
  m = tf_perm_256[2*i];					\
  n = tf_perm_256[2*i+1];				\
  X[m] += X[n];						\
  X[n] = X[m] ^ rot_l64(X[n], tf_rot_256[i+s]);
#define TWEAKE_256(r)		 \
  for (y=0;y<4;y++)		 \
    X[y] += ctx->key[(r+y) % 5]; \
  X[1] += ctx->tweak[(r) % 3];	 \
  X[2] += ctx->tweak[(r+1) % 3]; \
  X[3] += r
#define ROUNDE_256(r)					\
  PERMUTE_256(0); PERMUTE_256(1); PERMUTE_256(2); PERMUTE_256(3);	\
  PERMUTE_256(4); PERMUTE_256(5); PERMUTE_256(6); PERMUTE_256(7);	\
  TWEAKE_256(r); s ^= 8

void tf_encrypt_256(struct tf_ctx_256 *ctx, const uint64_t *p,
		    uint64_t *out, int flags)
{
  uint64_t X[4];
  int8_t i,m,n,s=0,y;
  
  if(flags & 8) {
    tf_prep_256(ctx);
  }
  if(flags & 4) {
    tf_tweak_256(ctx);
  }

  for(i=0;i<4;i++) {
    X[i] = p[i] + ctx->key[i];
  }
  X[1] += ctx->tweak[0];
  X[2] += ctx->tweak[1];
  
  /* The rounds: */
  ROUNDE_256(1); ROUNDE_256(2); ROUNDE_256(3); ROUNDE_256(4); ROUNDE_256(5); ROUNDE_256(6);
  ROUNDE_256(7); ROUNDE_256(8); ROUNDE_256(9); ROUNDE_256(10); ROUNDE_256(11); ROUNDE_256(12);
  ROUNDE_256(13); ROUNDE_256(14); ROUNDE_256(15); ROUNDE_256(16); ROUNDE_256(17); ROUNDE_256(18);
  
  switch(flags & 3) {
  case 2: // Bernd mode, fall through to ECB mode
    for (i=0; i<4; i++) {
      ctx->key[i] ^= X[i] ^ p[i];
    }
  case 0: // ECB mode
    memmove(out, X, 32);
    break;
  case 1: // SKEIN mode
    for (i=0; i<4; i++) {
      out[i] = X[i] ^ p[i];
    } break;
  default: break;
  }
}

#define PERMUTE_512(i)					\
  m = tf_perm_512[2*i];					\
  n = tf_perm_512[2*i+1];					\
  X[m] += X[n];						\
  X[n] = X[m] ^ rot_l64(X[n], tf_rot_512[i+s]);
#define TWEAKE_512(r)		 \
  for (y=0;y<8;y++)		 \
    X[y] += ctx->key[(r+y) % 9]; \
  X[5] += ctx->tweak[(r) % 3];	 \
  X[6] += ctx->tweak[(r+1) % 3]; \
  X[7] += r
#define ROUNDE_512(r)					\
  PERMUTE_512(0); PERMUTE_512(1); PERMUTE_512(2); PERMUTE_512(3);	\
  PERMUTE_512(4); PERMUTE_512(5); PERMUTE_512(6); PERMUTE_512(7);	\
  PERMUTE_512(8); PERMUTE_512(9); PERMUTE_512(10); PERMUTE_512(11);	\
  PERMUTE_512(12); PERMUTE_512(13); PERMUTE_512(14); PERMUTE_512(15);	\
  TWEAKE_512(r); s ^= 16

void tf_encrypt_512(struct tf_ctx_512 *ctx, const uint64_t *p,
		    uint64_t *out, int flags)
{
  uint64_t X[8];
  int8_t i,m,n,s=0,y;
  
  if(flags & 8) {
    tf_prep_512(ctx);
  }
  if(flags & 4) {
    tf_tweak_512(ctx);
  }

  for(i=0;i<8;i++) {
    X[i] = p[i] + ctx->key[i];
  }
  X[5] += ctx->tweak[0];
  X[6] += ctx->tweak[1];
  
  /* The rounds: */
  ROUNDE_512(1); ROUNDE_512(2); ROUNDE_512(3); ROUNDE_512(4); ROUNDE_512(5); ROUNDE_512(6);
  ROUNDE_512(7); ROUNDE_512(8); ROUNDE_512(9); ROUNDE_512(10); ROUNDE_512(11); ROUNDE_512(12);
  ROUNDE_512(13); ROUNDE_512(14); ROUNDE_512(15); ROUNDE_512(16); ROUNDE_512(17); ROUNDE_512(18);
  
  switch(flags & 3) {
  case 2: // Bernd mode, fall through to ECB mode
    for (i=0; i<8; i++) {
      ctx->key[i] ^= X[i] ^ p[i];
    }
  case 0: // ECB mode
    memmove(out, X, 64);
    break;
  case 1: // SKEIN mode
    for (i=0; i<8; i++) {
      out[i] = X[i] ^ p[i];
    } break;
  default: break;
  }
}

#define PERMUTD_256(i)					\
  m = tf_perm_256[2*i];					\
  n = tf_perm_256[2*i+1];					\
  X[n] = rot_r64(X[m]^X[n], tf_rot_256[i+s]);	\
  X[m] -= X[n]
#define TWEAKD_256(r)				\
  for (y=0;y<4;y++) {				\
    X[y] -= ctx->key[(r+y) % 5];		\
  }						\
  X[1] -= ctx->tweak[(r) % 3];			\
  X[2] -= ctx->tweak[(r+1) % 3];		\
  X[3] -= r;
#define ROUNDD_256(r)				      \
  TWEAKD_256(r);					      \
  PERMUTD_256(7); PERMUTD_256(6); PERMUTD_256(5); PERMUTD_256(4);     \
  PERMUTD_256(3); PERMUTD_256(2); PERMUTD_256(1); PERMUTD_256(0);     \
  s ^= 8;

void tf_decrypt_256(struct tf_ctx_256 *ctx, const uint64_t *c, uint64_t *out, int flags)
{
  uint64_t X[4];
  int8_t i,m,n,s=8,y;

  if(flags & 8) {
    tf_prep_256(ctx);
  }
  if(flags & 4) {
    tf_tweak_256(ctx);
  }
  
  memmove(X, c, 32);
  
  /* The rounds: */
  ROUNDD_256(18); ROUNDD_256(17); ROUNDD_256(16); ROUNDD_256(15); ROUNDD_256(14); ROUNDD_256(13);
  ROUNDD_256(12); ROUNDD_256(11); ROUNDD_256(10); ROUNDD_256(9); ROUNDD_256(8); ROUNDD_256(7);
  ROUNDD_256(6); ROUNDD_256(5); ROUNDD_256(4); ROUNDD_256(3); ROUNDD_256(2); ROUNDD_256(1);
  
  for (i=0; i<4; i++) {
    X[i] -= ctx->key[i];
  }
  X[1] -= ctx->tweak[0];
  X[2] -= ctx->tweak[1];
  
  switch(flags & 3) {
  case 2: // Bernd mode, fall through to ECB mode
    for (i=0; i<4; i++) {
      ctx->key[i] ^= X[i] ^ c[i];
    }
  case 0: // ECB mode
    memmove(out, X, 32);
    break;
  default: break;
  }
}

#define PERMUTD_512(i)					\
  m = tf_perm_512[2*i];					\
  n = tf_perm_512[2*i+1];					\
  X[n] = rot_r64(X[m]^X[n], tf_rot_512[i+s]);	\
  X[m] -= X[n]
#define TWEAKD_512(r)				\
  for (y=0;y<8;y++) {				\
    X[y] -= ctx->key[(r+y) % 9];		\
  }						\
  X[5] -= ctx->tweak[(r) % 3];			\
  X[6] -= ctx->tweak[(r+1) % 3];		\
  X[7] -= r;
#define ROUNDD_512(r)				      \
  TWEAKD_512(r);					      \
  PERMUTD_512(15); PERMUTD_512(14); PERMUTD_512(13); PERMUTD_512(12); \
  PERMUTD_512(11); PERMUTD_512(10); PERMUTD_512(9); PERMUTD_512(8);   \
  PERMUTD_512(7); PERMUTD_512(6); PERMUTD_512(5); PERMUTD_512(4);     \
  PERMUTD_512(3); PERMUTD_512(2); PERMUTD_512(1); PERMUTD_512(0);     \
  s ^= 16;

void tf_decrypt_512(struct tf_ctx_512 *ctx, const uint64_t *c, uint64_t *out, int flags)
{
  uint64_t X[8];
  int8_t i,m,n,s=16,y;

  if(flags & 8) {
    tf_prep_512(ctx);
  }
  if(flags & 4) {
    tf_tweak_512(ctx);
  }
  
  memmove(X, c, 64);
  
  /* The rounds: */
  ROUNDD_512(18); ROUNDD_512(17); ROUNDD_512(16); ROUNDD_512(15); ROUNDD_512(14); ROUNDD_512(13);
  ROUNDD_512(12); ROUNDD_512(11); ROUNDD_512(10); ROUNDD_512(9); ROUNDD_512(8); ROUNDD_512(7);
  ROUNDD_512(6); ROUNDD_512(5); ROUNDD_512(4); ROUNDD_512(3); ROUNDD_512(2); ROUNDD_512(1);
  
  for (i=0; i<8; i++) {
    X[i] -= ctx->key[i];
  }
  X[5] -= ctx->tweak[0];
  X[6] -= ctx->tweak[1];
  
  switch(flags & 3) {
  case 2: // Bernd mode, fall through to ECB mode
    for (i=0; i<8; i++) {
      ctx->key[i] ^= X[i] ^ c[i];
    }
  case 0: // ECB mode
    memmove(out, X, 64);
    break;
  default: break;
  }
}
