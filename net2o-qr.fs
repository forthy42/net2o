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
require ansi.fs

'▀' Constant upper-half-block
\ '▄' Constant lower-half-block
\ '█' Constant solid-block

20 Constant keyqr# \ key qr codes are 20x20 blocks
keyqr# dup * Constant keyqr#²
$40 Constant keymax#
  8 Constant keyline#

keyqr#² buffer: keyqr

: .prelines ( -- )
    rows keyqr# 2/ - 2/ 0 ?DO
	<black> cols spaces <default> cr  LOOP ;
: .preline ( -- )
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

: >keyframe ( -- )  keyqr keyqr#² erase
    $04 keyqr c!
    $05 keyqr keyqr# + 1- c!
    $06 keyqr keyqr#² + keyqr# - c!
    $07 keyqr keyqr#² + 1- c! ;
: byte>pixel ( byte addr -- )
    over 6 rshift over c! 1+
    over 4 rshift 3 and over c! keyqr# 1- +
    over 2 rshift 3 and over c! 1+
    swap 3 and swap c! ;

: >keylines ( addr u -- )
    keyqr keyqr# 1+ 2* + -rot keymax# umin bounds ?DO
	dup I keyline# bounds ?DO
	    I c@ over byte>pixel  2 +
	LOOP  drop
	[ keyqr# 2* ]L +
    keyline# +LOOP  drop ;

\ generate ECC

\ swap bit 0 and bit 1
Create 0.1-swap 0 c, 2 c, 1 c, 3 c, DOES> + c@ ;

: >ecc1 ( -- )
    keyqr keyqr# + keyqr# dup 2 - * bounds ?DO
	0 I 2 + keyqr# 4 - bounds ?DO
	i c@ xor  i 1+ c@ 0.1-swap xor  2 +LOOP  I 1 + c!
    keyqr# +LOOP ;
: >ecc2 ( -- )
    keyqr keyqr# + keyqr# dup 2 - * bounds ?DO
	0 I 2 + keyqr# 4 - bounds ?DO  i c@ xor  LOOP  I keyqr# 2 - + c!
    keyqr# +LOOP ;
: >ecc3 ( -- )
    keyqr keyqr# + 1+ keyqr# 2 - bounds ?DO
	0 I keyqr# + keyqr#² keyqr# 4 * - bounds ?DO
	i c@ xor  i keyqr# + c@ 0.1-swap xor  keyqr# 2* +LOOP  I c!
    LOOP ;
: >ecc4 ( -- )
    keyqr keyqr# + 1+ keyqr# 2 - bounds ?DO
	0 I keyqr# + keyqr#² keyqr# 3 * - bounds ?DO
	i c@ xor  keyqr# +LOOP	I keyqr#² keyqr# 3 * - + c!
    LOOP ;

: >ecc ( addr u -- ) >ecc1 >ecc2 >ecc3 >ecc4 ;

: .keyqr ( addr u -- ) \ 64 bytes
    >keyframe >keylines >ecc keyqr keyqr# qr.block ;

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