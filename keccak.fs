\ keccak wrapper

\ Copyright © 2012-2015   Bernd Paysan

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

[IFDEF] old-keccak
    fast-lib [IF]
	require keccakfast.fs false
    [ELSE]
	[IFDEF] android
	    s" libkeccakp.so" c-lib:open-path-lib drop
	[THEN] true
    [THEN]
    [IF]
	c-library keccak
	    s" keccakp" add-lib
	    \	s" keccakp/.libs" add-libpath \ find library during build
	    include keccaklib.fs
	end-c-library
    [THEN]
[ELSE]
    require keccaklow.fs
[THEN]

[IFUNDEF] crypto bye [THEN] \ stop here if libcompile only

\ crypto api integration

25 8 * Constant keccak#
128 Constant keccak#max
128 Constant keccak#cks

crypto class
    keccak# uvar keccak-state
    keccak#cks uvar keccak-checksums
    keccak#max uvar keccak-padded
    cell uvar keccak-rounds
end-class keccak

UValue @keccak

[IFDEF] old-keccak
: keccak0 ( -- ) @keccak KeccakInitializeState ;

: keccak* ( -- ) @keccak keccak-rounds @ KeccakF ;
: >keccak ( addr u -- )  @keccak -rot KeccakAbsorb ;
: +keccak ( addr u -- )  @keccak -rot KeccakEncrypt ;
: -keccak ( addr u -- )  @keccak -rot KeccakDecrypt ;
: keccak> ( addr u -- )  @keccak -rot KeccakExtract ;
[ELSE]
: keccak0 ( -- ) @keccak KeccakP1600_Initialize ;
: KeccakF ( state-addr rounds -- ) KeccakP1600_Permute_Nrounds ;
: keccak* ( -- ) @keccak keccak-rounds @ KeccakP1600_Permute_Nrounds ;
: >keccak ( addr u -- )  @keccak -rot 0 swap KeccakP1600_AddBytes ;
: keccak> ( addr u -- )  @keccak -rot 0 swap KeccakP1600_ExtractBytes ;
: KeccakEncrypt { state addr u -- }
    state addr 0 u KeccakP1600_AddBytes
    state addr 0 u KeccakP1600_ExtractBytes ;
