\ messages                                           06aug2014py

\ Copyright (C) 2014-2016   Bernd Paysan

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

Forward avalanche-to ( addr u o:context -- )
Forward pk-connect ( key u cmdlen datalen -- )
Forward addr-connect ( key+addr u cmdlen datalen xt -- )
Forward pk-peek? ( addr u0 -- flag )

: ?hash ( addr u hash -- ) >r
    2dup r@ #@ d0= IF  "" 2swap r> #!  ELSE  2drop rdrop  THEN ;

: >group ( addr u -- )  msg-groups ?hash ;

: avalanche-msg ( msg u1 otr-flag o:connect -- )
    \G forward message to all next nodes of that message group
    { d: msg otr-flag }
    last# cell+ $@ dup IF
	bounds ?DO  I @ o <> IF  msg otr-flag I @ .avalanche-to  THEN
	cell +LOOP
    ELSE  2drop  THEN ;

Variable msg-group$
Variable group-master
Variable msg-logs
Variable otr-mode
User replay-mode
User skip-sig?

Sema msglog-sema

: ?msg-context ( -- o )
    msging-context @ dup 0= IF
	drop
	msg-context @ 0= IF
	    n2o:new-msg msg-context !
	THEN
	n2o:new-msging dup msging-context !
    THEN ;

: >chatid ( group u -- id u )  defaultkey sec@ keyed-hash#128 ;

