\ messages                                           06aug2014py

\ Copyright (C) 2014   Bernd Paysan

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

defer avalanche-to ( addr u o:context -- )
defer pk-connect ( key u cmdlen datalen -- )
: avalanche-msg ( msg u1 groupaddr u2 -- )
    \g forward message to all next nodes of that message group
    2swap { d: msg }
    msg-groups #@ dup IF
	bounds ?DO  I @ o <> IF  msg I @ .avalanche-to  THEN
	cell +LOOP
    ELSE  2drop  THEN ;
: do-msg-nestsig ( -- )
    last-msg $@
    sigpksize# - 2dup + sigpksize# >$  c-state off
    do-nestsig ;
: do-avalanche ( -- )
    last-msg $@ last-group $@ parent @ .avalanche-msg ;
event: ->avalanche ( o -- )
    avalanche( ." Avalanche to: " dup hex. cr )
    >o do-avalanche o> ;
event: ->chat-connect ( o -- )
    drop ctrl Z inskey ;
event: ->reconnect ( o -- )
    >o last-group $@ msg-groups #@ d0=
    IF  "" last-group $@ msg-groups #!  THEN  last# >r
    last-msg $@ $A $A pk-connect o { w^ connection }
    connection cell r> cell+ $+! o> ;
event: ->msg-nestsig ( editor stack o -- editor stack )
    >o do-msg-nestsig o> ctrl L inskey ;

: ?msg-context ( -- o )
    msg-context @ dup 0= IF
	drop  n2o:new-msg dup msg-context !
	pubkey $@ msg-context @ .last-group $!
    THEN ;

get-current also net2o-base definitions

\g 
\g ### message commands ###
\g 
$34 net2o: msg ( -- o:msg ) \g push a message object
    ?msg-context n:>o c-state off ;

msg-table >table

reply-table $@ inherit-table msg-table

net2o' emit net2o: msg-start ( $:pksig -- ) \g start message
    !!signed? 1 !!>order? $> 2dup startdate@ .ticks space .key-id ;
+net2o: msg-group ( $:group -- ) \g specify a chat group
    !!signed?  8 $10 !!<>=order? \g already a message there
    $> last-group $! up@ receiver-task <> IF
	do-avalanche
    ELSE parent @ .wait-task @ ?dup-IF
	    <event o elit, ->avalanche event>  THEN
    THEN ;
+net2o: msg-join ( $:group -- ) \g join a chat group
    signed? !!signed!! $> msg-groups #@ d0<> IF \ we only join existing groups
	parent cell last# cell+ $+!
	parent @ .wait-task @ ?dup-IF
	    <event parent @ elit, ->chat-connect event>  THEN THEN ;
+net2o: msg-leave ( $:group -- ) \g leave a chat group
    signed? !!signed!! $> msg-groups #@ d0<> IF
	parent @ last# cell+ del$cell  THEN ;

+net2o: msg-signal ( $:pubkey -- ) \g signal message to one person
    !!signed? 3 !!>=order? $> keysize umin 2dup pkc over str=
    IF   <err>  THEN  ."  @" .key-id
    <default> ;
+net2o: msg-re ( $:hash ) \g relate to some object
    !!signed? 1 4 !!<>=order? $> ."  re: " 85type forth:cr ;
+net2o: msg-text ( $:msg -- ) \g specify message string
    !!signed? 1 8 !!<>=order? ." : " $> forth:type forth:cr ;
+net2o: msg-object ( $:object -- ) \g specify an object, e.g. an image
    !!signed? 1 8 !!<>=order? $> ."  wrapped object: " 85type forth:cr ;
+net2o: msg-action ( $:msg -- ) \g specify message string
    !!signed? 1 8 !!<>=order? $> space
    <warn> forth:type <default> forth:cr ;
+net2o: msg-reconnect ( $:pubkey -- ) \g rewire distribution tree
    signed? !!signed!! $> last-msg $!
    <event o elit, ->reconnect parent @ .wait-task @ event> ;
+net2o: msg-joined ( $:nick -- ) \g join a group, send your key with nick
    signed? !!signed!! 1 2 !!<>order? $> type ;
net2o' nestsig net2o: msg-nestsig ( $:cmd+sig -- ) \g check sig+nest
    $> nest-sig -rot last-msg $! IF
	parent @ .wait-task @ ?dup-IF
	    >r r@ <hide> <event o elit, ->msg-nestsig r> event>
	ELSE  do-msg-nestsig  THEN
    ELSE  true !!inv-sig!!  THEN ; \ balk on all wrong signatures

:noname ( addr u -- addr u flag )
    pk-sig?
; msg-class to nest-sig

gen-table $freeze
' context-table is gen-table

set-current

Variable msg-group$
Variable group-master
Variable msg-logs

: <msg ( -- ) \G start a msg block
    msg sign[
    msg-start ;
: msg> ( -- ) \G end a msg block by adding a signature
    msg-group$ $@ dup IF  $, msg-group  ELSE  2drop  THEN
    now>never ]pksign ;

previous

: msg-reply ( tag -- )
    reply( ." got reply " hex. pubkey $@ key>nick type cr )else( drop ) ;

