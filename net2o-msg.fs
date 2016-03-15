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

Defer avalanche-to ( addr u o:context -- )
Defer pk-connect ( key u cmdlen datalen -- )
Defer addr-connect ( key+addr u cmdlen datalen -- )
Defer pk-peek? ( addr u0 -- flag )

: avalanche-msg ( msg u1 -- )
    \G forward message to all next nodes of that message group
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
User skip-sig?

sema msglog-sema

: ?msg-context ( -- o )
    msg-context @ dup 0= IF
	drop  n2o:new-msg dup msg-context !
    THEN ;

: init-chatlog ( -- ) ?.net2o s" ~/.net2o/chats" $1FF init-dir ;

: >chatid ( group u -- id u )  defaultkey sec@ keyed-hash#128 ;

: msg-log@ ( group u -- addr u )
    [: msg-logs #@ save-mem ;] msglog-sema c-section ;

: save-msgs ( group u -- )
    n2o:new-msg >o enc-file $off
    2dup msg-log@ over >r
    [: bounds ?DO
	  I $@ net2o-base:$, net2o-base:nestsig
      cell +LOOP ;]
    gen-cmd$ 2drop tmp$ @ enc-file ! tmp$ off
    r> free throw  dispose o>
    >chatid [: ." ~/.net2o/chats/" 85type ;] $tmp enc-filename $!
    pk-off  key-list encfile-rest ;

: vault>msg ( -- )
    [: n2o:new-msg >o parent off do-cmd-loop dispose o> ;]
    is write-decrypt ;

: load-msg ( group u -- )
    >chatid [: ." ~/.net2o/chats/" 85type ." .v2o" ;] $tmp
    2dup file-status nip no-file# = IF  2drop EXIT  THEN
    replay-mode on  skip-sig? on
    vault>msg  decrypt-file
    replay-mode off  skip-sig? off ;

event: ->save-msgs over >r save-msgs r> free throw ;

: save-msgs& ( addr u -- )
    file-task 0= IF  create-file-task  THEN
    <event save-mem e$, ->save-msgs file-task event> ;

: ?msg-log ( -- )
    msg-group$ $@ msg-logs #@ d0= IF
	s" " msg-group$ $@ msg-logs #!  THEN ;

: +msg-log ( addr u -- flag )
    ?msg-log
    last# cell+ $[]# >r
    [: last# cell+ $ins[]date ;] msglog-sema c-section
    r> last# cell+ $[]# <>
    dup otr-mode @ replay-mode @ or 0= and
    IF  msg-group$ $@ save-msgs&  THEN ;

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
    msg@ +msg-log replay-mode @ 0= and IF
	msg@ msg-display msg-notify
    THEN
    msg- ;

: display-lastn ( addr u n -- )
    n2o:new-msg >o parent off
    cells >r msg-log@ over { log } dup r> - 0 max /string bounds ?DO
	I $@ msg-display
    cell +LOOP   log free throw  dispose o> ;

: >group ( addr u -- )
    2dup msg-groups #@ d0=
    IF  "" 2swap msg-groups #!  ELSE  2drop  THEN ;

Defer silent-join

\ !!FIXME!! should use an asynchronous "do-when-connected" thing

: +unique-con ( -- ) o last# cell+ +unique$ ;

: reconnect-chat ( -- )
    peer@ 2dup d0<> IF
	save-mem peer-  over >r
	reconnect( ." reconnect " 2dup 2dup + 1- c@ 1+ - .addr$ cr )
	0 >o last# >r $A $A addr-connect
	o to connection r> to last# silent-join +unique-con o>
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
    :noname ctrl U inskey ctrl D inskey ; is aback
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
    parent @ .+unique-con
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
:noname skip-sig? @ IF check-date ELSE pk-sig? THEN ; msg-class to nest-sig

gen-table $freeze
' context-table is gen-table

set-current

:noname ( tick -- )
    last# 0= ?EXIT
    last# cell+ [: 2dup 2>r startdate@ 64over 64u> IF
	  2r> dup maxstring $10 - u< IF  $, nestsig  ELSE  2drop  THEN
      ELSE  rdrop rdrop   THEN ;] $[]map 64drop ; is msg:last

: <msg ( -- )
    \G start a msg block
    msg sign[ msg-start ;
: msg> ( -- )
    \G end a msg block by adding a signature and the group (if any)
    msg-group$ $@ dup IF  2dup pkc over str= 0=  ELSE  dup  THEN
    IF  $, msg-group  ELSE  2drop  THEN
    now>never ]pksign ;

previous

: msg-reply ( tag -- )
    reply( ." got reply " hex. pubkey $@ key>nick type cr )else( drop ) ;
: expect-msg ( --- ) ['] msg-reply expect-reply-xt ;

: send-text ( addr u -- )
    net2o-code expect-msg
    <msg $, msg-text msg> endwith
    ( cookie+request ) end-code| ;

: send-text-to ( msg u nick u -- )
    net2o-code expect-msg
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
    key| $, msg-signal "left (timeout)" $, msg-action ;
previous

: send-join ( -- )
    net2o-code expect-msg join,
    ( cookie+request ) end-code| ;

:noname ( -- )
    net2o-code expect-msg silent-join,
    end-code ; is silent-join

: send-leave ( -- )
    net2o-code expect-msg leave,
    cookie+request end-code| ;

: .chat ( -- ) replay-mode on
    msg-group$ $@ msg-groups #@ drop @ >o ?msg-context >o
    nest-string 2@ msg+ do-msg-nestsig
    msg-group$ $@ 1 display-lastn
    replay-mode off
    msg-group$ $@ save-msgs o> o> notify- ;

$200 Constant maxmsg#

: xclear ( n -- )
    1+ dup xback-restore dup spaces xback-restore ;

: get-input-line ( -- addr u )  history >r  0 to history
    BEGIN  pad maxmsg# ['] accept catch
	dup dup -56 = swap -28 = or \ quit or ^c to leave
	IF    drop 2drop "/bye"
	ELSE
	    dup 0= IF
		drop dup xclear pad swap
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
    net2o-code expect-reply
    log !time endwith join, get-ip end-code ;

: chat-entry ( -- )  init-chatlog
    <warn> ." Type ctrl-D or '/bye' as single item to quit" <default> cr ;

also net2o-base
: send-avalanche ( xt -- )
    0 >o code-buf$ cmdreset
    <msg execute msg> endwith  o>
    cmdbuf$ 4 /string 2 - msg-group$ $@ >group code-buf avalanche-msg ;
previous

\ chat helper words

Variable chat-keys

: @/ ( addr u -- addr1 u1 addr2 u2 ) '@' $split ;
: @/2 ( addr u -- addr2 u2 ) '@' $split 2nip ;

: @nick>chat ( addr u -- )
    host.nick>pk dup 0= !!no-nick!! chat-keys $+[]! ;

: @nicks>chat ( -- )
    ['] @nick>chat @arg-loop ;

: nick>chat ( addr u -- )
    @/ dup IF
	host.nick>pk dup 0= !!no-nick!!
	[: 2swap type ." @" type ;] $tmp
    ELSE  2drop  THEN
    chat-keys $+[]! ;

: nicks>chat ( -- )
    ['] nick>chat arg-loop ;

\ debugging aids for classes

: .ack ( o:ack -- o:ack )
    ." ack context:" cr
    ." rtdelay: " rtdelay 64@ 64. cr ;

: .context ( o:context -- o:context )
    ." Connected with: " .con-id cr
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
    bl skip '$' skip 0. 2swap ['] >number $10 base-execute 2swap drop ;
: get-dec ( addr u -- addr' u' n )
    bl skip '#' skip 0. 2swap ['] >number #10 base-execute 2swap drop ;

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

also net2o-base scope: /chat

: me ( addr u -- )
    \U me <action>
    \G me: send remaining string as action
    [: $, msg-action ;] send-avalanche .chat ;

: peers ( addr u -- ) 2drop
    \U peers
    \G peers: list peers in all groups
    msg-groups [: dup .group ." : "
      cell+ $@ bounds ?DO
	  space I @ >o .con-id space
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

: chats ( addr u -- ) 2drop ." ===== chats: "
    \U chats
    \G chats: list all chats
    msg-groups [: >r
      r@ $@ msg-group$ $@ str= IF ." *" THEN
      r@ .group ." [" r> cell+ $@len cell/ 0 .r ." ]" space ;] #map
    ."  =====" forth:cr ;

: nat ( addr u -- )  2drop
    \U nat
    \G nat: list nat traversal information of all peers in all groups
    \U renat
    \G renat: redo nat traversal
    msg-groups [: dup ." ===== Group: " .group ."  =====" forth:cr
      cell+ $@ bounds ?DO
	  ." --- " I @ >o .con-id ." : " return-address .addr-path
	  ."  ---" forth:cr .nat-addrs o>
      cell +LOOP ;] #map ;

: notify ( addr u -- )
    \U notify always|on|off|led <rgb> <on-ms> <off-ms>|interval <time>[smh]|mode 0-3
    \G notify: Change notificaton settings
    bl skip bl $split 2swap ['] notify-cmds >body find-name-in dup IF
	name>int execute
    ELSE  nip IF  ." Unknown notify command" forth:cr  ELSE  .notify  THEN
    THEN ;

: beacons ( addr u -- )
    \U beacons
    \G beacons: list all beacons
    2drop ." === beacons ===" forth:cr
    beacons [: dup $@ .address space
      cell+ $@ over 64@ .ticks space
      1 64s safe/string bounds ?DO
	  I 2@ ?dup-IF ..con-id space THEN .name
      2 cells +LOOP forth:cr ;] #map ;

    \U n2o <cmd>
    \G n2o: Execute normal n2o command
}scope

: do-chat-cmds ( addr u -- )
    word-args  1 /string bl $split 2swap
    2dup ['] /chat >body find-name-in
    ?dup-IF  nip nip name>int execute
    ELSE  <err> ." unknown command: " forth:type <default> forth:cr  THEN ;

: avalanche-text ( addr u -- )
    [: BEGIN  dup  WHILE  over c@ '@' = WHILE  2dup { oaddr ou }
		  bl $split 2swap 1 /string ':' -skip nick>pk \ 0. if no nick
		  2dup d0= IF  2drop 2drop oaddr ou true
		  ELSE  $, msg-signal false  THEN
	      UNTIL  THEN  THEN
      $, msg-text ;] send-avalanche .chat ;

previous

50 Value last-chat#

: load-msgn ( addr u n -- )
    >r 2dup load-msg r> display-lastn ;

: +group ( -- )
    msg-group$ $@ dup IF
	2dup msg-groups #@ d0<> IF
	    +unique-con
	ELSE  o { w^ group } group cell 2swap msg-groups #!  THEN
    ELSE  2drop  THEN ;

: msg-timeout ( -- )
    cmd-resend? IF  reply( ." Resend to " pubkey $@ key>nick type cr )
	>next-timeout
    ELSE  -timeout EXIT  THEN
    timeout-expired? IF
	msg-group$ $@len IF
	    pubkey $@ ['] left, send-avalanche .chat
	THEN
	n2o:dispose-context
    THEN ;

: +resend-msg  ['] msg-timeout  timeout-xt ! o+timeout ;

: chat-connect ( addr u -- )
    $A $A pk-connect +resend-msg  greet +group ;

: key-ctrlbit ( -- n )
    \G return a bit mask for the control key pressed
    1 key dup $20 < >r lshift r> and ;

: wait-key ( -- )
    BEGIN  key-ctrlbit [ 1 ctrl L lshift 1 ctrl Z lshift or ]L
    and 0=  UNTIL ;

: chats# ( -- n ) 0 msg-groups
    [: dup $@len keysize < IF  drop 1  ELSE  cell+ $[]# THEN  + ;] #map ;

: wait-chat ( -- )
    chat-keys [: @/2 dup 0= IF  2drop  EXIT  THEN
      2dup keysize2 safe/string tuck <info> type IF '.' emit  THEN
      .key-id space ;] $[]map
    ." is not online. press key to recheck."
    [: 0 to connection -56 throw ;] is do-disconnect
    [: false chat-keys [: @/2 key| pubkey $@ key| str= or ;] $[]map
	IF  bl inskey  THEN  up@ wait-task ! ;] is do-connect
    wait-key cr [: up@ wait-task ! ;] IS do-connect ;

: last-chat-peer ( -- chat )
    msg-group$ $@ msg-groups #@ dup cell- 0 max /string
    IF  @  ELSE  drop 0  THEN ;

: search-connect ( key u -- o/0 )
    0 [: drop 2dup key| pubkey $@ key| str= o and  dup 0= ;] search-context
    nip nip  dup to connection ;

: search-peer ( -- chat )
    false chat-keys
    [: @/2 key| rot dup 0= IF drop search-connect
      ELSE  nip nip  THEN ;] $[]map ;

: search-chat ( -- chat )
    group-master @ IF  last-chat-peer  ELSE  search-peer  ThEN ;

: key>group ( addr u -- pk u )
    @/ 2swap tuck msg-group$ $!
    0= IF  2dup key| msg-group$ $!  THEN ; \ 1:1 chat-group=key

: ?load-msgn ( -- )
    msg-group$ $@ msg-logs #@ d0= IF
	msg-group$ $@ last-chat# load-msgn  THEN ;

: chat-connects ( -- )
    chat-keys [: key>group ?load-msgn
      dup 0= IF  msg-group$ $@ msg-groups #!  EXIT  THEN
      2dup search-connect ?dup-IF  .+group 2drop EXIT  THEN
      2dup pk-peek?  IF  chat-connect  ELSE  2drop  THEN ;] $[]map ;

: ?wait-chat ( -- ) 0. /chat:chats
    BEGIN  chats# 0= WHILE  wait-chat chat-connects  REPEAT ; \ stub

scope{ /chat
: chat ( addr u -- )
    \U chat @user|group
    \G chat: switch to chat with user or group
    chat-keys $[]off nick>chat 0 chat-keys $[]@ key>group
    msg-group$ $@ msg-groups #@ dup 0= IF  2drop
	nip IF  chat-connects
	ELSE  ." That chat isn't active" forth:cr  THEN
    ELSE
	bounds ?DO  2dup I @ .pubkey $@ key2| str= 0= WHILE  cell +LOOP
	    2drop chat-connects  ELSE  UNLOOP 2drop THEN
    THEN  0. chats ;
}scope

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
	drop @ .send-reconnects
    0 endcase ;
: disconnect-group ( group -- ) >r
    r@ cell+ $@ bounds ?DO  I @  cell +LOOP
    r> cell+ $@len 0 +DO  >o o to connection +resend-cmd
    disconnect-me o>  cell +LOOP ;
: disconnect-all ( group -- ) >r
    r@ cell+ $@ bounds ?DO  I @  cell +LOOP
    r> cell+ $@len 0 +DO  >o o to connection +resend-cmd send-leave
    disconnect-me o>  cell +LOOP ;

: leave-chat ( group -- )
    dup send-reconnect disconnect-group ;

: leave-chats ( -- )
    msg-groups ['] leave-chat #map ;

: do-chat ( -- ) chat-entry \ ['] cmd( >body on
    [: up@ wait-task ! ;] IS do-connect
    BEGIN  get-input-line
	2dup "/bye" str= >r 2dup "\\bye" str= r> or 0= WHILE
	    over c@ dup '/' = swap '\' = or IF  do-chat-cmds  ELSE
		msg-group$ $@ msg-groups #@ 0> IF
		    @ >o msg-context @ .avalanche-text o>
		ELSE  drop  nip xclear  THEN
	    THEN
    REPEAT  2drop leave-chats ;

:noname ( addr u o:context -- )
    avalanche( ." Send avalance to: " pubkey $@ key>nick type cr )
    o to connection +resend-msg
    net2o-code expect-msg msg $, nestsig endwith
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