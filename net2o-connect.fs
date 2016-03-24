\ net2o connection setup commands

\ Copyright (C) 2011-2014   Bernd Paysan

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

Defer >invitations

scope{ net2o-base
\ nat traversal functions

reply-table $@ inherit-table connect-table

\g 
\g ### connection generic commands ###
\g 

$20 net2o: request-done ( ureq -- ) 64>n \g signal request is completed
    o 0<> own-crypt? and IF  n2o:request-done  ELSE  drop  THEN ;
+net2o: set-cookie ( utimestamp -- ) \g cookies and round trip delays
    own-crypt? IF  trace( ." owncrypt " )
	64dup cookie>context?
	IF  cookie( ." context " dup hex. forth:cr ) >o rdrop  o to connection
	    ack@ >o ticker 64@ recv-tick 64! rtdelay! o> \ time stamp of arrival
	    EXIT
	ELSE \ just check if timeout didn't expire
	    cookie( ." ticker " forth:cr )
	    64dup context-ticker 64!
	    ticker 64@ 64swap 64- connect-timeout# 64< ?EXIT
	THEN
    ELSE  64drop  THEN  un-cmd ;
\ punch-stuff needs to be moved to connected
+net2o: punch-load, ( $:string -- ) \g use for punch payload: nest it
    $> $, nest  o IF
	['] punchs code-reply send-xt !
	punch-dispose o-beacon  THEN ;
+net2o: punch ( $:string -- ) \g punch NAT traversal hole
    $> net2o:punch ;
+net2o: punch-done ( -- ) \g punch received
    o 0<> own-crypt? and IF
	ret+beacon
	nat( ticks .ticks ."  punch done: " return-address .addr-path forth:cr )
    ELSE
	nat( ticks .ticks ."  punch not done: " return-addr .addr-path forth:cr )
    THEN ;

}scope

scope{ net2o-base

connect-table $@ inherit-table setup-table

\g 
\g ### connection setup commands ###
\g 

+net2o: tmpnest ( $:string -- ) \g nested (temporary encrypted) command
    $> cmdtmpnest ;

: ]tmpnest ( -- )  end-cmd cmd>tmpnest 2drop tmpnest ;

+net2o: new-data ( addr addr u -- ) \g create new data mapping
    o 0<> tmp-crypt? and own-crypt? or IF  64>n  new-data!  EXIT  THEN
    64drop 64drop 64drop  un-cmd ;
+net2o: new-code ( addr addr u -- ) \g crate new code mapping
    o 0<> tmp-crypt? and own-crypt? or IF  64>n  new-code!  EXIT  THEN
    64drop 64drop 64drop  un-cmd ;

: n2o:create-map
    { 64: addrs ucode udata 64: addrd -- addrd ucode udata addrs }
    addrs lit, addrd lit, ucode ulit, new-code
    addrs min-size ucode lshift n>64 64+ lit,
    addrd min-size ucode lshift n>64 64+ lit, udata ulit, new-data
    addrd ucode udata addrs ;

+net2o: store-key ( $:string -- ) $> \g store key
    own-crypt? IF
	key( ." store key: o=" o hex. 2dup .nnb forth:cr )
	2dup do-keypad sec!
	o IF  crypto-key sec!  ELSE  2drop  THEN
    ELSE  2drop un-cmd  THEN ;

+net2o: map-request ( addrs ucode udata -- ) \g request mapping
    2*64>n
    nest[
    ?new-mykey  ticker 64@ lit, set-cookie
    max-data# umin swap max-code# umin swap
    2dup + n2o:new-map n2o:create-map
    keypad keysize sec$, store-key  stskc KEYSIZE erase
    ]nest  n2o:create-map  nest-stack $[]# IF  ]tmpnest  THEN
    64drop 2drop 64drop ;

+net2o: set-tick ( uticks -- ) \g adjust time
    o IF
	adjust-timer( ." adjust timer" forth:cr )
	ack@ .adjust-ticks
    ELSE
	adjust-timer( ." no object: don't adjust timer " o hex. forth:cr )
	64drop
    THEN ;
+net2o: get-tick ( -- ) \g request time adjust
    ticks lit, set-tick ;

net2o-base

\ crypto functions

+net2o: receive-key ( $:key -- ) $> \g receive a key
    crypt( ." Received key: " tmpkey@ .nnb forth:cr )
    tmp-crypt? IF  net2o:receive-key  ELSE  2drop  THEN ;
+net2o: receive-tmpkey ( $:key -- ) $> \g receive emphemeral key
    net2o:receive-tmpkey ;
+net2o: key-request ( -- ) \g request a key
    crypt( ." Nested key: " tmpkey@ .nnb forth:cr )
    pkc keysize $, receive-key ;
+net2o: tmpkey-request ( -- ) \g request ephemeral key
    stpkc keysize $, receive-tmpkey nest[ ;
+net2o: keypair ( $:yourkey $:mykey -- ) \g select a pubkey
    $> $> tmp-crypt? IF  2swap net2o:keypair  ELSE  2drop 2drop  THEN ;
+net2o: update-key ( -- ) \g update secrets
    net2o:update-key ;
+net2o: gen-ivs ( $:string -- ) \g generate IVs
    $> tmp-ivs sec! tmp-receive? on ;

: cookie, ( xtd xtto -- )  add-cookie lit, set-cookie ;
: #request, ( -- )  ulit, request-done ;
: request, ( -- )  next-request #request, ;

: gen-punch ( -- ) nat( ." gen punches" forth:cr )
    my-addr$ [: -sig nat( ticks .ticks ."  gen punch: " 2dup .addr$ forth:cr ) $, punch ;] $[]map ;

: cookie+request ( -- ) request( ." gen cookie" forth:cr )
    nest[ cookie, request, ]nest ;

: new-request ( -- )
    next-request request-gen ! ;

: gen-punchload ( flag -- ) >r request( ." gen punchload" forth:cr )
    nest[ cookie, punch-done request-gen @ #request,
    reply-index ulit, ok
    r> IF  push' nop  THEN \ auto-nop if necessary
    ]nest$ punch-load, net2o:expect-reply maxdata code+ ;

+net2o: punch? ( -- ) \g Request punch addresses
    gen-punch ;

\ create commands to send back

:noname ( -- )
    tmp-ivs sec@ ivs-strings  tmp-receive? @ IF receive-ivs ELSE send-ivs THEN
    tmp-ivs sec-off ; is new-ivs
: all-ivs ( -- ) \G Seed and gen all IVS
    state# rng$ 2dup sec$, gen-ivs tmp-ivs sec!  tmp-receive? off ;

+net2o: >time-offset ( n -- ) \g set time offset
    o IF  ack@ .time-offset 64!  ELSE  64drop  THEN ;
+net2o: context ( -- ) \g make context active
    update-cdmap  o IF  context!  ELSE  connect( ." Can't " )  THEN
    connect( ." establish a context!" forth:cr ) ;

: time-offset! ( -- )  ticks 64dup lit, >time-offset ack@ .time-offset 64! ;
Variable id-hash
: gen-id ( -- addr u )
    $10 rng$ o { w^ idcon } idcon cell 2over id-hash #! ; 
: reply-key, ( -- )
    key-setup? @ !!doublekey!!
    nest[
        gen-id $, error-id
        pkc keysize $, pubkey $@len 0> keypad$ nip keysize u<= and IF
	    pubkey $@ key| $, keypair
	    pubkey $@ drop skc key-stage2
	ELSE  receive-key  THEN
    update-key all-ivs ;
: reply-key ( -- ) crypt( ." Reply key: " tmpkey@ .nnb forth:cr )
    reply-key, ( cookie+request ) time-offset! context ]tmpnest
    push-cmd ;

+net2o: gen-reply ( -- ) \g generate a key request reply
    own-crypt? IF  ['] reply-key IS expect-reply?  THEN ;
+net2o: gen-punch-reply ( -- ) ( obsolete dummy ) ;

\ one-shot packets

+net2o: oneshot-tmpkey ( $:tmpkey -- ) \g oneshot tmpkey
    $> keysize <> !!keysize!! skc swap keypad ed-dh do-keypad sec! ;
+net2o: invite ( $:nick+sig -- ) \g invite someone
    $> tmp-crypt? IF
	pk2-sig? !!sig!! >invitations do-keypad sec-off
    ELSE  2drop  THEN ;

\ version check
: ?version ( addr u -- )
    net2o-version 2over str< IF
	<warn> ." Other side has more recent net2o version: "
	forth:type ." , ours: " net2o-version forth:type <default> forth:cr
    ELSE  2drop  THEN ;

+net2o: check-version ( $:version -- ) \g version check
    $> ?version ;
+net2o: get-version ( $:version -- ) \g version cross-check
    string-stack $[]# IF  $> ?version  THEN \ accept query-only
    net2o-version $, check-version ;

gen-table $freeze

}scope

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z\-0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
     (("[:") (0 . 1) (0 . 1) immediate)
     ((";]") (-1 . 0) (0 . -1) immediate)
    )
End:
[THEN]