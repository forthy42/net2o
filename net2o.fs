\ net2o protocol stack

\ Copyright (C) 2010-2015   Bernd Paysan

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

\ helper words

require version.fs
require err.fs

\ required tools

require forward.fs
require mini-oof2.fs
require user-object.fs
require struct-val.fs
require rec-scope.fs
require unix/socket.fs
require unix/mmap.fs
require unix/pthread.fs
require unix/filestat.fs
require tools.fs
require debugging.fs
require kregion.fs
require crypto-api.fs
require keccak.fs
require threefish.fs
keccak-o crypto-o !
require rng.fs
require ed25519-donna.fs
require hash-table.fs
require bdelta.fs

\ crypto selection

Create crypt-modes ' keccak-t , ' threefish-t ,
here crypt-modes - cell/ Constant crypts#

: >crypt ( n -- )
    crypts# 1- umin cells crypt-modes + perform @ crypto-o ! c:init ;
0 >crypt

\ values, configurable

$4 Value max-size^2 \ 1k, don't fragment by default
$12 Value max-data# \ 16MB data space
$0C Value max-code# \ 256k code space
$10 Value max-block# \ 64k maximum block size+alignment

\ constants, and depending values

$2A Constant overhead \ constant overhead
$40 Constant min-size
1 Value buffers#
min-size max-size^2 lshift Value maxdata ( -- n )
maxdata overhead + Value maxpacket
maxpacket $F + -$10 and Value maxpacket-aligned
max-size^2 6 + Value chunk-p2
$10 Constant key-salt#
$10 Constant key-cksum#

\ for bigger blocks, we use use alloc+guard, i.e. mmap with a
\ guard page after the end.

: alloc-buf ( -- addr )
    maxpacket-aligned buffers# * alloc+guard ;
: alloc-buf+6 ( -- addr )  alloc-buf 6 + ;
: free-buf ( addr -- )
    maxpacket-aligned buffers# * 2dup erase free+guard ;
: free-buf+6 ( addr -- )
    6 - free-buf ;

[IFDEF] cygwin
    : no-hybrid ; \ cygwin can't deal with hybrid stacks
[THEN]

\ per-thread memory space

UValue inbuf    ( -- addr )
UValue tmpbuf   ( -- addr )
UValue outbuf   ( -- addr )
hash: routes#

\ add IP addresses

require classes.fs
require ip.fs
require socks.fs

UDefer other

