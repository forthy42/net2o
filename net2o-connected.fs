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

reply-table $@ inherit-table context-table

\ generic functions

$20 net2o: disconnect ( -- ) \ close connection
    o 0= ?EXIT n2o:dispose-context un-cmd ;
+net2o: set-ip ( $:string -- ) \ set address information
    $> setip-xt perform ;
+net2o: get-ip ( -- ) \ request address information
    >sockaddr $, set-ip [: $, set-ip ;] n2oaddrs ;

+net2o: set-blocksize ( n -- ) \ set blocksize
    64>n blocksize! ;
+net2o: set-blockalign ( n -- ) \ set block alignment
    64>n pow2?  blockalign ! ;
+net2o: close-all ( -- ) \ close all files
    n2o:close-all ;
\ better slurping

+net2o: set-top ( utop flag -- ) \ set top, flag is true when all data is sent
    >r 64>n r> data-rmap @ >o over dest-top @ <> and dest-end or! dest-top! o> ;
+net2o: slurp ( -- ) \ slurp in tracked files
    n2o:slurp swap ulit, flag, set-top
    ['] do-track-seek n2o:track-all-seeks net2o:send-chunks ;

\ object handles

$30 net2o: file-id ( uid -- o:file )
    64>n state-addr n:>o ;
fs-table >table

reply-table $@ inherit-table fs-table

:noname fs-id @ ulit, file-id ; fs-class to start-req
$20 net2o: open-file ( $:string mode -- ) \ open file with mode
    64>r $> 64r> fs-open ;
+net2o: file-type ( n -- ) \ choose file type
    fs-class! ;
+net2o: close-file ( -- ) \ close file
    fs-close ;
+net2o: set-size ( size -- ) \ set size attribute of current file
    track( ." file <" fs-id @ 0 .r ." > size: " 64dup 64. F cr ) size! ;
+net2o: set-seek ( useek -- ) \ set seek attribute of current file
    track( ." file <" fs-id @ 0 .r ." > seek: " 64dup 64. F cr ) seekto! ;
+net2o: set-limit ( ulimit -- ) \ set limit attribute of current file
    track( ." file <" fs-id @ 0 .r ." > seek to: " 64dup 64. F cr ) limit-min! ;
+net2o: set-stat ( umtime umod -- ) \ set time and mode of current file
    64>n n2o:set-stat ;
+net2o: get-size ( -- )
    fs-size 64@ lit, set-size ;
+net2o: get-stat ( -- ) \ request stat of current file
    n2o:get-stat >r lit, r> ulit, set-stat ;
+net2o: set-form ( w h -- ) \ if file is a terminal, set size
    term-h ! term-w ! ;
+net2o: get-form ( -- ) \ if file is a terminal, request size
    term-w @ ulit, term-h @ ulit, set-form ;
+net2o: poll-request ( ulimit -- ) \ poll a file to check for size changes
    poll! lit, set-size ;

gen-table $freeze
' context-table is gen-table

: blocksize! ( n -- )  dup ulit, set-blocksize blocksize! ;
: blockalign! ( n -- )  pow2? dup ulit, set-blockalign blockalign ! ;

:noname ( uid useek -- ) 64>r ulit, file-id
    64r> lit, set-seek endwith ; is do-track-seek

\ flow control functions

$31 net2o: ack ( -- o:acko )  ack@ n:>o ;
ack-table >table

reply-table $@ inherit-table ack-table

:noname ack ; ack-class to start-req
$20 net2o: ack-addrtime ( utime addr -- ) \ packet at addr received at time
    net2o:ack-addrtime ;
+net2o: ack-resend ( flag -- ) \ set resend toggle flag
    64>n  parent @ .net2o:ack-resend ;
+net2o: set-rate ( urate udelta-t -- ) \ set rate 
    parent @ >o cookie? IF  ack@ .net2o:set-rate
    ELSE  64drop 64drop ack@ .ns/burst dup >r 64@ 64-2* 64-2* r> 64!  THEN o> ;
+net2o: resend-mask ( addr umask -- ) \ resend mask blocks starting at addr
    2*64>n parent @ >o net2o:resend-mask net2o:send-chunks o> ;
+net2o: track-timing ( -- ) \ track timing
    net2o:track-timing ;
+net2o: rec-timing ( $:string -- ) \ recorded timing
    $> net2o:rec-timing ;
+net2o: send-timing ( -- ) \ request recorded timing
    net2o:timing$ maxtiming umin tuck $, net2o:/timing rec-timing ;
+net2o: ack-b2btime ( utime addr -- ) \ burst-to-burst time at packet addr
    net2o:ack-b2btime ;
+net2o: ack-cookies ( ucookie addr umask -- ) \ acknowledge cookie
    [IFUNDEF] 64bit 64>r 64>n 64r> [THEN]
    parent @ >o data-map @ cookie+ 64over 64over 64= 0= IF
	." cookies don't match! " 64over $64. 64dup $64. F cr
    THEN
    64= cookie-val and validated or! o> ;
+net2o: ack-resend# ( addr $:string -- )
    64>n $> parent @ .data-map @ .resend#? 0= IF
	." resend# don't match!" F cr
    ELSE
	cookie-val validated or!
    THEN ;
+net2o: ack-flush ( addr -- ) \ flushed to addr
    64>n parent @ .net2o:rewind-sender-partial ;
+net2o: set-head ( addr -- ) \ set head
    64>n parent @ .data-rmap @ .dest-head umax! ;
+net2o: timeout ( uticks -- ) \ timeout request
    parent @ >o net2o:timeout  data-map @ .dest-tail @ o> ulit, set-head ;
+net2o: set-rtdelay ( ticks -- ) \ set round trip delay only
    rtdelay! ;

\ profiling, nat traversal

gen-table $freeze
' context-table is gen-table

: net2o:gen-resend ( -- )
    recv-flag @ invert resend-toggle# and ulit, ack-resend ;
: net2o:ackflush ( n -- ) ulit, ack-flush ;
: n2o:done ( -- )  slurp next-request filereq# ! ;

: rewind ( -- )
    data-rmap @ >o dest-back @ do-slurp @ umax o> net2o:ackflush ;

\ safe initialization

: lit<   lit, push-lit ;
: slit<  slit, push-slit ;
:noname ( throwcode -- )
    connection @ .server? IF
	dup  IF  dup nlit, ko end-cmd
	    ['] end-cmd IS expect-reply? (end-code)  THEN
    THEN  throw ; IS >throw

set-current

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
    timing( 64>r 64dup $64. 64r> 64dup 64. ." acktime" F cr )
    lit, lit, ack-addrtime ;
: net2o:b2btime ( -- )
    last-raddr 64@ last-rtick 64@ 64dup 64-0=
    IF  64drop 64drop
    ELSE  ack@ .time-offset 64@ 64- lit, lit, ack-b2btime  THEN ;

\ ack bits, new code

: ack-cookie, ( map bits n -- ) [ 8 cells ]L * maxdata *
    2dup 2>r rot >r swap u>64 r> cookie+
    lit, 2r> ulit, ulit, ack-cookies ;

: net2o:ack-cookies ( -- )  data-rmap @ { map }
    map .data-ackbits-buf $@
    bounds ?DO
	\ map I 2@ ack-cookie,
	I 2 cells + 64@ lit,
	I 2@ [ 8 cells ]L * maxdata * ulit, ulit, ack-cookies
    [ 2 cells 64'+ ]L +LOOP
    map .data-ackbits-buf $off ;

: net2o:ack-resend# ( -- )  data-rmap @ { map }
    map .data-resend#-buf $@
    bounds ?DO
	I $@ over @ >r cell /string $FF -skip
	dup >r $FF skip r> over - r> + ulit, $, ack-resend#
    cell +LOOP
    map .data-resend#-buf $[]off ;

\ client side acknowledge

: net2o:genack ( -- )
    net2o:ack-cookies net2o:ack-resend#
    net2o:b2btime  net2o:acktime  >rate ;

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
	    resend( ." resend: " dup hex. over hex. F cr )
	    I ackm and bytes>addr ulit, $FFFFFFFF xor ulit, resend-mask  1+
	ELSE
	    drop dup 0= IF  I 4 + data-rmap @ .data-ack# !  THEN
	THEN
	dup max-resend# >= ?LEAVE \ no more than x resends
    4 +LOOP  drop !rdata-tail ;

: do-expect-reply ( -- )
    reply-index ulit, ok?  end-cmd  net2o:expect-reply
    msg( ." Expect reply" F cr )
    ['] end-cmd IS expect-reply? ;

: expect-reply ( -- ) cmd( ." expect reply:" F cr )
    ['] do-expect-reply IS expect-reply?  maxdata code+ ;

: resend-all ( -- )
    ticker 64@ resend-all-to 64@ u>= IF
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

: request-stats   F true to request-stats?  ack track-timing endwith ;

: expected@ ( -- head top )
    o IF  data-rmap @ >o
	o IF  dest-tail @ dest-top @  ELSE  0.  THEN o>
    ELSE  0.  THEN  ;

: expected? ( -- flag )
    expected@ tuck u>= and IF
	expect-reply
	msg( ." check: " data-rmap @ >o dest-back @ hex. dest-tail @ hex. dest-head @ hex.
	data-ackbits @ data-ack# @ dup hex. + l@ hex.
	o> F cr ." Block transfer done: " expected@ hex. hex. F cr )
	net2o:ack-cookies  rewind-transfer
	64#0 burst-ticks 64!
    ELSE  false  THEN ;

cell 8 = [IF] 6 [ELSE] 5 [THEN] Constant cell>>

: +ackbit ( bit -- ) 0. c:cookie { d^ new-ackbit 64^ new-cookie }
    dup  [ 8 cells 1- ]L and swap cell>> rshift
    data-ackbits-buf $@ bounds ?DO
	dup I @ = IF drop
	    cookie( ." cookie+ " I @ cell>> chunk-p2 + lshift hex. dup hex. new-cookie 64@ $64. )
	    I cell+ swap +bit
	    new-cookie 64@ I 2 cells + 64+!
	    cookie( I 2 cells + 64@ $64. I cell+ @ hex. F cr )
	    unloop EXIT  THEN
    [ 2 cells 64'+ ]L +LOOP
    cookie( ." cookie= " dup cell>> chunk-p2 + lshift hex. over hex. new-cookie 64@ $64. F cr )
    new-ackbit !
    new-ackbit cell+ swap +bit
    new-ackbit [ 2 cells 64'+ ]L data-ackbits-buf $+! ;

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
    data-ackbits @ r@ +bit@  dup 0= IF  r@ +ackbit  THEN  rdrop
    o> negate packetr2 +! ;

: +expected ( -- flag )
    data-rmap @ >o dest-head @ dest-top @ u>= ack-advance? @ and o>
    IF   resend-all  THEN  expected? ;

\ higher level functions

: map-request, ( ucode udata -- )
    2dup + n2o:new-map lit, swap ulit, ulit,
    map-request ;

: gen-request ( -- )  setup!
    cmd( ind-addr @ IF  ." in" THEN ." direct connect" F cr )
    net2o-code0
    ['] end-cmd IS expect-reply?
    gen-tmpkeys $, receive-tmpkey
    nest[ cookie, ind-addr @ IF  gen-punch-reply
    ELSE  gen-reply request,  THEN ]nest
    tmpkey-request
    dest-pubkey @ 0= IF  key-request  THEN
    ind-addr @  IF  punch?  THEN  other
    req-codesize @  req-datasize @  map-request,
    ['] push-cmd IS expect-reply?
    end-code ;

:noname ( addr u -- )
    cmd0! cmdreset also net2o-base
    [ also net2o-base ]
    ['] end-cmd IS expect-reply?
    $, nest end-code
; is punch-reply

: 0-resend? ( -- )
    resend0 @ IF
	\ ." Resend to 0" cr
	cmd0!
	[: resend0 $@ >r cmdbuf r@ move
	  r0-address return-addr $10 move
	  cmdbuf r> 64#0 send-cmd 1 packets2 +! ;]
	cmdlock c-section
    THEN ;

: map-resend? ( -- )
    code-map @ ?dup-IF  >o
	dest-replies @
	dest-size @ addr>replies bounds o> U+DO
	    I @ 0<> IF
		timeout( ." resend: " I 2@ n2o:see F cr )
		I 2@ I reply-dest 64@ send-cmd
		1 packets2 +!
	    THEN
	reply +LOOP
    THEN ;

: cmd-resend? ( -- )
    0-resend? map-resend? ;

: .expected ( -- )
    F .time ." expected/received: " recv-addr @ hex.
    data-rmap @ .data-ack# @ hex.
    expected@ hex. hex. F cr ;

\ acknowledge toplevel

: net2o:ack-code ( ackflag -- ackflag' )
    false dup { slurp? stats? }
    net2o-code  ack ['] end-cmd IS expect-reply?
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
    end-code r> dup ack-toggle# and IF  map-resend?  THEN ;

: net2o:do-ack ( -- )
    dest-addr 64@ recv-addr 64! \ last received packet
    ( recv-cookie ) +cookie
    inbuf 1+ c@ dup recv-flag ! \ last receive flag
    acks# and data-rmap @ .ack-advance? @
    IF  net2o:ack-code   ELSE  ack-receive @ xor  THEN  ack-timing
    ack( ." ack expected: " recv-addr 64@ $64. expected@ hex. hex. F cr )
;

: +flow-control ['] net2o:do-ack ack-xt ! ;
: -flow-control ['] noop         ack-xt ! ;

\ keepalive

also net2o-base
: .keepalive ( -- )  ." transfer keepalive " expected@ hex. hex.
    data-rmap @ >o dest-tail @ hex. dest-back @ hex. o>
    F cr ;
: transfer-keepalive? ( -- )  o to connection
    timeout( .keepalive )
    rewind-transfer 0= IF  .keepalive  EXIT  THEN
    expected@ tuck u>= and IF  net2o-code
	ack +expected endwith IF  slurp  THEN  end-code  EXIT  THEN
    net2o-code  expect-reply
    ack net2o:genack
       resend-all ticks lit, timeout rewind update-rtdelay
    endwith slurp
    end-code ;
previous

: connected-timeout ( -- ) timeout( ." connected timeout" F cr )
    \ timeout( .expected )
    1 ack@ .timeouts +! >next-timeout
    packets2 @ cmd-resend? packets2 @ = IF  transfer-keepalive?  THEN ;

\ : +connecting   ['] connecting-timeout timeout-xt ! ;
: +resend       ['] connected-timeout  timeout-xt ! o+timeout ;

: +get-time     ['] get-tick is other ;

: reqsize! ( ucode udata -- )  req-datasize !  req-codesize ! ;
: tail-connect ( -- )   +resend  client-loop
    -timeout tskc KEYBYTES erase resend0 $off  context! ;

: n2o:connect ( ucode udata -- )
    reqsize!  gen-request  tail-connect ;

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