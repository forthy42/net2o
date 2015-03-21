\ threefish wrapper

require unix/mmap.fs
require unix/pthread.fs
require 64bit.fs
require crypto-api.fs
require net2o-err.fs
require kregion.fs

\ dummy load for Android
[IFDEF] android
    s" /data/data/gnu.gforth/lib/libthreefish.so" open-lib drop
[THEN]

c-library threefish
    s" threefish" add-lib
    [IFDEF] android
	s" ./threefish" add-libpath
    [THEN]
    \c #include <threefish.h>
    \c void tf_encrypt_loop(struct tf_ctx *ctx, uint64_t *p, size_t n,
    \c 			    int flags1, int flags2) {
    \c   int flags=flags1;
    \c   while(n>=64) {
    \c     tf_encrypt(ctx, p, p, flags);
    \c     flags=flags2;
    \c     p+=8;
    \c     n-=64;
    \c     if(!++(ctx->tweak[0]))
    \c       (ctx->tweak[1])++;
    \c   }
    \c }
    \c void tf_decrypt_loop(struct tf_ctx *ctx, uint64_t *c, size_t n,
    \c 			    int flags1, int flags2) {
    \c   int flags=flags1;
    \c   while(n>=64) {
    \c     tf_decrypt(ctx, c, c, flags);
    \c     flags=flags2;
    \c     c+=8;
    \c     n-=64;
    \c     if(!++(ctx->tweak[0]))
    \c       (ctx->tweak[1])++;
    \c   }
    \c }
\ -------===< structs >===--------
\ tf_ctx
begin-structure tf_ctx
        drop 0 72 +field tf_ctx-key
        drop 72 24 +field tf_ctx-tweak
drop 96 end-structure

\ ------===< functions >===-------
c-function tf_encrypt tf_encrypt a a a n -- void
c-function tf_decrypt tf_decrypt a a a n -- void
c-function tf_encrypt_loop tf_encrypt_loop a a n n n -- void
c-function tf_decrypt_loop tf_decrypt_loop a a n n n -- void

end-c-library

UValue @threefish

$40 Constant threefish#max

\ crypto api integration

crypto class
    tf_ctx uvar threefish-state
    threefish#max uvar threefish-padded
end-class threefish

User threefish-t

: threefish-init ( -- )
    threefish-t @ dup crypto-o ! IF  crypto-up @ up@ = ?EXIT  THEN
    [: threefish new dup crypto-o ! threefish-t ! ;] crypto-a with-allocater
    up@ crypto-up ! threefish-state to @threefish ;

: threefish-free crypto-o @ ?dup-IF  .dispose  THEN
    0 to @threefish crypto-o off ;

: threefish0 ( -- )  threefish-state tf_ctx erase ;
: >threefish ( addr u -- )  threefish-state swap move
    threefish-state tf_ctx-tweak sizeof tf_ctx-tweak erase ;
: threefish> ( addr u -- )  threefish-state -rot move ;
: +threefish ( -- )
    threefish-state tf_ctx-tweak $10 bounds DO
	1 I +! I @ ?LEAVE \ continue when wraparound
    cell +LOOP  ;
: tf-tweak! ( 128b -- )
    threefish-state tf_ctx-tweak $10 erase
    threefish-state tf_ctx-tweak 128! ;

threefish-init

' threefish-init to c:init
' threefish-free to c:free

' @threefish to c:key@ ( -- addr )
\G obtain the key storage
' tf_ctx to c:key# ( -- n )
\G obtain key storage size
' threefish0 to c:0key ( -- )
\G set zero key
:noname threefish0 dup threefish#max >threefish
    threefish-state swap threefish#max + threefish#max $E dup tf_encrypt_loop
    64#0 64dup tf-tweak! ; to >c:key ( addr -- )
\G move 128 bytes from addr to the key
:noname threefish#max threefish> ; to c:key> ( addr -- )
\G get 64 bytes from the key to addr
' noop to c:diffuse ( -- ) \ no diffusing
:noname ( addr u -- )
    \G Encrypt message in buffer addr u, must be by *64
    $C >r
    BEGIN  dup threefish#max u>=  WHILE
	    over >r threefish-state r> dup r> tf_encrypt
	    threefish#max /string +threefish 4 >r
    REPEAT  2drop rdrop
; to c:encrypt
:noname ( addr u -- )
\G Fill buffer addr u with PRNG sequence
    2>r threefish-state 2r> $E dup tf_encrypt_loop
; to c:prng
:noname ( addr u tag -- )
    \G Encrypt message in buffer addr u, must be by *64
    \G authentication is stored in the 16 bytes following that buffer
    { tag }
    2>r threefish-state 2r@ $E dup tf_encrypt_loop
    threefish-padded threefish#max erase
    $80 tag + threefish-state tf_ctx-tweak $F + c! \ last block flag
    threefish-state threefish-padded dup $E tf_encrypt
    threefish-padded 128@ 2r> + 128!
; to c:encrypt+auth
:noname ( addr u -- )
    \G Decrypt message in buffer addr u, must be by *64
    $C >r
    BEGIN  dup threefish#max u>=  WHILE
	    over >r threefish-state r> dup r> tf_decrypt
	    threefish#max /string +threefish 4 >r
    REPEAT  2drop rdrop
; to c:decrypt
:noname ( addr u tag -- flag )
    \G Decrypt message in buffer addr u, must be by *64
    { tag }
    2>r threefish-state 2r@ $E dup tf_decrypt_loop
    threefish-padded threefish#max erase
    $80 tag + threefish-state tf_ctx-tweak $F + c! \ last block flag
    threefish-state threefish-padded dup $E tf_encrypt
    2r> + 128@ threefish-padded 128@ 128=
; to c:decrypt+auth
:noname ( addr u -- )
    \G Hash message in buffer addr u
    BEGIN  2dup threefish#max umin tuck
	dup threefish#max u< IF
	    threefish-padded threefish#max >padded
	    threefish-padded threefish#max
	THEN  drop threefish-state swap threefish-state $D tf_encrypt
    +threefish /string dup 0= UNTIL  2drop
; to c:hash
' tf-tweak! to c:tweak! ( 128b -- )
\G set tweek

crypto-o @ Constant threefish-o
