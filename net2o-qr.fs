\ net2o QR code

\ Copyright (C) 2015   Bernd Paysan

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

require net2o-tools.fs

\ unicode characters to display a color matrix

'▀' Constant upper-half-block
\ '▄' Constant lower-half-block
\ '█' Constant solid-block

\ tags

0
enum qr#ownkey
enum qr#key
enum qr#keysig
enum qr#hash
enum qr#sync   \ sychnronizing info: key+secret
drop

\ constants

24 Constant keyqr# \ key qr codes are 24x24 blocks
keyqr# dup * Constant keyqr#²
$40 Constant keymax#
4 Constant keyline#
8 Constant keylineskp#

keyqr#² buffer: keyqr

\ : half-blocks ( n -- ) 0 ?DO  upper-half-block xemit  LOOP ;
: .prelines ( -- )
    rows keyqr# 2/ - 2/ 0 ?DO
	\ [ red >fg green >bg or ]L attr!
	<black> cols spaces <default> cr  LOOP ;
: .preline ( -- )
    \ [ red >fg green >bg or ]L attr!
    <black> cols keyqr# - 2/ spaces ;
: qr.2lines ( addr u -- ) .preline
    tuck bounds ?DO
	I c@ over I + c@ $F xor >bg swap $F xor >fg or attr!
	upper-half-block xemit
    LOOP  drop .preline ;
: qr.block ( addr u -- ) .prelines
    tuck dup * bounds ?DO
	I over qr.2lines <default> cr
    dup 2* +LOOP  drop .prelines ;

: 4xc! ( c addr -- )
    2dup c! 2dup 1+ c!  keyqr# +
    2dup c! 1+ c! ;

: >keyframe ( -- )  keyqr keyqr#² erase
    $04 keyqr 4xc!
    $05 keyqr keyqr# + 2 - 4xc!
    $06 keyqr keyqr#² + keyqr# 2* - 4xc!
    $07 keyqr keyqr#² + keyqr# - 2 - 4xc! ;
: byte>hpixel ( byte addr -- )
    \ a byte is converted into four pixels:
    \ MSB green red | green red | green red | green red LSB
    over 6 rshift over c! 1+
    over 4 rshift 3 and over c! 1+
    over 2 rshift 3 and over c! 1+
    swap 3 and swap c! ;
: byte>vpixel ( byte addr -- )
    \ a byte is converted into four pixels:
    \ MSB green red | green red | green red | green red LSB
    over 6 rshift       over c! keyqr# +
    over 4 rshift 3 and over c! keyqr# +
    over 2 rshift 3 and over c! keyqr# +
    swap          3 and swap c! ;

: >keyhline ( destaddr srcaddr -- destaddr' )
    keyline# bounds ?DO  I c@ over byte>hpixel 4 +  LOOP ;
: >keyvline ( destaddr srcaddr -- destaddr' )
    keyline# bounds ?DO  I c@ over byte>vpixel [ keyqr# 4 * ]L +  LOOP ;
: >keylines ( addr u -- )
    keyqr [ keyqr# 1+ 2* 2* ]L + -rot keymax# umin bounds ?DO
	I >keyhline  keylineskp# +
    keyline# +LOOP  drop ;

\ generate checksum and tag bits

: rng>qr-key ( -- )  $8 rng$ qr-key keysize move-rep ;
: date>qr-key ( -- )  sigdate $8 qr-key keysize move-rep ;
: taghash-rest ( addr1 u1 addrchallenge u2 tag -- tag )  >r
    c:0key $8 umin hashtmp $8 smove r@ hashtmp $8 + c!
    hashtmp $9 c:shorthash c:shorthash hashtmp $8 + $8 c:hash@ r>
    msg( ) ." ecc= " hashtmp $10 xtype space dup hex. cr ( ) ;
: >taghash ( addr u tag -- tag )
    qr-key $8 rot taghash-rest ;
: taghash? ( addr u1 ecc u2 tag -- flag )
    >r 2tuck r> taghash-rest drop 8 /string hashtmp 8 + 8 str= ;
: >ecc ( addr u tag -- ) >taghash
    keyqr [ keyqr# #03 *  #4 + ]L +  hashtmp      >keyhline drop
    keyqr [ keyqr# #20 *  #4 + ]L +  hashtmp $4 + >keyhline drop
    keyqr [ keyqr# #04 *  #3 + ]L +  hashtmp $8 + >keyvline drop
    keyqr [ keyqr# #04 * #20 + ]L +  hashtmp $C + >keyvline drop
    dup 6 rshift       keyqr [ keyqr#  #3 *  #3 + ]L + c!
    dup 4 rshift 3 and keyqr [ keyqr#  #3 * #20 + ]L + c!
    dup 2 rshift 3 and keyqr [ keyqr# #20 *  #3 + ]L + c!
    ( )          3 and keyqr [ keyqr# #20 * #20 + ]L + c! ;

: .qr-rest ( addr u tag -- )
    >r >keyframe 2dup >keylines r> >ecc
    keyqr keyqr# qr.block ;

: .keyqr ( addr u tag -- ) \ 64 bytes
    rng>qr-key .qr-rest ;

: .sigqr ( addr u -- ) \ any string
    c:0key c:hash now>never sigdate +date
    sig-params ed-sign
    date>qr-key qr#keysig .qr-rest ;

[IFDEF] android   require android/qrscan.fs  [THEN]

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]