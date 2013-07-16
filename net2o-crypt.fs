\ symmetric encryption and decryption

256 buffer: key-assembly
: >wurst-key ( addr u -- )
    key-assembly state# + state# bounds DO
	2dup I swap move
    dup +LOOP  2drop
    key-assembly key( ." >wurst-key " dup .64b ." :" dup state# + .64b cr )
    >c:key ;
: >wurst-source' ( addr -- )  key-assembly state# move ;
: >wurst-source ( addr u -- )
    key-assembly state# bounds DO
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
    dest-addr 64@ 64>n o 2@ >r - dup r> u<
    IF
	max-size^2 1- rshift key( ." ivsc# " dup . cr )
	dest-ivs $@ drop over +
	swap regen-ivs o> key( ." ivs>code-s? " dup .64b ." :" dup state# + .64b cr )
	>c:key
	EXIT
    THEN
    drop o> ;

: ivs>source? ( addr -- )
\    dup @ 0= IF  drop  EXIT  THEN
    @ >o
    dest-addr @ o 2@ >r - dup r> u<
    IF
	max-size^2 1- rshift key( ." ivss# " dup . cr )
	dest-ivs $@ drop + o> key( ." ivs>source? " dup .64b ." :" dup state# + .64b cr )
	>c:key
	EXIT
    THEN
    drop o> ;

: wurst-key$ ( -- addr u )
    o 0= IF
	wurst-key state#
    ELSE
	crypto-key $@
    THEN ;

: default-key ( -- )
    c:key@ 0= IF
	key( ." Default-key " cr )
	rnd-init >wurst-source'
	wurst-key$ >wurst-key
    THEN ;

: wurst-outbuf-init ( flag -- )
    0 c:key!
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
    key( ." outbuf-init " c:key@ .64b ." :" c:key@ state# + .64b cr ) ;

: wurst-inbuf-init ( flag -- )
    0 c:key!
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
    key( ." inbuf-init " c:key@ .64b ." :" c:key@ state# + .64b cr ) ;

state# buffer: mykey \ server's private key
rng$ mykey swap move

: wurst-key-init ( addr u key u -- addr' u' ) 2>r
    over mykey-salt# >wurst-source
    2r> >wurst-key 
    mykey-salt# safe/string
    c:diffuse ;

: wurst-key-setup ( addr u key u -- addr' u' )
    2>r over >r  rng@ rng@ r> 128! 2r> wurst-key-init ;

: wurst-encrypt$ ( addr u -- ) +calc
    mykey state# wurst-key-setup 2 64s - c:encrypt+auth +enc ;

: encrypt$ ( addr u1 key u2 -- )
    wurst-key-setup 2 64s - c:encrypt+auth ;

: wurst-decrypt$ ( addr u -- addr' u' flag ) +calc $>align
    mykey state# wurst-key-init 2 64s - 2dup c:decrypt+auth +enc ;

: wurst-outbuf-encrypt ( flag -- ) +calc
    wurst-outbuf-init
    outbuf packet-data +cryptsu c:encrypt+auth +enc ;

: wurst-inbuf-decrypt ( flag1 -- flag2 ) +calc
    \G flag1 is true if code, flag2 is true if decrypt succeeded
    wurst-inbuf-init
    inbuf packet-data +cryptsu c:decrypt+auth +enc ;

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
    o 0= IF
	crypt( ." IVS generated for non-connection!" cr )
	wurst-key state#
    ELSE
	do-keypad @ IF
	    keypad keysize
	ELSE
	    crypto-key $@
	THEN
    THEN  >wurst-key ;

: regen-ivs/2 ( -- )
    c:key@ >r
    dest-ivsgen @ key( ." regen-ivs/2 " dup .64b ." :" dup state# + .64b cr ) c:key!
    clear-replies
    dest-ivs $@ dest-a/b c:prng
    -1 dest-ivslastgen xor! r> c:key! ;

: regen-ivs-all ( o:map -- )  c:key@ >r
    dest-ivsgen @ key( ." regen-ivs " dup .64b ." :" dup state# + .64b cr ) c:key!
    dest-ivs $@ c:prng r> c:key! ;

: regen-ivs-part ( new-back -- )  c:key@ >r
    dest-ivsgen @ key( ." regen-ivs " dup .64b ." :" dup state# + .64b cr ) c:key!
    dest-back @ - addr>keys >r
    dest-ivs $@ dest-back @ dest-size @ 1- and
    addr>keys /string r@ umin dup >r c:prng
    dest-ivs $@ r> r> - umin c:prng
    r> c:key! ;

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

: ivs-string ( addr u map -- )  c:key@ >r
    @ >o dest-size @ addr>keys dest-ivs o o> >r >r r@ $!len
    state# <> !!ivs!! >wurst-source' >wurst-key-ivs c:diffuse
    r> $@ c:prng
    r> >o dest-ivsgen @ c:key> o>
    r> c:key! ;

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
