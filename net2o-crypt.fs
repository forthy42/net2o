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

\ key related  constants

64 Constant state#
state# 2* Constant state2#
KEYBYTES Constant keysize \ our shared secred is only 32 bytes long
\ specify strength (in bytes), not length! length is 2*strength
32 Constant hash#128 \ 128 bit hash strength is enough!
64 Constant hash#256 \ 256 bit hash strength is more than enough!
\ Hash state variables

$41 Constant sigonlysize#
$51 Constant sigsize#
$71 Constant sigpksize#
$91 Constant sigpk2size#
$10 Constant datesize#

\ key storage stuff
$1E0 Constant keypack#
keypack# key-salt# + key-cksum# + Constant keypack-all#
key-salt# key-cksum# + Constant wrapper#

Variable my-0key

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
    1 64s     uvar last-mykey
    keysize   uvar stpkc \ server temporary keypair - once per connection setup
    keysize   uvar stskc
    keypack-all# uvar keypack-d
    $100      uvar vaultkey \ buffers for vault
    state2#   uvar vkey \ maximum size for session key
    keysize   uvar keygendh
    keysize   uvar vpk
    keysize   uvar vsk
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

: new-keybuf ( -- )
    keybuf-c >osize @ kalloc keybuf ! ;

: init-keybuf ( -- )
    keytmp @ IF
	up@ keytmp-up @ <> IF  BUT  THEN
	keytmp-c >osize @ kalloc keytmp !
    THEN
    keybuf @ ?EXIT  new-keybuf ; \ we have only one global keybuf

init-keybuf

#10.000.000.000 d>64 64Value delta-mykey# \ new mykey every 10 seconds

