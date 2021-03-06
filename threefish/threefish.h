/*
 * threefish.h
 * Copyright 2010 Jonathan Bowman
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


#ifndef _THREEFISH_H_
#define _THREEFISH_H_ 1

#include <stdint.h>

typedef struct tf_ctx_256 {
	uint64_t key[5];
	uint64_t tweak[3];
} tf_ctx_256;

typedef struct tf_ctx_512 {
	uint64_t key[9];
	uint64_t tweak[3];
} tf_ctx_512;

static void tf_prep_256(struct tf_ctx_256 *ctx);
static void tf_tweak_256(struct tf_ctx_256 *ctx);
void tf_encrypt_256(struct tf_ctx_256 *ctx, const uint64_t *p, uint64_t *out, int flags);
void tf_decrypt_256(struct tf_ctx_256 *ctx, const uint64_t *c, uint64_t *out, int flags);

static void tf_prep_512(struct tf_ctx_512 *ctx);
static void tf_tweak_512(struct tf_ctx_512 *ctx);
void tf_encrypt_512(struct tf_ctx_512 *ctx, const uint64_t *p, uint64_t *out, int flags);
void tf_decrypt_512(struct tf_ctx_512 *ctx, const uint64_t *c, uint64_t *out, int flags);

static uint64_t rot_l64(uint64_t x, uint16_t N);
static uint64_t rot_r64(uint64_t x, uint16_t N);

#endif  /* ifndef _THREEFISH_H_ */
