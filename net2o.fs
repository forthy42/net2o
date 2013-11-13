\ net2o protocol stack

\ Copyright (C) 2010-2013   Bernd Paysan

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

\ defined exceptions

: throwcode ( addr u -- )  exception Create ,
    [: >body @ >r ]] IF [[ r> ]] literal throw THEN [[ ;] set-compiler
  DOES> ( flag -- ) @ and throw ;

s" gap in file handles"          throwcode !!gap!!
s" invalid file id"              throwcode !!fileid!!
s" could not send"               throwcode !!send!!
s" wrong packet size"            throwcode !!size!!
s" no power of two"              throwcode !!pow2!!
s" unimplemented net2o function" throwcode !!function!!
s" too many commands"            throwcode !!commands!!
s" string does not fit"          throwcode !!stringfit!!
s" ivs must be 64 bytes"         throwcode !!ivs!!
s" key+pubkey must be 32 bytes"  throwcode !!keysize!!
s" net2o timed out"              throwcode !!timeout!!
s" no key file"                  throwcode !!nokey!!
s" maximum nesting reached"      throwcode !!maxnest!!
s" nesting stack empty"          throwcode !!minnest!!
s" invalid DHT key"              throwcode !!no-dht-key!!
s" invalid Ed25519 key"          throwcode !!no-ed-key!!

\ required tools

\ require smartdots.fs
require 64bit.fs
require debugging.fs
require unix/socket.fs
require unix/mmap.fs
require unix/pthread.fs
require unix/filestat.fs
require string.fs
require struct0x.fs
require libkeccak.fs
keccak-o crypto-o !
\ require wurstkessel.fs
\ wurstkessel-o crypto-o !
require rng.fs
require ed25519-donna.fs
require hash-table.fs
require mini-oof2.fs

\ porting helper to mini-oof2

0 [IF]
Variable do-stackrel

Create o-sp 0 ,  DOES> @ do-stackrel @ 0= IF  o#+ [ 0 , ] THEN + ;
comp: >body @ do-stackrel @ IF  postpone lit+  ELSE  postpone o#+ THEN , ;

' o-sp to var-xt
: [o  do-stackrel @ do-stackrel off ; immediate
: o]  do-stackrel ! ;  immediate
do-stackrel off
[THEN]

\ helper words

: ?nextarg ( -- addr u noarg-flag )
    argc @ 1 > IF  next-arg false  ELSE  true  THEN ;

[IFUNDEF] safe/string
: safe/string ( c-addr u n -- c-addr' u' )
\G protect /string against overflows.
    dup negate >r  dup 0> IF
        /string dup r> u>= IF  + 0  THEN
    ELSE
        /string dup r> u< IF  + 1+ -1  THEN
    THEN ;
[THEN]

: or!   ( x addr -- )   >r r@ @ or   r> ! ;
: xor!  ( x addr -- )   >r r@ @ xor  r> ! ;
: xorc! ( x c-addr -- )   >r r@ c@ xor  r> c! ;
: and!  ( x addr -- )   >r r@ @ and  r> ! ;
: min!  ( n addr -- )   >r r@ @ min  r> ! ;
: max!  ( n addr -- )   >r r@ @ max  r> ! ;
: umin! ( n addr -- )   >r r@ @ umin r> ! ;
: umax! ( n addr -- )   >r r@ @ umax r> ! ;

: max!@ ( n addr -- )   >r r@ @ max r> !@ ;

[IFDEF] 64bit
    ' min! Alias 64min!
    ' max! Alias 64max!
    ' !@ Alias 64!@
[ELSE]
    : 64!@ ( value addr -- old-value )   >r r@ 64@ 64swap r> 64! ;
    : 64min! ( d addr -- )  >r r@ 64@ dmin r> 64! ;
    : 64max! ( d addr -- )  >r r@ 64@ dmax r> 64! ;
[THEN]

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

[IFDEF] 64bit
    : p@+ ( addr -- u64 addr' )  >r 0
	BEGIN  7 lshift r@ c@ $7F and or r@ c@ $80 and  WHILE
		r> 1+ >r  REPEAT  r> 1+ ;
    : p-size ( u64 -- n ) \ to speed up: binary tree comparison
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
    : p!+ ( u64 addr -- addr' )  over p-size + dup >r >r
	dup $7F and r> 1- dup >r c!  7 rshift
	BEGIN  dup  WHILE  dup $7F and $80 or r> 1- dup >r c! 7 rshift  REPEAT
	drop rdrop r> ;
[ELSE]
    : p@+ ( addr -- u64 addr' )  >r 0.
	BEGIN  7 64lshift r@ c@ $7F and 0 64or r@ c@ $80 and  WHILE
		r> 1+ >r  REPEAT  r> 1+ ;
    : p-size ( x64 -- n ) \ to speed up: binary tree comparison
	\ flag IF  1  ELSE  2  THEN  equals  flag 2 +
	2dup   $FFFFFFFFFFFFFF. du<= IF
	    2dup      $FFFFFFF. du<= IF
		2dup     $3FFF. du<= IF
		    $00000007F. du<= 2 +  EXIT  THEN
		$00000001FFFFF. du<= 4 +  EXIT  THEN
	    2dup  $3FFFFFFFFFF. du<= IF
		$00007FFFFFFFF. du<= 6 +  EXIT  THEN
	    $00001FFFFFFFFFFFF. du<= 8 +  EXIT  THEN
	$000007FFFFFFFFFFFFFFF. du<= 10 + ;
    : p!+ ( u64 addr -- addr' )  >r 2dup p-size r> + dup >r >r
	over $7F and r> 1- dup >r c!  7 64rshift
	BEGIN  2dup or  WHILE  over $7F and $80 or r> 1- dup >r c! 7 64rshift  REPEAT
	2drop rdrop r> ;
[THEN]

\ bit reversing

: bitreverse8 ( u1 -- u2 )
    0 8 0 DO  2* over 1 and + swap 2/ swap  LOOP  nip ;

Create reverse-table $100 0 [DO] [I] bitreverse8 c, [LOOP]

: reverse8 ( c1 -- c2 ) reverse-table + c@ ;
: reverse ( x1 -- x2 )
    0 cell 0 DO  8 lshift over $FF and reverse8 or
       swap 8 rshift swap  LOOP  nip ;
: reverse$ ( addr u -- )
    BEGIN
	over c@ reverse8 >r
	1- dup WHILE
	    2dup + dup c@ r> rot c! reverse8 >r over r> swap c!
	1 /string dup 0= UNTIL
    ELSE
	over r> swap c!
    THEN
    2drop ;

\ Create udp socket

4242 Value net2o-port

Variable net2o-host "net2o.de" net2o-host $!

0 Value net2o-sock

: new-server ( -- )
    net2o-port create-udp-server46 s" w+" c-string fdopen
    to net2o-sock ;

: new-client ( -- )
    new-udp-socket46 s" w+" c-string fdopen
    to net2o-sock ;

$2A Constant overhead \ constant overhead
$4 Value max-size^2 \ 1k, don't fragment by default
$40 Constant min-size
$400000 Value max-data#
$10000 Value max-code#
1 Value buffers#
: maxdata ( -- n ) min-size max-size^2 lshift ;
maxdata overhead + Constant maxpacket
maxpacket $F + -$10 and Constant maxpacket-aligned
: chunk-p2 ( -- n )  max-size^2 6 + ;
$10 Constant mykey-salt#

UValue inbuf    ( -- addr )
UValue outbuf   ( -- addr )
UValue cmd0buf  ( -- addr )
UValue init0buf ( -- addr )
UValue sockaddr ( -- addr )
User 'statbuf
: statbuf 'statbuf $@ drop ;

: init-statbuf ( -- ) 'statbuf off "" 'statbuf $! file-stat 'statbuf $!len ;

sema cmd0lock

: alloc-buf ( addr -- addr' )
    maxpacket-aligned buffers# * alloc+guard 6 + ;

: alloc-io ( -- )  alloc-buf to inbuf  alloc-buf to outbuf
    maxdata allocate throw to cmd0buf
    maxdata 2/ mykey-salt# + $10 + allocate throw to init0buf
    sockaddr_in6 %size dup allocate throw dup to sockaddr swap erase
    init-statbuf
;

: free-io ( -- )
    inbuf  maxpacket-aligned buffers# * free+guard
    outbuf maxpacket-aligned buffers# * free+guard
    cmd0buf free throw
    init0buf free throw
    sockaddr free throw
    'statbuf $off
;

alloc-io

begin-structure net2o-header
    2 +field flags
   16 +field destination
    8 +field addr
end-structure

Variable packetr
Variable packets
Variable packetr2 \ double received
Variable packets2 \ double send

: .packets ( -- )
    ." IP packets send/received: " packets ? packetr ? cr
    ." Duplets send/received: " packets2 ? packetr2 ? cr
    packets off packetr off packets2 off packetr2 off ;

User ptimeout  cell uallot drop
#10000000 Value poll-timeout# \ 10ms, don't sleep too long
poll-timeout# 0 ptimeout 2!

2Variable socktimeout

: sock-timeout! ( socket -- )  fileno
    socktimeout 2@
    ptimeout 2@ >r 1000 / r> 2dup socktimeout 2! d<> IF
	SOL_SOCKET SO_RCVTIMEO socktimeout 2 cells setsockopt THEN
    drop ;

MSG_WAITALL   Constant do-block
MSG_DONTWAIT  Constant don't-block

: read-a-packet ( blockage -- addr u / 0 0 )
    >r sockaddr_in6 %size alen !
    net2o-sock fileno inbuf maxpacket r> sockaddr alen recvfrom
    dup 0< IF
	errno dup 11 = IF  2drop 0. EXIT  THEN
	512 + negate throw  THEN
    inbuf swap  1 packetr +! ;

$00000000 Value droprate#

: send-a-packet ( addr u -- n ) +calc
    droprate# IF  rng32 droprate# u< IF
	    \ ." dropping packet" cr
	    2drop 0  EXIT  THEN  THEN
    net2o-sock  1 packets +!
    fileno -rot 0 sockaddr alen @ sendto +send ;

\ clients routing table

Variable routes

: init-route ( -- )  s" " routes hash@ $! ; \ field 0 is me, myself

: info>string ( addr -- addr u )
    dup ai_addr @ swap ai_addrlen l@
    over w@ AF_INET = IF
	drop >r
	AF_INET6 sockaddr family w!
	r@ port w@ sockaddr port w!
	0     sockaddr sin6_flowinfo l!
	r> sin_addr l@ sockaddr sin6_addr 12 + l!
	$FFFF0000 sockaddr sin6_addr 8 + l!
	0 sockaddr sin6_addr 4 + l!
	0 sockaddr sin6_addr l!
	0 sockaddr sin6_scope_id l!
	sockaddr sockaddr_in6 %size
    THEN ;

0 Value lastaddr
Variable lastn2oaddr

: .ipv6 ( addr u -- ) 
    drop dup sin6_addr $10 bounds DO
	I be-uw@ 0 .r ':' emit
    2 +LOOP
    sin6_port be-uw@ decimal 0 .r ;

: .ipv4 ( addr u -- )
    drop dup sin_addr 4 bounds DO
	I c@ 0 <# #s #> type I 1+ I' <> IF  '.' emit  THEN
    LOOP  ." :"
    port be-uw@ decimal 0 .r ;

: .address ( addr u -- )
	over w@ AF_INET6 = IF ['] .ipv6 $10  ELSE  ['] .ipv4 #10 THEN
	base-execute ; 

: insert-address ( addr u -- net2o-addr )
    address( ." Insert address " 2dup .address cr )
    lastaddr IF  2dup lastaddr over str=
	IF  2drop lastn2oaddr @  EXIT  THEN
    THEN
    2dup routes #key dup -1 = IF
	drop s" " 2over routes #!
	last# $@ drop to lastaddr
	routes #key  dup lastn2oaddr !
    ELSE
	nip nip
    THEN ;

: insert-ip ( addr u port -- net2o-addr )
    get-info info>string insert-address ;

: address>route ( -- n/-1 )
    sockaddr alen @ insert-address ;
: route>address ( n -- ) dup >r
    routes #.key dup 0= IF  ." no address: " r> hex. cr drop  EXIT  THEN
    $@ sockaddr swap dup alen ! move  rdrop ;

\ route an incoming packet

User return-addr

\ these are all stubs for now

: ins-source ( addr packet -- )  >r
    reverse 0  r> destination 2! ;
: get-source ( packet -- addr )
    destination 2@ drop  reverse ;
: ins-dest ( addr packet -- )  0 -rot destination 2! ;
: get-dest ( packet -- addr )  destination @ ;

: packet-route ( orig-addr addr -- flag ) >r
    r@ get-dest [IFDEF] 64bit $38 [ELSE] $18 [THEN] rshift
    0=  IF  drop  true  rdrop EXIT  THEN \ local packet
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

: .header ( addr -- ) base @ >r hex
    dup c@ >r
    min-size r> datasize# and lshift hex. ." bytes to "
    addr 64@ 64. cr
    r> base ! ;

\ each source has multiple destination spaces

64User dest-addr

: >ret-addr ( -- )
    inbuf get-source return-addr ! ;
: >dest-addr ( -- )
    inbuf addr 64@  inbuf body-size 1- invert n>64 64and dest-addr 64! ;

current-o

object class
    64field: dest-vaddr
    field: dest-size
    field: dest-raddr
    field: code-flag
    field: dest-job
    field: dest-ivs
    field: dest-ivsgen
    field: dest-ivslastgen
    field: dest-timestamps
    field: dest-replies
    field: dest-cookies
    field: dest-round \ going to be obsoleted
    \                   sender:                receiver:
    field: dest-head  \ read up to here        received some
    field: dest-tail  \ send from here         received all
    field: dest-back  \ flushed on destination flushed
    1 pthread-mutexes +field dest-lock
    method free-data
end-class code-class

code-class class
    field: data-ackbits0
    field: data-ackbits1
    field: data-ackbits-buf
    field: data-firstack0#
    field: data-firstack1#
    field: data-lastack#
end-class rdata-class

\ job context structure

object class
    field: context#
    field: wait-task
    field: return-address
    64field: recv-tick
    64field: recv-addr
    field: recv-flag
    field: file-state
    field: read-file#
    field: write-file#
    field: residualread
    field: residualwrite
    field: blocksize
    field: blockalign
    field: crypto-key
    field: pubkey
    field: timeout-xt
    field: ack-xt
    field: resend0
    
    field: data-map
    field: data-rmap
    field: data-resend
    field: data-b2b
    
    field: code-map
    field: code-rmap
    
    cfield: ack-state
    cfield: ack-resend~
    cfield: ack-resend#
    cfield: is-server
    field: ack-receive
    
    field: req-codesize
    field: req-datasize
    \ flow control, sender part
    64field: min-slack
    64field: max-slack
    64field: ns/burst
    64field: last-ns/burst
    64field: extra-ns
    field: window-size \ packets in flight
    64field: bandwidth-tick \ ns
    64field: next-tick \ ns
    64field: next-timeout \ ns
    field: timeouts
    64field: rtdelay \ ns
    64field: lastack \ ns
    field: flyburst
    field: flybursts
    64field: lastslack
    64field: lastdeltat
    64field: slackgrow
    64field: slackgrow'
    \ flow control, receiver part
    64field: burst-ticks
    64field: firstb-ticks
    64field: lastb-ticks
    64field: delta-ticks
    field: acks
    64field: last-rate
    \ experiment: track previous b2b-start
    64field: last-rtick
    64field: last-raddr
    \ cookies
    field: last-ackaddr
    \ state machine
    field: expected
    field: total
    field: received  \ to be replaced with dest-tail
    \ statistics
    field: timing-stat
    64field: last-time
    \ make timestamps smaller
    64field: time-offset
    KEYBYTES +field tpkc
    KEYBYTES +field tskc
end-class context-class

begin-structure timestamp
64field: ts-ticks
end-structure

begin-structure reply
field: reply-len
field: reply-offset
64field: reply-dest
end-structure

begin-structure timestats
sffield: ts-delta
sffield: ts-slack
sffield: ts-reqrate
sffield: ts-rate
sffield: ts-grow
end-structure

\ check for valid destination

: >data-head ( addr o:map -- )
    dest-back @ dest-size @ 1- and - dup 0< IF  dest-size @ +  THEN
    maxdata + dest-back @ + dest-head umax! ;

Variable dest-map s" " dest-map $!

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
: dest-index ( -- addr ) dest-addr 64@ >dest-map ;

: check-dest ( -- addr 1/t / f )
    \G return false if invalid destination
    \G return 1 if code, -1 if data, plus destination address
    dest-index 2 cells bounds ?DO
	I @ IF
	    dest-addr 64@ I @ >o dest-vaddr 64@ 64- 64>n dup
	    dest-size @ u<
	    IF
		dest-raddr @ swap dup >data-head +
		code-flag @ invert 2* 1+
		dest-job @ o> >o rdrop
		UNLOOP  EXIT  THEN
	    drop o>
	THEN
    cell +LOOP
    false ;

\ context debugging

: .o ( -- ) context# ? ;
: o? ( -- ) ]] o 0= ?EXIT [[ ; immediate

\ Destination mapping contains
\ addr u - range of virtal addresses
\ addr' - real start address
\ context - for exec regions, this is the job context

Variable >code-flag

: addr>bits ( addr -- bits )
    chunk-p2 rshift ;
: bits>bytes ( bits -- bytes )
    1- 2/ 2/ 2/ 1+ ;
: addr>ts ( addr -- ts-offset )
    addr>bits timestamp * ;
: addr>replies ( addr -- replies )
    addr>bits reply * ;
: addr>keys ( addr -- keys )
    max-size^2 1- rshift ;

: alloz ( size -- addr )
    dup >r allocate throw dup r> erase ;
[IFUNDEF] alloc+guard
    ' alloz alias alloc+guard
[THEN]
: freez ( addr size -- )
    \g erase and then free - for secret stuff
    over swap erase free throw ;
: allocateFF ( size -- addr )
    dup >r allocate throw dup r> -1 fill ;
: allocate-bits ( size -- addr )
    dup >r cell+ allocateFF dup r> + off ; \ last cell is off

: alloc-data ( addr u -- u flag )
    dup >r dest-size ! dest-vaddr 64! r>
    dup alloc+guard dest-raddr !
    c:key# alloz dest-ivsgen !
    >code-flag @ dup code-flag !
    IF
	dup addr>replies  alloz dest-replies !
    ELSE
	dup addr>ts       alloz dest-timestamps !
    THEN ;

: map-data ( addr u -- o )
    o rdata-class new >o dest-job !
    alloc-data
    code-flag @ 0= IF
	dup addr>ts alloz dest-cookies !
	dup addr>bits bits>bytes allocate-bits data-ackbits0 !
	dup addr>bits bits>bytes allocate-bits data-ackbits1 !
	s" " data-ackbits-buf $!
    THEN
    data-lastack# on
    drop
    o o> ;

: map-source ( addr u addrx -- o )
    o code-class new >o dest-job !
    dest-lock 0 pthread_mutex_init drop
    alloc-data
    dup addr>ts alloz dest-cookies !
    drop
    o o> ;

' @ Alias m@

: map-data-dest ( vaddr u addr -- )
    \    return-addr @ routes #.key cell+ >r  r@ @ 0= IF  s" " r@ $!  THEN
    >r >r 64dup r> map-data r@ ! >dest-map r> @ swap ! ;
: map-code-dest ( vaddr u addr -- )
    \    return-addr @ routes #.key cell+ >r  r@ @ 0= IF  s" " r@ $!  THEN
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
$10 cells Value resend-size#
#30.000.000 d>64 64Constant init-delay# \ 20ms initial timeout step

Variable init-context#

: init-flow-control ( -- )
    max-int64 64-2/ min-slack 64!
    max-int64 64-2/ 64negate max-slack 64!
    init-delay# rtdelay 64!
    flybursts# dup flybursts ! flyburst !
    ticks lastack 64! \ asking for context creation is as good as an ack
    bandwidth-init n>64 ns/burst 64!
    never               next-tick 64!
    64#0                extra-ns 64! ;

resend-size# buffer: resend-init

: -timeout      ['] noop               timeout-xt ! ;

: n2o:new-context ( addr -- )
    context-class new >o rdrop
    init-context# @ context# !  1 init-context# +!
    dup return-addr !  return-address !
    resend-init resend-size# data-resend $!
    s" " crypto-key $!
    init-flow-control
    -timeout
    -1 blocksize !
    1 blockalign ! ;

\ create new maps

Variable mapstart $1 mapstart !

: server? ( -- flag )  is-server c@ negate ;
: server! ( -- )  1 is-server c! ;

: n2o:new-map ( u -- addr )
    drop mapstart @ 1 mapstart +! reverse
    [ cell 4 = ] [IF]  0 swap  [ELSE] $FFFFFFFF00000000 and [THEN] ; 
: n2o:new-data { 64: addrs 64: addrd u -- }
    o 0= IF
	addrd >dest-map @ ?EXIT
	return-addr @ n2o:new-context  server!  THEN
    >code-flag off
    addrd u data-rmap map-data-dest addrs u map-source  data-map ! ;
: n2o:new-code { 64: addrs 64: addrd u -- }
    o 0= IF
	addrd >dest-map @ ?EXIT
	return-addr @ n2o:new-context  server!  THEN
    >code-flag on
    addrd u code-rmap map-code-dest
    addrs u map-source code-map ! ;

\ dispose connection

: ?free ( addr -- )
    dup @ IF  dup @ free throw off  ELSE  drop  THEN ;

: ?free+guard ( addr u -- )
    over @ IF  over @ swap  free+guard  off  ELSE  2drop  THEN ;

: free-code ( o:data -- ) o 0= ?EXIT
    dest-raddr dest-size @ ?free+guard
    dest-ivsgen ?free
    dest-replies ?free
    dest-timestamps ?free
    dest-cookies ?free
    dispose ;
' free-code code-class to free-data

:noname ( o:data --- )
    data-ackbits0 ?free
    data-ackbits1 ?free
    data-ackbits-buf $off
    free-code ; rdata-class to free-data

: n2o:dispose-context ( o:addr -- o:addr )
    cmd( ." Disposing context... " )
    0. data-map @ >o dest-vaddr 64@ o> >dest-map 2!
    data-map @ >o free-data o>
    data-rmap @ >o free-data o>
    code-map @ >o free-data o>
    code-rmap @ >o free-data o>
    \ erase crypto keys
    crypto-key $@ erase
    tskc KEYBYTES erase
    resend0 $off
    crypto-key $off
    data-resend $off
    cmd( dispose  ." disposed" cr ) ;

\ data sending around

: >blockalign ( n -- block )
    blockalign @ dup >r 1- + r> negate and ;
: 64>blockalign ( 64 -- block )
    blockalign @ dup >r 1- n>64 64+ r> negate n>64 64and ;

: /data ( u -- )
    >blockalign data-map @ >o dest-head +! o> ;
: /back ( u -- )
    >blockalign data-rmap @ >o dest-back +! o> ;
: /dest-tail ( u -- )
    data-map @ >o dest-tail +! o> ;
: data-dest ( -- addr )
    data-map @ >o
    dest-vaddr 64@ dest-tail @ dest-size @ 1- and n>64 64+ o> ;

\ new data sending around stuff, with front+back

: fix-size ( base offset1 offset2 -- addr len )
    over - >r dest-size @ 1- and r> over + dest-size @ umin over - >r + r> ;
: data-head@ ( -- addr u )
    \ you can read into this, it's a block at a time (wraparound!)
    data-map @ >o
    dest-raddr @ dest-head @ dest-back @ dest-size @ +
    fix-size o> blocksize @ umin ;
: data-tail@ ( -- addr u )
    \ you can write from this, also a block at a time
    data-map @ >o
    dest-raddr @ dest-tail @ dest-head @
    fix-size o> blocksize @ umin ;
: rdata-back@ ( -- addr u )
    \ you can write from this, also a block at a time
    data-rmap @ >o
    dest-raddr @ dest-back @ dest-tail @
    fix-size o> blocksize @ umin ;

: data-head? ( -- flag )
    data-map @ >o dest-head @ dest-back @ dest-size @ + u< o> ;
: data-tail? ( -- flag )
    data-map @ >o dest-tail @ dest-head @ u< o> ;
: rdata-back? ( -- flag )
    data-rmap @ >o dest-back @ dest-tail @ u< o> ;

\ code sending around

: code-dest ( -- addr )
    code-map @ >o
    dest-raddr @ dest-tail @ + o> ;

: code-vdest ( -- addr )
    code-map @ >o
    dest-vaddr 64@ dest-tail @ n>64 64+ o> ;

: code-reply ( -- addr )
    code-map @ >o
    dest-tail @ addr>replies dest-replies @ + o> ;

: tag-addr ( -- addr )
    dest-addr 64@ code-rmap @ >o dest-vaddr 64@ 64- 64>n
    addr>replies dest-replies @ + o> ;

reply buffer: dummy-reply

: reply[] ( index -- addr )
    code-map @ >o
    dup dest-size @ addr>bits u<
    IF  reply * dest-replies @ +  ELSE  dummy-reply  THEN  o> ;

: reply-index ( -- index )
    code-map @ >o dest-tail @ addr>bits o> ;

: code+ ( -- )
    code-map @ >o
    maxdata dest-tail +!
    dest-tail @ dest-size @ u>= IF  dest-tail off  THEN
\    cmd( ." set dest-tail to " dest-tail @ hex. cr )
    o> ;

\ aligned buffer to make encryption/decryption fast
$400 buffer: aligned$
: $>align ( addr u -- addr' u ) dup $400 u> ?EXIT
    tuck aligned$ swap move aligned$ swap ;
    
\ timing records

: net2o:track-timing ( -- ) \ initialize timing records
    s" " timing-stat $! ;

: )stats ]] THEN [[ ;
: stats( ]] timing-stat @ IF [[ ['] )stats assert-canary ; immediate

: net2o:timing$ ( -- addr u )
    stats( timing-stat $@  EXIT ) ." no timing stats" cr s" " ;
: net2o:/timing ( n -- )
    stats( timing-stat 0 rot $del ) ;

: net2o:rec-timing ( addr u -- ) $>align \ do some dumps
    bounds ?DO
	I ts-delta sf@ f>64 last-time 64+!
	last-time 64@ 64>f 1n f* fdup f.
	time-offset 64@ 64>f 1n f* 10e fmod f+ f.
	\ I ts-delta sf@ f.
	I ts-slack sf@ 1u f* f.
	tick-init 1+ maxdata * 1k fm* fdup
	I ts-reqrate sf@ f/ f.
	I ts-rate sf@ f/ f.
	I ts-grow sf@ 1u f* f.
	." timing" cr
    timestats +LOOP ;

timestats buffer: stat-tuple

: stat+ ( addr -- )  stat-tuple timestats  timing-stat $+! ;

\ flow control

64Variable ticker

: !ticks ( -- )
    ticks ticker 64! ;

: ticks-init ( ticks -- )
    64dup bandwidth-tick 64!  next-tick 64! ;

: >rtdelay ( client serv -- client serv )
    recv-tick 64@ 64dup lastack 64!
    64over 64- rtdelay 64min! ;

: timestat ( client serv -- )
    64dup 64-0<=    IF  64drop 64drop  EXIT  THEN
    timing( 64over 64. 64dup 64. ." acktime" cr )
    >rtdelay  64- 64dup lastslack 64!
    lastdeltat 64@ delta-damp# 64rshift
    64dup min-slack 64+! 64negate max-slack 64+!
    64dup min-slack 64min!
    max-slack 64max! ;

: b2b-timestat ( client serv -- )
    64dup 64-0<=    IF  64drop 64drop  EXIT  THEN
    64- lastslack 64@ 64- slack( 64dup 64. .o ." grow" cr )
    slackgrow 64! ;

: >offset ( addr -- addr' flag )
    dest-vaddr 64@ 64- 64>n dup dest-size @ u< ;

#5000000 Value rt-bias# \ 5ms additional flybursts allowed

: net2o:set-flyburst ( -- bursts )
    rtdelay 64@ 64>n rt-bias# + ns/burst 64@ 64>n /
    flybursts# +
    bursts( dup . .o ." flybursts "
    rtdelay 64@ 64. ns/burst 64@ 64. ." rtdelay" cr )
    dup flybursts-max# min flyburst ! ;
: net2o:max-flyburst ( bursts -- )  flybursts-max# min flybursts max!@
    0= IF  bursts( .o ." start bursts" cr ) THEN ;

: >flyburst ( -- )
    flyburst @ flybursts max!@ \ reset bursts in flight
    0= IF  recv-tick 64@ ticks-init
	bursts( .o ." restart bursts " flybursts ? cr )
	net2o:set-flyburst net2o:max-flyburst
    THEN ;

: >timestamp ( time addr -- time' ts-array index / time' 0 0 )
    >flyburst
    64>r time-offset 64@ 64+ 64r>
    data-map @ dup 0= IF  drop 0 0  EXIT  THEN  >r
    r@ >o >offset  IF
	dest-tail @ o> over - 0 max addr>bits window-size !
	addr>ts r> >o dest-timestamps @ o> swap
    ELSE  o> rdrop 0 0  THEN ;

: net2o:ack-addrtime ( ticks addr -- )
    >timestamp over  IF
	dup tick-init 1+ timestamp * u>
	IF  + dup >r  ts-ticks 64@
	    r@ tick-init 1+ timestamp * - ts-ticks 64@
	    64dup 64-0<= >r 64over 64-0<= r> or
	    IF  64drop 64drop  ELSE  64- lastdeltat 64!  THEN  r>
	ELSE  +  THEN
	ts-ticks 64@ timestat
    ELSE  2drop 64drop  THEN ;

: net2o:ack-b2btime ( ticks addr -- )
    >timestamp over  IF  + ts-ticks 64@ b2b-timestat
    ELSE  2drop 64drop  THEN ;

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
	msg( ." slack ignored: " 64dup 64. cr )
	64drop 64#0 lastslack 64@ min-slack 64!
    THEN
    64>n  slack( dup . min-slack ? .o ." slack" cr )
    stats( dup s>f stat-tuple ts-slack sf! )
    slack-bias# - slack-min# max slack# 2* 2* min
    s>f slack# fm/ 2e fswap f**
    ( slack# / lshift ) ;

: aggressivity-rate ( slack -- slack' )
    slack-max# 64>n 2/ slack-default# tuck min swap 64*/ ;

: slackext ( rfactor -- slack )
    slackgrow 64@
    window-size @ tick-init 1+ bursts# - 64*/
    64>f f* f>64
    slackgrow' 64@ 64+ 64dup ext-damp# 64*/ slackgrow' 64!
    64#0 64max aggressivity-rate ;

: rate-limit ( rate -- rate' )
    \ not too quickly go faster!
    64dup last-ns/burst 64!@ 64max ;
\    64>n last-ns/burst 64@ 64>n \ obsolete
\    ?dup-IF  dup >r 2* 2* min r> 2/ 2/ max  THEN
\    dup n>64 last-ns/burst 64! n>64 ;

: extra-limit ( rate -- rate' )
    dup extra-ns 64@ 64>n 2* 2* u> IF
	extra-ns 64@ 64>n + dup 2/ 2/ dup n>64 extra-ns 64! -
    THEN ;

: >extra-ns ( rate -- rate' )
    >slack-exp fdup 64>f f* f>64 slackext
    64dup extra-ns 64! 64+ ( extra-limit ) ;

: rate-stat1 ( rate deltat -- )
    stats( ticks time-offset 64@ 64-
           64dup last-time 64!@ 64- 64>f stat-tuple ts-delta sf!
           64over 64>f stat-tuple ts-reqrate sf! )
    rate( 64over 64. .o ." clientrate" cr )
    deltat( 64dup 64. lastdeltat 64@ 64. .o ." deltat" cr ) ;

: rate-stat2 ( rate -- rate )
    rate( 64dup 64. .o ." rate" cr )
    stats( 64dup extra-ns 64@ 64+ 64>f stat-tuple ts-rate sf!
           slackgrow 64@ 64>f stat-tuple ts-grow sf! 
           stat+ ) ;

: net2o:set-rate ( rate deltat -- )  rate-stat1
    64>r 64dup >extra-ns ens( 64nip )else( 64drop )
    64r> delta-t-grow# 64*/ 64min ( no more than 2*deltat )
    bandwidth-max n>64 64max rate-limit rate-stat2 ns/burst 64!@
    bandwidth-init n>64 64= IF \ first acknowledge
	net2o:set-flyburst
	net2o:max-flyburst
    THEN ;

\ acknowledge

sema resize-lock

$20 Value mask-bits#
: >mask0 ( addr mask -- addr' mask' )
    BEGIN  dup 1 and 0= WHILE  1 rshift >r maxdata + r>  dup 0= UNTIL  THEN ;
: net2o:resend-mask ( addr mask -- )
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
	I @ 0= IF  >mask0 I 2! UNLOOP EXIT  THEN
    2 cells +LOOP  2drop ;
: net2o:ack-resend ( flag -- )  resend-toggle# and ack-resend~ c!
    flybursts @ ack-resend# c! ;
: resend$@ ( -- addr u )
    data-resend $@  IF
	2@ 1 and IF  maxdata  ELSE  0  THEN
	swap data-map @ >o dest-raddr @ + o> swap
    ELSE  drop 0 0  THEN ;

: resend-dest ( -- addr )
    data-resend $@ drop 2@ drop data-map @ >o n>64 dest-vaddr 64@ 64+ o> ;
: /resend ( u -- )
    0 +DO
	data-resend $@ drop
	dup >r 2@ -2 and >mask0 tuck r> 2!
	0= IF  data-resend $@ 2 cells - >r dup 2 cells + swap r> move
	    0. data-resend $@ 2 cells - + 2!
	THEN
    maxdata +LOOP ;

\ resend third handshake

: push-reply ( addr u -- )  resend0 $! ;

\ file handling

\ file states

object class
    64field: fs-size
    64field: fs-seek
    64field: fs-seekto
    64field: fs-limit
    64field: fs-time
    field: fs-fid
end-class file-state-class
file-state-class >osize @ Constant file-state-struct

file-state-struct buffer: new-file-state

: ?state ( -- )
    file-state @ 0= IF  s" " file-state $!  THEN ;

: id>addr ( id -- addr remainder )  ?state
    >r file-state $@ r> file-state-struct * /string ;
: id>addr? ( id -- addr )
    id>addr file-state-struct < !!fileid!! ;
: state-addr ( id -- addr )
    id>addr dup 0< !!gap!!
    0= IF  drop new-file-state file-state-struct file-state $+!
	file-state $@ + file-state-struct -  THEN ;

: +expected ( n -- ) >blockalign expected @ tuck + dup expected !
    data-rmap @ >o data-ackbits0 2@  2swap
    maxdata 1- + chunk-p2 rshift swap chunk-p2 rshift +DO
	dup I -bit  over I -bit  LOOP  2drop
    data-firstack0# off  data-firstack1# off o>
    firstack( ." expect more data" cr ) ;

: size! ( 64 id -- )  state-addr >o
    64dup fs-size 64!  fs-limit 64!
    64#0 fs-seekto 64! 64#0 fs-seek 64! o> ;
: seekto! ( 64 id -- )  state-addr >o
    fs-size 64@ 64umin 64dup fs-seekto 64!
    fs-seek 64@ 64- 64>n o> +expected ;
: limit! ( 64 id -- )  state-addr >o
    fs-size 64@ 64umin fs-limit 64! o> ;
: total! ( n -- )  total ! ;

: net2o:gen-total ( -- 64u ) 64#0
    file-state $@ bounds ?DO
	I >o fs-limit 64@ fs-seekto 64@ 64- o>
	64>blockalign 64#0 64max 64+
    file-state-struct +LOOP ;

: file+ ( addr -- ) >r 1 r@ +!
    r@ @ id>addr nip 0<= IF  r@ off  THEN  rdrop ;

: >seek ( size 64to 64seek -- size' )
    64dup 64>d fs-fid @ reposition-file throw 64- 64>n umin ;

: fstates ( -- n )  file-state $@len file-state-struct / ;

: ?residual ( addr len resaddr -- addr len' ) >r
    r@ @ 0= IF  blocksize @ over -
    ELSE  r@ @ umin  r@ @ over - THEN  r> ! ;

: n2o:save-block ( id -- delta ) 0 { id roff }
    msg( data-rmap @ >o dest-raddr @ o> to roff )
    rdata-back@ residualwrite ?residual
    id id>addr? >o fs-seekto 64@ fs-seek 64@ >seek
    msg( ." Write <" 2dup swap roff - hex. hex. o o>
         residualwrite @ hex. >o id 0 .r ." >" cr )
    tuck fs-fid @ write-file throw
    dup n>64 fs-seek 64+! o>
    dup /back ;

: save-all-blocks ( -- )  +calc fstates 0 { size fails }
    BEGIN  rdata-back?  WHILE
	    write-file# @ n2o:save-block IF 0 ELSE fails 1+ THEN to fails
	    rdata-back? residualwrite @ 0= or  IF
		write-file# file+ residualwrite off  THEN
    fails size u>= UNTIL  THEN msg( ." Write end" cr ) +file ;

: save-to ( addr u n -- )  state-addr >o
    r/w create-file throw fs-fid ! o> ;

\ file status stuff

: ?ior ( r -- )
    \G use errno to generate throw when failing
    IF  errno negate 512 - throw  THEN ;

: fstat-fake ( fileno buf -- ior ) >r drop
    $1A4 r@ st_mode l! ntime r> st_mtime ntime!  0 ;

: n2o:get-stat ( id -- mtime mod )
    id>addr? >o fs-fid @ fileno statbuf fstat o> ?ior
    statbuf st_mtime ntime@ d>64
    statbuf st_mode l@ $FFF and ;

: n2o:track-time ( mtime fileno -- ) >r
    [IFDEF] android  rdrop 64drop
    [ELSE]  \ ." Set time: " r@ . 64dup 64>d d. cr
	64>d 2dup statbuf ntime!
	statbuf 2 cells + ntime!
	r> statbuf futimens ?ior [THEN] ;

: n2o:track-mod ( mod fileno -- )
    [IFDEF] android 2drop
    [ELSE] swap fchmod ?ior [THEN] ;

: n2o:set-stat ( mtime mod id -- )
    id>addr? >o fs-fid @ fileno n2o:track-mod fs-time 64! o> ;

\ open a file - this needs *way more checking*! !!FIXME!!

: id>file ( id -- fid )  id>addr? >o fs-fid @ o> ;

: (n2o:close-file) ( o:file -- )
    fs-time 64@ 64dup 64-0= IF  64drop
    ELSE
	fs-fid @ flush-file throw
	fs-fid @ fileno n2o:track-time
    THEN
    fs-fid @ close-file throw  fs-fid off ;

: n2o:close-file ( id -- )
    id>addr? >o fs-fid @ IF  (n2o:close-file)  THEN  o> ;

: n2o:open-file ( addr u mode id -- )
    ?state  state-addr >o
    fs-fid @ IF  (n2o:close-file)  THEN
    msg( dup 2over ." open file: " type ."  with mode " . cr )
    open-file throw fs-fid !
    fs-fid @ file-size throw d>64 64dup fs-size 64! fs-limit 64!
    64#0 fs-seek 64! 64#0 fs-seekto 64! 64#0 fs-time 64! o> ;

: n2o:slurp-block ( id -- delta ) 0 { id roff }
    msg( data-map @ >o dest-raddr @ o> to roff )
    data-head@ residualread ?residual
    id id>addr? >o fs-limit 64@ fs-seekto 64@ >seek
    msg( ." Read <" 2dup swap roff - hex. hex. o o> residualread @ hex. >o id 0 .r ." >" cr )
    fs-fid @ read-file throw
    dup n>64 fs-seekto 64+! o>
    dup /data ;

: n2o:slurp-block' ( id -- seek )
    dup n2o:slurp-block drop id>addr? >o fs-seekto 64@ o> ;

: n2o:slurp-blocks-once ( idbits -- sum ) 0 { idbits sum }
    8 cells 0 DO
	1 I lshift idbits and IF
	    I n2o:slurp-block  sum + to sum
	THEN
    LOOP  sum ;

: n2o:slurp-blocks ( idbits -- )
    BEGIN  data-head?  WHILE
	dup n2o:slurp-blocks-once  0= UNTIL  THEN
    drop ;

: n2o:slurp-all-blocks ( -- )  +calc fstates 0 { size fails }
    0 BEGIN  data-head?  WHILE
	    read-file# @ n2o:slurp-block IF 0 ELSE fails 1+ THEN to fails
	    data-head? residualread @ 0= or  IF
		read-file# file+  residualread off  THEN
    fails size u>= UNTIL  THEN msg( ." Read end" cr ) +file ;

: n2o:track-seeks ( idbits xt -- ) { xt } ( i seeklen -- )
    8 cells 0 DO
	dup 1 and IF
	    I dup id>addr? >o fs-seek 64@ fs-seekto 64@ 64<> IF
		fs-seekto 64@ 64dup fs-seek 64! o>
		xt execute  ELSE  drop o>  THEN
	THEN  2/
    LOOP  drop ;

: n2o:track-all-seeks ( xt -- ) { xt } ( i seeklen -- )
    file-state $@len file-state-struct / 0 DO
	I dup id>addr? >o fs-seek 64@ fs-seekto 64@ 64<> IF
	    fs-seekto 64@ 64dup fs-seek 64! o>
	    xt execute  ELSE  drop o>  THEN
    LOOP ;

\ load crypto here

require net2o-crypt.fs
\ require net2o-keys.fs

\ cookie stuff

: cookie! ( -- )
    c:cookie
    dest-addr 64@ >offset 0= IF  2drop  EXIT  THEN
    addr>ts cookie( ." Cookie: " dup hex. >r 64dup .16 cr r> )
    dest-cookies @ + 64! ;

: send-cookie ( -- )  data-map  @ >o cookie! o> ;
: recv-cookie ( -- )  data-rmap @ >o cookie! o> ;

[IFDEF] 64bit
    : cookie+ ( addr bitmap map -- sum ) >o
	cookie( ." cookie: " 64>r dup hex. 64r> 64dup .16 space space ) >r
	addr>ts dest-size @ addr>ts umin
	dest-cookies @ + 0
	BEGIN  r@ 1 and IF  over @ cookie( 64dup .16 space ) +  THEN
	>r cell+ r> r> 1 rshift dup >r 0= UNTIL
	rdrop nip cookie( ." => " 64dup .16 cr ) o> ;
[ELSE]
    : cookie+ ( addr bitmap map -- sum ) >o
	cookie( ." cookies: " 64>r dup hex. 64r> 64dup .16 space space ) >r >r
	addr>ts dest-size @ addr>ts umin
	dest-cookies @ + { addr } 64#0 cookie( ." cookie: " )
	BEGIN  r@ 1 and IF  addr 64@ cookie( 64dup .16 space ) 64+  THEN
	addr 64'+ to addr r> r> 1 64rshift 64dup >r >r 64-0= UNTIL
	64r> 64drop cookie( ." => " 64dup .16 space cr ) o> ;
[THEN]

\ send blocks of memory

: set-dest ( addr target -- )
    outbuf ins-dest  64dup dest-addr 64!  outbuf addr 64! ;

Variable outflag  outflag off

: set-flags ( -- )
    outflag @ outbuf 1+ c! outflag off ;

: c+!  ( n addr -- )  dup >r c@ + r> c! ;

: outbody ( -- addr ) outbuf packet-body ;
: outsize ( -- n )    outbuf packet-size ;

#90 Constant EMSGSIZE

User code-packet

: send-packet ( flag -- ) +sendX
\    ." send " outbuf .header
    code-packet @ outbuf-encrypt
    code-packet @ 0= IF  send-cookie  THEN
    code-packet off
    out-route drop
    outbuf dup packet-size
    send-a-packet 0< IF
	errno EMSGSIZE = IF
	    max-size^2 1- to max-size^2  ." pmtu/2" cr
	ELSE
	    errno 512 + negate throw
	THEN
    THEN ;

: >send ( addr n -- )
    >r  r@ 64bit# or outbuf c!
    outbody min-size r> lshift move ;

: bandwidth+ ( -- )  o?
    ns/burst 64@ 64>n tick-init 1+ / n>64 bandwidth-tick 64+! ;

: burst-end ( flag -- flag )  data-b2b @ ?EXIT
    ticker 64@ bandwidth-tick 64@ 64max next-tick 64! drop false ;

: sendX ( addr taddr target n -- ) +sendX2
    >r set-dest  r> ( addr n -- ) >send  set-flags  bandwidth+  send-packet
    net2o:update-key ;

\ send chunk

: net2o:get-dest ( -- taddr target )
    data-dest return-address @ ;
: net2o:get-resend ( -- taddr target )
    resend-dest return-address @ ;

\ branchless version using floating point

FVariable <size-lb>

: send-size ( u -- n )
    min-size umax maxdata umin 1-
    [ min-size 2/ 2/ s>f 1/f ] FLiteral fm*
    <size-lb> df!  <size-lb> 6 + c@ 4 rshift ;

64Variable last-ticks

: ts-ticks! ( addr -- )
    addr>ts dest-timestamps @ + >r last-ticks 64@ r> ts-ticks
    dup 64@ 64-0= IF  64!  EXIT  THEN  64on 64drop 1 packets2 +! ;
\ set double-used ticks to -1 to indicate unkown timing relationship

: net2o:send-tick ( addr -- )
    data-map @ >o
    dest-raddr @ - dup dest-size @ u<
    IF  ts-ticks!  ELSE  drop  THEN  o> ;

: net2o:prep-send ( addr u dest addr -- addr taddr target n len )
    { 64: dest addr }  over  net2o:send-tick
    send-size min-size over lshift
    2>r dest addr 2r> ;

\ synchronous sending

: data-to-send ( -- flag )
    resend$@ nip 0> data-tail? or ;

: net2o:resend ( -- )
    resend$@ net2o:get-resend 2dup 2>r
    net2o:prep-send /resend
    2r> resend( ." resending " over hex. dup hex. outflag @ hex. cr ) 2drop ;

: net2o:send ( -- )
    data-tail@ net2o:get-dest 2dup 2>r
    net2o:prep-send /dest-tail
    2r> send( ." sending " over hex. dup hex. outflag @ hex. cr ) 2drop ;

: net2o:send-chunk ( -- )  +chunk
    ack-state c@ outflag or!
    bursts# 1- data-b2b @ = IF
	\ send a new packet for timing path
	data-tail? IF  net2o:send  ELSE  net2o:resend  THEN
    ELSE
	resend$@ nip IF  net2o:resend  ELSE  net2o:send  THEN
    THEN
    dup 0= IF  2drop 2drop  EXIT  THEN
    data-to-send 0= IF
	resend-toggle# outflag xor!  ack-toggle# outflag xor!
	sendX  never next-tick 64!
    ELSE  sendX  THEN ;

: bandwidth? ( -- flag )
    ticker 64@ 64dup last-ticks 64! next-tick 64@ 64- 64-0>=
    flybursts @ 0> and  ;

\ asynchronous sending

begin-structure chunks-struct
field: chunk-context
field: chunk-count
end-structure

Variable chunks s" " chunks $!
Variable chunks+
Create chunk-adder chunks-struct allot
0 Value sender-task
0 Value receiver-task

: do-send-chunks ( -- )
    chunks $@ bounds ?DO
	I chunk-context @ o = IF
	    UNLOOP  EXIT
	THEN
    chunks-struct +LOOP
    resize-lock lock
    o chunk-adder chunk-context !
    0 chunk-adder chunk-count !
    chunk-adder chunks-struct chunks $+!
    resize-lock unlock
    ticker 64@ ticks-init ;

event: ->send-chunks ( o -- ) >o do-send-chunks o> ;

: net2o:send-chunks  sender-task 0= IF  do-send-chunks  EXIT  THEN
    <event o elit, ->send-chunks sender-task event> ;

: chunk-count+ ( counter -- )
    dup @
    dup 0= IF
	ack-toggle# ack-state xorc!
	ack-resend# c@ 1- 0 max dup ack-resend# c!
	0= IF  ack-resend~ @ ack-state c@ resend-toggle# invert and or ack-state c!  THEN
	-1 flybursts +! bursts( ." bursts: " flybursts ? flyburst ? cr )
	flybursts @ 0<= IF
	    bursts( .o ." no bursts in flight " ns/burst ? data-tail@ swap hex. hex. cr )
	THEN
    THEN
    tick-init = IF  off  ELSE  1 swap +!  THEN ;

: send-a-chunk ( chunk -- flag )  >r
    data-b2b @ 0<= IF
	bandwidth? dup  IF
	    b2b-toggle# ack-state xorc!
	    bursts# 1- data-b2b !
	THEN
    ELSE
	-1 data-b2b +!  true
    THEN
    dup IF  r@ chunk-count+  net2o:send-chunk  burst-end  THEN
    rdrop  1 chunks+ +! ;

: .nosend ( -- ) ." done, "  4 set-precision
    .o ." rate: " ns/burst @ s>f tick-init chunk-p2 lshift s>f 1e9 f* fswap f/ fe. cr
    .o ." slack: " min-slack ? cr
    .o ." rtdelay: " rtdelay ? cr ;

: send-chunks-async ( -- flag )
    chunks $@ chunks+ @ chunks-struct * safe/string
    IF
	dup chunk-context @ >o rdrop
	chunk-count
	data-to-send IF
	    \ msg( ." send a chunk" cr )
	    send-a-chunk
	ELSE
	    drop msg( .nosend )
	    chunks chunks+ @ chunks-struct * chunks-struct $del
	    false
	THEN
    ELSE  drop chunks+ off false  THEN ;

: next-chunk-tick ( -- tick )
    64#-1 chunks $@ bounds ?DO
	I chunk-context @ >o next-tick 64@ o> 64umin
    chunks-struct +LOOP ;

: send-another-chunk ( -- flag )  false  0 >r  !ticks
    BEGIN  BEGIN  drop send-chunks-async dup  WHILE  rdrop 0 >r  REPEAT
	chunks+ @ 0= IF  r> 1+ >r  THEN
    r@ 2 u>=  UNTIL  rdrop ;

Variable sendflag  sendflag off
Variable recvflag  recvflag off
    
: send-anything? ( -- flag )  chunks $@len 0> ;

\ rewind buffer to send further packets

: clear-cookies ( -- )
    s" " data-rmap @ >o data-ackbits-buf $! o> ;

: rewind-timestamps ( o:map -- )
    code-flag @ 0= IF
	dest-timestamps @ dest-size @ addr>ts erase
    THEN ;

: rewind-timestamps-partial ( new-back o:map -- )
    code-flag @ 0= IF
	dest-back @ - addr>ts >r
	dest-timestamps @ dest-size @ addr>ts dest-back @ addr>ts over 1- and
	/string r@ umin dup >r erase
	dest-timestamps @ r> r> - erase
    ELSE
	drop
    THEN ;

: clearpages-partial ( new-back o:map -- )
    dest-back @ - >r
    dest-raddr @ dest-size @ dest-back @ over 1- and
    /string r@ umin dup >r clearpages
    dest-raddr @ r> r> - clearpages ;

: rewind-partial ( new-back o:map -- )
    \ dup clearpages-partial
    dup rewind-timestamps-partial
    regen-ivs-part ;

: rewind-buffer ( o:map -- )
    1 dest-round +!
    \ dest-size @ dest-back +!
    dest-tail off  dest-head off  dest-back off
    \ dest-raddr @ dest-size @ clearpages
    regen-ivs-all  rewind-timestamps ;

: rewind-ackbits ( o:map -- )
    data-firstack0# off  data-firstack1# off
    firstack( ." rewind firstacks" cr )
    data-lastack# on
    dest-size @ addr>bits bits>bytes
    data-ackbits0 @ over -1 fill
    data-ackbits1 @ swap -1 fill ;

: rewind-ackbits-partial ( new-back o:map -- )
    dest-back @ - addr>bits bits>bytes >r
    data-ackbits0 @
    dest-size @ addr>bits bits>bytes
    dest-back @ addr>bits bits>bytes over 1- and /string
    r@ umin dup >r -1 fill
    data-ackbits0 @ r> r@ - -1 fill
    data-ackbits1 @
    dest-size @ addr>bits bits>bytes
    dest-back @ addr>bits bits>bytes over 1- and /string
    r@ umin dup >r -1 fill
    data-ackbits1 @ r> r> - -1 fill ;

: net2o:rewind-sender ( n -- )
    data-map @ >o
    dest-round @ +DO  rewind-buffer  LOOP  o> ;

: net2o:rewind-receiver ( n -- ) cookie( ." rewind" cr )
    data-rmap @ >o
    dest-round @ +DO  rewind-buffer  LOOP
    rewind-ackbits ( clear-cookies ) o> ;

: net2o:rewind-sender-partial ( new-back -- )
    flush( ." rewind partial " dup hex. cr )
    data-map @ >o dup rewind-partial dest-back ! o> ;

: net2o:rewind-receiver-partial ( new-back -- )
    flush( ." rewind partial " dup hex. cr )
    data-rmap @ >o
    dup rewind-partial  dup rewind-ackbits-partial  dest-back ! o> ;

\ NAT traversal stuff

: .sockaddr { addr alen -- }
    case addr family w@
	AF_INET of
	    '4' emit addr sin_addr 4 type addr port 2 type
	endof
	AF_INET6 of
	    '6' emit addr sin6_addr $10 type addr sin6_port 2 type
	endof
    endcase ;

: .port ( addr len -- )
    drop be-uw@ 0 ['] .r #10 base-execute ;
: .ip4b ( addr len -- addr' len' )
    over c@ 0 ['] .r #10 base-execute 1 /string ;
: .ip4 ( addr len -- )
    .ip4b ." ." .ip4b ." ." .ip4b ." ." .ip4b ." :" .port ;
: .ip6w ( addr len -- addr' len' )
    over be-uw@ [: 0 <# # # # # #> type ;] $10 base-execute
    2 /string ;
: w, ( w -- )  here w! 2 allot ;
Create fake-ip4 $0000 w, $0000 w, $0000 w, $0000 w, $0000 w, $FFFF w,
: .ip6 ( addr len -- )
    2dup fake-ip4 12 string-prefix? IF  12 /string .ip4  EXIT  THEN
    '[' 8 0 DO  emit .ip6w ':'  LOOP  drop ." ]:" .port ;

: .ipaddr ( addr len -- )
    case  over c@ >r 1 /string r>
	'4' of  .ip4  endof
	'6' of  .ip6  endof
	-rot dump endcase cr ;

: >sockaddr ( -- addr len )
    return-address @ routes #.key $@ ['] .sockaddr $tmp ;

\ schedule delayed events

object class
field: queue-timestamp
field: queue-job
field: queue-xt
end-class queue-class
queue-class >osize @ Constant queue-struct

Variable queue s" " queue $!
queue-class >osize @ buffer: queue-adder  

: add-queue ( xt us -- )
    ticker 64@ +  o queue-adder >o queue-job !  queue-timestamp !
    queue-xt !  o queue-struct queue $+! o> ;

: eval-queue ( -- )
    queue $@len 0= ?EXIT  ticker 64@
    queue $@ bounds ?DO  I >o
	dup queue-timestamp @ u> IF
	    queue-xt @ queue-job @ >o execute o>
	    0 queue-timestamp !
	THEN o>
    queue-struct +LOOP  drop
    0 >r BEGIN  r@ queue $@len u<  WHILE
	    queue $@ r@ safe/string drop queue-timestamp @ 0= IF
		queue r@ queue-struct $del
	    ELSE
		r> queue-struct + >r
	    THEN
    REPEAT  rdrop ;

\ poll loop

UValue pollfd#  1 to pollfd#
User pollfds
pollfds pollfd %size pollfd# * dup cell- uallot drop erase

: fds!+ ( fileno flag addr -- addr' )
     >r r@ events w!  r@ fd l!  r> pollfd %size + ; 

: prep-socks ( -- )  pollfds >r
    net2o-sock  fileno POLLIN  r> fds!+ drop ;

: prep-evsocks ( -- )  pollfds >r
    epiper @  fileno POLLIN  r> fds!+ drop ;

: clear-events ( -- )  pollfds
    pollfd# 0 DO  0 over revents w!  pollfd %size +  LOOP  drop ;

: timeout! ( -- )
    sender-task dup IF  up@ =  ELSE  0=  THEN  IF
	next-chunk-tick 64dup 64#-1 64= 0= >r ticker 64@ 64- 64dup 64-0>= r> or
	IF    64>n 0 max poll-timeout# min 0 ptimeout 2!
	ELSE  64drop poll-timeout# 0 ptimeout 2!  THEN
    ELSE  poll-timeout# 0 ptimeout 2!  THEN ;

: max-timeout! ( -- ) poll-timeout# 0 ptimeout 2! ;

: poll-sock ( -- flag )
    eval-queue  clear-events  timeout!
    pollfds pollfd#
[IFDEF] ppoll
    ptimeout 0 ppoll 0>
[ELSE]
    ptimeout cell+ @ #1000000 / poll 0>
[THEN] +wait
;

: wait-send ( -- flag )
    clear-events  timeout!
    pollfds pollfd#
[IFDEF] ppoll
    ptimeout 0 ppoll 0>
[ELSE]
    ptimeout cell+ @ #1000000 / poll 0>
[THEN] ;

: read-a-packet4/6 ( -- addr u )
    pollfds revents w@ POLLIN = IF
	do-block read-a-packet  0 pollfds revents w! +rec EXIT  THEN
    don't-block read-a-packet +rec ;

: try-read-packet ( -- addr u / 0 0 )
    don't-block read-a-packet +rec ;

4 Value try-read#

: try-read-packet-wait ( -- addr u / 0 0 )
    try-read# 0 ?DO
	don't-block read-a-packet
	dup IF  unloop  +rec  EXIT  THEN  2drop  LOOP
    poll-sock drop read-a-packet4/6 ;

2 Value sends#
16 Value recvs# \ balance receive and send

: read-a-packet? ( -- addr u )
    don't-block read-a-packet dup IF  1 recvflag +!  THEN ;

: send-read-packet ( -- addr u )
    recvs# recvflag @ > IF  read-a-packet? dup ?EXIT  2drop  THEN
    recvflag off
    0. BEGIN  2drop  send-anything?
	sends# 0 ?DO
	    0= IF  try-read-packet-wait  UNLOOP  EXIT  THEN
	    send-another-chunk  LOOP  drop
    read-a-packet? dup UNTIL ;

: send-loop ( -- )
    send-anything?
    BEGIN  0= IF   wait-send drop
	    pollfds revents w@ POLLIN = IF  ?events  THEN  THEN
	send-another-chunk  AGAIN ;

Defer init-reply

: create-sender-task ( -- )
    o 1 stacksize4 NewTask4 dup to sender-task pass
    init-reply prep-evsocks
    >o rdrop  alloc-io c:init
    send-loop ;

: next-packet ( -- addr u )
    sender-task 0= IF
	send-read-packet
    ELSE
	try-read-packet-wait
	\ 0.  BEGIN  2drop do-block read-a-packet +rec dup  UNTIL
    THEN  dup 0= ?EXIT
    sockaddr alen @ insert-address  inbuf ins-source
    over packet-size over <> !!size!! +next ;

0 Value dump-fd

: net2o:timeout ( ticks -- ) \ print why there is nothing to send
    >flyburst
    timeout( ." timeout? " . send-anything? . chunks+ ? bandwidth? . next-chunk-tick ( ticks-u - ) . cr ) ;

Defer queue-command ( addr u -- )
' dump IS queue-command

: pow2? ( n -- n )  dup dup 1- and 0<> !!pow2!! ;

Variable validated

$01 Constant crypt-val
$02 Constant own-crypt-val
$04 Constant login-val
$08 Constant cookie-val
$10 Constant tmp-crypt-val
$20 Constant keys-val

: crypt?     ( -- flag )  validated @ crypt-val     and ;
: own-crypt? ( -- flag )  validated @ own-crypt-val and ;
: login?     ( -- flag )  validated @ login-val     and ;
: cookie?    ( -- flag )  validated @ cookie-val    and ;
: tmp-crypt? ( -- flag )  validated @ tmp-crypt-val and ;
: keys?      ( -- flag )  validated @ keys-val      and ;

: handle-cmd0 ( -- ) \ handle packet to address 0
    0 >o rdrop \ address 0 has no job context!
    true inbuf-decrypt 0= IF
	." invalid packet to 0" cr EXIT  THEN
    validated off \ packets to address 0 are not really validated
    inbuf packet-data queue-command ;

: handle-data ( addr -- )
    data( ." received: " inbuf .header cr )
    >r inbuf packet-data r> swap move
    +inmove ack-xt perform +ack ;

: handle-cmd ( addr -- )
    >r inbuf packet-data r@ swap dup >r move
    r> r> swap queue-command ;

: handle-dest ( addr f -- ) \ handle packet to valid destinations
    ticker 64@
    timing( dest-addr 64@ 64.
            64dup  time-offset 64@ 64- 64. ." recv timing" cr )
    recv-tick 64! \ time stamp of arrival
    dup 0> inbuf-decrypt 0= IF
	inbuf .header
	." invalid packet to " dest-addr 64@ .16 cr
	IF  drop  THEN  EXIT  THEN
    crypt-val validated ! \ ok, we have a validated connection
    return-addr @ dup return-address !@
    address( <> IF  ." handover" cr THEN )else( 2drop )
    0< IF  handle-data  ELSE  handle-cmd  THEN ;

: handle-packet ( -- ) \ handle local packet
    >ret-addr >dest-addr +desta
    header( inbuf .header )
    dest-addr 64@ 64-0= IF  handle-cmd0
    ELSE
	check-dest dup 0= IF  drop  EXIT  THEN +dest
	handle-dest
    THEN ;

: route-packet ( -- )  inbuf dup packet-size send-a-packet drop ;

\ timeout handling

: do-timeout ( -- )  o IF timeout-xt perform THEN ;

#2.000.000.000 d>64 64Value timeout-max# \ 2s maximum timeout
#12 Value timeouts# \ with 30ms initial timeout, gives 4.8s cummulative timeout

Sema timeout-sema
Variable timeout-tasks s" " timeout-tasks $!
Variable timeout-task

: o+timeout ( -- )  timeout-sema lock
    timeout-tasks $@ bounds ?DO  I @ o = IF
	    UNLOOP   timeout-sema unlock  EXIT  THEN
    cell +LOOP
    o timeout-task !  timeout-task cell timeout-tasks $+!
    timeout-sema unlock ;
: o-timeout ( -- )  timeout-sema lock
    timeout-tasks $@len 0 ?DO
	timeout-tasks $@ I /string drop @ o =  IF
	    timeout-tasks I cell $del
	LEAVE  THEN
    cell +LOOP  timeout-sema unlock ;
: sq2** ( 64n n -- 64n' )
    dup 1 and >r 2/ 64lshift r> IF  64dup 64-2/ 64+  THEN ;
: >next-timeout ( -- )  o?
    rtdelay 64@ timeouts @ sq2**
    timeout-max# 64min timeout( ." timeout setting: " 64dup 64. cr )
    ticker 64@ 64+ next-timeout 64!  o+timeout ;
: 64min? ( a b -- min flag )
    64over 64over 64< IF  64drop false  ELSE  64nip true  THEN ;
: next-timeout? ( -- time context ) 0 max-int64
    timeout-tasks $@ bounds ?DO
	I @ >o next-timeout 64@ o> 64min? IF  n64-swap drop I @ 64n-swap  THEN
    cell +LOOP  n64-swap ;
: ?timeout ( -- context/0 )
    ticker 64@ next-timeout? >r 64- 64-0>= r> and ;
: reset-timeout ( -- ) o?
    0 timeouts ! >next-timeout ; \ 2s timeout

\ loops for server and client

Variable requests

: packet-event ( -- )
    next-packet !ticks nip 0= ?EXIT  in-route
    IF    handle-packet  reset-timeout
    ELSE  ." route a packet" cr route-packet  THEN ;

: server-loop-nocatch ( -- ) \ 0 stick-to-core
    BEGIN  packet-event +event  AGAIN ;

: ?int ( throw-code -- throw-code )  dup -28 = IF  bye  THEN ;

event: ->request ( -- ) -1 requests +! msg( ." Request completed" cr ) ;
event: ->timeout ( -- ) requests off msg( ." Request timed out" cr )
true !!timeout!! ;

#2.000.000 d>64 64Constant watch-timeout# \ 2ms timeout check interval
64Variable watch-timeout ticks watch-timeout# 64+ watch-timeout 64!

: request-timeout ( -- )
    ?timeout ?dup-IF  >o rdrop
	>next-timeout
	do-timeout 1 timeouts +!
	timeouts @ timeouts# > IF  ->timeout  THEN
    THEN
    watch-timeout# watch-timeout 64+! ;

: watch-timeout? ( -- )
    watch-timeout 64@ ticker 64@ 64- 64-0< IF
	request-timeout
    THEN ;

: event-loop-nocatch ( -- ) \ 1 stick-to-core
    BEGIN  packet-event  +event  watch-timeout?
	o IF  wait-task @  ?dup-IF  event>  THEN  THEN  AGAIN ;

: n2o:request-done ( -- )
    o-timeout ->request ;

: do-event-loop ( -- )
    BEGIN  ['] event-loop-nocatch catch ?int dup  WHILE
	    s" event-loop: " etype DoError nothrow  REPEAT  drop ;

: create-receiver-task ( -- )
    o 1 stacksize4 NewTask4 dup to receiver-task pass
    init-reply  prep-socks
    >o rdrop  alloc-io c:init
    BEGIN  do-event-loop
	wait-task @ ?dup-IF  ->timeout event>  THEN  AGAIN ;

: event-loop-task ( -- )
    receiver-task 0= IF  create-receiver-task  THEN ;

: client-loop ( requests -- )
    requests !  !ticks reset-timeout
    o IF  up@ wait-task !  THEN  event-loop-task
    BEGIN  stop requests @ 0<= UNTIL ;

: server-loop ( -- )  0 >o rdrop  1 client-loop ;

\ client/server initializer

: init-client ( -- )
    dump( "n2o.dump" r/w create-file throw to dump-fd )
    init-timer new-client init-route prep-socks ;

: init-server ( -- )
    init-timer new-server init-route prep-socks ;

\ connection cookies

object class
    64field: cc-timeout
    field: cc-context
end-class con-cookie

con-cookie >osize @ Constant cookie-size#

Variable cookies s" " cookies $!
con-cookie >osize @ buffer: cookie-adder

#5000000000. d>64 64Constant connect-timeout#

: add-cookie ( -- cookie )
    o cookie-adder >o cc-context !
    ntime d>64 64dup cc-timeout 64!
    o o> cookie-size#  cookies $+! ;

: ?cookie ( cookie -- context true / false )
    ticker 64@ connect-timeout# 64- { 64: timeout }
    0 >r BEGIN  r@ cookies $@len u<  WHILE
	    cookies $@ r@ /string drop >o
	    cc-timeout 64@ timeout 64u< IF
		cookies r@ cookie-size# $del
	    ELSE
		64dup cc-timeout 64@ 64= IF
		    64drop cc-context @ o>
		    cookies r> cookie-size# $del
		    true  EXIT
		THEN
		r> cookie-size# + >r
	    THEN
    REPEAT  64drop rdrop false ;

: cookie>context? ( cookie -- context true / false )
    ?cookie over 0= over and IF
	nip return-addr @ n2o:new-context o 0 >o rdrop swap
    THEN ;

: rtdelay! ( time -- ) recv-tick 64@ 64swap 64- rtdelay 64! ;

\ load net2o commands

require net2o-cmd.fs
require net2o-keys.fs

0 [IF]
Local Variables:
forth-local-words:
    (
     (("debug:" "field:" "sffield:" "dffield:" "64field:") non-immediate (font-lock-type-face . 2)
      "[ \t\n]" t name (font-lock-variable-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
End:
[THEN]
