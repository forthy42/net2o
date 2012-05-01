\ net2o protocol stack

\ Copyright (C) 2010,2011,2012   Bernd Paysan

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

require unix/socket.fs
require string.fs
require struct0x.fs
require nacl.fs
require wurstkessel.fs
require wurstkessel-init.fs
require hash-table.fs

\ helper words

: safe/string ( c-addr u n -- c-addr' u' )
\G protect /string against overflows.
    dup negate >r  dup 0> IF
        /string dup r> u>= IF  + 0  THEN
    ELSE
        /string dup r> u< IF  + 1+ -1  THEN
    THEN ;

: or!   ( x addr -- )   >r r@ @ or   r> ! ;
: xor!  ( x addr -- )   >r r@ @ xor  r> ! ;
: and!  ( x addr -- )   >r r@ @ and  r> ! ;
: min!  ( n addr -- )   >r r@ @ min  r> ! ;
: max!  ( n addr -- )   >r r@ @ max  r> ! ;
: umin! ( n addr -- )   >r r@ @ umin r> ! ;
: umax! ( n addr -- )   >r r@ @ umax r> ! ;

: !@ ( value addr -- old-value )   dup @ >r ! r> ;
: max!@ ( n addr -- )   >r r@ @ max r> !@ ;

\ bit vectors, lsb first

: bits ( n -- n ) 1 swap lshift ;

: >bit ( addr n -- c-addr mask ) 8 /mod rot + swap bits ;
: +bit ( addr n -- )  >bit over c@ or swap c! ;
: +bit@ ( addr n -- flag )  >bit over c@ 2dup and >r
    or swap c! r> 0<> ;
: -bit ( addr n -- )  >bit invert over c@ and swap c! ;
: -bit@ ( addr n -- flag )  >bit over c@ 2dup and >r
    invert or invert swap c! r> 0<> ;
: bit! ( flag addr n -- ) rot IF  +bit  ELSE  -bit  THEN ;
: bit@ ( addr n -- flag )  >bit swap c@ and 0<> ;

\ variable length integers

