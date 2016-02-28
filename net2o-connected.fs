\ net2o connected commands

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

\ everything that follows here can assume to have a connection context

scope{ net2o-base

connect-table $@ inherit-table context-table

\ generic functions
\g 
\g ### connection commands ###
\g 
+net2o: disconnect ( -- ) \g close connection
    o 0= ?EXIT
    wait-task @ IF  <event o elit, ->disconnect wait-task @ event>
    ELSE  n2o:dispose-context  THEN  un-cmd ;
+net2o: set-ip ( $:string -- ) \g set address information
    $> setip-xt perform ;
+net2o: get-ip ( -- ) \g request address information
    >sockaddr $, set-ip [: $, set-ip ;] n2oaddrs ;

+net2o: set-blocksize ( n -- ) \g set blocksize to 2^n
    64>n 1 swap max-block# umin lshift blocksizes! ;
+net2o: set-blockalign ( n -- ) \g set block alignment to 2^n
    64>n 1 swap max-block# umin lshift blockalign ! ;
+net2o: close-all ( -- ) \g close all files
    n2o:close-all ;
\ better slurping

+net2o: set-top ( utop flag -- ) \g set top, flag is true when all data is sent
    >r 64>n r> data-rmap @ >o over dest-top @ <> and dest-end or! dest-top! o> ;
+net2o: slurp ( -- ) \g slurp in tracked files
    n2o:slurp swap ulit, flag, set-top
    ['] do-track-seek n2o:track-all-seeks net2o:send-chunks ;

\ object handles
\g 
\g ### file commands ###
\g 

$30 net2o: file-id ( uid -- o:file ) \g choose a file object
    64>n state-addr n:>o ;
fs-table >table

reply-table $@ inherit-table fs-table
:noname fs-id @ ulit, file-id ; fs-class to start-req
$20 net2o: open-file ( $:string mode -- ) \g open file with mode
    parent @ .perm-mask @ >r
    r@ perm%filename and 0= !!filename-perm!!
    64>n -2 and 4 umin dup r> ?rw-perm  >r $> r> fs-open ;
+net2o: file-type ( n -- ) \g choose file type
    fs-class! ;
+net2o: close-file ( -- ) \g close file
    fs-close ;
+net2o: set-size ( size -- ) \g set size attribute of current file
    track( ." file <" fs-id @ 0 .r ." > size: " 64dup 64. forth:cr ) size! ;
+net2o: set-seek ( useek -- ) \g set seek attribute of current file
    track( ." file <" fs-id @ 0 .r ." > seek: " 64dup 64. forth:cr ) seekto! ;
+net2o: set-limit ( ulimit -- ) \g set limit attribute of current file
    track( ." file <" fs-id @ 0 .r ." > seek to: " 64dup 64. forth:cr ) limit-min! ;
+net2o: set-stat ( umtime umod -- ) \g set time and mode of current file
    64>n n2o:set-stat ;
+net2o: get-size ( -- ) \g requuest file size
    fs-size 64@ lit, set-size ;
+net2o: get-stat ( -- ) \g request stat of current file
    n2o:get-stat >r lit, r> ulit, set-stat ;
+net2o: set-form ( w h -- ) \g if file is a terminal, set size
    term-h ! term-w ! ;
+net2o: get-form ( -- ) \g if file is a terminal, request size
    term-w @ ulit, term-h @ ulit, set-form ;
+net2o: poll-request ( ulimit -- ) \g poll a file to check for size changes
    poll! lit, set-size ;

gen-table $freeze
' context-table is gen-table

:noname ( uid useek -- ) 64>r ulit, file-id
    64r> lit, set-seek endwith ; is do-track-seek

\ flow control functions
\g 
\g ### ack commands ###
\g 

$31 net2o: ack ( -- o:acko ) \g ack object
    ack@ n:>o ;
ack-table >table
reply-table $@ inherit-table ack-table

:noname ack ; ack-class to start-req
$20 net2o: ack-addrtime ( utime addr -- ) \g packet at addr received at time
    net2o:ack-addrtime ;
+net2o: ack-resend ( flag -- ) \g set resend toggle flag
    64>n  parent @ .net2o:ack-resend ;
+net2o: set-rate ( urate udelta-t -- ) \g set rate 
    parent @ >o cookie? IF  ack@ .net2o:set-rate
    ELSE  64drop 64drop ack@ .ns/burst dup >r 64@ 64-2* 64-2* r> 64!  THEN o> ;
+net2o: resend-mask ( addr umask -- ) \g resend mask blocks starting at addr
    2*64>n parent @ >o net2o:resend-mask net2o:send-chunks o> ;
+net2o: track-timing ( -- ) \g track timing
    net2o:track-timing ;
+net2o: rec-timing ( $:string -- ) \g recorded timing
    $> net2o:rec-timing ;
+net2o: send-timing ( -- ) \g request recorded timing
    net2o:timing$ maxtiming umin tuck $, net2o:/timing rec-timing ;
+net2o: ack-b2btime ( utime addr -- ) \g burst-to-burst time at packet addr
    net2o:ack-b2btime ;
+net2o: ack-resend# ( addr $:string -- ) \g resend numbers
    64>n $> parent @ .data-map @ .resend#? dup 0= IF
	drop ." resend# don't match!" forth:cr
	parent @ .n2o:see-me
	[ cookie-val $FF xor ]L validated and!
    ELSE
	8 lshift validated +! cookie-val validated or!
    THEN ;
+net2o: ack-flush ( addr -- ) \g flushed to addr
    64>n parent @ .net2o:rewind-sender-partial ;
+net2o: set-head ( addr -- ) \g set head
    64>n parent @ .data-rmap @ .dest-head umax! ;
+net2o: timeout ( uticks -- ) \g timeout request
    parent @ >o net2o:timeout  data-map @ .dest-tail @ o> ulit, set-head ;
+net2o: set-rtdelay ( ticks -- ) \g set round trip delay only
    rtdelay! ;

\ profiling, nat traversal

gen-table $freeze
' context-table is gen-table

: net2o:gen-resend ( -- )
    recv-flag @ invert resend-toggle# and ulit, ack-resend ;
: net2o:ackflush ( n -- ) ulit, ack-flush ;
: n2o:done ( -- ) request( ." n2o:done request" forth:cr )
    slurp next-request filereq# ! ;

: rewind ( -- )
    data-rmap @ >o dest-back @ do-slurp @ umax o> net2o:ackflush ;

\ safe initialization

: lit<   lit, push-lit ;
: slit<  slit, push-slit ;
:noname ( throwcode -- )
    remote? @ IF
	dup  IF  dup nlit, ko end-cmd
	    ['] end-cmd IS expect-reply? (end-code)  THEN
    THEN  throw ; IS >throw

set-current

: blocksize! ( n -- )  max-block# umin dup ulit, set-blocksize
    1 swap lshift blocksizes! ;
: blockalign! ( n -- ) max-block# umin dup ulit, set-blockalign
    1 swap lshift blockalign ! ;

: open-tracked-file ( addr u mode --)
    open-file get-size get-stat ;

: n2o>file ( xt -- )
    file-reg# @ ulit, file-id  catch  endwith
    throw  1 file-reg# +! ;

: n2o:copy ( addrsrc us addrdest ud -- )
    [: 2swap $, r/o ulit, open-tracked-file
      file-reg# @ save-to ;] n2o>file ;

: seek! ( pos id -- ) >r d>64
    64dup r@ state-addr .fs-seek 64!
    r> ulit, file-id lit, set-seek endwith ;

: limit! ( pos id -- ) >r d>64
    r@ ulit, file-id 64dup lit, set-limit endwith
    r> init-limit! ;

file-reg# off

previous

\ client side timing

: ack-size ( -- )  1 acks +!
    ack@ .recv-tick 64@ 64dup lastb-ticks 64!@ 64- max-dticks 64max! ;
: ack-first ( -- )
    lastb-ticks 64@ firstb-ticks 64@ 64- delta-ticks 64+!
    ack@ .recv-tick 64@ 64dup firstb-ticks 64!  64dup lastb-ticks 64!
    last-rtick 64!  recv-addr 64@ last-raddr 64! ;

: ack-timing ( n -- )
    b2b-toggle# and  IF  ack-first  ELSE  ack-size  THEN ;

also net2o-base

: setrate-limit ( rate -- rate' )
    \ do not change requested rate by more than a factor 2
    last-rate 64@
    64dup 64-0<> IF  64tuck 64-2* 64min 64swap 64-2/ 64max  ELSE  64drop  THEN
    64dup last-rate 64! ;

: >rate ( -- )  delta-ticks 64@ 64-0= acks @ 0= or ?EXIT
    ack@ .recv-tick 64@ 64dup burst-ticks 64!@ 64dup 64-0<> IF
	64- max-dticks 64@ tick-init 1+ n>64 64* 64max 64>r
	delta-ticks 64@ tick-init 1+ acks @ 64*/ setrate-limit
	lit, 64r> lit, set-rate
    ELSE
	64drop 64drop
    THEN
    delta-ticks 64off  max-dticks 64off  acks off ;

: net2o:acktime ( -- )
    recv-addr 64@ ack@ .recv-tick 64@ ack@ .time-offset 64@ 64-
    timing( 64>r 64dup $64. 64r> 64dup 64. ." acktime" forth:cr )
    lit, lit, ack-addrtime ;
: net2o:b2btime ( -- )
    last-raddr 64@ last-rtick 64@ 64dup 64-0=
    IF  64drop 64drop
    ELSE  ack@ .time-offset 64@ 64- lit, lit, ack-b2btime  THEN ;

\ ack bits, new code

: net2o:ack-resend# ( -- )  data-rmap @ { map }
    map .data-resend#-buf $@
    bounds ?DO
	I $@ over @ >r cell /string $FF -skip
	dup >r $FF skip r> over - r> + ulit, $, ack-resend#
    cell +LOOP
    map .data-resend#-buf $[]off ;

\ client side acknowledge

: net2o:genack ( -- )
    net2o:ack-resend#  net2o:b2btime  net2o:acktime  >rate ;

: !rdata-tail ( -- )
    data-rmap @ >o
    data-ack# @ bytes>addr dest-top 2@ umin umin
    dest-tail @ umax dup dest-tail !@ o>
    u> IF  net2o:save& 64#0 burst-ticks 64!  THEN ;
: receive-flag ( -- flag )  recv-flag @ resend-toggle# and 0<> ;

2 Value max-resend#

: prepare-resend ( flag -- end start acks ackm taibits )
    data-rmap @ >o
    IF    dest-head @ addr>bits bits>bytes -4 and
    ELSE  dest-head @ 1- addr>bits bits>bytes 1+  THEN 0 max
    dest-tail @ addr>bytes -4 and dup data-ack# umin!
    data-ackbits @ dest-size @ addr>bytes 1-
    dest-tail @ addr>bits o> ;

: net2o:do-resend ( flag -- )
    o 0= IF  drop EXIT  THEN  data-rmap @ 0= IF  drop EXIT  THEN
    0 swap  prepare-resend { acks ackm tailbits }
    +DO
	acks I ackm and + l@
	I bytes>bits tailbits u< IF
	    -1 tailbits I bytes>bits - lshift invert or
	THEN
	dup $FFFFFFFF <> IF
	    resend( ." resend: " dup hex. over hex. forth:cr )
	    I ackm and bytes>addr ulit, $FFFFFFFF xor ulit, resend-mask  1+
	ELSE
	    drop dup 0= IF  I 4 + data-rmap @ .data-ack# !  THEN
	THEN
	dup max-resend# >= ?LEAVE \ no more than x resends
    4 +LOOP  drop !rdata-tail ;

: do-expect-reply ( -- )
    cmdbuf# @ 0> IF \ there's actuall something in the buffer
	reply-index ulit, ok?  end-cmd
	net2o:expect-reply  maxdata code+ \ don't reuse this buffer
    THEN  ['] end-cmd IS expect-reply? ;

: expect-reply-xt ( xt -- ) \ cmd( ." expect reply:" forth:cr )
    ['] do-expect-reply IS expect-reply?
    cmd-reply-xt ! ;

: expect-reply ( -- )
    ['] drop expect-reply-xt ;

: resend-all ( -- )
    ticker 64@ resend-all-to 64@ 64u>= IF
	false net2o:do-resend
	+timeouts resend-all-to 64!
    THEN ;

0 Value request-stats?

: update-rtdelay ( -- )
    ticks lit, push-lit push' set-rtdelay ;

: data-end? ( -- flag )
    0 data-rmap @ .dest-end !@ ;

: rewind-transfer ( -- flag )
    rewind data-end? IF  filereq# @ n2o:request-done  false
    ELSE  true  THEN ;

: request-stats   forth:true to request-stats?  ack track-timing endwith ;

: expected@ ( -- head top )
    o IF  data-rmap @ >o
	o IF  dest-tail @ dest-top @  ELSE  0.  THEN o>
    ELSE  0.  THEN  ;

: expected? ( -- flag )
    expected@ tuck u>= and IF
	expect-reply
	msg( ." check: " data-rmap @ >o dest-back @ hex. dest-tail @ hex. dest-head @ hex.
	data-ackbits @ data-ack# @ dup hex. + l@ hex.
	o> forth:cr ." Block transfer done: " expected@ hex. hex. forth:cr )
	net2o:ack-resend#  rewind-transfer
	64#0 burst-ticks 64!
    ELSE  false  THEN ;

cell 8 = [IF] 6 [ELSE] 5 [THEN] Constant cell>>

Create no-resend# bursts# 4 * 0 [DO] -1 c, [LOOP]

: +resend# ( bit -- ) >r
    dest-addr 64@ 64>n [ min-size 1- ]L and
    r@ [ bursts# 4 * 1- ]L and
    r> [ bursts# -4 * ]L and \ one block per burst
    data-resend#-buf $[]# 0 ?DO
	dup I data-resend#-buf $[]@ drop @ = IF
	    drop I data-resend#-buf $[]@ drop cell+ + c!
	    UNLOOP  EXIT  THEN
    LOOP
    data-resend#-buf $[]# { w^ burstblock n }
    burstblock cell data-resend#-buf $+[]!
    no-resend# [ bursts# 4 * ]L n data-resend#-buf $[]+!
    n data-resend#-buf $[]@ drop cell+ + c! ;

: +cookie ( -- )
    data-rmap @ >o  ack-bit# @ >r  r@ +resend#
    data-ackbits @ r> +bit@
    o> negate packetr2 +! ;

: +expected ( -- flag )
    data-rmap @ >o dest-head @ dest-top @ u>= ack-advance? @ and o>
    IF   resend-all  THEN  expected? ;

\ higher level functions

: map-request, ( ucode udata -- )
    2dup + n2o:new-map lit, swap ulit, ulit,
    map-request ;

: gen-request ( -- ) setup!
    cmd( ind-addr @ IF  ." in" THEN ." direct connect" forth:cr )
    net2o-code0
    ['] end-cmd IS expect-reply?
    net2o-version $, get-version
    tpkc keysize $, receive-tmpkey
    nest[ no-cookie-xt cookie,
    request( ." gen reply" forth:cr )
	gen-reply request,
    ]nest  other
    tmpkey-request
    pubkey @ 0= IF  key-request  THEN
    ind-addr @  IF  punch?  THEN
    req-codesize @  req-datasize @  map-request,
    ['] push-cmd IS expect-reply?
    end-code ;

also net2o-base
: nat-punch ( o:connection -- )
    ind-addr @ IF  pings new-request false gen-punchload gen-punch  THEN ;
previous

:noname ( addr u -- )
    outflag @ >r  cmdbuf-o @ >r
    [: cmd0! cmdreset also net2o-base
      [ also net2o-base ]
      ['] end-cmd IS expect-reply?
      $, nest end-code ;] catch
    r> cmdbuf-o !  r> outflag !  throw
; is punch-reply

: 0-resend? ( -- n )
    resend0 @ IF
	\ ." Resend to 0" cr
	cmd0!
	[:
	  resend( ." resend0: " resend0 $@ n2o:see forth:cr )
	  msg( ." resend0: " resend0 $@ swap hex. hex forth:cr )
	  cmdreset resend0 $@ +cmdbuf
	  r0-address return-addr $10 move
	  cmdbuf$ rng64 send-cmd drop
	  1 packets2 +! ;]
	cmdlock c-section  1
    ELSE  0  THEN ;

: map-resend? ( -- n )
    code-map @ ?dup-IF  >o 0  outflag off
	dest-replies @
	dest-size @ addr>replies bounds o> U+DO
	    I reply-xt @ IF
		resend( ." resend: " I reply-dest 64@ $64. I 2@ n2o:see forth:cr )
		msg( ." resend: " I reply-dest 64@ $64. I 2@ swap hex. hex. forth:cr )
		ticks I reply-time 64!
		I 2@ I reply-dest 64@ send-cmd drop
		1 packets2 +! 1+
	    THEN
	reply +LOOP
    ELSE  0  THEN ;

: cmd-resend? ( -- n )
    0-resend? map-resend? + ;

: .expected ( -- )
    forth:.time ." expected/received: " recv-addr @ hex.
    data-rmap @ .data-ack# @ hex.
    expected@ hex. hex. forth:cr ;

\ acknowledge toplevel

: net2o:ack-code ( ackflag -- ackflag' )
    false dup { slurp? stats? }
    net2o-code  ack expect-reply \ ['] end-cmd IS expect-reply?
    dup ack-receive !@ xor >r
    r@ ack-toggle# and IF
	net2o:gen-resend  net2o:genack
	r@ resend-toggle# and IF
	    true net2o:do-resend
	THEN
	0 data-rmap @ .do-slurp !@
	?dup-IF  net2o:ackflush
	    request-stats? to stats?  true to slurp?  THEN
    THEN  +expected slurp? or to slurp?
    endwith  cmdbuf# @ 2 = IF  cmdbuf# off  THEN
    slurp? IF  slurp  THEN
    stats? IF  ack send-timing endwith  THEN
    end-code r> ( dup ack-toggle# and IF  map-resend?  THEN ) ;

: net2o:do-ack ( -- )
    dest-addr 64@ recv-addr 64! \ last received packet
    +cookie
    inbuf 1+ c@ dup recv-flag ! \ last receive flag
    acks# and data-rmap @ .ack-advance? @
    IF  net2o:ack-code  ELSE  ack-receive @ xor  THEN  ack-timing
    ack( ." ack expected: " recv-addr 64@ $64. expected@ hex. hex. forth:cr )
;

: +flow-control ['] net2o:do-ack ack-xt ! ;

\ keepalive

also net2o-base
: .keepalive ( -- )  ." transfer keepalive " expected@ hex. hex.
    data-rmap @ >o dest-tail @ hex. dest-back @ hex. o>
    forth:cr ;
: transfer-keepalive? ( -- )
    o to connection
    timeout( .keepalive )
    rewind-transfer 0= IF  .keepalive  EXIT  THEN
    expected@ tuck u>= and IF  net2o-code
	  ack +expected endwith IF  slurp  THEN  end-code  EXIT  THEN
\    net2o-code  expect-reply
\      ack net2o:genack
\      resend-all ticks lit, timeout rewind update-rtdelay
\      endwith slurp
\    end-code
;
previous

: cmd-timeout ( -- )  >next-timeout cmd-resend?
    IF  timeout( ." resend commands" forth:cr )
	push-timeout
    ELSE  ack@ .timeouts off  THEN ;
: connected-timeout ( -- ) timeout( ." connected timeout" forth:cr )
    \ timeout( .expected )
    packets2 @ cmd-timeout packets2 @ =
    IF  transfer-keepalive?  THEN ;

\ : +connecting   ['] connecting-timeout timeout-xt ! ;
: +resend       ['] connected-timeout  timeout-xt ! o+timeout ;
: +resend-cmd   ['] cmd-timeout        timeout-xt ! o+timeout ;

: +get-time     ['] get-tick is other ;

: reqsize! ( ucode udata -- )  req-datasize !  req-codesize ! ;
: tail-connect ( -- )   +resend-cmd client-loop
    -timeout tskc KEYBYTES erase ( resend0 $off ) context! ;

: n2o:connect ( ucode udata -- )
    reqsize!  gen-tmpkeys  gen-request  tail-connect ;

: end-code| ( -- )  ]] end-code client-loop [[ ; immediate compile-only

previous

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