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
defer addr-connect ( key+addr u cmdlen datalen -- )
: avalanche-msg ( msg u1 -- )
    \g forward message to all next nodes of that message group
    { d: msg }
    last# cell+ $@ dup IF
	bounds ?DO  I @ o <> IF  msg I @ .avalanche-to  THEN
	cell +LOOP
    ELSE  2drop  THEN ;

Variable msg-group$
Variable group-master
Variable msg-logs
Variable otr-mode
User replay-mode

: ?msg-context ( -- o )
    msg-context @ dup 0= IF
	drop  n2o:new-msg dup msg-context !
    THEN ;

: init-chatlog ( -- ) ?.net2o s" ~/.net2o/chats" $1FF init-dir ;

: >chatid ( group u -- id u )  defaultkey sec@ keyed-hash#128 ;

: save-msgs ( group u -- )
    otr-mode @ replay-mode @ or IF  2drop  EXIT  THEN
    init-chatlog  enc-file $off  n2o:new-msg >o
    2dup msg-logs #@ bounds ?DO
	I $@ [: net2o-base:$, net2o-base:nestsig ;]
	gen-cmd$ enc-file $+!
    cell +LOOP  dispose o>
    >chatid
    [: ." ~/.net2o/chats/" 85type ;] $tmp enc-filename $!
    pk-off  key-list encfile-rest ;

: vault>msg ( -- )
    [: n2o:new-msg >o parent off do-cmd-loop dispose o> ;]
    is write-decrypt ;

: load-msg ( group u -- )
    >chatid [: ." ~/.net2o/chats/" 85type ." .v2o" ;] $tmp
    2dup file-status nip no-file# = ?EXIT
    replay-mode on  vault>msg  decrypt-file  replay-mode off ;

: +msg-log ( addr u -- flag )
    msg-group$ $@ msg-logs #@ d0= IF
	s" " msg-group$ $@ msg-logs #!  THEN
    last# cell+ $[]# >r
    last# cell+ $ins[]date msg-group$ $@ save-msgs
    r> last# cell+ $[]# <> ;

Sema queue-sema

\ msg queue

: msg@ ( -- addr u )
    [: 0 msgs[] $[]@ ;] queue-sema c-section ;
: msg+ ( addr u -- )
    [: msgs[] $+[]! ;] queue-sema c-section ;
: msg- ( -- )
    [: 0 msgs[] $[] $off
      msgs[] 0 cell $del ;] queue-sema c-section ;

\ peer queue

: peer@ ( -- addr u )
    [: 0 peers[] $[]@ ;] queue-sema c-section ;
: peer+ ( addr u -- )
    [: peers[] $+[]! ;] queue-sema c-section ;
: peer- ( -- )
    [: 0 peers[] $[] $off
      peers[] 0 cell $del ;] queue-sema c-section ;

\ events

: msg-display ( addr u -- )
    sigpksize# - 2dup + sigpksize# >$  c-state off do-nestsig ;

: do-msg-nestsig ( -- )
    msg@ 2dup +msg-log IF
	replay-mode @ 0= IF
	    msg-display msg-notify
	ELSE  2drop  THEN
    ELSE  2drop  THEN
    msg- ;

: display-lastn ( addr u n -- )
    n2o:new-msg >o parent off
    cells >r msg-logs #@ dup r> - 0 max /string bounds ?DO
	I $@ msg-display
    cell +LOOP   dispose o> ;

: >group ( addr u -- )
    2dup msg-groups #@ d0=
    IF  "" 2swap msg-groups #!  ELSE  2drop  THEN ;

Defer silent-join

\ !!FIXME!! should use an asynchronous "do-when-connected" thing

: reconnect-chat ( -- )
    peer@ 2dup d0<> IF
	save-mem peer-  over >r
	reconnect( ." reconnect " 2dup 2dup + 1- c@ 1+ - .addr$ cr )
	0 >o last# >r $A $A addr-connect o { w^ con }
	o to connection r> to last# silent-join o>
	con cell last# cell+ $+!
	r> free throw
    ELSE  2drop  THEN ;

: do-avalanche ( -- )
    msg@ parent @ .avalanche-msg msg- ;

event: ->avalanche ( o group -- )
    avalanche( ." Avalanche to: " dup hex. cr )
    to last# .do-avalanche ;
event: ->chat-connect ( o -- )
    drop ctrl Z inskey ;
event: ->chat-reconnect ( group o -- )
    to last# .reconnect-chat ;
