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
\ -------===< structs >===--------
\ tf_ctx
begin-structure tf_ctx
        drop 0 72 +field tf_ctx-key
        drop 72 24 +field tf_ctx-tweak
drop 96 end-structure

\ ------===< functions >===-------
c-function tf_prep tf_prep a -- void
c-function tf_tweak tf_tweak a -- void
c-function tf_encrypt tf_encrypt a a a n -- void
c-function tf_decrypt tf_decrypt a a a -- void

end-c-library

UValue @threefish

$40 Constant threefish#max

\ crypto api integration

crypto class
    tf_ctx uvar threefish-state
    threefish#max uvar threefish-padded
    threefish#max uvar threefish-hash
    cell uvar threefish-up
end-class threefish

: threefish-init crypto-o @ IF  threefish-up @ next-task = ?EXIT  THEN
    [: threefish new crypto-o ! ;] crypto-a with-allocater
    next-task threefish-up ! threefish-state to @threefish ;

: threefish-free crypto-o @ ?dup-IF  .dispose  THEN
    0 to @threefish crypto-o off ;

: threefish0 ( -- )  threefish-state tf_ctx erase
    threefish-state tf_prep ;
: >threefish ( addr u -- )  threefish-state swap move
    threefish-state tf_ctx-tweak sizeof tf_ctx-tweak erase
    threefish-state tf_prep ;
: threefish> ( addr u -- )  threefish-state -rot move ;
: +threefish ( -- )
    threefish-state tf_ctx-tweak $10 bounds DO
	1 I +! I @ ?LEAVE \ continue when wraparound
    cell +LOOP  threefish-state tf_tweak ;
: tf-tweak! ( 128b -- )
    threefish-state tf_ctx-tweak $10 erase
    threefish-state tf_ctx-tweak 128!
    threefish-state tf_tweak ;

threefish-init

' threefish-init to c:init
' threefish-free to c:free

' @threefish to c:key@ ( -- addr )
\G obtain the key storage
' tf_ctx to c:key# ( -- n )
\G obtain key storage size
' threefish0 to c:0key ( -- )
\G set zero key
:noname threefish0 threefish#max >threefish ; to >c:key ( addr -- )
\G move 64 bytes from addr to the key
:noname threefish#max threefish> ; to c:key> ( addr -- )
\G get 64 bytes from the key to addr
' noop to c:diffuse ( -- ) \ no diffusing
:noname ( addr u -- )
    \G Encrypt message in buffer addr u, must be by *64
    BEGIN  dup threefish#max u>=  WHILE
	    over >r threefish-state r> dup 0 tf_encrypt
	    threefish#max /string +threefish
    REPEAT  2drop
; dup to c:encrypt
to c:prng
\G Fill buffer addr u with PRNG sequence
:noname ( addr u -- )
    \G Decrypt message in buffer addr u, must be by *64
   BEGIN  dup threefish#max u>=  WHILE
	   over >r threefish-state r> dup tf_decrypt
	   threefish#max /string +threefish
    REPEAT  2drop
; to c:decrypt
:noname ( addr u -- )
    \G Hash message in buffer addr u
    threefish-hash threefish#max erase
    BEGIN  2dup threefish#max umin tuck
	dup threefish#max u< IF
	    threefish-padded threefish#max >padded
	    threefish-padded threefish#max
	THEN  drop threefish-state swap threefish-hash 1 tf_encrypt
    +threefish /string dup 0= UNTIL  2drop
; to c:hash
' tf-tweak! to c:tweak! ( 128b -- )
\G set tweek

\G Fill buffer addr u with PRNG sequence

crypto-o @ Constant threefish-o