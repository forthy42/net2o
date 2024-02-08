\ crypto region based allocation

\ Copyright © 2014   Bernd Paysan

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

2Variable kregion \ current region pointer + remainder
Variable kfree64' \ free list for 64 bytes keys

$4000 Constant /kregion

$10000 \ pathetic, but safe value
[IFDEF] RLIMIT_MEMLOCK
    RLIMIT_MEMLOCK pad getrlimit 0= [IF]
	drop pad rlim_cur 64@ 64>n
    [THEN]
    [IFDEF] __info
	pad sysinfo 0= [IF]
	    pad totalswap @ 0= [IF]  drop 0  [THEN]
	    \ If we have no swap at all, no need to mlock() anything
	[THEN]
    [THEN]
[THEN]

Value /kregion-max

$20 Constant crypt-align

0 Value /kregion#

: kalign ( addr -- addr' )
    [ crypt-align 1- ]L + [ crypt-align negate ]L and ;

: kalloc ( len -- addr )
    \G allocate a len byte block of non-swappable stuff
    kalign dup
    >r /kregion u> !!kr-size!!
    kregion 2@ dup r@ u< IF
	/kregion +to /kregion#  2drop /kregion
	\ we have to fall back to alloc+guard if we want more than 64k
	/kregion# /kregion-max u> IF  alloc+guard  ELSE  alloc+lock  THEN
	/kregion 2dup kregion 2!  THEN
    over swap r> safe/string kregion 2! ( kalloc( ." kalloc: " dup h. cr ) ;

:noname defers 'image  #0. kregion 2!  0 to /kregion# kfree64' off ; is 'image

\ fixed size secrets are assumed to be all 64 bytes long
\ if they are just 32 bytes, the second half is all zero

: kalloc64 ( -- addr )
    kfree64' @ ?dup-if  dup @ kfree64' ! dup off  exit  then
    64 kalloc ;
: kfree64 ( addr -- )
    dup 64 erase kfree64' @ over ! kfree64' ! ;
: kalloc64? ( addr -- addr' )
    dup @ IF  @  EXIT  THEN  drop kalloc64 ;

32 buffer: zero32

: sec-free ( addr -- ) dup @ dup  IF  kfree64 off EXIT  THEN  2drop ;
: sec! ( addr1 u1 addr2 -- )
    over 0= IF  sec-free 2drop  EXIT  THEN
    dup >r kalloc64? dup r> ! $40 smove ;
: sec@ ( addr -- addr1 u1 )
    @ dup IF  $40
	over $20 + $20 zero32 over str= IF  2/
	    over $10 + $10 zero32 over str= IF  2/  THEN
	THEN
    ELSE 0 THEN ;
: sec+! ( addr1 u1 addr2 -- )
    dup @ 0= IF  sec!  ELSE  sec@ dup >r + $40 r> - smove  THEN ;

: sec+[]! ( addr1 u1 addr2 -- ) >r
    { | w^ sec } sec sec! sec cell r> $+! ;

: sec[]@ ( i addr -- addr u )  $[] sec@ ;

: sec[]free ( addr -- ) dup
    >r $@ bounds ?DO
	I sec-free
    cell +LOOP
    r> $free ;

storage class end-class crypto-alloc

:noname  ( len -- addr )
    [ crypt-align cell- crypt-align 1- + ]L +
    [ crypt-align negate ]L and kalloc [ crypt-align cell- ]L +
; crypto-alloc is :allocate
' drop crypto-alloc is :free
\ we never free these classes, they are per-task temporary storages

crypto-alloc ' new static-a with-allocater Constant crypto-a
