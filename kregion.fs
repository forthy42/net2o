\ crypto region based allocation

\ Copyright (C) 2014   Bernd Paysan

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
Variable kfree32' \ free list for 32 bytes keys
Variable kfree64' \ free list for 64 bytes keys

$4000 Constant /kregion

: kalloc ( len -- addr ) >r
    \G allocate a len byte block of non-swappable stuff
    r@ /kregion u> !!kr-size!!
    kregion 2@ dup r@ u< IF
	2drop /kregion alloc+lock /kregion 2dup kregion 2!  THEN
    over swap r> safe/string kregion 2! ;

: kalloc32 ( -- addr )
    kfree32' @ ?dup-if  dup @ kfree32' ! dup off  exit  then
    32 kalloc ;
: kfree32 ( addr -- )
    dup 32 erase kfree32' @ over ! kfree32' ! ;

: kalloc64 ( -- addr )
    kfree64' @ ?dup-if  dup @ kfree64' ! dup off  exit  then
    64 kalloc ;
: kfree64 ( addr -- )
    dup 64 erase kfree64' @ over ! kfree64' ! ;

: kalloc32? ( addr -- addr' )
    dup @ IF  @  EXIT  THEN  drop kalloc32 ;
: kalloc64? ( addr -- addr' )
    dup @ IF  @  EXIT  THEN  drop kalloc64 ;

: sec! ( addr1 u1 addr2 -- )
    >r case
	32 of  r@ kalloc32? dup r@ ! 32 move endof
	64 of  r@ kalloc64? dup r@ ! 64 move endof
    nip endcase rdrop ;

storage class end-class crypto-alloc

:noname  ( len -- addr )  kalloc ; crypto-alloc to :allocate
\ we never free these classes, they are per-task temporary storages

crypto-alloc ' new static-a with-allocater Constant crypto-a
