/*
 * skein.h
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

#ifndef _SKEIN_H_
#define _SKEIN_H_     1

#include <stdint.h>
#include "threefish.h"

struct skein_ctx {
	uint32_t  digest_bits;
	uint32_t  b_cnt;
	struct tf_ctx_512 tf;
	uint8_t   b[64];
};

void skein_new_type(struct skein_ctx *ctx, uint64_t type);

void skein_process_block(struct skein_ctx *ctx, const uint8_t *blk_ptr,
				uint32_t blk_cnt, uint32_t byte_len);

void skein_rand_seed(struct skein_ctx *ctx, uint8_t *seed,
					uint32_t seed_bytes);

void skein_rand(struct skein_ctx *ctx, uint32_t request_bytes, uint8_t *out);

void skein_init(struct skein_ctx *ctx, uint32_t digest_bits,
			const uint8_t *key, uint32_t key_len);

void skein_update(struct skein_ctx *ctx, const uint8_t *msg,
					uint32_t msg_len);

void skein_final(struct skein_ctx *ctx, uint8_t *result, int output);

uint32_t skein_output(struct skein_ctx *ctx, uint8_t *result,
			uint32_t digest_size, uint32_t count);

/* "Internal" Skein definitions */
#define KEY        (0)
#define NONCE      (0x5400000000000000ULL)
#define MSG        (0x7000000000000000ULL)
#define CFG_FINAL  (0xc400000000000000ULL)
#define OUT_FINAL  (0xff00000000000000ULL)

#define SKEIN_SCHEMA_VER        (0x133414853ULL)
#define SKEIN_KS_PARITY         (0x1BD11BDAA9FC1A22ULL)

#endif  /* ifndef _SKEIN_H_ */
