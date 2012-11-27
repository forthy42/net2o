\ symmetric encryption and decryption

: >wurst-source' ( addr -- )  wurst-source state# move ;

: wurst-key$ ( -- addr u )
    j^ dup 0= IF
	drop wurst-key state#
    ELSE
	crypto-key $@
    THEN ;

0 Value @key
: !key ( key -- )  dup to @key rounds-setkey ;
: wurst-source-state> ( addr -- )  @key swap state# 2* move ;
: >wurst-source-state ( addr -- )  wurst-source state# 2* move
    wurst-source !key ;
: copy-key ( -- )
    @key ?dup-IF  >wurst-source-state  THEN ;

: >wurst-key ( addr u -- )
    wurst-source !key \ if we use wurst-state, we should set the key
    wurst-state swap move ;
: >wurst-source ( addr u -- )
    wurst-source state# bounds ?DO
	2dup I swap move
    dup +LOOP  2drop ;

\ regenerate ivs is a buffer swapping function:
\ regenerate half of the ivs per time, when you reach the middle of the other half
\ of the ivs buffer.

Defer regen-ivs

: dest-a/b ( addr u map -- addr1 u1 )
    dest-ivslastgen @ IF  dup 2/ safe/string  ELSE  2/  THEN ;

: clear-replies ( addr -- )  >r
    r@ dest-replies @ r@ dest-size @ addr>replies r@ dest-a/b
    cmd( ." Clear replies " over hex. dup hex. cr )
    erase  rdrop ;

: ivs>code-source? ( addr -- )
    dup @ 0= IF  drop  EXIT  THEN
    $@ drop >r
    dest-addr @ r@ 2@ bounds within 0=
    IF
	dest-addr @  r@ dest-vaddr @ -  max-size^2 1- rshift
	r@ dest-ivs @ IF
	    r@ dest-ivs $@ 2 pick safe/string drop
	    over r@ regen-ivs >wurst-source-state
	ELSE
	    crypt( ." No code source IVS" cr )
	THEN
	drop
    THEN
    rdrop ;

: ivs>source? ( addr -- )
    dup @ 0= IF  drop  EXIT  THEN
    $@ drop >r
    dest-addr @ r@ 2@ bounds within 0=
    IF
	dest-addr @  r@ dest-vaddr @ -  max-size^2 1- rshift
	r@ dest-ivs @ IF
	    r@ dest-ivs $@ 2 pick safe/string drop >wurst-source-state
	ELSE
	    crypt( ." No source IVS" cr )
	THEN
	drop
    THEN
    rdrop ;

: default-key ( -- )
    @key 0= IF
	rnd-init >wurst-source'
	wurst-key$ >wurst-key
    THEN ;

