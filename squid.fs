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

false Value sink?

scope{ net2o-base

\g 
\g ### payment commands ###
\g 

cmd-table $@ inherit-table pay-table

$20 net2o: pay-source ( $:source -- ) \g source, pk[+hash] for lookup
    \ existing sources always had a previous transaction
    \ new sources have only a pk and can only become a sink
    $> pay:source  false to sink? ;
+net2o: pay-sink ( n $:sig -- ) \g sink, signature
    \ sink that already exists as source number n in the contract
    64>n $> pay:sink  true to sink? ;
+net2o: pay-asset ( asset -- ) \g select global asset type
    64>n pay:asset  false to sink? ;
+net2o: pay-obligation ( $:enc-asset -- ) \g select per-contract obligation
    \ encrypted with the receiver's pubkey
    $> pay:obligation  false to sink? ;
+net2o: pay-amount ( 64amount -- ) \g add/subtract amount to current asset
    64>128 pay:amount  false to sink? ;
+net2o: pay-damount ( 128amount -- ) \g add/subtract 128 bit amount
    pay:amount  false to sink? ;
+net2o: pay-comment ( $:enc-comment -- ) \g comment, encrypted for selected key
    $> pay:comment  false to sink? ;
+net2o: pay-balance ( u -- ) \g select&balance asset
    \ a balance modifies the asset of the current active source
    64>n pay:balance  false to sink? ;
+net2o: pay-#source ( u -- ) \g select source
    64>n pay:#source  false to sink? ;

pay-table $save

}scope

\g 
\g ### Contracts ###
\g
\g Contracts are state changes to wallets.  A serialized wallet is a contract
\g that contains all the changes from an empty wallet to fill it; it is not
\g checked for balance.
\g
\g A dumb contract is checked for balance.  It consists of several selectors
\g (source/account, asset), transactions (amounts added or subtracted from an
\g asset), comments (encoded for the receiver, with a ephermeral pubkey as
\g start and a HMAC as end). Comments are fixed 64 bytes, either plain text or
\g hashes to files.  Transactions have to balance, which is facilitated with
\g the balance command, which balances the selected asset.
\g
\g The signature of a contract signs the wallet's state (serialized in
\g normalized form) after the contract has been executed.  The current
\g contract's hash is part of the serialization.

Variable SwapDragonChain# ( "hash" -- "contract" )
Variable SwapDragonKeys#  ( "pk" -- "hash+[asset,amount]*" )
\ Updates go to SwapDragonKeys'#; only one transaction per pk&cycle!
Variable SwapDragonKeys'#  ( "pk" -- "hash+[asset,amount]*" )
Variable $SwapAssets[] ( n -- asset u )

scope{ pay
$10 buffer: balance0
$10 cell+ buffer: new-asset

:noname { d: pk -- } \ pk[+hash]
    pk dup keysize = IF  [: type keysize spaces ;] $tmp  THEN
    sources[] dup $[]# to current-pk $+[]!
    pk key| SwapDragonKeys# #@
    2dup d0<> IF
	pk keysize /string 2over key| str= 0= !!squid-hash!!
	keysize /string
    THEN
    current-pk assets[] $[]!
; pay-class is source

: ?double-transaction ( hash u pk u -- hash u )
     SwapDragonKeys'# #@ 2dup d0= IF
	2drop
    ELSE \ you can check the same transaction twice
	2over str= 0= !!double-transaction!!
    THEN ;

:noname ( n -- )
    dup sources[] $[]# u>= !!inv-index!! to current-pk
; pay-class is #source

:noname ( n addr u -- )
    rot #source
    sigsize# <> !!no-sig!! { sig }
    cmdbuf$ over + sig umin over umax over - 2 - \ cmdbuf up to the sig string
    c:0key 2dup c:hash
    current-pk sources[] $[]@ dup 0= !!sink-cleared!! { d: pk+hash }
    pk+hash keysize /string
    2dup c:hash@ SwapDragonChain# #!
    sig sigsize# pk+hash drop pk-sig? !!sig!! 2drop
    [:  current-pk sources[] $[]@ keysize /string type
	current-pk assets[]  $[]@ type ;] $tmp
    pk+hash key| ?double-transaction
    pk+hash key| SwapDragonKeys'# #!
    current-pk sources[] $[]free
; pay-class is sink

:noname ( n -- )
    dup $SwapAssets[] $[]# u>= !!inv-index!!
    to current-asset
    current-asset balances[] $[]@ nip 0= IF
	balance0 $10 current-asset balances[] $[]!
    THEN
; pay-class is asset

: 128+!? ( 128x addr -- flag )
    dup >r 128@ 128+ r> over >r 128! r> 0< ;

:noname ( 128asset -- )
    64over 64over current-asset balances[] $[]@ drop 128+!? drop
    current-pk assets[] $[]@ bounds U+DO
	I @ current-asset = IF  I cell+ 128+!? !!insufficient-asset!!
	    UNLOOP  EXIT  THEN
    $10 cell+ +LOOP
    dup 0< !!insufficient-asset!!
    current-asset new-asset !  new-asset cell+ 128!
    new-asset $10 cell+ current-pk assets[] $[]+!
; pay-class is amount

:noname ( n -- ) asset
    64#0 64dup current-asset balances[] $[]@ drop 128@ 128- \ just a 128negate
    amount
; pay-class is balance

:noname ( -- )  sink? invert !!not-sunk!!
    balances[] $[]# 0 ?DO
	I balances[] $[]@ balance0 over str= 0= !!not-balanced!!
    LOOP
    sources[] $[]# 0 ?DO
	I sources[] $[]@ nip !!not-sunk!!
    LOOP
; pay-class is finalize

: update ( -- )
    SwapDragonKeys'#
    [: ( last -- ) >r r@ cell+ $@ r@ $@ SwapDragonKeys# #! ;] #map
    SwapDragonKeys'# #frees ;
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