event: ->msg-nestsig ( editor stack o -- editor stack )
    .do-msg-nestsig  ctrl L inskey ;

\ coordinates

6 sfloats buffer: coord"
: coord@ ( -- addr u ) coord" 6 sfloats ;
: sf[]@ ( addr i -- sf )  sfloats + sf@ ;
: sf[]! ( addr i -- sf )  sfloats + sf! ;

[IFDEF] android
    require unix/jni-location.fs
    also android
    : coord! ( -- ) location ?dup-IF  >o
	    getLatitude  coord" 0 sf[]!
	    getLongitude coord" 1 sf[]!
	    getAltitude  coord" 2 sf[]!
	    getSpeed     coord" 3 sf[]!
	    getBearing   coord" 4 sf[]!
	    getAccuracy  coord" 5 sf[]!
	    o>
	ELSE
	    start-gps
	THEN ;
    previous
[ELSE]
    : coord! ;
[THEN]

: .coords ( addr u -- ) $>align drop
    ." Lat: " dup 0 sf[]@ .deg cr
    ." Lon: " dup 1 sf[]@ .deg cr
    ." Alt: " dup 2 sf[]@ 7 1 0 f.rdp cr
    ." Spd: " dup 3 sf[]@ 8 2 0 f.rdp cr
    ." Dir: " dup 4 sf[]@ 8 2 0 f.rdp cr
    ." Acc: " dup 5 sf[]@ 8 2 0 f.rdp cr
    drop ;

Defer msg:last

scope{ net2o-base

\g 
\g ### message commands ###
\g 
$34 net2o: msg ( -- o:msg ) \g push a message object
    perm-mask @ perm%msg and 0= !!msg-perm!!
    ?msg-context n:>o c-state off ;

msg-table >table

reply-table $@ inherit-table msg-table

net2o' emit net2o: msg-start ( $:pksig -- ) \g start message
    !!signed? 1 !!>order? $> 2dup startdate@ .ticks space 2dup .key-id
    [: .simple-id ;] $tmp notify! ;
+net2o: msg-group ( $:group -- ) \g specify a chat group
    $> msg-groups #@ d0= replay-mode @ or ?EXIT \ produce flag and set last#
    signed? IF  8 $10 !!<>=order? \ already a message there
	up@ receiver-task <> IF
	    do-avalanche
	ELSE parent @ .wait-task @ ?dup-IF
		<event o elit, last# elit, ->avalanche event>  THEN
	THEN
    THEN ;
+net2o: msg-join ( $:group -- ) \g join a chat group
    replay-mode @ IF  $> 2drop  EXIT  THEN
    signed? !!signed!! $> >group
    parent cell last# cell+ $+!
    parent @ .wait-task @ ?dup-IF
	<event parent @ elit, ->chat-connect event>  THEN ;
+net2o: msg-leave ( $:group -- ) \g leave a chat group
    signed? !!signed!! $> msg-groups #@ d0<> IF
	parent @ last# cell+ del$cell  THEN ;

+net2o: msg-signal ( $:pubkey -- ) \g signal message to one person
    !!signed? 3 !!>=order? $> keysize umin 2dup pkc over str=
    IF   <err>  THEN  2dup [: ."  @" .simple-id ;] $tmp notify+
    ."  @" .key-id <default> ;
+net2o: msg-re ( $:hash ) \g relate to some object
    !!signed? 1 4 !!<>=order? $> ."  re: " 85type forth:cr ;
+net2o: msg-text ( $:msg -- ) \g specify message string
    !!signed? 1 8 !!<>=order? ." : " $>
    2dup [: ." : " forth:type ;] $tmp notify+ forth:type forth:cr ;
+net2o: msg-object ( $:object -- ) \g specify an object, e.g. an image
    !!signed? 1 8 !!<>=order? $> ."  wrapped object: " 85type forth:cr ;
+net2o: msg-action ( $:msg -- ) \g specify message string
    !!signed? 1 8 !!<>=order? $> space 2dup [: space forth:type ;] $tmp notify+
    <warn> forth:type <default> forth:cr ;
+net2o: msg-reconnect ( $:pubkey+addr -- ) \g rewire distribution tree
    signed? !!signed!! $> peer+
    parent @ .wait-task @ ?dup-IF
	<event o elit, last# elit, ->chat-reconnect event>
    ELSE
	reconnect-chat
    THEN ;
+net2o: msg-last? ( tick -- ) msg:last ;
+net2o: msg-coord ( $:gps -- )
    !!signed? 1 8 !!<>=order? ."  GPS: " $> forth:cr .coords ;
net2o' nestsig net2o: msg-nestsig ( $:cmd+sig -- ) \g check sig+nest
    $> nest-sig dup 0= IF drop msg+
	parent @ dup IF  .wait-task @ dup up@ <> and  THEN
	?dup-IF
	    >r r@ <hide> <event o elit, ->msg-nestsig
	    up@ elit, ->wakeme r> event>
	    stop
	ELSE  do-msg-nestsig  THEN
    ELSE  replay-mode @ IF  drop  ELSE  !!sig!!  THEN  THEN ; \ balk on all wrong signatures

' msg msg-class to start-req
' pk-sig? msg-class to nest-sig

gen-table $freeze
' context-table is gen-table

set-current

:noname ( tick -- )
    last# 0= ?EXIT
    last# cell+ [: 2dup 2>r startdate@ 64over 64u> IF
	  2r> dup maxstring $10 - u< IF  $, nestsig  ELSE  2drop  THEN
      ELSE  rdrop rdrop   THEN ;] $[]map 64drop ; is msg:last

