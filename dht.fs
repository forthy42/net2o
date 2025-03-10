\ distributed hash table                             16oct2013py

\ Copyright © 2013-2019   Bernd Paysan

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

\ For speed reasons, the DHT is in-memory
\ we may keep a log of changes on disk if we want persistence
\ might not be saved too frequently... robustness comes from distribution
\ This is actually a PHT, a prefix hash tree; base 256 (bytes)

$200 cells Constant dht-size# \ $100 entries + $100 chains

Sema dht-sema

Variable d#public \ root of public dht

: dht@ ( bucket -- addr )  dup
    >r @ 0= IF  dht-size# allocate throw dup r> ! dup dht-size# erase
    ELSE  r> @  THEN ;

: dht-map { dht xt: xt -- }
    dht @ 0= ?EXIT
    dht @ dht-size# 2/ bounds DO
	I @ ?dup-IF  .xt  THEN
    cell +LOOP
    dht @ dht-size# 2/ + dht-size# 2/ bounds DO
	I @ IF  I action-of xt recurse  THEN
    cell +LOOP ;

\ keys are enumerated small integers

0
enum k#hash  
enum k#peers  
enum k#owner  
enum k#host  
enum k#map   
enum k#tags  
\ most stuff is added as tag or tag:value pair
cells Constant k#size

cmd-class class
    field: dht-hash   \ hash itself is item 0
    field: dht-peers  \ distribution list - includes "where did I get this from"
                      \ managed by the hash owner himself
    field: dht-owner  \ owner(s) of the object (pubkey+signature): I own
    field: dht-host   \ network id+routing from there (+signature)
    field: dht-have   \ peers have (parts of) the object (desc+pubkey+signature): I have
    field: dht-tags   \ tags added
end-class dht-class

: dht-off ( o:dht -- o:dht )
    dht-hash $free
    dht-peers $[]free
    dht-owner $[]free
    dht-host $[]free
    dht-have $[]free
    dht-tags $[]free ;

Variable dht-table

\ map primitives
\ map layout: offset, bitmap pairs (64 bits each)
\ string array: starts with base map (32kB per bit)

\ !!TBD!!

\ checks for signatures

: >host ( addr u -- addr u )  dup sigsize# u< !!unsigned!!
    2dup sigsize# - gen>host 2drop ; \ hash from address

: verify-host ( addr u -- addr u flag )
    dht-hash $@ drop date-sig? ;

: revoke? ( addr u -- addr u flag )
    over c@ '!' = and over revsize# = and &&       \ verify size and prefix
    >host verify-host 0= &&                        \ verify it's a proper host
    2dup + sigsize# - sigdate datesize# move       \ copy signing date
    2dup 1 /string sigsize# -                      \ extract actual revoke part
    over "selfsign" revoke-verify &&'              \ verify self signature
    over keysize2 + "revoke" revoke-verify &&'     \ verify revoke signature
    over keysize2 + pkrev keymove
    pkrev dup sk-mask  dht-hash $@ drop keysize +  keypad ed-dh
    dht-hash $@ key| str= nip nip ;        \ verify revoke token

: .revoke ( addr u -- )
    ." new key: " 2dup 1 /string 2dup + 1- c@ 2* umin 85type space
    revoke? -rot space .sigdates .check ;

\ higher level checks

: check-host ( addr u -- addr u )
    over c@ '!' = IF  revoke?  ELSE  >host verify-host  THEN
    !!sig!! ;
: verify-owner ( addr u -- flag )
    2dup sigsize# -
    c:0key [: type dht-hash $@ type ;] $tmp c:hash
    dht-hash $@ drop date-sig? ;
: check-owner ( addr u -- addr u )
    verify-owner !!sig!! ;
: >tag ( addr u -- addr u )
    dup sigpksize# u< !!unsigned!!
    c:0key dht-hash $@ "tag" >keyed-hash
    2dup sigpksize# - c:hash ;
