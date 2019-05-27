\ Interface to the ed25519 primitives from donna     23oct2013py

\ Copyright (C) 2013-2015   Bernd Paysan

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

\ The high level stuff is all in Forth

\ dummy load for Android

require rec-scope.fs
require unix/cpu.fs

fast-lib [IF]
    require ed25519-donnafast.fs
[ELSE]
    [IFDEF] android
	s" libed25519prims.so" c-lib:open-path-lib drop
    [THEN]
    c-library ed25519_donna
	"ed25519prims" add-lib
	include ed25519-donnalib.fs
    end-c-library
[THEN]

[IFUNDEF] class bye [THEN] \ stop here if libcompile only

: 32b>sc25519 32 nb>sc25519 ;
: 64b>sc25519 64 nb>sc25519 ;

$20 Constant KEYBYTES

user-o edbuf

object class
    $60 uvar sigbuf
    $30 uvar sct0
    $30 uvar sct1
    $30 uvar sct2
    $30 uvar sct3
    $C0 uvar get0
    $C0 uvar get1
    $40 uvar hashtmp
    $40 uvar sigtmp
    $20 uvar pktmp
    keccak# uvar hstatetmp
    cell uvar task-id
end-class edbuf-c

: init-ed25519
    edbuf @ IF  task-id @ up@ = ?EXIT  THEN
    [: edbuf-c new edbuf ! ;] crypto-a with-allocater
    up@ task-id ! ;

init-ed25519

: free-ed25519 ( -- )
    edbuf @ ?dup-IF  [: .dispose ;] crypto-a with-allocater  THEN
    edbuf off ;

:noname defers 'image edbuf off ; is 'image

: clean-ed25519 ( -- )
    \g do this every time you computed using something secret
    sct0 task-id over - erase ;

: sk-mask ( sk -- )  dup c@ $F8 and over c!
    $1F + dup c@ $7F and $40 or swap c! ;

: gen-sk ( sk -- ) >r
    \G generate a secret key with the right bits set and cleared
    $20 rng$ r@ swap move r> sk-mask ;

: sk>pk ( sk pk -- )
    \G convert a secret key to a public key
    sct0 rot raw>sc25519
    get0 sct0 ge25519*base
    get0 ge25519-pack clean-ed25519 ;

: ed-keypair ( sk pk -- )
    \G generate a keypair
    over gen-sk sk>pk ;

: ed-keypairx { sk1 pkrev skc pkc -- }
    sct2 sk1 raw>sc25519
    pkrev sk-mask  sct1 pkrev raw>sc25519
    sk1 KEYBYTES erase  pkrev KEYBYTES erase \ things we don't need anymore
    sct2 sct2 sct1 sc25519*
    skc sct2 sc25519>32b
    skc pkc sk>pk ; \ this also cleans up temp stuff

: >hash ( addr u -- )
    \G absorb a short string, perform a hash round
    \G and output 64 bytes to hashtmp
    c:shorthash hashtmp $40 c:hash@ ;

: ed-sign { skh sk pk -- sig u }
    \G sign a message: the keccak state contains the hash of the message.
    c:key@ hstatetmp c:key# move \ we need this twice - move away
    skh $20 >hash                \ gen "random number" from secret to hashtmp
    hstatetmp c:key@ c:key# move \ restore state
    sct3 hashtmp 64b>sc25519     \ sct3 is k
    get0 sct3 ge25519*base       \ get0 is r=k*base
    sigbuf get0 ge25519-pack
    pk sigbuf $20 + $20 move
    sigbuf $40 >hash             \ z=hash(r,pk,message)
    sct1 hashtmp 64b>sc25519     \ sct1 is z
    sct2 sk raw>sc25519          \ sct2 is sk
    sct1 sct1 sct2 sc25519*
    sct1 sct1 sct3 sc25519+      \ s=z*sk+k
    sigbuf $20 + sct1 sc25519>32b
    clean-ed25519 sigbuf $40 ;   \ r,s

UValue no-ed-check?
0 to no-ed-check?