: wurst-outbuf-init ( flag -- )
    0 to @key
    j^ IF
	IF
	    j^ code-map ivs>code-source?
	ELSE
	    j^ data-map ivs>source?
	THEN
    ELSE
	drop
    THEN
    default-key
    key( @key .64b cr @key state# + .64b cr ) ;

: wurst-inbuf-init ( flag -- )
    0 to @key
    j^ IF
	IF
	    j^ code-rmap ivs>code-source?
	ELSE
	    j^ data-rmap ivs>source?
	THEN
    ELSE
	drop
    THEN
    default-key
    key( @key .64b cr @key state# + .64b cr ) ;

$10 Constant mykey-salt#
state# buffer: mykey \ server's private key
rng$ mykey swap move

: start-diffuse ( -- )  message roundse# rounds ;

: wurst-mykey-init ( addr u -- addr' u' )
    over mykey-salt# >wurst-source
    mykey state# >wurst-key
    mykey-salt# safe/string
    start-diffuse ;

: mem-rounds# ( size -- n )
    case
	min-size of  $22  endof
	min-size 2* of  $24  endof
	$28 swap
    endcase ;

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

: wurst-crc ( -- xd )
    start-diffuse  \ another key diffusion round
    64#0 64#0 @key state# + state# bounds ?DO  I 128@ 128xor 2 64s +LOOP ;
: wurst-cookie ( -- x )
    64#0 @key state# bounds ?DO  I 64@ 64xor  1 64s +LOOP ;

[IFDEF] nocrypt \ dummy for test
    : encrypt-buffer  ( addr u n -- addr' 0 )  drop + 0 ;
    : wurst-outbuf-encrypt drop ;
    : wurst-inbuf-decrypt drop true ;
    : wurst-encrypt$ ( addr u -- ) 2drop ;
    : wurst-decrypt$ ( addr u -- addr' u' flag )
	mykey-salt# safe/string 2 64s - true ;
[ELSE]
    : encrypt-buffer ( addr u n -- addr 0 ) dup >reads state# * { rnd reads }
	BEGIN  dup 0>  WHILE
		over rnd rounds  reads /string
	REPEAT ;
    : decrypt-buffer ( addr u n -- addr 0 ) dup >reads state# * { rnd reads }
	BEGIN  dup 0>  WHILE
		over rnd rounds-decrypt  reads /string
	REPEAT ;
    
    : wurst-outbuf-encrypt ( flag -- ) +calc
	wurst-outbuf-init
	outbuf packet-data
	outbuf body-size mem-rounds# +calc1 encrypt-buffer
	drop >r wurst-crc r> 128! +enc ;

    : wurst-inbuf-decrypt ( flag1 -- flag2 ) +calc
	\G flag1 is true if code, flag2 is true if decrypt succeeded
	wurst-inbuf-init
	inbuf packet-data
	inbuf body-size mem-rounds# +calc1 decrypt-buffer
	drop 128@ wurst-crc 128= +enc ;

    : wurst-encrypt$ ( addr u -- )  +calc
	wurst-mykey-setup 2 64s - dup mem-rounds#
	encrypt-buffer
	drop >r wurst-crc r> 128! +enc ;

    : wurst-decrypt$ ( addr u -- addr' u' flag ) +calc $>align
	wurst-mykey-init 2 64s - dup mem-rounds# >r
	2dup
	BEGIN  dup 0>  WHILE
		over r@ rounds-decrypt  r@ >reads state# * safe/string
	REPEAT
	rdrop drop 128@ wurst-crc 128= +enc ;
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
    wurst-source !key
    j^ dup 0= IF
	crypt( ." IVS generated for non-connection!" cr )
	drop wurst-key state# wurst-state swap move
    ELSE
	do-keypad @ IF
	    drop
	    keypad wurst-state keysize move
	    keypad wurst-state keysize + keysize move
	ELSE
	    crypto-key $@ wurst-state swap move
	THEN
    THEN ;

: regen-ivs/2 ( map -- ) >r
    r@ dest-ivsgen @ msg( dup .64b cr dup state# + .64b cr ) !key
    r@ clear-replies
    r@ dest-ivs $@ r@ dest-a/b 2dup erase
    dup mem-rounds# encrypt-buffer 2drop
    -1 r> dest-ivslastgen xor! ;

: gen-ivs ( ivs-addr -- ) >r  r@ $@ erase
    start-diffuse
    r@ $@ dup 2/ mem-rounds# encrypt-buffer 2drop
    r> cell+ @ wurst-source-state> ;

: regen-ivs-all ( map -- ) >r
    r@ dest-ivsgen @ !key
\    @key state# 2* dump
    r> dest-ivs gen-ivs ;

: (regen-ivs) ( offset map -- ) >r
    dup r@ dest-ivs $@len
    r@ dest-ivslastgen @ IF \ check if in quarter 2
	2/ 2/ dup
    ELSE \ check if in quarter 4
	2/ dup 2/ dup >r + r>
    THEN  bounds within 0=  IF
\	." regenerate ivs " dup . cr
	r@ regen-ivs/2
    THEN  drop rdrop ;
' (regen-ivs) IS regen-ivs

: ivs-string ( addr u n addr -- )
    >r r@ $!len
    >wurst-key-ivs
    state# <> !!ivs!! >wurst-source'
    r> gen-ivs ;

: ivs-size@ ( map -- n addr ) $@ drop >r
    r@ dest-size @ max-size^2 1- rshift r> dest-ivs ;

: net2o:gen-data-ivs ( addr u -- )
    j^ data-map ivs-size@ ivs-string ;
: net2o:gen-code-ivs ( addr u -- )
    j^ code-map ivs-size@ ivs-string ;
: net2o:gen-rdata-ivs ( addr u -- )
    j^ data-rmap ivs-size@ ivs-string ;
: net2o:gen-rcode-ivs ( addr u -- )
    j^ code-rmap ivs-size@ ivs-string ;

: set-key ( addr -- ) j^ 0= IF drop  ." key, no context!" cr  EXIT  THEN
    keysize 2* j^ crypto-key $!
    \ double key to get 512 bits
    j^ crypto-key $@ 2/ 2dup + swap move
    ( ." set key to:" j^ crypto-key $@ dump ) ;

: ?keysize ( u -- )
    keysize <> !!keysize!! ;

: net2o:receive-key ( addr u -- )
    j^ 0= IF  2drop EXIT  THEN
    ?keysize
    keypad skc rot crypto_scalarmult ;

: net2o:update-key ( -- )
    do-keypad @ IF
	keypad set-key
	do-keypad off
    THEN ;