: send-text ( addr u -- )
    net2o-code  ['] msg-reply expect-reply-xt
    <msg $, msg-text msg> endwith
    ( cookie+request ) end-code| ;

: send-text-to ( msg u nick u -- )
    net2o-code ['] msg-reply expect-reply-xt
    <msg nick>pk dup IF  keysize umin $, msg-signal  ELSE  2drop  THEN
    $, msg-text msg> endwith
    ( cookie+request ) end-code| ;

also net2o-base
: join, ( -- )
    msg-group$ $@ dup IF  msg $, msg-join
	sign[ msg-start "joined" $, msg-action msg> endwith
    ELSE  2drop  THEN ;

: leave, ( -- )
    msg-group$ $@ dup IF  msg $, msg-leave
	sign[ msg-start "left" $, msg-action msg> endwith
    ELSE  2drop  THEN ;

: left, ( addr u -- )
    keysize umin $, msg-signal " left (timeout)" $, msg-action ;
previous

: send-join ( -- )
    net2o-code ['] msg-reply expect-reply-xt join,
    ( cookie+request ) end-code| ;

: send-leave ( -- )
    net2o-code ['] msg-reply expect-reply-xt leave,
    cookie+request end-code| ;

: .chat ( -- )
    msg-group$ $@ msg-groups #@ drop @ >o ?msg-context >o
    nest-string 2@ last-msg $! do-msg-nestsig o> o> ;

$200 Constant maxmsg#

: get-input-line ( -- addr u )  history >r  0 to history
    BEGIN  pad maxmsg# ['] accept catch
	dup dup -56 = swap -28 = or \ quit or ^c to leave
	IF    drop 2drop "/bye"
	ELSE
	    dup 0= IF
		drop dup 1+ xback-restore  pad swap
	    ELSE
		DoError drop 0  THEN
	THEN
	dup 0= WHILE  2drop  REPEAT
    r> to history ;

: g?join ( -- )
    msg-group$ $@len IF  +resend-cmd send-join -timeout  THEN ;

: g?leave ( -- )
    msg-group$ $@len IF
	+resend-cmd send-leave -timeout
    THEN ;

: chat-entry ( -- )
    <warn> ." Type ctrl-D or '/bye' as single item to quit" <default> cr ;

also net2o-base
: send-avalanche ( xt -- )
    code-buf$ cmdreset
    <msg execute msg> endwith 
    cmdbuf$ 4 /string 2 - msg-group$ $@ code-buf avalanche-msg ;

Vocabulary chat-/cmds

get-current also chat-/cmds definitions

: me ( addr u -- )
    [: $, msg-action ;] send-avalanche .chat ;

: peers ( addr u -- ) 2drop ." peers:"
    msg-group$ $@ msg-groups #@ bounds ?DO
	space I @ .pubkey $@ .key-id
    cell +LOOP  forth:cr ;

: help ( addr u -- ) 2drop ." all commands start with / as first character: "
    ['] chat-/cmds >body wordlist-words ." bye" forth:cr ;

set-current previous

: do-chat-cmds ( addr u -- )
    1 /string bl $split 2swap
    2dup ['] chat-/cmds >body (search-wordlist)
    ?dup-IF  nip nip name>int execute
    ELSE  <err> ." unknown command: " forth:type <default> forth:cr  THEN ;

: avalanche-text ( addr u -- )
    over c@ '/' = IF  do-chat-cmds  EXIT  THEN
    [: BEGIN  dup  WHILE  over c@ '@' = WHILE  2dup { oaddr ou }
		  bl $split 2swap 1 /string ':' -skip nick>pk \ 0. if no nick
		  2dup d0= IF  2drop 2drop oaddr ou true
		  ELSE  $, msg-signal false  THEN
	  UNTIL  THEN  THEN
      $, msg-text ;] send-avalanche .chat ;
previous

: group-chat ( -- ) chat-entry \ ['] cmd( >body on
    [: up@ wait-task ! ret+beacon ;] IS do-connect
    BEGIN  get-input-line
	2dup "/bye" str= 0= >r
	msg-group$ $@ msg-groups #@ 0> r> and  WHILE
	    @ >o msg-context @ .avalanche-text o>
    REPEAT  drop 2drop
    msg-group$ $@ msg-groups #@ dup >r bounds ?DO  I @  cell +LOOP
    r> 0 ?DO  >o o to connection +resend-cmd send-leave
    ret-beacon disconnect-me o>  cell +LOOP ;

: msg-timeout ( -- )  1 ack@ .timeouts +! >next-timeout
    cmd-resend? IF  reply( ." Resend to " pubkey $@ key>nick type cr )
    ELSE  EXIT  THEN
    timeout-expired? IF  pubkey $@ ['] type $tmp n2o:dispose-context
	msg-group$ $@len IF
	    ['] left, send-avalanche .chat
	ELSE  2drop  THEN
    THEN ;

: +resend-msg  ['] msg-timeout  timeout-xt ! o+timeout ;

:noname ( addr u o:context -- )
    avalanche( ." Send avalance to: " pubkey $@ key>nick type cr )
    o to connection +resend-msg
    net2o-code ['] msg-reply expect-reply-xt msg $, nestsig endwith
    ( cookie+request ) end-code ; is avalanche-to

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