: ed-check? { sig pk -- flag }
    \G check a message: the keccak state contains the hash of the message.
    \G The unpacked pk is in get0, so this word can be used for batch checking.
    \G sig and pk need to be aligned properly, ed-verify does that alignment
    no-ed-check? IF  true  EXIT  THEN
    sig hashtmp $20 move  pk hashtmp $20 + $20 move
    hashtmp $40 c:shorthash hashtmp $40 c:hash@ \ z=hash(r+pk+message)
    sct2 hashtmp 64b>sc25519       \ sct2 is z
    sct3 sig $20 + raw>sc25519     \ sct3 is s
    get1 get0 sct2 sct3 ge25519*+  \ base*s-pk*z
    sigbuf $40 + get1 ge25519-pack \ =r
    sig sigbuf $40 + 32b= ;

: ed-verify ( sig pk -- flag ) \ message digest is in keccak state
    pktmp $20 move  sigtmp $40 move \ align inputs
    get0 pktmp ge25519-unpack- 0=  IF  false EXIT  THEN \ bad pubkey
    sigtmp pktmp ed-check? ;

: ed-quickcheck? { skh sk sig pk -- flag }
    \G quick check a message signed by ourself: the keccak state
    \G contains the hash of the message.
    c:key@ hstatetmp c:key# move \ we need this twice - move away
    skh $20 >hash                \ gen "random number" from secret to hashtmp
    hstatetmp c:key@ c:key# move \ restore state
    sct3 hashtmp 64b>sc25519     \ sct3 is k
    sig hashtmp $20 move  pk hashtmp $20 + $20 move
    hashtmp $40 c:shorthash hashtmp $40 c:hash@ \ z=hash(r+pk+message)
    sct2 hashtmp 64b>sc25519     \ sct2 is z
    sct1 sk raw>sc25519          \ sct1 is sk
    sct1 sct2 sct1 sc25519*      \ sct1 is z*sk
    sct3 sct3 sct1 sc25519+      \ sct3 is s=z*sk+k
    sigbuf $40 + sct3 sc25519>32b
    sigbuf $40 + sig $20 + 32b= ?dup-0=-IF
	\ quick check failed, do slow check
	\ old signatures had a different skh
	sct3 sig $20 + raw>sc25519     \ sct3 is s
	get1 get0 sct2 sct3 ge25519*+  \ base*s-pk*z
	sigbuf $40 + get1 ge25519-pack \ =r
	sig sigbuf $40 + 32b=
    THEN
    clean-ed25519 ;

: ed-quick-verify ( skh sk sig pk -- flag ) \ message digest is in keccak state
    pktmp $20 move  sigtmp $40 move \ align inputs
    get0 pktmp ge25519-unpack- 0=  IF  false EXIT  THEN \ bad pubkey
    sigtmp pktmp ed-quickcheck? ;

: ed-dh { sk pk dest -- secret len }
    pk pktmp $20 move
    get0 pktmp ge25519-unpack- 0= !!no-ed-key!!
    sct2 sk raw>sc25519
    get1 get0 sct2 ge25519*
    dest get1 ge25519-pack
    clean-ed25519 dest $20  $80 dest $1F + xorc! ;

: ed-dhx { offset sk pk dest -- secret len }
    pk pktmp $20 move
    get0 pktmp ge25519-unpack- 0= !!no-ed-key!!
    sct2 sk raw>sc25519
    offset pktmp $20 move
    sct1 pktmp 32b>sc25519
    sct2 sct2 sct1 sc25519*
    get1 get0 sct2 ge25519*
    dest get1 ge25519-pack
    clean-ed25519 dest $20  $80 dest $1F + xorc! ;

\ : ed-dhv { sk pk dest -- secret len }
\     get0 pk ge25519-unpack- 0= !!no-ed-key!!
\     sct2 sk raw>sc25519
\     get1 get0 sct2 ge25519*v
\     dest get1 ge25519-pack
\     clean-ed25519 dest $20  $80 dest $1F + xorc! ;
