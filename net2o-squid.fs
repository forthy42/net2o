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
\ * a bracket, which holds source and sink coins together
\
\ Payments are atomic operations; they can involve more than one asset
\ transfer, but must be embedded within a signed chat message.
\
\ Payment offers are partially, the receiver needs to add a sink coin to get
\ control over the transferred value.  BlockChain payments are full.  Active
\ data of a full node is just the coins, not the contracts.
\
\ Exchange contracts may require that the receiver also needs to add a source
\ coin of a different asset type to make the transaction valid.  The contract
\ is signed by the source; handed in by the sink.  For each source, there must
\ be a contract in the transaction to be valid.  All sources and sinks must be
\ required by at least one of the contracts.  All balances must match.  Assets
\ must be in the list of accepted assets of the chain.
\
\ A coin is a 128 bit big endian number for the value, followed by the asset
\ type string, and the signature of its owner.

$10 constant value-size#

scope{ net2o-base

\g 
\g ### payment commands ###
\g 

cmd-table $@ inherit-table pay-table

$20 net2o: pay-source ( $:source -- ) \g source coin, signed by source, final
    $> pay:source ;
+net2o: pay-sink ( $:remain -- ) \g sink coin, signed by sink, final
    $> pay:sink ;
+net2o: pay-bracket ( $:contract -- ) \g bracket, final
    $> pay:bracket ;
+net2o: pay-bracket+ ( $:contract -- ) \g bracket, non-final
    $> pay:bracket+ ;

gen-table $freeze

}scope

\g 
\g ### Contracts ###
\g
\g Contracts are now extremely simple: They just sign the sources and sinks
\g provided.  Every source needs a contract signature by the source owner.
\g All sources and sinks are signed by the contracts in order, including the
\g additional contracts.  Since all sources, sinks and previous contracts are
\g signed, too, hashes are only computed of the signatures (64 bytes), making
\g the hashing easier.  Contract validity is expressed by the start and end
\g date of the contract signature.
\g
\g Example for an exchange bid: “I offer 20 USD and want to receive 5 $cams on
\g my account” (with the $cam as traditional deflationary CryptoCurrency used
\g for speculation only).  The contract is only valid, if the source USD
\g account is present, and someone added another source to allow those 5
\g $scams to be deduced from, and a sink to move those 20 USD to.  All
\g contracts are execute-once, since their sources must exist and will be
\g replaced by the sinks on execution, and all contracts have implicit asset
\g transfers by mandating sinks.  If you want to implement more complex
\g contracts, use intents: The intent is for incomplete transactions, which
\g are completed by a contract signature; this contract signature may evaluate
\g other conditions.
\g

Variable SwapDragonChain#

scope{ pay

: +sigs ( addr u -- )
    + sigonlysize# - sigonlysize# 1- $sigs $+! ;
: >sigs ( addr u -- )
    + sigonlysize# - sigonlysize# 1- $sigs $! ;
: +pks ( addr u -- )
    + sigpksize# - KEYSIZE pks[] $ins[] drop ;
: -pks ( addr u -- )
    + sigpksize# - KEYSIZE pks[] $del[] ;
: ?pk-size ( addr u -- addr u )
    dup sigpksize# u< !!no-sig!! ;
: ?chain-sig ( addr u -- addr' u' )
    \G check for a signature, and append the sig itself to the sigs
    2dup +sigs 2dup +pks
    pk-sig? !!sig!! ;
: ?value-size ( addr u -- addrval uval addrunit uunit )
    dup value-size# u<= !!no-unit!!
    2dup value-size# umin 2swap value-size# /string ;
: ?contract-sig ( addr u -- )
    c:0key $sigs $@ c:hash
    over date-sig? !!sig!! ;
: ?token-exists ( addr-source u -- )
    \G check if the token exists and fail if not
    2dup + sigpksize# - KEYSIZE SwapDragonChain# #@ str= 0= !!no-coin!! ;

:noname ( addr-source u -- )
    ?pk-size
    2dup ?token-exists
    ?chain-sig 2dup sigpksize# + sources[] $+[]!
    ?value-size { d: val }
    val balance-in# #@ dup IF
	drop >r drop be-128@ r@ be-128@ d+ r> be-128!
    ELSE
	val balance-in# #!
    THEN ; pay-class to source
:noname ( addr-source u -- )
    ?pk-size
    ?chain-sig 2dup sigpksize# + sinks[] $+[]!
    ?value-size { d: val }
    val balance-out# #@ dup IF
	drop >r drop be-128@ r@ be-128@ 128+ r> be-128!
    ELSE
	val balance-out# #!
    THEN ; pay-class to sink
:noname ( addr-sig u -- )
    2dup ?contract-sig 2dup >sigs +pks ; pay-class to bracket+
:noname ( addr-sig u -- )
    2dup ?contract-sig 2dup >sigs -pks ; pay-class to bracket

: balance-sum ( hash -- 128sum )
    64#0 64dup r> [: cell+ $@ $10 = IF  be-128@ 128+  ELSE  drop  THEN ;] #map ;

: balance-ok? ( -- flag )
    balance-in# balance-sum  balance-out# balance-sum 128-
    64-0= >r 64-0= r> and ;

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
