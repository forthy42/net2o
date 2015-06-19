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
: avalanche-msg ( msg groupaddr u -- )
    \g forward message to all next nodes of that message group
    2swap { d: msg }
    msg-groups #@ dup IF
	bounds ?DO  I @ o <> IF  msg I @ .avalanche-to  THEN
	cell +LOOP
    ELSE  2drop  THEN ;
event: ->avalanche ( o -- )
    avalanche( ." Avalanche to: " dup hex. cr )
    >o last-msg $@ last-group $@ parent @ .avalanche-msg o> ;
event: ->chat-connect ( o -- )
    drop ctrl Z unkey ;

get-current also net2o-base definitions

\g 
\g ### message commands ###
\g 
$34 net2o: msg ( -- o:msg ) \g push a message object
    msg-context @ dup 0= IF
	drop  n2o:new-msg dup msg-context !
    THEN
    n:>o c-state off ;

msg-table >table

reply-table $@ inherit-table msg-table

net2o' emit net2o: msg-start ( $:pksig -- ) \g start message
    !!signed? 1 !!>order? $> 2dup startdate@ .ticks space .key-id ." : " ;
+net2o: msg-group ( $:group -- ) \g specify a chat group
    !!signed?  8 $10 !!<>=order? \g already a message there
    $> last-group $!
    parent @ .wait-task @ ?dup-IF
	<event o elit, ->avalanche event>  THEN ;
+net2o: msg-join ( $:group -- ) \g join a chat group
    signed? !!signed!! $> msg-groups #@ d0<> IF \ we only join existing groups
	parent cell last# cell+ $+!
	parent @ .wait-task @ ?dup-IF
	    <event parent @ elit, ->chat-connect event>  THEN THEN ;
+net2o: msg-leave ( $:group -- ) \g leave a chat group
    signed? !!signed!! $> msg-groups #@ d0<> IF
	parent @ last# cell+ del$cell  THEN ;

+net2o: msg-signal ( $:pubkey -- ) \g signal message to one person
    !!signed? 1 2 !!<>order? $> keysize umin 2dup pkc over str=
    IF   info-color attr!  THEN  ." @" .key-id space
    reset-color ;
+net2o: msg-re ( $:hash ) \g relate to some object
    !!signed? 1 4 !!<>=order? $> ." re: " 85type forth:cr ;
+net2o: msg-text ( $:msg -- ) \g specify message string
    !!signed? 1 8 !!<>=order? $> forth:type forth:cr ;
+net2o: msg-object ( $:object -- ) \g specify an object, e.g. an image
    !!signed? 1 8 !!<>=order? $> ." wrapped object: " 85type forth:cr ;
+net2o: msg-action ( $:msg -- ) \g specify message string
    !!signed? 1 8 !!<>=order? $> .\" \b\b " forth:type forth:cr ;

:noname ( addr u -- addr u flag )
    pk-sig? dup >r IF
	2dup last-msg $!
	sigpksize# - 2dup + sigpksize# >$  c-state off
    THEN r>
; msg-class to nest-sig

gen-table $freeze
' context-table is gen-table

set-current

Variable msg-group$
Variable group-master

: <msg ( -- ) \G start a msg block
    msg sign[
    msg-start ;
: msg> ( -- ) \G end a msg block by adding a signature
    msg-group$ $@ dup IF  $, msg-group  ELSE  2drop  THEN
    now>never ]pksign ;

previous

: send-text ( addr u -- )
    net2o-code  expect-reply
    <msg $, msg-text msg> endwith
    cookie+request end-code| ;

: send-text-to ( msg u nick u -- )
    net2o-code expect-reply
    <msg nick>pk dup IF  keysize umin $, msg-signal  ELSE  2drop  THEN
    $, msg-text msg> endwith
    cookie+request end-code| ;

also net2o-base
: join, ( -- )
    msg-group$ $@ dup IF  msg $, msg-join
	sign[ msg-start "joined" $, msg-action msg> endwith
    ELSE  2drop  THEN ;

: leave, ( -- )
    msg-group$ $@ dup IF  msg $, msg-leave
	sign[ msg-start "left" $, msg-action msg> endwith
    ELSE  2drop  THEN ;
previous

: send-join ( -- )
    net2o-code expect-reply join,
    cookie+request end-code| ;

: send-leave ( -- )
    net2o-code expect-reply leave,
    cookie+request end-code| ;

: .chat ( addr u -- )
    sigdate 64@ .ticks space pkc keysize .key-id
    2dup s" /me " string-prefix? IF  4 /string space
    ELSE  ." : "  THEN type cr ;

$200 Constant maxmsg#

: get-input-line ( -- addr u )  history >r  0 to history
    BEGIN  pad maxmsg# ['] accept catch
	dup dup -56 = swap -28 = or \ quit or ^c to leave
	IF    drop 2drop "/bye"
	ELSE
	    dup 0= IF
		drop dup 1+ xback-restore  pad swap
	    ELSE \ fixme: do DoError instead
		DoError drop 0  THEN
	THEN
	dup 0= WHILE  2drop  REPEAT
    r> to history ;

: g?join ( -- )
    msg-group$ $@len IF  +resend-cmd send-join -timeout  THEN ;

: g?leave ( -- )
    msg-group$ $@len connection 0<> and IF
	+resend-cmd send-leave -timeout
    THEN ;

: chat-entry ( -- )
    warn-color attr!
    ." Type ctrl-D or '/bye' as single item to quit" cr
    default-color attr! ;

: do-chat ( -- ) chat-entry  -timeout
    \ ['] cmd( >body on
    BEGIN  get-input-line
	2dup "/bye" str= 0= connection 0<> and  WHILE
	    2dup +resend-cmd send-text -timeout .chat
    REPEAT  2drop g?leave ;

also net2o-base
: avalanche-text ( addr u -- )
    code-buf$ cmdreset
    <msg
    2dup s" /me " string-prefix? IF  4 /string $, msg-action
    ELSE  $, msg-text  THEN
    msg> endwith
    cmdbuf$ 4 /string 2 - msg-group$ $@ code-buf avalanche-msg ;
previous

: group-chat ( -- ) chat-entry \ ['] cmd( >body on
    [: up@ wait-task ! ret+beacon ;] IS do-connect
    BEGIN  get-input-line
	2dup "/bye" str= 0=
	msg-group$ $@ msg-groups #@ nip 0> and  WHILE
	    msg-group$ $@ msg-groups #@ drop @ >o
	    2dup msg-context @ .avalanche-text .chat o>
    REPEAT  2drop g?leave
    msg-group$ $@ msg-groups #@ dup >r bounds ?DO  I @  cell +LOOP
    r> 0 ?DO  >o o to connection ret-beacon disconnect-me o>  cell +LOOP ;

:noname ( addr u o:context -- )
    avalanche( ." Send avalance to: " pubkey $@ key>nick type cr )
    o to connection +resend-cmd
    net2o-code expect-reply msg $, nestsig endwith
    cookie+request end-code ; is avalanche-to

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