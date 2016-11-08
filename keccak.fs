\ keccak wrapper

\ Copyright (C) 2012-2015   Bernd Paysan

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
    require keccakfast.fs false
[ELSE]
    [IFDEF] android
	s" libkeccak.so" c-lib:open-path-lib drop
    [THEN] true
[THEN]
[IF]
    c-library keccak
	s" keccak" add-lib
	include keccaklib.fs
    end-c-library
[THEN]

[IFUNDEF] crypto bye [THEN] \ stop here if libcompile only

25 8 * Constant keccak#
128 Constant keccak#max
128 Constant keccak#cks

UValue @keccak
24 Value rounds

: keccak0 ( -- ) @keccak KeccakInitializeState ;

: keccak* ( -- ) @keccak rounds KeccakF ;
: >keccak ( addr u -- )  @keccak -rot KeccakAbsorb ;
: +keccak ( addr u -- )  @keccak -rot KeccakEncrypt ;
: -keccak ( addr u -- )  @keccak -rot KeccakDecrypt ;
: keccak> ( addr u -- )  @keccak -rot KeccakExtract ;

: move-rep ( srcaddr u1 destaddr u2 -- )
    bounds ?DO
	I' I - umin 2dup I swap move
    dup +LOOP  2drop ;

\ crypto api integration

crypto class
    keccak# uvar keccak-state
    keccak#cks uvar keccak-checksums
    keccak#max uvar keccak-padded
end-class keccak

User keccak-t

: keccak-init ( -- )
    keccak-t @ dup crypto-o ! IF  crypto-up @ up@ = ?EXIT  THEN
    [: keccak new dup crypto-o ! keccak-t ! ;] crypto-a with-allocater
    up@ crypto-up ! keccak-state to @keccak ;

: keccak-free crypto-o @ ?dup-IF  [: .dispose ;] crypto-a with-allocater  THEN
    0 to @keccak crypto-o off ;

keccak-init

:noname defers 'cold keccak-init ; is 'cold
:noname defers 'image crypto-o off  keccak-t off ; is 'image

' keccak-init to c:init
' keccak-free to c:free
:noname to @keccak ; to c:key! ( addr -- )
\G use addr as key storage
' @keccak to c:key@ ( -- addr )
\G obtain the key storage
' keccak# to c:key# ( -- n )
\G obtain key storage size
' keccak0 to c:0key ( -- )
\G set zero key
:noname keccak0 keccak#max >keccak ; to >c:key ( addr -- )
\G move 128 bytes from addr to the state
:noname keccak#max keccak> ; to c:key> ( addr -- )
\G get 128 bytes from the state to addr
' keccak* to c:diffuse ( -- )
\G perform a diffuse round
:noname ( addr u -- )
    \G Encrypt message in buffer addr u
    @keccak -rot rounds KeccakEncryptLoop  drop
; to c:encrypt
:noname ( addr u -- )
    \G Decrypt message in buffer addr u
    @keccak -rot rounds KeccakDecryptLoop  drop
; to c:decrypt ( addr u -- )
:noname ( addr u tag -- )
    \G Encrypt message in buffer addr u with auth
    { tag } @keccak -rot rounds KeccakEncryptLoop
    keccak*
    >r keccak-checksums keccak#cks keccak>
    keccak-checksums tag 7 and 4 lshift + r> $10 move
; to c:encrypt+auth ( addr u tag -- )
:noname ( addr u tag -- flag )
    \G Decrypt message in buffer addr u, with auth check
    { tag } @keccak -rot rounds KeccakDecryptLoop
    keccak*
    keccak-checksums keccak#cks keccak>
    keccak-checksums tag 7 and 4 lshift + $10 tuck str=
; to c:decrypt+auth ( addr u tag -- flag )
:noname ( addr u -- )
\G Hash message in buffer addr u
    BEGIN  2dup keccak#max umin tuck
	dup keccak#max u< IF
	    keccak-padded keccak#max >padded
	    keccak-padded keccak#max
	THEN  >keccak  keccak*
    /string dup 0= UNTIL  2drop
; to c:hash
:noname ( addr u -- )
\G Fill buffer addr u with PRNG sequence
    2dup erase @keccak -rot rounds KeccakEncryptLoop drop
; to c:prng
:noname ( addr u -- ) >keccak keccak* ;
\G absorb + hash for a message <= 64 bytes
to c:shorthash
' keccak> ( addr u -- )
    \G extract short hash (up to 64 bytes)
to c:hash@
:noname ( x128 addr u -- )
    \G set key plus tweak
    keccak-padded keccak#max dup 2/ /string move-rep
    keccak-padded keccak#max 2/ bounds DO
	64over 64over I le-128!  $10 +LOOP  64drop 64drop
    keccak0 keccak-padded keccak#max >keccak ;
to c:tweakkey!
    
crypto-o @ Constant keccak-o
