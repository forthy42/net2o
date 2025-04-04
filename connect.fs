\ net2o connection setup commands

\ Copyright © 2011-2014   Bernd Paysan

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

Forward >invitations
in net2o Forward dispose-addrs
Forward mynick$
Forward invite-me
Forward qr-invite-me
Defer <invite-result>

scope{ net2o-base
\ nat traversal functions

reply-table $@ inherit-table connect-table

\g 
\g ### connection generic commands ###
\g 

$20 net2o: request-done ( ureq -- ) 64>n \g signal request is completed
    o 0<> own-crypt? and IF  net2o:request-done  ELSE  drop  THEN ;
+net2o: set-cookie ( utimestamp -- ) \g cookies and round trip delays
    own-crypt? IF  trace( ." owncrypt " )
	64dup cookie>context?
	IF  cookie( ." context " dup h. forth:cr ) >o rdrop  o to connection
	    ack@ >o ticker 64@ recv-tick 64! rtdelay! o> \ time stamp of arrival
	    EXIT
	ELSE \ just check if timeout didn't expire
	    cookie( ." ticker " forth:cr )
	    64dup context-ticker 64!
	    [ tmp-cookie .cc-secret ]L KEYBYTES do-keypad sec!
	    ticker 64@ 64swap 64- connect-timeout# 64< ?EXIT
	    <err> ." cookie: no context, un-cmd" <default> forth:cr
	THEN
    ELSE  64drop
	<err> ." cookie: no owncrypt, un-cmd" <default> forth:cr
    THEN
    un-cmd ;
+net2o: punch-load, ( $:string -- ) \g use for punch payload: nest it
    $> $, nest  o IF
	nat( ." punch from: " return-address .addr-path forth:cr )
	['] punchs code-reply is send-xt
	punch-addrs net2o:dispose-addrs \ first punch load: empty addresses
    THEN ;
+net2o: punch ( $:string -- ) \g punch NAT traversal hole
    $> nat( ." punch to: " 2dup .addr$ forth:cr ) net2o:punch ;
+net2o: punch-done ( -- ) \g punch received
    o 0<> own-crypt? and IF
	o-beacon ret+beacon
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

$30 net2o: tmpnest ( $:string -- ) \g nested (temporary encrypted) command
    $> cmdtmpnest ;
+net2o: encnest ( $:string -- ) \g nested (completely encrypted) command
    $> cmdencnest ;

: ]tmpnest ( -- )  end-cmd cmd>tmpnest 2drop tmpnest ;
: ]encnest ( -- )  end-cmd cmd>encnest 2drop encnest ;

+net2o: close-tmpnest ( -- )
    \g cose a opened tmpnest, and add the necessary stuff
    nest-stack $[]# IF  ]tmpnest  THEN ;
+net2o: close-encnest ( -- )
    \g cose a opened encnest, and add the necessary stuff
    nest-stack $[]# IF  ]encnest  THEN ;

+net2o: new-data ( addr addr u -- ) \g create new data mapping
    o 0<> tmp-crypt? and own-crypt? or IF  64>n  new-data!  EXIT  THEN
    64drop 64drop 64drop  un-cmd ;
+net2o: new-code ( addr addr u -- ) \g crate new code mapping
    o 0<> tmp-crypt? and own-crypt? or IF  64>n  new-code!  EXIT  THEN
    64drop 64drop 64drop  un-cmd ;

in net2o : create-map
    { 64: addrs ucode udata 64: addrd -- addrd ucode udata addrs }
    addrs lit, addrd lit, ucode ulit, new-code
    addrs min-size ucode lshift n>64 64+ lit,
    addrd min-size ucode lshift n>64 64+ lit, udata ulit, new-data
    addrd ucode udata addrs ;

: cookie, ( -- )  add-cookie lit, set-cookie ;
: #request, ( -- )  ulit, request-done ;
: request, ( -- )  next-request #request, ;

+net2o: store-key ( $:string -- ) $> \g store key
    own-crypt? IF  true !!deprecated!!
	key( ." store key: o=" o h. 2dup .nnb forth:cr )
	2dup do-keypad sec!
	o IF  crypto-key sec!  ELSE  2drop  THEN
    ELSE  2drop un-cmd  THEN ;

: sec-cookie, ( -- )  ?new-mykey
    keypad [ tmp-cookie .cc-secret ]L keysize move
    0 >o cookie, o> stskc KEYSIZE erase ;

+net2o: map-request ( addrs ucode udata -- ) \g request mapping
    2*64>n
    nest[ sec-cookie,
    max-data# umin swap max-code# umin swap
    net2o:new-map net2o:create-map
    \ keypad keysize sec$, store-key  stskc KEYSIZE erase
    ]nest  net2o:create-map
    64drop 2drop 64drop ;

+net2o: set-tick ( uticks -- ) \g adjust time
    o IF
	adjust-timer( ." adjust timer" forth:cr )
	ack@ .adjust-ticks
    ELSE
	adjust-timer( ." no object: don't adjust timer " o h. forth:cr )
	64drop
    THEN ;
+net2o: get-tick ( -- ) \g request time adjust
    ticks lit, set-tick ;

net2o-base

\ crypto functions

+net2o: receive-tmpkey ( $:key -- ) $> \g receive emphemeral key
    net2o:receive-tmpkey ;
