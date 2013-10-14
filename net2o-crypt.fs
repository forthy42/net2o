\ symmetric encryption and decryption

128 buffer: key-assembly
: >crypt-key ( addr u -- ) key( dup . )
    dup 0= IF  2drop wurst-key state#  THEN
    key-assembly state# + state# bounds DO
	2dup I swap move
    dup +LOOP  2drop
    key-assembly key( ." >crypt-key " dup .64b ." :" dup state# + .64b cr )
    >c:key ;
: >crypt-source' ( addr -- )
    crypt( ." ivs iv: "  dup state# .nnb cr )
    key-assembly state# move ;
: >crypt-source ( addr u -- )
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
    dest-addr 64@ dest-vaddr 64@ 64- 64dup dest-size @ n>64 64u<
    IF
	64>n max-size^2 1- rshift key( ." ivsc# " dup . cr )
	dest-ivs $@ drop over +
	swap regen-ivs o> key( ." ivs>code-s? " dup .64b ." :" dup state# + .64b cr )
	>c:key
	EXIT
    THEN
    64drop o> ;

: ivs>source? ( addr -- )
\    dup @ 0= IF  drop  EXIT  THEN
    @ >o
    dest-addr 64@ dest-vaddr 64@ 64- 64dup dest-size @ n>64 64u<
    IF
	64>n max-size^2 1- rshift key( ." ivss# " dup . cr )
	dest-ivs $@ drop + o> key( ." ivs>source? " dup .64b ." :" dup state# + .64b cr )
	>c:key
	EXIT
    THEN
    64drop o> ;

: wurst-key$ ( -- addr u )
    o 0= IF
	wurst-key state#
    ELSE
	crypto-key $@
    THEN ;

: default-key ( -- )
    c:key@ 0= IF
	key( ." Default-key " cr )
	rnd-init >crypt-source'
	wurst-key$ >crypt-key
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
state# rng$ mykey swap move

: wurst-key-init ( addr u key u -- addr' u' ) 2>r
    over mykey-salt# >crypt-source
    2r> >crypt-key 
    mykey-salt# safe/string
    c:diffuse ;

\ !!TBD!! use a nonce to setup and make sure each such string
\ can be decrypted only once!
: wurst-key-setup ( addr u1 key u2 -- addr' u' )
    2>r over >r  rng@ rng@ r> 128! 2r> wurst-key-init ;

: encrypt$ ( addr u1 key u2 -- )
    wurst-key-setup  2 64s - c:encrypt+auth ;

: decrypt$ ( addr u1 key u2 -- addr' u' flag )
    wurst-key-init 2 64s - 2dup c:decrypt+auth ;

: wurst-encrypt$ ( addr u -- ) +calc mykey state# encrypt$ +enc ;

: wurst-decrypt$ ( addr u -- addr' u' flag )
    +calc $>align mykey state# decrypt$ +enc ;

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
keysize buffer: stpkc \ server temporary keypair - once per connection setup
keysize buffer: stskc
\ shared secred
keysize buffer: keypad
Variable do-keypad "" do-keypad $!

: (gen-keys) { skc pkc -- }
    keysize rng$ skc swap move
    pkc skc base9 crypto_scalarmult ;
\ the theory here is that sks*pkc = skc*pks
\ we send our public key and query the server's public key.
: gen-keys ( -- ) skc pkc (gen-keys) ;
: gen-tmpkeys ( -- ) tskc tpkc (gen-keys)
\    tskc keysize .nnb cr  tpkc keysize .nnb cr cr
;
: gen-stkeys ( -- ) stskc stpkc (gen-keys)
\    stskc keysize .nnb cr  stpkc keysize .nnb cr cr
;

: >crypt-key-ivs ( -- )
    o 0= IF
	crypt( ." IVS generated for non-connection!" cr )
	wurst-key state#
    ELSE
	do-keypad $@ dup 0= IF  2drop
	    crypto-key $@
	THEN
    THEN crypt( ." ivs key: " 2dup .nnb cr )
    >crypt-key ;

: regen-ivs/2 ( -- )
    c:key@ >r
    dest-ivsgen @ key( ." regen-ivs/2 " dup c:key# .nnb cr ) c:key!
    clear-replies
    dest-ivs $@ dest-a/b c:prng
    -1 dest-ivslastgen xor! r> c:key! ;

: regen-ivs-all ( o:map -- )  c:key@ >r
    dest-ivsgen @ key( ." regen-ivs " dup c:key# .nnb cr ) c:key!
    dest-ivs $@ c:prng r> c:key! ;

: regen-ivs-part ( new-back -- )  c:key@ >r
    dest-ivsgen @ key( ." regen-ivs-part " dup c:key# .nnb cr ) c:key!
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
    @ >o dest-ivsgen @ c:key!
    dest-size @ addr>keys dest-ivs o> >r r@ $!len
    state# <> !!ivs!! >crypt-source' >crypt-key-ivs c:diffuse
    r> $@ c:prng
    r> c:key! ;

\ : ivs-key ( addr u map -- )  @ >o  dest-ivsgen @ swap move o> ;

: set-key ( addr -- ) o 0= IF drop  ." key, no context!" cr  EXIT  THEN
    keysize crypto-key $!
    ." set key to:" o crypto-key $@ .nnb cr ;

: ?keysize ( u -- )
    keysize <> !!keysize!! ;

Defer check-key

: net2o:receive-key ( addr u -- )
    o 0= IF  2drop EXIT  THEN
    ?keysize dup keysize check-key
    keypad skc rot crypto_scalarmult
    keypad keysize do-keypad $+! ;
: net2o:receive-tmpkey ( addr u -- )  ?keysize \ dup keysize .nnb cr
    o 0= IF  gen-stkeys stskc  ELSE  tskc  THEN \ dup keysize .nnb cr
    keypad swap rot crypto_scalarmult
    o IF  keypad keysize do-keypad $!  THEN
    ( keypad keysize .nnb cr ) ;

: tmpkey@ ( -- addr u )
    do-keypad $@  dup ?EXIT  2drop
    keypad keysize ;

: net2o:update-key ( -- )
    do-keypad $@ dup IF
	key( ." store key, o=" o hex. 2dup .nnb cr ) crypto-key $!
	"" do-keypad $!
	EXIT
    THEN
    2drop ;
