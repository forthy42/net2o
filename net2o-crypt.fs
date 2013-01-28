\ symmetric encryption and decryption

\ generics from the crypto api

: encrypt-buffer ( addr u -- ) crypto@ >o c:encrypt o> ;
: decrypt-buffer ( addr u -- ) crypto@ >o c:decrypt o> ;
: start-diffuse ( -- )  crypto@ >o c:diffuse o> ;
: source-state> ( addr -- )  crypto@ >o c:key> o> ;
: >source-state ( addr -- )  crypto@ >o >c:key o> ;
: prng-buffer ( addr u -- ) crypto@ >o c:prng o> ;
: wurst-crc ( -- xd )  crypto@ >o c:checksum o> ;
: wurst-cookie ( -- x )  crypto@ >o c:cookie o> ;

: >wurst-source' ( addr -- )  wurst-source state# move ;

: wurst-key$ ( -- addr u )
    o 0= IF
	wurst-key state#
    ELSE
	crypto-key $@
    THEN ;

: >wurst-key ( addr u -- )
    wurst-source rounds-setkey \ if we use wurst-state, we should set the key
    wurst-state swap move ;
: >wurst-source ( addr u -- )
    wurst-source state# bounds ?DO
	2dup I swap move
    dup +LOOP  2drop ;

\ regenerate ivs is a buffer swapping function:
\ regenerate half of the ivs per time, when you reach the middle of the other half
\ of the ivs buffer.

Defer regen-ivs

: dest-a/b ( addr u -- addr1 u1 )
    dest-ivslastgen @ IF  dup 2/ safe/string  ELSE  2/  THEN ;

: clear-replies ( -- )
    dest-replies @ dest-size @ addr>replies dest-a/b
    cmd( ." Clear replies " over hex. dup hex. cr )
    erase ;

: ivs>code-source? ( addr -- )
    dup @ 0= IF  drop  EXIT  THEN
    @ >o
    dest-addr @ o 2@ >r - dup r> u<
    IF
	max-size^2 1- rshift
	dest-ivs @ over +
	swap o regen-ivs >source-state o>
	EXIT
    THEN
    drop o> ;

: ivs>source? ( addr -- )
    dup @ 0= IF  drop  EXIT  THEN
    @ >o
    dest-addr @ o 2@ >r - dup r> u<
    IF
	max-size^2 1- rshift
	dest-ivs @ + >source-state o>
	EXIT
    THEN
    drop o> ;

: default-key ( -- )
    @state 0= IF
	rnd-init >wurst-source'
	wurst-key$ >wurst-key
    THEN ;

