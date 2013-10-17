\ distributed hash table                             16oct2013py

\ Copyright (C) 2013   Bernd Paysan

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

keccak# buffer: keyed-hash-buf
keccak#max buffer: keyed-hash-out
\ specify strength, not length!
32 Constant hash#128 \ 128 bit hash strength is enough!
64 Constant hash#256 \ 256 bit hash strength is more than enough!

\ Idea: set "r" to the hashed value, and "c" to the key, diffuse
\ we use explicitely Keccak here, this needs to be globally the same!
\ Keyed hashs are there for unique handles

: >padded { addr1 u1 addr2 u2 -- }
    addr1 addr2 u1 u2 umin move
    addr2 u1 u2 >pad 2drop ;

: >keyed-hash ( valaddr uval keyaddr ukey -- )
    keyed-hash-buf keccak# keccak#max /string >padded
    keyed-hash-buf keccak#max >padded
    keccak0 keyed-hash-buf keccak# >keccak keccak* ;

: keyed-hash#128 ( valaddr uval keyaddr ukey -- hashaddr uhash )
    >keyed-hash  keyed-hash-out hash#128 2dup keccak> ;
: keyed-hash#256 ( valaddr uval keyaddr ukey -- hashaddr uhash )
    >keyed-hash  keyed-hash-out hash#256 2dup keccak> ;

\ For speed reasons, the DHT is in-memory
\ we may keep a log of changes on disk if we want persistence
\ might not be saved too frequently... robustness comes from distribution
\ This is actually a PHT, a prefix hash tree; base 256 (bytes)

$200 cells Constant dht-size# \ $100 entris + $100 chains


: dht@ ( bucket -- addr )  >r
    r@ @ 0= IF  dht-size# allocate throw dup r> ! dup dht-size# erase
    ELSE  r> @  THEN ;

\ keys are enumerated small integers

: enum ( n -- n+1 )  dup Constant 1+ ;

0
enum k#hash     \ hash itself is item 0
enum k#peers    \ peers who have copies of that entry - private
enum k#n2ohost  \ n2o network id+routing from there
enum k#ipv4host \ 6 bytes: addr:port
enum k#ipv6host \ 18 bytes: addr:port
enum k#dnshost  \ name+2 bytes port
enum k#filename \ path+file for object
enum k#person   \ person associated
enum k#object   \ object associated
\ more to come
cells Constant k#size

\ some primitives

: d#? ( addrkey u bucket -- addr u bucket/0 )
    dup @ 0= ?EXIT
    >r 2dup r@ @ $@ str=  IF  r>  EXIT  THEN
    rdrop false ;

: d# ( addr u hash -- bucket ) { hash }
    bounds ?DO
	I c@ cells hash dht@ + d#? ?dup-IF
	    UNLOOP  EXIT  THEN
	I c@ $100 + cells hash dht@ + to hash
    LOOP  true abort" dht exhausted - this should not happen" ;


: dht:id ( addr u -- )  2drop ;
: dht:key ( n -- )  drop ;
: dht:value+ ( addr u time -- )  64drop 2drop ;
: dht:have? ( start end n -- )  drop 64drop 64drop ;

\ commands for DHT

130 net2o: dht-id ( addr u -- ) dht:id ;
\g set dht id for further operations on it
131 net2o: dht-key ( n -- ) 64>n dht:key ;
\g set the dht key for the current dht id
132 net2o: dht-value+ ( addr u time -- ) dht:value+ ;
\g add a value plus expire time to the given dht key
133 net2o: dht-values? ( n -- ) 64>n drop ;
\g query the dht values and send back up to n items with dht-value+
134 net2o: dht-have? ( start end n -- ) 64>n dht:have? ;
\g send a list of hashes back to determine what you have