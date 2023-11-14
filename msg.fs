\ messages                                           06aug2014py

\ Copyright © 2014-2023   Bernd Paysan

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

Forward avalanche-to ( o:context -- )
Forward pk1-connect? ( key u cmdlen datalen -- flag )
Forward pk-connect-dests?
Forward addr-connect ( key+addr u cmdlen datalen xt -- )
Forward pk-peek? ( addr u0 -- flag )

: ?hash ( addr u hash -- ) >r
    2dup r@ #@ d0= IF  "" 2swap r> #!  ELSE  2drop rdrop  THEN ;

Variable otr-mode \ global otr mode

: >group ( addr u -- )
    2dup msg-group# #@ d0= IF
	net2o:new-msg >o 2dup to msg:name$
	otr-mode @ IF  msg:+otr  THEN
	o o>
	cell- [ msg:class >osize @ cell+ ]L
	2over msg-group# #!
    THEN  last# cell+ $@ drop cell+ to msg-group-o
    2drop ;

User ihave$
User push[]
$300 Constant max#have

: (avalanche-msg) ( o:connect -- )
    msg-group-o .msg:peers[] $@
    bounds ?DO  I @ o <> IF  I @ .avalanche-to  THEN
    cell +LOOP ;
: cleanup-msg ( -- )
    push[] $[]free ;
: avalanche-msg ( o:connect -- )
    \G forward message to all next nodes of that message group
    (avalanche-msg) cleanup-msg ;

Variable msg-group$
User replay-mode
User skip-sig?

Sema msglog-sema

: ?msg-context ( -- o )
    msging-context @ dup 0= IF
	drop
	net2o:new-msging dup msging-context !
    THEN ;

: >chatid ( group u -- id u )  defaultkey sec@ keyed-hash#128 ;

: msg-log@ ( -- addr u )
    [: msg-group-o .msg:log[] $@ save-mem ;] msglog-sema c-section ;

: purge-log ( -- )
    [: msg-group-o .msg:log[] { a[] }
	0  BEGIN  dup a[] $[]# u<  WHILE
		dup a[] $[]@ check-date nip nip
		over a[] $[]@ "\x60\x03\x00\x61" string-prefix? or  IF
		    dup a[] $[] $free
		    a[] over cells cell $del
		ELSE
		    1+
		THEN
	REPEAT  drop ;] msglog-sema c-section ;

forward msg-scan-hash
forward msg-add-hashs

: serialize-log ( addr u -- $addr )
    [: bounds ?DO
	    I $@ check-date 0= IF
		net2o-base:$, net2o-base:nestsig
	    ELSE   msg( ." removed entry " dump )else( 2drop )  THEN
	cell +LOOP  ;] gen-cmd ;

: scan-log-hashs ( -- )
    msg-log@ over >r
    [: bounds ?DO
	    I $@ msg:display
	cell +LOOP
	msg-add-hashs
    ;] msg-scan-hash r> free throw ;

Variable saved-msg$
64Variable saved-msg-ticks

: save-msgs ( group-o -- ) to msg-group-o
    msg( ." Save messages in group " msg-group-o dup h. .msg:name$ type cr )
    ?.net2o/chats  net2o:new-msging >o
    msg-log@ over >r  serialize-log enc-file $!buf
    r> free throw  dispose o>
    msg-group-o .msg:name$ >chatid .chats/ enc-filename $!
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
    >group msg-group-o .msg:log[] $@len 0=
    IF  msg-group-o [{: xo :}h1 xo .msg:name$ load-msg ;]
	parent .wait-task @
	dup 0= IF  drop ?file-task  THEN  send-event  THEN ;