: <msg ( -- )
    \g start a msg block
    msg sign[ msg-start ;
: msg> ( -- )
    \g end a msg block by adding a signature and the group (if any)
    msg-group$ $@ dup IF  2dup pkc over str= 0=  ELSE  dup  THEN
    IF  $, msg-group  ELSE  2drop  THEN
    now>never ]pksign ;

previous

: msg-reply ( tag -- )
    reply( ." got reply " hex. pubkey $@ key>nick type cr )else( drop ) ;

: send-text ( addr u -- )
    net2o-code ['] msg-reply expect-reply-xt
    <msg $, msg-text msg> endwith
    ( cookie+request ) end-code| ;

: send-text-to ( msg u nick u -- )
    net2o-code ['] msg-reply expect-reply-xt
    <msg nick>pk dup IF  keysize umin $, msg-signal  ELSE  2drop  THEN
    $, msg-text msg> endwith
    ( cookie+request ) end-code| ;

: ?destpk ( addr u -- addr' u' )
    2dup pubkey $@ key| str= IF  2drop pkc keysize  THEN ;

also net2o-base
: join, ( -- )
    msg-group$ $@ dup IF  msg ?destpk $, msg-join
	sign[ msg-start "joined" $, msg-action msg> endwith
    ELSE  2drop  THEN ;

: silent-join, ( -- )
    last# $@ dup IF  msg $, msg-join  endwith
    ELSE  2drop  THEN ;

: leave, ( -- )
    msg-group$ $@ dup IF  msg ?destpk $, msg-leave
	sign[ msg-start "left" $, msg-action msg> endwith
    ELSE  2drop  THEN ;

: left, ( addr u -- )
    key| $, msg-signal " left (timeout)" $, msg-action ;
previous

: send-join ( -- )
    net2o-code ['] msg-reply expect-reply-xt join,
    ( cookie+request ) end-code| ;

:noname ( -- )
    net2o-code ['] msg-reply expect-reply-xt silent-join,
    end-code ; is silent-join

: send-leave ( -- )
    net2o-code ['] msg-reply expect-reply-xt leave,
    cookie+request end-code| ;

: .chat ( -- ) replay-mode on
    msg-group$ $@ msg-groups #@ drop @ >o ?msg-context >o
    nest-string 2@ msg+ do-msg-nestsig o> o>
    replay-mode off ;

$200 Constant maxmsg#

: get-input-line ( -- addr u )  history >r  0 to history
    BEGIN  pad maxmsg# ['] accept catch
	dup dup -56 = swap -28 = or \ quit or ^c to leave
	IF    drop 2drop "/bye"
	ELSE
	    dup 0= IF
		drop dup 1+ xback-restore dup 1+ spaces dup 1+ xback-restore
		pad swap
	    ELSE
		DoError drop 0  THEN
	THEN
	dup 0= WHILE  2drop  REPEAT
    r> to history ;

: g?join ( -- )
    msg-group$ $@len IF  +resend-cmd send-join -timeout  THEN ;

: g?leave ( -- )
    msg-group$ $@len IF  +resend-cmd send-leave -timeout  THEN ;

: greet ( -- )
    net2o-code expect-reply  log !time endwith
    join, get-ip end-code ;

: chat-entry ( -- )
    <warn> ." Type ctrl-D or '/bye' as single item to quit" <default> cr ;

also net2o-base
: send-avalanche ( xt -- )
    0 >o code-buf$ cmdreset
    <msg execute msg> endwith  o>
    cmdbuf$ 4 /string 2 - msg-group$ $@ >group code-buf avalanche-msg ;
previous

\ chat helper words

Variable chat-keys

: nick>chat ( addr u -- )
    host.nick>pk dup 0= !!no-nick!! chat-keys $+[]! ;

: nicks>chat ( -- )
    ['] nick>chat @arg-loop ;

\ debugging aids for classes

: .ack ( o:ack -- o:ack )
    ." ack context:" cr
    ." rtdelay: " rtdelay 64@ 64. cr ;

: .context ( o:context -- o:context )
    ." Connected with: " pubkey $@ .key-id cr
    ack-context @ ?dup-IF  ..ack  THEN ;

: .group ( addr -- )
    $@ 2dup printable? IF  forth:type  ELSE  ." @" .key-id  THEN ;

: .notify ( -- )
    ." notify " notify? ? ." led " notify-rgb hex. notify-on . notify-off .
    ." interval " delta-notify 64>d 1000000 um/mod . drop
    ." mode " notify-mode .
    notify-text IF  ." visible"  ELSE  ." hidden"  THEN
    forth:cr ;

: get-hex ( addr u -- addr' u' n )
    bl skip 0. 2swap ['] >number $10 base-execute 2swap drop ;
: get-dec ( addr u -- addr' u' n )
    bl skip 0. 2swap ['] >number #10 base-execute 2swap drop ;

scope: notify-cmds

: on ( addr u -- ) 2drop -2 notify? ! .notify ;
: always ( addr u -- ) 2drop -3 notify? ! .notify ;
: off ( addr u -- ) 2drop 0 notify? ! .notify ;
: led ( addr u -- ) \ "<rrggbb> <on-ms> <off-ms>"
    get-hex to notify-rgb
    get-dec #500 max to notify-on
    get-dec #500 max to notify-off
    2drop .notify msg-builder ;
: interval ( addr u -- )
    0. 2swap ['] >number #10 base-execute 1 = IF  nip c@ case
	    's' of 1000 * endof
	    'm' of 60000 * endof
	    'h' of 36000000 * endof
	endcase
    ELSE  2drop  THEN  1000000 um* d>64 to delta-notify .notify ;
: mode ( addr u -- )
    get-dec 3 and to notify-mode 2drop .notify msg-builder ;
: visible ( addr u -- )
    2drop true to notify-text .notify ;
: hidden ( addr u -- )
    2drop false to notify-text .notify ;

}scope

[IFUNDEF] find-name-in
    synonym find-name-in (search-wordlist)
[THEN]
[IFUNDEF] (.time)
    : (.time) ( delta-f -- )
	fdup 1e f>= IF  13 9 0 f.rdp ." s "   EXIT  THEN  1000 fm*
	fdup 1e f>= IF  10 6 0 f.rdp ." ms "  EXIT  THEN  1000 fm*
	fdup 1e f>= IF   7 3 0 f.rdp ." µs "  EXIT  THEN  1000 fm*
	f>s 3 .r ." ns " ;
[THEN]

: .chathelp ( addr u -- addr u )
    ." /" source 7 /string type cr ;

also net2o-base scope: chat-/cmds

: me ( addr u -- )
    \U me <action>
    \G me: send remaining string as action
    [: $, msg-action ;] send-avalanche .chat ;

: peers ( addr u -- ) 2drop
    \U peers
    \G peers: list peers in all groups
    msg-groups [: dup .group ." : "
      cell+ $@ bounds ?DO
	  space I @ >o pubkey $@ .key-id space
	  ack@ .rtdelay 64@ 64>f 1n f* (.time) o>
      cell +LOOP  forth:cr ;] #map ;

: here ( addr u -- ) 2drop
    \U here
    \G here: send your coordinates
    coord! coord@ 2dup 0 -skip nip 0= IF  2drop
    ELSE  [: $, msg-coord ;] send-avalanche .chat  THEN ;

: help ( addr u -- )
    \U help
    \G help: list help
    bl skip '/' skip
    2dup [: ."     \U " forth:type ;] $tmp ['] .chathelp search-help
    [: ."     \G " forth:type ':' forth:emit ;] $tmp ['] .cmd search-help ;

: invitations ( addr u -- )
    \U invitations
    \G invitations: handle invitations: accept, ignore or block invitations
    2drop .invitations ;

: chats ( addr u -- ) 2drop ." chats: "
    \U chats
    \G chats: list all chats
    msg-groups [: >r
      r@ $@ msg-group$ $@ str= IF ." *" THEN
      r@ .group ." [" r> cell+ $@len cell/ 0 .r ." ]" space ;] #map
    forth:cr ;

: chat ( addr u -- )
    \U chat @user|group
    \G chat: switch to chat with user or group
    over c@ '@' = IF  1 /string nick>pk key|  THEN
    2dup msg-groups #@ nip 0= IF
	." That chat isn't active" forth:cr 2drop \ !!FIXME!!
    ELSE
	msg-group$ $!
    THEN  0. chats ;

: nat ( addr u -- )  2drop
    \U nat
    \G nat: list nat traversal information of all peers in all groups
    msg-groups [: dup ." ===== Group: " .group ."  =====" forth:cr
      cell+ $@ bounds ?DO
	  ." --- " I @ >o pubkey $@ .key-id ." : " return-address .addr-path
	  ."  ---" forth:cr .nat-addrs o>
      cell +LOOP ;] #map ;

: notify ( addr u -- )
    \U notify always|on|off|led <rgb> <on-ms> <off-ms>|interval <time>[smh]|mode 0-3
    \G notify: Change notificaton settings
    bl skip bl $split 2swap ['] notify-cmds >body find-name-in dup IF
	name>int execute
    ELSE  nip IF  ." Unknown notify command" forth:cr  ELSE  .notify  THEN
    THEN ;

}scope

: do-chat-cmds ( addr u -- )
    1 /string bl $split 2swap
    2dup ['] chat-/cmds >body find-name-in
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

50 Value last-chat#

: ?chat-group ( -- )
    msg-group$ $@len 0= IF  0 chat-keys $[]@ key| msg-group$ $!  THEN
    msg-group$ $@ load-msg  msg-group$ $@ last-chat# display-lastn ;

also net2o-base
: reconnect, ( group -- )
    cell+ $@ cell safe/string bounds ?DO
	[: 0 punch-addrs $[] @ o>addr forth:type
	  pubkey $@ key| tuck forth:type forth:emit ;]
	I @ .$tmp
	reconnect( ." send reconnect: " 2dup 2dup + 1- c@ 1+ - .addr$ forth:cr )
	$, msg-reconnect
    cell +LOOP ;

: send-reconnects ( group o:connection -- )  o to connection
    net2o-code expect-reply msg
    dup  $@ ?destpk $, msg-leave  reconnect,
    sign[ msg-start "left" $, msg-action msg>
    endwith cookie+request end-code| ;
previous

: send-reconnect ( group -- )
    dup cell+ $@
    case
	0    of  2drop  endof
	cell of  @ >o o to connection +resend-cmd send-leave o>  endof
	default: drop @ .send-reconnects
    endcase ;
: disconnect-group ( group -- ) >r
    r@ cell+ $@ bounds ?DO  I @  cell +LOOP
    r> cell+ $@len 0 +DO  >o o to connection +resend-cmd
    ret-beacon disconnect-me o>  cell +LOOP ;
: disconnect-all ( group -- ) >r
    r@ cell+ $@ bounds ?DO  I @  cell +LOOP
    r> cell+ $@len 0 +DO  >o o to connection +resend-cmd send-leave
    ret-beacon disconnect-me o>  cell +LOOP ;

: leave-chat ( group -- )
    dup send-reconnect disconnect-group
\    disconnect-all
;
: leave-chats ( -- )
    msg-groups ['] leave-chat #map ;

: group-chat ( -- ) chat-entry \ ['] cmd( >body on
    [: up@ wait-task ! ret+beacon ;] IS do-connect
    BEGIN  get-input-line
	2dup "/bye" str= 0= >r
	msg-group$ $@ msg-groups #@ 0> r> and  WHILE
	    @ >o msg-context @ .avalanche-text o>
    REPEAT  drop 2drop leave-chats ;

: msg-timeout ( -- )  1 ack@ .timeouts +! >next-timeout
    cmd-resend? IF  reply( ." Resend to " pubkey $@ key>nick type cr )
    ELSE  EXIT  THEN
    timeout-expired? IF
	msg-group$ $@len IF
	    pubkey $@ ['] left, send-avalanche .chat
	THEN
	n2o:dispose-context
    THEN ;

: +resend-msg  ['] msg-timeout  timeout-xt ! o+timeout ;

:noname ( addr u o:context -- )
    avalanche( ." Send avalance to: " pubkey $@ key>nick type cr )
    o to connection +resend-msg
    net2o-code ['] msg-reply expect-reply-xt msg $, nestsig endwith
    end-code ; is avalanche-to

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