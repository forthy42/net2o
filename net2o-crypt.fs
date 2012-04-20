\ symmetric encryption and decryption

: >wurst-source' ( addr -- )  wurst-source state# move ;
: wurst-source-state> ( addr -- )  wurst-source swap state# 2* move ;
: >wurst-source-state ( addr -- )  wurst-source state# 2* move ;

: >wurst-source ( d -- )
    wurst-source state# bounds ?DO  2dup I 2!  2 cells +LOOP  2drop ;

: >wurst-key ( -- )
    j^ dup 0= IF
	drop wurst-key state#
    ELSE
	crypto-key $@
    THEN
    wurst-state swap move ;

\ regenerate ivs is a buffer swapping function:
\ regenerate half of the ivs per time, when you reach the middle of the other half
\ of the ivs buffer.

Defer regen-ivs

: clear-replies ( addr -- )  >r
    r@ code-flag @ IF
	r@ dest-timestamps @
	r@ dest-size @ 2/ addr>replies
	r@ dest-ivslastgen @ 0= IF
	    dup >r + r>
	THEN  erase
    THEN  rdrop ;

: ivs>code-source? ( addr -- )
    dup @ 0= IF  drop  EXIT  THEN
    $@ drop >r
    dest-addr @ r@ 2@ bounds within 0=
    IF
	dest-addr @  r@ dest-vaddr @ -  max-size^2 rshift
	r@ dest-ivs @ IF
	    r@ clear-replies
	    r@ dest-ivs $@ 2 pick safe/string drop >wurst-source'
	    dup r@ regen-ivs
	THEN
	drop
    THEN
    rdrop ;

: ivs>source? ( addr -- )
    dup @ 0= IF  drop  EXIT  THEN
    $@ drop >r
    dest-addr @ r@ 2@ bounds within 0=
    IF
	dest-addr @  r@ dest-vaddr @ -  max-size^2 rshift
	r@ dest-ivs @ IF
	    r@ dest-ivs $@ 2 pick safe/string drop >wurst-source'
	THEN
	drop
    THEN
    rdrop ;

: wurst-outbuf-init ( flag -- )
    rnd-init >wurst-source'
    j^ IF
	IF
	    j^ code-map ivs>code-source?
	ELSE
	    j^ data-map ivs>source?
	THEN
    ELSE
	drop
    THEN
    >wurst-key ;

: wurst-inbuf-init ( flag -- )
    rnd-init >wurst-source'
    j^ IF
	IF
	    j^ code-rmap ivs>code-source?
	ELSE
	    j^ data-rmap ivs>source?
	THEN
    ELSE
\	." no iv mapping" cr
	drop
    THEN
    >wurst-key ;

: mem-rounds# ( size -- n )
    case
	min-size of  $22  endof
	min-size 2* of  $24  endof
	$28 swap
    endcase ;

: 2xor ( ud1 ud2 -- ud3 )  rot xor >r xor r> ;

: wurst-crc ( -- xd )
    pad roundse# rounds  \ another key diffusion round
    0. wurst-state state# bounds ?DO  I 2@ 2xor 2 cells +LOOP ;

[IFDEF] nocrypt \ dummy for test
    : encrypt-buffer  ( addr u n -- addr' 0 )  drop + 0 ;
    : wurst-outbuf-encrypt drop ;
    : wurst-inbuf-decrypt drop true ;
[ELSE]
    : encrypt-buffer ( addr u n -- addr 0 ) >r
	over roundse# rounds
	BEGIN  dup 0>  WHILE
		over r@ rounds  r@ >reads state# * safe/string
	REPEAT  rdrop ;
    
    : wurst-outbuf-encrypt ( flag -- )
	wurst-outbuf-init
	outbuf body-size mem-rounds# >r
	outbuf packet-data r@ encrypt-buffer
	rdrop drop wurst-crc rot 2! ;

    : wurst-inbuf-decrypt ( flag1 -- flag2 )
	\G flag1 is true if code, flag2 is true if decrypt succeeded
	wurst-inbuf-init
	inbuf body-size mem-rounds# >r
	inbuf packet-data
	over roundse# rounds
	BEGIN  dup 0>  WHILE
		over r@ rounds-decrypt  r@ >reads state# * safe/string
	REPEAT
	rdrop drop 2@ wurst-crc d= ;
[THEN]

\ public key encryption

\ these are dummy keys for testing!!!

$20 Constant keysize \ our shared secred is only 32 bytes long
\ server keys
Create pks
$21982058BCCB3476. 64, $36623B3840D9F393. 64, $B4B038E18F007E95. 64, $79CAED9D9F043F9B. 64,
Create sks
$EFDA8C1AE4F04358. 64, $4320CCB35C5F6C27. 64, $CE16D65418EA8575. 64, $127701E350CC537F. 64,
\ client keys
keysize buffer: pkc
keysize buffer: skc
\ shared secred
keysize buffer: keypad
Variable do-keypad

\ the theory here is that sks*pkc = skc*pks
\ we send our public key and know the server's public key.

: >wurst-key-ivs ( -- )
    j^ dup 0= IF
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
    r@ dest-ivsgen @ >wurst-source-state
    r@ dest-ivs $@
    r@ dest-ivslastgen @ IF  dup 2/ safe/string  ELSE  2/  THEN
    2dup erase
    dup mem-rounds# encrypt-buffer 2drop
    r@ dest-ivsgen @ wurst-source-state>
    -1 r> dest-ivslastgen xor! ;

: gen-ivs ( ivs-addr -- ) >r
    r@ $@ erase
    r@ $@ dup 2/ mem-rounds# encrypt-buffer 2drop
    r> cell+ @ wurst-source-state> ;

: regen-ivs-all ( map -- ) >r
    r@ dest-ivsgen @ >wurst-source-state
\    wurst-source state# 2* dump
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
    state# <> abort" 64 byte ivs!" >wurst-source'
    r> gen-ivs ;

: ivs-size@ ( map -- n addr ) $@ drop >r
    r@ dest-size @ max-size^2 rshift r> dest-ivs ;

: net2o:gen-data-ivs ( addr u -- )
    j^ data-map ivs-size@ ivs-string ;
: net2o:gen-code-ivs ( addr u -- )
    j^ code-map ivs-size@ ivs-string ;
: net2o:gen-rdata-ivs ( addr u -- )
    j^ data-rmap ivs-size@ ivs-string ;
: net2o:gen-rcode-ivs ( addr u -- )
    j^ code-rmap ivs-size@ ivs-string ;

: set-key ( addr -- )
    keysize 2* j^ crypto-key $!
    \ double key to get 512 bits
    j^ crypto-key $@ 2/ 2dup + swap move
    ( ." set key to:" j^ crypto-key $@ dump ) ;

: net2o:receive-key ( addr u -- )
    keysize <> abort" key+pubkey: expected 32 bytes"
    pkc keysize move
    keypad sks pkc crypto_scalarmult_curve25519 ;

: net2o:send-key ( pks -- pkc-addr u )
    keypad skc rot crypto_scalarmult_curve25519
    pkc keysize  do-keypad on ;

: update-key ( -- )
    do-keypad @ IF
	keypad set-key
	do-keypad off
    THEN ;

