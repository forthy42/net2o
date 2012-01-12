\ generic hash table functions

require string.fs
require wurstkessel.fs
require wurstkessel-init.fs

state# 2* buffer: 'hashinit
state# 8 * Constant message#

\ random initializer for hash

: hash-init-rng ( -- )
    rng$ 'hashinit swap move
    rng$ 'hashinit over + swap move ;

hash-init-rng

\ this computes a cryptographic secure hash over the input string -
\ of the first 510 bytes of the input string

: string-hash ( addr u -- )
    'hashinit wurst-source state# 2* move
    message message# erase
    dup message message# xc!+? drop
    rot umin dup >r move
    message r> 1- 6 rshift $11 + rounds  message 2 rounds ;

\ hierarchical hash table

\ hash tables store key,value-pairs.
\ Each hierarchy uses one byte of wurst-state as index (only lower 7 bits)
\ if there is a collission, add another indirection

: #!? ( addrval u addrkey u bucket -- true / addrval u addrkey u false )
    >r r@ @ 0= IF  r@ $! r> cell+ $!  true  EXIT  THEN
    2dup r@ $@ str=  IF  2drop r> cell+ $! true  EXIT  THEN
    rdrop false ;

: #@? ( addrkey u bucket -- addrval u true / addrkey u false )
    >r r@ @ 0= IF  rdrop false  EXIT  THEN
    2dup r@ $@ str=  IF  2drop r> cell+ $@ true  EXIT  THEN
    rdrop false ;    

: #off? ( addrkey u bucket -- true / addrkey u false )
    >r r@ @ 0= IF  rdrop false  EXIT  THEN
    2dup r@ $@ str=  IF  2drop r@ $off r> cell+ $off true  EXIT  THEN
    rdrop false ;    

$180 cells Constant table-size#

: hash@ ( bucket -- addr )  >r
    r@ @ 0= IF  table-size# allocate throw dup r> !
    ELSE  r> @  THEN ;

warnings @ warnings off \ hash-bang will be redefined

: #! ( addrval u addrkey u hash -- ) { hash }
    2dup string-hash  wurst-state state# bounds ?DO
	I c@ $7F and 2* cells hash hash@ + #!? IF
	    UNLOOP  EXIT  THEN
	I c@ $80 or $100 + cells hash + hash@  to hash
    LOOP  2drop 2drop true abort" hash exhausted, please reboot universe" ;

warnings !

: #@ ( addrkey u hash -- addrval u / 0 0 ) { hash }
    2dup string-hash  wurst-state state# bounds ?DO
	I c@ $7F and 2* cells hash @ dup 0= IF  2drop  LEAVE  THEN
	+ #@? IF  UNLOOP  EXIT  THEN
	I c@ $80 or $100 + cells hash + @ dup 0= IF  drop  LEAVE  THEN
	to hash
    LOOP  2drop 0 0 ;

: #off ( addrkey u hash -- )  { hash }
    2dup string-hash  wurst-state state# bounds ?DO
	I c@ $7F and 2* cells hash @ dup 0= IF  2drop  LEAVE  THEN
	+ #off? IF  UNLOOP  EXIT  THEN
	I c@ $80 or $100 + cells hash + @ dup 0= IF  drop  LEAVE  THEN
	to hash
    LOOP  2drop ;
