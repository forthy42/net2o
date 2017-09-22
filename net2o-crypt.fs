\ symmetric encryption and decryption

\ Copyright (C) 2011-2015   Bernd Paysan

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

\ key storage stuff
$1E0 Constant keypack#
keypack# key-salt# + key-cksum# + Constant keypack-all#
key-salt# key-cksum# + Constant wrapper#

Variable my-0key
Variable your-0key

user-o keytmp \ storage for secure temporary keys

object class
    state2#   uvar key-assembly
    state2#   uvar ivs-assembly
    state#    uvar mykey    \ instance's rotating private key
    state#    uvar oldmykey \ previous rotating private key
    keysize   uvar oldpkc   \ previous pubkey after revocation
    keysize   uvar oldskc   \ previous secret key after revocation
    keysize   uvar oldpkrev \ previous revocation pubkey after revocation
    keysize   uvar oldskrev \ previous revocation secret after revocation
    keysize   uvar keypad
    hash#256  uvar keyed-hash-out
    datesize# uvar sigdate
    keysize   uvar stpkc \ server temporary keypair - once per connection setup
    keysize   uvar stskc
    keypack-all# uvar keypack-d
    $100      uvar vaultkey \ buffers for vault
    $100      uvar keydump-buf  \ buffer for dumping keys
    state2#   uvar vkey \ maximum size for session key
    state2#   uvar voutkey \ for keydump
    keysize   uvar keygendh
    keysize   uvar vpk
    keysize   uvar vsk
    1 64s     uvar last-mykey
    cell      uvar keytmp-up
end-class keytmp-c

user-o keybuf \ storage for secure permanent keys

object class
    \ key storage
    \ client keys
    keysize uvar pkc   \ pubkey
    keysize uvar pk1   \ pubkey 1 for revokation
    keysize uvar skc   \ secret key
    keysize uvar sksig \ secret key for signature
    keysize uvar sk1   \ secret key 1 for revokation (will not last)
    keysize uvar pkrev \ pubkey for revoking keys
    keysize uvar skrev \ secret for revoking keys
end-class keybuf-c

state2# buffer: no-key \ just zeros for no key
keysize buffer: qr-key \ key used for QR challenge (can be only one)
state#  buffer: qr-hash \ hash of challenge

: new-keybuf ( -- )
    keybuf-c >osize @ kalloc keybuf ! ;
: new-keytmp ( -- )
    keytmp @ IF
	up@ keytmp-up @ <> IF  BUT  THEN
	keytmp-c >osize @ kalloc keytmp !
	up@ keytmp-up !
    THEN ;

: init-keybuf ( -- )
    keysize rng$ qr-key swap move \ qr-key shall not be guessable
    new-keytmp  new-keybuf ; \ we have only one global keybuf

init-keybuf

:noname keytmp off keybuf off defers 'image ; is 'image
:noname defers 'cold init-keybuf ; is 'cold
:noname defers alloc-code-bufs  new-keytmp ; is alloc-code-bufs
\ :noname defers free-code-bufs ; is free-code-bufs

#10.000.000.000 d>64 64Value delta-mykey# \ new mykey every 10 seconds

