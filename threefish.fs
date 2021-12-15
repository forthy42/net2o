\ threefish wrapper

\ Copyright © 2015,2018   Bernd Paysan

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

require rec-scope.fs
require unix/cpu.fs

fast-lib [IF]
    require threefishfast.fs false
[ELSE]
    [IFDEF] android
	s" libthreefish.so" c-lib:open-path-lib drop
    [THEN] true
[THEN]
[IF]
    c-library threefish
	s" threefish" add-lib
\	s" threefish/.libs" add-libpath \ find library during build
	include threefishlib.fs
    end-c-library
[THEN]

[IFUNDEF] crypto bye [THEN] \ stop here if libcompile only

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
: >threefish ( addr u -- )  threefish-state threefish#max smove
    threefish-state tf_ctx-tweak sizeof tf_ctx-tweak erase ;
: threefish> ( addr u -- )  threefish-state -rot threefish#max umin move ;
: +threefish ( -- )
    threefish-state tf_ctx-tweak $10 bounds DO
	1 I +! I @ ?LEAVE \ continue when wraparound
    cell +LOOP  ;
: tf-tweak! ( x128 -- )
    threefish-state tf_ctx-tweak le-128! ;

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
    threefish#max + threefish-padded threefish#max move
    threefish-state threefish-padded threefish#max $E dup tf_encrypt_loop
    64#0 64dup tf-tweak! ; to >c:key ( addr -- )
\G move 128 bytes from addr to the key
:noname threefish#max threefish> ; to c:key> ( addr -- )
\G get 64 bytes from the key to addr
:noname ( -- )
    threefish-padded threefish#max erase
    threefish-state threefish-padded threefish#max $E dup tf_encrypt_loop
; to c:diffuse ( -- ) \ no diffusing
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
    $80 tag + threefish-state tf_ctx-tweak $F + c! \ last block flag
    c:diffuse
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
    2r> + threefish-padded $10 tuck str=
; to c:decrypt+auth
:noname ( addr u -- )
    \G Hash message in buffer addr u
    BEGIN  2dup threefish#max umin tuck
	dup threefish#max u< IF
	    threefish-padded threefish#max >padded
	    threefish-padded threefish#max
	THEN  drop threefish-state swap over $D tf_encrypt
    +threefish /string dup 0= UNTIL  2drop
; to c:hash
:noname ( addr u -- )
\G absorb + hash for a message <= 64 bytes
    threefish-padded threefish#max >padded
    threefish-state threefish-padded over $D tf_encrypt
; to c:shorthash
' threefish> to c:hash@
\G extract short hash (up to 64 bytes)
:noname ( x128 addr u -- )
    \G set key plus tweak
    threefish#max umin threefish-state swap move
    tf-tweak! ;
to c:tweakkey!

crypto-o @ Constant threefish-o

