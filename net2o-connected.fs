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
    o IF
	wait-task @ ?dup-IF
	    <event o elit, :>dispose-context event>
	ELSE  n2o:dispose-context  THEN
	un-cmd  THEN ;
+net2o: set-ip ( $:string -- ) \g set address information
    $> setip-xt ;
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
    >r 64>n r> data-rmap with mapc
    over dest-top <> and dest-end or! dest-top!
    endwith ;
+net2o: slurp ( -- ) \g slurp in tracked files
    n2o:slurp swap ulit, flag, set-top
    ['] do-track-seek n2o:track-all-seeks net2o:send-chunks ;
+net2o: ack-reset ( -- ) \g reset ack state
    0 ack-state c! ;

\ object handles
\g 
\g ### file commands ###
\g 

$30 net2o: file-id ( uid -- o:file ) \g choose a file object
    64>n state-addr n:>o ;
fs-table >table

reply-table $@ inherit-table fs-table

: file-start-req fs-id @ ulit, file-id ;
' file-start-req
file-classes file-classes# cells bounds
[DO] dup [I] @ to start-req cell [+LOOP]
drop

$20 net2o: open-file ( $:string mode -- ) \g open file with mode
    parent .perm-mask @ >r r@ fs-perm?
    64>n -2 and 4 umin dup r> ?rw-perm  >r $> r> fs-open ;
+net2o: file-type ( n -- ) \g choose file type
    64>n fs-class! ;
+net2o: close-file ( -- ) \g close file
    fs-close ;
+net2o: set-size ( size -- ) \g set size attribute of current file
    track( ." file <" fs-id @ 0 .r ." > size: " 64dup u64. forth:cr ) size! ;
+net2o: set-seek ( useek -- ) \g set seek attribute of current file
    track( ." file <" fs-id @ 0 .r ." > seek: " 64dup u64. forth:cr ) seekto! ;
+net2o: set-limit ( ulimit -- ) \g set limit attribute of current file
    track( ." file <" fs-id @ 0 .r ." > seek to: " 64dup u64. forth:cr ) limit-min! ;
+net2o: set-stat ( umtime umod -- ) \g set time and mode of current file
    64>n fs-set-stat ;
+net2o: get-size ( -- ) \g request file size
    fs-size 64@ lit, set-size ;
+net2o: get-stat ( -- ) \g request stat of current file
    fs-get-stat >r lit, r> ulit, set-stat ;
+net2o: set-form ( w h -- ) \g if file is a terminal, set size
    term-h ! term-w ! ;
+net2o: get-form ( -- ) \g if file is a terminal, request size
    term-w @ ulit, term-h @ ulit, set-form ;
+net2o: poll-request ( ulimit -- ) \g poll a file to check for size changes
    poll! lit, set-size ;

gen-table $freeze
' context-table is gen-table

:noname ( uid useek -- ) 64>r ulit, file-id
    64r> lit, set-seek end-with ; is do-track-seek

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
    64>n  parent .net2o:ack-resend ;
+net2o: set-rate ( urate udelta-t -- ) \g set rate 
    parent >o cookie? IF  ack@ .net2o:set-rate
    ELSE  64drop 64drop ack-order?
	IF  ack@ .ns/burst dup >r 64@ 64-2* 64-2* r> 64!
	THEN
    THEN o> ;
+net2o: resend-mask ( addr umask -- ) \g resend mask blocks starting at addr
    2*64>n parent >o net2o:resend-mask net2o:send-chunks o> ;
+net2o: track-timing ( -- ) \g track timing
    net2o:track-timing ;
+net2o: rec-timing ( $:string -- ) \g recorded timing
    $> net2o:rec-timing ;
+net2o: send-timing ( -- ) \g request recorded timing
    net2o:timing$ maxtiming umin tuck $, net2o:/timing rec-timing ;
+net2o: ack-b2btime ( utime addr -- ) \g burst-to-burst time at packet addr
    net2o:ack-b2btime ;
+net2o: ack-resend# ( addr $:string -- ) \g resend numbers
    64>n $>
    ack-order? IF
	parent .data-map .mapc:resend#? dup 0= IF
	    drop timeout( ." resend# don't match!" forth:cr
	    parent .n2o:see-me )
	    [ cookie-val 1 validated# lshift 1- xor ]L validated and!
	ELSE
	    validated# lshift validated +! cookie-val validated or!
	THEN
    ELSE  timeout( ." out of order arrival of ack" forth:cr )
	2drop drop
    THEN ;
+net2o: ack-flush ( addr -- ) \g flushed to addr
    64>n parent .net2o:rewind-sender-partial ;
+net2o: set-head ( addr -- ) \g set head
    64>n parent .data-rmap >o addr mapc:dest-head o> umax! ;
+net2o: timeout ( uticks -- ) \g timeout request
    parent >o net2o:timeout  data-map .mapc:dest-tail o> ulit, set-head ;
+net2o: set-rtdelay ( ticks -- ) \g set round trip delay only
    rtdelay! ;
+net2o: seq# ( n -- ) \g set the ack number and check for smaller
    64>n parent .data-map with mapc
    dup send-ack# u> IF  to send-ack#  ack-order-val validated or!  ELSE
	drop [ ack-order-val invert ]L validated and!
    THEN
    endwith ;

\ profiling, nat traversal

gen-table $freeze
' context-table is gen-table

: net2o:gen-resend ( -- )
    ack-receive invert resend-toggle# and ulit, ack-resend ;
: net2o:gen-reset ( -- )
    ack-reset 0 to ack-receive ;

: slurp@ ( -- addr )
    data-rmap with mapc dest-back do-slurp @ umax endwith ;

: rewind ( -- )
    slurp@ ulit, ack-flush ;

\ safe initialization

: lit<   lit, push-lit ;
: slit<  slit, push-slit ;
:noname ( throwcode -- )
    remote? @ IF
	?dup-IF  init-reply
	    $error-id $@ dup IF  $, error-id  ELSE  2drop  THEN
	    nlit, ko  THEN
    ELSE
	error-id>o ?dup-IF
	    >o <event elit, :>throw wait-task @ event> o>
	ELSE  throw  THEN
    THEN ; IS >throw

also }scope

: blocksize! ( n -- )  max-block# umin dup ulit, set-blocksize
    1 swap lshift blocksizes! ;
: blockalign! ( n -- ) max-block# umin dup ulit, set-blockalign
    1 swap lshift blockalign ! ;

: open-sized-file ( addr u mode --)
    open-file get-size ;
: open-tracked-file ( addr u mode --)
    open-sized-file get-stat ;

: n2o>file ( xt -- )
    file-reg# @ ulit, file-id  catch  end-with
    throw  1 file-reg# +! ;

: n2o:copy ( addrsrc us addrdest ud -- )
    [: file( ." copy '" 2over forth:type ." ' -> '" 2dup forth:type
      ." '" forth:cr )
      2swap $, r/o ulit, open-tracked-file
      file-reg# @ save-to ;] n2o>file
    1 file-count +! ;

: n2o:copy# ( addrhash u -- )
    [: file( ." copy# " 2dup 85type forth:cr )
      1 ulit, file-type 2dup $, r/o ulit, open-tracked-file
      file-reg# @ save-to# ;] n2o>file
    1 file-count +! ;

: seek! ( pos id -- ) >r d>64
    64dup r@ state-addr .fs-seek 64!
    r> ulit, file-id lit, set-seek end-with ;

: limit! ( pos id -- ) >r d>64
    r@ ulit, file-id 64dup lit, set-limit end-with
    r> init-limit! ;

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
    timing( 64>r 64dup x64. 64r> 64dup u64. ." acktime" forth:cr )
    lit, lit, ack-addrtime ;
: net2o:b2btime ( -- )
    last-raddr 64@ last-rtick 64@ 64dup 64-0=
    IF  64drop 64drop
    ELSE  ack@ .time-offset 64@ 64- lit, lit, ack-b2btime  THEN ;

\ ack bits, new code

: net2o:ack-resend# ( -- )  data-rmap { map }
    map .mapc:data-resend#-buf $@
    bounds ?DO
	I $@ over @ >r cell /string $FF -skip
	dup >r $FF skip dup IF
	    r> over - r> + ulit, $, ack-resend#
	ELSE  2drop rdrop rdrop  THEN
    cell +LOOP
    map .mapc:data-resend#-buf $[]off ;

\ client side acknowledge

: net2o:genack ( -- )
    net2o:ack-resend#  net2o:b2btime  net2o:acktime  >rate ;

: !rdata-tail ( -- )
    data-rmap with mapc
    data-ack# @ bytes>addr
    dest-head umin dest-top umin
    dest-tail umax dup addr dest-tail !@ endwith
    ack( ." tail: " over hex. dup hex. forth:cr )
    u> IF  net2o:save& 64#0 burst-ticks 64!  THEN ;

$20 Value max-resend#

: prepare-resend ( flag -- end start acks ackm taibits )
    data-rmap with mapc
    ack( ." head/tail: " dup forth:. dest-head hex. dest-tail hex. forth:cr )
    IF    dest-head addr>bytes -4 and
    ELSE  dest-head 1- addr>bytes 1+  THEN 0 max
    dest-tail addr>bytes -4 and \ dup data-ack# umin!
    data-ackbits @ dest-size addr>bytes 1-
    dest-tail addr>bits endwith ;

: net2o:do-resend ( flag -- )
    o 0= IF  drop EXIT  THEN  data-rmap 0= IF  drop EXIT  THEN
    0 swap  prepare-resend { acks ackm tailbits }
    ack( ." ack loop: " over hex. dup hex. forth:cr )
    +DO
	acks I ackm and + l@
	acks( ." acks[" I ackm and 0 .r ." ]=" dup hex. forth:cr )
	I bytes>bits tailbits u< IF
	    -1 tailbits I bytes>bits - lshift invert or
	THEN
	dup $FFFFFFFF <> IF
	    resend( ." resend: " dup hex. over hex. forth:cr )
	    I ackm and bytes>addr ulit, $FFFFFFFF xor ulit, resend-mask  1+
	ELSE
	    drop dup 0= IF \ if we didn't have a resend yet, increase data-ack#
		I 4 + data-rmap .mapc:data-ack# !
	    THEN
	THEN
	dup max-resend# >= ?LEAVE \ no more than x resends
    4 +LOOP  drop !rdata-tail ;

: do-expect-reply ( -- )
    cmdbuf# @ 0> IF \ there's actuall something in the buffer
	reply-index ulit, ok?  end-cmd
	net2o:expect-reply  maxdata code+ \ don't reuse this buffer
    THEN  ['] end-cmd IS expect-reply? ;

: do-expect+slurp ( -- )
    cmdbuf# @ 0> IF \ there's actuall something in the buffer
	slurp next-request filereq# !  true data-rmap >o to mapc:dest-req o>
	reply-index ulit, ok?  end-cmd
	net2o:expect-reply  maxdata code+ \ don't reuse this buffer
    THEN  ['] end-cmd IS expect-reply? ;

: expect-reply-xt ( xt -- ) \ cmd( ." expect reply:" forth:cr )
    ['] do-expect-reply IS expect-reply?
    cmd-reply-xt ! ;

: expect-reply ( -- )
    ['] drop expect-reply-xt ;

: expect+slurp-xt ( xt -- ) \ cmd( ." expect reply:" forth:cr )
    ['] do-expect+slurp IS expect-reply?
    cmd-reply-xt ! ;

: expect+slurp ( -- )
    ['] drop expect+slurp-xt ;

UValue rec-ack-pos#

: seq#, ( -- )
    cmdbuf# @ 1 = IF
	data-rmap .mapc:rec-ack# ulit, seq#
	cmdbuf# @ to rec-ack-pos#
    THEN ;

: resend-all ( -- )
    seq#,
    false net2o:do-resend
    ack@ .+timeouts resend-all-to 64! ;

0 Value request-stats?

: update-rtdelay ( -- )
    ticks lit, push-lit push' set-rtdelay ;

: data-end? ( -- flag )
    0 data-rmap .mapc:dest-end !@ ;

: expected@ ( -- head top )
    o IF  data-rmap with mapc
	o IF  dest-tail dest-top
	    msg( ." expected: " over hex. dup hex. forth:cr )
	ELSE  #0. msg( ." expected: no data-rmap" forth:cr )  THEN endwith
    ELSE  #0. msg( ." expected: no object" forth:cr )  THEN  ;

: rewind-transfer ( -- flag )
    data-end? IF  filereq# @ n2o:request-done  false
	data-rmap >o dup to mapc:dest-req o>
    ELSE  data-rmap .mapc:dest-req  THEN ;

: request-stats   forth:true to request-stats?  ack track-timing end-with ;

: expected? ( -- flag )
    expected@ u>= IF
	expect-reply
	msg( ." check: " data-rmap with mapc
	dest-back hex. dest-tail hex. dest-head hex.
	data-ackbits @ data-ack# @ dup hex. + l@ hex.
	endwith
	forth:cr ." Block transfer done: " expected@ hex. hex. forth:cr )
	seq#,
	net2o:save&done  net2o:ack-resend#  rewind  rewind-transfer
	64#0 burst-ticks 64!
    ELSE  false  THEN ;

cell 8 = [IF] 6 [ELSE] 5 [THEN] Constant cell>>

Create no-resend# bursts# 4 * 0 [DO] -1 c, [LOOP]

scope{ mapc

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

}scope

: +cookie ( -- )
    data-rmap with mapc  ack-bit# @ >r  r@ +resend#
    data-ackbits @ r> +bit@
    endwith negate packetr2 +! ;

: resend-all? ( -- flag )
    data-rmap with mapc
    ack-advance?  dest-head dest-top u>=  and endwith
    ticker 64@ resend-all-to 64@ 64u>= and
    timeout( dup IF  ." resend all" forth:cr  THEN ) ;

: +expected ( -- flag )
    resend-all?  IF   resend-all  THEN  expected? ;

\ higher level functions

: map-request, ( ucode udata -- )
    n2o:new-map lit, swap ulit, ulit, map-request ;

also net2o-base
: nat-punch ( o:connection -- )
    pings new-request false gen-punchload gen-punch ;
previous

: punch-reply ( addr u -- )
    outflag @ >r  cmdbuf-o @ >r
    [: cmd0! cmdreset init-reply also net2o-base
      [ also net2o-base ]
      $, nest end-code ;] catch
    r> cmdbuf-o !  r> outflag !  throw ;

: 0-resend? ( -- n )
    resend0 @ IF
	\ ." Resend to 0" cr
	cmd0!
	[:
	  resend( ." resend0: " resend0 $@ n2o:see forth:cr )
	  msg( ." resend0: " resend0 $@ swap hex. hex forth:cr )
	  cmdreset init-reply resend0 $@ +cmdbuf
	  r0-address return-addr $10 move
	  cmdbuf$ rng64 send-cmd drop
	  1 packets2 +! ;]
	cmdlock c-section  1
    ELSE  0  THEN ;

: map-resend? ( -- n )
    code-map ?dup-IF  with mapc 0  outflag off
	dest-replies
	dest-size addr>replies bounds endwith U+DO
	    I addr reply-xt @ IF
		resend( ." resend: " I reply-dest 64@ x64. I 2@ n2o:see forth:cr )
		msg( ." resend: " I reply-dest 64@ x64. I 2@ swap hex. hex. forth:cr )
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
    data-rmap .mapc:data-ack# @ hex.
    expected@ hex. hex. forth:cr ;

\ acknowledge toplevel

: net2o:ack-code ( ackflag -- ackflag )  >r
    false dup { slurp? stats? }
    net2o-code
    ack expect-reply  1 to rec-ack-pos#
    ack( ." ack: " r@ hex. forth:cr )
    r@ ack-toggle# and IF
	seq#,
	net2o:gen-resend  net2o:genack
	r@ resend-toggle# and IF
	    ack( ." ack: do-resend" forth:cr )
	    true net2o:do-resend
	THEN
	0 data-rmap .mapc:do-slurp !@
	?dup-IF  ulit, ack-flush
	    request-stats? to stats?  true to slurp?  THEN
    THEN  +expected
    slurp? or to slurp?
    stats? IF  send-timing  THEN
    end-with  cmdbuf# @ rec-ack-pos# 1+ stats? - = IF  cmdbuf# off
    ELSE  1 data-rmap with mapc +to rec-ack# endwith  THEN
    slurp? IF  slurp  THEN
    end-code r> ( dup ack-toggle# and IF  map-resend?  THEN ) ;

: net2o:do-ack-rest ( ackflag -- )
    dup resend-toggle# and IF
	cmd-resend? timeout( dup IF  ." resend " dup . cr THEN ) drop
    THEN
    acks# and data-rmap .mapc:ack-advance?
    IF  net2o:ack-code  THEN  ack-timing ;

: net2o:do-ack ( -- )
    dest-addr 64@ recv-addr 64!  +cookie \ last received packet
    inbuf 1+ c@ ack-receive over to ack-receive xor
    +timeout0 resend-all-to 64!
    net2o:do-ack-rest ;

: +flow-control ['] net2o:do-ack is ack-xt ;

\ keepalive

also net2o-base
: .keepalive ( -- )  ." transfer keepalive e/e h t b " expected@ hex. hex.
    data-rmap with mapc  dest-head hex. dest-tail hex. dest-back hex.
    data-ackbits @ dest-size addr>bytes dump
    endwith
    forth:cr ;
: transfer-keepalive? ( -- flag )
    o to connection
    timeout( .keepalive )
    data-rmap with mapc dest-req dup ack-advance? or to ack-advance? endwith
    dup IF
	!ticks ticker 64@ resend-all-to 64!
	[ ack-toggle# resend-toggle# or ]L net2o:do-ack-rest  THEN ;
previous

: cmd-timeout ( -- )  >next-timeout cmd-resend?
    IF  timeout( ." resend commands" forth:cr )
	push-timeout
    ELSE  ack@ .timeouts off  THEN ;
: connected-timeout ( -- ) timeout( ." connected timeout" forth:cr )
    >next-timeout cmd-resend?
    IF  timeout( ." resend commands" forth:cr )
	push-timeout
    ELSE
	transfer-keepalive? 0=
	IF  ack@ .timeouts off  ELSE  push-timeout  THEN
    THEN ;

\ : +connecting   ['] connecting-timeout is timeout-xt ;
: +resend       ['] connected-timeout  is timeout-xt o+timeout
    64#0 resend-all-to 64! ;
: +resend-cmd   ['] cmd-timeout        is timeout-xt o+timeout ;

: +get-time     ['] get-tick is other ;

: reqsize! ( ucode udata -- )  to req-datasize  to req-codesize ;
: connect-rest ( n -- )
    clean-request -timeout tskc KEYBYTES erase context! ;

: end-code| ( -- )  ]] end-code client-loop [[ ; immediate compile-only

: gen-request ( -- )
    setup!  +resend-cmd  gen-tmpkeys  ['] connect-rest rqd?
    cmd( ind-addr @ IF  ." in" THEN ." direct connect" forth:cr )
    ivs( ." gen request" forth:cr )
    net2o-code0
    net2o-version $, version?  0key,
    tpkc keysize $, receive-tmpkey
    nest[ cookie, gen-reply request, ]nest  other
    tmpkey-request
    ind-addr @  IF  punch?  THEN
    req-codesize  req-datasize  map-request,  close-tmpnest
    ['] push-cmd IS expect-reply?
    end-code| ;

: n2o:connect ( ucode udata -- )  reqsize! gen-request ;

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
