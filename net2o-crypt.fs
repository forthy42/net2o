\ symmetric encryption and decryption

\ generics from the crypto api

: encrypt-buffer ( addr u -- ) crypto@ >o c:encrypt o> ;
: decrypt-buffer ( addr u -- ) crypto@ >o c:decrypt o> ;
: encrypt-auth ( addr u -- ) crypto@ >o c:encrypt+auth o> ;
: decrypt-auth ( addr u -- flag ) crypto@ >o c:decrypt+auth o> ;
: start-diffuse ( -- )  crypto@ >o c:diffuse o> ;
: source-state> ( addr -- )  crypto@ >o c:key> o> ;
: >source-state ( addr -- )  crypto@ >o >c:key o> ;
: prng-buffer ( addr u -- ) crypto@ >o c:prng o> ;
: checksum ( -- xd )  crypto@ >o c:checksum o> ;
: cookie ( -- x )  crypto@ >o c:cookie o> ;

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
	swap regen-ivs o> >source-state
	EXIT
    THEN
    drop o> ;

: ivs>source? ( addr -- )
\    dup @ 0= IF  drop  EXIT  THEN
    @ >o
    dest-addr @ o 2@ >r - dup r> u<
    IF
	max-size^2 1- rshift
	dest-ivs @ + o> >source-state
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

: wurst-mykey-setup ( addr u -- addr' u' )
    over >r  rng@ rng@ r> 128! wurst-mykey-init ;

: wurst-outbuf-encrypt ( flag -- ) +calc
    wurst-outbuf-init
    outbuf packet-data +cryptsu encrypt-auth +enc ;

: wurst-inbuf-decrypt ( flag1 -- flag2 ) +calc
    \G flag1 is true if code, flag2 is true if decrypt succeeded
    wurst-inbuf-init
    inbuf packet-data +cryptsu decrypt-auth +enc ;

: wurst-encrypt$ ( addr u -- ) +calc
    wurst-mykey-setup 2 64s - encrypt-auth +enc ;

: wurst-decrypt$ ( addr u -- addr' u' flag ) +calc $>align
    wurst-mykey-init 2 64s - 2dup decrypt-auth +enc ;

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

: (regen-ivs) ( offset o:map -- )
    dup dest-ivs $@len dest-ivslastgen @
    IF \ check if in quarter 2
	2/ 2/ dup bounds within 0=
    ELSE \ check if in quarter 4
	2/ dup 2/ + u>
    THEN  IF
	regen-ivs/2
    THEN  drop ;
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
