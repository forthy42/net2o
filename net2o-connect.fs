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

reply-table $@ inherit-table setup-table

$20 net2o: tmpnest ( $:string -- ) \ nested (temporary encrypted) command
    $> cmdtmpnest ;

: ]nest$  ( -- )  end-cmd cmd>nest $, ;
: ]nest  ( -- )  ]nest$ push-$ push' nest ;
: ]tmpnest ( -- )  end-cmd cmd>tmpnest $, tmpnest ;

+net2o: new-data ( addr addr u -- ) \ create new data mapping
    o 0<> tmp-crypt? and own-crypt? or IF  64>n  n2o:new-data  EXIT  THEN
    64drop 64drop 64drop  un-cmd ;
+net2o: new-code ( addr addr u -- ) \ crate new code mapping
    o 0<> tmp-crypt? and own-crypt? or IF  64>n  n2o:new-code  EXIT  THEN
    64drop 64drop 64drop  un-cmd ;
+net2o: set-cookie ( utimestamp -- ) \ cookie and round trip delay
    own-crypt? IF
	64dup cookie>context?
	IF  >o rdrop  o to connection
	    ack@ >o ticker 64@ recv-tick 64! rtdelay! o> \ time stamp of arrival
	    EXIT
	ELSE \ just check if timeout didn't expire
	    ticker 64@ connect-timeout# 64- 64u< 0= ?EXIT
	THEN
    ELSE  64drop  THEN  un-cmd ;

: n2o:create-map
    { 64: addrs ucode udata 64: addrd -- addrd ucode udata addrs }
    addrs lit, addrd lit, ucode ulit, new-code
    addrs min-size ucode lshift n>64 64+ lit,
    addrd min-size ucode lshift n>64 64+ lit, udata ulit, new-data
    addrd ucode udata addrs ;

+net2o: store-key ( $:string -- ) $> \ store key
    o 0= IF  ." don't store key, o=0: " .nnb F cr un-cmd  EXIT  THEN
    own-crypt? IF
	key( ." store key: o=" o hex. 2dup .nnb F cr )
	2dup do-keypad sec!
	crypto-key sec!
    ELSE  ." don't store key: o=" o hex. .nnb F cr  THEN ;

+net2o: map-request ( addrs ucode udata -- ) \ request mapping
    2*64>n
    nest[
    ?new-mykey ticker 64@ lit, set-cookie
    max-data# umin swap max-code# umin swap
    2dup + n2o:new-map n2o:create-map
    keypad keysize $, store-key  stskc KEYSIZE erase
    ]nest  n2o:create-map  nest-stack $[]# IF  ]tmpnest  THEN
    64drop 2drop 64drop ;

+net2o: set-tick ( uticks -- ) \ adjust time
    o IF  ack@ .adjust-ticks  ELSE  64drop  THEN ;
+net2o: get-tick ( -- ) \ request time adjust
    ticks lit, set-tick ;

net2o-base

\ crypto functions

+net2o: receive-key ( $:key -- ) $> \ receive a key
    crypt( ." Received key: " tmpkey@ .nnb F cr )
    tmp-crypt? IF  net2o:receive-key  ELSE  2drop  THEN ;
+net2o: receive-tmpkey ( $:key -- ) $> \ receive emphemeral key
    net2o:receive-tmpkey ;
+net2o: key-request ( -- ) \ request a key
    crypt( ." Nested key: " tmpkey@ .nnb F cr )
    pkc keysize $, receive-key ;
+net2o: tmpkey-request ( -- ) \ request ephemeral key
    stpkc keysize $, receive-tmpkey nest[ ;
+net2o: keypair ( $:yourkey $:mykey -- ) \ select a pubkey
    $> $> tmp-crypt? IF  2swap net2o:keypair  ELSE  2drop 2drop  THEN ;
+net2o: update-key ( -- ) \ update secrets
    net2o:update-key ;
+net2o: gen-ivs ( $:string -- ) \ generate IVs
    $> ivs-strings receive-ivs ;

\ nat traversal functions

+net2o: punch ( $:string -- ) \ punch NAT traversal hole
    $> net2o:punch ;
+net2o: punch-load, ( $:string -- ) \ use for punch payload: nest it
    $> punch-load $! ;
+net2o: punch-done ( -- ) \ punch received
    o 0<> own-crypt? and IF
	return-addr return-address $10 move  resend0 $off
    THEN ;

: cookie, ( -- )  add-cookie lit, set-cookie ;
: request, ( -- )  next-request ulit, request-done ;

: gen-punch ( -- )
    my-ip$ [: $, punch ;] $[]map ;

: cookie+request ( -- )  nest[ cookie, request, ]nest ;

: gen-punchload ( -- )
    nest[ cookie, punch-done request, ]nest$ punch-load, ;

+net2o: punch? ( -- ) \ Request punch addresses
    gen-punch ;

\ create commands to send back

: all-ivs ( -- ) \ Seed and gen all IVS
    state# rng$ 2dup $, gen-ivs ivs-strings send-ivs ;

+net2o: >time-offset ( n -- ) \ set time offset
    o IF  ack@ .time-offset 64!  ELSE  64drop  THEN ;
+net2o: context ( -- ) \ make context active
    o IF  context!  ELSE  ." Can't "  THEN  ." establish a context!" F cr ;

: time-offset! ( -- )  ticks 64dup lit, >time-offset ack@ .time-offset 64! ;
: reply-key, ( -- )
    nest[ pkc keysize $, dest-pubkey @ IF
	dest-pubkey $@ $, keypair
	dest-pubkey $@ drop skc key-stage2
    ELSE  receive-key  THEN
    update-key all-ivs ;

+net2o: gen-reply ( -- ) \ generate a key request reply reply
    own-crypt? 0= ?EXIT
    [: crypt( ." Reply key: " tmpkey@ .nnb F cr )
      reply-key, cookie+request time-offset! context ]tmpnest
      push-cmd ;]  IS expect-reply? ;
+net2o: gen-punch-reply ( -- )  o? \ generate a key request reply reply
    [: crypt( ." Reply key: " tmpkey@ .nnb F cr )
      reply-key, gen-punchload gen-punch time-offset! context ]tmpnest
      push-cmd ;]  IS expect-reply? ;

gen-table $freeze

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