: init-mykey ( -- )
    ticks delta-mykey# 64+ last-mykey 64!
    mykey oldmykey state# move
    state# rng$ mykey swap move
    genkey( ." mykey: " mykey state# xtype cr ) ;

: init-my0key ( -- )
    no0key( EXIT ) keysize rng$ my-0key sec! ;

: ?new-mykey ( -- )
    last-mykey 64@ ticker 64@ 64- 64-0< IF  init-mykey  THEN ;

: >crypt-key ( addr u -- ) key( dup . )
    dup 0= IF  2drop no-key state#  THEN
    key-assembly state# + state# move-rep
    key-assembly tweak( ." >crypt-key " dup state2# 85type cr )
    >c:key ;
: >crypt-source ( addr u -- )
    key-assembly state# move-rep ;

\ regenerate ivs is a buffer swapping function:
\ regenerate half of the ivs per time, when you reach the middle of the other half
\ of the ivs buffer.

scope{ mapc

: dest-a/b ( addr u -- addr1 u1 )
    2/  dest-ivslastgen 1 = IF  dup >r + r>  THEN ;

: clear-replies ( -- )
    dest-replies dest-size addr>replies dest-a/b
    reply( ." Clear replies " over hex. dup hex. cr )
    erase ;

: >ivskey ( 64addr -- keyaddr )
    64>n addr>keys dest-ivs$ rot umin + ;

}scope

: crypt-key$ ( -- addr u )
    o 0= IF  no-key state#  ELSE  crypto-key sec@  THEN ;

: default-key ( -- )
    cmd( ." Default-key " cr )
    c:0key ;

: addr>assembly ( addr64 flag -- x128 )
    [ acks# invert 8 lshift ]L and n>64 ;

: ivs-tweak ( 64addr keyaddr -- )
    >r dest-flags le-uw@ addr>assembly
    r> state# c:tweakkey!
    tweak( ." tweak key: " voutkey c:key> voutkey @ hex. voutkey state# + $10 .nnb cr ) ;

scope{ mapc

: ivs>source? ( o:map -- )
    dest-addr 64@ dest-vaddr 64-
    64dup dest-size n>64 64u>= !!inv-dest!!
    64dup 64dup >ivskey ivs-tweak 64>n addr>keys regen-ivs ;

}scope

: key>dump ( -- addr u )
    keydump-buf c:key> keydump-buf c:key# ;

: crypt-key-init ( addr u key u -- addr' u' ) 2>r
    over le-128@ 2r> c:tweakkey!
    key-salt# safe/string
    tweak( ." key init: " key>dump .nnb cr ) ;

: crypt-key-setup ( addr u1 key u2 -- addr' u' )
    2>r over >r  $10 rng$ drop dup r> $10 move le-128@ 2r> c:tweakkey!
    key-salt# safe/string ;

: encrypt$ ( addr u1 key u2 -- )
    crypt-key-setup
    over >r $>align 2dup key-cksum# - 0 c:encrypt+auth
    r> swap move ;

: decrypt$ ( addr u1 key u2 -- addr' u' flag )
    crypt-key-init
    $>align key-cksum# - 2dup 0 c:decrypt+auth ;

\ passphraese encryption needs to diffuse a lot after mergin in the salt

: crypt-pw-setup ( addr u1 key u2 n -- addr' u' n' ) { n }
    2>r over >r  $10 rng$ r@ swap move
    r@ c@ n $F0 mux r> c! 2r> crypt-key-init $100 n 2* lshift ;

: pw-diffuse ( diffuse# -- )
    -1 +DO  c:diffuse  LOOP ; \ just to waste time ;-)
: pw-setup ( addr u -- diffuse# )
    \G compute between 256 and ridiculously many iterations
    drop c@ $F and 2* $100 swap lshift ;

: encrypt-pw$ ( addr u1 key u2 n -- )
    crypt-pw-setup  pw-diffuse  key-cksum# - 0 c:encrypt+auth ;

: decrypt-pw$ ( addr u1 key u2 -- addr' u' flag )  2over pw-setup >r
    crypt-key-init   r> pw-diffuse key-cksum# - 2dup 0 c:decrypt+auth ;

\ encrypt with own key

: mykey-encrypt$ ( addr u -- ) +calc
    mykey state# encrypt$ +enc ;

: mykey-decrypt$ ( addr u -- addr' u' flag )
    +calc 2dup mykey state# decrypt$
    IF  +enc 2nip true  EXIT  THEN  2drop
    oldmykey state# decrypt$ +enc ;

: outbuf-encrypt ( map -- ) +calc
    .mapc:ivs>source? outbuf packet-data +cryptsu
    outbuf 1+ c@ c:encrypt+auth +enc ;

: inbuf-decrypt ( map -- flag ) +calc
    .mapc:ivs>source? inbuf packet-data +cryptsu
    inbuf 1+ c@ c:decrypt+auth +enc ;

: set-0key ( tweak128 keyaddr u -- )
    dup 0= IF  2drop no-key state#  THEN
    cmd0( ." 0key: " 2dup 85type cr )
    c:tweakkey! ;

: try-0decrypt ( addr -- flag ) >r
    inbuf mapaddr le-64@ inbuf hdrflags le-uw@ addr>assembly
    r> sec@ set-0key
    inbuf packet-data tmpbuf swap 2dup 2>r $10 + move
    2r> +cryptsu
    inbuf 1+ c@ c:decrypt+auth +enc
    dup IF  tmpbuf inbuf packet-data move  THEN ;

: inbuf0-decrypt ( -- flag ) +calc
    my-0key try-0decrypt ;

: outbuf0-encrypt ( -- ) +calc
    outbuf mapaddr le-64@ outbuf hdrflags le-uw@ addr>assembly
    o IF  dest-0key  ELSE  your-0key  THEN  sec@ set-0key
    outbuf packet-data +cryptsu
    outbuf 1+ c@ c:encrypt+auth +enc ;

\ IVS

Sema regen-sema

: keypad$ ( -- addr u )
    do-keypad sec@ dup 0= IF  2drop  crypto-key sec@  THEN ;

: >crypt-key-ivs ( -- )
    o 0= IF  no-key state#  ELSE  keypad$  THEN
    crypt( ." ivs key: " 2dup .nnb cr )
    >crypt-key ;

scope{ mapc

: regen-ivs/2 ( -- )
    [: c:key@ >r
	dest-ivsgen kalign reply( ." regen-ivs/2 " dup c:key# .nnb cr ) c:key!
	clear-replies
	dest-ivs$ dest-a/b c:prng ivs( ." Regen A/B IVS" cr )
	2 addr dest-ivslastgen xorc! r> c:key! ;]
    regen-sema c-section  ;

: regen-ivs-all ( o:map -- ) [: c:key@ >r
      dest-ivsgen kalign key( ." regen-ivs " dup c:key# .nnb cr ) c:key!
      dest-ivs$ c:prng ivs( ." Regen all IVS" cr )
      r> c:key! ;]
    regen-sema c-section ;

: rest+ ( addr u -- addr u )
    addr dest-ivsrest$ $@len IF
	2dup dest-ivsrest$ rot umin >r swap r@ move
	r@ safe/string
	addr dest-ivsrest$ 0 r> $del
    THEN ;

: rest-prng ( addr u -- )
    rest+
    2dup dup keccak#max negate and safe/string 2>r
    keccak#max negate and c:prng
    2r> dup IF
	keccak#max addr dest-ivsrest$ $!len  dest-ivsrest$ c:prng
	rest+
    THEN  2drop ;

: regen-ivs-part ( old-back new-back -- )
    [: c:key@ >r
      dest-ivsgen kalign
      regen( ." regen-ivs-part " dest-back hex. over hex. dup c:key# .nnb cr )
      c:key!
      swap U+DO
	  I I' fix-size dup { len }
	  addr>keys >r addr>keys >r dest-ivs$ r> safe/string r> umin
	  rest-prng
      len +LOOP
      regen( ." regen-ivs-part' " dest-ivsgen kalign c:key# .nnb cr )
      r> c:key! ;] regen-sema c-section ;

: (regen-ivs) ( offset o:map -- )
    addr dest-ivs$ $@len 2/ 2/ / dest-ivslastgen =
    IF	tweak( ." regen-ivs/2" cr ) regen-ivs/2  THEN ;
' (regen-ivs) code-class to regen-ivs
' (regen-ivs) rcode-class to regen-ivs

}scope

: one-ivs ( map-addr -- )
    with mapc c:key@ >r
    key-assembly state2# c:prng
    dest-ivsgen kalign c:key!  key-assembly >c:key
    dest-size addr>keys addr dest-ivs$ $!len
    dest-ivs$ c:prng ivs( ." Regen one IVS" cr )
    r> c:key! endwith ;

: clear-keys ( -- )
    crypto-key sec-off  tskc KEYBYTES erase  stskc KEYBYTES erase
    true to key-setup? ;

\ We generate a shared secret out of three parts:
\ 64 bytes IV, 32 bytes from the one-time-keys and
\ 32 bytes from the permanent keys

$60 Constant rndkey#

: receive-ivs ( -- )
    genkey( ." ivs key: " key>dump over rndkey# xtype cr
            ." con key: " rndkey# /string xtype cr )
    ivs( ." regen receive IVS" cr )
    code-map one-ivs   code-rmap one-ivs
    data-map one-ivs   data-rmap one-ivs
    clear-keys ;

: send-ivs ( -- )
    genkey( ." ivs key: " key>dump over rndkey# xtype cr
            ." con key: " rndkey# /string xtype cr )
    ivs( ." regen send IVS" cr )
    code-rmap one-ivs  code-map one-ivs
    data-rmap one-ivs  data-map one-ivs
    clear-keys ;

: ivs-strings ( addr u -- )
    key-setup? !!doublekey!!
    dup state# <> !!ivs!! >crypt-source >crypt-key-ivs ;

\ hash with key and sksig generation

: >keyed-hash ( valaddr uval keyaddr ukey -- )
    \G generate a keyed hash: keyaddr ukey is the key for hasing valaddr uval
    \ hash( ." hashing: " 2over 85type ':' emit 2dup 85type cr )
    c:hash c:hash
    \ hash( @keccak 200 85type cr cr ) \ debuggin may leak secrets!
;

\ public key encryption

\ the theory here is that pkc*sks = pks*skc
\ because pk=base*sk, so base*skc*sks = base*sks*skc
\ base and pk are points on the curve, sk is a skalar
\ we send our public key and query the server's public key.

: gen-keys ( -- )
    \G generate revocable keypair
    sk1 pk1 ed-keypair \ generate first keypair
    skrev pkrev ed-keypair \ generate keypair for recovery
    sk1 pkrev skc pkc ed-keypairx \ generate real keypair
    genkey( ." gen key: " skc keysize .85warn pkc keysize .85info cr )
;
: check-rev? ( pk -- flag )
    \G check generated key if revocation is possible
    >r skrev pkrev sk>pk pkrev dup sk-mask
    r@ keysize + keypad ed-dh r> keysize str= ;
: gen-tmpkeys ( -- ) tskc tpkc ed-keypair
    genkey( ." tmp key: " tskc keysize .85warn tpkc keysize .85info cr ) ;
: gen-stkeys ( -- ) stskc stpkc ed-keypair
    genkey( ." tmpskey: " stskc keysize .85warn stpkc keysize .85info cr ) ;

\ setting of keys

: set-key ( addr -- ) o 0= IF drop  ." key, no context!" cr  EXIT  THEN
    keysize crypto-key sec!
    ." set key to:" o crypto-key sec@ .nnb cr ;

: ?keysize ( u -- )
    keysize <> !!keysize!! ;

Forward check-key \ check if we know that key
Forward search-key \ search if that is one of our pubkeys
Forward search-key? \ search if that is one of our pubkeys

Variable tmpkeys-ls16b
$1000 Value max-tmpkeys# \ no more than 256 keys in queue

: ?repeat-tmpkey ( addr -- )
    tmpkeys-ls16b $@len max-tmpkeys# u>= IF
	tmpkeys-ls16b 0 max-tmpkeys# 2/ $del
    THEN
    tmpkeys-ls16b $@ bounds ?DO
	dup I $10 tuck str= !!repeated-tmpkey!!
    $10 +LOOP
    health( ." non-repeated tmp key " dup $10 85type cr )
    $10 tmpkeys-ls16b $+! ; \ save only half of the tmpkey

: key-stage2 ( pk sk -- ) >r
    keypad$ keysize <> !!no-tmpkey!!
    r> rot keypad ed-dhx do-keypad sec+! ;
: key-rest ( addr u sk -- ) >r
    ?keysize dup keysize check-key
    dup keysize tmp-pubkey $! r> key-stage2
    keypair-val validated or! ;
: net2o:keypair ( pkc uc pk u -- )
    ?keysize search-key swap tmp-my-key ! key-rest ;
: net2o:receive-tmpkey ( addr u -- )  ?keysize \ dup keysize .nnb cr
    o 0= IF  gen-stkeys stskc
	\ repeated tmpkeys are allowed here due to packet duplication
    ELSE  dup ?repeat-tmpkey \ not allowed here, duplicates will be rejected
	tskc  THEN \ dup keysize .nnb cr
    swap keypad ed-dh
    o IF  do-keypad sec!  ELSE  2drop  THEN
    ( keypad keysize .nnb cr ) ;

: tmpkey@ ( -- addr u )
    do-keypad sec@ dup ?EXIT  2drop
    keypad keysize ;

: net2o:update-key ( -- )
    o? do-keypad sec@ dup keysize2 = IF
	key( ." store key, o=" o hex. 2dup .nnb cr )
	crypto-key sec! do-keypad sec-off
	EXIT
    THEN
    2drop ;

\ signature stuff

\ Idea: set "r" first half to the value, "r" second half to the key, diffuse
\ we use explicitely Keccak here, this needs to be globally the same!
\ Keyed hashs are there for unique handles

: keyed-hash#128 ( valaddr uval keyaddr ukey -- hashaddr uhash )
    c:0key >keyed-hash  keyed-hash-out hash#128 2dup keccak> ;
: keyed-hash#256 ( valaddr uval keyaddr ukey -- hashaddr uhash )
    c:0key >keyed-hash  keyed-hash-out hash#256 2dup keccak> ;

\ signature printing

: now>never ( -- )  ticks sigdate le-64! 64#-1 sigdate 64'+ le-64! ;
: forever ( -- )  64#0 sigdate le-64! 64#-1 sigdate 64'+ le-64! ;
: now+delta ( delta64 -- )  ticks 64dup sigdate le-64! 64+ sigdate 64'+ le-64! ;

e? max-xchar $100 < [IF]
    : .check ( flag -- ) 'x' 'v' rot select xemit ;
[ELSE]
    : .check ( flag -- ) '✘' '✔' rot select xemit ;
[THEN]
: .sigdate ( tick -- )
    64dup 64#0  64= IF  64drop .forever  EXIT  THEN
    64dup 64#-1 64= IF  64drop .never    EXIT  THEN
    ticks 64over 64- 64dup #60.000.000.000 d>64 64u< IF
	64>f -1e-9 f* 10 6 0 f.rdp 's' emit 64drop
    ELSE  64drop .ticks  THEN ;
: .sigdates ( addr u -- )
    2dup startdate@ .sigdate ." ->" enddate@ .sigdate ;

\ signature verification

: +date ( addr -- )
    datesize# "date" >keyed-hash ;
: >date ( addr u -- addr u )
    2dup + sigsize# - +date ;
: gen>host ( addr u -- addr u )
    2dup c:0key "host" >keyed-hash ;

#10.000.000.000 d>64 64Constant fuzzedtime# \ allow clients to be 10s off

-5
enum sig-keysize
enum sig-unsigned
enum sig-early
enum sig-late
enum sig-wrong
enum sig-ok
drop

: early/late? ( n64 min64 max64 -- sig-error )
    64>r 64over 64r> 64u>= sig-late and >r 64u< sig-early and r> min ;

: check-date ( addr u -- addr u flag )
    2dup + 1- c@ keysize <> sig-keysize and ?dup-IF  EXIT  THEN
    2dup enddate@ 64>r 2dup startdate@ 64>r
    ticks fuzzedtime# 64+ 64r> 64r>
    64dup 64#-1 64<> IF  fuzzedtime# 64-2* 64+  THEN
    early/late? ;
: verify-sig ( addr u pk -- addr u flag )  >r
    check-date dup 0= IF  drop
	2dup + sigonlysize# - r> ed-verify 0= sig-wrong and
	EXIT  THEN
    rdrop ;
: quick-verify-sig ( addr u pk -- addr u flag )  >r
    check-date dup 0= IF  drop
	2dup + sigonlysize# -
	r@ dup last# >r search-key? r> to last#
	dup 0= IF  nip nip rdrop  EXIT  THEN
	swap .ke-sksig sec@ drop swap 2swap
	ed-quick-verify 0= sig-wrong and
    THEN
    rdrop ;

: date-sig? ( addr u pk -- addr u flag )
    >r >date r> verify-sig ;
: pk-sig? ( addr u -- addr u' flag )
    dup sigpksize# u< IF  sig-unsigned  EXIT  THEN
    2dup sigpksize# - c:0key
    2dup c:hash + date-sig? ;
: pk-quick-sig? ( addr u -- addr u' flag )
    dup sigpksize# u< IF  sig-unsigned  EXIT  THEN
    2dup sigpksize# - c:0key
    2dup c:hash + >r >date r> quick-verify-sig ;
: pk-date? ( addr u -- addr u' flag ) \ check only the date
    dup sigpksize# u< IF  sig-unsigned  EXIT  THEN
    check-date ;
: pk2-sig? ( addr u -- addr u' flag )
    dup sigpk2size# u< IF  sig-unsigned  EXIT  THEN
    2dup sigpk2size# - + >r c:0key 2dup sigsize# - c:hash r> date-sig? ;
: my-key? ( -- o )  o IF  my-key  ELSE  my-key-default  THEN ;
: sig-params ( -- sksig sk pk )
    my-key? ?dup-IF
	>o ke-sksig sec@ drop ke-sk sec@ drop ke-pk $@ drop o>  EXIT
    THEN  !!FIXME!! ( old version ) sksig skc pkc ;
: pk@ ( -- pk u )
    my-key? .ke-pk $@ ;
: sk@ ( -- pk u )
    my-key? .ke-sk sec@ ;
: .sig ( -- )
    sigdate +date sigdate datesize# type
    sig-params ed-sign type keysize emit ;
: .pk ( -- )  pkc keysize type ;
: pk-sig ( addr u -- sig u )
    c:0key c:hash [: .pk .sig ;] $tmp ;

: +sig$ ( addr u -- hostaddr host-u ) [: type .sig ;] $tmp ;
: gen-host ( addr u -- addr' u' )
    gen>host +sig$ ;
: >delete ( addr u type u2 -- addr u )
    "delete" >keyed-hash ;
: gen-host-del ( addr u -- addr' u' )
    gen>host "host" >delete +sig$ ;

0 [IF]
Local Variables:
forth-local-words:
    (
     (("event:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     (("debug:" "field:" "2field:" "sffield:" "dffield:" "64field:" "uvar" "uvalue") non-immediate (font-lock-type-face . 2)
      "[ \t\n]" t name (font-lock-variable-name-face . 3))
     ("[a-z\-0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("event:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]
