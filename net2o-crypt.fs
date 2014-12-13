\ symmetric encryption and decryption

\ Copyright (C) 2011-2013   Bernd Paysan

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

64 Constant state#

Variable my-0key

user-o keybuf

state# 2* Constant state2#
KEYBYTES Constant keysize \ our shared secred is only 32 bytes long

object class
    state2# uvar key-assembly
    state2# uvar ivs-assembly
    state2# uvar no-key \ just zeros for no key
    state# uvar mykey \ instance's private key
    state# uvar oldmykey \ previous private key
    
    \ key storage
    \ client keys
    keysize uvar pkc   \ pubkey
    keysize uvar pk1   \ pubkey 1 for revokation
    keysize uvar skc   \ secret key
    keysize uvar sk1   \ secret key 1 for revokation (will not last)
    keysize uvar pkrev \ pubkey for revoking keys
    keysize uvar skrev \ secret for revoking keys
    keysize uvar stpkc \ server temporary keypair - once per connection setup
    keysize uvar stskc
    keysize uvar oldpkc   \ previous pubkey after revocation
    keysize uvar oldskc   \ previous secret key after revocation
    keysize uvar oldpkrev \ previous revocation pubkey after revocation
    keysize uvar oldskrev \ previous revocation secret after revocation
    \ shared secred
    keysize uvar keypad
    1 64s uvar last-mykey
end-class keybuf-c

: init-keybuf ( -- )
    keybuf @ ?EXIT \ we have only one global keybuf
    keybuf-c >osize @ kalloc keybuf ! ;

init-keybuf

#10.000.000.000 d>64 64Value delta-mykey# \ new mykey every 10 seconds

