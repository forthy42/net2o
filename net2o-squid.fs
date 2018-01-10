\ net2o block chain and cryptographic asset transactions

\ Copyright (C) 2017   Bernd Paysan

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

\ search for a key with a particular pubkey prefix

here kalign here - allot

here $140 erase
here $20 allot here $20 allot here $C0 allot
here $C0 allot
Constant pkt0 Constant bp8
Constant pk0 Constant sk0

: pk~ ( pk -- )  $1F + dup >r c@ $80 xor r> c! ;

ge25519-basepoint bp8 ge25519 move
bp8 bp8 bp8 ge25519+ \ *2
bp8 bp8 bp8 ge25519+ \ *4
bp8 bp8 bp8 ge25519+ \ *8

: next-key ( -- )
    pkt0 pkt0 bp8 ge25519+
    pk0 pkt0 ge25519-pack ;

: search-key-prefix ( l1 mask -- )
    sk0 gen-sk  sk0 pk0 sk>pk
    pk0 pk~  pkt0 pk0 ge25519-unpack- drop
    BEGIN  2dup pk0 be-ul@ and <> WHILE  next-key 8 u>64 sk0 64+!
	    msg( dup $FFFF and pk0 w@ = IF  '.' emit  THEN )
    REPEAT 2drop ;

\ wallet

\ The secret key for a wallet is just 128 bits, so you can write it down
\ The wallet keys are extracted from that secret key through keccak expansion
\ Secret keys generate pubkeys, which are binned found-first in the
\ ledger hypercube.

keccak# buffer: walletkey
$8 Value wallets# \ 8 wallet bits, dummy value for testing
Variable wallet[]

: >walletkey ( addr u -- )
    walletkey keccak# move-rep  walletkey c:key!  c:diffuse ;
: prng>pk ( -- )
    sk0 KEYSIZE c:prng
    sk0 sk-mask  sk0 pk0 sk>pk ;
: wallet-kp[]! ( -- flag )
    pk0 be-ul@ $20 wallets# - rshift
    dup wallet[] $[]@ d0= IF  sk0 KEYSIZE2 rot wallet[] $[]! true
    ELSE  drop false  THEN ;

: wallet-expand ( addr u -- )
    \G take a wallet key, and expand it to the pubkeys
    c:key@ >r
    >walletkey  0
    BEGIN
	prng>pk wallet-kp[]! -
	dup 1 wallets# lshift u>= UNTIL  drop
    r> c:key! ;

: .wallets ( -- ) \G print the wallet pubkeys
    1 wallets# lshift 0 U+DO
	I wallet[] $[]@ KEYSIZE /string over c@ hex. space 85type cr
    LOOP ;

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]
