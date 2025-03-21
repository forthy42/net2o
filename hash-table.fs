\ hash table

\ Copyright © 2010-2021   Bernd Paysan

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

\ generic hash table functions

2 64s buffer: hashinit

:is 'image defers 'image hashinit 2 64s erase ;

\ this computes a cryptographic somewhat secure hash over the input string

User hash-state 2 64s cell- uallot drop

: string-hash ( addr u -- )  hashinit hash-state [ 2 64s ]L move
    false hash-state hashkey2 ;

: hash$ ( -- addr u )  hash-state [ 2 64s ]L ;

\ hierarchical hash table

\ hash tables store key,value-pairs.
\ Each hierarchy uses one byte of state as index (only lower 7 bits)
\ if there is a collission, add another indirection

uvalue last#

: #!? ( addrval u addrkey u bucket -- true / addrval u addrkey u false )
    dup >r @ 0= IF  r@ $! r@ cell+ $!  r> to last#
	true  EXIT  THEN
    2dup r@ $@ str=  IF  2drop r@ cell+ $! r> to last#  true  EXIT  THEN
    rdrop false ;

: #@? ( addrkey u bucket -- addrval u true / addrkey u false )
    dup >r @ 0= IF  rdrop false  EXIT  THEN
    2dup r@ $@ str=  IF  2drop r> dup to last# cell+ $@ true  EXIT  THEN
    rdrop false ;

: bucket-off ( bucket -- )
    ?dup-IF  dup $free cell+ $free  THEN ;

: #free? ( addrkey u bucket -- true / addrkey u false )
    dup >r @ 0= IF  rdrop false  EXIT  THEN
    2dup r@ $@ str=  IF  2drop r> bucket-off true  EXIT  THEN
    rdrop false ;

$180 cells Constant table-size#

: hash@ ( bucket -- addr )  dup
    >r @ 0= IF  table-size# allocate throw dup table-size# erase dup r> !
    ELSE  r> @  THEN ;

warnings @ warnings off \ hash-bang will be redefined

: #! ( addrval u addrkey u hash -- ) { hash }
    2dup string-hash  hash$ bounds ?DO
	I c@ $7F and 2* cells hash hash@ + #!? IF
	    UNLOOP  EXIT  THEN
	I c@ $80 or $80 + cells hash hash@ + to hash
    LOOP  2drop 2drop true abort" hash exhausted, please reboot universe" ;

warnings !

: #@ ( addrkey u hash -- addrval u / 0 0 ) { hash }
    2dup string-hash  hash$ bounds ?DO
	I c@ $7F and 2* cells hash @ dup 0= IF  2drop  LEAVE  THEN
	+ #@? IF  UNLOOP  EXIT  THEN
	I c@ $80 or $80 + cells hash @ + to hash
    LOOP  2drop #0. ;

: #+! ( addr1 u1 addr2 u2 hash -- ) >r
    2dup r@ #@ d0= IF  r> #!  ELSE  2drop rdrop last# cell+ $+!  THEN ;

: #free ( addrkey u hash -- )  { hash }
    2dup string-hash  hash$ bounds ?DO
	I c@ $7F and 2* cells hash @ dup 0= IF  2drop  LEAVE  THEN
	+ #free? IF  UNLOOP  EXIT  THEN
	I c@ $80 or $80 + cells hash @ + to hash
    LOOP  2drop ;

: #frees ( hash -- ) dup @ 0= IF  drop  EXIT  THEN  dup
    >r @             $100 cells bounds DO  I $free    cell +LOOP
    r@ @ $100 cells + $80 cells bounds DO  I recurse  cell +LOOP
    r@ @ free throw  r> off ;

-1 8 rshift invert Constant msbyte#

: leftalign ( key -- key' )
    BEGIN  dup msbyte# and 0= WHILE  8 lshift  dup 0= UNTIL  THEN ;

: #key ( addrkey u hash -- path / -1 ) 0 { hash key }
    2dup string-hash  hash$ drop cell bounds ?DO
	key 8 lshift I c@ $80 or or  to key
	I c@ $7F and 2* cells hash @ dup 0= IF  2drop  LEAVE  THEN
	+ #@? IF  2drop key -$81 and leftalign   UNLOOP  EXIT  THEN
	I c@ $80 or $80 + cells hash @ + to hash
    LOOP  2drop -1 ;

: #.key ( path hash -- item ) @ { hash }
    BEGIN
	hash 0= IF  drop 0  EXIT  THEN
	$100 um* dup $80 and WHILE
	    $80 + cells hash + @ to hash
    REPEAT \ stack: pathlow pathhigh (<=$7F)
    nip 2* cells hash + ;

: #map  { hash xt -- } \ xt: ( ... node -- ... )
    hash @ 0= ?EXIT
    hash @ $100 cells bounds DO
	I @ IF  I xt execute  THEN
    2 cells +LOOP
    hash @ $100 cells + $80 cells bounds DO
	I @ IF  I xt recurse  THEN
    cell +LOOP ;

: #.entry ( hash-entry -- ) dup $@ type ."  -> " cell+ $@ type cr ;

0 warnings !@
: #. ( hash -- )  ['] #.entry #map ;
warnings !

' Variable alias hash:

\ test: move dictionary to hash

\\\
variable ht
: test ( -- )
    context @ cell+ BEGIN  @ dup  WHILE
	    dup name>string 2dup ht #!
    REPEAT  drop ;
: test1 ( -- )
    context @ cell+ BEGIN  @ dup  WHILE
	    dup name>string 2dup ht #@ str= 0= IF ." unequal" cr THEN
    REPEAT  drop ;
: test2 ( -- )
    context @ cell+ BEGIN  @ dup  WHILE
	    dup name>string 2dup ht #key dup h. cr ht #.key $@ str= 0= IF ." unequal" cr THEN
    REPEAT  drop ;
