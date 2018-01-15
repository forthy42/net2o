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
    2>r 64#0 64dup 2r> walletkey keccak# c:tweakkey!
    walletkey c:key!  c:diffuse ;
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

\ payment handling

\ payments are handled in chat messages.  A payment is essentially
\ a smart contract with some simple functions:
\
\ * source coins which is owned by the sender, will be taken out of the block chain
\ * sink coins (owned by sender or receiver), will be added to the block chain
\ * a contract (which states what things are exchanged), must be valid to process the transaction
\
\ Payments are atomic operations; they can involve more than one asset
\ transfer, but must be embedded within a signed chat message.
\
\ Payment offers are partially, the receiver needs to add a sink coin
\ to get control over the transferred value.
\
\ Exchange contracts may require that the receiver also needs to add
\ a source coin of a different asset type to make the transaction valid.
\ The contract is signed by the source; handed in by the sink
\
\ A coin is a 128 bit big endian number for the value, followed by the asset
\ type string, and the signature of its owner.

scope{ net2o-base

\g 
\g ### payment commands ###
\g 

cmd-table $@ inherit-table pay-table

$20 net2o: pay-source ( $:source -- ) \g source coin, signed by source
    $> 1 !!>=order? pay:source ;
+net2o: pay-sink ( $:remain -- ) \g remain coin, signed by sink
    $> 2 !!>=order? pay:sink ;
+net2o: pay-contract ( $:contract -- ) \g contract, signed by a source
    $> 4 !!>order? pay:contract ;

gen-table $freeze

\g 
\g ### contract commands ###
\g 
\g Contracts are now very simple logic: each contract statement may fail. If
\g it does, the contract is not valid. The time the contract is valid is
\g defined by the signature's time.
\g
\g Example for an exchange contract: “I offer 20 USD and want to receive 5
\g $cams on my account” (with the $cam as traditional deflationary
\g CryptoCurrency used for speculation only).  The contract is only valid, if
\g the source USD account is present, and someone added enough transactions to
\g allow those 5 $scams to be deduced from.  All contracts are execute-once,
\g since their sources must exit, and all contracts have implicit asset
\g transfers by mandating sinks.
\g 
\g Hashes are taken from the signature only.

cmd-table $@ inherit-table contract-table
$20 net2o: ?source ( $:source-hash -- ) \g source must be present
    $> contract:source ;
+net2o: ?sink ( $:sink-hash -- ) \g sink must be present
    $> contract:sink ;

gen-table $freeze
}scope

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