: !save-all-msgs ( -- )
    syncfile( save-all-msgs )else(
    ['] save-all-msgs ?file-task send-event ?file-task event-block ) ;
: save-msgs& ( -- )
    syncfile( msg-group-o saved-msg$ +unique$ )else(
    msg-group-o [{: group-o :}h1 group-o saved-msg$ +unique$ ;]
      ?file-task send-event ) ;

0 Value log#
2Variable last-msg

: +msg-log ( addr u -- addr' u' / 0 0 )
    [: msg-group-o .msg:log[] $ins[]date  dup  dup 0< xor to log#
	log# msg-group-o .msg:log[] $[]@ last-msg 2!
	0< IF  #0.  ELSE  last-msg 2@  THEN
    ;] msglog-sema c-section ;
: ?save-msg ( -- )
    msg( ." saving messages in group " msg-group-o dup h. .msg:name$ type cr )
    msg-group-o .msg:?otr replay-mode @ or 0= IF  save-msgs&  THEN ;

Sema queue-sema

\ peer queue, in msg context

: peer> ( -- addr / 0 )
    [: msg:peers[] back> ;] queue-sema c-section ;
: >peer ( addr u -- )
    [: msg:peers[] $+[]! ;] queue-sema c-section ;

\ events

msg:class class end-class msg-notify-class

msg-notify-class ' new static-a with-allocater Constant msg-notify-o

: >msg-log ( addr u -- addr' u )
    +msg-log ?save-msg ;

: do-msg-nestsig ( addr u -- )
    2dup msg-group-o .msg:display
    msg-notify-o .msg:display ;

: display-lastn ( n -- )
    msg-group-o .msg:redisplay ;
: display-sync-done ( -- )
    rows  msg-group-o .msg:redisplay ;

: display-one-msg { d: msgt -- }
    msg-group-o >o
    msgt ['] msg:display catch IF  ." invalid entry"  cr  2drop  THEN
    o> ;

Forward silent-join

\ !!FIXME!! should use an asynchronous "do-when-connected" thing

: +unique-con ( -- ) o msg-group-o .msg:peers[] +unique$ ;

Forward +chat-control

: chat-silent-join ( -- )
    reconnect( ." silent join " o h. connection h. cr )
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

: reconnect-chat ( addr u $chat -- )
    peer-buf $!buf  last# peer-buf $@
    reconnect( ." reconnect " 2dup 2dup + 1- c@ 1+ - .addr$ cr )
    reconnect( ." in group: " last# dup h. $. cr )
    0 >o $A $A [: reconnect( ." prepare reconnection" cr )
      ?msg-context >o silent-last# ! o>
      ['] chat-rqd-nat ['] chat-rqd-nonat ind-addr @ select rqd! ;]
    addr-connect 2dup d0= IF  2drop  ELSE  push[] $+[]! avalanche-to  THEN o> ;

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
		fix [IFDEF] gps:gps_fix_t-altHAE gps:gps_fix_t-altHAE
		[ELSE] [IFDEF] gps:gps_fix_t-altMSL gps:gps_fix_t-altMSL
		    [ELSE] [IFDEF] gps:gps_fix_t-altitude gps:gps_fix_t-altitude
			[ELSE] drop 0e { f^ dummy } dummy
			[THEN]
		    [THEN]
		[THEN]                      df@ coord" 2 sf[]!
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

: pk.host ( -- addr u ) [: pk@ type host$ $. ;] $tmp ;
: pk1.host ( -- addr u ) [: pk@ key| type host$ $. ;] $tmp ;

Forward msg:last?
Forward msg:last

hash: have#       \ list of owner ids per hash
hash: have-group# \ list of interested groups per hash
hash: fetch#      \ list of wanted hashs->fetcher objects
\ state: want, fetching, got it
\ methods: want->fetch, fetching-progress, fetch->got it
hash: fetch-finish#
Variable fetch-queue[]

: >send-have ( addr u -- )
    msg-group-o >r [:
	have-group# #@ dup IF
	    bounds ?DO
		fetch( ." send have to group '"
		msg-group-o .msg:name$ forth:type
		." ' about hash '" ihave$ $@ 85type forth:cr )
		I @ to msg-group-o 0 .(avalanche-msg)
		push[] ['] >msg-log $[]map
	    cell +LOOP
	ELSE  2drop  THEN
    ;] catch
    r> to msg-group-o  cleanup-msg  throw ;

forward ihave>push

also fetcher
:noname fetching# to state ; fetcher:class is fetch
' 2drop fetcher:class is fetching
:noname have# to state
    last# $@ 2dup ihave$ $+! ihave>push
    >send-have ; fetcher:class is got-it
previous

: .@host.id ( pk+host u -- )
    '@' emit
    2dup keysize safe/string type '.' emit
    key| .simple-id ;
: .ihaves ( -- )
    ." ====== hash owend by ======" cr
    have# [: dup $@ 85type ." :"
	cell+ $@ bounds U+DO
	    space I $@ .@host.id
	cell +LOOP cr ;] #map ;

: check-ihave ( sig u1 hash u2 -- sig u1 hash u2 )
    c:0key 2dup c:hash 2over  dup sigpksize# u< IF  sig-unsigned !!sig!!  THEN
    2dup sigpksize# - 2dup c:hash + date-sig? !!sig!! 2drop ;
: gen-ihave ( hash u1 -- sig u2 )
    host$ $@ [: .pk type ;] $tmp ;

: >ihave.id ( hash u1 pk.id u2 -- )
    2swap bounds U+DO  2dup I keysize have# #!ins[]  keysize +LOOP  2drop ;
: >ihave ( hash u -- )
    0 .gen-ihave >ihave.id ;

: msg-pack ( -- xt )
    0 push[] !@  0 ihave$ !@
    [{: push ihave :}h1 push push[] !  ihave ihave$ ! ;] ;

: push-msg ( o:parent -- )
    up@ receiver-task <> IF
	avalanche-msg
    ELSE wait-task @ ?dup-IF
	    >r
	    o msg-group-o msg-pack [{: xo group-o xt: pack :}h1
		pack
		avalanche( ." Avalanche to: " group-o h. cr )
		group-o to msg-group-o xo .avalanche-msg ;]
	    r> send-event
	ELSE  2drop  THEN
    THEN ;
: show-msg ( addr u -- )
    parent dup IF  .wait-task @ dup up@ <> and  THEN
    ?dup-IF
	dup >r <hide>
	$make o msg-group-o
	[{: w^ m xo group-o :}h1
	    group-o to msg-group-o
	    xo >o m $@ do-msg-nestsig m $free o>
	    ctrl L inskey ;]
	r> send-event
    ELSE  do-msg-nestsig  THEN ;

: date>i ( date -- i )
    msg-group-o .msg:log[] dup $[]# >r $search[]date r> 1- umin ;
: date>i' ( date -- i )
    msg-group-o .msg:log[] dup $[]# >r $search[]date r> umin ;
: sighash? ( addr u -- flag )
    over le-64@ date>i
    dup 0< IF  drop 2drop  false  EXIT  THEN  >r
    over le-64@ 64#1 64+ date>i' >r [ 1 64s ]L /string
    r> r> U+DO
	c:0key I msg-group-o .msg:log[] $[]@ sigonly@ >hash
	2dup hashtmp over str= IF
	    verbose( ." match @"  i . forth:cr )
	    I to log#  2drop true  UNLOOP   EXIT
	ELSE  verbose( 2dup 85type ."  <> " hashtmp over 85type )  THEN
    LOOP
    2drop false ;

: msg-key! ( addr u -- )
    0 msg-group-o .msg:keys[] [: rot >r 2over str= r> or ;] $[]map
    IF  2drop  ELSE  \ ." msg-key+ " 2dup 85type forth:cr
	$make msg-group-o .msg:keys[] >back  THEN ;

\ message commands

scope{ net2o-base

\g 
\g ### message commands ###
\g 

reply-table $@ inherit-table msg-table

$20 net2o: msg-start ( $:pksig -- ) \g start message
    1 !!>order? $> msg:start ;
+net2o: msg-tag ( $:tag -- ) \g tagging (can be anywhere)
    2 !!>=order? $> msg:tag ;
+net2o: msg-id ( $:id -- ) \g a hash id
    2 !!>=order? $> msg:id ;
+net2o: msg-chain ( $:dates,sighash -- ) \g chained to message[s]
    2 !!>=order? $> msg:chain ;
+net2o: msg-signal ( $:pubkey -- ) \g signal message to one person
    2 !!>=order? $> msg:signal ;
+net2o: msg-re ( $:hash ) \g relate to some object
    2 !!>=order? $> msg:re ;
+net2o: msg-text ( $:msg -- ) \g specify message string
    2 !!>=order? $> msg:text ;
+net2o: msg-object ( $:object type -- ) \g specify an object, e.g. an image
    2 !!>=order? 64>n $> rot msg:object ;
+net2o: msg-action ( $:msg -- ) \g specify action string
    2 !!>=order? $> msg:action ;
+net2o: msg-payment ( $:contract -- ) \g payment transaction
    2 !!>=order? $> msg:payment ;
+net2o: msg-otrify ( $:date+sig $:newdate+sig -- ) \g turn a past message into OTR
    $> $> msg:otrify ;
+net2o: msg-coord ( $:gps -- ) \g GPS coordinates
    2 !!>=order? $> msg:coord ;
+net2o: msg-url ( $:url -- ) \g specify message URL
    $> msg:url ;
+net2o: msg-like ( xchar -- ) \g add a like
    64>n msg:like ;
+net2o: msg-lock ( $:key -- ) \g lock down communciation
    $> msg:lock ;
+net2o: msg-unlock ( -- ) \g unlock communication
    msg:unlock ;
+net2o: msg-perms ( $:pk perm -- ) \g permissions
    $> msg:perms ;
+net2o: msg-vote ( xchar -- ) \g add a vote tag; votes are set by likes
    64>n msg:vote ;
+net2o: msg-text+format ( $text format -- )
    64>n >r $> r> msg:text+format ;

$60 net2o: msg-silent-start ( $:pksig -- ) \g silent message tag
    1 !!>order? $40 c-state !  $> msg:silent-start ;
+net2o: msg-hashs ( $:hashs -- ) \g ihave part 1 within signed message
    ( $40 !!order? )  $> msg:hashs ;
+net2o: msg-hash-id ( $:id -- ) \g ihave part 2 within signed message
    ( $41 !!order? )  $> msg:hash-id ;
+net2o: msg-otrify2 ( $:date+sig $:newdate+sig -- ) \g turn a past message into OTR, silent version
    $40 !!order?  $> $> msg:otrify ;
+net2o: msg-updates ( $:fileinfo $:hash -- ) \g Files got an update.
    \g The fileinfo string contains fileno:len tuples in command encoding.
    \g Each additional context is hashed to a 64 byte hash, and all the hashs
    \g are hashed together sequentially in the same order as the fileinfo
    \g describes.
    $40 !!order?  $> $> msg:updates ;
}scope

msg-table $save

' context-table is gen-table

\ Code for displaying messages: logstyle for TUI deferred-based

: .otr-info ( -- )
    <info> ." [otr] " <default> "[otr] " notify+ notify-otr? on ;
: .otr-err ( -- )
    <err> ." [exp] " <default> 1 notify-otr? ! ;
: .otr ( tick -- )
    64dup 64#-1 64= IF  64drop  notify-otr? off  EXIT  THEN
    ticks 64- 64dup fuzzedtime# 64negate 64< IF  64drop .otr-err  EXIT  THEN
    otrsig-delta# fuzzedtime# 64+ 64< IF  .otr-info  THEN ;

config:logmask-tui# Value logmask#

: .log-num  ( -- )
    logmask# @ log:num  and IF '#' emit log# u.  THEN ;
: .log-date ( 64ticks -- )
    logmask# @ log:date and IF .ticks space  ELSE  64drop  THEN ;
: .log-end  ( 64ticks -- )
    logmask# @ log:end  and IF  64dup .ticks space  THEN  .otr ;

\ logstyle for GUI bitmask-based

Defer update-log
' noop is update-log

: .group ( addr u -- )
    2dup printable? IF  forth:type  ELSE  ." @" .key-id  THEN ;

scope: logstyles
: +num  log:num  logmask# or! update-log ;
: -num  log:num  invert logmask# and! update-log ;
: +date log:date logmask# or! update-log ;
: -date log:date invert logmask# and! update-log ;
: +end  log:end  logmask# or! update-log ;
: -end  log:end  invert logmask# and! update-log ;
: +len  log:len  logmask# or! update-log ;
: -len  log:len  invert logmask# and! update-log ;
: +perm log:perm logmask# or! update-log ;
: -perm log:perm invert logmask# and! update-log ;
}scope

:noname ( addr u -- )
    2dup key| 0 .pk@ key| str= IF  2drop un-cmd  EXIT  THEN
    last# >r  2dup key| to msg:id$
    [: .simple-id ." : " ;] $tmp notify-nick!
    r> to last# ; msg-notify-class is msg:start
' 2drop msg-notify-class is msg:silent-start
:noname ( addr u -- ) "#" notify+ $utf8> notify+
; msg-notify-class is msg:tag
:noname ( addr u -- )
    2dup [: ." @" .simple-id ;] $tmp notify+ ; msg-notify-class is msg:signal
:noname ( addr u -- ) $utf8> notify+ ; msg-notify-class is msg:text
:noname ( addr u format -- ) drop $utf8> notify+ ; msg-notify-class is msg:text+format
:noname ( addr u -- ) $utf8> notify+ ; msg-notify-class is msg:url
:noname ( addr u -- ) $utf8> notify+ ; msg-notify-class is msg:action
' 2drop msg-notify-class is msg:chain
' 2drop msg-notify-class is msg:re
' 2drop  msg-notify-class is msg:lock
' noop  msg-notify-class is msg:unlock
:noname 2drop 64drop ; msg-notify-class is msg:perms
' drop  msg-notify-class is msg:away
' 2drop msg-notify-class is msg:coord
:noname 2drop 2drop ." otrify " ; msg-notify-class is msg:otrify
' 2drop msg-notify-class is msg:hashs
' 2drop msg-notify-class is msg:hash-id
:noname case
	msg:image# of 2drop "img[] " notify+ endof
	msg:thumbnail# of 2drop "thumb[] " notify+ endof
	msg:audio# of 2drop "audio[] " notify+ endof
	msg:video# of 2drop "video[] " notify+ endof
	2drop
    endcase ; msg-notify-class is msg:object
:noname ( -- )
    msg-notify ; msg-notify-class is msg:end
:noname ( xchar -- ) ['] xemit $tmp notify+ ; msg-notify-class is msg:like
:noname ( xchar -- ) [: cr ." vote: " xemit ;] $tmp notify+ ; msg-notify-class is msg:vote

\ msg scan for hashes class

msg:class class
    field: ?hashs[]
end-class msg-?hash-class

' 2drop msg-?hash-class is msg:start
' noop  msg-?hash-class is msg:end
' 2drop msg-?hash-class is msg:tag
' 2drop msg-?hash-class is msg:signal
' 2drop msg-?hash-class is msg:chain
' 2drop msg-?hash-class is msg:id
' 2drop msg-?hash-class is msg:re
' 2drop msg-?hash-class is msg:text
:noname 2drop drop ; msg-?hash-class is msg:text+format
' 2drop msg-?hash-class is msg:url
' drop  msg-?hash-class is msg:like
' drop  msg-?hash-class is msg:vote
:noname ( addr u -- )
    0 .v-dec$ dup IF
	msg-key!  msg-group-o .msg:+lock  THEN ; msg-?hash-class is msg:lock
:noname ( -- )
    msg-group-o .msg:-lock ; msg-?hash-class is msg:unlock
' drop msg-?hash-class is msg:away
:noname 2drop 64drop ; msg-?hash-class is msg:perms
:noname ( addr u id -- )
    case
	msg:image#      of  key| ?hashs[] $+[]!  endof
	msg:thumbnail#  of  key| ?hashs[] $+[]!  endof
	msg:patch#      of  key| ?hashs[] $+[]!  endof
	msg:snapshot#   of  key| ?hashs[] $+[]!  endof
	msg:audio#      of  key| ?hashs[] $+[]!  endof
	msg:audio-idx#  of  key| ?hashs[] $+[]!  endof
	msg:video#      of  key| ?hashs[] $+[]!  endof
	msg:video-idx#  of  key| ?hashs[] $+[]!  endof
	2drop
    endcase ; msg-?hash-class is msg:object

: msg-scan-hash ( ... xt -- ... )
    msg-?hash-class new >o
    msg-table @ token-table !
    catch dispose o> throw ;

\ main message class

:noname ( addr u -- )
    last# >r  2dup key| to msg:id$
    false to msg:silent?
    .log-num
    2dup startdate@ .log-date
    2dup enddate@ .log-end
    .key-id ." : " 
    r> to last# ; msg:class is msg:start
:noname ( addr u -- )
    silent( 2dup startdate@ .log-date 2dup .key-id ." : Silent: " )
    key| to msg:id$ true to msg:silent? ; msg:class is msg:silent-start
:noname ( addr u -- ) $utf8>
    <warn> '#' forth:emit .group <default> ; msg:class is msg:tag
:noname ( addr u -- ) last# >r
    key| 2dup 0 .pk@ key| str=
    IF   <err>  THEN ." @" .key-id? <default>
    r> to last# ; msg:class is msg:signal
:noname ( addr u -- )
    2dup sighash? IF  <info>  ELSE  <err>  THEN
    ."  <" over le-64@ .ticks
    verbose( dup keysize - /string ." ," 85type )else( 2drop ) <default>
; msg:class is msg:chain
:noname ( addr u -- )
    space <warn> ." [" 85type ." ]->" <default> ; msg:class is msg:re
:noname ( addr u -- )
    space <warn> ." [" 85type ." ]:" <default> ; msg:class is msg:id
:noname ( addr u -- ) utf8-sanitize ; msg:class is msg:text
: format>ansi ( format -- ansi )
    0
    over msg:#bold and 0<> Bold and or
    over msg:#italic and 0<> Italic and or
    over msg:#underline and 0<> Underline and or
    swap msg:#strikethrough and 0<> Strikethrough and or ;
:noname ( addr u format -- )
    format>ansi attr!
    utf8-sanitize 0 attr! ; msg:class is msg:text+format
:noname ( addr u -- ) $utf8>
    <warn> forth:type <default> ; msg:class is msg:url
:noname ( xchar -- )
    <info> utf8emit <default> ; msg:class is msg:like
:noname ( xchar -- )
    <info> cr ." vote: " utf8emit <default> ; msg:class is msg:vote
:noname ( addr u -- )
    0 .v-dec$ dup IF
	msg-key!  msg-group-o .msg:+lock
	<info> ." chat is locked" <default>
    ELSE  2drop
	<err> ." locked out of chat" <default>
    THEN ; msg:class is msg:lock
:noname ( -- )  msg-group-o .msg:-lock
    <info> ." chat is free for all" <default> ; msg:class is msg:unlock
' drop msg:class is msg:away
: .perms ( n -- )
    "👹" bounds U+DO
	dup 1 and IF  I xc@ xemit  THEN  2/
    I I' over - x-size  +LOOP  drop ;
:noname { 64^ perm d: pk -- }
    perm [ 1 64s ]L pk msg-group-o .msg:perms# #!
    pk .key-id ." : " perm 64@ 64>n .perms space
; msg:class is msg:perms
:noname ( hash u -- )
    silent( ." hash: " 2dup 85type forth:cr )
    to msg:hashs$
; msg:class is msg:hashs
:noname ( id u -- )
    silent( ." id: " 2dup forth:type forth:cr )
    fetch( ." ihave:" msg:id$ .key-id '.' emit 2dup type msg:hashs$ bounds U+DO
    forth:cr I keysize 85type keysize +LOOP forth:cr )
    msg:id$ key| [: type type ;] $tmp
    msg:hashs$ 2swap >ihave.id
; msg:class is msg:hash-id

: hash-finished { d: hash -- }
    fetch( ." finished " 2dup 85type forth:cr )
    hash fetch# #@ IF  cell+ .fetcher:got-it  ELSE  drop  THEN
    hash fetch-finish# #@ dup IF
	bounds U+DO
	    hash I @ execute
	cell +LOOP
	last# bucket-off
    ELSE  2drop  THEN
    hash >ihave  hash drop free throw ;

: fetch-hash ( hashaddr u tsk -- )
    >r save-mem
    fetch( ." fetching " 2dup 85type forth:cr )
    2dup fetch# #@ IF  cell+ .fetcher:fetch  ELSE  drop  THEN
    2dup net2o:copy#
    r> [{: d: hash tsk :}h1
	hash [{: d: hash :}h1 hash hash-finished ;]
	tsk send-event ;]
    lastfile@ >o to file-xt o> ;

: fetch-hashs ( addr u tsk pk$ -- )
    { tsk pk$ | hashs }
    fetch( ." fetch from " pk$ $@ .@host.id forth:cr )
    bounds U+DO
	net2o-code expect+slurp $10 blocksize! $A blockalign!
	I' I U+DO
	    false  I keysize have# #@  bounds U+DO
		I $@ pk$ $@ str= or
	    cell +LOOP
	    IF
		I keysize tsk fetch-hash
		1 +to hashs
	    THEN
	    hashs $10 u>= ?LEAVE
	keysize +LOOP
	end-code| net2o:close-all
    keysize hashs *  0 to hashs  +LOOP ;

: fetch-queue { tsk w^ want# -- }
    0 .pk1.host $make { w^ pk$ }
    want# tsk pk$ [{: tsk pk$ :}l { want }
	want $@ pk$ $@ str= IF
	    msg( ." I really should have this myself" forth:cr )
	    \ don't fetch from myself
	ELSE
	    want $@ [: $8 $E pk1-connect? ;] catch 0=
	    IF
		IF
		    +resend +flow-control
		    want cell+ $@ tsk want fetch-hashs
		    disconnect-me
		THEN
	    ELSE
		fetch( ." failed, doesn't connect" forth:cr )
		nothrow 2drop
	    THEN
	THEN ;] #map
    want# #frees
    pk$ $free ;

: fetch>want ( -- want# )
    { | w^ want# }
    fetch# want# [{: want# :}l
	dup cell+ $@ drop cell+ >o fetcher:state o> 0= IF
	    $@ 2dup have# #@ bounds U+DO
		2dup I $@ want# #+!
	    cell +LOOP  2drop
	ELSE  drop  THEN ;] #map
    want# @ ;

fetcher:class ' new static-a with-allocater Constant fetcher-prototype
: >fetch# ( addr u -- )
    [:  2dup fetch# #@ d0= IF
	    fetcher-prototype cell- [ fetcher:class >osize @ cell+ ]L
	    2over fetch# #!
	THEN ;] resize-sema c-section  2drop ;

: transmit-queue ( queue -- )
    up@ [{: w^ queue[] task :}h1
	queue[] ['] >fetch# $[]map
	task fetch>want fetch-queue ;]
    ?query-task send-event ;

Variable queue?
: enqueue ( -- )
    -1 queue? !@ 0= IF
	[: 0 fetch-queue[] !@ queue? off transmit-queue ;]
	up@ send-event
    THEN ;

forward need-hashed?
: >have-group ( addr u -- )
    last# >r
    msg-group-o { w^ grp }
    2dup have-group# #@ nip IF
	grp last# cell+ +unique$  2drop
    ELSE
	grp cell 2swap have-group# #!
    THEN  r> to last# ;

: >fetch-queue ( addr u -- )
    2dup need-hashed? IF
	fetch-queue[] $ins[] drop
    ELSE  >ihave  THEN ;
: ?fetch ( addr u -- )
    key| 2dup >have-group >fetch-queue ;

: .posting ( addr u -- )
    2dup keysize /string
    2dup printable? IF  '[' emit type '@' emit
    ELSE  ." #["  85type ." /@"  THEN
    key| .key-id? ;

:noname ( addr u type -- )
    space <warn> case
	msg:image#     of  ." img["      2dup 85type ?fetch  endof
	msg:thumbnail# of  ." thumb["    2dup key| 85type
	    space 2dup keysize safe/string IF  c@ '0' + emit  ELSE  drop  THEN
	    ?fetch  endof
	msg:audio#     of  ." audio["    2dup 85type ?fetch  endof
	msg:video#     of  ." video["    2dup 85type ?fetch  endof
	msg:audio-idx# of  ." audio-idx[" 2dup 85type ?fetch  endof
	msg:video-idx# of  ." video-idx[" 2dup 85type ?fetch  endof
	msg:patch#     of  ." patch["    85type  endof
	msg:snapshot#  of  ." snapshot[" 85type  endof
	msg:message#   of  ." message["  85type  endof
	msg:posting#   of  ." posting" .posting  endof
	drop .posting
	0
    endcase ." ]" <default> ;
msg:class is msg:object
:noname ( addr u -- ) $utf8>
    <warn> forth:type <default> ; msg:class is msg:action
:noname ( addr u -- )
    <warn> ."  GPS: " .coords <default> ; msg:class is msg:coord

: wait-2s-key ( -- )
    ntime 50 0 DO  key? ?LEAVE
    2dup i #40000000 um* d+ deadline  LOOP  2drop ;
: xclear ( addr u -- ) x-width 1+ x-erase ;

:noname ( -- )
    <info>
    [: ." nobody's online" msg-group-o .msg:?otr 0= IF ." , saving away"  THEN ;] $tmp
    2dup type <default>
    wait-2s-key xclear ; msg:class is msg:.nobody

\ encrypt+sign
\ features: signature verification only when key is known
\           identity only revealed when correctly decrypted

: msg-dec-sig? ( addr u -- addr' u' flag )
    sigpksize# - 2dup + { pksig }
    msg-group-o .msg:keys[] $@ bounds U+DO
	I $@ 2over pksig decrypt-sig?
	dup -5 <> IF
	    >r 2nip r> unloop  EXIT
	THEN  drop 2drop
    cell +LOOP
    sigpksize# +  -5 ;

: msg-sig? ( addr u -- addr u' flag )
    skip-sig? @ IF   quicksig( pk-quick-sig? )else( pk-date? )
    ELSE  pk-sig?  THEN ;

: msg-dec?-sig? ( addr u -- addr' u' flag )
    2dup 2 - + c@ $80 and IF  msg-dec-sig?  ELSE  msg-sig?  THEN ;
: msg-dec?-sig?-fast ( addr u -- addr' u' flag )
    2dup 2 - + c@ $80 and IF  msg-dec-sig?  ELSE  pk-date?  THEN ;
: ?msg-dec-sig? ( addr u -- addr' u' )
    2dup 2 - + c@ $80 and IF  msg-dec-sig? !!sig!!  THEN ;
: msg-log-dec@ ( index -- addr u )
    msg-group-o .msg:log[] $[]@ ?msg-dec-sig? ;

: replace-sig { addrsig usig addrmsg umsg -- }
    addrsig usig addrmsg umsg usig - [: type type ;] $tmp
    2dup msg-dec?-sig? !!sig!! 2drop addrmsg umsg smove ;
: new-otrsig ( addr u flag -- addrsig usig )
    >r 2dup startdate@ old>otr
    predate-key keccak# c:key@ c:key# smove
    [: sktmp pkmod sk@ drop >modkey .encsign-rest ;]
    ['] .sig r@ select $tmp
    2dup + 2 - r> swap cor!
    ( 2dup dump ) 1 64s /string ;
:noname { sig u' addr u -- }
    u' 64'+ u =  u sigsize# = and IF
	addr u startdate@ 64dup date>i >r 64#1 64+ date>i' r>
	\ 2dup = IF  ."  [otrified] "  addr u startdate@ .ticks  THEN
	U+DO
	    I msg-log-dec@ 
	    2dup dup sigpksize# - /string key| msg:id$ str= IF
		dup u - /string addr u str= IF
		    otrify( I [: ."  [OTRifying] #" u. forth:cr ;] do-debug )
		    I [: ."  [OTRify] #" u. ;] $tmp forth:type
		    sig u' I msg-group-o .msg:log[] $[]@ replace-sig
		    save-msgs&
		ELSE
		    ."  [OTRified] #" I u.
		THEN
	    ELSE
		otrify( I [: ."  [OTRifignore] #" u. forth:cr ;] do-debug )
		2drop
	    THEN
	LOOP
    THEN ; msg:class is msg:otrify

:noname ( -- )
    msg:silent? 0= IF  forth:cr  THEN  enqueue ; msg:class is msg:end

\g 
\g ### group description commands ###
\g 

hash: group#

static-a to allocater
align here
groups:class new Constant group-o
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
    pk@ pkc swap move  sk@ skc swap move \ normalize pkc
    pkc keysize 3 * \ hash of pkc+pk1+skc keyed with "group"
    "group" keyed-hash#128 .chats/ ;

: read-chatgroups ( -- )
    0 ..chats/group [: type ." .v2o" ;] $tmp
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

: admin>pk ( -- )
    groups:admin sec@ drop dup sk-mask
    keysize addr groups:id$ $!len
    groups:id$ drop sk>pk ;

: gen-admin-key ( -- )
    $20 rng$ groups:admin sec! admin>pk ;

: save-chatgroups ( -- )
    0 ..chats/group enc-filename $!
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
    IF  ." =" 2drop
    ELSE  ''' emit <info> 85type <default> ''' emit THEN space
    groups:member[] [: '@' emit .simple-id space ;] $[]map
\    ." admin " groups:admin[] [: '@' emit .simple-id space ;] $[]map
    ." +" groups:perms# x64.
    o> cr ;
: .chatgroups ( -- )
    groups>sort[]
    group-list[] $@ bounds ?DO  I @ .chatgroup  cell +LOOP ;

: ?pkgroup ( addr u -- addr u )
    \ if no group has been selected, use the pubkey as group
    last# 0= IF  2dup + sigpksize# - keysize >group  THEN ;

: handle-msg ( addr-o u-o addr-dec u-dec -- )
    ?pkgroup 2swap >msg-log
    2dup d0<> replay-mode @ 0= and \ do something if it is new
    IF
	2over show-msg 2dup push[] $+[]!
    THEN  2drop 2drop ;

\g 
\g ### messaging commands ###
\g 

scope{ net2o-base

$34 net2o: message ( -- o:msg ) \g push a message object
    perm-mask @ perm%msg and 0= !!msg-perm!!
    ?msg-context n:>o c-state off  0 to last# ;

msging-table >table

reply-table $@ inherit-table msging-table

$21 net2o: msg-group ( $:group -- ) \g set group
    $> >group ;
+net2o: msg-join ( $:group -- ) \g join a chat group
    $> >load-group parent >o
    +unique-con +chat-control
    wait-task @ ?dup-IF  <hide>  THEN
    o> ;
+net2o: msg-leave ( $:group -- ) \g leave a chat group
    $> >group parent msg-group-o .msg:peers[] del$cell ;
+net2o: msg-reconnect ( $:pubkey+addr -- ) \g rewire distribution tree
    $> $make
    [{: $chat d: last-msg xo group-o :}h1
	group-o to msg-group-o
	last-msg $chat xo .reconnect-chat ;]
    parent .wait-task @ ?query-task over select send-event ;
+net2o: msg-last? ( start end n -- ) \g query messages time start:end, n subqueries
    64>n msg:last? ;
+net2o: msg-last ( $:[tick0,msgs,..tickn] n -- ) \g query result
    64>n msg:last ;
\ old want, ignore it
+net2o: msg-want ( $:[hash0,...,hashn] -- ) \g request objects
    $> 2drop ;
\ old ihave, ignore it
+net2o: msg-ihave ( $:[hash0,...,hashn] $:[id] -- ) \g show what objects you have
    $> $> 2drop 2drop ;

net2o' nestsig net2o: msg-nestsig ( $:cmd+sig -- ) \g check sig+nest
    $> nest-sig ?dup-0=-IF
	handle-msg
    ELSE
	invalid( replay-mode @ IF  drop  ELSE  !!sig!!  THEN )else( drop )
	2drop 2drop \ balk on all wrong signatures only in invalid mode
    THEN ;
net2o' end-with net2o: msg-end-with ( -- ) \g push out avalanche
    do-req> n:o> push-msg ;

\ generate an encryt+sign packet

: ]encpksign ( -- )
    +zero16 nest$
    0 msg-group-o .msg:keys[] $[]@ encrypt$
    ['] .encsign ']nestsig ;

\ nest-sig for msg/msging classes

' message msging-class is start-req
:noname check-date \ quicksig( check-date )else( pk-sig? )
    >r 2dup r> ; msging-class is nest-sig
' message msg:class is start-req
:noname 2dup msg-dec?-sig? ; msg:class is nest-sig

' context-table is gen-table

also }scope

\ serialize hashes

: msg-add-hashs ( -- )
    0 .pk1.host $make { w^ pk$ }
    ?hashs[] pk$ [{: pk$ :}l
	2dup need-hashed? 0= IF
	    pk$ $@ 2over have# #!ins[]?
	    IF  2dup ihave$ $+!  THEN
	THEN  2drop
    ;] $[]map
    ?hashs[] $[]free
    pk$ $free ;

msging-table $save

: msg-reply ( tag -- )
    ." got reply " h. pubkey $@ key>nick forth:type forth:cr ;
: expect-msg ( o:connection -- )
    reply( ['] msg-reply )else( ['] drop ) expect-reply-xt +chat-control ;

User hashtmp$  hashtmp$ off

: last-msg@ ( -- ticks )
    last# >r
    last# $@ >group msg-group-o .msg:log[] $[]# ?dup-IF
	1- msg-group-o .msg:log[] $[]@ startdate@
    ELSE  64#0  THEN   r> to last# ;
: l.hashs ( end start -- hashaddr u )
    hashtmp$ $free
    msg-group-o .msg:log[] $[]# IF
	[: U+DO  I msg-group-o .msg:log[] $[]@ 1- dup 1 64s - safe/string forth:type
	  LOOP ;] hashtmp$ $exec hashtmp$ $@
	\ [: 2dup dump ;] stderr outfile-execute \ dump hash inputs
    ELSE  2drop s" "  THEN \ we have nothing yet
    >file-hash 1 64s umin ;
: i.date ( i -- )
    msg-group-o .msg:log[] $[]@ startdate@ 64#0 { 64^ x }
    x le-64! x 1 64s forth:type ;
: i.date+1 ( i -- )
    msg-group-o .msg:log[] $[]@ startdate@ 64#0 { 64^ x }
    64#1 64+ x le-64! x 1 64s forth:type ;
: last-msgs@ ( startdate enddate n -- addr u n' )
    \G print n intervals for messages from startdate to enddate
    \G The intervals contain the same size of messages except the
    \G last one, which may contain less (rounding down).
    \G Each interval contains a 64 bit hash of the last 64 bit of
    \G each message within the interval
    last# >r >r last# $@ >group purge-log
    msg-group-o .msg:log[] $[]#
    IF
	date>i' >r date>i' r> swap
	2dup - r> over >r 1- 1 max / 0 max 1+ -rot
	[: over >r U+DO  I i.date
	      dup I + I' umin I l.hashs forth:type
	  dup +LOOP
	  r> dup msg-group-o .msg:log[] $[]# u< IF  i.date
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
    over le-64@ .ticks 1 64s /string  ." @"
    .group ;
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
    last# $@ >group
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
    last# >r  ask-msg-files[] $[]free
    forth:. ." Messages:" forth:cr
    ?ask-msg-files ask-msg-files[] $[]# IF
	parent >o  expect+slurp
	cmdbuf# @ 0= IF  $10 blocksize! $1 blockalign!  THEN
	ask-msg-files[] ['] net2o:copy-msg $[]map o>
    ELSE
	." === nothing to sync ===" forth:cr
	parent .sync-none-xt \ sync-nothing-xt???
    THEN
    r> to last# ;

:noname ( -- 64len )
    \ poll serializes the 
    fs-outbuf $free
    fs-path $@ 2 64s /string >group
    msg-log@ over >r
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
    msg( ." chat-sync-done " 2dup forth:type forth:cr )
    >group display-sync-done !save-all-msgs
    net2o-code expect-msg close-all net2o:gen-reset end-code
    net2o:close-all
    ." === sync done ===" forth:cr sync-done-xt ;
: ev-msg-eval ( parent $pack $addr -- )
    { w^ buf w^ group }
    group $@ 2 64s /string { d: gname }
    gname >group
    msg-group-o .msg:log[] $[]# u.
    buf $@ true replay-mode ['] msg-eval !wrapper
    buf $free ?save-msg
    group $@ .chat-file ."  saved "
    msg-group-o .msg:log[] $[]# u. forth:cr
    >o -1 file-count +!@ 1 =
    IF  gname chat-sync-done  THEN  group $free
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
    [ termserver-class ] defers fs-read
; msgfs-class is fs-read
:noname ( -- )
    parent 0 fs-inbuf !@ 0 fs-path !@
    [{: px $pack $addr :}h1 px $pack $addr ev-msg-eval ;]
    parent .wait-task @ send-event
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
; msgfs-class is fs-perm?
:noname ( -- date perm )
    64#0 0 ; msgfs-class is fs-get-stat
:noname ( date perm -- )
    drop 64drop ; msgfs-class is fs-set-stat
' file-start-req msgfs-class is start-req

\ message composer

: group, ( addr u -- )
    $, msg-group ;
: <msg ( -- )
    sign[ msg-group-o .msg:?lock IF  +zero16  THEN ;

: msg> ( -- )
    \G end a message block by adding a signature
    msg-group-o .msg:?lock IF  ]encpksign  ELSE  ]pksign  THEN ;
: msg-otr> ( -- )
    \G end a message block by adding a short-time signature
    now>otr msg> ;
: msg-log, ( -- addr u )
    last-signed 2@ >msg-log ;

previous

: ?destpk ( addr u -- addr' u' )
    2dup connection .pubkey $@ key| str= IF  2drop pk@ key|  THEN ;

: last-signdate@ ( -- 64date )
    msg-group-o .msg:log[] $@ dup IF
	+ cell- $@ startdate@ 64#1 64+
    ELSE  2drop 64#-1  THEN ;

also net2o-base
: [msg,] ( xt -- )  last# >r
    msg-group$ $@ dup IF  message ?destpk 2dup >group $,
	execute  end-with
    ELSE  2drop drop  THEN  r> to last# ;

: last, ( -- )
    64#0 64#-1 ask-last# last-msgs@ >r $, r> ulit, msg-last ;

: last?, ( -- )
    last-signdate@ { 64: date }
    64#0 lit, date lit, ask-last# ulit, msg-last?
    date 64#-1 64<> IF
	date lit, 64#-1 lit, 1 ulit, msg-last?
    THEN ;

: sync-ahead?, ( -- )
    last-signdate@ 64#1 64+ lit, 64#-1 lit, ask-last# ulit, msg-last? ;

: join, ( -- )
    [: msg-join sync-ahead?,
      <msg msg-start "joined" $, msg-action msg-otr> ;] [msg,] ;

: silent-join, ( -- )
    msg-group$ $@ dup IF  message $, msg-join  end-with
    ELSE  2drop  THEN ;

: leave, ( -- )
    [: msg-leave
      <msg msg-start "left" $, msg-action msg-otr> ;] [msg,] ;

: silent-leave, ( -- )
    ['] msg-leave [msg,] ;

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
    connection .data-rmap IF  net2o-code expect-msg leave, end-code|  THEN ;
: send-silent-leave ( -- )
    connection .data-rmap IF  net2o-code expect-msg silent-leave, end-code|  THEN ;

: [group] ( xt -- flag )
    msg-group-o .msg:peers[] $@len IF
	msg-group-o .execute true
    ELSE
	0 .execute false
    THEN ;
: .chat ( addr u -- )
    [: last# >r o IF  2dup do-msg-nestsig
	ELSE  2dup display-one-msg  THEN  push[] $+[]!
	r> to last# 0 .avalanche-msg ;] [group] drop notify- ;

\ chat message, text only

: msg-tdisplay ( addr u -- )
    ( 2dup dump )
    logmask# @ log:len and IF  dup h.  THEN
    2dup 2 - + c@ $80 and IF  msg-dec-sig? IF
	    2drop <err> ." Undecryptable message" <default> cr  EXIT
	THEN  <info>  THEN
    sigpksize# - 2dup + sigpksize# >$  c-state off
    nest-cmd-loop o IF  msg:end  THEN <default> ;
: msg-tdisplay-silent ( addr u -- )
    2dup 2 - + c@ $80 and IF  msg-dec-sig? IF  2drop  EXIT  THEN  THEN
    sigpksize# - 2dup + sigpksize# >$  c-state off
    nest-cmd-loop msg:end ;
' msg-tdisplay msg:class is msg:display
' msg-tdisplay msg-notify-class is msg:display
' msg-tdisplay-silent msg-?hash-class is msg:display
: ?search-lock ( addr u -- )
    BEGIN  dup  WHILE
	    cell- 2dup + $@ sigpksize# - 1- + c@
	    [ also net2o-base net2o' msg-lock ]L
	    [ net2o' msg-unlock 1+ previous ]L within IF
		2dup + $@ ['] msg:display catch IF  2drop  THEN
		msg-group-o .msg:keys[] $[]# IF  drop 0  THEN
	    THEN
    REPEAT  2drop ;
: ?scan-pks ( addr u -- )
    bounds U+DO
	I $@  msg-dec?-sig?-fast IF  ( undecryptable )  2drop
	ELSE  sigpksize# - + keysize 2dup key# #@ d0= IF
		"key" 2swap msg-group-o .msg:pks# #!
	    ELSE  2drop  THEN
	THEN
    cell +LOOP ;

forward key-from-dht

: free-obtained-pks ( addr -- )
    [: $@ >d#id >o dht-owner $[]# 0> IF
	    key-from-dht
	    last# $free  last# cell+ $free
	THEN o> ;] #map ;
: fetch-pks ( o:peer-con -- )
    0 msg-group-o .msg:pks# [: drop 1+ ;] #map 0<>  IF
	o to connection
	{ | w^ start w^ requests }
	msg-group-o .msg:pks#
	start requests [{: start requests :}l
	    pks( ." pk@" dup $@ 85type forth:cr )
	    start @ 0= IF  net2o-code  expect-reply  THEN
	    $@ $, dht-id dht-owner? end-with
	    start @ 3 u< IF
		1 start +!
	    ELSE
		start off  1 requests +!  cookie+request
		requests @ $10 > IF  end-code|  0 to requests
		ELSE  [ also net2o-base ]   end-code  THEN
	    THEN ;] #map
	start @ IF  [ also net2o-base ] cookie+request end-code|  THEN
	msg-group-o .msg:pks# free-obtained-pks
    THEN  save-keys ;
: ?fetch-pks
    msg-group-o >o msg:peers[] $[]# 0 ?DO
	I msg:peers[] $[] @ .fetch-pks
    LOOP o> ;
: msg-tredisplay ( n -- )
    reset-time
    msg-group-o >o msg:?otr msg:-otr o> >r
    [: cells >r msg-log@ { log u } u r> - 0 max { u' }
      log u u' /string ?scan-pks  ?fetch-pks \ activate ?fetch-pks
      log u' ['] ?search-lock msg-scan-hash
      log u u' /string bounds ?DO
	  I log - cell/ to log#
	  I $@ { d: msgt }
	  msgt ['] msg:display catch IF  ." invalid entry" cr
	      2drop  THEN
      cell +LOOP
      log free throw ;] catch
    r> IF  msg-group-o .msg:+otr  THEN  throw ;
' msg-tredisplay msg:class is msg:redisplay

msg:class class
end-class textmsg-class

: .formatter ( format -- )
    dup msg:#bold and if  '*' emit  then
    dup msg:#italic and if  '/' emit  then
    dup msg:#underline and if  '_' emit  then
    dup msg:#strikethrough and if  '-' emit  then
    msg:#mono and if  '`' emit  then ;

' 2drop textmsg-class is msg:start
' 2drop textmsg-class is msg:silent-start
' 2drop textmsg-class is msg:hashs
' 2drop textmsg-class is msg:hash-id
' 2drop textmsg-class is msg:updates
:noname '#' emit type ; textmsg-class is msg:tag
:noname '@' emit .simple-id ; textmsg-class is msg:signal
' 2drop textmsg-class is msg:re
' 2drop textmsg-class is msg:chain
' 2drop textmsg-class is msg:id
' 2drop textmsg-class is msg:lock
' noop textmsg-class is msg:unlock
' drop textmsg-class is msg:away
' type textmsg-class is msg:text
' type textmsg-class is msg:url
:noname drop 2drop ; textmsg-class is msg:object
:noname
    dup >r .formatter type r> .formatter ; textmsg-class is msg:text+format
:noname ." /me " type ; textmsg-class is msg:action
:noname ." /here " 2drop ; textmsg-class is msg:coord
:noname ." vote:" utf8emit ; textmsg-class is msg:vote
:noname ." /like " utf8emit ; textmsg-class is msg:like
' noop textmsg-class is msg:end
:noname 2drop 2drop ; textmsg-class is msg:otrify
' 2drop textmsg-class is msg:payment

textmsg-class ' new static-a with-allocater Constant textmsg-o
msg-notify-o >o msg-table @ token-table ! o>
textmsg-o >o msg-table @ token-table ! o>

\ chat history browsing

64Variable line-date 64#-1 line-date 64!
Variable $lastline

: !date ( addr u -- addr u )
    2dup + sigsize# - le-64@ line-date 64! ;
: find-prev-chatline { maxlen addr -- max span addr span }
    msg-group$ $@ >group
    msg-group-o .msg:log[] $[]# 0= IF  maxlen 0 addr over  EXIT  THEN
    line-date 64@ date>i'
    BEGIN  1- dup 0>= WHILE  dup msg-log-dec@
	dup sigpksize# - /string key| pk@ key| str=  UNTIL  THEN
    msg-log-dec@ dup 0= IF  nip
    ELSE  !date ['] msg:display textmsg-o .$tmp
	dup maxlen u> IF  dup >r maxlen 0 addr over r> grow-tib
	    2drop to addr drop to maxlen  THEN
	tuck addr maxlen smove
    THEN
    maxlen swap addr over ;
: find-next-chatline { maxlen addr -- max span addr span }
    msg-group$ $@ >group
    line-date 64@ date>i
    BEGIN  1+ dup msg-group-o .msg:log[] $[]# u< WHILE
	    dup msg-log-dec@
	dup sigpksize# - /string key| pk@ key| str=  UNTIL  THEN
    dup  msg-group-o .msg:log[] $[]# u>=
    IF    drop $lastline $@  64#-1 line-date 64!
    ELSE  msg-log-dec@ !date ['] msg:display textmsg-o .$tmp  THEN
    dup maxlen u> IF  dup >r maxlen 0 addr over r> grow-tib
	2drop to addr drop to maxlen  THEN
    tuck addr maxlen smove
    maxlen swap addr over ;

: chat-prev-line  ( max span addr pos1 -- max span addr pos2 false )
    line-date 64@ 64#-1 64= IF
	>r 2dup swap $lastline $! r>  THEN
    clear-line find-prev-chatline
    edit-update false ;
: chat-next-line  ( max span addr pos1 -- max span addr pos2 false )
    line-date 64@ 64#-1 64= IF  false  EXIT  THEN
    clear-line find-next-chatline
    edit-update false ;
: chat-enter ( max span addr pos1 -- max span addr pos2 true )
    drop over edit-update edit-curpos-off true 64#-1 line-date 64! ;

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

keycode-limit keycode-start - cells buffer: chat-ekeys
std-ekeys chat-ekeys keycode-limit keycode-start - cells move

' chat-ekeys is ekeys

' chat-next-line k-down  ebindkey
' chat-prev-line k-up    ebindkey
' chat-next-line k-next  ebindkey
' chat-prev-line k-prior ebindkey
' chat-enter     k-enter ebindkey

edit-terminal edit-out !

: chat-history ( -- )
    chat-terminal edit-out ! ;

\ chat line editor

$300 Constant maxmsg#

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
    connection .data-rmap 0= ?EXIT
    net2o-code expect-msg
    log !time end-with join, get-ip end-code ;

: chat-entry ( -- )  ?.net2o/chats  word-args
    <warn> ." Type ctrl-D or '/bye' as single item to quit" <default> cr ;

also net2o-base
\ chain messages to one previous message
: chain, ( msgaddr u -- )
    [: 2dup startdate@ 64#0 { 64^ sd } sd le-64!  sd 1 64s forth:type
	c:0key sigonly@ >hash hashtmp hash#128 forth:type ;] $tmp $, msg-chain ;
: push, ( -- )
    push[] [: $, nestsig ;] $[]map ;
: ihave>push ( -- )
    ihave$ $@ bounds U+DO
	I I' over - max#have umin
	[: <msg msg-silent-start
	    $, msg-hashs
	    host$ $@ $, msg-hash-id
	    msg> ;] 0 .gen-cmd$
	+last-signed last-signed 2@ push[] $+[]!
    max#have +LOOP  ihave$ $free ;

: (send-avalanche) ( xt -- addr u flag )
    [:  [: <msg msg-start execute msg> ;] 0 .gen-cmd$
	+last-signed msg-log, ;] [group] ihave>push ;
previous

: send-avalanche ( xt -- )
    msg-group-o .msg:?otr IF  now>otr  ELSE  now>never  THEN
    (send-avalanche)
    >r .chat r> 0= IF  msg-group-o .msg:.nobody  THEN ;

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

\ do otrify

also net2o-base

: do-otrify ( n -- ) >r
    msg-group$ $@ >group msg-group-o .msg:log[] $@
    r> cells dup 0< IF  over + 0 max  THEN safe/string
    IF  $@
	2dup + 2 - c@ $80 and dup >r
	IF  msg-dec-sig?  ELSE  pk-sig?  THEN  !!sig!!
	2dup + sigpksize# - sigpksize#
	over pk@ drop 32b= IF
	    keysize /string $,
	    r> new-otrsig $,
	    msg-otrify 2drop
	ELSE
	    rdrop 2drop 2drop ." not your message!" forth:cr
	THEN
    ELSE  drop  THEN ;

previous

\ debugging aids for classes

: .ack ( o:ack -- o:ack )
    ." ack context:" cr
    ." rtdelay: " rtdelay 64@ s64. cr ;

: .context ( o:context -- o:context )
    ." Connected with: " .con-id cr
    ack-context @ ?dup-IF  ..ack  THEN ;

: .notify ( -- )
    ." notify " config:notify?# ?
    ." led " config:notify-rgb# @ h. config:notify-on# ? config:notify-off# ?
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

false value away?

: group#map ( xt -- )
    msg-group# swap [{: xt: xt :}l cell+ $@ drop cell+ .xt ;] #map ;

$100 buffer: permchar>bits
msg:role-admin# msg:key-admin# msg:moderator# or or 'a' permchar>bits + c!
msg:role-admin# 'r' permchar>bits + c!
msg:key-admin#  'k' permchar>bits + c!
msg:moderator#  'm' permchar>bits + c!
msg:troll#      't' permchar>bits + c!
: >perms ( addr u -- perms )
    0 -rot bounds ?DO  I c@ permchar>bits + c@
	dup 0= !!inv-perm!! or  LOOP ;

uval-o chat-cmd-o

object uclass chat-cmd-o
    \ internal stuff
    umethod ./otr-info
    umethod ./mono-info
also net2o-base scope: /chat
    umethod /away ( addr u -- )
    \U away [<action>]      send string or "away from keyboard" as action
    \G away: send string or "away from keyboard" as action
    synonym /back /away
    umethod /beacons ( addr u -- )
    \U beacons              list beacons
    \G beacons: list all beacons
    umethod /bye ( addr u -- )
    \U bye
    \G bye: leaves the current chat
    umethod /cancel ( addr u -- )
    \U cancel #line[s]      cancel message
    \G cancel local messages
    umethod /chat ( addr u -- )
    \U chat [group][@user]  switch/connect chat
    \G chat: switch to chat with user or group
    umethod /chats ( addr u -- )
    \U chats                list chats
    \G chats: list all chats
    umethod /connections ( addr u -- )
    \U connections          list active connections
    \G connections: list active connections
    umethod /fetch ( addr u -- )
    \U fetch                trigger fetching
    \G fetch: fetch the hashes I want
    umethod /format ( addr u -- )
    \U format               set format string
    \G format: set format string to specific characters or on/off
    umethod /gps ( addr u -- )
    \U gps                  send coordinates
    \G gps: send your coordinates
    umethod /have ( addr u -- )
    \U have                 print out have list
    \G have: print out the hashes and their providers
    umethod /help ( addr u -- )
    \U help                 show help
    \G help: list help
    synonym /here /gps
    umethod /imgs ( addr u -- )
    \U imgs                 print out img list
    \G imgs: print out hashes for album viewer
    umethod /invitations ( addr u -- )
    \U invitations          handle invitations
    \G invitations: handle invitations: accept, ignore or block invitations
    umethod /like ( addr u -- )
    \U like #line [emoji]   like message
    umethod /log ( addr u -- )
    \U log [#lines]         show log
    \G log: show the log, default is a screenful
    umethod /logstyle ( addr u -- )
    \U logstyle [+-style]   set log style
    \G logstyle: set log styles, the following settings exist:
    \G logstyle: +num       the message number per log line
    \G logstyle: +date      the date per log line
    \G logstyle: +end       the end date per log line
    \G logstyle: +len       the message length per log line
    umethod /lock ( addr u -- )
    \U lock {@nick}         lock down
    \G lock: lock down communication to list of nicks
    umethod /lock? ( addr u -- )
    \U lock?                check lock status
    \G lock?: report lock status
    umethod /me ( addr u -- )
    \U me <action>          send string as action
    \G me: send remaining string as action
    umethod /myaddrs ( addr u -- )
    \U myaddrs              list my addresses
    \G myaddrs: list my own local addresses (debugging)
    umethod /!myaddrs ( addr u -- )
    \U !myaddrs             re-obtain my addresses
    \G !myaddrs: if automatic detection of address changes fail,
    \G !myaddrs: you can use this command to re-obtain your local addresses
    umethod /nat ( addr u -- )
    \U nat                  list NAT info
    \G nat: list nat traversal information of all peers in all groups
    umethod /n2o ( addr u -- )
    \U n2o <cmd>            execute n2o command
    \G n2o: Execute normal n2o command
    umethod /nick ( addr u -- )
    \U nick <orig> <alias>  Add a new nickname
    \G nick: Add a new nickname to an existing account
    umethod /notify ( addr u -- )
    \U notify always|on|off|led <rgb> <on-ms> <off-ms>|interval <time>[smh]|mode 0-3
    \G notify: Change notificaton settings
    umethod /otr ( addr u -- )
    \U otr on|off|message   turn otr mode on/off (or one-shot)
    umethod /otrify ( addr u -- )
    \U otrify #line[s]      otrify message
    \G otrify: turn an older message of yours into an OTR message
    umethod /peers ( addr u -- )
    \U peers                list peers
    \G peers: list peers in all groups
    umethod /perms ( addr u -- )
    \U perms roles {@keys}  set and change permissions of users
    \G perms: set permissions
    umethod /renat ( addr u -- )
    \U renat                redo NAT traversal
    \G renat: redo nat traversal
    umethod /rescan# ( addr u -- )
    \U rescan#              rescan for hashes
    \G rescan#: search the entire chat log for hashes and if you have them
    umethod /stats ( addr u -- )
    \U stats                show stats
    \G stats: show the statistics of transfers
    umethod /split ( addr u -- )
    \U split                split load
    \G split: reduce distribution load by reconnecting
    umethod /sync ( addr u -- )
    \U sync [+date] [-date] synchronize logs
    \G sync: synchronize chat logs, starting and/or ending at specific
    \G sync: time/date
    umethod /unlock ( addr u -- )
    \U unlock               stop lock down
    \G unlock: stop lock down
    umethod /version ( addr u -- )
    \U version              version string
    \G version: print version string
    umethod /want ( addr u -- )
    \U want                 print out want list
    \G want: print out the hashes I want
    }scope
end-class chat-cmds

chat-cmds ' new static-a with-allocater Constant text-chat-cmd-o

text-chat-cmd-o to chat-cmd-o

scope{ /chat
' 2drop is /imgs \ stub

:noname ( addr u -- )
    [: $, msg-action ;] send-avalanche ; is /me

:noname ( addr u -- )
    dup 0= IF  2drop
	away? IF  "I'm back"  ELSE  "Away from keyboard"  THEN
    THEN  away? 0= to away?
    [: $, msg-action ;] send-avalanche ; is /away

:noname ( flag -- )
    <info> ." === " IF  ." enter"  ELSE  ." leave"  THEN
    ."  otr mode ===" <default> forth:cr ; is ./otr-info
:noname ( flag -- )
    <info> ." === " IF  ." enter"  ELSE  ." leave"  THEN
    ."  mono mode ===" <default> forth:cr ; is ./mono-info

:noname ( addr u -- )
    2dup -trailing s" on" str= >r
    2dup -trailing s" off" str= r@ or IF   2drop
	msg-group-o r@ IF  .msg:+otr  ELSE  .msg:-otr  THEN
	r> ./otr-info
    ELSE  rdrop
	msg-group-o .msg:mode @ >r
	msg-group-o .msg:+otr avalanche-text
	r> msg-group-o .msg:mode !
    THEN ; is /otr

:noname ( addr u -- )  2drop
    [: msg:name$ .group ." : "
	msg:peers[] $@ bounds ?DO
	    space I @ >o .con-id space
	    ack@ .rtdelay 64@ 64>f 1n f* (.time) o>
	cell +LOOP  forth:cr ;] group#map ; is /peers

:noname ( addr u -- )  2drop
    ." send: " packets @ 0 .r '+' forth:emit packets2 @ forth:.
    ." recv: " packetr @ 0 .r '+' forth:emit packetr2 @ forth:. ; is /stats

:noname ( addr u -- )  2drop
    coord! coord@ 2dup 0 -skip nip 0= IF  2drop
    ELSE
	[: $, msg-coord ;] send-avalanche
    THEN ; is /gps

:noname ( addr u -- )
    bl skip '/' skip
    2dup [: ."     \U " forth:type ;] $tmp ['] .chathelp search-help
    [: ."     \G " forth:type ':' forth:emit ;] $tmp ['] .cmd search-help ;
is /help

:noname ( addr u -- )
    2drop .invitations ; is /invitations

:noname ( addr u -- )
    2drop ." ===== chats: "
    [:  msg:name$ msg-group$ $@ str= IF ." *" THEN
	msg:name$ .group
	." [" msg:peers[] $[]# 0 .r ." ]#"
	msg:log[] $[]# u. ;] group#map
    ." =====" forth:cr ; is /chats

:noname ( addr u -- )  2drop
    [:  ." ===== Group: " msg:name$ .group ."  =====" forth:cr
	msg:peers[] $@ bounds ?DO
	    ." --- " I @ >o .con-id ." @" remote-host$ $. ." : " return-address .addr-path
	    ."  ---" forth:cr .nat-addrs o>
	cell +LOOP ;] group#map ; is /nat

:noname ( addr u -- )
    2drop
    ." ===== all =====" forth:cr    .my-addr$s
    ." ===== public =====" forth:cr .pub-addr$s
    ." ===== private =====" forth:cr .priv-addr$s ; is /myaddrs
:noname ( addr u -- )
    2drop !my-addr ; is /!myaddrs

:noname ( addr u -- )
    ['] notify-cmds evaluate-in .notify ; is /notify

:noname ( addr u -- )
    2drop ." === beacons ===" forth:cr
    beacons# [: dup $@ .address space
      cell+ $@ over 64@ .ticks space
      1 64s safe/string bounds ?DO
	  I 2@ ?dup-IF ..con-id space THEN id.
      2 cells +LOOP forth:cr ;] #map ; is /beacons

:noname ( addr u -- )
    s>unumber? IF  drop  ELSE  2drop 0  THEN  cells >r
    msg-group-o .msg:peers[] $@ r@ u<= IF  drop rdrop  EXIT  THEN
    r> + @ >o o to connection
    ." === sync ===" forth:cr
    net2o-code expect-msg [: msg-group last?, ;] [msg,] end-code o> ; is /sync

:noname ( addr u -- )
    2drop .n2o-version space .gforth-version forth:cr ; is /version

:noname ( addr u -- )
    s>unumber? IF  drop >r  ELSE  2drop rows >r  THEN
    msg-group$ $@ >group purge-log
    r>  display-lastn ; is /log

:noname ( addr u -- )
    ['] logstyles ['] evaluate-in catch IF
	2drop drop "logstyle" /help
    THEN ; is /logstyle

:noname ( addr u -- )
    bl $split 2swap s>unumber? 0= abort" Line number needed!" drop >r
    IF  xc@  ELSE  drop  '👍'  THEN  r>
    msg-group-o .msg:log[] $[]# >r
    dup 0< IF   r@ +  THEN  r> dup 0<> - umin
    [: msg-group-o .msg:log[] $[]@ chain,
	ulit, msg-like ;] (send-avalanche) drop .chat save-msgs& ; is /like

:noname ( addr u -- )
    [: BEGIN  bl $split 2>r dup  WHILE  s>number? WHILE
		    drop do-otrify  2r>  REPEAT THEN
	2drop 2r> 2drop  now>otr
    ;] (send-avalanche) drop .chat save-msgs& ; is /otrify

:noname ( addr u -- )
    [: BEGIN  bl $split 2>r dup  WHILE
		'-' $split dup 0= IF
		    2drop s>number? IF  drop 1  ELSE  0 0  THEN
		ELSE
		    third 0= IF
			2nip s>number? IF
			    drop msg-group-o .msg:log[] $[]# swap - 0 max 1
			ELSE  0 0  THEN
		    ELSE  s>number? IF  drop >r s>number?
			    IF  drop r> over - 1+
			    ELSE  2drop rdrop  0 0  THEN
			ELSE  2drop rdrop 0 0  THEN
		    THEN
		THEN
		bounds ?DO  I msg-group-o .msg:log[] $[] $free  LOOP
		2r>  REPEAT  2drop 2r> 2drop
	0 msg-group-o .msg:log[] del$cell
    ;] msglog-sema c-section save-msgs&
; is /cancel

:noname ( addr u -- )
    msg-group-o .msg:-lock
    word-args ['] args>keylist execute-parsing
    [: key-list v-enc$ $, net2o-base:msg-lock ;] send-avalanche
    vkey keysize $make msg-group-o .msg:keys[] >back
    msg-group-o .msg:+lock
; is /lock
:noname ( addr u -- )
    2drop msg-group-o .msg:-lock
    [: net2o-base:msg-unlock ;] send-avalanche
; is /unlock
:noname ( addr u -- )
    2drop msg-group-o .msg:?lock 0= IF  ." un"  THEN  ." locked" forth:cr
; is /lock?
:noname 2drop .ihaves ; is /have
:noname 2drop scan-log-hashs
    ihave>push push[] [: >msg-log 2drop ;] $[]map
    avalanche-msg ; is /rescan#

:noname ( addr u -- )
    word-args [: parse-name >perms args>keylist ;] execute-parsing
    [{: perm :}l
	perm key-list [: key| $, dup ulit, net2o-base:msg-perms ;] $[]map drop
    ;] send-avalanche
; is /perms

:noname ( addr u -- )
    2drop -1 [IFDEF] android android:level# [ELSE] level# [THEN] +! ; is /bye

:noname ( addr u -- )
    2drop [:
      remote-host$ $. ." @" pubkey $@ .simple-id ." :" forth:cr
	true ;] search-context ; is /connections

:noname ( addr u -- )  2drop enqueue ; is /fetch

:noname ( addr u -- )
    2dup s" off" str= IF  config:chat-format$ $free  2drop  EXIT  THEN
    2dup s" on" str=  IF  2drop "*/_-`"  THEN
    5 umin  config:chat-format$ $! ; is /format

:noname ( addr u -- )  2drop
    ." Want:" forth:cr
    fetch# [: { item }
	." hash: " item $@ 85type space
	case item cell+ $@ drop cell+ .fetcher:state
	    0 of  ." want from"
		item $@ have# #@ bounds U+DO
		    forth:cr 4 spaces I $@ .@host.id
		cell +LOOP
	    endof
	    1 of  ." fetching..."  endof
	    2 of  ." got it"  endof
	endcase forth:cr
    ;] #map ; is /want
}scope

: ?slash ( addr u -- addr u flag )
    over c@ dup '/' = swap '\' = or ;

Defer chat-cmd-file-execute
:noname catch ?dup-IF  nip nip DoError THEN ; is chat-cmd-file-execute

Forward ```

: do-chat-cmd? ( addr u -- t / addr u f )
    2dup "```" str= IF  2drop ``` true  EXIT  THEN
    ?slash dup 0= ?EXIT  drop
    bl $split 2swap
    2dup save-mem over >r '/' r@ c!
    ['] /chat >wordlist find-name-in r> free throw
    ?dup-IF  nip nip name>interpret chat-cmd-file-execute true
    ELSE  drop -rot + over - false
    THEN ;

0 Value last->in
0 Value current-format

: ?flush-text ( addr -- )
    last->in ?dup-0=-IF  source drop  THEN
    tuck - dup IF
	\ ." text: '" forth:type ''' forth:emit forth:cr
	current-format ?dup-IF  ulit, $, msg-text+format
	ELSE  $, msg-text  THEN
    ELSE  2drop  THEN ;

$Variable punctation$
s" minos2/unicode/punctation.db" open-fpath-file
0= [IF] 2drop dup punctation$ $slurp forth:close-file throw
[ELSE] s" .,:;!" punctation$ $! [THEN]
$Variable brackets$
s" minos2/unicode/brackets.db" open-fpath-file
0= [IF] 2drop dup brackets$ $slurp forth:close-file throw
[ELSE] s" ()[]{}" brackets$ $! [THEN]

: x-skip1 ( addr u xc -- addr u' )
    \ Character is either xc or xc+emoji variant selector
    dup xc-size { xc size }
    dup size 3 + u>= IF
	2dup x\string- + xc@ $FE0F = IF
	    2dup x\string- x\string- + xc@ xc = IF
		x\string- x\string- EXIT
	    THEN
	THEN
    THEN
    dup size u>= IF
	2dup x\string- + xc@ xc = IF
	    x\string- EXIT
	THEN
    THEN ;
: x-skip$ ( addr u $class -- addr u' )
    $@ bounds  DO  I xc@ x-skip1  I I' over - x-size  +LOOP ;
: -skip-punctation ( addr u -- addr u' )
    BEGIN  dup >r
	punctation$ x-skip$
	brackets$   x-skip$
    dup r> = UNTIL ;

: text-rec ( addr u -- )
    2drop ['] noop rectype-nt ;
: tag-rec ( addr u -- )
    over c@ '#' = IF
	-skip-punctation
	over ?flush-text 2dup + to last->in
	[: 1 /string
	    \ ." tag: '" forth:type ''' forth:emit forth:cr
	    $, msg-tag
	;] rectype-nt
    ELSE  2drop rectype-null  THEN ;
: vote-rec ( addr u -- )
    2dup "vote:" string-prefix? IF
	over ?flush-text 2dup + to last->in
	5 /string drop xc@
	[: ulit, msg-vote ;] rectype-nt
    ELSE  2drop rectype-null  THEN ;
: pk-rec ( addr u -- rectype )
    over c@ '@' <> over 3 < or IF
	2drop rectype-null  EXIT  THEN \ minimum nick: 2 characters
    -skip-punctation  2dup 1 /string  nick>pk
    2dup d0= IF  2drop 2drop rectype-null
    ELSE
	2>r over ?flush-text + to last->in
	last->in source drop - >in !  2r>
	[:
	    \ ." signal: '" 85type ''' forth:emit forth:cr
	    $, msg-signal
	;] rectype-nt
    THEN ;
: chain-rec ( addr u -- )
    over c@ '!' = IF
	-skip-punctation  2dup 1 /string
	dup 0= IF  2drop 2drop rectype-null  EXIT  THEN
	snumber?
	case
	    0 of  endof
	    -1 of
		msg-group-o .msg:log[] $[]#
		over abs over u< IF  over 0< IF  +  ELSE  drop  THEN
		    >r over ?flush-text + to last->in  r>
		    [: msg-group-o .msg:log[] $[]@ chain, ;]
		    rectype-nt  EXIT  THEN
	    endof
	    2drop
	endcase
    THEN  2drop  rectype-null  ;

: rework-% ( addr u -- addr' u' )
    [: [: bounds ?DO
	    I c@ '%' = IF
		I 1+ I' over - 2 umin s>number drop emit 3
	    ELSE
		I c@ emit 1
	    THEN
	+LOOP ;] $tmp ;] $10 base-execute ;

: http-rec ( addr u -- )
    2dup "https://" string-prefix? >r
    2dup "http://" string-prefix? r> or IF
	over ?flush-text
	-skip-punctation 2dup + to last->in
	[: rework-% $, msg-url ;] rectype-nt
    ELSE  2drop rectype-null  THEN ;

forward hash-in

: jpeg? ( addr u -- flag )
    2dup dup 4 - 0 max safe/string ".jpg" capscompare 0= >r
    dup 5 - 0 max safe/string ".jpeg" capscompare 0= r> or ;

: >have+group ( addr u -- addr u )
    2dup key|  2dup >have-group  2dup >ihave  ihave$ $+! ;

: file-in ( addr u -- hash u )
    slurp-file over >r hash-in r> free throw >have+group ;

: suffix ( addr u -- addr' u' )
    2dup '.' scan-back nip /string ;

scope: file-suffixes
: jpg ( addr u -- )
    2dup jpeg? IF
	2dup >thumbnail
	dup IF  over >r hash-in
	    [: forth:type img-orient @ 1- 0 max forth:emit ;] $tmp
	    r> free throw  THEN
    ELSE  #0.  THEN
    2swap file-in
    2swap dup IF   >have+group  THEN
    [:  dup IF  $, msg:thumbnail# ulit, msg-object  ELSE  2drop  THEN
	$, msg:image# ulit, msg-object ;] ;
synonym jpeg jpg
synonym png jpg

: opus ( addr u -- )
    2dup file-in save-mem 2>r
    [: 5 - forth:type ." .opus" ;] $tmp file-in save-mem 2r>
    [:  over >r $, msg:audio-idx# ulit, msg-object r> free throw
	over >r $, msg:audio# ulit, msg-object r> free throw ;] ;
synonym aidx opus
}scope

: genfile-file ( addr u -- )
    file-in save-mem
    [:  over >r $, msg:files# ulit, msg-object r> free throw ;] ;

: expand-to-file ( addr u -- addr u' flag )
    drop source + over -
    BEGIN  -trailing dup  WHILE
	    2dup 7 /string rework-% file-status nip 0= IF
		2dup + source drop - >in !  true EXIT  THEN
	    bl -scan  REPEAT
    false ;

: file-rec ( addr u -- .. token )
    2dup "file://" string-prefix? IF
	expand-to-file IF
	    over ?flush-text 7 /string
	    2dup + >r  rework-% save-mem over >r
	    2dup suffix ['] file-suffixes >wordlist find-name-in
	    dup 0= IF  drop ['] genfile-file  THEN
	    catch
	    r> free throw  r> to last->in
	    0= IF  rectype-nt  EXIT  THEN
	THEN
    THEN
    2drop rectype-null ;

$100 buffer: format-chars

: !format-chars ( -- )
    format-chars $100 erase
    msg:#bold config:chat-format$ $@ bounds ?DO
	I c@ bl <> IF  dup I c@ format-chars + c!  THEN  2*
    LOOP  drop ;

: >format-chars ( addr u -- addr' u' format-chars )
    0 >r  BEGIN  dup 0> WHILE
	    over c@ format-chars + c@
	    current-format invert and
	    ?dup WHILE
		dup r@ and IF  r> xor $100 or  EXIT  THEN
		r> or >r 1 /string  REPEAT
    THEN  r> ;
: <format-chars ( addr u -- addr u' format-chars )
    0 >r  BEGIN  dup 0> WHILE
	    2dup + 1- c@ format-chars + c@
	    current-format and
	    ?dup WHILE
		dup r@ and IF  r> xor $100 or  EXIT  THEN
		r> or >r  1-  REPEAT  THEN  r> ;
: ~current ( x -- )
    current-format xor $FF and to current-format ;
: format-text-rec ( addr u -- .. token )
    over { start }
    2dup + { end }
    false { success }
    >format-chars over 0> and ?dup-IF
	>r over >r start ?flush-text r> to last->in
	r> ~current  true to success  THEN
    <format-chars over 0> and ?dup-IF
	>r 2dup + ?flush-text end to last->in
	r> ~current  true to success  THEN
    2drop
    success IF  ['] noop rectype-nt  ELSE  rectype-null  THEN ;

depth >r
' text-rec  ' format-text-rec  ' vote-rec  ' file-rec
' http-rec  ' chain-rec ' tag-rec   ' pk-rec
depth r> - recognizer-sequence: msg-smart-text

Defer msg-recognize
' msg-smart-text is msg-recognize

0 Value ```-state

: ``` ```-state IF
	['] msg-smart-text is msg-recognize
	0 to current-format
    ELSE
	['] text-rec is msg-recognize
	msg:#mono to current-format
    THEN
    ```-state 0= dup to ```-state ./mono-info ;

: parse-text ( addr u -- ) last# >r  action-of forth-recognize >r
    0 to last->in  !format-chars
    ['] msg-recognize is forth-recognize 2dup evaluate
    last->in IF  + last->in tuck -  THEN  dup IF
	\ ." text: '" forth:type ''' forth:emit forth:cr
	current-format ?dup-IF  ulit, $, msg-text+format
	ELSE  $, msg-text  THEN
    ELSE  2drop  THEN
    r> is forth-recognize  r> to last#
    msg:#mono ```-state and to current-format ;

: avalanche-text ( addr u -- )
    >utf8$ ['] parse-text send-avalanche ;

previous

: load-msgn ( addr u n -- )
    >r load-msg r> display-lastn ;

: +group ( -- ) msg-group$ $@ >group +unique-con ;

: msg-timeout ( -- )
    packets2 @  connected-timeout  packets2 @ <>
    IF  reply( ." Resend to " pubkey $@ key>nick type cr )
	timeout-expired? IF
	    timeout( <err> ." Excessive timeouts from "
	    pubkey $@ key>nick type ." : "
	    ack@ .timeouts @ . <default> cr )
	    ungroup-ctx \ ungroup before sending avalanches!
	    msg-group$ $@len IF
		msg-group-o ?dup-IF  .msg:mode dup @ msg:otr# or swap
		    [: pubkey $@ ['] left, send-avalanche ;] !wrapper
		THEN
	    THEN
	    net2o:dispose-context
	    EXIT
	THEN
    ELSE  expected@ u<= IF  -timeout  THEN  THEN ;

: +resend-msg ( -- )
    ['] msg-timeout is timeout-xt  o+timeout ;

$B $E 2Value chat-bufs#

: +chat-control ( -- )
    +resend-msg +flow-control ;

: chat#-connect? ( addr u buf1 buf2 --- flag )
    pk-connect-dests? dup IF  connection >o rdrop +chat-control  +group  THEN ;

: chat-connect ( addr u -- )
    chat-bufs# chat#-connect? IF  greet fetch-pks  THEN ;

: key-ctrlbit ( -- n )
    \G return a bit mask for the control key pressed
    1 key dup bl < >r lshift r> and ;

: wait-key ( -- )
    BEGIN  key-ctrlbit [ 1 ctrl L lshift 1 ctrl Z lshift or ]L
    and 0=  UNTIL ;

: chats# ( -- n )
    0 [: msg:peers[] $[]# 1 min + ;] group#map ;
: chat-keys# ( -- n )
    0 chat-keys [: @/2 nip 0<> - ;] $[]map ;

: wait-chat ( -- )
    chat-keys [: @/2 dup 0= IF  2drop  EXIT  THEN
      2dup keysize2 safe/string tuck <info> type IF '.' emit  THEN
      .key-id space ;] $[]map
    ." is not online. press key to recheck."
    [: 0 to connection -56 throw ;] is do-disconnect
    [: false chat-keys [: @/2 key| pubkey $@ key| str= or ;] $[]map
	IF  bl inskey  THEN  up@ wait-task ! ;] is do-connect
    wait-key cr [: up@ wait-task ! ;] IS do-connect ;

: search-connect ( key u -- o/0 )  key|
    0 [: drop 2dup pubkey $@ key| str= o and
      o IF
	  context-table @ token-table @ = and
	  code-map 0<> and
      THEN
      dup 0= ;] search-context
    nip nip  dup to connection ;

: search-peer ( -- chat )
    false chat-keys
    [: @/2 key| rot dup 0= IF drop search-connect
      ELSE  nip nip  THEN ;] $[]map ;

: key>group ( addr u -- pk u )
    @/ 2swap tuck msg-group$ $!  0=
    IF  2dup key| msg-group$ $!  THEN ; \ 1:1 chat-group=key

: ?load-msgn ( -- )
    msg-group$ $@ >group msg-group-o .msg:log[] $@len 0= IF
	msg-group$ $@ rows load-msgn  THEN ;

: chat-connects ( -- )
    chat-keys [: key>group ?load-msgn
      dup 0= IF  2drop msg-group$ $@ >group  EXIT  THEN
      2dup search-connect ?dup-IF  >o +group greet o> 2drop EXIT  THEN
      2dup pk-peek?  IF  chat-connect  ELSE  2drop  THEN ;] $[]map ;

: ?wait-chat ( -- addr u ) #0. /chat:/chats
    BEGIN  chats# 0= chat-keys# 0> and WHILE  wait-chat chat-connects  REPEAT
    msg-group$ $@ ; \ stub

scope{ /chat
:noname ( addr u -- )
    chat-keys $[]free nick>chat 0 chat-keys $[]@ key>group
    msg-group$ $@ >group msg-group-o .msg:peers[] $@ dup 0= IF  2drop
	nip IF  chat-connects
	ELSE  ." That chat isn't active" forth:cr  THEN
    ELSE
	bounds ?DO  2dup I @ .pubkey $@ key2| str= 0= WHILE  cell +LOOP
	    2drop chat-connects  ELSE  UNLOOP 2drop THEN
    THEN  #0. /chats ; is /chat
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

: reconnects, ( o:group -- )
    msg-group-o .msg:peers[] $@ cell safe/string bounds U+DO
	I @ .reconnect,
    cell +LOOP ;

: send-reconnects ( o:group -- )
    net2o-code expect-msg
    [:  msg-group-o .msg:name$ ?destpk $, msg-leave
	<msg msg-start "left" $, msg-action msg-otr>
	reconnects, ;] [msg,]
    end-code| ;

: send-reconnect1 ( o:group -- )
    net2o-code expect-msg
    [:  msg:name$ ?destpk $, msg-leave
	<msg msg-start "left" $, msg-action msg-otr>
	.reconnect, ;] [msg,]
    end-code| ;
previous

: send-reconnect-xt ( o:group xt -- ) { xt: xt }
    msg:peers[] $@
    case
	0    of  drop  endof
	cell of  @ >o o to connection xt o>  endof
	drop @ >o o to connection  send-reconnects o>
	0
    endcase ;
: send-reconnect ( o:group -- )
    ['] send-leave send-reconnect-xt ;
: send-silent-reconnect ( o:group -- )
    ['] send-silent-leave send-reconnect-xt ;

: disconnect-group ( o:group -- )
    msg:peers[] get-stack 0 ?DO  >o o to connection
	disconnect-me o>
    LOOP ;
: disconnect-all ( o:group -- )
    msg:peers[] get-stack 0 ?DO  >o o to connection
	send-leave  disconnect-me o>
    LOOP ;

: leave-chat ( o:group -- )
    send-reconnect disconnect-group ;
: silent-leave-chat ( o:group -- )
    send-silent-reconnect disconnect-group ;

: leave-chats ( -- )
    ['] leave-chat group#map ;

: split-load ( o:group -- )
    msg:peers[] >r 0
    BEGIN  dup 1+ r@ $[]# u<  WHILE
	    dup r@ $[] 2@ .send-reconnect1
	    1+ dup r@ $[] @ >o o to connection disconnect-me o>
    REPEAT drop rdrop ;

scope{ /chat
:noname ( addr u -- )  2drop
    msg-group$ $@ >group msg-group-o .split-load ; is /split
}scope

\ chat toplevel

: do-chat ( addr u -- )
    status-xts get-stack n>r  get-order n>r
    chat-history  edit-curpos-off
    ['] /chat >wordlist 1 set-order
    msg-group$ $! chat-entry \ ['] cmd( >body on
    [: up@ wait-task ! ;] IS do-connect
    [: #0. /chat:/peers ;]
    [: #0. /chat:/stats ;]
    ['] .stacks
    ['] .order 4 status-xts set-stack
    BEGIN  .status get-input-line .unstatus
	2dup "/bye" str= >r 2dup "\\bye" str= r> or 0= WHILE
	    do-chat-cmd? 0= IF  avalanche-text  THEN
    REPEAT  2drop leave-chats  xchar-history
    nr> set-order nr> status-xts set-stack ;

: msg-name, ( -- )
    msg-group-o .msg:name$ 2dup pubkey $@ key| str=
    IF  2drop  ELSE  group,  THEN ;

: avalanche-to ( o:context -- )
    avalanche( ." Send avalanche to: " pubkey $@ key>nick type space over h. cr )
    o to connection
    push[] [: net2o-code expect-msg message msg-name, $, nestsig end-with
	end-code ;] $[]map ;

\\\
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     (("\\U") immediate (font-lock-comment-face . 1)
      "[\n]" nil comment (font-lock-comment-face . 1))
     ("[a-z\-0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
    )
End:
