\ threefish wrapper

\ Copyright (C) 2015,2018   Bernd Paysan

\ This program is free software: you can redistribute it and/or modify
\ it under the terms of the GNU Affero General Public License as published by
\ the Free Software Foundation, either version 3 of the License, or
\ (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU Affero General Public License for more details.

\ You should have received a copy of the GNU Affero General Public License
\ along with this program.  If not, see <http://www.gnu.org/licenses/>.

\c #include <threefish.h>
\c void tf_encrypt_loop(struct tf_ctx_512 *ctx, uint64_t *p, size_t n,
\c 			    int flags1, int flags2) {
\c   int flags=flags1;
\c   while(n>=64) {
\c     tf_encrypt_512(ctx, p, p, flags);
\c     flags=flags2; p+=8; n-=64;
\c     ctx->tweak[1] += !++(ctx->tweak[0]);
\c   }
\c }
\c void tf_decrypt_loop(struct tf_ctx_512 *ctx, uint64_t *c, size_t n,
\c 			    int flags1, int flags2) {
\c   int flags=flags1;
\c   while(n>=64) {
\c     tf_decrypt_512(ctx, c, c, flags);
\c     flags=flags2; c+=8; n-=64;
\c     ctx->tweak[1] += !++(ctx->tweak[0]);
\c   }
\c }
\c void tf_tweak256_pp(struct tf_ctx_256 *ctx)
\c {
\c   ctx->tweak[1] += !++(ctx->tweak[0]);
\c }
\c void tf_tweak512_pp(struct tf_ctx_512 *ctx)
\c {
\c   ctx->tweak[1] += !++(ctx->tweak[0]);
\c }
\ -------===< structs >===--------
\ tf_ctx_256
begin-structure tf_ctx_256
    drop 0 40 +field tf_ctx_256-key
    drop 40 24 +field tf_ctx_256-tweak
    drop 64 end-structure
\ tf_ctx
begin-structure tf_ctx
    drop 0 72 +field tf_ctx-key
    drop 72 24 +field tf_ctx-tweak
    drop 96 end-structure

\ ------===< functions >===-------
c-function tf_encrypt tf_encrypt_512 a a a n -- void
c-function tf_decrypt tf_decrypt_512 a a a n -- void
c-function tf_encrypt_256 tf_encrypt_256 a a a n -- void
c-function tf_decrypt_256 tf_decrypt_256 a a a n -- void
c-function tf_encrypt_loop tf_encrypt_loop a a n n n -- void
c-function tf_decrypt_loop tf_decrypt_loop a a n n n -- void
c-function tf_tweak256++ tf_tweak256_pp a -- void
c-function tf_tweak512++ tf_tweak512_pp a -- void