: msg-log@ ( last# -- addr u )
    [: cell+ $@ save-mem ;] msglog-sema c-section ;

: serialize-log ( addr u -- $addr )
    [: bounds ?DO
	  I $@ net2o-base:$, net2o-base:nestsig
      cell +LOOP ;]
    gen-cmd ;

Variable saved-msg$
64Variable saved-msg-ticks

: save-msgs ( last -- )
    msg( ." Save messages" cr )
    ?.net2o/chats  n2o:new-msging >o
    dup msg-log@ over >r  serialize-log enc-file $!buf
    r> free throw  dispose o>
    $@ >chatid chat-85 .chats/ enc-filename $!
    pk-off  key-list encfile-rest ;

: save-all-msgs ( -- )
    saved-msg$ $@ bounds ?DO  I @ save-msgs  cell +LOOP
    saved-msg$ $free ;

: save-msgs? ( -- )
    saved-msg-ticks 64@ ticker 64@ 64u<= IF  save-all-msgs
	ticks config:savedelta& 2@ d>64 64+ saved-msg-ticks 64!  THEN ;

: next-saved-msg ( -- time )
    saved-msg-ticks 64@ 64dup 64#0 64= IF
	64drop ticks 64dup saved-msg-ticks 64!  THEN ;

: msg-eval ( addr u -- )
    n2o:new-msging >o parent off do-cmd-loop dispose o> ;

: vault>msg ( -- )
    ['] msg-eval is write-decrypt ;

: load-msg ( group u -- )  2dup >group
    >chatid chat-85 .chats/ [: type ." .v2o" ;] $tmp
    2dup file-status nip no-file# = IF  2drop EXIT  THEN
    replay-mode on  skip-sig? on
    vault>msg  ['] decrypt-file catch
    ?dup-IF  DoError 2drop
	\ try read backup instead
	[: enc-filename $. '~' emit ;] $tmp ['] decrypt-file catch
	?dup-IF  DoError 2drop  THEN
    THEN  replay-mode off  skip-sig? off ;

: >load-group ( group u -- )
    2dup msg-logs #@ d0= IF  2dup load-msg  THEN >group ;

event: ->save-msgs ( last# -- ) saved-msg$ +unique$ ;
event: ->save-all-msgs ( task -- )
    save-all-msgs restart ;

: !save-all-msgs ( -- )  file-task 0= ?EXIT
    <event up@ elit, ->save-all-msgs file-task event> stop ;

: save-msgs& ( -- )
    file-task 0= IF  create-file-task  THEN
    <event last# elit, ->save-msgs file-task event> ;

: ?msg-log ( addr u -- )  msg-logs ?hash ;

: +msg-log ( addr u -- addr' u' / 0 0 )
    last# $@ ?msg-log
    [: last# cell+ $ins[]date
      dup -1 = IF drop #0. 0 to last#  ELSE  last# cell+ $[]@  THEN
    ;] msglog-sema c-section ;
: ?save-msg ( -- )
    last# otr-mode @ replay-mode @ or 0= and
    IF  save-msgs&  THEN ;

Sema queue-sema

\ peer queue

: peer> ( -- addr / 0 )
    [: peers[] back> ;] queue-sema c-section ;
: >peer ( addr u -- )
    [: peers[] $+[]! ;] queue-sema c-section ;

\ events

: msg-display ( addr u -- )
    sigpksize# - 2dup + sigpksize# >$  c-state off
    do-nestsig msg:end ;

: >msg-log ( addr u -- addr' u )
    last# >r +msg-log ?save-msg r> to last# ;

Variable otr-log
: >otr-log ( addr u -- addr' u )
    [: otr-log $ins[]date
      dup -1 = IF  drop #0.  ELSE  otr-log $[]@  THEN
    ;] msglog-sema c-section ;

: do-msg-nestsig ( addr u -- )
    parent @ .msg-context @ .msg-display msg-notify ;

: display-lastn ( addr u n -- )
    otr-mode @ >r otr-mode off
    [: n2o:new-msg >o parent off
      cells >r ?msg-log last# msg-log@ 2dup { log u }
      dup r> - 0 max /string bounds ?DO
	  I $@ ['] msg-display catch IF  ." invalid entry" cr 2drop  THEN
      cell +LOOP
      log free dispose o> throw ;] catch
    r> otr-mode ! throw ;

: display-one-msg ( addr u -- )
    n2o:new-msg >o parent off
    ['] msg-display catch IF  ." invalid entry" cr 2drop  THEN
    dispose o> ;

Forward silent-join

\ !!FIXME!! should use an asynchronous "do-when-connected" thing

: +unique-con ( -- ) o last# cell+ +unique$ ;
Forward +chat-control

: chat-silent-join ( -- )
    reconnect( ." silent join " o hex. connection hex. cr )
    o to connection
    ?msg-context >o silent-last# @ to last# o>
    reconnect( ." join: " last# $. cr )
    +unique-con silent-join ;

: chat-silent-rqd ( n -- )
    reconnect( ." silent requst" cr )
    clean-request chat-silent-join ;

: ?nat ( -- )  o to connection
    net2o-code nat-punch end-code ;

: ?chat-nat ( -- )
    ['] chat-silent-rqd rqd! ?nat ;

: chat-rqd-nat ( n -- )
    reconnect( ." chat req done, start nat traversal" cr )
    connect-rest  +flow-control +resend ?chat-nat ;

: chat-rqd-nonat ( n -- )
    reconnect( ." chat req done, start silent join" cr )
    connect-rest  +flow-control +resend chat-silent-join ;

User peer-buf

: reconnect-chat ( -- )
    peer> ?dup-IF
	peer-buf $off peer-buf !  last# peer-buf $@
	reconnect( ." reconnect " 2dup 2dup + 1- c@ 1+ - .addr$ cr )
	reconnect( ." in group: " last# dup hex. $. cr )
	0 >o $A $A [: reconnect( ." prepare reconnection" cr )
	  ?msg-context >o silent-last# ! o>
	  ['] chat-rqd-nat ['] chat-rqd-nonat ind-addr @ select rqd! ;]
	addr-connect o>
    THEN ;

event: ->avalanche ( addr u otr-flag o group -- )
    avalanche( ." Avalanche to: " dup hex. cr )
    to last# .avalanche-msg ;
event: ->chat-connect ( o -- )
    drop ctrl Z inskey ;
event: ->chat-reconnect ( o group -- )
    to last# .reconnect-chat ;
event: ->msg-nestsig ( addr u o group -- )
    to last# .do-msg-nestsig  ctrl L inskey ;

\ coordinates

6 sfloats buffer: coord"
90e coord" sfloat+ sf!
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
    :noname level# @ 0> IF  -1 level# +!
	ELSE  ctrl U inskey ctrl D inskey THEN ; is aback
    previous
[ELSE]
    [IFDEF] has-gpsd?
	s" unix/gpslib.fs" ' required catch [IF]
	    2drop : coord! ;
	[ELSE]
	    0 Value gps-opened?
	    : coord! ( -- ) gps-opened? 0= IF
		    gps-local-open 0= to gps-opened?
		    gps-opened? 0= ?EXIT
		THEN
		gps-fix { fix }
		fix gps:gps_fix_t-latitude  df@ coord" 0 sf[]!
		fix gps:gps_fix_t-longitude df@ coord" 1 sf[]!
		fix gps:gps_fix_t-altitude  df@ coord" 2 sf[]!
		fix gps:gps_fix_t-speed     df@ coord" 3 sf[]!
		fix gps:gps_fix_t-track     df@ coord" 4 sf[]!
		fix gps:gps_fix_t-epx df@ f**2
		fix gps:gps_fix_t-epy df@ f**2
		f+ fsqrt                        coord" 5 sf[]! ;
	[THEN]
    [ELSE]
	: coord! ( -- ) ;
    [THEN]
[THEN]

: .coords ( addr u -- ) $>align drop
    dup 0 sf[]@ fdup fabs .deg f0< 'S' 'N' rot select emit space
    dup 1 sf[]@ fdup fabs .deg f0< 'W' 'E' rot select emit space
    dup 2 sf[]@ 7 1 0 f.rdp ." m "
    dup 3 sf[]@ 8 2 0 f.rdp ." km/h "
    dup 4 sf[]@ 8 2 0 f.rdp ." ° ~"
    dup 5 sf[]@ fsplit 0 .r '.' emit 100e f* f>s .## ." m"
    drop ;

Forward msg:last?
Forward msg:last

: push-msg ( addr u otr-flag o:parent -- )
    up@ receiver-task <> IF
	avalanche-msg
    ELSE wait-task @ ?dup-IF
	    >r <event >r e$, r> elit, o elit, last# elit,
	    ->avalanche r> event>
	ELSE  drop 2drop  THEN
    THEN ;
: show-msg ( addr u -- )
    parent @ dup IF  .wait-task @ dup up@ <> and  THEN
    ?dup-IF
	>r r@ <hide> <event e$, o elit, last# elit, ->msg-nestsig
	r> event>
    ELSE  do-msg-nestsig  THEN ;

scope{ net2o-base

\g 
\g ### message commands ###
\g 

reply-table $@ inherit-table msg-table

$20 net2o: msg-start ( $:pksig -- ) \g start message
    !!signed? 1 !!>order? $> msg:start ;
+net2o: msg-tag ( $:group -- ) \g tagging (can be anywhere)
    !!signed? $> msg:tag ;
+net2o: msg-id ( $:id -- ) \g a hash id
    !!signed? 2 !!>=order? $> msg:id ;
$24 net2o: msg-signal ( $:pubkey -- ) \g signal message to one person
    !!signed? 2 !!>=order? $> msg:signal ;
+net2o: msg-re ( $:hash ) \g relate to some object
    !!signed? 4 !!>=order? $> msg:re ;
+net2o: msg-text ( $:msg -- ) \g specify message string
    !!signed? 8 !!>=order? $> msg:text ;
+net2o: msg-object ( $:object type -- ) \g specify an object, e.g. an image
    !!signed? 8 !!>=order? 64>n $> rot msg:object ;
+net2o: msg-action ( $:msg -- ) \g specify action string
    !!signed? 8 !!>=order? $> msg:action ;
$2B net2o: msg-coord ( $:gps -- ) \g GPS coordinates
    !!signed? 8 !!>=order? $> msg:coord ;

gen-table $freeze
' context-table is gen-table

\ Code for displaying messages

:noname ( addr u -- )
    2dup startdate@ .ticks space 2dup .key-id
    [: .simple-id ;] $tmp notify! ; msg-class to msg:start
:noname ( addr u -- )
    space <warn> '#' forth:emit forth:type <default> ; msg-class to msg:tag
:noname ( addr u -- )
    keysize umin 2dup pkc over str=
    IF   <err>  THEN  2dup [: ."  @" .simple-id ;] $tmp notify+
    ."  @" .key-id <default> ; msg-class to msg:signal
:noname ( addr u -- )
    space <warn> ." [" 85type ." ]->" <default> ; msg-class to msg:re
:noname ( addr u -- )
    space <warn> ." [" 85type ." ]:" <default> ; msg-class to msg:id
:noname ( addr u -- )
    [: ." : " 2dup forth:type ;] $tmp notify+
    ." : " forth:type ; msg-class to msg:text
:noname ( addr u type -- )
    space <warn> 0 .r ." :[" 85type ." ]" <default> ;
msg-class to msg:object
:noname ( addr u -- )
    [: space 2dup forth:type ;] $tmp notify+
    space <warn> forth:type <default> ; msg-class to msg:action
:noname ( addr u -- )
    <warn> ."  GPS: " .coords <default> ; msg-class to msg:coord
:noname ( -- )
    parent @ ?dup-IF
	.msging-context @ ?dup-IF
	    .otr-shot @ IF <info> ."  [otr]" <default> THEN
	THEN
    ELSE
	otr-mode @ IF <info> ."  [otr]" <default> THEN
    THEN  forth:cr ; msg-class to msg:end

\g 
\g ### messaging commands ###
\g 

$34 net2o: msg ( -- o:msg ) \g push a message object
    perm-mask @ perm%msg and 0= !!msg-perm!!
    ?msg-context n:>o c-state off  otr-shot off  0 to last# ;

msging-table >table

reply-table $@ inherit-table msging-table

$21 net2o: msg-group ( $:group -- ) \g set group
    $> >group ;
+net2o: msg-join ( $:group -- ) \g join a chat group
    replay-mode @ IF  $> 2drop  EXIT  THEN
    $> >load-group parent @ >o
    +unique-con +chat-control
    wait-task @ ?dup-IF  <event o elit, ->chat-connect event>  THEN
    o> ;
+net2o: msg-leave ( $:group -- ) \g leave a chat group
    $> msg-groups #@ d0<> IF
	parent @ last# cell+ del$cell  THEN ;
+net2o: msg-reconnect ( $:pubkey+addr -- ) \g rewire distribution tree
    $> >peer
    parent @ .wait-task @ ?dup-IF
	<event o elit, last# elit, ->chat-reconnect event>
    ELSE
	reconnect-chat
    THEN ;
+net2o: msg-last? ( start end n -- ) 64>n msg:last? ;
+net2o: msg-last ( $:[tick0,msgs,..tickn] n -- ) 64>n msg:last ;
+net2o: msg-otr ( -- ) \g this message is otr, don't save it
    otr-shot on ;

: ?pkgroup ( addr u -- addr u )
    \ if no group has been selected, use the pubkey as group
    last# 0= IF  2dup + sigpksize# - keysize >group  THEN ;

net2o' nestsig net2o: msg-nestsig ( $:cmd+sig -- ) \g check sig+nest
    $> nest-sig ?dup-0=-IF
	?pkgroup otr-shot @ IF  >otr-log  ELSE  >msg-log  THEN
	2dup d0<> \ do something if it is new
	IF  replay-mode @ 0= IF
		2dup show-msg  2dup otr-shot @ parent @ .push-msg
	    THEN
	THEN  2drop
    ELSE  replay-mode @ IF  drop  ELSE  !!sig!!  THEN  THEN ; \ balk on all wrong signatures

:noname skip-sig? @ IF check-date ELSE pk-sig? THEN ;  ' msg  2dup
msging-class to start-req
msging-class to nest-sig
msg-class to start-req
msg-class to nest-sig

gen-table $freeze
' context-table is gen-table

also }scope

User hashtmp$  hashtmp$ off

: last-msg@ ( -- ticks )
    last# >r
    last# $@ ?msg-log last# cell+ $[]# ?dup-IF
	1- last# cell+ $[]@ startdate@
    ELSE  64#0  THEN   r> to last# ;
: l.hashs ( end start -- hashaddr u )
    hashtmp$ $off
    last# cell+ $[]# IF
	[: U+DO  I last# cell+ $[]@ 1- dup 1 64s - safe/string forth:type
	  LOOP ;] hashtmp$ $exec hashtmp$ $@
	\ [: 2dup dump ;] stderr outfile-execute \ dump hash inputs
    ELSE  2drop s" "  THEN \ we have nothing yet
    >file-hash 1 64s umin ;
: i.date ( i -- )
    last# cell+ $[]@ startdate@ 64#0 { 64^ x }
    x le-64! x 1 64s forth:type ;
: date>i ( date -- i )
    last# cell+ $search[]date last# cell+ $[]# 1- umin ;
: last-msgs@ ( startdate enddate n -- addr u n' )
    \G print n intervals for messages from startdate to enddate
    \G The intervals contain the same size of messages except the
    \G last one, which may contain less (rounding down).
    \G Each interval contains a 64 bit hash of the last 64 bit of
    \G each message within the interval
    last# >r >r last# $@ ?msg-log
    last# cell+ $[]#
    IF
	date>i >r date>i r> swap
	2dup - r> over 1+ >r 1- 1 max / 0 max 1+ -rot
	[: over >r U+DO  I i.date
	      dup I + I' umin 1+ I l.hashs forth:type
	  dup +LOOP  r> i.date
	  drop ;] $tmp r>
    ELSE  rdrop 64drop 64drop s" "  0 THEN   r> to last# ;

\ sync chatlog through virtual file access

termserver-class class
end-class msgfs-class

file-classes# Constant msgfs-class#
msgfs-class +file-classes

: save-to-msg ( addr u n -- )
    state-addr >o  msgfs-class# fs-class! w/o fs-create o> ;
: n2o:copy-msg ( filename u -- )
    ." copy msg: " 2dup
    over le-64@ .ticks 1 64s /string  ." ->"
    over le-64@ .ticks 1 64s /string  ." @" forth:type
    [: msgfs-class# ulit, file-type 2dup $, r/o ulit, open-sized-file
      file-reg# @ save-to-msg ;] n2o>file
    1 file-count +! forth:cr ;

$20 Value max-last#
$20 Value ask-last#

Variable ask-msg-files[]

: msg:last? ( start end n -- )
    last# $@ $, msg-group
    max-last# umin
    last-msgs@ >r $, r> ulit, msg-last ;
: ?ask-msg-files ( addr u -- )
    64#-1 64#0 { 64^ startd 64^ endd } \ byte order of 0 and -1 don't matter
    last# $@ ?msg-log
    $> bounds ?DO
	I' I 64'+ u> IF
	    I le-64@ date>i
	    I 64'+ 64'+ le-64@ date>i 1+ swap l.hashs drop 64@
	    I 64'+ 64@ 64<> IF
		I 64@ startd le-64@ 64umin
		I 64'+ 64'+ 64@ endd le-64@ 64umax
	    ELSE
		startd le-64@ 64#-1 64<> IF
		    endd startd [: 1 64s forth:type 1 64s forth:type last# $. ;]
		    ask-msg-files[] dup $[]# swap $[] $exec
		THEN
		64#-1 64#0
	    THEN  endd le-64! startd le-64!
	THEN
    2 64s +LOOP
    startd le-64@ 64#-1 64<> IF
	endd startd [: 1 64s forth:type 1 64s forth:type last# $. ;]
	ask-msg-files[] dup $[]# swap $[] $exec
    THEN ;
: msg:last ( $:[tick0,tick1,...,tickn] n -- )
    last# >r  ask-msg-files[] $[]off
    forth:. ." Messages:" forth:cr
    ?ask-msg-files ask-msg-files[] $[]# IF
	parent @ >o  expect+slurp
	cmdbuf# @ 0= IF  $10 blocksize! $1 blockalign!  THEN
	ask-msg-files[] [: n2o:copy-msg ;] $[]map o>
    ELSE
	." === nothing to sync ===" forth:cr
    THEN
    r> to last# ;

:noname ( -- 64len )
    \ poll serializes the 
    fs-outbuf $off
    fs-path $@ 2 64s /string ?msg-log
    last# msg-log@ over >r
    fs-path $@ drop le-64@ last# cell+ $search[]date \ start index
    fs-path $@ drop 64'+ le-64@ last# cell+ $search[]date 1+ \ end index
    over - >r last# cell+ $[]# 1- umin
    cells safe/string r> cells umin
    req? @ >r req? off  serialize-log   r> req? !  fs-outbuf $!buf
    r> free throw
    fs-outbuf $@len u>64 ; msgfs-class is fs-poll
:noname ( addr u mode -- )
    \G addr u is starttick endtick name concatenated together
    fs-close drop fs-path $!  fs-poll fs-size!
; msgfs-class is fs-open

\ syncing done
event: ->chat-sync-done ( -- )
    msg( ." chat-sync-done event" forth:cr )
    msg-group$ $@ ?msg-log ?save-msg
    last# $@ rows  display-lastn
    ." === sync done ===" forth:cr ;
: chat-sync-done ( -- )
    msg( ." chat-sync-done" forth:cr )
    n2o:close-all net2o-code expect-reply close-all end-code
    msg( ." chat-sync-done closed" forth:cr )
    <event ->chat-sync-done wait-task @ event> ;
: +sync-done ( -- )
    ['] chat-sync-done sync-done-xt ! ;
event: ->msg-eval ( $pack last -- )
    $@ ?msg-log { w^ buf }
    buf $@ true replay-mode ['] msg-eval !wrapper
    buf $off ;
: msg-file-done ( -- )
    msg( ." msg file done" forth:cr ) ;
:noname ( addr u mode -- )
    fs-close drop fs-path $!
    ['] msg-file-done file-xt !
; msgfs-class is fs-create
:noname ( addr u -- u )
    [ termserver-class :: fs-read ]
; msgfs-class is fs-read
:noname ( -- )
    fs-path @ 0= ?EXIT
    -1 parent @ .file-count +!@ drop \ make this atomic
    fs-inbuf $@len IF
	fs-path $@ 2 64s /string >group
	parent @ .wait-task @ ?dup-IF
	    <event 0 fs-inbuf !@ elit, last# elit,
	    ->msg-eval  event>
	ELSE
	    fs-inbuf $@ true replay-mode ['] msg-eval !wrapper fs-inbuf $off
	THEN
    THEN
    fs:fs-clear
; msgfs-class is fs-close
:noname ( perm -- )
    perm%msg and 0= !!msg-perm!!
; msgfs-class to fs-perm?
:noname ( -- date perm )
    64#0 0 ; msgfs-class is fs-get-stat
:noname ( date perm -- )
    drop 64drop ; msgfs-class is fs-set-stat
' file-start-req msgfs-class is start-req

\ message composer

: group, ( addr u -- )
    $, msg-group ;
: <msg ( -- )
    \G start a msg block
    msg-group$ $@ group, msg sign[ msg-start ;
: msg> ( -- )
    \G end a msg block by adding a signature
    now>never ]pksign ;
: msg-log, ( -- addr u )
    last-signed 2@ >msg-log ;
: otr-log, ( -- addr u )
    last-signed 2@ >otr-log ;

previous

: msg-reply ( tag -- )
    reply( ." got reply " hex. pubkey $@ key>nick forth:type forth:cr )else( drop ) ;
: expect-msg ( --- ) ['] msg-reply expect-reply-xt ;

: send-text ( addr u -- )
    net2o-code expect-msg
    <msg $, msg-text msg> end-with
    ( cookie+request ) end-code| ;

: send-text-to ( msg u nick u -- )
    net2o-code expect-msg
    <msg nick>pk dup IF  keysize umin $, msg-signal  ELSE  2drop  THEN
    $, msg-text msg> end-with
    ( cookie+request ) end-code| ;

: ?destpk ( addr u -- addr' u' )
    2dup pubkey $@ key| str= IF  2drop pkc keysize  THEN ;

: last-signdate@ ( -- 64date )
    msg-group$ $@ msg-logs #@ dup IF
	+ cell- $@ startdate@
    ELSE  2drop 64#-1  THEN ;

also net2o-base
: [msg,] ( xt -- )  last# >r
    msg-group$ $@ dup IF  msg ?destpk 2dup >group $,
	execute  end-with
    ELSE  2drop drop  THEN  r> to last# ;

: last, ( -- )
    msg-group  64#0 64#-1 ask-last# last-msgs@ >r $, r> ulit, msg-last ;

: last?, ( -- )
    msg-group  last-signdate@ { 64: date }
    64#0 lit, date slit, ask-last# ulit, msg-last?
    date 64#-1 64<> IF
	date 64#1 64+ lit, 64#-1 slit, 1 ulit, msg-last?
    THEN ;

: join, ( -- )
    [: msg-otr msg-join
      \ 64#0 lit, 64#-1 slit, ask-last# ulit, msg-last?
      sign[ msg-start "joined" $, msg-action msg> ;] [msg,] ;

: silent-join, ( -- )
    last# $@ dup IF  msg msg-otr $, msg-join  end-with
    ELSE  2drop  THEN ;

: leave, ( -- )
    [: msg-otr msg-leave
      sign[ msg-start "left" $, msg-action msg> ;] [msg,] ;

: left, ( addr u -- )
    key| $, msg-signal "left (timeout)" $, msg-action ;
previous

: send-join ( -- )
    net2o-code expect-msg join,
    ( cookie+request ) end-code| ;

: silent-join ( -- )
    net2o-code expect-msg silent-join,
    end-code ;

: send-leave ( -- )
    net2o-code expect-msg leave,
    cookie+request end-code| ;

: [group] ( xt -- flag )
    msg-group$ $@ msg-groups #@ IF
	@ >o ?msg-context .execute o> true
    ELSE
	drop "" msg-group$ $@ msg-groups #!
	0 .execute false
    THEN ;
: .chat ( addr u -- )
    [: last# >r o IF  otr-shot off 2dup do-msg-nestsig
      ELSE  2dup display-one-msg  THEN  r> to last#
      false 0 .avalanche-msg ;] [group] drop notify- ;
: .otr-chat ( addr u -- )
    [: last# >r o IF  otr-shot on 2dup do-msg-nestsig
      ELSE  2dup display-one-msg  THEN  r> to last#
      true 0 .avalanche-msg ;] [group] drop notify- ;

\ chat message, text only

msg-class class
end-class textmsg-class

' 2drop textmsg-class to msg:start
:noname space '#' emit type ; textmsg-class to msg:tag
:noname '@' emit .simple-id space ; textmsg-class to msg:signal
' 2drop textmsg-class to msg:re
' type textmsg-class to msg:text
:noname drop 2drop ; textmsg-class to msg:object
:noname ." /me " type ; textmsg-class to msg:action
:noname ." /here " 2drop ; textmsg-class to msg:coord
' noop textmsg-class to msg:end

textmsg-class ' new static-a with-allocater Constant textmsg-o
textmsg-o >o msg-table @ token-table ! o>

\ chat history browsing

64Variable line-date 64#-1 line-date 64!
Variable $lastline

: !date ( addr u -- addr u )
    2dup + sigsize# - le-64@ line-date 64! ;
: find-prev-chatline { maxlen addr -- max span addr span }
    msg-group$ $@ ?msg-log
    line-date 64@ last# cell+ $search[]date
    BEGIN  1- dup 0>= WHILE  dup last# cell+ $[]@
	dup sigpksize# - /string key| pkc over str=  UNTIL  THEN
    last# cell+ $[]@ !date ['] msg-display textmsg-o .$tmp 
    tuck addr maxlen smove
    maxlen swap addr over ;
: find-next-chatline { maxlen addr -- max span addr span }
    msg-group$ $@ ?msg-log
    line-date 64@ last# cell+ $search[]date
    BEGIN  1+ dup last# cell+ $[]# u< WHILE  dup last# cell+ $[]@
	dup sigpksize# - /string key| pkc over str=  UNTIL  THEN
    dup last# cell+ $[]# u>=
    IF    drop $lastline $@  64#-1 line-date 64!
    ELSE  last# cell+ $[]@ !date ['] msg-display textmsg-o .$tmp  THEN
    tuck addr maxlen smove
    maxlen swap addr over ;

: chat-prev-line  ( max span addr pos1 -- max span addr pos2 false )
    line-date 64@ 64#-1 64= IF
	>r 2dup swap $lastline $! r>  THEN
    clear-line find-prev-chatline
    2dup type 2dup cur-correct edit-update false ;
: chat-next-line  ( max span addr pos1 -- max span addr pos2 false )
    clear-line find-next-chatline
    2dup type 2dup cur-correct edit-update false ;
: chat-enter ( max span addr pos1 -- max span addr pos2 true )
     .all space true 64#-1 line-date 64! ;

[IFDEF] xchar-ctrlkeys
    bl cells buffer: chat-ctrlkeys
    xchar-ctrlkeys chat-ctrlkeys bl cells move
    
    chat-ctrlkeys to ctrlkeys
    
    ' chat-next-line ctrl N bindkey
    ' chat-prev-line ctrl P bindkey
    ' chat-enter     #lf    bindkey
    ' chat-enter     #cr    bindkey
    :noname #tab (xins) 0 ; #tab   bindkey
    
    xchar-ctrlkeys to ctrlkeys

    : chat-history ( -- )
	chat-ctrlkeys to ctrlkeys ;
[ELSE]
    : chat-history ( -- )
	['] chat-next-line ctrl N bindkey
	['] chat-prev-line ctrl P bindkey
	['] chat-enter     #lf    bindkey
	['] chat-enter     #cr    bindkey
	['] false          #tab   bindkey ;
[THEN]

\ chat line editor

$200 Constant maxmsg#

: xclear ( addr u -- ) x-width
    1+ dup xback-restore dup spaces xback-restore ;

: get-input-line ( -- addr u )  chat-history
    BEGIN  pad maxmsg# ['] accept catch
	dup dup -56 = swap -28 = or \ quit or ^c to leave
	IF    drop 2drop "/bye"
	ELSE
	    dup 0= IF
		drop pad swap 2dup xclear
	    ELSE
		DoError drop 0  THEN
	THEN
	dup 0= WHILE  2drop  REPEAT
    xchar-history ;

\ joining and leaving

: g?join ( -- )
    msg-group$ $@len IF  send-join -timeout  THEN ;

: g?leave ( -- )
    msg-group$ $@len IF  send-leave -timeout  THEN ;

: greet ( -- )
    net2o-code expect-reply
    log !time end-with join, get-ip end-code ;

: chat-entry ( -- )  ?.net2o/chats  word-args
    <warn> ." Type ctrl-D or '/bye' as single item to quit" <default> cr ;

: wait-2s-key ( -- )
    200 0 DO  key? ?LEAVE  10 ms  LOOP ;
: .nobody ( -- )
    <info>
    [: ." nobody's online" otr-mode @ 0= IF ." , saving away"  THEN ;] $tmp
    2dup type <default>
    wait-2s-key xclear ;

also net2o-base
: (send-avalanche) ( xt -- addr u flag )
    [: 0 >o [: sign[ msg-start execute msg> ;] gen-cmd$ o>
      +last-signed msg-log, ;] [group] ;
: (send-otr-avalanche) ( xt -- addr u flag )
    [: 0 >o [: msg-otr sign[ msg-start execute msg> ;] gen-cmd$ o>
      +last-signed otr-log, ;] [group] ;
previous
: send-avalanche ( xt -- )      (send-avalanche)
    >r .chat r> 0= IF  .nobody  THEN ;
: send-otr-avalanche ( xt -- )  (send-otr-avalanche)
    >r .otr-chat r> 0= IF  .nobody  THEN ;

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
    ." rtdelay: " rtdelay 64@ s64. cr ;

: .context ( o:context -- o:context )
    ." Connected with: " .con-id cr
    ack-context @ ?dup-IF  ..ack  THEN ;

: .group ( addr -- )
    $@ 2dup printable? IF  forth:type  ELSE  ." @" .key-id  THEN ;

: .notify ( -- )
    ." notify " config:notify?# ?
    ." led " config:notify-rgb# @ hex. config:notify-on# ? config:notify-off# ?
    ." interval " config:delta-notify& 2@ 1000000 um/mod . drop
    ." mode " config:notify-mode# @ .
    config:notify-text# @ IF  ." visible"  ELSE  ." hidden"  THEN
    forth:cr ;

: get-hex ( addr u -- addr' u' n )
    bl skip '$' skip #0. 2swap ['] >number $10 base-execute 2swap drop ;
: get-dec ( addr u -- addr' u' n )
    bl skip '#' skip #0. 2swap ['] >number #10 base-execute 2swap drop ;

scope: notify-cmds

: on ( addr u -- ) 2drop -2 config:notify?# ! .notify ;
: always ( addr u -- ) 2drop -3 config:notify?# ! .notify ;
: off ( addr u -- ) 2drop 0 config:notify?# ! .notify ;
: led ( addr u -- ) \ "<rrggbb> <on-ms> <off-ms>"
    get-hex config:notify-rgb# !
    get-dec #500 max config:notify-on# !
    get-dec #500 max config:notify-off# !
    2drop .notify msg-builder ;
: interval ( addr u -- )
    #0. 2swap ['] >number #10 base-execute 1 = IF  nip c@ case
	    's' of     #1000 * endof
	    'm' of    #60000 * endof
	    'h' of #36000000 * endof
	endcase
    ELSE  2drop  THEN  #1000000 um* config:delta-notify& 2! .notify ;
: mode ( addr u -- )
    get-dec 3 and config:notify-mode# ! 2drop .notify msg-builder ;
: visible ( addr u -- )
    2drop config:notify-text# on .notify ;
: hidden ( addr u -- )
    2drop config:notify-text# off .notify ;

}scope

: .chathelp ( addr u -- addr u )
    ." /" source 7 /string type cr ;

: .n2o-version ( -- )
    ." n2o-" net2o-version forth:type ;
: .gforth-version ( -- )
    ." gforth-"
    case threading-method
	0 of debugging-method 0= IF ." fast-"  THEN  endof
	1 of ." itc-" endof
	2 of ." ditc-" endof
    endcase
    version-string forth:type '-' forth:emit
    machine forth:type ;

also net2o-base scope: /chat

: me ( addr u -- )
    \U me <action>          send string as action
    \G me: send remaining string as action
    [: $, msg-action ;] send-avalanche ;

: otr ( addr u -- )
    \U otr on|off           turn otr mode on/off
    2dup s" on" str= >r
    s" off" str= r@ or IF   r> otr-mode !
	<info> ." === " otr-mode @ IF  ." enter"  ELSE  ." leave"  THEN
	."  otr mode ==="
    ELSE  <err> ." only 'otr on|off' are allowed" rdrop  THEN
    <default> forth:cr ;

: peers ( addr u -- ) 2drop
    \U peers                list peers
    \G peers: list peers in all groups
    msg-groups [: dup .group ." : "
      cell+ $@ bounds ?DO
	  space I @ >o .con-id space
	  ack@ .rtdelay 64@ 64>f 1n f* (.time) o>
      cell +LOOP  forth:cr ;] #map ;

: gps ( addr u -- ) 2drop
    \U gps                  send coordinates
    \G gps: send your coordinates
    coord! coord@ 2dup 0 -skip nip 0= IF  2drop
    ELSE
	[: $, msg-coord ;] send-avalanche
    THEN ;

' gps alias here

: help ( addr u -- )
    \U help                 show help
    \G help: list help
    bl skip '/' skip
    2dup [: ."     \U " forth:type ;] $tmp ['] .chathelp search-help
    [: ."     \G " forth:type ':' forth:emit ;] $tmp ['] .cmd search-help ;

: invitations ( addr u -- )
    \U invitations          handle invitations
    \G invitations: handle invitations: accept, ignore or block invitations
    2drop .invitations ;

: chats ( addr u -- ) 2drop ." ===== chats: "
    \U chats                list chats
    \G chats: list all chats
    msg-groups [: >r
      r@ $@ msg-group$ $@ str= IF ." *" THEN
      r@ .group
      ." [" r@ cell+ $@len cell/ 0 .r ." ]#"
      r@ $@ msg-logs #@ nip cell / u. rdrop ;] #map
    ." =====" forth:cr ;

: nat ( addr u -- )  2drop
    \U nat                  list NAT info
    \G nat: list nat traversal information of all peers in all groups
    \U renat                redo NAT traversal
    \G renat: redo nat traversal
    msg-groups [: dup ." ===== Group: " .group ."  =====" forth:cr
      cell+ $@ bounds ?DO
	  ." --- " I @ >o .con-id ." : " return-address .addr-path
	  ."  ---" forth:cr .nat-addrs o>
      cell +LOOP ;] #map ;

: myaddrs ( addr u -- )
    \U myaddrs              list my addresses
    \G myaddrs: list my own local addresses (debugging)
    2drop
    ." ===== all =====" forth:cr    .my-addr$s
    ." ===== public =====" forth:cr .pub-addr$s
    ." ===== private =====" forth:cr .priv-addr$s ;
: !myaddrs ( addr u -- )
    \U !myaddrs             re-obtain my addresses
    \G !myaddrs: if automatic detection of address changes fail,
    \G !myaddrs: you can use this command to re-obtain your local addresses
    2drop !my-addr ;

: notify ( addr u -- )
    \U notify always|on|off|led <rgb> <on-ms> <off-ms>|interval <time>[smh]|mode 0-3
    \G notify: Change notificaton settings
    bl skip bl $split 2swap ['] notify-cmds >body find-name-in dup IF
	name>int execute
    ELSE  nip IF  ." Unknown notify command" forth:cr  ELSE  .notify  THEN
    THEN ;

: beacons ( addr u -- )
    \U beacons              list beacons
    \G beacons: list all beacons
    2drop ." === beacons ===" forth:cr
    beacons [: dup $@ .address space
      cell+ $@ over 64@ .ticks space
      1 64s safe/string bounds ?DO
	  I 2@ ?dup-IF ..con-id space THEN .name
      2 cells +LOOP forth:cr ;] #map ;

    \U n2o <cmd>            execute n2o command
    \G n2o: Execute normal n2o command

: sync ( addr u -- )
    \U sync [+date] [-date] synchronize logs
    \G sync: synchronize chat logs, starting and/or ending at specific
    \G sync: time/date
    2drop o 0= IF msg-group$ $@ msg-groups #@
	IF @ >o rdrop ?msg-context ELSE EXIT THEN
    THEN o to connection +chat-control
    ." === sync ===" forth:cr
    net2o-code expect-reply ['] last?, [msg,] end-code ;

: version ( -- )
    \U version              version string
    \G version: print version string
    .n2o-version space .gforth-version forth:cr ;
}scope

: ?slash ( addr u -- addr u flag )
    over c@ dup '/' = swap '\' = or ;

: do-chat-cmd? ( addr u -- t / addr u f )
    ?slash dup 0= ?EXIT  drop
    1 /string bl $split 2swap
    2dup ['] /chat >body find-name-in
    ?dup-IF  nip nip name>int execute true
    ELSE  drop 1- -rot + over - false
    THEN ;

: signal-list, ( addr u -- addr' u' )  last# >r
    BEGIN  dup  WHILE  over c@ '@' = WHILE  2dup { oaddr ou }
		bl $split 2swap 1 /string ':' -skip nick>pk \ #0. if no nick
		2dup d0= IF  2drop 2drop oaddr ou true
		ELSE  $, msg-signal false  THEN
	    UNTIL  THEN  THEN  r> to last# ;

: avalanche-text ( addr u -- )
    [: signal-list, $, msg-text ;]
    otr-mode @ IF  send-otr-avalanche  ELSE  send-avalanche  THEN ;

previous

: load-msgn ( addr u n -- )
    >r 2dup load-msg r> display-lastn ;

: +group ( -- )
    msg-group$ $@ dup IF
	2dup msg-groups #@ d0<> IF
	    +unique-con
	ELSE  o { w^ group } group cell 2swap msg-groups #!  THEN
    ELSE  2drop  THEN ;

: msg-timeout ( -- )
    >next-timeout cmd-resend?
    IF  reply( ." Resend to " pubkey $@ key>nick type cr )
	timeout-expired? IF
	    msg-group$ $@len IF
		pubkey $@ ['] left, send-otr-avalanche
	    THEN
	    n2o:dispose-context
	    rdrop EXIT
	THEN
    ELSE  expected@ u< IF  -timeout  THEN  THEN ;

: +resend-msg  ['] msg-timeout  timeout-xt ! o+timeout ;

$A $C 2Value chat-bufs#

: +chat-control ( -- )
    +resend-msg +flow-control +sync-done ;

: chat#-connect ( addr u buf1 buf2 --- )
    pk-connect connection .+chat-control  greet +group ;

: chat-connect ( addr u -- )
    chat-bufs# chat#-connect ;

: key-ctrlbit ( -- n )
    \G return a bit mask for the control key pressed
    1 key dup bl < >r lshift r> and ;

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
    @/ 2swap tuck msg-group$ $!  0=
    IF  2dup key| msg-group$ $!  THEN ; \ 1:1 chat-group=key

: ?load-msgn ( -- )
    msg-group$ $@ msg-logs #@ d0= IF
	msg-group$ $@ rows load-msgn  THEN ;

: chat-connects ( -- )
    chat-keys [: key>group ?load-msgn
      dup 0= IF  2drop msg-group$ $@ >group  EXIT  THEN
      2dup search-connect ?dup-IF  .+group 2drop EXIT  THEN
      2dup pk-peek?  IF  chat-connect  ELSE  2drop  THEN ;] $[]map ;

: ?wait-chat ( -- ) #0. /chat:chats
    BEGIN  chats# 0= WHILE  wait-chat chat-connects  REPEAT
    msg-group$ $@ ; \ stub

scope{ /chat
: chat ( addr u -- )
    \U chat [group][@user]  switch/connect chat
    \G chat: switch to chat with user or group
    chat-keys $[]off nick>chat 0 chat-keys $[]@ key>group
    msg-group$ $@ msg-groups #@ dup 0= IF  2drop
	nip IF  chat-connects
	ELSE  ." That chat isn't active" forth:cr  THEN
    ELSE
	bounds ?DO  2dup I @ .pubkey $@ key2| str= 0= WHILE  cell +LOOP
	    2drop chat-connects  ELSE  UNLOOP 2drop THEN
    THEN  #0. chats ;
}scope

also net2o-base
: reconnect, ( o:connection -- )
    [: 0 punch-addrs $[] @ o>addr forth:type
      pubkey $@ key| tuck forth:type forth:emit ;] $tmp
    reconnect( ." send reconnect: " 2dup 2dup + 1- c@ 1+ - .addr$ forth:cr )
    $, msg-reconnect ;

: reconnects, ( group -- )
    cell+ $@ cell safe/string bounds U+DO
	I @ .reconnect,
    cell +LOOP ;

: send-reconnects ( group o:connection -- )  o to connection
    net2o-code expect-reply msg msg-otr
    dup  $@ ?destpk 2dup >group $, msg-leave  reconnects,
    sign[ msg-start "left" $, msg-action msg>
    end-with cookie+request end-code| ;

: send-reconnect1 ( o o:connection -- ) o to connection
    net2o-code expect-reply msg last# $@ $, msg-group
    .reconnect,  end-with  end-code| ;
previous

: send-reconnect ( group -- )
    dup cell+ $@
    case
	0    of  2drop  endof
	cell of  nip @ >o o to connection send-leave o>  endof
	drop @ .send-reconnects
    0 endcase ;
: disconnect-group ( group -- ) >r
    r@ cell+ $@ bounds ?DO  I @  cell +LOOP
    r> cell+ $@len 0 +DO  >o o to connection
    disconnect-me o>  cell +LOOP ;
: disconnect-all ( group -- ) >r
    r@ cell+ $@ bounds ?DO  I @  cell +LOOP
    r> cell+ $@len 0 +DO  >o o to connection send-leave
    disconnect-me o>  cell +LOOP ;

: leave-chat ( group -- )
    dup send-reconnect disconnect-group ;

: leave-chats ( -- )
    msg-groups ['] leave-chat #map ;

: split-load ( group -- )
    cell+ >r 0
    BEGIN  dup 1+ r@ $[]# u<  WHILE
	    dup r@ $[] 2@ .send-reconnect1
	    1+ dup r@ $[] @ >o o to connection disconnect-me o>
    REPEAT drop rdrop ;

scope{ /chat
: split ( -- )
    \U split                split load
    \G split: reduce distribution load by reconnecting
    msg-group$ $@ >group last# split-load ;
}scope

\ chat toplevel

: do-chat ( addr u -- ) msg-group$ $! chat-entry \ ['] cmd( >body on
    [: up@ wait-task ! ;] IS do-connect
    BEGIN  get-input-line
	2dup "/bye" str= >r 2dup "\\bye" str= r> or 0= WHILE
	    do-chat-cmd? 0= IF  avalanche-text  THEN
    REPEAT  2drop leave-chats ;

: avalanche-to ( addr u otr-flag o:context -- )
    avalanche( ." Send avalance to: " pubkey $@ key>nick type cr )
    o to connection +resend-msg
    net2o-code expect-msg msg IF msg-otr THEN
    last# $@ 2dup pubkey $@ key| str= IF  2drop  ELSE  group,  THEN
    $, nestsig end-with
    end-code ;

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
