\ generic hash table functions

require string.fs
\ require wurstkessel.fs
require rng.fs

2 64s buffer: hashinit

\ random initializer for hash

: hash-init-rng ( -- )  rng@ hashinit 64! rng@ hashinit 64'+ 64! ;

hash-init-rng

\ this computes a cryptographic secure hash over the input string -
\ in three variants: a medium speed 64 bit hash, a very fast 128 bit hash,
\ and a slow cryptographically secure 512 bit hash

: use-hash-128 ;

[IFDEF] use-hash-64
    64Variable hash-state
    
    : string-hash ( addr u -- )  hashinit 64@ hash64 hash-state 64! ;
    
    : hash$ ( -- addr u )  hash-state [ 1 64s ]L ;
[THEN]
[IFDEF] use-hash-128
    2 64s buffer: hash-state
    
    : string-hash ( addr u -- )  hashinit hash-state [ 2 64s ]L move
	false hash-state hashkey2 ;
    
    : hash$ ( -- addr u )  hash-state [ 2 64s ]L ;
[IFDEF] use-hash-wurst
\ hash of the first 510 bytes of the input string, 3 times slower
    state# 8 * Constant message#

    : string-hash ( addr u -- )
	'hashinit wurst-source state# 2* move
	message message# erase
	dup message message# xc!+? drop
	rot umin dup >r move
	message r> 6 rshift $11 + rounds  message 2 rounds ;

    : hash$ ( -- addr u )  wurst-state state# ;
[THEN]

\ hierarchical hash table

\ hash tables store key,value-pairs.
\ Each hierarchy uses one byte of state as index (only lower 7 bits)
\ if there is a collission, add another indirection

0 value last#

: #!? ( addrval u addrkey u bucket -- true / addrval u addrkey u false )
    >r r@ @ 0= IF  r@ $! r@ cell+ $!  r> to last#
	true  EXIT  THEN
    2dup r@ $@ str=  IF  2drop r@ cell+ $! r> to last#  true  EXIT  THEN
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
    r@ @ 0= IF  table-size# allocate throw dup r> ! dup table-size# erase
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
    LOOP  2drop 0 0 ;

: #off ( addrkey u hash -- )  { hash }
    2dup string-hash  hash$ bounds ?DO
	I c@ $7F and 2* cells hash @ dup 0= IF  2drop  LEAVE  THEN
	+ #off? IF  UNLOOP  EXIT  THEN
	I c@ $80 or $80 + cells hash @ + to hash
    LOOP  2drop ;

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

: #.key ( path hash -- item ) { hash }
    BEGIN
	hash @ 0= IF  drop 0  EXIT  THEN
	$100 um* dup $80 and WHILE
	    $80 + cells hash @ + to hash
    REPEAT
    nip 2* cells hash @ + ;

: #map  { hash xt -- } \ xt: ( ... node -- ... )
    hash @ 0= ?EXIT
    hash @ $100 cells bounds DO
	I @ IF  I xt execute  THEN
    2 cells +LOOP
    hash @ $100 cells + $80 cells bounds DO
	I @ IF  I xt recurse  THEN
    cell +LOOP ;

: #.entry ( hash-entry -- ) dup $@ type ."  -> " cell+ $@ type cr ;

: #. ( hash -- )  ['] #.entry #map ;

\ test: move dictionary to hash

0 [IF]
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
	    dup name>string 2dup ht #key dup hex. cr ht #.key $@ str= 0= IF ." unequal" cr THEN
    REPEAT  drop ;
[THEN]