: -other        ['] noop is other ;
-other

Defer alloc-code-bufs ' noop is alloc-code-bufs
Defer free-code-bufs  ' noop is free-code-bufs

Variable task-id#

: alloc-io ( -- ) \ allocate IO and reset generic user variables
    io-buffers new io-mem !
    1 task-id# +!@ task# !
    -other
    alloc-buf+6 to inbuf
    alloc-buf to tmpbuf
    alloc-buf+6 to outbuf
    alloc-code-bufs
    init-ed25519 c:init ;

:noname ( -- ) defers thread-init
    alloc-io b-out op-vector @ debug-vector ! ; is thread-init

: free-io ( -- )
    free-ed25519 c:free
    free-code-bufs
    0 io-mem !@ .dispose
    inbuf  free-buf+6
    tmpbuf free-buf
    outbuf free-buf+6 ;

alloc-io

Variable net2o-tasks

: net2o-pass ( params xt n task -- )
    dup net2o-tasks >stack  pass
    ?salt-init off  rng-o off \ make double sure no rng is active
    alloc-io prep-socks catch-loop
    1+ ?dup-IF  free-io 1- ?dup-IF  DoError  THEN
    ELSE  ~~ bflush 0 (bye) ~~  THEN ;
: net2o-task ( params xt n -- task )
    stacksize4 NewTask4 dup >r net2o-pass r> ;

Variable kills
event: :>killed ( -- )  -1 kills +! ;
event: :>kill ( task -- )
    <event :>killed event> 0 (bye) ;
: send-kill ( task -- ) <event up@ elit, :>kill event> ;

2 Constant kill-seconds#
kill-seconds# 1+ #1000000000 um* 2constant kill-timeout# \ 3s

: net2o-kills ( -- )
    net2o-tasks get-stack kills !  net2o-tasks $free
    kills @ 0 ?DO  send-kill  LOOP
    ntime kill-timeout# d+ { d: timeout }
    kill-seconds# >r \ give time to terminate
    BEGIN  timeout ntime d- 2dup d0> kills @ and  WHILE
	    stop-dns
	    timeout ntime d- 1000000000 fm/mod nip
	    dup r> <> IF  '.' emit  THEN  >r
    REPEAT
    r> kill-seconds# <> IF  cr  THEN  2drop ;

forward !save-all-msgs

0 warnings !@
: bye  !save-all-msgs net2o-kills bye ;
warnings !

\ packet&header size

\ The first byte is organized in a way that works on wired-or busses,
\ e.g. CAN bus, i.e. higher priority and smaller header and data size
\ wins arbitration.  Use MSB first, 0 as dominant bit.

$00 Constant qos0# \ highest priority
$40 Constant qos1#
$80 Constant qos2#
$C0 Constant qos3# \ lowest

$30 Constant headersize#
$00 Constant 16bit# \ protocol for very small networks
$10 Constant 64bit# \ standard, encrypted protocol
$0F Constant datasize#

Create header-sizes  $06 c, $1a c, $FF c, $FF c,
Create tail-sizes    $00 c, $10 c, $FF c, $FF c,
Create add-sizes     $06 c, $2a c, $FF c, $FF c,
\ we don't know the header sizes of protocols 2 and 3 yet ;-)

: header-size ( addr -- n )  c@ headersize# and 4 rshift header-sizes + c@ ;
: tail-size ( addr -- n )  c@ headersize# and 4 rshift tail-sizes + c@ ;
: add-size ( addr -- n )  c@ headersize# and 4 rshift add-sizes + c@ ;
: body-size ( addr -- n ) min-size swap c@ datasize# and lshift ;
: packet-size ( addr -- n )
    dup add-size swap body-size + ;
: packet-body ( addr -- addr )
    dup header-size + ;
: packet-data ( addr -- addr u )
    >r r@ header-size r@ + r> body-size ;

add-sizes 1+ c@ min-size + Constant minpacket#

\ second byte constants

$80 Constant broadcasting# \ special flags for switches
$40 Constant multicasting#

\ $30 Constant net2o-reserved# - should be 0

$08 Constant stateless# \ stateless message
$07 Constant acks#
$01 Constant ack-toggle#
$02 Constant b2b-toggle#
$04 Constant resend-toggle#

\ short packet information

: .header ( addr -- ) base @ >r hex
    dup c@ >r
    min-size r> datasize# and lshift hex. ." bytes to "
    mapaddr le-64@ u64. cr
    r> base ! ;

\ each source has multiple destination spaces

64User dest-addr
User dest-flags

: >ret-addr ( -- )
    inbuf destination return-addr reverse$16 ;
: >dest-addr ( -- )
    inbuf mapaddr le-64@ dest-addr 64!
    inbuf hdrflags le-uw@ dest-flags le-w! ;

\ validation stuff

User validated

$0001 Constant crypt-val
$0002 Constant own-crypt-val
$0004 Constant login-val
$0008 Constant cookie-val
$0010 Constant tmp-crypt-val
$0020 Constant signed-val
$0040 Constant newdata-val
$0080 Constant newcode-val
$0100 Constant keypair-val
$0200 Constant receive-val
$0400 Constant ivs-val
$0800 Constant qr-tmp-val
$1000 Constant enc-crypt-val
$2000 Constant ack-order-val
$4000 Constant wallet-val

$10 Constant validated#

: crypt?     ( -- flag )  validated @ crypt-val     and ;
: own-crypt? ( -- flag )  validated @ own-crypt-val and ;
: login?     ( -- flag )  validated @ login-val     and ;
: cookie?    ( -- flag )  validated @ cookie-val    and ;
: tmp-crypt? ( -- flag )  validated @ tmp-crypt-val and ;
: signed?    ( -- flag )  validated @ signed-val    and ;
: qr-crypt?  ( -- flag )  validated @ qr-tmp-val    and ;
: enc-crypt? ( -- flag )  validated @ enc-crypt-val and ;
: ack-order? ( -- flag )  validated @ ack-order-val and ;
: !!wallet?  ( -- )       validated @ wallet-val    and 0= !!wallet!! ;
: !!signed?  ( -- ) signed? 0= !!unsigned!! ;
: !!unsigned?  ( -- ) signed?  !!signed!! ;

\ : reqmask ( -- addr )
\     task# @ reqmask[] $[] ;

\ events for context-oriented behavior

Defer do-connect
Defer do-disconnect

event: :>connect    ( connection -- ) .do-connect ;

\ check for valid destination

Variable dest-map s" " dest-map $!
:noname defers 'cold alloc-io dest-map $init ; is 'cold

$100 Value dests#
56 Value dests>>

: set-dests# ( bits -- )
    1 over lshift to dests#
    64 swap - to dests>>
    dests# 2* cells dest-map $!len
    dest-map $@ erase ;

8 set-dests#

: >dest-map ( vaddr -- addr )
    dests>> 64rshift 64>n 2* cells dest-map $@ drop + ;

scope{ mapc

: >data-head ( addr o:map -- flag )  dest-size 1- >r
    dup dest-back r@ and u< IF  r@ + 1+  THEN
    dest-back r> invert and + \ add total offset
    maxdata +                   \ add one packet size
    dup addr dest-head umax!@ <> ;

}scope

: dest-index ( -- addr ) dest-addr 64@ >dest-map ;

: check-dest ( size -- addr map o:job / f )
    \G return false if invalid destination
    \G return 1 if code, -1 if data, plus destination address
    negate \ generate mask
    dest-index 2 cells bounds ?DO
	I @ IF
	    dup dest-addr 64@ I @ with mapc
	    dest-vaddr 64- 64>n and dup
	    dest-size u<
	    IF
		dup addr>bits ack-bit# !
		dest-raddr swap dup >data-head to ack-advance? +
		o parent o> >o rdrop
		UNLOOP  rot drop  EXIT  THEN
	    drop endwith
	THEN
    cell +LOOP
    drop false ;

\ context debugging

: .o ( -- ) context# ? ;
: o? ( -- ) ]] o 0= ?EXIT [[ ; immediate

\ Destination mapping contains
\ addr u - range of virtal addresses
\ addr' - real start address
\ context - for exec regions, this is the job context

User >code-flag

scope{ mapc

: alloc-data ( addr u -- u )
    dup >r to dest-size to dest-vaddr r>
    dup alloc+guard to dest-raddr
    c:key# crypt-align + alloz addr dest-ivsgen ! \ !!FIXME!! should be a kalloc
    >code-flag @
    IF
	dup addr>replies  alloc+guard to dest-replies
	3 to dest-ivslastgen
    ELSE
	dup addr>ts       alloz to dest-timestamps
    THEN ;

}scope

: parent! ( o -- )
    dup to parent ?dup-IF  .my-key  ELSE  my-key-default  THEN  to my-key ;

: map-data ( addr u -- o )
    o >code-flag @ IF mapc:rcode-class ELSE mapc:rdata-class THEN new
    with mapc parent!
    alloc-data
    >code-flag @ 0= IF
	dup addr>bytes allocate-bits data-ackbits !
    THEN
    drop
    o endwith ;

: map-source ( addr u addrx -- o )
    o >code-flag @ IF mapc:code-class ELSE mapc:data-class THEN new
    with mapc parent!
    alloc-data
    >code-flag @ 0= IF
	dup addr>ts allo1 data-resend# !
    THEN
    drop
    o endwith ;

: map-data-dest ( vaddr u addr -- )
    >r >r 64dup r> map-data r@ ! >dest-map r> @ swap ! ;
: map-code-dest ( vaddr u addr -- )
    >r >r 64dup r> map-data r@ ! >dest-map cell+ r> @ swap ! ;

\ create context

8 Value bursts# \ number of 
8 Value delta-damp# \ for clocks with a slight drift
bursts# 2* 2* 1- Value tick-init \ ticks without ack
#1000000 max-size^2 lshift Value bandwidth-init \ 32µs/burst=2MB/s
#2000 max-size^2 lshift Value bandwidth-max
64#-1 64Constant never
2 Value flybursts#
$100 Value flybursts-max#
$20 cells Value resend-size#
#50.000.000 d>64 64Constant init-delay# \ 50ms initial timeout step
#60.000.000.000 d>64 64Constant connect-timeout# \ 60s connect timeout

Variable init-context#
Variable msg-groups

UValue connection

in net2o : new-log ( -- o )
    cmd-class new >o  log-table @ token-table ! o o> ;
in net2o : new-ack ( -- o )
    o ack-class new >o  parent!  ack-table @ token-table !
    init-delay# rtdelay 64!
    flybursts# dup flybursts ! flyburst !
    ticks lastack 64! \ asking for context creation is as good as an ack
    bandwidth-init n>64 ns/burst 64!
    never               next-tick 64!
    64#0                extra-ns 64!
    max-int64 64-2/ min-slack 64!
    max-int64 64-2/ 64negate max-slack 64!
    o o> ;
: ack@ ( -- o )
    ack-context @ ?dup-0=-IF  net2o:new-ack dup ack-context !  THEN ;
in net2o : new-msg ( -- o )
    o msg-class new >o  parent!  msg-table @ token-table ! o o> ;
in net2o : new-msging ( -- o )
    o msging-class new >o  parent!  msging-table @ token-table ! o o> ;

: no-timeout ( -- )  max-int64 next-timeout 64!
    ack-context @ ?dup-IF  .timeouts off  THEN ;

: -flow-control ['] noop is ack-xt ;

64User ticker
64User context-ticker  64#0 context-ticker 64!

: rtdelay! ( time -- )
    timeouts @ IF \ don't update rtdelay if there were timeouts
	rtdelay 64@ init-delay# 64<> IF  64drop  EXIT  THEN
    THEN
    recv-tick 64@ 64swap 64-
    rtd( ." rtdelay: " 64dup 64>f .ns cr ) rtdelay 64! ;

in net2o : new-context ( -- o )
    context-class new >o timeout( ." new context: " o hex. cr )
    my-key-default to my-key \ set default key
    o contexts !@ next-context !
    o to connection \ current connection
    context-table @ token-table ! \ copy pointer
    init-context# @ context# !  1 init-context# +!
    return-addr return-address $10 move
    ['] no-timeout is timeout-xt ['] .iperr is setip-xt
    ['] noop is punch-done-xt ['] noop is sync-done-xt  ['] noop is ack-xt
    -flow-control
    -1 blocksize !
    1 blockalign !
    end-semas start-semas DO  I 0 pthread_mutex_init drop
    1 pthread-mutexes +LOOP
    64#0 context-ticker 64!@ 64dup 64#0 64<> IF
	ack@ >o ticker 64@ recv-tick 64! rtdelay! o>  ELSE  64drop  THEN
    o o> ;

: ret-addr ( -- addr ) o IF  return-address  ELSE  return-addr  THEN ;

\ create new maps

Variable mapstart $1 mapstart !

: ret0 ( -- ) return-addr $10 erase ;
: setup! ( -- )   setup-table @ token-table !  ret0 ;
: context! ( -- )
    context-table @ token-table !
    <event wait-task @ main-up@ over select o elit, :>connect event> ;

: new-code@ ( -- addrs addrd u -- )
    new-code-s 64@ new-code-d 64@ new-code-size @ ;
: new-code! ( addrs addrd u -- )
    new-code-size ! new-code-d 64! new-code-s 64! newcode-val validated or! ;
: new-data@ ( -- addrs addrd u -- )
    new-data-s 64@ new-data-d 64@ new-data-size @ ;
: new-data! ( addrs addrd u -- )
    new-data-size ! new-data-d 64! new-data-s 64! newdata-val validated or! ;

in net2o : new-map ( -- addr )
    mapstart @ 1 mapstart +! reverse
    [ cell 4 = ] [IF]  0 swap  [ELSE] $FFFFFFFF00000000 and [THEN] ;
in net2o : new-data ( addrs addrd u -- )
    dup max-data# u> !!mapsize!! min-size swap lshift
    { 64: addrs 64: addrd u -- }
    o 0= IF
	addrd >dest-map @ ?EXIT
	net2o:new-context >o rdrop  setup!  THEN
    msg( ." data map: " addrs x64. ." own: " addrd x64. u hex. cr )
    >code-flag off
    addrd u addr data-rmap map-data-dest
    addrs u map-source to data-map ;
in net2o : new-code ( addrs addrd u -- )
    dup max-code# u> !!mapsize!! min-size swap lshift
    { 64: addrs 64: addrd u -- }
    o 0= IF
	addrd >dest-map @ ?EXIT
	net2o:new-context >o rdrop  setup!  THEN
    msg( ." code map: " addrs x64. ." own: " addrd x64. u hex. cr )
    >code-flag on
    addrd u addr code-rmap map-code-dest
    addrs u map-source to code-map ;

Forward new-ivs ( -- )
\G Init the new IVS
: create-maps ( -- ) validated @ >r
    [ newcode-val newdata-val or invert ]L r@ and validated !
    r@ newcode-val and IF  new-code@ net2o:new-code ELSE  rdrop EXIT  THEN
    r> newdata-val and IF  new-data@ net2o:new-data THEN ;
: update-cdmap ( -- )
    o 0= IF  do-keypad sec@ nip keysize2 <> ?EXIT  THEN
    create-maps
    o IF
	validated @ keypair-val and IF
	    tmp-pubkey  $@ pubkey  $!
	    tmp-my-key   @ to my-key
	THEN
	validated @ ivs-val and IF  new-ivs  THEN
	tmp-perm @ ?dup-IF  perm-mask !  tmp-perm off  THEN
	[ keypair-val ivs-val or invert ]L validated and!
    THEN ;

\ dispose connection

scope{ mapc

: free-resend ( o:data ) dest-size addr>ts >r
    data-resend#    r@ ?free
    addr dest-timestamps r> ?free ;
: free-resend' ( o:data ) dest-size addr>ts >r
    addr dest-timestamps r> ?free ;
: free-code ( o:data -- ) dest-size >r
    addr dest-raddr   r@              ?free+guard
    addr dest-ivsgen  c:key#          ?free
    addr dest-replies r@ addr>replies ?free+guard
    rdrop dispose ;
' free-code code-class to free-data
:noname ( o:data -- )
    free-resend free-code ; data-class to free-data

: free-rcode ( o:data --- )
    data-ackbits dest-size addr>bytes ?free
    data-ackbits-buf $off
    free-code ;
:noname ( o:data -- )
    free-resend' free-rcode ; rdata-class to free-data
' free-rcode rcode-class to free-data

}scope

\ symmetric key management and searching in open connections

: search-context ( .. xt -- .. ) { xt }
    \G xt has ( .. -- .. flag ) with true to continue
    contexts  BEGIN  @ dup  WHILE  >o  xt execute
	next-context o> swap  0= UNTIL  THEN  drop ;

\ data sending around

: >blockalign ( n -- block )
    blockalign @ dup >r 1- + r> negate and ;
: >maxalign ( n -- block )
    maxdata dup >r 1- + r> negate and ;
: 64>blockalign ( 64 -- block )
    blockalign @ dup >r 1- n>64 64+ r> negate n>64 64and ;
: /head ( u -- )
    >blockalign dup negate residualread +!
    data-map with mapc +to dest-head endwith ;
: max/head ( -- )
    data-map with mapc dest-head >maxalign to dest-head endwith ;
: /back ( u -- )
    >blockalign dup negate residualwrite +!
    data-rmap with mapc +to dest-back endwith ;
: max/back ( -- )
    data-rmap with mapc dest-back >maxalign to dest-back endwith ;
: /tail ( u -- )
    data-map >o +to mapc:dest-tail o> ;
: data-dest ( -- addr )
    data-map with mapc
    dest-vaddr dest-tail dest-size 1- and n>64 64+ endwith ;

\ new data sending around stuff, with front+back

scope{ mapc

: fix-size ( offset1 offset2 -- addr len )
    over - >r dest-size 1- and r> over + dest-size umin over - ;
: fix-tssize ( offset1 offset2 -- addr len )
    over - >r dest-size addr>ts 1- and r> over +
    dest-size addr>ts umin over - ;
: fix-bitsize ( offset1 offset2 -- addr len )
    over - >r dest-size addr>bits 1- and r> over +
    dest-size addr>bits umin over - ;
: raddr+ ( addr len -- addr' len ) >r dest-raddr + r> ;
: fix-size' ( base offset1 offset2 -- addr len )
    over - >r dest-size 1- and + r> ;

}scope

: head@ ( -- head )  data-map .mapc:dest-head ;

: data-head@ ( -- addr u )
    \G you can read into this, it's a block at a time (wraparound!)
    data-map with mapc
    dest-head dest-back dest-size + fix-size raddr+ endwith
    residualread @ umin ;
: rdata-back@ ( tail -- addr u )
    \G you can write from this, also a block at a time
    data-rmap with mapc
    dest-back swap fix-size raddr+ endwith
    residualwrite @ umin ;
: data-tail@ ( -- addr u )
    \G you can send from this - as long as you stay block aligned
    data-map with mapc
    dest-raddr dest-tail dest-head fix-size' endwith ;

: data-head? ( -- flag )
    \G return true if there is space to read data in
    data-map with mapc dest-head dest-back dest-size + u< endwith ;
: data-tail? ( -- flag )
    \G return true if there is data to send
    data-map with mapc dest-tail dest-head u< endwith ;
: rdata-back? ( tail -- flag )
    \G return true if there is data availabe to write out
    data-rmap .mapc:dest-back u> ;

\ code sending around

: code-dest ( -- addr )
    code-map with mapc dest-raddr dest-tail maxdata negate and + endwith ;
: code-vdest ( -- addr )
    code-map with mapc dest-vaddr dest-tail n>64 64+ endwith ;
: code-reply ( -- addr )
    code-map with mapc dest-tail addr>replies dest-replies + endwith ;
: send-reply ( -- addr )
    code-map with mapc dest-addr 64@ dest-vaddr 64- 64>n addr>replies
	dest-replies + endwith ;

: tag-addr ( -- addr )
    code-rmap with mapc dest-addr 64@ dest-vaddr 64- 64>n addr>replies
	dest-replies + endwith ;

reply buffer: dummy-reply
' noop dummy-reply is reply-xt

: reply[] ( index -- addr )
    code-map with mapc
    dup dest-size addr>bits u<
    IF  reply * dest-replies +  ELSE  dummy-reply  THEN  endwith ;

: reply-index ( -- index )
    code-map .mapc:dest-tail addr>bits ;

: code+ ( n -- )
    connection .code-map with mapc dup negate dest-tail and +
    dest-size 1- and to dest-back endwith ;

: code-update ( n -- ) drop \ to be used later
    connection .code-map with mapc dest-back to dest-tail endwith ;

\ aligned buffer to make encryption/decryption fast

: $>align ( addr u -- addr' u ) dup $400 u> ?EXIT
    tuck aligned$ swap move aligned$ swap ;
    
\ timing records

Sema timing-sema

in net2o : track-timing ( -- ) \ initialize timing records
    timing-stat $off ;

: )stats ]] THEN [[ ;
: stats( ]] timing-stat @ IF [[ ['] )stats assert-canary ; immediate

in net2o : timing$ ( -- addr u )
    stats( timing-stat $@  EXIT ) ." no timing stats" cr s" " ;
in net2o : /timing ( n -- )
    stats( timing-stat 0 rot $del ) ;

: .rec-timing ( addr u -- )
    [: ack@ >o track-timing $@ \ do some dumps
      bounds ?DO
	  I ts-delta f>64 last-time 64+!
	  last-time 64@ 64>f 1n f* fdup f.
	  time-offset 64@ 64>f 1n f* 10e fmod f+ f.
	  \ I ts-delta f.
	  I ts-slack 1u f* f.
	  tick-init 1+ maxdata * 1k fm* fdup
	  I ts-reqrate f/ f.
	  I ts-rate f/ f.
	  I ts-grow 1u f* f.
	  ." timing" cr
      timestats +LOOP
      track-timing $off o> ;] timing-sema c-section ;

in net2o : rec-timing ( addr u -- )
    [: track-timing $+! ;] timing-sema c-section ;

: stat+ ( addr -- )  stat-tuple timestats  timing-stat $+! ;

\ flow control

: !ticks ( -- )
    ticks ticker 64! ;

: ticks-init ( ticks -- )
    64dup bandwidth-tick 64!  next-tick 64! ;

: >rtdelay ( client serv -- client serv )
    recv-tick 64@ 64dup lastack 64!
    64over 64- rtd( ." rtdelay min to " 64dup 64>f .ns cr ) rtdelay 64min! ;

: timestat ( client serv -- )
    64dup 64-0<=    IF  64drop 64drop  EXIT  THEN
    timing( 64over u64. 64dup u64. ." acktime" cr )
    >rtdelay  64- 64dup lastslack 64!
    lastdeltat 64@ delta-damp# 64rshift
    64dup min-slack 64+! 64negate max-slack 64+!
    64dup min-slack 64min!
    max-slack 64max! ;

: b2b-timestat ( client serv -- )
    64dup 64-0<= IF  64drop 64drop  EXIT  THEN
    64- lastslack 64@ 64- slackgrow 64! ;

scope{ mapc

: >offset ( addr -- addr' flag )
    dest-vaddr 64- 64>n dup dest-size u< ;

}scope

#5000000 Value rt-bias# \ 5ms additional flybursts allowed

in net2o : set-flyburst ( -- bursts )
    rtdelay 64@ 64>f rt-bias# s>f f+ ns/burst 64@ 64>f f/ f>s
    flybursts# +
    bursts( dup . .o ." flybursts "
    rtdelay 64@ u64. ns/burst 64@ u64. ." rtdelay" cr )
    dup flybursts-max# min rate( ." flyburst: " dup . ) flyburst ! ;
in net2o : max-flyburst ( bursts -- )  flybursts-max# min flybursts max!@
    bursts( 0= IF  .o ." start bursts" cr THEN )else( drop ) ;

: >flyburst ( -- )
    flyburst @ flybursts max!@ \ reset bursts in flight
    0= IF  recv-tick 64@ ticks-init
	bursts( .o ." restart bursts " flybursts ? cr )
	net2o:set-flyburst net2o:max-flyburst
    THEN ;

: >timestamp ( time addr -- time' ts-array index / time' 0 0 )
    >flyburst
    64>r time-offset 64@ 64+ 64r>
    parent .data-map dup 0= IF  drop 0 0  EXIT  THEN  >r
    r@ with mapc >offset  IF
	dest-tail dest-size endwith  >r over - r> 1- and
	addr>bits 1 max window-size !
	addr>ts r> .mapc:dest-timestamps swap
    ELSE  o> rdrop 0 0  THEN ;

in net2o : ack-addrtime ( ticks addr -- )
    >timestamp over  IF
	dup tick-init 1+ 64s u>
	IF  + dup >r  64@
	    r@ tick-init 1+ 64s - 64@
	    64dup 64-0<= >r 64over 64-0<= r> or
	    IF  64drop 64drop  ELSE  64- lastdeltat 64!  THEN  r>
	ELSE  +  THEN
	64@ timestat
    ELSE  2drop 64drop  THEN ;

in net2o : ack-b2btime ( ticks addr -- )
    >timestamp over  IF  + 64@ b2b-timestat
    ELSE  2drop 64drop  THEN ;

\ set rate calculation

#20000000 Value slack-default# \ 20ms slack leads to backdrop of factor 2
#1000000 Value slack-bias# \ 1ms without effect
slack-default# 2* 2* n>64 64Constant slack-ignore# \ above 80ms is ignored
#0 Value slack-min# \ minimum effect limit
3 4 2Constant ext-damp# \ 75% damping
5 2 2Constant delta-t-grow# \ 4 times delta-t

: slack-max# ( -- n ) max-slack 64@ min-slack 64@ 64- ;
: slack# ( -- n )  slack-max# 64>n 2/ 2/ slack-default# max ;

: >slack-exp ( -- rfactor )
    lastslack 64@ min-slack 64@ 64-
    64dup 64abs slack-ignore# 64u> IF
	msg( ." slack ignored: " 64dup u64. cr )
	64drop 64#0 lastslack 64@ min-slack 64!
    THEN
    64>n stats( dup s>f stat-tuple to ts-slack )
    slack-bias# - slack-min# max slack# 2* 2* min
    s>f slack# fm/ 2e fswap f** ;

: aggressivity-rate ( slack -- slack' )
    slack-max# 64-2/ 64>n slack-default# tuck min swap 64*/ ;

: slackext ( rfactor -- slack )
    slackgrow 64@
    window-size @ tick-init 1+ bursts# - 2* 64*/
    64>f f* f>64
    slackgrow' 64@ 64+ 64dup ext-damp# 64*/ slackgrow' 64!
    64#0 64max aggressivity-rate ;

: rate-limit ( rate -- rate' )
    \ not too quickly go faster!
    64dup last-ns/burst 64!@ 64max ;

: >extra-ns ( rate -- rate' )
    >slack-exp fdup 64>f f* f>64 slackext
    64over 64-2* 64-2* 64min \ limit to 4* rate
    64dup extra-ns 64! 64+ ;

: rate-stat1 ( rate deltat -- )
    stats( recv-tick 64@ time-offset 64@ 64-
           64dup last-time 64!@ 64- 64>f stat-tuple to ts-delta
           64over 64>f stat-tuple to ts-reqrate ) ;

: rate-stat2 ( rate -- rate )
    stats( 64dup extra-ns 64@ 64+ 64>f stat-tuple to ts-rate
           slackgrow 64@ 64>f stat-tuple to ts-grow
           stat+ ) ;

in net2o : set-rate ( rate deltat -- )
    rate( ." r/d: " 64over u64. 64dup u64. )
    rate-stat1
    64>r tick-init 1+ validated @ validated# rshift 1 max 64*/
    64dup >extra-ns noens( 64drop )else( 64nip )
    64r> delta-t-grow# 64*/ 64min ( no more than 2*deltat )
    bandwidth-max n>64 64max
    rate-limit  rate-stat2 rate( ." rate: " 64dup u64. )
    ns/burst 64!@ bandwidth-init n>64 64= IF \ first acknowledge
	net2o:set-flyburst
	net2o:max-flyburst
    THEN rate( cr ) ;

\ acknowledge

$20 Value mask-bits#
: >mask0 ( addr mask -- addr' mask' )
    BEGIN  dup 1 and 0= WHILE  1 rshift >r maxdata + r>  dup 0= UNTIL  THEN ;
in net2o : resend-mask ( addr mask -- ) >mask0
    >r dup data-rmap .mapc:dest-size u>= IF
	msg( ." Invalid resend: " hex. r> hex. cr )else( drop rdrop ) EXIT
    THEN  r>
    resend( ." mask: " hex[ >r dup u. r> dup u. ]hex cr )
    data-resend $@ bounds ?DO
	over I cell+ @ swap dup maxdata mask-bits# * + within IF
	    over I 2@ rot >r
	    BEGIN  over r@ u>  WHILE  2* >r maxdata - r>  REPEAT
	    rdrop nip or >mask0
	    resend( I 2@ hex[ ." replace: " swap . . ." -> "
	    >r dup u. r> dup u. cr ]hex )
	    I 2!  UNLOOP  EXIT
	THEN
    2 cells +LOOP { d^ mask+ } mask+ 2 cells data-resend $+! ;
in net2o : ack-resend ( flag -- )  resend-toggle# and to ack-resend~ ;
: resend$@ ( -- addr u )
    data-resend $@  IF
	2@ >mask0 1 and IF  maxdata  ELSE  0  THEN
	swap data-map .mapc:dest-raddr + swap
    ELSE  drop 0 0  THEN ;
: resend? ( -- flag )
    data-resend $@  IF  @ 0<>  ELSE  drop false  THEN ;

: resend-dest ( -- addr )
    data-resend $@ drop 2@ drop n>64 data-map .mapc:dest-vaddr 64+ ;
: /resend ( u -- )
    0 +DO
	data-resend $@ drop
	dup >r 2@ -2 and >mask0 tuck r> 2!
	0= IF  data-resend 0 2 cells $del  THEN
    maxdata +LOOP ;

\ resend third handshake

: push-reply ( addr u -- )  resend0 $!  return-addr r0-address $10 move ;

\ load crypto here

require crypt.fs

\ file handling

require file.fs

\ helpers for addresses

Forward >sockaddr
Forward sockaddr+return

: -sig ( addr u -- addr u' ) 2dup + 1- c@ 2* $11 + - ;
: n2oaddrs ( xt -- )
    my-addr$ [: -sig sockaddr+return rot dup >r execute r> ;] $[]map drop ;

\ send blocks of memory

: >dest ( addr -- ) outbuf destination $10 move ;
: set-dest ( target -- )
    64dup dest-addr 64!  outbuf mapaddr le-64! ;
: set-dest# ( resend# -- )
    n>64 dest-addr 64+!  dest-addr 64@ outbuf mapaddr le-64! ;

User outflag  outflag off

: set-flags ( -- )
    0 outflag !@ outbuf hdrtags c!
    outbuf hdrflags le-uw@ dest-flags le-w! ;

#90 Constant EMSGSIZE

: ?msgsize ( ior -- )
    0< IF
	errno EMSGSIZE <> ?ior
	max-size^2 1- to max-size^2  ." pmtu/2" cr
    THEN ;

: packet-to ( -- )
    out-route  outbuf dup packet-size
    send-a-packet ?msgsize ;

: send-code-packet ( -- ) +sendX
    header( ." send code " outbuf .header )
    outbuf hdrtags c@ stateless# and IF
	outbuf0-encrypt
	cmd0( .time ." cmd0 to: " ret-addr .addr-path cr )
    ELSE
	code-map outbuf-encrypt
    THEN   ret-addr >dest packet-to ;

: send-data-packet ( -- ) +sendX
    header( ." send data " outbuf .header )
    data-map  outbuf-encrypt
    ret-addr >dest packet-to ;

: >send ( addr n -- )
    >r  r@ [ 64bit# qos3# or ]L or outbuf c!  set-flags
    outbuf packet-body min-size r> lshift move ;

: bandwidth+ ( -- )
    ns/burst 64@ 1 tick-init 1+ 64*/ bandwidth-tick 64+! ;

: burst-end ( flag -- flag )
    ticker 64@ bandwidth-tick 64@ 64max next-tick 64! drop false ;

: send-cX ( addr n -- ) +sendX2  >send  send-code-packet ;

\ !!FIXME!! use ffz>, branchless with floating point

: 64ffz< ( 64b -- u / -1 )
    \G find first zero from the right, u is bit position
    64 0 DO
	64dup 64>n 1 and 0= IF  64drop I unloop  EXIT  THEN
	64-2/
    LOOP 64drop $40 ;

scope{ mapc

: resend#+ ( addr -- n )
    dest-raddr - addr>64 data-resend# @ + { addr }
    rng8 $3F and { r }
    addr le-64@ r 64ror 64ffz< r + $3F and to r
    64#1 r 64lshift addr le-64@ 64or
    \ timeout( ." resend#: " addr data-resend# @ dup hex. - hex. 64dup x64. cr )
    addr le-64! 
    r ;

: resend#? ( off addr u -- n )
    0 rot 2swap \ count addr off u
    bounds dest-size addr>bits tuck umin >r umin r> \ limits
    64s data-resend# @ + swap
    64s data-resend# @ + swap ?DO
	dup c@ $40 u< IF
	    dup c@ >r 64#1 r> 64lshift
	    I le-64@
	    \ 64over 64invert 64over 64and I le-64! \ ack only once!
	    64and 64-0= IF \ check if had been zero already
		timeout( ." resend# unmatch: "
		I data-resend# @ dup hex. - hex.
		dup c@ hex. I le-64@ x64. cr )
		2drop 0 UNLOOP  EXIT
	    THEN  swap 1+ swap
	THEN  1+
    8 +LOOP  drop ;

}scope

: send-dX ( addr n -- ) +sendX2
    over data-map .mapc:resend#+ set-dest#
    >send  ack@ .bandwidth+  send-data-packet ;

Defer addr>sock \ uses locals
Forward punch-reply
Forward new-addr

: send-punch ( addr u -- addr u )
    check-addr1 0= IF  2drop  EXIT  THEN
    temp-addr ret-addr $10 move
    insert-address ret-addr ins-dest
    nat( ticks .ticks ."  send punch to: " ret-addr .addr-path cr )
    2dup send-cX ;

in net2o : punch ( addr u o:connection -- )
    o IF
	new-addr { w^ punch-addr }
	punch-addr cell punch-addrs $+!
    ELSE  2drop  THEN ;

: pings ( o:connection -- )
    \G ping all addresses except the first one
    punch-addrs $@ cell safe/string bounds ?DO
	I @ ['] ping-addr1 addr>sock
    cell +LOOP ;

: punchs ( addr u o:connection -- )
    \G send a reply to all addresses
    punch-addrs $@ bounds ?DO
	I @ ['] send-punch addr>sock
    cell +LOOP  2drop ;

\ send chunk

\ branchless version using floating point

: send-size ( u -- n )
    min-size umax maxdata umin 1-
    [ min-size 2/ 2/ s>f 1/f ] FLiteral fm*
    { f^ <size-lb> }  <size-lb> 6 + c@ 4 rshift ;

64Variable last-ticks

scope{ mapc

: ts-ticks! ( addr -- )
    addr>ts dest-timestamps + >r last-ticks 64@ r>
    dup 64@ 64-0= IF  64!  EXIT  THEN  64on 64drop 1 packets2 +! ;
\ set double-used ticks to -1 to indicate unkown timing relationship

}scope

in net2o : send-tick ( addr -- )
    data-map with mapc
    dest-raddr - dup dest-size u<
    IF  ts-ticks!  ELSE  drop  THEN  endwith ;

in net2o : prep-send ( addr u dest -- addr n len )
    set-dest  over  net2o:send-tick
    send-size min-size over lshift ;

\ synchronous sending

: data-to-send? ( -- flag )
    resend? data-tail? or ;

in net2o : resend ( -- addr n )
    resend$@ resend-dest net2o:prep-send /resend ;

in net2o : send ( -- addr n )
    data-tail@ data-dest net2o:prep-send /tail ;

: ?toggle-ack ( -- )
    data-to-send? 0= IF
	[ resend-toggle# ack-toggle# or ]L outflag xor!
	never ack@ .next-tick 64!
    THEN ;

in net2o : send-chunk ( -- )  +chunk
    ack-state c@ outflag or!
    bursts# 1- data-b2b @ = IF data-tail? ELSE resend? 0= THEN
    IF  net2o:send  ELSE  net2o:resend  THEN
    ?toggle-ack send-dX ;

: bandwidth? ( -- flag )
    ticker 64@ 64dup last-ticks 64! next-tick 64@ 64- 64-0>=
    flybursts @ 0> and  ;

\ asynchronous sending

begin-structure chunks-struct
field: chunk-context
field: chunk-count
end-structure

Variable chunks
Variable chunks+
Create chunk-adder chunks-struct allot
0 Value sender-task   \ asynchronous sender thread (unused)
0 Value receiver-task \ receiver thread
0 Value timeout-task  \ for handling timeouts
0 Value query-task    \ for background queries initiated in other tasks

: .0depth ( -- ) <warn> "Stack should always be empty!" type cr <default> ;
: !!0depth!! ( -- ) ]] depth IF  .0depth ~~bt clearstack  THEN [[ ; immediate
: event-loop' ( -- )  BEGIN  stop  !!0depth!!  AGAIN ;
: create-query-task ( -- )
    ['] event-loop' 1 net2o-task to query-task ;
: ?query-task ( -- task )
    query-task 0= IF  create-query-task  THEN  query-task ;

: do-send-chunks ( -- ) data-to-send? 0= ?EXIT
    [: chunks $@ bounds ?DO
	  I chunk-context @ o = IF
	      UNLOOP  EXIT
	  THEN
      chunks-struct +LOOP
      o chunk-adder chunk-context !
      0 chunk-adder chunk-count !
      chunk-adder chunks-struct chunks $+! ;]
    resize-sema c-section
    ticker 64@ ack@ .ticks-init ;

: o-chunks ( -- )
    [: chunks $@ bounds ?DO
	    I chunk-context @ o = IF
		chunks I chunks-struct del$one
		unloop chunks next$ ?DO NOPE 0
	    ELSE  chunks-struct  THEN  +LOOP ;]
    resize-sema c-section ;

event: :>send-chunks ( o -- ) .do-send-chunks ;

in net2o : send-chunks  sender-task 0= IF  do-send-chunks  EXIT  THEN
    <event o elit, :>send-chunks sender-task event> ;

: chunk-count+ ( counter -- )
    dup @
    dup 0= IF
	ack-toggle# ack-state xorc!
	ack-resends#
	ack-resend~ ack-state c@ xor resend-toggle# and 0<> +
	0 max dup to ack-resends#
	0= IF  ack-resend~ ack-state c@ resend-toggle# invert and or
	    ack-state c!  ack@ .flybursts @ to ack-resends#  THEN
	-1 ack@ .flybursts +! bursts( ." bursts: " ack@ .flybursts ? ack@ .flyburst ? cr )
	ack@ .flybursts @ 0<= IF
	    bursts( .o ." no bursts in flight " ack@ .ns/burst ? data-tail@ swap hex. hex. cr )
	THEN
    THEN
    tick-init = IF  off  ELSE  1 swap +!  THEN ;

: send-a-chunk ( chunk -- flag )  >r
    data-b2b @ 0<= IF
	ack@ .bandwidth? dup  IF
	    b2b-toggle# ack-state xorc!
	    bursts# 1- data-b2b !
	THEN
    ELSE
	-1 data-b2b +!  true
    THEN
    dup IF  r@ chunk-count+  net2o:send-chunk
	data-b2b @ 0<= IF  ack@ .burst-end  THEN  timeout( '.' emit )  THEN
    rdrop  1 chunks+ +! ;

: .nosend ( -- ) ack@ >o ." done, "  4 set-precision
    .o ." rate: " ns/burst @ s>f tick-init chunk-p2 lshift s>f 1e9 f* fswap f/ fe. cr
    .o ." slack: " min-slack 64@ u64. max-slack 64@ u64. cr
    .o ." rtdelay: " rtdelay 64@ u64. cr o>
    data-map with mapc
    ." Data h b t: " dest-head hex. dest-back hex. dest-tail hex. cr
    endwith ;

: send-chunks-async ( -- flag )
    chunks $@ dup 0= IF  nip  EXIT  THEN
    chunks+ @ chunks-struct * safe/string
    IF
	dup chunk-context @ >o rdrop
	chunk-count
	data-to-send? IF
	    send-a-chunk
	ELSE
	    drop msg( .nosend )
	    [: chunks chunks+ @ chunks-struct * chunks-struct $del ;]
	    resize-sema c-section
	    false
	THEN
    ELSE  drop chunks+ off false  THEN ;

: next-chunk-tick ( -- tick )
    64#-1 chunks $@ bounds ?DO
	I chunk-context @ .ack@ .next-tick 64@ 64umin
    chunks-struct +LOOP ;

: send-another-chunk ( -- flag )  false  0 >r  !ticks
    BEGIN  BEGIN  drop send-chunks-async dup  WHILE  rdrop 0 >r  REPEAT
	chunks+ @ 0= IF  r> 1+ >r  THEN
    r@ 2 u>=  UNTIL  rdrop ;

: send-anything? ( -- flag )  chunks $@len 0> ;

\ rewind buffer to send further packets

scope{ mapc

:noname ( o:map -- ) dest-size addr>ts 
    dest-timestamps over erase
    data-resend# @ swap $FF fill ;
data-class to rewind-timestamps
:noname ( o:map -- ) dest-size addr>ts
    dest-timestamps over erase ;
rdata-class to rewind-timestamps

: rewind-ts-partial ( old-back new-back addr o:map -- )
    { addr } addr>ts swap addr>ts U+DO
	I I' fix-tssize	{ len } addr + len erase
    len +LOOP ;
:noname ( old-back new-back o:map -- )
    2dup data-resend# @ rewind-ts-partial
    2dup dest-timestamps rewind-ts-partial
    regen-ivs-part ;
data-class to rewind-partial
:noname ( old-back new-back o:map -- )
    2dup ackbits-erase
    2dup dest-timestamps rewind-ts-partial
    regen-ivs-part ;
rdata-class to rewind-partial

}scope

in net2o : rewind-sender-partial ( new-back -- )
    data-map with mapc dest-back umax dest-back over rewind-partial to dest-back
    endwith ;

\ separate thread for loading and saving...

in net2o : save { tail -- }
    data-rmap ?dup-IF
	.mapc:dest-back { oldback }
	oldback tail net2o:spit { back }
	data-rmap with mapc
	    oldback back rewind-partial
	    dest-req IF  back do-slurp !  THEN  back to dest-back
	endwith
    THEN ;

Defer do-track-seek

event: :>save ( tail o -- )  .net2o:save ;
event: :>save&done ( tail o -- )
    >o net2o:save sync-done-xt o> ;
event: :>close-all ( o -- )
    .net2o:close-all ;

0 Value file-task

: create-file-task ( -- )
    ['] event-loop' 1 net2o-task to file-task ;
in net2o : save& ( -- )
    file-task 0= IF  create-file-task  THEN
    data-rmap .mapc:dest-tail elit,
    o elit, :>save file-task event> ;
in net2o : save&done ( -- )
    file-task 0= IF  create-file-task  THEN
    data-rmap .mapc:dest-tail elit,
    o elit, :>save&done file-task event| ;

\ schedule delayed events

object class
64field: queue-timestamp
field: queue-job
defer: queue-xt
end-class queue-class
queue-class >osize @ Constant queue-struct

Variable queue
queue-class >osize @ buffer: queue-adder  

: add-queue ( xt us -- )
    ticker 64@ +  o queue-adder >o queue-job !  queue-timestamp 64!
    is queue-xt  o queue-struct queue $+! o> ;

: eval-queue ( -- )
    queue $@len 0= ?EXIT  ticker 64@
    queue $@ bounds ?DO  I >o
	64dup queue-timestamp 64@ 64u> IF
	    addr queue-xt @ queue-job @ .execute o>
	    queue I queue-struct del$one
	    unloop queue next$ ?DO  NOPE 0
	ELSE  o>  queue-struct  THEN
    +LOOP  64drop ;

\ poll loop

: prep-evsocks ( -- )
    epiper @    fileno POLLIN pollfds fds!+ drop 1 to pollfd# ;

: clear-events ( -- )  pollfds
    pollfd# 0 DO  0 over revents w!  pollfd +  LOOP  drop ;

: timeout! ( -- )
    sender-task dup IF  up@ =  ELSE  0=  THEN  IF
	next-chunk-tick 64dup 64#-1 64= 0= >r ticker 64@ 64- 64dup 64-0>= r> or
	IF    64#0 64max poll-timeout# n>64 64min 64>d
	ELSE  64drop poll-timeout# 0  THEN
    ELSE  poll-timeout# 0  THEN  ptimeout 2! ;

: max-timeout! ( -- ) poll-timeout# 0 ptimeout 2! ;

: >poll ( addr u -- flag ) \ prep-socks
[IFDEF] ppoll
    ptimeout 0 ppoll 0>
[ELSE]
    ptimeout 2@ #1000 * swap #1000000 / + poll 0>
[THEN] +wait
;

: wait-send ( -- flag )
    ( clear-events )  timeout!  pollfds pollfd# >poll ;

: poll-sock ( -- flag )
    eval-queue  wait-send ;

User try-reads
4 Value try-read#

: read-a-packet4/6 ( -- addr u )
    pollfds [ pollfd revents ]L + w@ POLLIN and IF  try-reads off
	do-block read-a-packet
	( 0 pollfds [ pollfd revents ]L + w! ) +rec EXIT  THEN
    [IFDEF] no-hybrid
	pollfds [ pollfd 2* revents ]L + w@ POLLIN and IF  try-reads off
	    do-block read-a-packet4
	    ( 0 pollfds [ pollfd 2* revents ]L + w! ) +rec EXIT  THEN
    [THEN]
    try-read# try-reads !  0 0 ;

: read-event ( -- )
    pollfds revents w@ POLLIN and IF
	?events  \ 0 pollfds revents w!
    THEN ;

: try-read-packet-wait ( -- addr u / 0 0 )
    [defined] no-hybrid ( [defined] darwin ) [ ( or ) 0= ] [IF]
	try-read# try-reads @ ?DO
	    don't-block read-a-packet
	    dup IF  unloop  +rec  EXIT  THEN  2drop
	LOOP
    [THEN]
    poll-sock IF read-a-packet4/6 read-event ELSE 0 0 THEN ;

4 Value sends#
4 Value sendbs#
16 Value recvs# \ balance receive and send
Variable recvflag  recvflag off

[defined] no-hybrid ( [defined] darwin or ) [IF]
    ' try-read-packet-wait alias read-a-packet? ( -- addr u )
[ELSE]
    : read-a-packet? ( -- addr u )
	don't-block read-a-packet dup IF  1 recvflag +!  THEN ;
[THEN]

: send-read-packet ( -- addr u )
    recvs# recvflag @ > IF  read-a-packet? dup ?EXIT  2drop  THEN
    recvflag off
    #0. sendbs# 0 DO
	2drop  send-anything?
	sends# 0 ?DO
	    0= IF  try-read-packet-wait
		dup IF  UNLOOP  UNLOOP  EXIT  THEN  2drop  THEN
	    send-another-chunk  LOOP  drop
    read-a-packet? dup ?LEAVE LOOP ;

: send-loop ( -- )
    send-anything?
    BEGIN  0= IF   wait-send drop read-event  THEN
	!!0depth!! send-another-chunk  AGAIN ;

: create-sender-task ( -- )
    [:  \ ." created sender task " up@ hex. cr
	prep-evsocks send-loop ;] 1 net2o-task to sender-task ;

Forward handle-beacon
Forward handle-beacon+hash

: add-source ( -- )
    sockaddr alen @ insert-address inbuf ins-source ;

: next-packet ( -- addr u )
    sender-task 0= IF  send-read-packet  ELSE  try-read-packet-wait  THEN
    dup minpacket# u>= IF
	( nat( ." packet from: " sockaddr alen @ .address cr )
	over packet-size over <>
	header( ~~ !!size!! )else( IF  2drop 0 0 EXIT  !!size!!  THEN )
	+next
	EXIT
    THEN
    dup 1 = IF  drop c@ false swap handle-beacon   0 0  EXIT  THEN
    dup $11 = IF  handle-beacon+hash    0 0  EXIT  THEN ;

0 Value dump-fd

\ in net2o : timeout ( ticks -- ) \ print why there is nothing to send
\     ack@ .>flyburst net2o:send-chunks
\     timeout( ." timeout? " .ticks space
\     resend? . data-tail? . data-head? . fstates .
\     chunks+ ? ack@ .bandwidth? . next-chunk-tick .ticks cr
\     data-rmap @ with mapc data-ackbits @ dest-size addr>bytes dump endwith
\     )else( 64drop ) ;

\ timeout handling

#10.000.000.000 d>64 64Value timeout-max# \ 10s maximum timeout
#100.000.000 d>64 64Value timeout-min# \ 100ms minimum timeout
#14 Value timeouts# \ with 100ms initial timeout, gives 31.75s cummulative timeout

Sema timeout-sema
Variable timeout-tasks

: sq2** ( 64n n -- 64n' )
    dup 1 and >r 2/ 64lshift r> IF  64dup 64-2/ 64+  THEN ;
: >timeout ( 64n n -- 64n )
    >r 64-2* timeout-min# 64max r> sq2** timeout-max# 64min ;
: +next-timeouts ( -- timeout )
    rtdelay 64@ timeouts @ >timeout ticks 64+ ;
: +timeouts ( -- timeout ) 
    +next-timeouts 1 timeouts +! ( @ ." TO inc: " . cr ) ;
: +timeout0 ( -- timeout )
    rtdelay 64@ ticker 64@ 64+ ;
: 0timeout ( -- )
    0 ack@ .timeouts !@  IF  timeout-task wake  THEN
    ack@ .+next-timeouts next-timeout 64! ;

: o+timeout ( -- )  0timeout
    timeout( ." +timeout: " o hex. ." task: " task# ? addr timeout-xt @ .name cr )
    [: timeout-tasks $@ bounds ?DO  I @ o = IF
	      UNLOOP  EXIT  THEN
      cell +LOOP
      o { w^ timeout-o }  timeout-o cell timeout-tasks $+! ;]
    timeout-sema c-section  timeout-task wake ;
: o-timeout ( -- )
    0timeout  timeout( ." -timeout: " o hex. ." task: " task# ? cr )
    [: o timeout-tasks del$cell ;] timeout-sema c-section ;

: >next-timeout ( -- )  ack@ .+timeouts next-timeout 64! ;
: 64min? ( a b -- min flag )
    64over 64over 64< IF  64drop false  ELSE  64nip true  THEN ;
: next-timeout? ( -- time context ) [: 0 { ctx } max-int64
    timeout-tasks $@ bounds ?DO
	I @ .next-timeout 64@ 64min? IF  I @ to ctx  THEN
    cell +LOOP  ctx ;] timeout-sema c-section ;
: ?timeout ( -- context/0 )
    ticker 64@ next-timeout? >r 64- 64-0>= r> and ;

: -timeout      ['] no-timeout  is timeout-xt o-timeout ;

\ handling last packets

begin-structure last-packet
    64value: lp-addr
    64value: lp-time
    $value: lp$
end-structure

last-packet buffer: last-packet-desc

Variable last-packets

Sema lp-sema

: last-packet! ( -- )
    unhandled( ." last packet @" dest-addr 64@ x64. cr )
    outbuf dup packet-size last-packet-desc to lp$
    dest-addr 64@ last-packet-desc to lp-addr
    ticks last-packet-desc to lp-time
    [: last-packet-desc last-packet last-packets $+! ;]
    lp-sema c-section
    last-packet-desc addr lp$ off ;

: last-packet? ( addr -- flag )
    [: last-packets $@ bounds U+DO
	  64dup I lp-addr 64= IF
	      unhandled( ." resend last packet @" 64dup x64. cr )
	      I lp$ over 0 swap packet-route drop send-a-packet ?msgsize
	      64drop true unloop  EXIT
	  THEN
      last-packet +LOOP  64drop false ;] lp-sema c-section ;

: last-packet-tos ( -- )
    ticks connect-timeout# 64-
    [: last-packets $@ bounds U+DO
	  64dup I lp-time 64u> IF
	      I addr lp$ $free
	  ELSE
	      last-packets 0 I last-packets $@ drop - $del
	      64drop unloop  EXIT
	  THEN
      last-packet +LOOP  64drop  s" " last-packets $! ;]
    lp-sema c-section ;

\ handling packets

Forward cmd-exec ( addr u -- )

: !!<order?   ( n -- )  dup c-state @ u>  !!inv-order!! c-state or! ;
: !!>order?   ( n -- )  dup c-state @ u<= !!inv-order!! c-state or! ;
: !!>=order?   ( n -- )  dup c-state @ over 1- invert and u< !!inv-order!! c-state or! ;
: !!<>order?   ( n1 n2 -- )  dup >r
    c-state @ -rot swap within !!inv-order!! r> c-state or! ;
: !!<>=order?   ( n1 n2 -- )  dup >r 1+
    c-state @ -rot swap within !!inv-order!! r> c-state or! ;

User remote?

: handle-cmd0 ( -- ) \ handle packet to address 0
    cmd0( .time ." handle cmd0 " sockaddr alen @ .address cr )
    0 >o rdrop remote? on \ address 0 has no job context!
    inbuf0-decrypt 0= IF
	invalid( ." invalid packet to 0" cr ) EXIT  THEN
    add-source  >ret-addr
    validated off     \ we have no validated encryption, only anonymous
    do-keypad sec-free \ no key exchange may have happened
    $error-id $off    \ no error id so far
    stateless# outflag !  tmp-perm off
    inbuf packet-data cmd-exec
    update-cdmap  net2o:update-key  remote? off ;

scope{ mapc

: handle-data ( addr -- ) parent >o  o to connection
    msg( ." Handle data " inbuf hdrflags be-uw@ hex. ." to addr: " inbuf mapaddr le-64@ hex. cr )
    >r inbuf packet-data r> swap move
    +inmove ack-xt +ack 0timeout o> ;
' handle-data rdata-class to handle
' drop data-class to handle

: handle-cmd ( addr -- )  parent >o
    msg( ." Handle command to addr: " inbuf mapaddr le-64@ x64. cr )
    outflag off  wait-task @ 0= remote? !
    $error-id $off    \ no error id so far
    maxdata negate and >r inbuf packet-data r@ swap dup >r move
    r> r> swap cmd-exec
    o IF  closing?  IF  last-packet!  THEN  o>  ELSE  rdrop  THEN
    remote? off ;
' handle-cmd rcode-class to handle
' drop code-class to handle

: .inv-packet ( -- )
    ." invalid packet to "
    dest-addr 64@ o IF  dest-vaddr 64-  THEN  x64.
    ." size " min-size inbuf c@ datasize# and lshift hex. cr ;

}scope

: handle-dest ( addr map -- ) \ handle packet to valid destinations
    ticker 64@  ack@ .recv-tick 64! \ time stamp of arrival
    dup >r inbuf-decrypt 0= IF
	invalid( r> .mapc:.inv-packet drop )else( rdrop drop ) EXIT
    THEN
    add-source  >ret-addr
    crypt-val validated ! \ ok, we have a validated connection
    r> with mapc handle o IF  endwith  ELSE  rdrop  THEN ;

: handle-packet ( -- ) \ handle local packet
    >dest-addr +desta
    dest-flags 1+ c@ stateless# and  IF
	handle-cmd0
    ELSE
	inbuf body-size check-dest dup 0= IF
	    drop  dest-addr 64@ last-packet? 0= IF
		unhandled( ." unhandled packet to: " dest-addr 64@ x64. cr )
	    THEN  EXIT  THEN +dest
	handle-dest
    THEN ;

: route-packet ( -- )
    add-source
    inbuf >r r@ get-dest route>address IF
	route( ." route to: " sockaddr alen @ .address space
	inbuf destination .addr-path cr )
	r@ dup packet-size send-a-packet 0< ?ior
    THEN  rdrop ;

\ dispose context

: unlink-ctx ( next hit ptr -- )
    next-context @ o contexts
    BEGIN  2dup @ <> WHILE  @ dup .next-context swap 0= UNTIL
	2drop drop EXIT  THEN  nip ! ;
: ungroup-ctx ( -- )
    msg-groups [: >r o r> cell+ del$cell ;] #map ;

Defer extra-dispose ' noop is extra-dispose

in net2o : dispose-context ( o:addr -- o:addr )
    [: cmd( ." Disposing context... " o hex. cr )
      timeout( ." Disposing context... " o hex. ." task: " task# ? cr )
      o-timeout o-chunks extra-dispose
      data-rmap IF  #0. data-rmap .mapc:dest-vaddr >dest-map 2!  THEN
      end-maps start-maps DO  I @ ?dup-IF .mapc:free-data THEN  cell +LOOP
      end-strings start-strings DO  I $off      cell +LOOP
      end-secrets start-secrets DO  I sec-free  cell +LOOP
      fstate-off
      \ erase crypto keys
      log-context @ ?dup-IF  .dispose  THEN
      ack-context @ ?dup-IF
	  >o timing-stat $off track-timing $off dispose o>
      THEN
      msging-context @ ?dup-IF  .dispose  THEN
      msg-context    @ ?dup-IF  .dispose  THEN
      unlink-ctx  ungroup-ctx
      end-semas start-semas DO  I pthread_mutex_destroy drop
      1 pthread-mutexes +LOOP
      dispose  0 to connection
      cmd( ." disposed" cr ) ;] file-sema c-section ;

event: :>dispose-context ( o -- )  .net2o:dispose-context ;
  
\ loops for server and client

8 cells 1- Constant maxrequest#

: next-request ( -- n )
    1 dup request# +!@ maxrequest# and tuck lshift reqmask or!
    request( ." Request added: " dup . ." o " o hex. ." task: " task# ? cr ) ;

: packet-event ( -- )
    next-packet !ticks nip 0= ?EXIT  inbuf route?
    IF  route-packet  ELSE  handle-packet  THEN ;

: clean-request ( n -- )
    1 over lshift invert reqmask and!
    request( ." Request completed: " . ." o " o hex. ." task: " task# ? cr
    )else( drop ) ;

: rqd@ ( n -- xt )
    0 swap rqd-xts $[] !@ ?dup-0=-IF  ['] clean-request  THEN ;

: rqd! ( xt -- )
    \G store request
    request# @ rqd-xts $[] ! ;
: rqd? ( xt -- )
    \G store request if no better is available
    request# @ rqd-xts $[] dup @ IF  2drop  ELSE  !  THEN ;

event: :>request ( n o -- ) >o maxrequest# and
    dup rqd@ request( ." request xt: " dup .name cr )  execute
    reqmask @ 0= IF  request( ." Remove timeout" cr ) -timeout
    ELSE  request( ." Timeout remains: " reqmask @ hex. cr ) THEN  o> ;
event: :>timeout ( o -- )
    timeout( ." Request timed out" forth:cr )
    >o 0 reqmask !@ >r -timeout r> o> msg( ." Request timed out" cr )
    0<> !!timeout!! ;
event: :>throw ( error -- ) throw ;

: timeout-expired? ( -- flag )
    ack@ .timeouts @ timeouts# >= ;
: push-timeout ( o:connection -- )
    timeout-expired? wait-task @ and  ?dup-IF
	o elit, :>timeout event>  THEN ;

: request-timeout ( -- )
    ?timeout ?dup-IF  >o rdrop
	timeout( ." do timeout: " o hex. addr timeout-xt @ .name cr )
	timeout-xt
    THEN ;

\ beacons
\ UDP connections through a NAT close after timeout,
\ typically after a minute or so.
\ To keep connections alive, you have to send a "beacon" a bit before
\ the connection would expire to refresh the NAT window.
\ beacons are send regularly regardless if you have any other traffic,
\ because that's easier to do.
\ beacons are one-byte packets, with ASCII characters to say what they mean

#50.000.000.000 d>64 64Value beacon-ticks# \ 50s beacon tick rate
#2.000.000.000 d>64 64Value beacon-short-ticks# \ 2s short beacon tick rate

Variable beacons \ destinations to send beacons to
Variable need-beacon# need-beacon# on \ true if needs a hash for the ? beacon

: next-beacon ( -- 64tick )
    64#-1 beacons [: cell+ $@ drop 64@ 64umin ;] #map ;

: send-beacons ( -- ) !ticks
    beacons [: { beacon }
	beacon $@ { baddr u }
	beacon cell+ $@ drop 64@ ticker 64@ 64u<= IF
	    beacon( ticks .ticks ."  send beacon to: " baddr u .address )
	    ticker 64@ beacon-short-ticks# 64+ beacon cell+ $@ drop 64!
	    net2o-sock
	    beacon cell+ $@ drop 64'+ @ >o o IF
		beacon-hash $@ beacon( ."  hash: " 2dup 85type )
	    ELSE
		s" ?"
	    THEN  o>
	    beacon( cr )
	    0 baddr u sendto drop +send
	THEN
	;] #map ;

: beacon? ( -- )
    next-beacon ticker 64@ 64u<= IF  send-beacons  THEN ;

: +beacon ( sockaddr len xt -- )
    >r ticks beacon-short-ticks# 64+ o r> { 64^ dest w^ obj w^ xt }
    beacon( ." add beacon: " 2dup .address ."  ' " xt @ .name cr )
    2dup beacons #@ d0= IF
	dest 1 64s cell+ cell+ 2swap beacons #!
    ELSE
	obj 2 cells last# cell+ $+! 2drop
    THEN ;

: o-beacon ( -- )
    beacon( ." remove beacons: " o hex. cr )
    beacons [: { bucket } bucket cell+ $@ 1 64s /string bounds ?DO
	    I @ o = IF
		bucket cell+ I over $@ drop - 2 cells $del  LEAVE  THEN
	2 cells +LOOP
	bucket cell+ $@len 8 = IF
	    bucket $off bucket cell+ $off
	THEN
    ;] #map ;

:noname o-beacon defers extra-dispose ; is extra-dispose

: gen-beacon-hash ( -- hash u )
    dest-0key sec@ "beacon" keyed-hash#128 2/ ;

: add-beacon ( net2oaddr xt -- )
    >r route>address IF
	sockaddr alen @ r@ +beacon
	o IF
	    s" ?" beacon-hash $!  gen-beacon-hash beacon-hash $+!
	THEN
    THEN  rdrop ;
: ret+beacon ( -- )  ret-addr be@ ['] 2drop add-beacon ;

\ timeout loop

: event-send ( -- )
    o IF  wait-task @ ?query-task over select event> 0 >o rdrop  THEN ;

#10000000 Constant watch-timeout# \ 10ms timeout check interval
#10.000000000 d>64 64Constant max-timeout# \ 10s sleep, no more

[IFDEF] android
    64Variable old-beacon 64#-1 old-beacon 64!
    : set-beacon-alarm ( beacon-tick -- )
	64dup old-beacon 64@ 64= IF  64drop  EXIT  THEN
	64dup old-beacon 64!
	64>d 1000000 ud/mod clazz .set_alarm drop ;
    : android-wakeup ( 0 -- ) drop
	timeout-task wake ;
    also android
    ' android-wakeup is android-alarm
    previous
[THEN]

Forward save-msgs?
Forward next-saved-msg

: >next-ticks ( -- )
    next-timeout? drop next-beacon
    [IFDEF] android 64dup set-beacon-alarm [THEN]
    64umin next-saved-msg 64umin ticks 64-
    64#0 64max max-timeout# 64min \ limit sleep time to 1 seconds
    wait( ." wait for " 64dup u64. ." ns" cr ) stop-64ns
    wait( ticker 64@ ) !ticks
    wait( ticker 64@ 64swap 64- ." waited for " u64. ." ns" cr ) ;

: timeout-loop ( -- ) [IFDEF] android jni:attach [THEN]
    !ticks  BEGIN
	>next-ticks     !!0depth!!
	beacon?         !!0depth!!
	save-msgs?      !!0depth!!
	request-timeout !!0depth!!
	event-send      !!0depth!!
	last-packet-tos !!0depth!!
    AGAIN ;

: create-timeout-task ( -- )  timeout-task ?EXIT
    ['] timeout-loop 1 net2o-task to timeout-task ;

\ packet reciver task

: packet-loop ( -- ) \ 1 stick-to-core
    BEGIN  packet-event  !!0depth!!  event-send  !!0depth!!  AGAIN ;

in net2o : request-done ( n -- )  elit, o elit, :>request ;

: create-receiver-task ( -- )
    ['] packet-loop 1 net2o-task to receiver-task ;

: event-loop-task ( -- )
    receiver-task 0= IF  create-receiver-task  THEN ;

: requests->0 ( -- ) request( ." wait reqmask=" o IF reqmask @ hex. THEN cr )
    BEGIN  stop
	o IF  reqmask @ 0= file-count @ 0= and ( reqcount @ 0= and )
	ELSE  false  THEN
    UNTIL
    o IF  o-timeout  THEN  request( ." wait done" cr ) ;

: client-loop ( -- )
    !ticks
    connection >o
    o IF  up@ wait-task !  o+timeout  THEN
    event-loop-task requests->0 o> ;

: server-loop ( -- )
    0 >o rdrop  BEGIN  client-loop  AGAIN ;

: server-loop-catch ( -- )
    ['] server-loop catch
    dup #-28 <> over #-56 <> and and throw ;

\ client/server initializer

Defer init-rest

:noname ( port -- )  init-mykey init-mykey \ generate two keys
    my-0key @ 0= IF  init-my0key  THEN  init-header-key
    \ hash-init-rng
    init-timer net2o-socket init-route prep-socks
    sender( create-sender-task ) create-timeout-task ; is init-rest

Variable initialized

: init-client ( -- )  true initialized !@ 0= IF
	init-dirs  net2o-client-port  init-rest  THEN ;
: init-server ( -- )  true initialized !@ 0= IF
	init-dirs  net2o-port  init-rest  THEN ;

\ connection cookies

Variable cookies

: add-cookie ( -- cookie64 )
    [: ticks 64dup o
	{ 64^ cookie-adder cookie-o }
	cookie-adder cookie-size#  cookies $+! ;]
    resize-sema c-section ;

: do-?cookie ( cookie -- context true / false )
    ticker 64@ connect-timeout# 64- { 64: timeout }
    cookies $@ bounds ?DO
	64dup I .cc-timeout 64@ 64= IF \ if we have a hit, use that
	    64drop I .cc-context @
	    cookies I cookie-size# del$one drop
	    unloop  true  EXIT
	THEN
	I .cc-timeout 64@ timeout 64u< IF
	    cookies I cookie-size# del$one
	    cookies next$
	    unloop  ?DO  NOPE \ this replaces the loop variables
	    0 \ we re-iterate over the exactly same index
	ELSE
	    cookie-size#
	THEN
    +LOOP  64drop 0 ;

: ?cookie ( cookie -- context true / false )
    ['] do-?cookie resize-sema c-section ;

: cookie>context? ( cookie -- context true / false )
    ?cookie over 0= over and IF
	nip net2o:new-context swap
    THEN ;

: adjust-ticks ( time -- )  o 0= IF  64drop  EXIT  THEN
    recv-tick 64@ 64- rtdelay 64@ 64dup 64-0<> >r 64-2/
    64over 64abs 64over 64> r> and IF
	64+ adjust-timer( ." adjust timer: " 64dup u64. forth:cr )
	tick-adjust 64!
    ELSE
	64+ adjust-timer( ." don't adjust timer: " 64dup u64. forth:cr )
	64drop  THEN ;

\ load net2o plugins: first one with integraded command space

require notify.fs
require cmd.fs
require connect.fs
require connected.fs
require log.fs
require keys.fs
require addr.fs
require dht.fs
require vault.fs
require msg.fs
require helper.fs
require qr.fs
\ require term.fs
require dvcs.fs
require squid.fs

\ configuration stuff

require dhtroot.fs \ configuration for DHT root

\ freeze tables

context-table   $save

\ show problems

.unresolved

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     (("64field:") non-immediate (font-lock-type-face . 2)
      "[ \t\n]" t name (font-lock-variable-name-face . 3))
     (("with") compile-only (font-lock-type-face . 2)
      "[ \t\n]" t name (font-lock-variable-name-face . 3))
     (("endwith") compile-only (font-lock-type-face . 2))
    )
forth-local-indent-words:
    (
    (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
    (("with") (0 . 2) (0 . 2) compile-only)
    (("endwith") (-2 . 0) (0 . -2) compile-only)
    )
End:
[THEN]