: init-mykey ( -- )
    ticks delta-mykey# 64+ last-mykey 64!
    mykey oldmykey state# move
    state# rng$ mykey swap move
    genkey( ." mykey: " mykey state# xtype cr ) ;

: ?new-mykey ( -- )
    last-mykey 64@ ticker 64@ 64- 64-0< IF  init-mykey  THEN ;

: >crypt-key ( addr u -- ) key( dup . )
    dup 0= IF  2drop no-key state#  THEN
    key-assembly state# + state# move-rep
    key-assembly key( ." >crypt-key " dup state2# xtype cr )
    >c:key ;
: >crypt-source ( addr u -- )
    key-assembly state# move-rep ;

\ regenerate ivs is a buffer swapping function:
\ regenerate half of the ivs per time, when you reach the middle of the other half
\ of the ivs buffer.

: dest-a/b ( addr u -- addr1 u1 )
    2/  dest-ivslastgen @ 1 = IF  dup >r + r>  THEN ;

: clear-replies ( -- )
    dest-replies @ dest-size @ addr>replies dest-a/b
    reply( ." Clear replies " over hex. dup hex. cr )
    erase ;

: crypt-key$ ( -- addr u )
    o 0= IF  no-key state#  ELSE  crypto-key sec@  THEN ;

: default-key ( -- )
    cmd( ." Default-key " cr )
    c:0key ;

: addr>assembly ( addr64 flag -- x128 )
    [ acks# invert 8 lshift ]L and n>64 ;

: >ivskey ( 64addr -- keyaddr )
    64>n addr>keys dest-ivs $@ rot umin + ;
: ivs-tweak ( 64addr keyaddr -- )
    >r dest-flags w@ addr>assembly r> state# c:tweakkey! ;

: ivs>source? ( o:map -- )
    o 0=  dest-flags 1+ c@ stateless# and  or IF  default-key  EXIT  THEN
    dest-addr 64@ dest-vaddr 64@ 64-
    64dup dest-size @ n>64 64u>= !!inv-dest!!
    64dup 64dup >ivskey ivs-tweak 64>n addr>keys regen-ivs ;

: crypt-buf-init ( map -- ) >r
    o IF  r@ .ivs>source?  ELSE  default-key  THEN
    ( cmd( ." key: " c:key@ c:key# xtype cr ) rdrop ;

: crypt-key-init ( addr u key u -- addr' u' ) 2>r
    over 128@ 2r> c:tweakkey!
    key-salt# safe/string
    key( ." key init: " c:key@ c:key# .nnb cr ) ;

: crypt-key-setup ( addr u1 key u2 -- addr' u' )
    2>r over >r  rng128 64over 64over r> 128! 2r> c:tweakkey!
    key-salt# safe/string ;

: encrypt$ ( addr u1 key u2 -- )
    crypt-key-setup  key-cksum# - 0 c:encrypt+auth ;

: decrypt$ ( addr u1 key u2 -- addr' u' flag )
    crypt-key-init key-cksum# - 2dup 0 c:decrypt+auth ;

\ passphraese encryption needs to diffuse a lot after mergin in the salt

: crypt-pw-setup ( addr u1 key u2 n -- addr' u' n' ) { n }
    2>r over >r  rng128 r@ 128!
    r@ c@ n $F0 mux r> c! 2r> crypt-key-init $100 n 2* lshift ;

: pw-diffuse ( diffuse# -- )
    -1 +DO  c:diffuse  LOOP ; \ just to waste time ;-)
: pw-setup ( addr u -- diffuse# )
    \G compute between 256 and ridiculously many iteratsions
    drop c@ $F and 2* $100 swap lshift ;

: encrypt-pw$ ( addr u1 key u2 n -- )
    crypt-pw-setup  pw-diffuse  key-cksum# - 0 c:encrypt+auth ;

: decrypt-pw$ ( addr u1 key u2 -- addr' u' flag )  2over pw-setup >r
    crypt-key-init   r> pw-diffuse key-cksum# - 2dup 0 c:decrypt+auth ;

\ encrypt with own key

: mykey-encrypt$ ( addr u -- ) +calc mykey state# encrypt$ +enc ;

: mykey-decrypt$ ( addr u -- addr' u' flag )
    +calc 2dup $>align mykey state# decrypt$
    IF  +enc 2nip true  EXIT  THEN  2drop
    $>align oldmykey state# decrypt$ +enc ;

: outbuf-encrypt ( map -- ) +calc
    crypt-buf-init outbuf packet-data +cryptsu
    outbuf 1+ c@ c:encrypt+auth +enc ;

: inbuf-decrypt ( map -- flag ) +calc
    crypt-buf-init inbuf packet-data +cryptsu
    inbuf 1+ c@ c:decrypt+auth +enc ;

: set-0key ( tweak128 keyaddr u -- )
    dup 0= IF  2drop no-key state#  THEN
\    ." 0key: " ivs-assembly state# 2* 85type cr
    c:tweakkey! ;

: try-0decrypt ( addr -- flag ) >r
    inbuf addr 64@ inbuf flags w@ addr>assembly
    r> sec@ set-0key
    inbuf packet-data tmpbuf swap 2dup 2>r $10 + move
    2r> +cryptsu
    inbuf 1+ c@ c:decrypt+auth +enc
    dup IF  tmpbuf inbuf packet-data move  THEN ;

: inbuf0-decrypt ( -- flag ) +calc
    my-0key try-0decrypt dup IF  EXIT  THEN  drop
    false [: try-0decrypt or dup 0= ;] search-0key ;

: outbuf0-encrypt ( -- ) +calc
    outbuf addr 64@ outbuf flags w@ addr>assembly
    o IF  dest-0key  ELSE  my-0key  THEN  sec@ set-0key
    outbuf packet-data +cryptsu
    outbuf 1+ c@ c:encrypt+auth +enc ;

\ IVS

Variable do-keypad
Sema regen-sema

: keypad$ ( -- addr u )
    do-keypad sec@ dup 0= IF  2drop  crypto-key sec@  THEN ;

: >crypt-key-ivs ( -- )
    o 0= IF  no-key state#  ELSE  keypad$  THEN
    crypt( ." ivs key: " 2dup .nnb cr )
    >crypt-key ;

: regen-ivs/2 ( -- )
    [: c:key@ >r
	dest-ivsgen @ kalign reply( ." regen-ivs/2 " dup c:key# .nnb cr ) c:key!
	clear-replies
	dest-ivs $@ dest-a/b c:prng
	2 dest-ivslastgen xor! r> c:key! ;]
    regen-sema c-section  ;

: regen-ivs-all ( o:map -- ) [: c:key@ >r
      dest-ivsgen @ kalign key( ." regen-ivs " dup c:key# .nnb cr ) c:key!
      dest-ivs $@ c:prng r> c:key! ;]
    regen-sema c-section ;

: rest+ ( addr u -- addr u )
    dest-ivsrest $@len IF
	2dup dest-ivsrest $@ rot umin >r swap r@ move
	r@ safe/string
	dest-ivsrest 0 r> $del
    THEN ;

: rest-prng ( addr u -- )
    rest+
    2dup dup keccak#max negate and safe/string 2>r
    keccak#max negate and c:prng
    2r> dup IF
	keccak#max dest-ivsrest $!len  dest-ivsrest $@ c:prng
	rest+
    THEN  2drop ;

: regen-ivs-part ( new-back -- )
    [: c:key@ >r
      dest-ivsgen @ kalign
      key( ." regen-ivs-part " dest-back @ hex. over hex. dup c:key# .nnb cr )
      regen( ." regen-ivs-part " dest-back @ hex. over hex. dup c:key# .nnb cr )
      c:key!
      dest-back @ U+DO
	  I I' fix-size dup { len }
	  addr>keys >r addr>keys >r dest-ivs $@ r> safe/string r> umin
	  rest-prng
      len +LOOP
      key( ." regen-ivs-part' " dest-ivsgen @ kalign c:key# .nnb cr )
      regen( ." regen-ivs-part' " dest-ivsgen @ kalign c:key# .nnb cr )
      r> c:key! ;] regen-sema c-section ;

: (regen-ivs) ( offset o:map -- )
    dest-ivs $@len 2/ 2/ / dest-ivslastgen @ =
    IF	regen-ivs/2  THEN ;
' (regen-ivs) code-class to regen-ivs
' (regen-ivs) rcode-class to regen-ivs

: one-ivs ( addr -- )
    @ >o c:key@ >r
    key-assembly state2# c:prng
    dest-ivsgen @ kalign c:key!  key-assembly >c:key
    dest-size @ addr>keys dest-ivs $!len
    dest-ivs $@ c:prng
    r> c:key! o> ;

: clear-keys ( -- )
    crypto-key sec-off  tskc KEYBYTES erase  stskc KEYBYTES erase ;

\ We generate a shared secret out of three parts:
\ 64 bytes IV, 32 bytes from the one-time-keys and
\ 32 bytes from the permanent keys

$60 Constant rndkey#

: receive-ivs ( -- )
    genkey( ." ivs key: " c:key@ c:key# over rndkey# xtype cr
            ." con key: " rndkey# /string xtype cr )
    code-map one-ivs   code-rmap one-ivs
    data-map one-ivs   data-rmap one-ivs
    clear-keys ;

: send-ivs ( -- )
    genkey( ." ivs key: " c:key@ c:key# over rndkey# xtype cr
            ." con key: " rndkey# /string xtype cr )
    code-rmap one-ivs  code-map one-ivs
    data-rmap one-ivs  data-map one-ivs
    clear-keys ;

: ivs-strings ( addr u -- )
    dup state# <> !!ivs!! >crypt-source >crypt-key-ivs ;

\ public key encryption

\ the theory here is that pkc*sks = pks*skc
\ because pk=base*sk, so base*skc*sks = base*sks*skc
\ base and pk are points on the curve, sk is a skalar
\ we send our public key and query the server's public key.
: >sksig ( -- )
    c:0key pkc $60 c:hash sksig $20 keccak> ;
: gen-keys ( -- )
    \G generate revocable keypair
    sk1 pk1 ed-keypair \ generate first keypair
    skrev pkrev ed-keypair \ generate keypair for recovery
    sk1 pkrev skc pkc ed-keypairx \ generate real keypair
    genkey( ." gen key: " skc keysize .85warn pkc keysize .85info cr )
    >sksig ;
: check-rev? ( -- flag )
    \G check generated key if revocation is possible
    skrev pkrev sk>pk pkrev dup sk-mask pk1 keypad ed-dh pkc keysize str= ;
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

Defer check-key \ check if we know that key
Defer search-key \ search if that is one of our pubkeys

: key-stage2 ( pk sk -- ) >r
    keypad$ keysize <> !!no-tmpkey!!
    r> rot keypad ed-dhx do-keypad sec+! ;
: key-rest ( addr u sk -- ) >r
    ?keysize dup keysize [: check-key ;] $err
    dup keysize pubkey $! r> key-stage2 ;
: net2o:receive-key ( addr u -- )
    o 0= IF  2drop EXIT  THEN  pkc keysize mpubkey $! skc key-rest ;
: net2o:keypair ( pkc uc pk u -- )
    o 0= IF  2drop EXIT  THEN
    2dup mpubkey $! ?keysize search-key key-rest ;
: net2o:receive-tmpkey ( addr u -- )  ?keysize \ dup keysize .nnb cr
    o 0= IF  gen-stkeys stskc  ELSE  tskc  THEN \ dup keysize .nnb cr
    swap keypad ed-dh
    o IF  do-keypad sec!  ELSE  2drop  THEN
    ( keypad keysize .nnb cr ) ;

: tmpkey@ ( -- addr u )
    do-keypad sec@ dup ?EXIT  2drop
    keypad keysize ;

: net2o:update-key ( -- )
    do-keypad sec@ dup IF
	key( ." store key, o=" o hex. 2dup .nnb cr ) crypto-key sec!
	do-keypad sec-off
	EXIT
    THEN
    2drop ;

\ signature stuff

\ Idea: set "r" first half to the value, "r" second half to the key, diffuse
\ we use explicitely Keccak here, this needs to be globally the same!
\ Keyed hashs are there for unique handles

: >keyed-hash ( valaddr uval keyaddr ukey -- )
    \G generate a keyed hash: keyaddr ukey is the key for hasing valaddr uval
    hash( ." hashing: " 2over 85type ':' emit 2dup 85type cr )
    c:hash c:hash
    hash( @keccak 200 85type cr cr ) ;

: keyed-hash#128 ( valaddr uval keyaddr ukey -- hashaddr uhash )
    c:0key >keyed-hash  keyed-hash-out hash#128 2dup keccak> ;
: keyed-hash#256 ( valaddr uval keyaddr ukey -- hashaddr uhash )
    c:0key >keyed-hash  keyed-hash-out hash#256 2dup keccak> ;

\ signature printing

: now>never ( -- )  ticks sigdate 64! 64#-1 sigdate 64'+ 64! ;
: forever ( -- )  64#0 sigdate 64! 64#-1 sigdate 64'+ 64! ;
: now+delta ( delta64 -- )  ticks 64dup sigdate 64! 64+ sigdate 64'+ 64! ;

: startdate@ ( addr u -- date ) + sigsize# - 64@ ;
: enddate@ ( addr u -- date ) + sigsize# - 64'+ 64@ ;

: .check ( flag -- ) '✔' '✘' rot select xemit ;
: .sigdate ( tick -- )
    64dup 64#0  64= IF  64drop ." forever"  EXIT  THEN
    64dup 64#-1 64= IF  64drop ." never"    EXIT  THEN
    ticks 64over 64- 64dup #60.000.000.000 d>64 64u< IF
	64>f -1e-9 f* 10 6 0 f.rdp 's' emit 64drop
    ELSE  64drop .ticks  THEN ;
: .sigdates ( addr u -- )
    space 2dup startdate@ .sigdate ." ->" enddate@ .sigdate ;

\ signature verification

: +date ( addr -- )
    datesize# "date" >keyed-hash ;
: >date ( addr u -- addr u )
    2dup + sigsize# - +date ;
: gen>host ( addr u -- addr u )
    2dup c:0key "host" >keyed-hash ;

#10.000.000.000 d>64 64Constant fuzzedtime# \ allow clients to be 10s off

: check-date ( addr u -- addr u flag )
    2dup + 1- c@ keysize = &&
    2dup + sigsize# - >r
    ticks fuzzedtime# 64+ r@ 64@ r> 64'+ 64@
    64dup 64#-1 64<> IF  fuzzedtime# 64-2* 64+  THEN
    64within ;
: verify-sig ( addr u pk -- addr u flag )  >r
    check-date IF
	2dup + sigonlysize# - r> ed-verify
	EXIT  THEN
    rdrop false ;
: date-sig? ( addr u pk -- addr u flag )
    >r >date r> verify-sig ;
: pk-sig? ( addr u -- addr u' flag )
    dup sigpksize# u< !!unsigned!!
    2dup sigpksize# - c:0key 2dup c:hash + date-sig? ;
: pk2-sig? ( addr u -- addr u' flag )
    dup sigpk2size# u< !!unsigned!!
    2dup sigpk2size# - + >r c:0key 2dup sigsize# - c:hash r> date-sig? ;
: .sig ( -- )
    sigdate +date sigdate datesize# type
    sksig skc pkc ed-sign type keysize emit ;
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