+net2o: tmpkey-request ( -- ) \g request ephemeral key
    stpkc keysize $, receive-tmpkey nest[ ;
+net2o: keypair ( $:yourkey $:mykey -- ) \g select a pubkey
    $> $> tmp-crypt? IF  2swap net2o:keypair  ELSE  2drop 2drop  THEN ;
+net2o: update-key ( -- ) \g update secrets
    net2o:update-key ;
+net2o: gen-ivs ( $:string -- ) \g generate IVs
    $> tmp-ivs sec! [ ivs-val receive-val or ]L validated or! ;
+net2o: addr-key! ( $:string -- ) \g set key for cmd0-reply
    $> dup ?keysize lastaddr# cell+ $! ;

: 0key, ( -- ) my-0key sec@ sec$, addr-key! ;
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
    nat( ." punch?" forth:cr ) gen-punch ;

\ create commands to send back

}scope

: new-ivs ( -- )
    tmp-ivs sec@ ivs-strings
    validated @ receive-val and  IF receive-ivs ELSE send-ivs THEN
    tmp-ivs sec-free ;

scope{ net2o-base

: all-ivs ( -- ) \G Seed and gen all IVS
    state# rng$ 2dup sec$, gen-ivs tmp-ivs sec!
    validated @ ivs-val or receive-val invert and validated ! ;

+net2o: >time-offset ( n -- ) \g set time offset
    o IF  ack@ .time-offset 64!  ELSE  64drop  THEN ;
+net2o: context ( -- ) \g make context active
    update-cdmap  o IF  context!  ELSE  connect( ." Can't " )  THEN
    connect( ." establish a context!" forth:cr ) ;

: time-offset! ( -- )  ticks 64dup lit, >time-offset ack@ .time-offset 64! ;

\ error ID handling

Variable id#
Sema id-sema

: new-error-id ( -- addr u )
    [: o { w^ idcon } $10 rng$ idcon cell 2over id# #! ;] id-sema c-section
    2dup my-error-id $! ;
: error-id>o ( addr u -- o/0 )
    $error-id $@ ?dup-IF
	id# #@ cell = IF
	    @  EXIT  THEN  THEN
    drop 0 ;
: error-id$free ( -- )
    [: my-error-id $@ ?dup-IF  id# #free  ELSE  drop  THEN
      my-error-id $free ;] id-sema c-section ;

:is extra-dispose  error-id$free defers extra-dispose ;

\ compile a reply key

: reply-key, ( -- )
    key-setup? !!doublekey!!
    nest[
        new-error-id $, error-id
        pk@ key| $, pubkey $@len 0> keypad$ nip keysize u<= and IF
	    pubkey $@ key| $, keypair
	    pubkey $@ drop sk@ drop key-stage2
	ELSE  true !!nokey!!  THEN
    update-key all-ivs ;
: reply-key ( -- ) crypt( ." Reply key: " tmpkey@ .nnb forth:cr )
    reply-key, ( cookie+request ) time-offset! context
    ]tmpnest
    push-cmd ;

+net2o: gen-reply ( -- ) \g generate a key request reply
    own-crypt? IF
	['] reply-key IS expect-reply?
	o IF
	    ['] send-cX   IS send0-xt
	    return-addr return-address $10 move
	THEN
    THEN ;
+net2o: gen-punch-reply ( -- ) ( obsolete dummy ) ;

\ one-shot packets

+net2o: invite ( $:nick+sig $:pk -- ) \g invite someone
    $> ?keysize search-key 2drop
    $> tmp-crypt? dup invit:pend# and ulit, <invite-result>
    IF
	pk2-sig? !!sig!! >invitations
	do-keypad sec-free
    ELSE  ." invitation didn't decrypt" forth:cr 2drop  THEN ;
+net2o: request-invitation ( -- )
    \g ask for an invitation as second stage of invitation handshake
    own-crypt? IF  invite-me  THEN ;

\ more one shot stuff for QR codes

+net2o: sign-invite ( $:signature -- ) \g send you a signature
    $> sigpksize# <> !!unsigned!!
    c:0key mynick$ sigsize# - c:hash pk-sig? \ 0 is valid signature
    0= IF  ke-sigs[] $+[]!  ELSE  2drop  THEN
    \ !!FIXME!! qr scan done, do something about it
;
+net2o: request-qr-invitation ( -- )
    \g ask for an invitation as second stage of invitation handshake
    own-crypt? IF  qr-invite-me  THEN ;
+net2o: tmp-secret, ( -- )
    nest[ sec-cookie, ]nest ;
+net2o: qr-challenge ( $:challenge $:respose -- )
    \ !!FIXME!! the qr-challenge should include pubkey+sig into the hash
    $> $> c:0key ." challenge: " 2dup 85type space
    qr-key $8 2dup 85type forth:cr >keyed-hash qr-hash $40 c:hash@
    qr-hash over $10 umax str= dup invit:qr# and ulit, <invite-result>
    \ challenge will fail if less than 16 bytes
    IF  msg( ." challenge accepted" forth:cr )
	qr-tmp-val validated or!
    ELSE
	msg( ." challenge failed: " qr-hash $40 85type
	forth:cr ." qr-key: " qr-key 8 xtype forth:cr )
    THEN ;
+net2o: invite-result ( flag -- )
    o IF  to invite-result#  THEN ;
+net2o: set-host ( $:host -- )
    $> o IF  remote-host$  ELSE  $remote-host  THEN  $! ;
+net2o: get-host ( -- )
    host$ $@ $, set-host ;
' invite-result is <invite-result>
}scope

setup-table $save
connect-table $save

\\\
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