: >have ( addr u -- addr u )
    dup sigpksize# u< !!unsigned!!
    c:0key dht-hash $@ "have" >keyed-hash
    2dup sigpksize# - c:hash ;
: verify-tag ( addr u -- addr u flag )
    2dup + sigpksize# - date-sig? ;
: check-tag ( addr u -- addr u )
    >tag verify-tag !!sig!! ;
: check-have ( addr u -- addr u )
    >have verify-tag !!sig!! ;
: delete-tag? ( addr u -- addr u flag )
    >tag "tag" >delete verify-tag ;
: delete-have? ( addr u -- addr u flag )
    >tag "have" >delete verify-tag ;
: delete-host? ( addr u -- addr u flag )
    >host "host" >delete verify-host ;
: delete-owner? ( addr u -- addr u flag )
    >host "owner" >delete verify-host ;

\ some hash storage primitives

: d#? ( addrkey u bucket -- addr u bucket/0 )
    dup @ 0= ?EXIT
    dup >r @ .dht-hash $@ 2over string-prefix? IF  r> EXIT  THEN
    rdrop false ;

: d# ( addr u hash -- bucket ) { hash }
    2dup bounds ?DO
	I c@ cells hash dht@ + d#? ?dup-IF
	    nip nip UNLOOP  EXIT  THEN
	I c@ $100 + cells hash dht@ + to hash
    LOOP  true !!dht-full!! ;

dht-class ' new static-a with-allocater constant dummy-dht

: >d#id ( addr u -- o )
    [: 2dup d#public d#
      dup @ 0= IF
	  over $40 = IF  dht-table dht-class new-tok >o
	      o swap !  dht-hash $!  o o>
	  ELSE  2drop drop dummy-dht dup .dht-off  THEN
      ELSE  @ nip nip  THEN ;] dht-sema c-section ;
: .tag ( addr u -- ) 2dup 2>r 
    >tag verify-tag >r sigpksize# - type r> 2r> space .sigdates .check ;
: .host ( addr u -- ) over c@ '!' = IF  .revoke  EXIT  THEN
    2dup sigsize# - .addr$
    2dup space .sigdates >host verify-host .check 2drop ;
: .owner ( addr u -- )  2dup sigsize# - ['] .key$ catch-nobt IF
	2drop [: <err> ."  invalid key" cr ;] execute-theme-color  THEN
    2dup space .sigdates verify-owner .check 2drop ;