: init-mykey ( -- )
    ticks delta-mykey# 64+ last-mykey 64!
    mykey oldmykey state# move
    state# rng$ mykey swap move
    genkey( ." mykey: " mykey state# xtype cr ) ;

: ?new-mykey ( -- )
    last-mykey 64@ ticker 64@ 64- 64-0< IF  init-mykey  THEN ;

: move-rep ( srcaddr u1 destaddr u2 -- )
    bounds ?DO
	I' I - umin 2dup I swap move
    dup +LOOP  2drop ;

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
    cmd( ." Clear replies " over hex. dup hex. cr )
    erase ;

: crypt-key$ ( -- addr u )
    o 0= IF  no-key state#  ELSE  crypto-key sec@  THEN ;

: default-key ( -- )
    cmd( ." Default-key " cr )
    c:0key ;

: addr>assembly ( addr flag -- )
    [ acks# invert 8 lshift ]L and
    ivs-assembly state# + 64'+ w!
    ivs-assembly state# + 64! ; \ the address is part of the key

User last-ivskey

: ivs>source? ( o:map -- )  o 0= IF  default-key  EXIT  THEN
    dest-addr 64@ dest-vaddr 64@ 64- 64dup dest-size @ n>64 64u<
    IF  64dup dest-flags w@ addr>assembly
	\ the flags, too, except the ack toggle bits
	64>n addr>keys dest-ivs $@ drop over + dup last-ivskey !
	ivs-assembly state# move
	key( ." key: " ivs-assembly state# + 64@ $64. ivs-assembly state# 2* xtype cr )
	ivs-assembly >c:key regen-ivs  EXIT  THEN  64drop
    dest-flags 1+ c@ stateless# and
    IF  default-key  ELSE  true !!inv-dest!!  THEN ;

: crypt-buf-init ( map -- ) >r
    o IF  r@ .ivs>source?  ELSE  default-key  THEN
    ( cmd( ." key: " c:key@ c:key# xtype cr ) rdrop ;

: crypt-key-init ( addr u key u -- addr' u' ) 2>r
    over mykey-salt# >crypt-source
    2r> >crypt-key 
    mykey-salt# safe/string
    key( ." key init: " c:key@ c:key# .nnb cr ) c:diffuse ;

: crypt-key-setup ( addr u1 key u2 -- addr' u' )
    2>r over >r  rng@ rng@ r> 128! 2r> crypt-key-init ;

: encrypt$ ( addr u1 key u2 -- )
    crypt-key-setup  2 64s - 0 c:encrypt+auth ;

: decrypt$ ( addr u1 key u2 -- addr' u' flag )
    crypt-key-init 2 64s - 2dup 0 c:decrypt+auth ;

\ passphraese encryption needs to diffuse a lot after mergin in the salt

: crypt-pw-setup ( addr u1 key u2 n -- addr' u' n' ) { n }
    2>r over >r  rng@ rng@ r@ 128!
    r@ c@ n $F0 mux r> c! 2r> crypt-key-init $100 n 2* lshift ;

: pw-diffuse ( diffuse# -- )
    0 ?DO  c:diffuse  LOOP ; \ just to waste time ;-)
: pw-setup ( addr u -- diffuse# )
    \G compute between 256 and ridiculously many iteratsions
    drop c@ $F and 2* $100 swap lshift ;

: encrypt-pw$ ( addr u1 key u2 n -- )
    crypt-pw-setup  pw-diffuse  2 64s - 0 c:encrypt+auth ;

: decrypt-pw$ ( addr u1 key u2 -- addr' u' flag )  2over pw-setup >r
    crypt-key-init   r> pw-diffuse  2 64s - 2dup 0 c:decrypt+auth ;

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

: set-0key ( keyaddr u -- )
    dup IF
	ivs-assembly state# move-rep
    ELSE
	2drop ivs-assembly state# erase
    THEN
\    ." 0key: " ivs-assembly state# 2* 85type cr
    ivs-assembly >c:key ;

: try-0decrypt ( addr -- flag )  sec@ set-0key
    inbuf packet-data tmpbuf swap 2dup 2>r $10 + move
    2r> +cryptsu
    inbuf 1+ c@ c:decrypt+auth +enc
    dup IF  tmpbuf inbuf packet-data move  THEN ;

: inbuf0-decrypt ( -- flag ) +calc
    inbuf addr 64@ inbuf flags w@ addr>assembly
    my-0key try-0decrypt dup IF  EXIT  THEN  drop
    0 [: ." inbuf0 context " o hex.
	dest-0key try-0decrypt or dup 0= ;] search-context ;

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
    c:key@ >r
    dest-ivsgen @ key( ." regen-ivs/2 " dup c:key# .nnb cr ) c:key!
    clear-replies
    dest-ivs $@ dest-a/b c:prng
    2 dest-ivslastgen xor! r> c:key! ;

: regen-ivs-all ( o:map -- ) [: c:key@ >r
      dest-ivsgen @ key( ." regen-ivs " dup c:key# .nnb cr ) c:key!
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
      dest-ivsgen @
      key( ." regen-ivs-part " dest-back @ hex. over hex. dup c:key# .nnb cr )
      regen( ." regen-ivs-part " dest-back @ hex. over hex. dup c:key# .nnb cr )
      c:key!
      dest-back @ U+DO
	  I I' fix-size dup { len }
	  addr>keys >r addr>keys >r dest-ivs $@ r> safe/string r> umin
	    rest-prng
      len +LOOP
      key( ." regen-ivs-part' " dest-ivsgen @ c:key# .nnb cr )
      regen( ." regen-ivs-part' " dest-ivsgen @ c:key# .nnb cr )
      r> c:key! ;] regen-sema c-section ;

: (regen-ivs) ( offset o:map -- )
    dest-ivs $@len 2/ 2/ / dest-ivslastgen @ =
    IF	regen-ivs/2  THEN ;
' (regen-ivs) code-class to regen-ivs
' (regen-ivs) rcode-class to regen-ivs

: one-ivs ( addr -- )
    @ >o c:key@ >r
    key-assembly state2# c:prng
    dest-ivsgen @ c:key!  key-assembly >c:key
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
: gen-keys ( -- )
    \g generate revocable keypair
    sk1 pk1 ed-keypair \ generate first keypair
    skrev pkrev ed-keypair \ generate keypair for recovery
    sk1 pkrev skc pkc ed-keypairx \ generate real keypair
    genkey( ." gen key: " skc keysize xtype cr ) ;
: check-rev? ( -- flag )
    \g check generated key if revocation is possible
    skrev pkrev sk>pk pkrev dup sk-mask pk1 keypad ed-dh pkc keysize str= ;
: gen-tmpkeys ( -- pk addr ) tskc tpkc ed-keypair tpkc keysize
    genkey( ." tmp key: " tskc keysize xtype cr ) ;
: gen-stkeys ( -- ) stskc stpkc ed-keypair
    genkey( ." tmpskey: " stskc keysize xtype cr ) ;

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
    do-keypad sec@  dup ?EXIT  2drop
    keypad keysize ;

: net2o:update-key ( -- )
    do-keypad sec@ dup IF
	key( ." store key, o=" o hex. 2dup .nnb cr ) crypto-key sec!
	do-keypad sec-off
	EXIT
    THEN
    2drop ;

0 [IF]
Local Variables:
forth-local-words:
    (
     (("debug:" "field:" "sffield:" "dffield:" "64field:") non-immediate (font-lock-type-face . 2)
      "[ \t\n]" t name (font-lock-variable-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
End:
[THEN]
