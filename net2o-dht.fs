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
\ specify strength, not length! length is 2*strength
32 Constant hash#128 \ 128 bit hash strength is enough!
64 Constant hash#256 \ 256 bit hash strength is more than enough!

\ Idea: set "r" first half to the value, "r" second half to the key, diffuse
\ we use explicitely Keccak here, this needs to be globally the same!
\ Keyed hashs are there for unique handles

\ padding

: >pad ( addr u u2 -- addr u2 )
    swap >r 2dup r@ /string r> fill ;
: >unpad ( addr u' -- addr u ) over + 1- c@ ;
: ?padded ( addr u' -- flag )
    2dup + 1- c@ dup >r /string r> skip nip 0= ;

: >padded { addr1 u1 addr2 u2 -- }
    addr1 addr2 u1 u2 umin move
    addr2 u1 u2 >pad 2drop ;

: >keyed-hash ( valaddr uval keyaddr ukey -- )
    hash( ." hashing: " 2over xtype ':' emit 2dup xtype F cr )
    keyed-hash-buf keccak#max keccak#max 2/ /string >padded
    keyed-hash-buf keccak#max 2/ >padded
    keyed-hash-buf keccak#max >keccak keccak*
    hash( @keccak 200 xtype F cr F cr ) ;

: keyed-hash#128 ( valaddr uval keyaddr ukey -- hashaddr uhash )
    keccak0 >keyed-hash  keyed-hash-out hash#128 2dup keccak> ;
: keyed-hash#256 ( valaddr uval keyaddr ukey -- hashaddr uhash )
    keccak0 >keyed-hash  keyed-hash-out hash#256 2dup keccak> ;

\ For speed reasons, the DHT is in-memory
\ we may keep a log of changes on disk if we want persistence
\ might not be saved too frequently... robustness comes from distribution
\ This is actually a PHT, a prefix hash tree; base 256 (bytes)

$200 cells Constant dht-size# \ $100 entris + $100 chains

Variable d#public

: dht@ ( bucket -- addr )  >r
    r@ @ 0= IF  dht-size# allocate throw dup r> ! dup dht-size# erase
    ELSE  r> @  THEN ;

\ keys are enumerated small integers

: enum ( n -- n+1 )  dup Constant 1+ ;

0
enum k#hash     \ hash itself is item 0
enum k#peers    \ distribution list - includes "where did I get this from"
                \ managed by the hash owner himself
enum k#owner    \ owner(s) of the object (pubkey+signature)
enum k#host     \ network id+routing from there (+signature)
enum k#map      \ peers have those parts of the object
enum k#tags     \ tags added
\ most stuff is added as tag or tag:value pair
cells Constant k#size

\ map primitives
\ map layout: offset, bitmap pairs (64 bits each)
\ string array: starts with base map (32kB per bit)

\ !!TBD!!

\ hash errors

s" invalid DHT key"              throwcode !!no-dht-key!!
s" DHT permission denied"        throwcode !!dht-permission!!
s" no signature"                 throwcode !!no-sig!!
s" invalid signature"            throwcode !!wrong-sig!!

\ Hash state variables

UValue d#id
User d#hashkey cell uallot drop
User d#sigsize \ size for signature, temporary value

\ checks for signatures

: >delete ( addr u type u2 -- addr u )
    "delete" >keyed-hash ;
: >host ( addr u -- addr u )  dup $40 u< !!no-sig!!
    keccak0 2dup $50 - "host" >keyed-hash
    2dup + $50 - $10 "date" >keyed-hash ; \ hash from address
: verify-host ( addr u -- addr u flag )
    2dup $40 - + d#hashkey 2@ drop ed-verify $50 d#sigsize ! ;
: check-host ( addr u -- addr u )
    >host verify-host 0= !!wrong-sig!! ;
: >tag ( addr u -- addr u )
    dup $70 u< !!no-sig!!
    keccak0 d#hashkey 2@ "tag" >keyed-hash
    2dup + $70 - $10 "date" >keyed-hash
    2dup $70 - ':' $split 2swap >keyed-hash ;
: verify-tag ( addr u -- addr u flag )
    2dup + $60 - dup $20 + swap ed-verify $70 d#sigsize ! ;
: check-tag ( addr u -- addr u )
    >tag verify-tag 0= !!wrong-sig!! ;
: delete-tag? ( addr u -- addr u flag )
    >tag "tag" >delete verify-tag ;
: delete-host? ( addr u -- addr u flag )
    >host "host" >delete verify-host ;

\ some hash storage primitives

: d#? ( addrkey u bucket -- addr u bucket/0 )
    dup @ 0= ?EXIT
    >r 2dup r@ @ $@ str= IF  r> EXIT  THEN
    rdrop false ;

: d# ( addr u hash -- bucket ) { hash }
    2dup bounds ?DO
	I c@ cells hash dht@ + d#? ?dup-IF
	    nip nip UNLOOP  EXIT  THEN
	I c@ $100 + cells hash dht@ + to hash
    LOOP  true abort" dht exhausted - this should not happen" ;

Variable ins$0 \ just a null pointer

: $ins[] ( addr u $array -- )
    \G insert O(log(n)) into pre-sorted array
    { $a } 0 $a $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup $# $a $[]@ compare dup 0= IF
		drop $# $a $[]! EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    ins$0 cell $a r@ cells $ins r> $a $[]! ;
: $del[] ( addr u $array -- )
    \G delete O(log(n)) from pre-sorted array
    { $a } 0 $a $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup $# $a $[]@ compare dup 0= IF
		drop $# $a $[] $off
		$a $# cells cell $del
		2drop EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT 2drop 2drop ; \ not found

: $ins[]sig ( addr u $array -- )
    \G insert O(log(n)) into pre-sorted array
    { $a } 0 $a $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup d#sigsize @ - $# $a $[]@ d#sigsize @ - compare dup 0= IF
		drop $# $a $[]! EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    ins$0 cell $a r@ cells $ins r> $a $[]! ;
: $del[]sig ( addr u $array -- )
    \G delete O(log(n)) from pre-sorted array, check sigs
    { $a } 0 $a $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup d#sigsize @ - $# $a $[]@ d#sigsize @ - compare dup 0= IF
		$# $a $[] $off
		$a $# cells cell $del
		2drop EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT 2drop 2drop ; \ not found

: >d#id ( addr u -- ) 2dup d#hashkey 2! d#public d# to d#id ;
: (d#value+) ( addr u key -- ) \ without sanity checks
    cells dup k#size u>= !!no-dht-key!!
    d#id @ 0= IF \ want to allocate it? check first!
	k#size alloz d#id !
	d#hashkey 2@ d#id @ $!
    THEN
    d#id @ + $ins[]sig ;
: d#. ( -- )
    d#id @ $@ xtype ." :" cr
    k#size cell DO
	I cell/ 0 .r ." : "
	d#id @ I + [: cr xtype ." , " ;] $[]map cr
    cell +LOOP ;
: d#value- ( addr u key -- )
    cells dup k#size u>= !!no-dht-key!!
    d#id @ 0= IF  drop 2drop  EXIT  THEN \ we don't have it
    dup >r d#id @ +
    r@ k#host cells = IF  >r delete-host? IF  r> $del[]sig dht( d#. )
	ELSE  2drop rdrop  THEN  rdrop EXIT  THEN
    r@ k#tags cells = IF  >r delete-tag?  IF  r> $del[]sig dht( d#. )
	ELSE  2drop rdrop  THEN  rdrop EXIT  THEN
    rdrop drop 2drop ;
: d#value+ ( addr u key -- ) \ with sanity checks
    dup >r k#peers u<= !!dht-permission!! \ can't change hash+peers
    r@ k#host = IF  check-host  THEN
    r@ k#tags = IF  check-tag   THEN
    r> (d#value+) dht( d#. ) ;
Defer d#value? ( key -- )
: d#values? ( mask n -- ) drop 64drop ;

\ commands for DHT

130 net2o: dht-id ( addr u -- ) >d#id ;
\g set dht id for further operations on it
131 net2o: dht-value+ ( addr u key -- ) 64>n d#value+ ;
\g add a value to the given dht key
132 net2o: dht-value- ( addr u key -- ) 64>n d#value- ;
\g remove a value from the given dht key
133 net2o: dht-value? ( type -- ) 64>n d#value? ;
134 net2o: dht-values? ( mask n -- ) 64>n drop 64drop ;
\g query the dht values mask selects which) and send back up to n
\g items with dht-value+

\ value reading requires constructing answer packet

also net2o-base

:noname ( key -- )  d#id @ 0= ?EXIT
    d#id @ $@ $, dht-id \ this is the id we send
    k#tags umin dup cells d#id @ + [: $, dup ulit, dht-value+ ;] $[]map
    drop ; IS d#value?

previous

\ facilitate stuff

$10 buffer: sigdate \ date+expire date
: now>never ( -- )  ticks sigdate 64! 64#-1 sigdate 64'+ 64! ;
: forever ( -- )  64#0 sigdate 64! 64#-1 sigdate 64'+ 64! ;
: now+delta ( delta64 -- )  ticks 64dup sigdate 64! 64+ sigdate 64'+ 64! ;

: gen>host ( addr u -- addr u )
    2dup keccak0 "host" >keyed-hash
    sigdate $10 "date" >keyed-hash ;
: host$ ( addr u -- hsotaddr host-u )
    [: type sigdate $10 type skc pkc ed-sign type ;] $tmp ;
: gen-host ( addr u -- addr' u' )
    gen>host host$ ;
: gen-host-del ( addr u -- addr' u' )
    gen>host "host" >delete host$ ;

: gen>tag ( addr u hash-addr uh -- addr u )
    keccak0 "tag" >keyed-hash
    sigdate $10 "date" >keyed-hash
    2dup ':' $split 2swap >keyed-hash ;
: tag$ ( addr u -- tagaddr tag-u )
    [: type sigdate $10 type pkc keysize type skc pkc ed-sign type ;] $tmp ;

: gen-tag ( addr u hash-addr uh -- addr' u' )
    gen>tag tag$ ;
: gen-tag-del ( addr u hash-addr uh -- addr' u' )
    gen>tag "tag" >delete tag$ ;

also net2o-base
: addme-end nest[ request-done ]nest ;
: addme ( addr u -- ) 2dup .iperr  now>never
    what's expect-reply? ['] addme-end <> IF
	expect-reply pkc keysize $, dht-id
	my-ip$ [: gen-host $, k#host ulit, dht-value+ ;] $[]map
    THEN
    gen-host $, k#host ulit, dht-value+
    ['] addme-end IS expect-reply? ;
previous

: +addme ['] addme setip-xt ! ;
: -setip ['] .iperr setip-xt ! ;