: KeccakDecrypt { state addr u | kbuf[ $88 ] -- }
    state kbuf[ 0 $80 KeccakP1600_ExtractBytes
    state addr 0 u KeccakP1600_OverwriteBytes
    [ machine "amd64" str= machine "arm64" str= or
    machine "386" str= or machine "arm" str= or ] [IF]
	\ machine supports unaligned accesses
	kbuf[ addr u xor_lanes
    [ELSE]
	addr cell 1 - and 0= IF \ access is aligned
	    kbuf[ addr u xor_lanes
	ELSE \ we do it with known unaligned accesses
	    -1 u cell 1 - and dfloats lshift invert
	    [ cell 8 = ] [IF] xle [ELSE] lle [THEN]
	    kbuf[ u -1 cells and + and!
	    kbuf[ addr u bounds ?DO
		[ cell 8 = ] [IF]
		    dup @ I x@ xor I x! cell+
		[ELSE]
		    dup @ I l@ xor I l! cell+
		[THEN]
	    cell +LOOP drop
	THEN
    [THEN] ;
: +keccak ( addr u -- )  @keccak -rot KeccakEncrypt ;
: -keccak ( addr u -- )  @keccak -rot KeccakDecrypt ;

: KeccakEncryptLoop ( state addr u rounds -- )
    { rounds }
    bounds over >r ?DO
	dup rounds KeccakP1600_Permute_Nrounds
	dup I delta-I $80 umin KeccakEncrypt
    $80 +LOOP drop r> ;
: KeccakDecryptLoop ( state addr u rounds -- )
    { rounds }
    bounds over >r ?DO
	dup rounds KeccakP1600_Permute_Nrounds
	dup I delta-I $80 umin KeccakDecrypt
    $80 +LOOP drop r> ;
[THEN]

: move-rep ( srcaddr u1 destaddr u2 -- )
    bounds ?DO
	delta-I umin 2dup I swap move
    dup +LOOP  2drop ;

User keccak-t
0 Value keccak-o

User keyak-t
0 Value keyak-o

: keccak-init ( -- )
    keccak-t @ dup crypto-o ! IF  crypto-up @ up@ = ?EXIT  THEN
    [: keccak new dup crypto-o ! #24 keccak-rounds ! keccak-t ! ;] crypto-a with-allocater
    up@ crypto-up ! keccak-state to @keccak
    crypto-o @ to keccak-o ;

: keccak-free
    keccak-t @ ?dup-IF  [: .dispose ;] crypto-a with-allocater  THEN
    0 to @keccak crypto-o off  keccak-t off ;

keccak-init

' keccak-init is c:init
' keccak-free is c:free
:noname to @keccak ; is c:key! ( addr -- )
\G use addr as key storage
' @keccak is c:key@ ( -- addr )
\G obtain the key storage
' keccak# is c:key# ( -- n )
\G obtain key storage size
' keccak0 is c:0key ( -- )
\G set zero key
:noname keccak0 keccak#max >keccak ; is >c:key ( addr -- )
\G move 128 bytes from addr to the state
:noname keccak#max keccak> ; is c:key> ( addr -- )
\G get 128 bytes from the state to addr
' keccak* is c:diffuse ( -- )
\G perform a diffuse round
:noname ( addr u -- )
    \G Encrypt message in buffer addr u
    @keccak -rot keccak-rounds @ KeccakEncryptLoop  drop
; is c:encrypt
:noname ( addr u -- )
    \G Decrypt message in buffer addr u
    @keccak -rot keccak-rounds @ KeccakDecryptLoop  drop
; is c:decrypt ( addr u -- )
:noname ( addr u tag -- )
    \G Encrypt message in buffer addr u with auth
    { tag } @keccak -rot keccak-rounds @ KeccakEncryptLoop
    keccak*
    >r keccak-checksums keccak#cks keccak>
    keccak-checksums tag 7 and 4 lshift + r> $10 move
; is c:encrypt+auth ( addr u tag -- )
:noname ( addr u tag -- flag )
    \G Decrypt message in buffer addr u, with auth check
    { tag } @keccak -rot keccak-rounds @ KeccakDecryptLoop
    keccak*
    keccak-checksums keccak#cks keccak>
    keccak-checksums tag 7 and 4 lshift + $10 tuck str=
; is c:decrypt+auth ( addr u tag -- flag )
:noname ( addr u -- )
\G Hash message in buffer addr u
    BEGIN  2dup keccak#max umin tuck
	dup keccak#max u< IF
	    keccak-padded keccak#max >padded
	    keccak-padded keccak#max
	THEN  >keccak  keccak*
    /string dup 0= UNTIL  2drop
; is c:hash
:noname ( addr u -- )
\G Fill buffer addr u with PRNG sequence
    2dup erase @keccak -rot keccak-rounds @ KeccakEncryptLoop drop
; is c:prng
:noname ( addr u -- ) >keccak keccak* ;
\G absorb + hash for a message <= 64 bytes
is c:shorthash
' keccak> ( addr u -- )
    \G extract short hash (up to 64 bytes)
is c:hash@
:noname ( x128 addr u -- )
    \G set key plus tweak
    keccak-padded keccak#max dup 2/ /string move-rep
    keccak-padded keccak#max 2/ bounds DO
	64over 64over I le-128!  $10 +LOOP  64drop 64drop
    keccak0 keccak-padded keccak#max >keccak ;
is c:tweakkey!

\ 12 rounds variation

keccak class
end-class keyak

: keyak-init ( -- )
    keyak-t @ dup crypto-o ! IF  crypto-up @ up@ = ?EXIT  THEN
    [: keyak new dup crypto-o ! #12 keccak-rounds ! keyak-t ! ;] crypto-a with-allocater
    up@ crypto-up ! keccak-state to @keccak
    crypto-o @ to keyak-o ;

: keyak-free
    keyak-t @ ?dup-IF  [: .dispose ;] crypto-a with-allocater  THEN
    0 to @keccak crypto-o off  keyak-t off ;

keyak-init

' keyak-init is c:init
' keyak-free is c:free

:noname defers 'cold keccak-init keyak-init ; is 'cold
:noname defers 'image crypto-o off  keccak-t off  keyak-t off
    0 to keccak-o  0 to keyak-o ; is 'image
