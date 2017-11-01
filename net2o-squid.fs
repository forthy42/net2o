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

: search-key-prefix ( l1 -- )
    sk0 gen-sk  sk0 pk0 sk>pk
    pk0 pk~  pkt0 pk0 ge25519-unpack- drop
    BEGIN  dup pk0 l@ <> WHILE  next-key 8 u>64 sk0 64+!
	    dup $FFFF and pk0 w@ = IF  '.' emit  THEN
    REPEAT ;

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