: host>$ ( addr u -- addr u' flag )
    >host verify-host 0= >r sigsize# - r> ;
: d#. ( -- )
    dht-hash $@ 85type ." :" cr
    k#size cell DO
	I cell/ 0 .r ." : "
	dht-hash I +
	I cell/ case
	    k#host  of  [: cr .host  ." ,"  ;] $[]map  endof
	    k#tags  of  [: cr .tag   ." , " ;] $[]map  endof
	    k#owner of  [: cr .owner ." , " ;] $[]map  endof
	    nip endcase  cr
    cell +LOOP ;
: d#.s ( -- )
    d#public ['] d#. dht-map ;

: d#owner+ ( addr u -- ) \ with sanity checks
    [: check-owner dht-owner $rep[]sig dht( d#. ) ;] dht-sema c-section ;
: d#host+ ( addr u -- ) \ with sanity checks
    [: check-host dht-host $ins[]sig drop dht( d#. ) ;] dht-sema c-section ;
: d#tags+ ( addr u -- ) \ with sanity checks
    [: check-tag dht-tags $ins[]sig drop dht( d#. ) ;] dht-sema c-section ;
: d#have+ ( addr u -- ) \ with sanity checks
    [: check-have dht-have $ins[]sig drop dht( d#. ) ;] dht-sema c-section ;
: d#owner- ( addr u -- ) \ with sanity checks
    [: delete-owner? 0= IF  dht-owner $del[]sig dht( d#. )
      ELSE  2drop  THEN ;] dht-sema c-section ;
: d#host- ( addr u -- ) \ with sanity checks
    [: delete-host? 0= IF  dht-host $del[]sig dht( d#. )
      ELSE  2drop  THEN ;] dht-sema c-section ;
: d#tags- ( addr u -- ) \ with sanity checks
    [: delete-tag? 0= IF  dht-tags $del[]sig dht( d#. )
      ELSE  2drop  THEN ;] dht-sema c-section ;
: d#have- ( addr u -- ) \ with sanity checks
    [: delete-have? 0= IF  dht-have $del[]sig dht( d#. )
      ELSE  2drop  THEN ;] dht-sema c-section ;

: d#cleanup ( o:dht -- )
    dht-hash k#size + dht-hash cell+ U+DO
	I $@ bounds U+DO
	    I @ IF  I $@ check-date IF  I $free  THEN  2drop  THEN
	cell +LOOP  0 I del$cell
    cell +LOOP ;
: d#cleanups ( -- )
    d#public [: ['] d#cleanup dht-sema c-section ;] dht-map ;

64Variable last-d#cleanup

: d#cleanups? ( -- )
    last-d#cleanup 64@ ticks 64u< IF
	last-d#cleanup 64@ 64-0<> IF  ['] d#cleanups catch  ELSE  0  THEN
	ticks config:dht-cleaninterval& 2@ d>64 64+ last-d#cleanup 64!
	throw
    THEN ;

\ commands for DHT

scope{ net2o-base

\g 
\g ### dht commands ###
\g 

$33 net2o: dht-id ( $:string -- o:o )
    \g set DHT id for further operations on it
    perm-mask @ perm%dht and 0= !!dht-perm!!
    $> >d#id dht( ." set dht to: " dup h. forth:cr ) n:>o ;
dht-table >table

reply-table $@ inherit-table dht-table

dht-class :method start-req dht-hash $@ $, dht-id ;
net2o' emit net2o: dht-host+ ( $:string -- ) $> d#host+ ;
    \g add host to DHT
+net2o: dht-host- ( $:string -- ) $> d#host- ;
    \g delete host from DHT
+net2o: dht-host? ( -- )  dht-host
    [: dup $A0 + maxstring < IF  $, dht-host+  ELSE  2drop  THEN ;] $[]map ;
    \g query DHT host
+net2o: dht-tags+ ( $:string -- ) $> d#tags+ ;
    \g add tags to DHT
+net2o: dht-tags- ( $:string -- ) $> d#tags- ;
    \g delete tags from DHT
+net2o: dht-tags? ( -- )  dht-tags
    [: dup $A0 + maxstring < IF  $, dht-tags+  ELSE  2drop  THEN ;] $[]map ;
    \g query DHT tags
+net2o: dht-owner+ ( $:string -- ) $> d#owner+ ;
    \g add owner to DHT
+net2o: dht-owner- ( $:string -- ) $> d#owner- ;
    \g delete owner from DHT
+net2o: dht-owner? ( -- ) dht-owner
    [: dup $A0 + maxstring < IF  $, dht-owner+  ELSE  2drop  THEN ;] $[]map ;
    \g query DHT owner
+net2o: dht-have+ ( $:string -- ) $> d#have+ ;
    \g add have to DHT
+net2o: dht-have- ( $:string -- ) $> d#have- ;
    \g delete have from DHT
+net2o: dht-have? ( -- )  dht-have
    [: dup $A0 + maxstring < IF  $, dht-have+  ELSE  2drop  THEN ;] $[]map ;
    \g query DHT have

\ +net2o: dht-open ( fid -- ) 64>n d#open ;
\ +net2o: dht-query ( addr u mask fid -- ) 2*64>n d#query ;

}scope

dht-table $save

\ queries

fs-class class
    field: dht-queries
end-class dht-file-class

: d#c, ( addr u c -- addr' u' ) -rot xc!+? drop ; 
: d#$, ( addr1 u1 addr2 u2 -- addr' u' )
    2swap 2 pick d#c, 2swap
    2over rot umin dup >r move r> /string ;
: d#id, ( addr u -- addr' u' )
    0 d#c, dht-hash $@ d#$, ;
: d#values, ( addr u mask -- addr' u' ) { mask }
    k#size cell/ 1 DO
	mask 1 and IF
	    dht-hash I cells + I
	    dht( ." access dht: " over h. dup . forth:cr )
	    [{: k# :}l check-exact-date
		0= IF  k# d#c, d#$,  ELSE  2drop  THEN ;] $[]map
	THEN  mask 2/ to mask
    LOOP ;

dht-file-class :method fs-open 64#-1 64dup to fs-limit to fs-size ;

dht-file-class :method fs-read ( addr u -- n )  dup >r
    dht-queries $@ bounds ?DO
	I 1+ I c@ 2dup >d#id >o + c@ >r
	d#id, r> d#values, o>
    I c@ 2 + +LOOP  nip r> swap - ;

: new>dht ( -- )
    [: dht-file-class new { w^ fs-ins } fs-ins cell file-state $+! drop ;]
    filestate-sema c-section ;

: d#open ( fid -- )  new>dht lastfile@ .fs-open ;
: d#query ( addr u mask fid -- )  state-addr >o
    >r dup dht-queries c$+! dht-queries $+! r> dht-queries c$+! o> ;

dummy-dht >o dht-table @ token-table ! o>

\ value reading requires constructing answer packet

' context-table is gen-table

\ facility stuff

: gen-owner-del ( addr u -- addr' u' )
    gen>host "owner" >delete +sig$ ;

: gen>tag ( addr u hash-addr uh -- addr u )
    c:0key "tag" >keyed-hash
    2dup ':' $split 2swap >keyed-hash ;
: tag$ ( addr u -- tagaddr tag-u ) [: type .pk .sig ;] $tmp ;

: gen-tag ( addr u hash-addr uh -- addr' u' )
    gen>tag tag$ ;
: gen-tag-del ( addr u hash-addr uh -- addr' u' )
    gen>tag "tag" >delete tag$ ;

\ Generate view for beacons

Variable beacon-tuple$

: beacon-tuple ( o:addr -- )
    beacon-tuple$ $free
    [: host:ipv4 l@ ( lbe ) 0=
      IF    host:ipv6 $10 type  host:portv6
      ELSE  host:ipv4   4 type  host:portv4  THEN
      w@ dup 8 rshift emit $FF and emit ;]
    beacon-tuple$ $exec ;

\ addme stuff

also net2o-base

false Value add-myip

\ new address formats

: pub-addr, ( addr u -- )
     2dup pub-addr$ $ins[]sig drop $, dht-host+ ;
: addme-end ( -- ) request( ." addme" forth:cr )
    add-myip IF
	my-addr$ ['] pub-addr, $[]map
    THEN  end-with
    nest[ cookie, request-gen @ #request, ]nest
    do-expect-reply ;
: addme ( addr u -- )  $>addr { addr }
    config:ekey-timeout& 2@ d>64 now+delta
    addr .+my-id
    nat( ." addme: " addr .addr )
    addr .host:route $@len 0= IF
	addr my-addr-merge drop
	addr o>addr gen-host
	2dup my-addr$ $ins[]sig drop
	priv-addr$ $ins[]sig drop
	addr .beacon-tuple
	addr .net2o:dispose-addr
	nat( ."  public" forth:cr ) EXIT  THEN
    addr my-addr? 0= IF
	addr o>addr gen-host my-addr$ $ins[]sig drop
	nat( ."  routed" ) THEN
    nat( forth:cr )
    action-of expect-reply? ['] addme-end <> IF
	expect-reply pk@ $, dht-id
	mynick$ $, dht-owner+
    THEN
    addr o>addr gen-host pub-addr,
    addr .net2o:dispose-addr
    ['] addme-end IS expect-reply? ;
previous

: +addme ['] addme  is setip-xt  next-request request-gen ! ;
: -setip ['] .iperr is setip-xt ;

: add-me-id ( -- )
    dht-connection @ >o o to connection  +resend
    net2o-code  expect-reply
    expect-reply pk@ $, dht-id
    mynick$ $, dht-owner+
    end-with
    cookie+request
    end-code| o> ;
: sub-me ( -- ) msg( ." sub-me" forth:cr )
    dht-connection @ >o o to connection  +resend
    net2o-code  expect-reply
    pk@ $, dht-id
    pub-addr$ [: sigsize# - 2dup + sigdate datesize# move
      gen-host-del $, dht-host- ;] $[]map
    end-with
    cookie+request
    end-code| o> ;

: addme-owndht ( -- )
    pk@ >d#id [: >o dht-host $[]free
      my-addr$ [: dht-host $+[]! ;] $[]map o> ;] dht-sema c-section ;
: addnick-owndht ( addr u -- )
    2dup sigpk2size# - + keysize2 >d#id
    [: >o [: 2dup sigpk2size# - type + sigsize# - sigsize# type ;] $tmp
      dht-owner o> $rep[]sig ;] dht-sema c-section ;

\ replace me stuff

also net2o-base
: replace-me, ( -- )
    pk@ $, dht-id dht-host? end-with ;

: my-host? ( addr u -- flag )
    $>addr >o host:id $@ host$ $@ str= net2o:dispose-addr o> ;

: my-addrs? ( addr u -- addr u flag )
    false my-addr$ [: rot >r sigsize# - 2over str= r> or ;] $[]map ;

: remove-me, ( addr -- )
    \ 0 swap !@ { w^ host } host
    [: [: sigsize# - my-addrs? >r 2dup my-host? r> invert and IF
		2dup + sigdate datesize# move
		gen-host-del $, dht-host-
		false  ELSE  2drop true  THEN ;] $[]filter
    ;] dht-sema c-section
    ( host $free ) ;

: fetch-id, ( id-addr u -- )
    key2| $, dht-id dht-host? end-with ;
: fetch-host, ( nick u -- )
    nick>pk fetch-id, ;
previous

: me>d#id ( -- ) pk@ >d#id ;

in net2o : send-replace ( -- )
    me>d#id .dht-host dup
    >r $[]# IF  +resend
	net2o-code   expect-reply
	pk@ $, dht-id
	r@ remove-me, end-with
	cookie+request
	end-code|
    THEN  rdrop ;

: set-revocation ( addr u -- )
    dht-host $ins[]sig drop ;

in net2o : send-revoke ( addr u -- )
    ?keysize me>d#id >o +resend
    net2o-code  expect-reply
	dht-hash $@ $, dht-id dht-host remove-me,
	revoke-key 2dup set-revocation
	2dup $, dht-host+ end-with
	cookie+request
    end-code| \ send revocation upstrem
    dht-hash $@ renew-key drop o> ; \ replace key in key storage

: replace-me ( -- )  +addme +resend
    net2o-code   expect-reply get-ip replace-me, cookie+request
    end-code| -setip
    net2o:send-replace ;

: revoke-me ( addr u -- )
    \G give it your revocation secret
    +addme +resend
    net2o-code   expect-reply replace-me, cookie+request  end-code|
    -setip net2o:send-revoke ;

: disconnect-me ( -- )
    connection >o  data-rmap 0= IF  o> EXIT  THEN
    max-timeouts 4 umin to max-timeouts \ be impatient with disconnects
    +resend -flow-control
    net2o-code expect-reply
      connect( log .time s" Disconnect" $, type cr end-with )
      close-all ack rewind end-with disconnect
    end-code| msg( ." dht: disconnected" forth:cr )
    net2o:dispose-context msg( ." Disposed context" forth:cr ) o> ;

\\\
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