: wurst-outbuf-init ( flag -- )
    0 to @state
    o IF
	IF
	    code-map ivs>code-source?
	ELSE
	    data-map ivs>source?
	THEN
    ELSE
	drop
    THEN
    default-key
    key( @state .64b cr @state state# + .64b cr ) ;

: wurst-inbuf-init ( flag -- )
    0 to @state
    o IF
	IF
	    code-rmap ivs>code-source?
	ELSE
	    data-rmap ivs>source?
	THEN
    ELSE
	drop
    THEN
    default-key
    key( @state .64b cr @state state# + .64b cr ) ;

$10 Constant mykey-salt#
state# buffer: mykey \ server's private key
rng$ mykey swap move

: wurst-mykey-init ( addr u -- addr' u' )
    over mykey-salt# >wurst-source
    mykey state# >wurst-key
    mykey-salt# safe/string
    start-diffuse ;

[IFDEF] 64bit
    : 128xor ( ud1 ud2 -- ud3 )  rot xor >r xor r> ;
    ' 2@ Alias 128@ ( addr -- d )
    ' d= Alias 128= ( d1 d2 -- flag )
    ' 2! Alias 128! ( d addr -- )
[ELSE]
    : 128xor { x1 x2 x3 x4 y1 y2 y3 y4 -- z1 z2 z3 z4 }
	x1 y1 xor  x2 y2 xor  x3 y3 xor  x4 y4 xor ;
    : 128@ ( addr -- x1..x4 )
	>r
	r@ 3 cells + @
	r@ 2 cells + @
	r@ cell+ @
	r> @ ;
    : 128= ( x1..y4 y1..y4 -- flag )  128xor  or or or 0= ;
    : 128! ( x1..x4 addr -- )
	>r
	r@ !
	r@ cell+ !
	r@ 2 cells + !
	r> 3 cells + ! ;
[THEN]

: wurst-mykey-setup ( addr u -- addr' u' )
    over >r  rng@ rng@ r> 128! wurst-mykey-init ;

[IFDEF] nocrypt \ dummy for test
    : encrypt-buffer  ( addr u -- )  2drop ;
    : decrypt-buffer  ( addr u -- )  2drop ;
    : wurst-outbuf-encrypt drop ;
    : wurst-inbuf-decrypt drop true ;
    : wurst-encrypt$ ( addr u -- ) 2drop ;
    : wurst-decrypt$ ( addr u -- addr' u' flag )
	mykey-salt# safe/string 2 64s - true ;
[ELSE]
    : wurst-outbuf-encrypt ( flag -- ) +calc
	wurst-outbuf-init
	outbuf packet-data +cryptsu 2dup + >r encrypt-buffer
	wurst-crc r> 128! +enc ;

    : wurst-inbuf-decrypt ( flag1 -- flag2 ) +calc
	\G flag1 is true if code, flag2 is true if decrypt succeeded
	wurst-inbuf-init
	inbuf packet-data +cryptsu 2dup decrypt-buffer
	+ 128@ wurst-crc 128= +enc ;

    : wurst-encrypt$ ( addr u -- ) +calc
	wurst-mykey-setup 2 64s -
	2dup + >r encrypt-buffer wurst-crc r> 128! +enc ;

    : wurst-decrypt$ ( addr u -- addr' u' flag ) +calc $>align
	wurst-mykey-init 2 64s -
	2dup decrypt-buffer 2dup + 128@ wurst-crc 128= +enc ;
[THEN]

\ public key encryption

KEYBYTES Constant keysize \ our shared secred is only 32 bytes long
\ client keys
keysize buffer: pkc
keysize buffer: skc
\ shared secred
keysize buffer: keypad
Variable do-keypad

: gen-keys ( -- )
    rng$ keysize umin skc swap move
    pkc skc base9 crypto_scalarmult ;
\ the theory here is that sks*pkc = skc*pks
\ we send our public key and query the server's public key.

: >wurst-key-ivs ( -- )
    wurst-source rounds-setkey
    o 0= IF
	crypt( ." IVS generated for non-connection!" cr )
	wurst-key state# wurst-state swap move
    ELSE
	do-keypad @ IF
	    keypad wurst-state keysize move
	    keypad wurst-state keysize + keysize move
	ELSE
	    crypto-key $@ wurst-state swap move
	THEN
    THEN ;

: regen-ivs/2 ( -- )
    dest-ivsgen @ msg( dup .64b cr dup state# + .64b cr ) rounds-setkey
    clear-replies
    dest-ivs $@ dest-a/b prng-buffer
    -1 dest-ivslastgen xor! ;

: gen-ivs ( ivs-addr -- ) >r
    start-diffuse
    r@ $@ prng-buffer
    r> cell+ @ source-state> ;

: regen-ivs-all ( o:map -- )
    dest-ivsgen @ rounds-setkey
\    @state state# 2* dump
    dest-ivs gen-ivs ;

: (regen-ivs) ( offset map -- ) >o
    dup dest-ivs $@len
    dest-ivslastgen @ IF \ check if in quarter 2
	2/ 2/ dup bounds within 0=
    ELSE \ check if in quarter 4
	2/ dup 2/ + u>
    THEN  IF
	regen-ivs/2
    THEN  drop o> ;
' (regen-ivs) IS regen-ivs

: ivs-string ( addr u n addr -- )
    >r r@ $!len
    >wurst-key-ivs
    state# <> !!ivs!! >wurst-source'
    r> gen-ivs ;

: ivs-size@ ( map -- n addr ) @ >o
    dest-size @ max-size^2 1- rshift dest-ivs o> ;

: net2o:gen-data-ivs ( addr u -- )
    data-map ivs-size@ ivs-string ;
: net2o:gen-code-ivs ( addr u -- )
    code-map ivs-size@ ivs-string ;
: net2o:gen-rdata-ivs ( addr u -- )
    data-rmap ivs-size@ ivs-string ;
: net2o:gen-rcode-ivs ( addr u -- )
    code-rmap ivs-size@ ivs-string ;

: set-key ( addr -- ) o 0= IF drop  ." key, no context!" cr  EXIT  THEN
    keysize 2* crypto-key $!
    \ double key to get 512 bits
    crypto-key $@ 2/ 2dup + swap move
    ( ." set key to:" o crypto-key $@ dump ) ;

: ?keysize ( u -- )
    keysize <> !!keysize!! ;

: net2o:receive-key ( addr u -- )
    o 0= IF  2drop EXIT  THEN
    ?keysize
    keypad skc rot crypto_scalarmult ;

: net2o:update-key ( -- )
    do-keypad @ IF
	keypad set-key
	do-keypad off
    THEN ;
