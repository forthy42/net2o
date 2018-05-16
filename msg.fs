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

: avalanche-msg ( msg u1 o:connect -- )
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
Variable chain-mode
User replay-mode
User skip-sig?

Sema msglog-sema

: ?msg-context ( -- o )
    msging-context @ dup 0= IF
	drop
	msg-context @ 0= IF
	    net2o:new-msg msg-context !
	THEN
	net2o:new-msging dup msging-context !
    THEN ;

: >chatid ( group u -- id u )  defaultkey sec@ keyed-hash#128 ;

: msg-log@ ( last# -- addr u )
    [: cell+ $@ save-mem ;] msglog-sema c-section ;

: serialize-log ( addr u -- $addr )
    [: bounds ?DO
	    I $@ check-date 0= IF  net2o-base:$, net2o-base:nestsig
	    ELSE   2drop  THEN
      cell +LOOP ;]
    gen-cmd ;

Variable saved-msg$
64Variable saved-msg-ticks

: save-msgs ( last -- )
    msg( ." Save messages" cr )
    ?.net2o/chats  net2o:new-msging >o
    dup msg-log@ over >r  serialize-log enc-file $!buf
    r> free throw  dispose o>
    $@ >chatid .chats/ enc-filename $!
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
    net2o:new-msging >o 0 to parent do-cmd-loop dispose o> ;

: load-msg ( group u -- )  2dup >group
    >chatid .chats/ [: type ." .v2o" ;] $tmp
    2dup file-status nip no-file# = IF  2drop EXIT  THEN
    replay-mode on  skip-sig? on
    ['] decrypt@ catch
    ?dup-IF  DoError 2drop
	\ try read backup instead
	[: enc-filename $. '~' emit ;] $tmp ['] decrypt@ catch
	?dup-IF  DoError 2drop
	ELSE  msg-eval  THEN
    ELSE  msg-eval  THEN
    replay-mode off  skip-sig? off  enc-file $free ;

: >load-group ( group u -- )
    2dup msg-logs #@ d0= IF  2dup load-msg  THEN >group ;

event: :>save-msgs ( last# -- ) saved-msg$ +unique$ ;
event: :>save-all-msgs ( -- )
    save-all-msgs ;

: !save-all-msgs ( -- )  file-task 0= ?EXIT
    <event :>save-all-msgs file-task event| ;

: save-msgs& ( -- )
    file-task 0= IF  create-file-task  THEN
    <event last# elit, :>save-msgs file-task event> ;

: ?msg-log ( addr u -- )  msg-logs ?hash ;

0 Value log#

: +msg-log ( addr u -- addr' u' / 0 0 )
    last# $@ ?msg-log
    [: last# cell+ $ins[]date  dup to log#
      dup -1 = IF drop #0. ( 0 to last# )  ELSE  last# cell+ $[]@  THEN
    ;] msglog-sema c-section ;
: ?save-msg ( addr u -- )
    ?msg-log
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
    nest-cmd-loop msg:end ;

: >msg-log ( addr u -- addr' u )
    last# >r +msg-log last# ?dup-IF  $@ ?save-msg  THEN  r> to last# ;

Variable otr-log
: >otr-log ( addr u -- addr' u )
    [: otr-log $ins[]date
      dup -1 = IF  drop #0.  ELSE  otr-log $[]@  THEN
    ;] msglog-sema c-section ;

: do-msg-nestsig ( addr u -- )
    parent .msg-context @ .msg-display msg-notify ;

: display-lastn ( addr u n -- )  reset-time
    otr-mode @ >r otr-mode off
    [: net2o:new-msg >o 0 to parent
	cells >r ?msg-log last# msg-log@ 2dup { log u }
	dup r> - 0 max /string bounds ?DO
	    I log - cell/ to log#
	    I $@ ['] msg-display catch IF  ." invalid entry" cr 2drop  THEN
	cell +LOOP
	log free dispose o> throw ;] catch
    r> otr-mode ! throw ;

: display-one-msg ( addr u -- )
    net2o:new-msg >o 0 to parent
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

: reconnect-chat ( $chat -- )
    peer-buf $!buf  last# peer-buf $@
    reconnect( ." reconnect " 2dup 2dup + 1- c@ 1+ - .addr$ cr )
    reconnect( ." in group: " last# dup hex. $. cr )
    0 >o $A $A [: reconnect( ." prepare reconnection" cr )
      ?msg-context >o silent-last# ! o>
      ['] chat-rqd-nat ['] chat-rqd-nonat ind-addr @ select rqd! ;]
    addr-connect o> ;

event: :>avalanche ( addr u o group -- )
    avalanche( ." Avalanche to: " dup hex. cr )
    to last# .avalanche-msg ;
event: :>chat-reconnect ( $chat o group -- )
    to last# .reconnect-chat ;
event: :>msg-nestsig ( $addr o group -- )
    to last# >o { w^ m } m $@ do-msg-nestsig m $free o>
    ctrl L inskey ;

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

: push-msg ( addr u o:parent -- )
    up@ receiver-task <> IF
	avalanche-msg
    ELSE wait-task @ ?dup-IF
	    <event >r e$, o elit, last# elit,
	    :>avalanche r> event>
	ELSE  drop 2drop  THEN
    THEN ;
: show-msg ( addr u -- )
    parent dup IF  .wait-task @ dup up@ <> and  THEN
    ?dup-IF
	>r r@ <hide> <event $make elit, o elit, last# elit, :>msg-nestsig
	r> event>
    ELSE  do-msg-nestsig  THEN ;

: date>i ( date -- i )
    last# cell+ $search[]date last# cell+ $[]# 1- umin ;
: date>i' ( date -- i )
    last# cell+ $search[]date last# cell+ $[]# umin ;
: sighash? ( addr u -- flag )
    over le-64@ date>i
    dup 0< IF  drop 2drop  false  EXIT  THEN  >r
    over le-64@ 64#1 64+ date>i' >r [ 1 64s ]L /string
    r> r> U+DO
	c:0key I last# cell+ $[]@ sigonly@ >hash
	2dup hashtmp over str= IF  2drop true  UNLOOP   EXIT
	ELSE  2dup 85type ."  <> " hashtmp over 85type  THEN
    LOOP
    2drop false ;

\ message commands

scope{ net2o-base

\g 
\g ### message commands ###
\g 

reply-table $@ inherit-table msg-table

$20 net2o: msg-start ( $:pksig -- ) \g start message
    1 !!>order? $> msg:start ;
+net2o: msg-tag ( $:tag -- ) \g tagging (can be anywhere)
    $> msg:tag ;
+net2o: msg-id ( $:id -- ) \g a hash id
    2 !!>=order? $> msg:id ;
+net2o: msg-chain ( $:dates,sighash -- ) \g chained to message[s]
    $10 !!>=order? $> msg:chain ;
+net2o: msg-signal ( $:pubkey -- ) \g signal message to one person
    2 !!>=order? $> msg:signal ;
+net2o: msg-re ( $:hash ) \g relate to some object
    4 !!>=order? $> msg:re ;
+net2o: msg-text ( $:msg -- ) \g specify message string
    8 !!>=order? $> msg:text ;
+net2o: msg-object ( $:object type -- ) \g specify an object, e.g. an image
    8 !!>=order? 64>n $> rot msg:object ;
+net2o: msg-action ( $:msg -- ) \g specify action string
    8 !!>=order? $> msg:action ;
+net2o: msg-payment ( $:contract -- ) \g payment transaction
    8 !!>=order? $> msg:payment ;
+net2o: msg-otrify ( $:date+sig $:newdate+sig -- ) \g turn a past message into OTR
    $> $> msg:otrify ;
$2B net2o: msg-coord ( $:gps -- ) \g GPS coordinates
    8 !!>=order? $> msg:coord ;

}scope

msg-table $save

' context-table is gen-table

\ Code for displaying messages

Defer .log-num
Defer .log-date
Defer .log-end

: .otr-info ( -- )
    <info> ." [otr] " <default> "[otr] " notify+ notify-otr? on ;
: .otr-err ( -- )
    <err> ." [exp] " <default> 1 notify-otr? ! ;
: .otr ( tick -- )
    64dup 64#-1 64= IF  64drop  EXIT  THEN
    ticks 64- 64dup 64-0< IF  64drop .otr-err  EXIT  THEN
    otrsig-delta# 64< IF  .otr-info  THEN ;

scope: logstyles
: +num [: '#' emit log# u. ;] is .log-num ;
: -num ['] noop is .log-num ;
: +date [: .ticks space ;] is .log-date ;
: -date ['] 64drop is .log-date ;
: +end [: 64dup .ticks space .otr ;] is .log-end ;
: -end ['] .otr is .log-end ;

+date -num -end
}scope

:noname ( addr u -- )
    last# >r  2dup key| to msg:id$
    .log-num
    2dup startdate@ .log-date
    2dup enddate@ .log-end
    2dup .key-id
    ['] .simple-id $tmp notify-nick!
    r> to last# ; msg-class to msg:start
:noname ( addr u -- ) $utf8>
    space <warn> '#' forth:emit forth:type <default> ; msg-class to msg:tag
:noname ( addr u -- ) last# >r
    key| 2dup pk@ key| str=
    IF   <err>  THEN  2dup [: ."  @" .simple-id ;] $tmp notify+
    ."  @" .key-id <default>
    r> to last# ; msg-class to msg:signal
:noname ( addr u -- )
    last# >r last# $@ ?msg-log
    2dup sighash? IF  <info>  ELSE  <err>  THEN
    ."  <" over le-64@ .ticks
    verbose( dup keysize - /string ." ," 85type )else( 2drop ) <default>
    r> to last# ; msg-class to msg:chain
:noname ( addr u -- )
    space <warn> ." [" 85type ." ]->" <default> ; msg-class to msg:re
:noname ( addr u -- )
    space <warn> ." [" 85type ." ]:" <default> ; msg-class to msg:id
:noname ( addr u -- ) $utf8>
    [: ." : " 2dup forth:type ;] $tmp notify+
    ." : " forth:type ; msg-class to msg:text
:noname ( addr u type -- )
    space <warn> 0 .r ." :[" 85type ." ]" <default> ;
msg-class to msg:object
:noname ( addr u -- ) $utf8>
    [: space 2dup forth:type ;] $tmp notify+
    space <warn> forth:type <default> ; msg-class to msg:action
:noname ( addr u -- )
    <warn> ."  GPS: " .coords <default> ; msg-class to msg:coord
: replace-sig { addrsig usig addrmsg umsg -- }
    \ !!dummy!! need to verify signature!
    addrsig usig addrmsg umsg usig - [: type type ;] $tmp
    2dup pk-sig? !!sig!! 2drop addrmsg umsg smove ;
: new-otrsig ( addr u -- addrsig usig )
    2dup startdate@ old>otr
    c:0key sigpksize# - c:hash ['] .sig $tmp 1 64s /string ;

:noname { sig u' addr u -- }
    u' 64'+ u =  u sigsize# = and IF
	last# >r last# $@ ?msg-log
	addr u startdate@ 64dup date>i >r 64#1 64+ date>i' r>
	U+DO
	    I last# cell+ $[]@
	    2dup dup sigpksize# - /string key| msg:id$ str= IF
		dup u - /string addr u str= IF
		    ." OTRify #" I u.
		    sig u' I last# cell+ $[]@ replace-sig
		    \ !!Schedule message saving!!
		THEN
	    ELSE
		2drop
	    THEN
	LOOP
	r> to last#
    THEN ; msg-class to msg:otrify

:noname ( -- )
    forth:cr ; msg-class to msg:end

\g
\g ### group description commands ###
\g

hash: group#

static-a to allocater
align here
group-class new Constant group-o
dynamic-a to allocater
here over - 2Constant sample-group$

: last>o ( -- )
    \G use last hash access as object
    last# cell+ $@ drop cell+ >o rdrop ;

: make-group ( addr u -- o:group )
    sample-group$ 2over group# #! last>o to groups:id$ ;

cmd-table $@ inherit-table group-table

scope{ net2o-base

$20 net2o: group-name ( $:name -- ) \g group symbolic name
    $> make-group ;
+net2o: group-id ( $:group -- ) \g group id, is a pubkey
    group-o o = !!no-group-name!! $> to groups:id$ ;
+net2o: group-member ( $:memberkey -- ) \g add member key
    group-o o = !!no-group-name!! $> groups:member[] $+[]! ;
+net2o: group-admin ( $:adminkey -- ) \g set admin key
    group-o o = !!no-group-name!! $> groups:admin sec! ;
+net2o: group-perms ( 64u -- ) \g permission/modes bitmask
    group-o o = !!no-group-name!! to groups:perms# ;

}scope

group-table $save

group-table @ group-o .token-table !

' context-table is gen-table

: .chats/group ( -- addr u )
    pkc keysize 3 * \ hash of pkc+pk1+skc keyed with "group"
    "group" keyed-hash#128 .chats/ ( [: type ." .v2o" ;] $tmp ) ;

: read-chatgroups ( -- )
    .chats/group [: type ." .v2o" ;] $tmp
    2dup file-status nip no-file# = IF  2drop  EXIT  THEN
    decrypt@ group-o .do-cmd-loop  enc-file $free ;

also net2o-base

: serialize-chatgroup ( last# -- )
    dup $@ 2dup $, group-name
    rot cell+ $@ drop cell+ >o
    groups:id$ dup IF
	2tuck str= 0= IF  $, group-id  ELSE  2drop  THEN
    ELSE  2drop 2drop  THEN
    groups:member[] [: $, group-member ;] $[]map
    groups:admin sec@ dup IF  sec$, group-admin  ELSE  2drop  THEN
    groups:perms# 64dup 64-0<> IF  lit, group-perms  ELSE  64drop  THEN
    o> ;

previous

: save-chatgroups ( -- )
    .chats/group enc-filename $!
    [: group# ['] serialize-chatgroup #map ;] gen-cmd enc-file $!buf
    pk-off  key-list encfile-rest ;

Variable group-list[]
: $ins[]group ( o:group $array -- pos )
    \G insert O(log(n)) into pre-sorted array
    \G @var{pos} is the insertion offset or -1 if not inserted
    { a[] } 0 a[] $[]#
    BEGIN  2dup u<  WHILE  2dup + 2/ { left right $# }
	    o $@ $# a[] $[] @ $@ compare dup 0= IF
		drop o cell+ $@ drop cell+ .groups:id$
		$# a[] $[] @ cell+ $@ drop cell+ .groups:id$ compare  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    o { w^ ins$0 } ins$0 cell a[] r@ cells $ins r> ;
: groups>sort[] ( -- )  group-list[] $free
    group# [: >o group-list[] $ins[]group o> drop ;] #map ;

: .chatgroup ( last# -- )
    dup $. space dup $@ rot cell+ $@ drop cell+ >o
    groups:id$ 2tuck str=
    IF  ." ="  ELSE  ''' emit <info> 85type <default> ''' emit THEN space
    groups:member[] [: '@' emit .simple-id space ;] $[]map
\    ." admin " groups:admin[] [: '@' emit .simple-id space ;] $[]map
    ." +" groups:perms# x64.
    o> cr ;
: .chatgroups ( -- )
    groups>sort[]
    group-list[] $@ bounds ?DO  I @ .chatgroup  cell +LOOP ;

\g 
\g ### messaging commands ###
\g 

scope{ net2o-base

$34 net2o: msg ( -- o:msg ) \g push a message object
    perm-mask @ perm%msg and 0= !!msg-perm!!
    ?msg-context n:>o c-state off  0 to last# ;

msging-table >table

reply-table $@ inherit-table msging-table

$21 net2o: msg-group ( $:group -- ) \g set group
    $> >group ;
+net2o: msg-join ( $:group -- ) \g join a chat group
    replay-mode @ IF  $> 2drop  EXIT  THEN
    $> >load-group parent >o
    +unique-con +chat-control
    wait-task @ ?dup-IF  <hide>  THEN
    o> ;
+net2o: msg-leave ( $:group -- ) \g leave a chat group
    $> msg-groups #@ d0<> IF
	parent last# cell+ del$cell  THEN ;
+net2o: msg-reconnect ( $:pubkey+addr -- ) \g rewire distribution tree
    $> $make
    <event elit, o elit, last# elit, :>chat-reconnect
    parent .wait-task @ ?query-task over select event> ;
+net2o: msg-last? ( start end n -- ) 64>n msg:last? ;
+net2o: msg-last ( $:[tick0,msgs,..tickn] n -- ) 64>n msg:last ;

: ?pkgroup ( addr u -- addr u )
    \ if no group has been selected, use the pubkey as group
    last# 0= IF  2dup + sigpksize# - keysize >group  THEN ;

net2o' nestsig net2o: msg-nestsig ( $:cmd+sig -- ) \g check sig+nest
    $> nest-sig ?dup-0=-IF
	?pkgroup >msg-log
	2dup d0<> \ do something if it is new
	IF  replay-mode @ 0= IF
		2dup show-msg
		2dup parent .push-msg
	    THEN
	THEN  2drop
    ELSE  replay-mode @ IF  drop 2drop
	ELSE  !!sig!!  THEN \ balk on all wrong signatures
    THEN ;

:noname skip-sig? @ IF   quicksig( pk-quick-sig? )else( pk-date? )
    ELSE  pk-sig?  THEN ;  ' msg  2dup
msging-class to start-req
msging-class to nest-sig
msg-class to start-req
msg-class to nest-sig

' context-table is gen-table

also }scope

msging-table $save

: msg-reply ( tag -- )
    ." got reply " hex. pubkey $@ key>nick forth:type forth:cr ;
: expect-msg ( --- )
    reply( ['] msg-reply )else( ['] drop ) expect-reply-xt +chat-control ;

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
: i.date+1 ( i -- )
    last# cell+ $[]@ startdate@ 64#0 { 64^ x }
    64#1 64+ x le-64! x 1 64s forth:type ;
: last-msgs@ ( startdate enddate n -- addr u n' )
    \G print n intervals for messages from startdate to enddate
    \G The intervals contain the same size of messages except the
    \G last one, which may contain less (rounding down).
    \G Each interval contains a 64 bit hash of the last 64 bit of
    \G each message within the interval
    last# >r >r last# $@ ?msg-log
    last# cell+ $[]#
    IF
	date>i' >r date>i' r> swap
	2dup - r> over >r 1- 1 max / 0 max 1+ -rot
	[: over >r U+DO  I i.date
	      dup I + I' umin I l.hashs forth:type
	  dup +LOOP
	  r> dup last# cell+ $[]# u< IF  i.date
	  ELSE  1- i.date+1  THEN
	  drop ;] $tmp r> \ over 1 64s u> -
    ELSE  rdrop 64drop 64drop s" "  0 THEN   r> to last# ;

\ sync chatlog through virtual file access

termserver-class class
end-class msgfs-class

file-classes# Constant msgfs-class#
msgfs-class +file-classes

: save-to-msg ( addr u n -- )
    state-addr >o  msgfs-class# fs-class! w/o fs-create o> ;
: .chat-file ( addr u -- )
    over le-64@ .ticks 1 64s /string  ." ->"
    over le-64@ .ticks 1 64s /string  ." @" forth:type ;
in net2o : copy-msg ( filename u -- )
    ." copy msg: " 2dup .chat-file forth:cr
    [: msgfs-class# ulit, file-type 2dup $, r/o ulit, open-sized-file
      file-reg# @ save-to-msg ;] n2o>file
    1 file-count +! ;

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
	    I le-64@ date>i'
	    I 64'+ 64'+ le-64@ date>i' swap
	    l.hashs drop le-64@
	    I 64'+ le-64@ 64<> IF
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
	parent >o  expect+slurp
	cmdbuf# @ 0= IF  $10 blocksize! $1 blockalign!  THEN
	ask-msg-files[] ['] net2o:copy-msg $[]map o>
    ELSE
	." === nothing to sync ===" forth:cr
    THEN
    r> to last# ;

:noname ( -- 64len )
    \ poll serializes the 
    fs-outbuf $off
    fs-path $@ 2 64s /string ?msg-log
    last# msg-log@ over >r
    fs-path $@ drop le-64@ date>i \ start index
    fs-path $@ drop 64'+ le-64@ 64#1 64+ date>i' \ end index
    over - >r
    cells safe/string r> cells umin
    req? @ >r req? off  serialize-log   r> req? !  fs-outbuf $!buf
    r> free throw
    fs-outbuf $@len u>64 ; msgfs-class is fs-poll
:noname ( addr u mode -- )
    \G addr u is starttick endtick name concatenated together
    fs-close drop fs-path $!  fs-poll fs-size!
    ['] noop is file-xt
; msgfs-class is fs-open

\ syncing done
: chat-sync-done ( group-addr u -- )
    msg( ." chat-sync-done" forth:cr )
    net2o-code expect-msg close-all net2o:gen-reset end-code
    net2o:close-all
    ?msg-log last# $@ rows  display-lastn
    !save-all-msgs
    ." === sync done ===" forth:cr
    ['] noop is sync-done-xt ;
event: :>msg-eval ( parent $pack $addr -- )
    { w^ buf w^ group }
    group $@ 2 64s /string ?msg-log
    group $@ 2 64s /string msg-logs #@ nip cell/ u.
    buf $@ true replay-mode ['] msg-eval !wrapper
    buf $free group $@ 2 64s /string ?save-msg
    group $@ .chat-file ."  saved "
    group $@ 2 64s /string msg-logs #@ nip cell/ u. forth:cr
    >o -1 file-count +!@ 1 =
    IF  group $@ 2 64s /string chat-sync-done  THEN  group $free
    o> ;
: msg-file-done ( -- )
    fs-path $@len IF
	msg( ." msg file done: " fs-path $@ .chat-file forth:cr )
	['] fs-flush file-sema c-section
    THEN ;
:noname ( addr u mode -- )
    fs-close drop fs-path $!
    ['] msg-file-done is file-xt
; msgfs-class is fs-create
:noname ( addr u -- u )
    [ termserver-class :: fs-read ]
; msgfs-class is fs-read
:noname ( -- )
	<event parent elit, 0 fs-inbuf !@ elit,  0 fs-path !@ elit, :>msg-eval
	parent .wait-task @ event>
	fs:fs-clear
; msgfs-class is fs-flush    
:noname ( -- )
    fs-path @ 0= ?EXIT
    fs-inbuf $@len IF
	msg( ." Closing file " fs-path $@ .chat-file forth:cr )
	fs-flush
    THEN
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
    otr-mode @ IF  now>otr  ELSE  now>never  THEN ]pksign ;
: msg-otr> ( -- )
    \G end a msg block by adding a short-time signature
    now>otr ]pksign ;
: msg-log, ( -- addr u )
    last-signed 2@ >msg-log ;
: otr-log, ( -- addr u )
    last-signed 2@ >otr-log ;

previous

: ?destpk ( addr u -- addr' u' )
    2dup pubkey $@ key| str= IF  2drop pk@ key|  THEN ;

: last-signdate@ ( -- 64date )
    msg-group$ $@ msg-logs #@ dup IF
	+ cell- $@ startdate@ 64#1 64+
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
    64#0 lit, date lit, ask-last# ulit, msg-last?
    date 64#-1 64<> IF
	date lit, 64#-1 lit, 1 ulit, msg-last?
    THEN ;

: sync-ahead?, ( -- )
    last-signdate@ 64#1 64+ lit, 64#-1 lit, ask-last# ulit, msg-last? ;

: join, ( -- )
    [: msg-join sync-ahead?,
      sign[ msg-start "joined" $, msg-action msg-otr> ;] [msg,] ;

: silent-join, ( -- )
    last# $@ dup IF  msg $, msg-join  end-with
    ELSE  2drop  THEN ;

: leave, ( -- )
    [: msg-leave
      sign[ msg-start "left" $, msg-action msg-otr> ;] [msg,] ;

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
    [: last# >r o IF  2dup do-msg-nestsig
      ELSE  2dup display-one-msg  THEN  r> to last#
      0 .avalanche-msg ;] [group] drop notify- ;

\ chat message, text only

msg-class class
end-class textmsg-class

' 2drop textmsg-class to msg:start
:noname space '#' emit type ; textmsg-class to msg:tag
:noname '@' emit .simple-id space ; textmsg-class to msg:signal
' 2drop textmsg-class to msg:re
' 2drop textmsg-class to msg:chain
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
    last# cell+ $[]# 0= IF  maxlen 0 addr over  EXIT  THEN
    line-date 64@ date>i'
    BEGIN  1- dup 0>= WHILE  dup last# cell+ $[]@
	dup sigpksize# - /string key| pk@ key| str=  UNTIL  THEN
    last# cell+ $[]@ !date ['] msg-display textmsg-o .$tmp 
    tuck addr maxlen smove
    maxlen swap addr over ;
: find-next-chatline { maxlen addr -- max span addr span }
    msg-group$ $@ ?msg-log
    line-date 64@ date>i
    BEGIN  1+ dup last# cell+ $[]# u< WHILE  dup last# cell+ $[]@
	dup sigpksize# - /string key| pk@ key| str=  UNTIL  THEN
    dup last# cell+ $[]# u>=
    IF    drop $lastline $@  64#-1 line-date 64!
    ELSE  last# cell+ $[]@ !date ['] msg-display textmsg-o .$tmp  THEN
    tuck addr maxlen smove
    maxlen swap addr over ;

: chat-prev-line  ( max span addr pos1 -- max span addr pos2 false )
    line-date 64@ 64#-1 64= IF
	>r 2dup swap $lastline $! r>  THEN
    clear-line find-prev-chatline
    edit-update false ;
: chat-next-line  ( max span addr pos1 -- max span addr pos2 false )
    clear-line find-next-chatline
    edit-update false ;
: chat-enter ( max span addr pos1 -- max span addr pos2 true )
    drop over edit-update true 64#-1 line-date 64! ;

edit-terminal-c class
end-class chat-terminal-c
chat-terminal-c ' new static-a with-allocater Constant chat-terminal

bl cells buffer: chat-ctrlkeys
xchar-ctrlkeys chat-ctrlkeys bl cells move

chat-terminal edit-out !

' chat-ctrlkeys is ctrlkeys

' chat-next-line ctrl N bindkey
' chat-prev-line ctrl P bindkey
' chat-enter     #lf    bindkey
' chat-enter     #cr    bindkey
\ :noname #tab (xins) 0 ; #tab   bindkey
[IFDEF] ebindkey
    keycode-limit keycode-start - cells buffer: chat-ekeys
    std-ekeys chat-ekeys keycode-limit keycode-start - cells move
    
    ' chat-ekeys is ekeys
    
    ' chat-next-line k-down ebindkey
    ' chat-prev-line k-up   ebindkey
[THEN]

edit-terminal edit-out !

: chat-history ( -- )
    chat-terminal edit-out ! ;

\ chat line editor

$200 Constant maxmsg#

: xclear ( addr u -- ) x-width 1+ x-erase ;

: get-input-line ( -- addr u )
    BEGIN  pad maxmsg# ['] accept catch
	dup dup -56 = swap -28 = or \ quit or ^c to leave
	IF    drop 2drop "/bye"
	ELSE
	    dup 0= IF
		drop pad swap 2dup xclear
	    ELSE
		DoError drop 0  THEN
	THEN
	dup 0= WHILE  2drop  REPEAT ;

\ joining and leaving

: g?join ( -- )
    msg-group$ $@len IF  send-join -timeout  THEN ;

: g?leave ( -- )
    msg-group$ $@len IF  send-leave -timeout  THEN ;

: greet ( -- )
    net2o-code expect-msg
    log !time end-with join, get-ip end-code ;

: chat-entry ( -- )  ?.net2o/chats  word-args
    <warn> ." Type ctrl-D or '/bye' as single item to quit" <default> cr ;

: wait-2s-key ( -- )
    ntime 50 0 DO  key? ?LEAVE
    2dup i #40000000 um* d+ deadline  LOOP  2drop ;
: .nobody ( -- )
    <info>
    [: ." nobody's online" otr-mode @ 0= IF ." , saving away"  THEN ;] $tmp
    2dup type <default>
    wait-2s-key xclear ;

also net2o-base
\ chain messages to one previous message
: ?chain, ( -- )  chain-mode @ 0= ?EXIT
    last# >r last# $@ ?msg-log
    last# cell+ $[]# 1- dup 0< IF  drop
    ELSE  last# cell+ $[]@
	[: 2dup startdate@ 64#0 { 64^ sd } sd le-64!  sd 1 64s forth:type
	  c:0key sigonly@ >hash hashtmp hash#128 forth:type ;] $tmp $, msg-chain
    THEN  r> to last# ;

: (send-avalanche) ( xt -- addr u flag )
    [: 0 >o [: sign[ msg-start execute ?chain, msg> ;] gen-cmd$ o>
      +last-signed msg-log, ;] [group] ;
previous
: send-avalanche ( xt -- )      (send-avalanche)
    >r .chat r> 0= IF  .nobody  THEN ;

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
    config:notify-text# @
    case
	-1 of  ." visible"  endof
	0 of  ." hidden"  endof
	1 of  ." hide-otr"  endof
    endcase
    forth:cr ;

: get-hex ( -- n )
    parse-name '$' skip #0. 2swap ['] >number $10 base-execute 2swap drop ;
: get-dec ( -- n )
    parse-name '#' skip #0. 2swap ['] >number #10 base-execute 2swap drop ;

scope: notify-cmds

: on ( -- ) -2 config:notify?# ! ;
: always ( -- ) -3 config:notify?# ! ;
: off ( -- ) 0 config:notify?# ! ;
: led ( -- ) \ "<rrggbb> <on-ms> <off-ms>"
    get-hex config:notify-rgb# !
    get-dec #500 max config:notify-on# !
    get-dec #500 max config:notify-off# !
    msg-builder ;
: interval ( -- ) parse-name
    #0. 2swap ['] >number #10 base-execute 1 = IF  nip c@ case
	    's' of     #1000 * endof
	    'm' of    #60000 * endof
	    'h' of #36000000 * endof
	endcase
    ELSE  2drop  THEN  #1000000 um* config:delta-notify& 2! ;
: mode ( -- )
    get-dec 3 and config:notify-mode# ! msg-builder ;
: visible ( -- )
    config:notify-text# forth:on ;
: hidden ( -- )
    config:notify-text# forth:off ;
: hide-otr ( -- )
    1 config:notify-text# ! ;

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

forward avalanche-text

also net2o-base scope: /chat

: /me ( addr u -- )
    \U me <action>          send string as action
    \G me: send remaining string as action
    [: $, msg-action ;] send-avalanche ;

: /otr ( addr u -- )
    \U otr on|off|message   turn otr mode on/off (or one-shot)
    2dup s" on" str= >r
    2dup s" off" str= r@ or IF   2drop r> otr-mode !
	<info> ." === " otr-mode @ IF  ." enter"  ELSE  ." leave"  THEN
	."  otr mode ===" <default> forth:cr
    ELSE  rdrop
	true otr-mode !@ >r  avalanche-text  r> otr-mode !
    THEN ;

: /chain ( addr u -- )
    \U chain on|off         turn chain mode on/off
    2dup s" on" str= >r
    s" off" str= r@ or IF   r> chain-mode !
	<info> ." === " chain-mode @ IF  ." enter"  ELSE  ." leave"  THEN
	."  chain mode ==="
    ELSE  <err> ." only 'chain on|off' are allowed" rdrop  THEN
    <default> forth:cr ;

: /peers ( addr u -- ) 2drop
    \U peers                list peers
    \G peers: list peers in all groups
    msg-groups [: dup .group ." : "
      cell+ $@ bounds ?DO
	  space I @ >o .con-id space
	  ack@ .rtdelay 64@ 64>f 1n f* (.time) o>
      cell +LOOP  forth:cr ;] #map ;

: /gps ( addr u -- ) 2drop
    \U gps                  send coordinates
    \G gps: send your coordinates
    coord! coord@ 2dup 0 -skip nip 0= IF  2drop
    ELSE
	[: $, msg-coord ;] send-avalanche
    THEN ;

' /gps alias /here

: /help ( addr u -- )
    \U help                 show help
    \G help: list help
    bl skip '/' skip
    2dup [: ."     \U " forth:type ;] $tmp ['] .chathelp search-help
    [: ."     \G " forth:type ':' forth:emit ;] $tmp ['] .cmd search-help ;

: /invitations ( addr u -- )
    \U invitations          handle invitations
    \G invitations: handle invitations: accept, ignore or block invitations
    2drop .invitations ;

: /chats ( addr u -- ) 2drop ." ===== chats: "
    \U chats                list chats
    \G chats: list all chats
    msg-groups [: >r
      r@ $@ msg-group$ $@ str= IF ." *" THEN
      r@ .group
      ." [" r@ cell+ $@len cell/ 0 .r ." ]#"
      r@ $@ msg-logs #@ nip cell/ u. rdrop ;] #map
    ." =====" forth:cr ;

: /nat ( addr u -- )  2drop
    \U nat                  list NAT info
    \G nat: list nat traversal information of all peers in all groups
    \U renat                redo NAT traversal
    \G renat: redo nat traversal
    msg-groups [: dup ." ===== Group: " .group ."  =====" forth:cr
      cell+ $@ bounds ?DO
	  ." --- " I @ >o .con-id ." : " return-address .addr-path
	  ."  ---" forth:cr .nat-addrs o>
      cell +LOOP ;] #map ;

: /myaddrs ( addr u -- )
    \U myaddrs              list my addresses
    \G myaddrs: list my own local addresses (debugging)
    2drop
    ." ===== all =====" forth:cr    .my-addr$s
    ." ===== public =====" forth:cr .pub-addr$s
    ." ===== private =====" forth:cr .priv-addr$s ;
: /!myaddrs ( addr u -- )
    \U !myaddrs             re-obtain my addresses
    \G !myaddrs: if automatic detection of address changes fail,
    \G !myaddrs: you can use this command to re-obtain your local addresses
    2drop !my-addr ;

: /notify ( addr u -- )
    \U notify always|on|off|led <rgb> <on-ms> <off-ms>|interval <time>[smh]|mode 0-3
    \G notify: Change notificaton settings
    get-order n>r ['] notify-cmds >body 1 set-order
    ['] evaluate catch nr> set-order throw .notify ;

: /beacons ( addr u -- )
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

: /sync ( addr u -- )
    \U sync [+date] [-date] synchronize logs
    \G sync: synchronize chat logs, starting and/or ending at specific
    \G sync: time/date
    2drop o 0= IF msg-group$ $@ msg-groups #@
	IF @ >o rdrop ?msg-context ELSE EXIT THEN
    THEN o to connection
    ." === sync ===" forth:cr
    net2o-code expect-msg ['] last?, [msg,] end-code ;

: /version ( addr u -- )
    \U version              version string
    \G version: print version string
    2drop .n2o-version space .gforth-version forth:cr ;

: /log ( addr u -- )
    \U log [#lines]         show log
    \G log: show the log, default is a screenful
    s>unumber? IF  drop >r  ELSE  2drop rows >r  THEN
    msg-group$ $@ ?msg-log last# $@ r>  display-lastn ;

: /logstyle ( addr u -- )
    \U logstyle [+-style]   set log style
    \G logstyle: set log styles, the following settings exist:
    \G logstyle: +date      a date per log line
    \G logstyle: +num       a message number per log line
    get-order n>r ['] logstyles >body 1 set-order
    ['] evaluate catch nr> set-order throw ;

: /otrify ( -- )
    \U otrify #line         otrify message
    \G otrify: turn an older message of yours into an OTR message
    s>unumber? IF  drop >r  ELSE  2drop  EXIT  THEN
    msg-group$ $@ ?msg-log last# cell+ $@ r> cells safe/string
    IF  $@ 2dup + sigpksize# - sigpksize#
	over keysize pkc over str= IF
	    keysize /string 2swap new-otrsig 2swap
	    true otr-mode [:
		[: $, $, msg-otrify ;] (send-avalanche) drop ;] !wrapper
	    .chat
	ELSE
	    2drop 2drop ." not your message!" forth:cr
	THEN
    THEN ;
}scope

: ?slash ( addr u -- addr u flag )
    over c@ dup '/' = swap '\' = or ;

: do-chat-cmd? ( addr u -- t / addr u f )
    ?slash dup 0= ?EXIT  drop
    over '/' swap c! bl $split 2swap
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

: avalanche-text ( addr u -- ) >utf8$
    [: signal-list, $, msg-text ;] send-avalanche ;

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
    packets2 @  connected-timeout  packets2 @ <>
    IF  reply( ." Resend to " pubkey $@ key>nick type cr )
	timeout-expired? IF
	    timeout( <err> ." Excessive timeouts from "
	    pubkey $@ key>nick type ." : "
	    ack@ .timeouts @ . <default> cr )
	    msg-group$ $@len IF
		true otr-mode
		[: pubkey $@ ['] left, send-avalanche ;] !wrapper
	    THEN
	    net2o:dispose-context
	    EXIT
	THEN
    ELSE  expected@ u<= IF  -timeout  THEN  THEN ;

: +resend-msg  ['] msg-timeout is timeout-xt o+timeout ;

$B $E 2Value chat-bufs#

: +chat-control ( -- )
    +resend-msg +flow-control ;

: chat#-connect ( addr u buf1 buf2 --- )
    pk-connect connection >o rdrop +chat-control  greet +group ;

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

: ?wait-chat ( -- ) #0. /chat:/chats
    BEGIN  chats# 0= WHILE  wait-chat chat-connects  REPEAT
    msg-group$ $@ ; \ stub

scope{ /chat
: /chat ( addr u -- )
    \U chat [group][@user]  switch/connect chat
    \G chat: switch to chat with user or group
    chat-keys $[]off nick>chat 0 chat-keys $[]@ key>group
    msg-group$ $@ msg-groups #@ dup 0= IF  2drop
	nip IF  chat-connects
	ELSE  ." That chat isn't active" forth:cr  THEN
    ELSE
	bounds ?DO  2dup I @ .pubkey $@ key2| str= 0= WHILE  cell +LOOP
	    2drop chat-connects  ELSE  UNLOOP 2drop THEN
    THEN  #0. /chats ;
}scope

also net2o-base
: punch-addr-ind@ ( -- o )
    punch-addrs $[]# 0 U+DO
	I punch-addrs $[] @ .host:route $@len IF
	    I punch-addrs $[] @ unloop  EXIT
	THEN
    LOOP  0 punch-addrs $[] @ ;
: reconnect, ( o:connection -- )
    [: punch-addr-ind@ o>addr forth:type
      pubkey $@ key| tuck forth:type forth:emit ;] $tmp
    reconnect( ." send reconnect: " 2dup 2dup + 1- c@ 1+ - .addr$ forth:cr )
    $, msg-reconnect ;

: reconnects, ( group -- )
    cell+ $@ cell safe/string bounds U+DO
	I @ .reconnect,
    cell +LOOP ;

: send-reconnects ( group o:connection -- )  o to connection
    net2o-code expect-msg
    [: dup  $@ ?destpk 2dup >group $, msg-leave  reconnects,
      sign[ msg-start "left" $, msg-action msg> ;] [msg,]
    end-code| ;

: send-reconnect1 ( o o:connection -- ) o to connection
    net2o-code expect-msg
    [: last# $@ $, msg-group .reconnect, ;] [msg,]
    end-code| ;
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
	disconnect-me o>
    cell +LOOP ;
: disconnect-all ( group -- ) >r
    r@ cell+ $@ bounds ?DO  I @  cell +LOOP
    r> cell+ $@len 0 +DO  >o o to connection
	send-leave  disconnect-me o>
    cell +LOOP ;

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
: /split ( addr u -- )  2drop
    \U split                split load
    \G split: reduce distribution load by reconnecting
    msg-group$ $@ >group last# split-load ;
}scope

\ chat toplevel

: do-chat ( addr u -- )
    get-order n>r
    chat-history  ['] /chat >body 1 set-order
    msg-group$ $! chat-entry \ ['] cmd( >body on
    [: up@ wait-task ! ;] IS do-connect
    BEGIN  get-input-line
	2dup "/bye" str= >r 2dup "\\bye" str= r> or 0= WHILE
	    do-chat-cmd? 0= IF  avalanche-text  THEN
    REPEAT  2drop leave-chats  xchar-history
    nr> set-order ;

: avalanche-to ( addr u o:context -- )
    avalanche( ." Send avalanche to: " pubkey $@ key>nick type space over hex. cr )
    o to connection
    net2o-code expect-msg msg
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
    )
End:
[THEN]
