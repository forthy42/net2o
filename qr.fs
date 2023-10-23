\ net2o QR code

\ Copyright © 2015   Bernd Paysan

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

require tools.fs

\ unicode characters to display a color matrix

e? max-xchar $100 < [IF] '^' [ELSE] '▀' [THEN] Constant upper-half-block
\ '▄' Constant lower-half-block
\ '█' Constant solid-block

\ constants

24 Constant keyqr# \ key qr codes are 24x24 blocks
keyqr# dup * Constant keyqr#² \ code block size
$40 Constant keymax#
4 Constant keyline#
8 Constant keylineskp#

keyqr#² buffer: keyqr \ code block buffer
keyqr#² sfloats buffer: keyqr-rgba \ code block in RGBA
$10 buffer: qrecc

Defer <rest>  ' <white> is <rest>
$8 Value 2b>col

: white-qr ( -- )
    ['] <white> is <rest>
    $8 to 2b>col ;
: black-qr ( -- )
    ['] <black> is <rest>
    $F to 2b>col ;

\ : half-blocks ( n -- ) 0 ?DO  upper-half-block xemit  LOOP ;
\ : blocks ( n -- ) 0 U+DO solid-block xemit LOOP ;
: .prelines ( -- )
    rows keyqr# 2/ - 2/ 0 ?DO
	\ [ red >fg green >bg or ]L attr!
	<rest> cols spaces <default> cr  LOOP ;
: .preline ( -- )
    \ [ red >fg green >bg or ]L attr!
    <rest> cols keyqr# - 2/ spaces ;
: qr.2lines ( addr u -- ) .preline
    tuck bounds ?DO
	I c@ over I + c@ 2b>col xor >bg swap 2b>col xor >fg or attr!
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
    $04 [ keyqr                        ]L 4xc!
    $05 [ keyqr keyqr# + 2 -           ]L 4xc!
    $06 [ keyqr keyqr#² + keyqr# 2* -  ]L 4xc!
    $07 [ keyqr keyqr#² + keyqr# - 2 - ]L 4xc! ;
: byte>pixel ( byte addr dist -- )
    \ a byte is converted into four pixels:
    \ MSB green red | green red | green red | green red LSB
    >r
    over 6 rshift       over c! r@ +
    over 4 rshift 3 and over c! r@ +
    over 2 rshift 3 and over c! r> +
    swap          3 and swap c! ;
: byte>hpixel ( byte addr -- )
    \ a byte is converted into four pixels:
    \ MSB green red | green red | green red | green red LSB
    1 byte>pixel ;
: byte>vpixel ( byte addr -- )
    \ a byte is converted into four pixels:
    \ MSB green red | green red | green red | green red LSB
    keyqr# byte>pixel ;

: >keyhline ( destaddr srcaddr -- destaddr' )
    keyline# bounds ?DO  I c@ over byte>hpixel 4 +  LOOP ;
: >keyvline ( destaddr srcaddr -- destaddr' )
    keyline# bounds ?DO  I c@ over byte>vpixel [ keyqr# 4 * ]L +  LOOP ;
: >keylines ( addr u -- )
    keyqr [ keyqr# 1+ 2* 2* ]L + -rot keymax# umin bounds ?DO
	I >keyhline  keylineskp# +
    keyline# +LOOP  drop ;

\ qr to RGBA

Create >rgba
$00000000 ,
$FF000000 ,
$00FF0000 ,
$FFFF0000 ,
$0000FF00 ,
$FF00FF00 ,
$00FFFF00 ,
$FFFFFF00 ,
$000000FF ,
$FF0000FF ,
$00FF00FF ,
$FFFF00FF ,
$0000FFFF ,
$FF00FFFF ,
$00FFFFFF ,
$FFFFFFFF ,

: qr>rgba ( -- )
    keyqr-rgba keyqr keyqr#² bounds DO
	I c@ 2b>col xor 7 xor cells >rgba + @ lbe over l! sfloat+
    LOOP drop ;

\ generate checksum and tag bits

: >qr-key ( addr u -- ) qr-key keysize move-rep ;
: rng>qr-key ( -- )  $8 rng$ >qr-key qr( qr-key 8 xtype cr ) ;
: date>qr-key ( -- )  sigdate $8 >qr-key ;
: taghash-rest ( addr1 u1 addrchallenge u2 tag -- tag )  >r
    c:0key $8 umin qrecc $8 smove r@ qrecc $8 + c!
    qrecc $9 c:shorthash c:shorthash qrecc $8 + $8 c:hash@ r>
    msg( ." ecc= " qrecc $10 xtype space dup h. cr ) ;
: >taghash ( addr u tag -- tag )
    qr-key $8 rot taghash-rest ;
: taghash? ( addr u1 ecc u2 tag -- flag )
    >r 2tuck over $8 >qr-key
    r> taghash-rest drop 8 /string qrecc 8 + 8 str=
    qr( dup IF  ." ecc= " qrecc $10 xtype cr  THEN ) ;
: >ecc ( addr u tag -- ) >taghash
    qr( ." ecc: " qrecc $10 xtype cr )
    keyqr [ keyqr# #03 *  #4 + ]L +  qrecc      >keyhline drop
    keyqr [ keyqr# #20 *  #4 + ]L +  qrecc $4 + >keyhline drop
    keyqr [ keyqr# #04 *  #3 + ]L +  qrecc $8 + >keyvline drop
    keyqr [ keyqr# #04 * #20 + ]L +  qrecc $C + >keyvline drop
    dup 6 rshift       keyqr [ keyqr#  #3 *  #3 + ]L + c!
    dup 4 rshift 3 and keyqr [ keyqr#  #3 * #20 + ]L + c!
    dup 2 rshift 3 and keyqr [ keyqr# #20 *  #3 + ]L + c!
    ( )          3 and keyqr [ keyqr# #20 * #20 + ]L + c! ;

: .qr-rest ( addr u tag -- )
    >r >keyframe 2dup >keylines r> >ecc
    keyqr keyqr# qr.block ;

: .keyqr ( addr u tag -- ) \ 64 bytes
    qr( >r 2dup bounds U+DO ." qr : " I $10 xtype cr $10 +LOOP
    r> ." tag: " dup h. cr )
    rng>qr-key .qr-rest ;

: .sigqr ( addr u -- ) \ any string
    c:0key c:hash now>never sigdate +date
    sig-params ed-sign
    date>qr-key qr:keysig# .qr-rest ;

\\\
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
