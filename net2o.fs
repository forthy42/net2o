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

\ timing ticks

[IFDEF] 64bit
    : ticks ( -- u )  ntime drop ;
    ' ticks Alias ticks-u
[ELSE]
    ' ntime Alias ticks
    : ticks-u ( -- u )  ntime drop ;
[THEN]

\ debugging aids

: debug)  ]] THEN [[ ;

true [IF]
    : debug: ( -- ) Create immediate false ,  DOES>
	state @ IF  ]] Literal @ IF [[ ['] debug) assert-canary
	ELSE  @ IF ['] noop assert-canary ELSE postpone (  THEN
	THEN  ;
[ELSE]
    : debug: ( -- )  Create immediate false , DOES>
	@ IF  ['] noop assert-canary  ELSE  postpone (  THEN ;
[THEN]

: hex[ ]] base @ >r hex [[ ; immediate
: ]hex ]] r> base ! [[ ; immediate
: x~~ ]] hex[ ~~ ]hex [[ ; immediate

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
s" connection attempt timed out" exception Constant !!contimeout!!

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
debug: data(
debug: cmd(
debug: send(
debug: firstack(
debug: msg(
debug: profile(
debug: stat(
debug: timeout(
debug: ack(

: +db ( "word" -- ) ' >body on ;

Variable debug-eval

: +debug ( -- )
    BEGIN  argc @ 1 > WHILE
	    1 arg s" +" string-prefix?  WHILE
		1 arg debug-eval $!
		s" db " debug-eval 1 $ins  s" (" debug-eval $+!
		debug-eval $@ evaluate
		shift-args
	REPEAT  THEN ;

\ timing measurements

Variable last-tick

: delta-t ( -- n )
    ticks-u dup last-tick !@ - ;

: timing ;
[IFDEF] timing
    Variable calc-time
    Variable calc1-time
    Variable send-time
    Variable rec-time
    Variable enc-time
    Variable wait-time
    
    : init-timer ( -- )
	ticks-u last-tick ! calc-time off send-time off rec-time off wait-time off
	calc1-time off enc-time off ;
    
    : +calc  profile( delta-t calc-time +! ) ;
    : +calc1 profile( delta-t calc1-time +! ) ;
    : +send  profile( delta-t send-time +! ) ;
    : +enc   profile( delta-t enc-time +! ) ;
    : +rec   profile( delta-t rec-time +! ) ;
    : +wait  profile( delta-t wait-time +! ) ;
    
    : .times ( -- ) profile(
	." wait: " wait-time @ s>f 1n f* f. cr
	." send: " send-time @ s>f 1n f* f. cr
	." rec : " rec-time  @ s>f 1n f* f. cr
	." enc : " enc-time  @ s>f 1n f* f. cr
	." calc: " calc-time @ s>f 1n f* f. cr
	." calc1: " calc1-time @ s>f 1n f* f. cr ) ;
[ELSE]
    ' noop alias +calc immediate
    ' noop alias +calc1 immediate
    ' noop alias +send immediate
    ' noop alias +rec immediate
    ' noop alias +wait immediate
    ' noop alias +enc immediate
    ' noop alias init-timer
    ' noop alias .times
[THEN]

\ Create udp socket

4242 Value net2o-port

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

here 1+ -8 and 6 + here - allot
here maxpacket-aligned buffers# * allot
here maxpacket-aligned buffers# * allot
Constant outbuf' Constant inbuf'

begin-structure net2o-header
    2 +field flags
   16 +field destination
    8 +field addr
end-structure

Variable packetr
Variable packets

2Variable ptimeout
#100000000 Value poll-timeout# \ 100ms
poll-timeout# 0 ptimeout 2!

inbuf'  Constant inbuf
outbuf' Constant outbuf

2Variable socktimeout

: sock-timeout! ( socket -- )  fileno
    ptimeout 2@ >r 1000 / r> socktimeout 2!
    SOL_SOCKET SO_RCVTIMEO socktimeout 2 cells setsockopt drop ;

: read-socket-quick ( socket -- addr u )
    sockaddr_in6 %size alen !
    fileno inbuf maxpacket MSG_WAITALL sockaddr-tmp alen recvfrom
    dup 0< IF  errno 512 + negate throw  THEN
    inbuf swap ;

: read-a-packet ( -- addr u )
    net2o-sock read-socket-quick  1 packetr +! ;

$00000000 Value droprate#

: send-a-packet ( addr u -- n ) +calc
    droprate# IF  rng32 droprate# u< IF
	    \ ." dropping packet" cr
	    2drop 0  EXIT  THEN  THEN
    net2o-sock  1 packets +!
    fileno -rot 0 sockaddr-tmp alen @ sendto +send ;

\ clients routing table

Variable routes

: init-route ( -- )  s" " routes hash@ $! ; \ field 0 is me, myself

: info>string ( addr -- addr u )
    dup ai_addr @ swap ai_addrlen l@
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
    THEN ;

sockaddr_in %size buffer: lastaddr
Variable lastn2oaddr

: insert-address ( addr u -- net2o-addr )
    2dup lastaddr over str= IF  2drop lastn2oaddr @  EXIT  THEN
    2dup routes #key dup -1 = IF
	drop 2dup lastaddr swap move
	s" " 2over routes #! routes #key  dup lastn2oaddr !
    ELSE
	nip nip
    THEN ;

: insert-ip ( addr u port -- net2o-addr )
    get-info info>string insert-address ;

: address>route ( -- n/-1 )
    sockaddr-tmp alen @ insert-address ;
: route>address ( n -- ) dup >r
    routes #.key dup 0= IF  ." no address: " r> hex. cr drop  EXIT  THEN
    $@ sockaddr-tmp swap dup alen ! move  rdrop ;

\ route an incoming packet

Variable return-addr

\ these are all stubs for now

: ins-source ( addr packet -- )  >r
    reverse 0  r> destination 2! ;
: get-source ( packet -- addr )
    destination 2@ drop  reverse ;
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

: .header ( addr -- ) base @ >r hex
    dup c@ >r
    min-size r> datasize# and lshift hex. ." bytes to "
    addr 64@ 64. cr
    r> base ! ;

\ packet delivery table

0 Value j^

\ each source has multiple destination spaces

Variable dest-addr

: >ret-addr ( -- )
    inbuf get-source return-addr ! ;
: >dest-addr ( -- )
    inbuf addr @  inbuf body-size 1- invert and dest-addr ! ;

' dffield: Alias 64field:

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
field: dest-cookies
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
field: data-ackbits-buf
field: data-firstack0#
field: data-firstack1#
field: data-lastack#
end-structure
\ job context structure

begin-structure context-struct
field: context#
field: context-state
field: return-address
64field: recv-tick
field: recv-addr
field: recv-flag
field: recv-high
field: cmd-buf#
field: file-state
field: blocksize
field: blockalign
field: crypto-key
field: timeout-xt
field: ack-xt

field: data-map
field: data-rmap
field: data-resend
field: data-b2b

field: code-map
field: code-rmap

field: ack-state
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
64field: rtdelay \ ns
64field: lastack \ ns
field: flyburst
field: flybursts
64field: lastslack
64field: lastdeltat
64field: slackgrow
\ flow control, receiver part
64field: burst-ticks
64field: firstb-ticks
64field: lastb-ticks
64field: delta-ticks
field: acks
64field: last-rate
\ experiment: track previous b2b-start
64field: last-rtick
field: last-raddr
\ cookies
field: last-ackaddr
\ state machine
field: expected
field: total
field: received
\ statistics
field: timing-stat
64field: last-time
\ make timestamps smaller
64field: time-offset
end-structure

begin-structure timestamp
64field: ts-ticks
end-structure

begin-structure reply
field: reply-offset
field: reply-len
end-structure

begin-structure timestats
sffield: ts-delta
sffield: ts-slack
sffield: ts-reqrate
sffield: ts-rate
sffield: ts-grow
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
: allocate-bits ( size -- addr )
    dup >r cell+ allocateFF dup r> + off ; \ last cell is off

: map-string ( addr u addrx -- addrx u2 )
    >r tuck r@ dest-size 2!
    dup allocatez r@ dest-raddr !
    state# 2* allocatez r@ dest-ivsgen !
    >code-flag @ IF
	dup addr>replies allocatez r@ dest-timestamps !
    ELSE
	dup addr>ts allocatez r@ dest-timestamps !
	dup addr>ts allocatez r@ dest-cookies !
	dup addr>bits bits>bytes allocate-bits r@ data-ackbits0 !
	dup addr>bits bits>bytes allocate-bits r@ data-ackbits1 !
	s" " r@ data-ackbits-buf $!
    THEN
    r@ data-lastack# on
    drop
    j^ r@ dest-job !
    r> rdata-struct ;

: map-source-string ( addr u addrx -- addrx u2 )
    >r tuck r@ dest-size 2!
    dup allocatez r@ dest-raddr !
    dup addr>ts allocatez r@ dest-cookies !
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

8 Value bursts# \ number of 
8 Value delta-damp#
bursts# 2* 2* 1- Value tick-init \ ticks without ack
#1000000 max-size^2 lshift Value bandwidth-init \ 32µs/burst=2MB/s
64#-1 64Constant never
2 Value flybursts#

Variable init-context#

: init-flow-control ( -- )
    max-int64 64-2/ j^ min-slack 64!
    max-int64 64-2/ 64negate j^ max-slack 64!
    max-int64 j^ rtdelay 64!
    flybursts# dup j^ flybursts ! j^ flyburst !
    ticks j^ lastack 64! \ asking for context creation is as good as an ack
    bandwidth-init n>64 j^ ns/burst 64!
    never               j^ next-tick 64!
    64#0                j^ extra-ns 64! ;

: n2o:new-context ( addr -- )
    context-struct allocate throw to j^
    j^ context-struct erase
    init-context# @ j^ context# !  1 init-context# +!
    dup return-addr !  j^ return-address !
    s" " j^ data-resend $!
    wurst-key state# j^ crypto-key $!
    init-flow-control
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

\ timing records

: net2o:track-timing ( -- ) \ initialize timing records
    s" " j^ timing-stat $! ;

: )stats ]] THEN [[ ;
: stats( ]] j^ timing-stat @ IF [[ ['] )stats assert-canary ; immediate

: net2o:timing$ ( -- addr u )
    stats( j^ timing-stat $@  EXIT ) ." no timing stats" cr s" " ;
: net2o:/timing ( n -- )
    stats( j^ timing-stat 0 rot $del ) ;

: net2o:rec-timing ( addr u -- ) \ do some dumps
    bounds ?DO
	I ts-delta sf@ f>64 j^ last-time 64+!
	j^ last-time 64@ 64>f 1n f* fdup f.
	j^ time-offset 64@ &10000000000 [IFDEF] 64bit mod [ELSE] um/mod drop [THEN] s>f 1n f* f+ f. 
	I ts-slack sf@ 1u f* f.
	tick-init 1+ maxdata * 1k fm* fdup
	I ts-reqrate sf@ f/ f.
	I ts-rate sf@ f/ f.
	I ts-grow sf@ 1u f* f.
	." timing" cr
    timestats +LOOP ;

timestats buffer: stat-tuple

: stat+ ( addr -- )  stat-tuple timestats  j^ timing-stat $+! ;

\ flow control

: ticks-init ( ticks -- )
    64dup j^ bandwidth-tick 64!  j^ next-tick 64! ;

: >rtdelay ( client serv -- client serv )
    j^ recv-tick 64@ 64dup j^ lastack 64!
    64over 64- j^ rtdelay 64min! ;

: timestat ( client serv -- )
    64dup 64-0=     IF  64drop 64drop  EXIT  THEN
    64dup 64#-1 64= IF  64drop 64drop  EXIT  THEN
    timing( over . dup . ." acktime" cr )
    >rtdelay  64- 64dup j^ lastslack 64!
    j^ lastdeltat 64@ delta-damp# 64rshift
    64dup j^ min-slack 64+! 64negate j^ max-slack 64+!
    64dup j^ min-slack 64min!
    j^ max-slack 64max! ;

: b2b-timestat ( client serv -- )
    64dup 64-0=     IF  64drop 64drop  EXIT  THEN
    64dup 64#-1 64= IF  64drop 64drop  EXIT  THEN
    64- j^ lastslack 64@ 64- 64negate slack( 64dup 64. .j ." grow" cr )
    j^ slackgrow 64! ;

: map@ ( -- addr/0 )
    0 j^ 0= ?EXIT  j^ data-map @ 0= ?EXIT
    drop j^ data-map $@ drop ;
: rmap@ ( -- addr/0 )
    0 j^ 0= ?EXIT  j^ data-rmap @ 0= ?EXIT
    drop j^ data-rmap $@ drop ;

: >offset ( addr map -- addr' flag ) >r
    r@ dest-vaddr @ - dup r> dest-size @ u< ;

: >flyburst ( -- )
    j^ flyburst @ j^ flybursts max!@ \ reset bursts in flight
    0= IF  j^ recv-tick 64@ ticks-init
	bursts( .j ." restart bursts " j^ flybursts ? cr )
    THEN ;

: >timestamp ( time addr -- ts-array index / 0 0 )
    >flyburst
    >r j^ time-offset @ + r>
    map@ dup 0= IF  2drop 0 0  EXIT  THEN  >r
    r@ >offset  IF
	r@ dest-tail @ over - 0 max addr>bits j^ window-size !
	addr>ts r> dest-timestamps @ swap
    ELSE  drop rdrop 0 0  THEN ;

: net2o:ack-addrtime ( ticks addr -- )
    >timestamp
    over  IF
	dup tick-init 1+ timestamp * u>
	IF  + dup >r  dup ts-ticks 64@
	    r> tick-init 1+ timestamp * - ts-ticks 64@ 64- j^ lastdeltat 64!
	ELSE  +  THEN
	ts-ticks 64@ timestat
    ELSE  2drop 64drop  THEN ;

: net2o:ack-b2btime ( ticks addr -- )  >timestamp
    over  IF  + ts-ticks 64@ b2b-timestat
    ELSE  2drop 64drop  THEN ;

#50000000 Value slack-default# \ 50ms slack leads to backdrop of factor 2
#10000000 Value slack-bias#

: slack# ( -- n )  j^ max-slack @ j^ min-slack @ - 2/ 2/ slack-default# max ;

: net2o:set-flyburst ( -- bursts )
    j^ rtdelay 64@ 64>n j^ ns/burst 64@ 64>n / flybursts# +
    bursts( dup . .j ." flybursts" cr ) dup j^ flyburst ! ;
: net2o:max-flyburst ( bursts -- ) j^ flybursts max!@
    0= IF  bursts( .j ." start bursts" cr ) THEN ;

: >slack-exp ( rate -- rate' )
    j^ lastslack 64@ j^ min-slack 64@ 64- 64>n
    slack( dup . j^ min-slack ? .j ." slack" cr )
    stats( dup s>f stat-tuple ts-slack sf! )
    slack-bias# - 0 max slack# 2* 2* min
    s>f slack# fm/ 2e fswap f** fm* f>s
    ( slack# / lshift ) ;

: slackext ( -- slack )
    j^ slackgrow 64@ j^ extra-ns 64@ 64- 64#0 64max 64>n
    j^ ns/burst 64@ 64>n j^ extra-ns 64@ 64>n bounds */
    j^ window-size @ tick-init 1+ bursts# - */ n>64
    j^ slackgrow 64@ j^ extra-ns 64@ 64min 64max ;

: rate-limit ( rate -- rate' ) \ obsolete
    \ not too quickly go slower or faster!
    64>n j^ last-ns/burst 64@ 64>n
    ?dup-IF  dup >r 2* 2* min r> 2/ 2/ max  THEN
    dup n>64 j^ last-ns/burst 64! n>64 ;

: extra-limit ( rate -- rate' )
    dup j^ extra-ns 64@ 64>n 2* 2* u> IF
	j^ extra-ns 64@ 64>n + dup 2/ 2/ dup n>64 j^ extra-ns 64! -
    THEN ;

: >extra-ns ( rate -- rate' )
    64>n dup >slack-exp  tuck slackext 64>n rot */
    2/ dup n>64 j^ extra-ns 64! + ( extra-limit ) n>64 ;

: rate-stat1 ( rate deltat -- )
    stats( j^ recv-tick 64@ j^ time-offset 64@ 64-
           64dup j^ last-time 64!@ 64- 64>f stat-tuple ts-delta sf!
           64over 64>f stat-tuple ts-reqrate sf! )
    rate( 64over 64. .j ." clientrate" cr )
    deltat( 64dup 64. j^ lastdeltat ? .j ." deltat" cr ) ;

: rate-stat2 ( rate -- )
    rate( 64dup 64. .j ." rate" cr )
    stats( 64dup j^ extra-ns 64@ 64+ 64>f stat-tuple ts-rate sf!
           j^ slackgrow 64@ 64>f stat-tuple ts-grow sf! 
           stat+ ) ;

: net2o:set-rate ( rate deltat -- )  rate-stat1
    64drop >extra-ns ( rate-limit ) rate-stat2
    j^ ns/burst 64!@
    bandwidth-init n>64 64= IF \ first acknowledge
	net2o:set-flyburst
	net2o:max-flyburst
    THEN ;

\ acknowledge

Create resend-buf  0 , 0 ,
$20 Value mask-bits#
: >mask0 ( addr mask -- addr' mask' )
    BEGIN  dup 1 and 0= WHILE  1 rshift >r maxdata + r>  dup 0= UNTIL  THEN ;
: net2o:resend-mask ( addr mask -- )
    resend( ." mask: " hex[ 64>r dup . 64r> 64dup 64. ]hex cr )
    j^ data-resend $@ bounds ?DO
	over I cell+ @ swap dup maxdata mask-bits# * + within IF
	    over I 2@ rot >r
	    BEGIN  over r@ u>  WHILE  2* >r maxdata - r>  REPEAT
	    rdrop nip or >mask0
	    resend( I 2@ hex[ ." replace: " swap . . ." -> "
	    64>r dup . 64r> 64dup 64. cr ]hex )
	    I 2!  UNLOOP  EXIT
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

\ cookie stuff

: cookie! ( map -- ) dup 0= IF  drop  EXIT  THEN  >r
    wurst-cookie
    dest-addr @ r@ >offset 0= IF  rdrop 2drop  EXIT  THEN
    addr>ts r> dest-cookies @ + 64! ;

: send-cookie ( -- )  map@ cookie! ;
: recv-cookie ( -- ) rmap@ cookie! ;

[IFDEF] 64bit
    : cookie+ ( addr bitmap map -- sum ) -rot >r
	addr>ts over dest-size @ addr>ts umin
	swap dest-cookies @ + 0
	BEGIN  r@ 1 and IF  over @ +  THEN
	>r cell+ r> r> 1 rshift dup >r 0= UNTIL
	rdrop nip ;
[ELSE]
    : cookie+ ( addr bitmap map -- sum ) { map } >r >r
	addr>ts map dest-size @ addr>ts umin
	map dest-cookies @ + { addr } 64#0
	BEGIN  r@ 1 and IF  addr 64@ 64+  THEN
	addr 64'+ to addr r> r> 1 64rshift 64dup >r >r 64-0= UNTIL
	64r> 64drop ;
[THEN]

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
    code-packet @ wurst-outbuf-encrypt
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

: >send ( addr n -- )  >r  r@ 64bit# or outbuf c!
    outbody min-size r> lshift move ;

: bandwidth+ ( -- )  j^ 0= ?EXIT
    j^ ns/burst 64@ 64>n tick-init 1+ / n>64 j^ bandwidth-tick 64+! ;

: burst-end ( -- )  j^ data-b2b @ ?EXIT
    ticks j^ bandwidth-tick 64@ 64max j^ next-tick 64! ;

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

Variable no-ticks

: ts-ticks! ( addr map -- )
\    no-ticks @ IF  2drop EXIT  THEN
    >r addr>ts r> dest-timestamps @ + >r ticks r> ts-ticks
    dup 64@ 64-0= 0= IF  64on 64drop  EXIT  THEN 64! ;

: net2o:send-tick ( addr -- )
    j^ data-map $@ drop >r
    r@ dest-raddr @ - dup r@ dest-size @ u<
    IF  r> ts-ticks!  ELSE  drop rdrop  THEN ;

: net2o:prep-send ( addr u dest addr -- addr taddr target n len )
    2>r  over  net2o:send-tick
    send-size min-size over lshift
    2r> 2swap ;

: net2o:send-packet ( addr u dest addr -- len )
    net2o:prep-send >r sendX r> ;

\ synchronous sending

: data-to-send ( -- flag )
    resend$@ nip 0> dest-tail$@ nip 0> or ;

: net2o:resend ( -- )
    no-ticks on resend$@ net2o:get-resend 2dup 2>r
    net2o:prep-send /resend
    2r> resend( ." resending " over hex. dup hex. outflag @ hex. cr ) 2drop ;

: net2o:send ( -- )
    no-ticks off dest-tail$@ net2o:get-dest 2dup 2>r
    net2o:prep-send /dest-tail
    2r> send( ." sending " over hex. dup hex. outflag @ hex. cr ) 2drop ;

: net2o:send-chunk ( -- )
    j^ ack-state @ outflag or!
    bursts# 1- j^ data-b2b @ = IF
	\ send a new packet for timing path
	dest-tail$@ nip IF  net2o:send  ELSE  net2o:resend  THEN
    ELSE
	resend$@ nip IF  net2o:resend  ELSE  net2o:send  THEN
    THEN
    data-to-send 0= IF
	resend-toggle# outflag xor!  ack-toggle# outflag xor!
	sendX  never j^ next-tick 64!
    ELSE  sendX  THEN ;

: bandwidth? ( -- flag )  ticks j^ next-tick 64@ 64- 64-0>=
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
	j^ extra-ns @ j^ bandwidth-tick +!
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
	    bursts# 1- j^ data-b2b !
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
	    msg( ." send a chunk" cr )
	    send-a-chunk
	ELSE
	    drop msg( .nosend )
	    chunks chunks+ @ chunks-struct * chunks-struct $del
	    false
	THEN
    ELSE  drop chunks+ off false  THEN ;

: next-chunk-tick ( -- tick )
    64#-1 chunks $@ bounds ?DO
	I chunk-context @ next-tick 64@ 64umin
    chunks-struct +LOOP ;

: send-another-chunk ( -- flag )  false  0 >r
    BEGIN  BEGIN  send-chunks-async  WHILE  drop rdrop true 0 >r  REPEAT
	    chunks+ @ 0= IF  r> 1+ >r  THEN
	r@ 2 u>=  UNTIL  rdrop ;

Variable sendflag  sendflag off
: send?  ( -- flag )  sendflag @ ;
: send-anything? ( -- flag )  chunks $@len 0> ;

\ rewind buffer to send further packets

: rewind-timestamps ( map -- )  >r
    r@ code-flag @ IF  rdrop  EXIT  THEN
    r@ dest-timestamps @ r> dest-size @ addr>ts erase ;

: rewind-buffer ( map -- ) >r
    1 r@ dest-round +!
    r@ dest-tail off  r@ data-head off
    r@ regen-ivs-all  r> rewind-timestamps ;

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

Create pollfds   here pollfd %size dup allot erase

: fds!+ ( fileno flag addr -- addr' )
     >r r@ events w!  r@ fd l!  r> pollfd %size + ; 

: prep-socks ( -- )  pollfds >r
    net2o-sock  fileno POLLIN  r> fds!+ drop ;

: clear-events ( -- )  pollfds
    2 0 DO  0 over revents w!  pollfd %size +  LOOP  drop ;

: timeout! ( -- )
    next-chunk-tick 64dup 64#-1 64= 0= >r ticks 64- 64>n dup 0>= r> or
    IF    0 max 0 ptimeout 2!
    ELSE  drop poll-timeout# 0 ptimeout 2!  THEN ;

: poll-sock ( -- flag )
    eval-queue  clear-events  timeout!
    pollfds 1
[ environment os-type s" linux" string-prefix? ] [IF]
    ptimeout 0 ppoll 0>
[ELSE]
    ptimeout cell+ @ #1000000 / poll 0>
[THEN] +wait
;

: read-a-packet4/6 ( -- addr u )
    pollfds revents w@ POLLIN = IF
	read-a-packet  0 pollfds revents w! +rec EXIT  THEN
    0 0 ;

: try-read-packet ( -- addr u / 0 0 )
    poll-sock drop read-a-packet4/6 ;

: next-packet ( -- addr u )
    send-anything? sendflag !
    BEGIN  sendflag @ 0= IF  try-read-packet dup 0=  ELSE  0. true  THEN
    WHILE  2drop send-another-chunk sendflag !  REPEAT
    sockaddr-tmp alen @ insert-address  inbuf ins-source
    over packet-size over <> !!size!! and throw ;

: next-client-packet ( -- addr u )
    try-read-packet  2dup d0= ?EXIT
    sockaddr-tmp alen @ insert-address
    inbuf ins-source
    over packet-size over <> IF  !!size!! throw  THROW  THEN
    \ ELSE  hex.  ." Unknown source"  0 0  THEN
;

: net2o:timeout ( ticks -- ) \ print why there is nothing to send
    >flyburst
    timeout( ." timeout? " . send-anything? . chunks+ ? next-chunk-tick dup . cr )
    drop ;

Defer queue-command ( addr u -- )
' dump IS queue-command

: pow2? ( n -- n )  dup dup 1- and 0<> !!pow2!! and throw ;

Variable validated

$01 Constant crypt-val
$02 Constant own-crypt-val
$04 Constant login-val
$08 Constant cookie-val

: crypt?     ( -- flag )  validated @ crypt-val     and ;
: own-crypt? ( -- flag )  validated @ own-crypt-val and ;
: login?     ( -- flag )  validated @ login-val     and ;
: cookie?    ( -- flag )  validated @ cookie-val    and ;

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
        ticks j^ recv-tick 64! \ time stamp of arrival
	dup 0> wurst-inbuf-decrypt 0= IF
	    inbuf .header
	    ." invalid packet to " dest-addr @ hex. cr
	    IF  drop  THEN  EXIT  THEN
	crypt-val validated ! \ ok, we have a validated connection
	dup 0< IF \ data packet
	    data( ." received: " inbuf .header cr )
	    drop  >r inbuf packet-data r> swap move
	    j^ ack-xt perform
	ELSE \ command packet
	    drop
	    >r inbuf packet-data r@ swap dup >r move
	    r> r> swap queue-command
	THEN
    THEN ;

: route-packet ( -- )  inbuf dup packet-size send-a-packet drop ;

: server-event ( -- )
    next-packet 2drop  in-route
    IF    handle-packet
    ELSE  ." route a packet" cr route-packet  THEN ;

: client-event ( addr u -- )
    2drop in-check
    IF    handle-packet
    ELSE  ( drop packet )  THEN ;

\ loops for server and client

0 Value server?
Variable requests
Variable timeouts
: reset-timeout  20 timeouts ! ; \ 2s timeout

Defer do-timeout  ' noop IS do-timeout

: server-loop-nocatch ( -- )
    BEGIN  server-event +calc1  AGAIN ;

: ?int ( throw-code -- throw-code )  dup -28 = IF  bye  THEN ;

: server-loop ( -- ) true to server?
    BEGIN  ['] server-loop-nocatch catch ?int dup  WHILE
	    DoError nothrow  REPEAT  drop ;

: client-loop-nocatch ( -- )
    BEGIN  next-client-packet dup
	IF    client-event +calc1 reset-timeout
	ELSE  2drop do-timeout -1 timeouts +!  THEN
     timeouts @ 0<=  requests @ 0= or  UNTIL ;

: client-loop ( requests -- )  requests !  reset-timeout  false to server?
    BEGIN  ['] client-loop-nocatch catch ?int dup  WHILE
	    DoError nothrow  REPEAT  drop ;

\ client/server initializer

: init-client ( -- )
    init-timer new-client init-route prep-socks ;

: init-server ( -- )
    init-timer new-server init-route prep-socks ;

\ load net2o commands

include net2o-cmd.fs

0 [IF]
Local Variables:
forth-local-words:
    (
     (("debug:" "field:" "sffield:" "dffield:" "64field:") non-immediate (font-lock-type-face . 2)
      "[ \t\n]" t name (font-lock-variable-name-face . 3))
     ("[a-z]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
End:
[THEN]