: p@+ ( addr -- u addr' )  >r 0
    BEGIN  7 lshift r@ c@ $7F and or r@ c@ $80 and  WHILE
	    r> 1+ >r  REPEAT  r> 1+ ;
: p-size ( u -- n ) \ to speed up: binary tree comparison
    \ flag IF  1  ELSE  2  THEN  equals  flag 2 +
    dup    $FFFFFFFFFFFFFF u<= IF
	dup       $FFFFFFF u<= IF
	    dup      $3FFF u<= IF
		$00000007F u<= 2 +  EXIT  THEN
	    $00000001FFFFF u<= 4 +  EXIT  THEN
	dup   $3FFFFFFFFFF u<= IF
	    $00007FFFFFFFF u<= 6 +  EXIT  THEN
	$00001FFFFFFFFFFFF u<= 8 +  EXIT  THEN
    $000007FFFFFFFFFFFFFFF u<= 10 + ;
: p!+ ( u addr -- addr' )  over p-size + dup >r >r
    dup $7F and r> 1- dup >r c!  7 rshift
    BEGIN  dup  WHILE  dup $7F and $80 or r> 1- dup >r c! 7 rshift  REPEAT
    drop rdrop r> ;

\ bit reversing

: bitreverse8 ( u1 -- u2 )
    0 8 0 DO  2* over 1 and + swap 2/ swap  LOOP  nip ;

Create reverse-table $100 0 [DO] [I] bitreverse8 c, [LOOP]

: reverse8 ( c1 -- c2 ) reverse-table + c@ ;
: reverse64 ( x1 -- x2 )
    0 8 0 DO  8 lshift over $FF and reverse8 or
	swap 8 rshift swap  LOOP  nip ;

\ timing ticks

: ticks ( -- u )  ntime drop ;

\ debugging aids

: debug)  ]] THEN [[ ;

false [IF]
    : debug: ( -- ) Create immediate false ,  DOES>
	]] Literal @ IF [[ ['] debug) assert-canary ;
[ELSE]
    : debug: ( -- )  Create immediate false , DOES>
	@ IF  ['] noop assert-canary  ELSE  postpone (  THEN ;
[THEN]

: x~~ ]] base @ >r hex ~~ r> base ! [[ ; immediate

\ defined exceptions

s" gap in file handles"          exception Constant !!gap!!
s" invalid file id"              exception Constant !!fileid!!
s" could not send"               exception Constant !!send!!
s" wrong packet size"            exception Constant !!size!!
s" no power of two"              exception Constant !!pow2!!
s" unimplemented net2o function" exception Constant !!function!!
s" too many commands"            exception Constant !!commands!!
s" string does not fit"          exception Constant !!stringfit!!
s" ivs must be 64 bytes"         exception Constant !!ivs!!
s" key+pubkey must be 32 bytes"  exception Constant !!keysize!!

\ this is already defined in assertions

debug: timing(
debug: rate(
debug: ratex(
debug: deltat(
debug: slack(
debug: slk(
debug: bursts(
debug: resend(
debug: track(
debug: cmd(
debug: send(
debug: firstack(
debug: msg(

: +db ( "word" -- ) ' >body on ;

\ +db bursts(
\ +db rate(
\ +db ratex(
\ +db slack(
\ +db timing(
\ +db deltat(
\ +db resend(
\ +db track(
\ +db cmd(
\ +db send(
\ +db firstack(
\ +db msg(

\ Create udp socket

4242 Value net2o-port

0 Value net2o-sock
0 Value net2o-sock6

true Value sock46 immediate

: new-server ( -- )
    sock46 [IF]
	net2o-port create-udp-server46 s" w+" c-string fdopen
	dup to net2o-sock to net2o-sock6
    [ELSE]
	net2o-port create-udp-server s" w+" c-string fdopen to net2o-sock
	net2o-port create-udp-server6 s" w+" c-string fdopen to net2o-sock6
    [THEN] ;

: new-client ( -- )
    sock46 [IF]
	new-udp-socket46 s" w+" c-string fdopen
	dup to net2o-sock to net2o-sock6
    [ELSE]
	new-udp-socket s" w+" c-string fdopen to net2o-sock
	new-udp-socket6 s" w+" c-string fdopen to net2o-sock6
    [THEN] ;

$2A Constant overhead \ constant overhead
$4 Value max-size^2 \ 1k, don't fragment by default
$40 Constant min-size
$400000 Value max-data#
$10000 Value max-code#
[IFDEF] recvmmsg 8 [ELSE] 1 [THEN] Value buffers#
: maxdata ( -- n ) min-size max-size^2 lshift ;
maxdata overhead + Constant maxpacket
maxpacket $F + -$10 and Constant maxpacket-aligned
: chunk-p2 ( -- n )  max-size^2 6 + ;

here 1+ -8 and 6 + here - allot
here maxpacket-aligned buffers# * allot
here maxpacket-aligned buffers# * allot
Constant outbuf' Constant inbuf'

begin-structure net2o-header
    2 +field flags
   16 +field destination
    8 +field addr
end-structure

Variable packet4r
Variable packet4s
Variable packet6r
Variable packet6s

2Variable ptimeout
#100000000 Value poll-timeout# \ 100ms
poll-timeout# 0 ptimeout 2!

[IFDEF] recvmmsg
    iovec   %size     buffers# * buffer: iovecbuf
    mmsghdr %size     buffers# * buffer: hdr
    sockaddr_in %size buffers# * buffer: sockaddrs

    : setup-iov ( -- )
	inbuf'  iovecbuf iovec %size buffers# * bounds ?DO
	    dup I iov_base !  maxpacket I iov_len !  maxpacket-aligned +
	iovec %size +LOOP  drop ;
    setup-iov

    : setup-msg ( -- )
	iovecbuf sockaddrs  hdr mmsghdr %size buffers# * bounds ?DO
	    over              I msg_iov !
	    1                 I msg_iovlen !
	    dup               I msg_name !
	    sockaddr_in %size I msg_namelen !
	    swap iovec %size + swap sockaddr_in %size +
	mmsghdr %size +LOOP  2drop ;
    setup-msg
    
    : timeout-init ( -- ) 	poll-timeout# 0 ptimeout 2! ;
    2Variable socktimeout

    Variable read-remain
    Variable read-ptr
    Variable write-ptr
    : rd[] ( base size -- addr )  read-ptr @ * + ;
    : wr[] ( base size -- addr )  write-ptr @ * + ;
    : inbuf  ( -- addr ) inbuf'  maxpacket-aligned rd[] ;
    : outbuf ( -- addr ) outbuf' maxpacket-aligned wr[] ;

    : sock@ ( -- addr u )
	inbuf hdr mmsghdr %size rd[] msg_len @
	sockaddrs sockaddr_in %size rd[]
	sockaddr-tmp hdr mmsghdr %size rd[]
	msg_namelen @ dup alen ! move ;

    : sock-timeout! ( fid -- )
	ptimeout 2@ >r 1000 / r> socktimeout 2!
	SOL_SOCKET SO_RCVTIMEO socktimeout 2 cells setsockopt drop ;
    
    : read-socket-quick ( socket -- addr u )  fileno
	1 read-ptr +!
	read-remain @ read-ptr @ u>  IF  drop sock@  EXIT  THEN
	dup sock-timeout!
	hdr buffers# MSG_WAITFORONE ( MSG_WAITALL or ) ptimeout recvmmsg
	dup 0< IF
	    errno 11 <> IF  errno 512 + negate throw  THEN
	    drop 0 0  EXIT  THEN
	dup read-remain !  0 read-ptr !
	0= IF  0 0  ELSE  sock@  THEN ;
[ELSE]
    inbuf'  Constant inbuf
    outbuf' Constant outbuf
    : read-socket-quick ( socket -- addr u )
	fileno inbuf maxpacket MSG_WAITALL sockaddr-tmp alen recvfrom
	dup 0< IF  errno 512 + negate throw  THEN
	inbuf swap ;
[THEN]

: read-a-packet ( -- addr u )
    net2o-sock read-socket-quick  1 packet4r +! ;

: read-a-packet6 ( -- addr u )
    net2o-sock6 read-socket-quick  1 packet6r +! ;

$00000000 Value droprate#

[IFDEF] sendmmsg-
[ELSE]
    : send-a-packet ( addr u -- n )
	droprate# IF  rng32 droprate# u< IF
		\ ." dropping packet" cr
		2drop 0  EXIT  THEN  THEN
	sock46 [IF]
	    net2o-sock  1 packet4s +!
	[ELSE]
	    sockaddr-tmp w@ AF_INET6 = IF
		net2o-sock6  1 packet6s +!
	    ELSE
		net2o-sock  1 packet4s +!
	    THEN
	[THEN]
	fileno -rot 0 sockaddr-tmp alen @ sendto ;
    : send-flush ( -- ) ;
[THEN]

\ clients routing table

Variable routes

: init-route ( -- )  s" " routes hash@ $! ; \ field 0 is me, myself

: info>string ( addr -- addr u )
    dup ai_addr @ swap ai_addrlen l@
    sock46 [IF]
	over w@ AF_INET = IF
	    drop >r
	    AF_INET6 sockaddr-tmp family w!
	    r@ port w@ sockaddr-tmp port w!
	    0     sockaddr-tmp sin6_flowinfo l!
	    r> sin_addr l@ sockaddr-tmp sin6_addr 12 + l!
	    $FFFF0000 sockaddr-tmp sin6_addr 8 + l!
	    0 sockaddr-tmp sin6_addr !
	    0 sockaddr-tmp sin6_scope_id l!
	    sockaddr-tmp sockaddr_in6 %size
	THEN
    [THEN] ;

: check-address ( addr u -- net2o-addr / -1 ) routes #key ;
: insert-address ( addr u -- net2o-addr )
    2dup routes #key dup -1 = IF
	drop s" " 2over routes #! routes #key
    ELSE
	nip nip
    THEN ;

: insert-ip ( addr u port -- net2o-addr )
    get-info info>string insert-address ;

: address>route ( -- n/-1 )
    sockaddr-tmp alen @ check-address ;
: route>address ( n -- )
    routes #.key $@ sockaddr-tmp swap dup alen ! move ;

\ route an incoming packet

Variable return-addr

\ these are all stubs for now

: ins-source ( addr packet -- )  >r
    reverse64 0  r> destination 2! ;
: get-source ( packet -- addr )
    destination 2@ drop  reverse64 ;
: ins-dest ( addr packet -- )  0 -rot destination 2! ;
: get-dest ( packet -- addr )  destination 2@ nip ;

: packet-route ( orig-addr addr -- flag ) >r
    r@ get-dest $38 rshift 0=  IF  drop  true  rdrop EXIT  THEN \ local packet
    r@ get-dest route>address  r> ins-source  false ;

: in-route ( -- flag )  address>route inbuf packet-route ;
: in-check ( -- flag )  address>route -1 <> ;
: out-route ( -- flag )  0  outbuf packet-route ;

\ packet&header size

$C0 Constant headersize#
$00 Constant 16bit#
$40 Constant 64bit#
$0F Constant datasize#

Create header-sizes  $06 c, $1a c, $FF c, $FF c,
Create tail-sizes    $00 c, $10 c, $FF c, $FF c,
\ we don't know the header sizes of protocols 2 and 3 yet ;-)

: header-size ( addr -- n )  c@ 6 rshift header-sizes + c@ ;
: tail-size ( addr -- n )  c@ 6 rshift tail-sizes + c@ ;
: body-size ( addr -- n ) min-size swap c@ datasize# and lshift ;
: packet-size ( addr -- n )
    dup header-size over body-size + swap tail-size + ;
: packet-body ( addr -- addr )
    dup header-size + ;
: packet-data ( addr -- addr u )
    >r r@ header-size r@ + r> body-size ;

\ second byte constants

$40 Constant multicasting#
$80 Constant broadcasting#

$00 Constant qos0#
$10 Constant qos1#
$20 Constant qos2#
$30 Constant qos3#

$0F Constant acks#
$01 Constant ack-toggle#
$02 Constant b2b-toggle#
$04 Constant resend-toggle#

\ short packet information

: chunk@ ( addr flag -- value addr' )
    IF  dup @ swap 8 +  ELSE  dup w@ swap 2 +  THEN ;

: .header ( addr -- )
    dup c@ >r 2 +
    r@ datasize# and 'A' + emit
    r@ headersize# and chunk@
    r@ headersize# and chunk@
    drop rdrop swap
    ."  to " hex. ."  @ " hex. ." from " return-addr @ hex. cr ;

\ packet delivery table

0 Value j^

\ each source has multiple destination spaces

Variable dest-addr

: >ret-addr ( -- )
    inbuf get-source return-addr ! ;
: >dest-addr ( -- )
    inbuf addr @  inbuf body-size 1- invert and dest-addr ! ;

begin-structure dest-struct
field: dest-size
field: dest-vaddr
field: dest-raddr
field: dest-job
field: dest-round
field: dest-ivs
field: dest-ivsgen
field: dest-ivslastgen
field: dest-timestamps
field: dest-tail
end-structure

dest-struct extend-structure code-struct
field: code-flag
end-structure

dest-struct extend-structure data-struct
field: data-head
end-structure

code-struct extend-structure rdata-struct
field: data-ackbits0
field: data-ackbits1
field: data-firstack0#
field: data-firstack1#
field: data-lastack#
end-structure
\ job context structure

begin-structure context-struct
field: context#
field: return-address
field: recv-tick
field: recv-addr
field: recv-flag
field: recv-high
field: cmd-buf#
field: file-state
field: blocksize
field: blockalign
field: crypto-key

field: data-map
field: data-rmap
field: data-resend
field: data-b2b

field: code-map
field: code-rmap

field: ack-state
field: ack-receive
\ flow control, sender part
field: min-slack
field: ns/burst
field: last-ns/burst
field: bandwidth-tick \ ns
field: next-tick \ ns
field: rtdelay \ ns
field: lastack \ ns
field: flyburst
field: flybursts
\ flow control, receiver part
field: burst-ticks
field: firstb-ticks
field: lastb-ticks
field: delta-ticks
field: acks
field: last-rate
\ state machine
field: expected
field: total
field: received
end-structure

begin-structure timestamp
field: ts-ticks
end-structure

begin-structure reply
field: reply-offset
field: reply-len
end-structure

\ check for valid destination

Variable dest-map s" " dest-map $!

: check-dest ( -- addr 1/t / f )
    \G return false if invalid destination
    \G return 1 if code, -1 if data, plus destination address
    0 to j^
\    return-addr @ routes #.key dup 0= IF  drop false  EXIT  THEN  cell+
    dest-map
    $@ bounds ?DO
	I @ 2@ 1- bounds dest-addr @ within
	0= IF
	    I @ dest-vaddr 2@ dest-addr @ swap - +
	    I @ code-flag @ IF  1  ELSE  -1  THEN
	    I @ dest-job @ to j^
	    return-addr @ dup j^ return-address !@ <>
	    IF  msg( ." handover" cr )  THEN
	    UNLOOP  EXIT  THEN
    cell +LOOP
    false ;

\ context debugging

: .j ( -- ) j^ context# ? ;

\ Destination mapping contains
\ addr u - range of virtal addresses
\ addr' - real start address
\ context - for exec regions, this is the job context

Create dest-mapping    here rdata-struct dup allot erase
dest-mapping code-flag Constant >code-flag

Create source-mapping  here data-struct dup allot erase
Variable mapping-addr

: addr>bits ( addr -- bits )
    chunk-p2 rshift ;
: bits>bytes ( bits -- bytes )
    1- 2/ 2/ 2/ 1+ ;
: addr>ts ( addr -- ts-offset )
    addr>bits timestamp * ;
: addr>replies ( addr -- replies )
    addr>bits reply * ;

: allocatez ( size -- addr )
    dup >r allocate throw dup r> erase ;
: allocateFF ( size -- addr )
    dup >r allocate throw dup r> -1 fill ;

: map-string ( addr u addrx -- addrx u2 )
    >r tuck r@ dest-size 2!
    dup allocatez r@ dest-raddr !
    state# 2* allocatez r@ dest-ivsgen !
    >code-flag @ IF
	dup addr>replies allocatez r@ dest-timestamps !
    ELSE
	dup addr>ts allocatez r@ dest-timestamps !
	dup addr>bits bits>bytes allocateFF r@ data-ackbits0 !
	dup addr>bits bits>bytes allocateFF r@ data-ackbits1 !
    THEN
    r@ data-lastack# on
    drop
    j^ r@ dest-job !
    r> rdata-struct ;

: map-source-string ( addr u addrx -- addrx u2 )
    >r tuck r@ dest-size 2!
    dup allocatez r@ dest-raddr !
    state# 2* allocatez r@ dest-ivsgen !
    dup >code-flag @ IF  addr>replies  ELSE  addr>ts  THEN
    allocatez r@ dest-timestamps !
    drop
    j^ r@ dest-job !
    r> code-struct ;

: map-dest ( vaddr u addr -- )
\    return-addr @ routes #.key cell+ >r  r@ @ 0= IF  s" " r@ $!  THEN
    dest-map >r
    >r  dest-mapping map-string  r@ $!
    r> $@ drop mapping-addr tuck ! cell r> $+! ;

: map-source ( addr u -- addr u )
    source-mapping map-source-string drop data-struct ;

Variable mapstart $10000 mapstart !

: n2o:new-map ( u -- addr )  mapstart @ swap mapstart +! ; 
: n2o:new-data ( addrs addrd u -- )  >code-flag off
    tuck  j^ data-rmap map-dest  map-source  j^ data-map $! ;
: n2o:new-code ( addrs addrd u -- )  >code-flag on
    tuck  j^ code-rmap map-dest  map-source  j^ code-map $! ;

\ create context

8 Value b2b-chunk#
b2b-chunk# 2* 2* 1- Value tick-init \ ticks without ack
#1000000 max-size^2 lshift Value bandwidth-init \ 32µs/burst=2MB/s
-1 Constant never
-1 1 rshift Constant max-int64
2 Value flybursts#

Variable init-context#

: n2o:new-context ( addr -- )
    context-struct allocate throw to j^
    j^ context-struct erase
    init-context# @ j^ context# !  1 init-context# +!
    dup return-addr !  j^ return-address !
    s" " j^ data-resend $!
    wurst-key state# j^ crypto-key $!
    max-int64 2/ j^ min-slack !
    max-int64 j^ rtdelay !
    flybursts# dup j^ flybursts ! j^ flyburst !
    ticks j^ lastack ! \ asking for context creation is as good as an ack
    bandwidth-init j^ ns/burst !
    never          j^ next-tick !
    -1 j^ blocksize !
    1 j^ blockalign ! ;

: data$@ ( -- addr u )
    j^ data-map $@ drop >r
    r@ dest-raddr @  r@ dest-size @ r> data-head @ safe/string ;
: /data ( u -- )
    j^ data-map $@ drop data-head +! ;
: dest-tail$@ ( -- addr u )
    j^ data-map $@ drop >r
    r@ dest-raddr @  r@ data-head @ r> dest-tail @ safe/string ;
: /dest-tail ( u -- )
    j^ data-map $@ drop dest-tail +! ;
: data-dest ( -- addr )
    j^ data-map $@ drop >r
    r@ dest-vaddr @ r> dest-tail @ + ;

\ code sending around

: code-dest ( -- addr )
    j^ code-map $@ drop >r
    r@ dest-raddr @ r> dest-tail @ + ;

: code-vdest ( -- addr )
    j^ code-map $@ drop >r
    r@ dest-vaddr @ r> dest-tail @ + ;

: code-reply ( -- addr )
    j^ code-map $@ drop >r
    r@ dest-tail @ addr>replies r> dest-timestamps @ + ;

reply buffer: dummy-reply

: reply[] ( index -- addr )
    j^ code-map $@ drop >r
    dup r@ dest-size @ addr>bits u<
    IF  reply * r@ dest-timestamps @ +  ELSE  dummy-reply  THEN  rdrop ;

: reply-index ( -- index )
    j^ code-map $@ drop dest-tail @ addr>bits ;

: code+ ( -- )
    j^ code-map $@ drop >r
    maxdata r@ dest-tail +!
    r@ dest-tail @ r@ dest-size @ u>= IF  r@ dest-tail off  THEN
\    cmd( ." set dest-tail to " r@ dest-tail @ hex. cr )
    rdrop ;

\ flow control

: ticks-init ( ticks -- )
    dup j^ bandwidth-tick !  j^ next-tick ! ;

Variable lastdiff
Variable lastdeltat

: timestat ( client serv -- )
    timing( over . dup . ." acktime" cr )
    ticks
    j^ flyburst @ j^ flybursts max!@ \ reset bursts in flight
    0= IF  dup ticks-init  bursts( .j ." restart bursts " j^ flybursts ? cr )  THEN
    dup j^ lastack !
    over - j^ rtdelay min!
    - dup lastdiff !
    lastdeltat @ 8 rshift j^ min-slack +!
    j^ min-slack min! ;

: net2o:ack-addrtime ( addr ticks -- )  swap
    j^ 0= IF  2drop EXIT  THEN
    j^ data-map @ 0= IF  2drop  EXIT  THEN
    j^ data-map $@ drop >r
    r@ dest-vaddr @ -
    timing( over . dup . ." addrtick" cr )
    dup r@ dest-size @ u<
    IF  addr>ts r> dest-timestamps @
	over tick-init 1+ timestamp * - 0>
	IF  + dup ts-ticks @
	    over tick-init 1+ timestamp * - ts-ticks @ - lastdeltat !
	ELSE  +  THEN 
	ts-ticks @ timestat
    ELSE  2drop rdrop  THEN ;

#3000000 Value slack# \ 3ms slack leads to backdrop of factor 2

: net2o:set-flyburst ( -- bursts )
    j^ rtdelay @ j^ ns/burst @ / flybursts# +
    bursts( dup . .j ." flybursts" cr ) dup j^ flyburst ! ;
: net2o:max-flyburst ( bursts -- ) j^ flybursts max!@
    0= IF  bursts( .j ." start bursts" cr ) THEN ;

: net2o:set-rate ( rate deltat -- )
    rate( over . .j ." clientrate" cr )
    deltat( dup . lastdeltat ? .j ." deltat" cr )
    dup 0<> lastdeltat @ 0<> and
    IF  over >r
	lastdeltat @ over max swap 2dup 2>r */ 2r> */
	r> 2* min \ no more than a factor two!
    ELSE  drop  THEN
    rate( dup . .j ." clientavg" cr )
    \ negative rate means packet reordering
    lastdiff @ j^ min-slack @ - slack( dup . j^ min-slack ? .j ." slack" cr )
    0 max slack# 2* 2* min slack# / lshift
    j^ last-ns/burst @  ?dup-IF  2* 2* umin  THEN \ not too quickly go slower!
    dup j^ last-ns/burst !
    rate( dup . .j ." rate" cr )
    j^ ns/burst !@ >r
    r> bandwidth-init = IF \ first acknowledge
	net2o:set-flyburst
	net2o:max-flyburst
    THEN ;

\ acknowledge

Create resend-buf  0 , 0 ,
$20 Value mask-bits#
: >mask0 ( addr mask -- addr' mask' )
    BEGIN  dup 1 and 0= WHILE  2/ >r maxdata + r>  dup 0= UNTIL  THEN ;
: net2o:resend-mask ( addr mask -- ) 
    j^ data-resend $@ bounds ?DO
	over I cell+ @ swap dup maxdata mask-bits# * + within IF
	    over I 2@ rot >r
	    BEGIN  over r@ u>  WHILE  2* >r maxdata - r>  REPEAT
	    rdrop nip or >mask0 I 2!  UNLOOP  EXIT
	THEN
    2 cells +LOOP
    >mask0 resend-buf 2!
    resend( ." Resend-mask: " resend-buf 2@ swap hex. hex. cr )
    resend-buf 2 cells j^ data-resend $+! ;
: net2o:ack-resend ( flag -- )  resend-toggle# and
    j^ ack-state @ resend-toggle# invert and or j^ ack-state ! ;
: >real-range ( addr -- addr' )
    j^ data-map $@ drop >r r@ dest-vaddr @ - r> dest-raddr @ + ;
: resend$@ ( -- addr u )
    j^ data-resend $@  IF
	2@ 1 and IF  maxdata  ELSE  0  THEN
	swap >real-range swap
    ELSE  drop 0 0  THEN ;

: resend-dest ( -- addr )
    j^ data-resend $@ drop 2@ drop ;
: /resend ( u -- )
    0 +DO  j^ data-resend $@ 0= IF  drop  LEAVE  THEN
	dup >r 2@ -2 and >mask0  dup 0= IF
	    2drop j^ data-resend 0 2 cells $del
	ELSE
	    r@ 2!
	THEN  rdrop
    maxdata +LOOP ;

\ file handling

: ?nogap ( flag -- )  !!gap!! and throw ;

\ file states

begin-structure file-state-struct
field: fs-size
field: fs-seek
field: fs-oldseek
field: fs-fid
end-structure

file-state-struct buffer: new-file-state

: ?state ( -- )
    j^ file-state @ 0= IF  s" " j^ file-state $!  THEN ;

: id>addr ( id -- addr remainder )  ?state
    >r j^ file-state $@ r> file-state-struct * /string ;
: id>addr? ( id -- addr )
    id>addr file-state-struct < !!fileid!! and throw ;
: state-addr ( id -- addr )  ?state
    id>addr dup 0< ?nogap
    0= IF  drop new-file-state file-state-struct j^ file-state $+!
	j^ file-state $@ + file-state-struct -  THEN ;

: +expected ( n -- ) j^ expected @ tuck + dup j^ expected !
    j^ data-rmap $@ drop >r r@ data-ackbits0 2@  2swap
    maxdata 1- + chunk-p2 rshift 1+ swap chunk-p2 rshift +DO
	dup I -bit  over I -bit  LOOP  2drop
    r@ data-firstack0# off  r> data-firstack1# off
    firstack( ." expect more data" cr ) ;

: size! ( n id -- )  over j^ total    +!  state-addr  fs-size ! ;
: seek! ( n id -- )  over >r state-addr  fs-seek !@ r> swap -
    +expected ;

: size@ ( id -- n )  state-addr  fs-size @ ;
: seek@ ( id -- n )  state-addr  fs-seek @ ;

: >blockalign ( n -- block )
    j^ blockalign @ dup >r 1- + r> negate and ;

: save-blocks ( -- ) ?state
    j^ data-rmap $@ drop >r r@ dest-raddr @ r@ dest-tail @ +
    j^ file-state $@ bounds ?DO
	I fs-seek @ I fs-oldseek @ 2dup = IF  2drop
	ELSE
	    - j^ blocksize @ umin dup I fs-oldseek +!
	    msg( ." flush file <" I j^ file-state $@ drop - file-state-struct / 0 .r ." >: " dup . cr )
	    I fs-fid @ IF
		2dup I fs-fid @ write-file throw
	    THEN  >blockalign +
	THEN
    file-state-struct +LOOP
    r@ dest-raddr @ - r> dest-tail ! ;

: save-all-blocks ( -- )  j^ data-rmap $@ drop >r 
    BEGIN
	r@ dest-tail @ >r  save-blocks  r>
	r@ dest-tail @ =  UNTIL  rdrop ;

: save-to ( addr u n -- )  state-addr >r
    r/w create-file throw r> fs-fid ! ;

\ open a file - this needs *way more checking*!

: id>file ( id -- fid )  id>addr? fs-fid @ ;

: n2o:open-file ( addr u mode id -- )
    ?state  state-addr >r
    r@ fs-fid @ ?dup-IF  close-file throw  THEN
    msg( dup 2over ." open file: " type ."  with mode " . cr )
    open-file throw r@ fs-fid !
    r@ fs-fid @ file-size throw drop r@ fs-size !
    0. r> fs-seek 2! ;

: n2o:close-file ( id -- )
    ?state  id>addr?  fs-fid dup @ ?dup-IF  close-file throw  THEN  off ;

: n2o:slurp-block ( id -- nextseek )
    id>addr? >r r@ fs-seek @ dup 0 r@ fs-fid @ reposition-file throw
    data$@ j^ blocksize @ umin r@ fs-fid @ read-file throw
    dup >blockalign /data +
    dup r> fs-seek ! ;

: n2o:slurp-block' ( id -- delta )
    id>addr? >r r@ fs-seek @ dup 0 r@ fs-fid @ reposition-file throw
    data$@ j^ blocksize @ umin r@ fs-fid @ read-file throw
    dup >blockalign /data +
    dup r> fs-seek !@ - ;

: n2o:slurp-blocks-once ( idbits -- sum ) 0 { idbits sum }
    8 cells 0 DO
	1 I lshift idbits and IF
	    I n2o:slurp-block'  sum + to sum
	THEN
    LOOP  sum ;

: n2o:slurp-blocks ( idbits -- )
    BEGIN  data$@ nip  WHILE
	dup n2o:slurp-blocks-once  0= UNTIL  THEN
    drop ;

: n2o:slurp-all-blocks-once ( -- sum ) 0 { sum }
    0 j^ file-state $@ bounds DO
	dup n2o:slurp-block'  sum + to sum  1+
    file-state-struct +LOOP  drop sum ;

: n2o:slurp-all-blocks ( -- )
    BEGIN  data$@ nip  WHILE
	n2o:slurp-all-blocks-once  0= UNTIL  THEN ;

: n2o:track-seeks ( idbits xt -- ) { xt } ( i seeklen -- )
    8 cells 0 DO
	dup 1 and IF  I id>addr? fs-seek 2@ <> IF
		I dup id>addr? dup >r fs-seek @ dup r> fs-oldseek !
		xt execute  THEN
	THEN  2/
    LOOP  drop ;

: n2o:track-all-seeks ( xt -- ) { xt } ( i seeklen -- )
    j^ file-state $@len file-state-struct / 0 DO
	I id>addr? fs-seek 2@ <> IF
	    I dup id>addr? dup >r fs-seek @ dup r> fs-oldseek !
	    xt execute  THEN
    LOOP ;

include net2o-crypt.fs

\ send blocks of memory

: set-dest ( addr target -- )
    outbuf ins-dest  dup dest-addr !  outbuf addr ! ;

Variable outflag  outflag off

: set-flags ( -- )
    outflag @ outbuf 1+ c! outflag off ;

: c+!  ( n addr -- )  dup >r c@ + r> c! ;

: outbody ( -- addr ) outbuf packet-body ;
: outsize ( -- n )    outbuf packet-size ;

#90 Constant EMSGSIZE

Variable code-packet

: send-packet ( flag -- )
\    ." send " outbuf .header
    code-packet @ wurst-outbuf-encrypt  code-packet off
    out-route drop
    outbuf dup packet-size
    send-a-packet 0< IF
	errno EMSGSIZE = IF
	    max-size^2 1- to max-size^2  ." pmtu/2" cr
	ELSE
	    errno 512 + negate throw
	THEN
    THEN ;

: >send ( addr n -- )  >r  r@ 64bit# or outbuf c!
    outbody min-size r> lshift move ;

: bandwidth+ ( -- )  j^ 0= ?EXIT
    j^ ns/burst @ tick-init 1+ / j^ bandwidth-tick +! ;

: burst-end ( -- )  j^ data-b2b @ ?EXIT
    ticks j^ bandwidth-tick @ umax j^ next-tick ! ;

: sendX ( addr taddr target n -- )
    >r set-dest  r> >send  set-flags  bandwidth+  send-packet
    net2o:update-key ;

\ send chunk

: net2o:get-dest ( -- taddr target )
    data-dest j^ return-address @ ;
: net2o:get-resend ( -- taddr target )
    resend-dest j^ return-address @ ;

: send-size ( u -- n )
    0 max-size^2 DO
	dup min-size 2/ I lshift u>= IF
	    drop I  UNLOOP  EXIT
	THEN
    -1 +LOOP
    drop 0 ;

: ts-ticks! ( addr map -- )
    >r addr>ts r> dest-timestamps @ + ticks swap ts-ticks ! ;

: net2o:send-tick ( addr -- )
    j^ data-map $@ drop >r
    r@ dest-raddr @ - dup r@ dest-size @ u<
    IF  r> ts-ticks!  ELSE  drop rdrop  THEN ;

: net2o:prep-send ( addr u dest addr -- addr taddr target n len )
    2>r  over  net2o:send-tick
    ( dup >r ) send-size min-size over lshift
    \ dup r> u>= IF  ack-toggle# outflag xor!  THEN
    2r> 2swap ;

: net2o:send-packet ( addr u dest addr -- len )
    net2o:prep-send >r sendX r> ;

\ synchronous sending

: data-to-send ( -- flag )
    resend$@ nip 0> dest-tail$@ nip 0> or ;

: net2o:send-chunk ( -- )
    j^ ack-state @ outflag or!
    resend$@ dup IF
	net2o:get-resend 2dup 2>r
	net2o:prep-send /resend
	2r> send( ." resending " over hex. dup hex. outflag @ hex. cr ) 2drop
    ELSE
	2drop
	dest-tail$@ net2o:get-dest 2dup 2>r
	net2o:prep-send /dest-tail
	2r> send( ." sending " over hex. dup hex. outflag @ hex. cr ) 2drop
    THEN
    data-to-send 0= IF
	resend-toggle# outflag xor!  ack-toggle# outflag xor!
	sendX  never j^ next-tick !
    ELSE  sendX  THEN ;

: bandwidth? ( -- flag )  ticks j^ next-tick @ - 0>=
    j^ flybursts @ 0> and  ;

\ asynchronous sending

begin-structure chunks-struct
field: chunk-context
field: chunk-count
end-structure

Variable chunks s" " chunks $!
Variable chunks+
Create chunk-adder chunks-struct allot

: net2o:send-chunks ( -- )
    chunks $@ bounds ?DO
	I chunk-context @ j^ = IF
	    UNLOOP  EXIT
	THEN
    chunks-struct +LOOP
    j^ chunk-adder chunk-context !
    0 chunk-adder chunk-count !
    chunk-adder chunks-struct chunks $+!
    ticks ticks-init ;

: chunk-count+ ( counter -- )
    dup @
    dup 0= IF
	ack-toggle# j^ ack-state xor!
	-1 j^ flybursts +!
	j^ flybursts @ 0<= IF
	    bursts( .j ." no bursts in flight " j^ ns/burst ? dest-tail$@ swap hex. hex. cr )
	THEN
    THEN
    tick-init = IF  off  ELSE  1 swap +!  THEN ;

: send-a-chunk ( chunk -- flag )  >r
    j^ data-b2b @ 0<= IF
	bandwidth? dup  IF
	    b2b-toggle# j^ ack-state xor!
	    b2b-chunk# 1- j^ data-b2b !
	THEN
    ELSE
	-1 j^ data-b2b +!  true
    THEN
    dup IF  r@ chunk-count+  net2o:send-chunk  burst-end  THEN
    rdrop  1 chunks+ +! ;

: .nosend ( -- ) ." done, "  4 set-precision
    .j ." rate: " j^ ns/burst @ s>f tick-init chunk-p2 lshift s>f 1e9 f* fswap f/ fe. cr
    .j ." slack: " j^ min-slack ? cr
    .j ." rtdelay: " j^ rtdelay ? cr ;

: send-chunks-async ( -- flag )
    chunks $@ chunks+ @ chunks-struct * safe/string
    IF
	dup chunk-context @ to j^
	chunk-count
	data-to-send IF
	    send-a-chunk
	ELSE
	    drop msg( .nosend )
	    chunks chunks+ @ chunks-struct * chunks-struct $del
	    false
	THEN
    ELSE  drop chunks+ off false  THEN ;

: next-chunk-tick ( -- tick )
    -1 chunks $@ bounds ?DO
	I chunk-context @ next-tick @ umin
    chunks-struct +LOOP ;

: send-another-chunk ( -- flag )  false  0 >r
    BEGIN  BEGIN  send-chunks-async  WHILE  drop rdrop true 0 >r  REPEAT
	    chunks+ @ 0= IF  r> 1+ >r  THEN
	r@ 2 u>=  UNTIL  rdrop ;

Variable sendflag  sendflag off
: send?  ( -- flag )  sendflag @ ;
: send-anything? ( -- flag )  chunks $@len 0> ;

\ rewind buffer to send further packets

: rewind-buffer ( map -- ) >r
    1 r@ dest-round +!
    r@ dest-tail off  r@ data-head off
    r> regen-ivs-all ;

: rewind-ackbits ( map -- ) >r
    r@ data-firstack0# off  r@ data-firstack1# off
    firstack( ." rewind firstacks" cr )
    r@ data-lastack# on
    r@ dest-size @ addr>bits bits>bytes
    r@ data-ackbits0 @ over -1 fill
    r> data-ackbits1 @ swap -1 fill ;

: net2o:rewind-sender ( n -- )
    j^ data-map $@ drop
    tuck dest-round @ +DO  dup rewind-buffer  LOOP  drop ;

: net2o:rewind-receiver ( -- )
    j^ recv-high on
    j^ data-rmap $@ drop
    tuck dest-round @ +DO  dup rewind-buffer  LOOP  rewind-ackbits ;

\ Variable timeslip  timeslip off
\ : send? ( -- flag )  timeslip @ chunks $@len 0> and dup 0= timeslip ! ;

\ schedule delayed events

begin-structure queue-struct
field: queue-timestamp
field: queue-job
field: queue-xt
end-structure

Variable queue s" " queue $!
Create queue-adder  queue-struct allot

: add-queue ( xt us -- )
    ticks +  queue-adder queue-timestamp !
    j^ queue-adder queue-job !
    queue-adder queue-xt !
    queue-adder queue-struct queue $+! ;

: eval-queue ( -- )
    queue $@len 0= ?EXIT  ticks
    queue $@ bounds ?DO
	dup I queue-timestamp @ u> IF
	    I queue-job @ to j^
	    I queue-xt @ execute
	    0 I queue-timestamp !
	THEN
    queue-struct +LOOP  drop
    0 >r BEGIN  r@ queue $@len u<  WHILE
	    queue $@ r@ safe/string drop queue-timestamp @ 0= IF
		queue r@ queue-struct $del
	    ELSE
		r> queue-struct + >r
	    THEN
    REPEAT  rdrop ;

\ poll loop

Create pollfds   here pollfd %size 2 * dup allot erase

: fds!+ ( fileno flag addr -- addr' )
     >r r@ events w!  r@ fd l!  r> pollfd %size + ; 

: prep-socks ( -- )  pollfds >r
    net2o-sock  fileno POLLIN  r> fds!+ >r
    net2o-sock6 fileno POLLIN  r> fds!+ drop ;

: clear-events ( -- )  pollfds
    2 0 DO  0 over revents w!  pollfd %size +  LOOP  drop ;

: timeout! ( -- )
    next-chunk-tick dup -1 <> >r ticks - dup 0>= r> or
    IF    0 max 0 ptimeout 2!
    ELSE  drop poll-timeout# 0 ptimeout 2!  THEN ;

: poll-sock ( -- flag )
    eval-queue  clear-events  timeout!
    pollfds 2  postpone sock46 +
[ environment os-type s" linux" string-prefix? ] [IF]
    ptimeout 0 ppoll 0>
[ELSE]
    ptimeout cell+ @ #1000000 / poll 0>
[THEN]
;

: read-a-packet4/6 ( -- addr u )
    sock46 [IF]
	pollfds revents w@ POLLIN = IF
	    read-a-packet6  0 pollfds revents w! EXIT  THEN
    [ELSE]
	pollfds revents w@ POLLIN = IF
	    read-a-packet   0 pollfds revents w! EXIT  THEN
	pollfds pollfd %size + revents w@ POLLIN = IF
	    read-a-packet6  0 pollfds pollfd %size + revents w! EXIT  THEN
    [THEN]
    0 0 ;

[IFDEF] recvmmsg
    : try-read-packet ( -- addr u / 0 0 )
	eval-queue  timeout!  read-a-packet ;
[ELSE]
    : try-read-packet ( -- addr u / 0 0 )
	poll-sock drop read-a-packet4/6 ;
[THEN]
    

: next-packet ( -- addr u )
    send-anything? sendflag !
    BEGIN  sendflag @ 0= IF  try-read-packet dup 0=  ELSE  0. true  THEN
    WHILE  2drop send-another-chunk sendflag !  REPEAT
    sockaddr-tmp alen @ insert-address  inbuf ins-source
    over packet-size over <> !!size!! and throw ;

: next-client-packet ( -- addr u )
    try-read-packet  2dup d0= ?EXIT
    sockaddr-tmp alen @ check-address dup -1 <> IF
	inbuf ins-source
	over packet-size over <> !!size!! and throw
    ELSE  hex.  ." Unknown source"  0 0  THEN ;

: net2o:timeout ( ticks -- ) \ print why there is nothing to send
    ." timeout? " . send-anything? . chunks+ ? next-chunk-tick . cr ;

Defer queue-command ( addr u -- )
' dump IS queue-command
Defer do-ack ( -- )
' noop IS do-ack

: pow2? ( n -- n )  dup dup 1- and 0<> !!pow2!! and throw ;

Variable validated

$01 Constant crypt-val
$02 Constant own-crypt-val
$04 Constant login-val

: handle-packet ( -- ) \ handle local packet
    >ret-addr >dest-addr
\    inbuf .header
    dest-addr @ 0= IF
	0 to j^ \ address 0 has no job context!
	true wurst-inbuf-decrypt 0= IF
	    inbuf' dup packet-size dump
	    inbuf dup packet-size dump
	    ." invalid packet to 0" cr EXIT  THEN
	validated off \ packets to address 0 are not really validated
	inbuf packet-data queue-command
    ELSE
	check-dest dup 0= IF  drop  EXIT  THEN
	dup 0> wurst-inbuf-decrypt 0= IF
	    inbuf .header
	    ." invalid packet to " dest-addr @ hex. cr
	    IF  drop  THEN  EXIT  THEN
	crypt-val validated ! \ ok, we have a validated connection
	dup 0< IF \ data packet
	    drop  >r inbuf packet-data r> swap move
	    do-ack
	ELSE \ command packet
	    drop
	    >r inbuf packet-data r@ swap dup >r move
	    r> r> swap queue-command
	THEN
    THEN ;

: route-packet ( -- )  inbuf dup packet-size send-a-packet drop ;

: server-event ( -- )
    next-packet 2drop  in-route
    IF  ['] handle-packet catch
	?dup-IF  ( inbuf packet-data dump ) DoError nothrow  THEN
    ELSE  ." route a packet" cr route-packet  THEN ;

: client-event ( addr u -- )
    2drop in-check
    IF  ['] handle-packet catch
	?dup-IF  ( inbuf packet-data dump ) DoError nothrow  THEN
    ELSE  ( drop packet )  THEN ;

\ loops for server and client

0 Value server?
Variable requests
Variable timeouts
: reset-timeout  20 timeouts ! ; \ 2s timeout

Defer do-timeout  ' noop IS do-timeout

: server-loop ( -- )  true to server?
    BEGIN  server-event  AGAIN ;

: client-loop ( requests -- )  requests !  reset-timeout  false to server?
    BEGIN  next-client-packet dup
	IF    client-event reset-timeout
	ELSE  2drop do-timeout -1 timeouts +!  THEN
     timeouts @ 0<=  requests @ 0= or  UNTIL ;

\ client/server initializer

: init-client ( -- )
    new-client init-route prep-socks ;

: init-server ( -- )
    new-server init-route prep-socks ;

\ load net2o commands

include net2o-cmd.fs
