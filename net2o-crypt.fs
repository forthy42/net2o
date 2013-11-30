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
state# 2* buffer: key-assembly
state# 2* buffer: no-key \ just zeros for no key
state# buffer: mykey \ instance's private key
state# buffer: oldmykey \ previous private key

\ key storage
KEYBYTES Constant keysize \ our shared secred is only 32 bytes long
\ client keys
keysize buffer: pkc
keysize buffer: skc
keysize buffer: stpkc \ server temporary keypair - once per connection setup
keysize buffer: stskc
\ shared secred
keysize buffer: keypad
64Variable last-mykey
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
    key-assembly state# + state# bounds DO
	2dup I swap move
    dup +LOOP  2drop
    key-assembly key( ." >crypt-key " dup state# 2* xtype cr )
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
    dest-ivslastgen @ 1 = IF  dup 2/ safe/string  ELSE  2/  THEN ;

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
	swap regen-ivs o> key( ." ivs>code-s? " dup state# 2* xtype cr )
	>c:key
	rdrop  EXIT
    THEN
    64drop o> ;

: ivs>source? ( addr -- )
\    dup @ 0= IF  drop  EXIT  THEN
    @ >o
    dest-addr 64@ dest-vaddr 64@ 64- 64dup dest-size @ n>64 64u<
    IF
	64>n max-size^2 1- rshift key( ." ivss# " dup . cr )
	dest-ivs $@ drop + o> key( ." ivs>source? " dup state# 2* xtype cr )
	>c:key
	rdrop  EXIT
    THEN
    64drop o> ;

: crypt-key$ ( -- addr u )
    o 0= IF  no-key state#  ELSE  crypto-key $@  THEN ;

: default-key ( -- )
    c:key@ 0= IF
	key( ." Default-key " cr )
	no-key >crypt-source'
	crypt-key$ >crypt-key
    THEN ;

: crypt-outbuf-init ( flag -- )  >r
    0 c:key!
    o IF
	r@ IF
	    code-map ivs>code-source?
	ELSE
	    data-map ivs>source?
	THEN
    ELSE
	drop
    THEN
    default-key
    r> IF  cmd( ." key: " c:key@ c:key# xtype cr )  THEN ;

: crypt-inbuf-init ( flag -- )
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

: crypt-key-init ( addr u key u -- addr' u' ) 2>r
    over mykey-salt# >crypt-source
    2r> >crypt-key 
    mykey-salt# safe/string
    key( ." key init: " c:key@ c:key# .nnb cr ) c:diffuse ;

\ !!TBD!! use a nonce to setup and make sure each such string
\ can be decrypted only once!
: crypt-key-setup ( addr u1 key u2 -- addr' u' )
    2>r over >r  rng@ rng@ r> 128! 2r> crypt-key-init ;

: encrypt$ ( addr u1 key u2 -- )
    crypt-key-setup  2 64s - c:encrypt+auth ;

: decrypt$ ( addr u1 key u2 -- addr' u' flag )
    crypt-key-init 2 64s - 2dup c:decrypt+auth ;

: mykey-encrypt$ ( addr u -- ) +calc mykey state# encrypt$ +enc ;

: mykey-decrypt$ ( addr u -- addr' u' flag )
    +calc 2dup $>align mykey state# decrypt$
    IF  +enc 2nip true  EXIT  THEN  2drop
    $>align oldmykey state# decrypt$ +enc ;

: outbuf-encrypt ( flag -- ) +calc
    crypt-outbuf-init
    outbuf packet-data +cryptsu c:encrypt+auth +enc ;

: inbuf-decrypt ( flag1 -- flag2 ) +calc
    \G flag1 is true if code, flag2 is true if decrypt succeeded
    crypt-inbuf-init
    inbuf packet-data +cryptsu c:decrypt+auth +enc ;

\ IVS

Variable do-keypad "" do-keypad $!

: keypad$ ( -- addr u )
    do-keypad $@ dup 0= IF  2drop  crypto-key $@  THEN ;

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
    dest-ivs $@len 2/ 2/ / dest-ivslastgen @ =
    IF	regen-ivs/2  THEN ;
' (regen-ivs) IS regen-ivs

: one-ivs ( addr -- )  c:key@ >r
    @ >o key-assembly state# 2* c:prng
    dest-ivsgen @ c:key! key-assembly >c:key
    dest-size @ addr>keys dest-ivs $!len
    dest-ivs $@ c:prng o>
    r> c:key! ;

: clear-keys ( -- )
    crypto-key $@ erase  tskc KEYBYTES erase  stskc KEYBYTES erase ;

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
    state# <> !!ivs!! >crypt-source' >crypt-key-ivs ;

\ public key encryption

\ the theory here is that pkc*sks = pks*skc
\ because pk=base*sk, so base*skc*sks = base*sks*skc
\ base and pk are points on the curve, sk is a skalar
\ we send our public key and query the server's public key.
: gen-keys ( -- ) skc pkc ed-keypair
    genkey( ." gen key: " skc keysize xtype cr ) ;
: gen-tmpkeys ( -- pk addr ) tskc tpkc ed-keypair tpkc keysize
    genkey( ." tmp key: " tskc keysize xtype cr ) ;
: gen-stkeys ( -- ) stskc stpkc ed-keypair
    genkey( ." tmpskey: " stskc keysize xtype cr ) ;

\ setting of keys

: set-key ( addr -- ) o 0= IF drop  ." key, no context!" cr  EXIT  THEN
    keysize crypto-key $!
    ." set key to:" o crypto-key $@ .nnb cr ;

: ?keysize ( u -- )
    keysize <> !!keysize!! ;

Defer check-key \ check if we know that key

: net2o:receive-key ( addr u -- )
    o 0= IF  2drop EXIT  THEN
    ?keysize dup keysize check-key
    dup keysize pubkey $!
    skc swap ed-dh 2dup keypad swap move do-keypad $+! ;
: net2o:receive-tmpkey ( addr u -- )  ?keysize \ dup keysize .nnb cr
    o 0= IF  gen-stkeys stskc  ELSE  tskc  THEN \ dup keysize .nnb cr
    swap ed-dh 2dup keypad swap move
    o IF  do-keypad $!  ELSE  2drop  THEN
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
