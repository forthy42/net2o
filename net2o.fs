\ Internet 2.0 experiments

require unix/socket.fs
require string.fs
require struct0x.fs
require nacl.fs

: safe/string ( c-addr u n -- c-addr' u' )
\G protect /string against overflows.
    dup negate >r  dup 0> IF
        /string dup r> u>= IF  + 0  THEN
    ELSE
        /string dup r> u< IF  + 1+ -1  THEN
    THEN ;

: or! ( value addr -- )
    >r r@ @ or r> ! ;

\ Create udp socket

4242 Constant net2o-udp

0 Value net2o-sock
0 Value net2o-sock6

: new-server ( -- )
    net2o-udp create-udp-server s" w+" c-string fdopen to net2o-sock
    net2o-udp create-udp-server6 s" w+" c-string fdopen to net2o-sock6
;

: new-client ( -- )
    new-udp-socket s" w+" c-string fdopen to net2o-sock
    new-udp-socket6 s" w+" c-string fdopen to net2o-sock6 ;

$101A Constant maxpacket

here 1+ -8 and 6 + here - allot here maxpacket allot Constant inbuf
here 1+ -8 and 6 + here - allot here maxpacket allot Constant outbuf

2 8 2Constant address%

struct
    short% field flags
    address% field destination
    address% field addr
    address% field nonce
end-struct net2o-header

: read-a-packet ( -- addr u )
    net2o-sock inbuf maxpacket read-socket-from ;

: read-a-packet6 ( -- addr u )
    net2o-sock6 inbuf maxpacket read-socket-from ;

: send-a-packet ( addr u -- n )
    sockaddr-tmp w@ AF_INET6 = IF  net2o-sock6  ELSE  net2o-sock  THEN
    fileno -rot 0 sockaddr-tmp alen @ sendto ;

\ clients routing table

8 Value route-bits
8 Constant /address
' dfloats Alias addresses
0 Value routes

: init-route ( -- )
    routes IF  routes free  0 to routes  throw  THEN
    /address route-bits lshift dup allocate throw to routes
    routes swap erase ;

: info>string ( addr -- addr u )
    dup ai_addr @ swap ai_addrlen @ ;

: route-hash ( addr u -- hash )
    route-bits (hashkey1) ;

: sock-route! ( addr u -- hash )
    2dup route-hash dup >r addresses routes + $! r> ;
: insert-address ( addr u -- net2o-addr )
    sock-route! $38 lshift ;
: check-address ( addr u -- net2o-addr flag )
    2dup route-hash dup >r addresses routes +
    $@ str= r> $38 lshift swap ;
\ FIXME: doesn't check for collissons

: insert-ipv4 ( addr u port -- net2o-addr )
    get-info info>string insert-address ;

: address>route ( -- n/-1 )
    sockaddr-tmp alen @ check-address 0= IF  drop -1  THEN ;
: route>address ( n -- )
    addresses routes + $@ dup alen ! sockaddr-tmp swap move ;

\ bit reversing

: bitreverse8 ( u1 -- u2 )
    0 8 0 DO  2* over 1 and + swap 2/ swap  LOOP  nip ;

Create reverse-table $100 0 [DO] [I] bitreverse8 c, [LOOP]

: reverse8 ( c1 -- c2 ) reverse-table + c@ ;
: reverse64 ( x1 -- x2 )
    0 8 0 DO  8 lshift over $FF and reverse8 or
	swap 8 rshift swap  LOOP  nip ;

\ route an incoming packet

: packet-route ( orig-addr addr -- flag ) >r
    r@ destination c@ 0= IF  drop  true  rdrop EXIT  THEN \ local packet
    r@ destination c@ route>address
    r@ destination dup 1+ swap /address 1- move
    r> destination /address 1- + c!  false ;

: in-route ( -- flag )  address>route reverse64  inbuf packet-route ;
: in-check ( -- flag )  address>route -1 <> ;
: out-route ( -- flag )  0  outbuf packet-route ;

\ packet&header size

$80 Constant destsize#
$40 Constant addrsize#
$20 Constant noncesize#
$07 Constant datasize#

: (header-size ( x -- u ) >r 2
    r@ destsize#  and IF  8  ELSE  2  THEN +
    r@ addrsize#  and IF  8  ELSE  2  THEN +
    r@ noncesize# and IF  8  ELSE  0  THEN +
    rdrop ;

Create header-sizes  $100 0 [DO] [I] (header-size c, $20 [+LOOP]

: header-size ( addr -- n )  c@ 5 rshift header-sizes + c@ ;
: body-size ( addr -- n ) $20 swap c@ datasize# and lshift ;
: packet-size ( addr -- n )
    dup header-size swap body-size + ;
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

$08 Constant endnode-fc#
$01 Constant first-ack#
$02 Constant send-ack#

\ short packet information

: chunk@ ( addr flag -- value addr' )
    IF  dup be-ux@ swap 8 +  ELSE  dup be-uw@ swap 2 +  THEN ;

: .header ( addr -- )
    dup c@ >r 2 +
    r@ datasize# and 'A' + emit
    r@ destsize# and chunk@
    r> addrsize# and chunk@
    drop swap
    ."  to " hex. ."  @ " hex. cr ;

\ packet delivery table

Variable job-context

\ each source has multiple destination spaces

0 Value delivery-table
Variable return-addr
Variable dest-addr
8 Value delivery-bits

: init-delivery-table ( -- )
    delivery-table IF  delivery-table free  0 to delivery-table  throw  THEN
    1 cells delivery-bits lshift dup allocate throw to delivery-table
    delivery-table swap erase ;

: >ret-addr ( -- )
    inbuf destination be-ux@ reverse64 return-addr ! ;
: >dest-addr ( -- )
    inbuf addr be-ux@  inbuf body-size 1- invert and dest-addr ! ;

: ret-hash ( -- n )  return-addr 1 cells delivery-bits (hashkey1) ;

5 cells Constant /dest

: check-dest ( -- addr 1/t / f )  job-context off
    ret-hash cells delivery-table +
    dup @ 0= IF  drop false  EXIT  THEN
    $@ bounds ?DO
	I 2@ 1- bounds dest-addr @ within
	0= IF
	    I cell+ 2@ dest-addr @ swap - +
	    I 4 cells + @ IF  1  ELSE  -1  THEN
	    I 3 cells + @ job-context !
	    UNLOOP  EXIT  THEN
    /dest +LOOP
    false ;

\ Destination mapping contains
\ addr u - range of virtal addresses
\ addr' - real start address
\ context - for exec regions, this is the job context

                    \  u   addr real-addr job code-flag
Create dest-mapping    0 , 0 ,  0 ,       0 , here 0 ,
Constant code-flag
                    \  u   addr real-addr job head tail
Create source-mapping  0 , 0 ,  0 ,       0 , 0 ,  0 ,

begin-structure data-struct
field: data-size
field: data-vaddr
field: data-raddr
field: data-job
field: data-head
field: data-tail
end-structure

: map-string ( addr u addr' addrx -- addrx u2 )
    >r r@ 2 cells + ! r@ 2!
    job-context @ r@ 3 cells + !
    r> /dest ;

: map-dest ( addr u addr' -- )
    ret-hash cells delivery-table + >r
    r@ @ 0= IF  s" " r@ $!  THEN
    dest-mapping map-string r> $+! ;

: map-source ( addr u addr' -- addr u )
    source-mapping map-string 2 cells + ;

: n2o:new-map ( addr u -- )  code-flag off
    dup allocate throw map-dest ;

: n2o:new-code-map ( addr u -- )  code-flag on
    dup allocate throw map-dest ;

\ job context structure

begin-structure context-struct
field: return-address
field: cmd-out
field: file-handles
field: crypto-keys
field: auth-info
field: status
field: data-map
field: data-resend
field: code-map
field: data-ack
field: code-ack
field: last-ack
field: delta-ack
field: pending-ack
end-structure

begin-structure cmd-struct
field: cmd-accu#
field: cmd-accu
field: cmd-slot
field: cmd-extras
field: cmd-buf#
$800 +field cmd-buf
end-structure

: n2o:new-context ( -- addr )  context-struct allocate throw >r
    r@ context-struct erase  return-addr @ r@ return-address !
    s" " r@ cmd-out $!
    s" " r@ data-ack $!
    s" " r@ data-resend $!
    s" " r@ code-ack $!
    cmd-struct r@ cmd-out $!len
    r@ cmd-out $@ erase r> ;

: n2o:new-data ( addr u -- )  dup allocate throw map-source
    job-context @ data-map $! ;
: n2o:new-code ( addr u -- )  dup allocate throw map-source
    job-context @ code-map $! ;

: data$@ ( -- addr u )
    job-context @ data-map $@ drop >r
    r@ data-raddr @  r@ data-size @ r> data-head @ safe/string ;
: /data ( u -- )
    job-context @ data-map $@ drop data-head +! ;
: data-tail$@ ( -- addr u )
    job-context @ data-map $@ drop >r
    r@ data-raddr @  r@ data-head @ r> data-tail @ safe/string ;
: /data-tail ( u -- )
    job-context @ data-map $@ drop data-tail +! ;
: data-dest ( -- addr )
    job-context @ data-map $@ drop >r
    r@ data-vaddr @ r> data-tail @ + ;

\ code sending around

: code-dest ( -- addr )
    job-context @ code-map $@ drop >r
    r@ data-vaddr @ r> data-tail @ + ;

\ acknowledge map

2Variable 'range

[IFUNDEF] umax
    : umax ( u1 u2 -- u )
	2dup u<
	if
	    swap
	then
	drop ;
[THEN]

: range+ ( addr1 addr2 addr u -- addr1' addr2' )
    1+ bounds rot umin >r umax r> tuck - 1- ;

: add-range ( addr u map -- )
    >r 1+ bounds r@ $@ bounds ?DO
	over I 2@ 1+ bounds within
	over I 2@ 1+ bounds within and 0=  IF
	    I 2@ range+ I 2!
	    I UNLOOP
	    r@ $@ drop -
	    r@ $@ 2 pick /string 2 cells > IF
		dup 2@ 1+ bounds 2 pick cell+ cell+ 2@
		range+ rot 2!
		cell+ cell+ 2 cells r> -rot $del
	    ELSE  2drop  rdrop  THEN
	    EXIT  THEN
	over I 2@ drop u< IF
	    I  UNLOOP  r@ $@ drop - >r tuck - 1- 'range 2!
	    'range 2 cells r> r> swap $ins  EXIT
	THEN
    2 cells +LOOP
    tuck - 1- 'range 2! 'range 2 cells r> $+! ;

: del-range ( addr u map -- )
    >r 1+ bounds r@ $@ bounds ?DO
	over I 2@ 1+ bounds within
	over I 2@ 1+ bounds within and 0=  IF
	    2dup I 2@ 1+ bounds rot u>= IF
		u>= IF  0. I 2!
		ELSE
		    over I 2@ 1+ bounds drop 1- swap 1- tuck - I 2!
		THEN
	    ELSE
		u>= IF  dup I 2@ 1+ bounds nip tuck - I 2!
		ELSE
		    swap 1- swap  I UNLOOP
		    dup r@ $@ drop - 2 cells + >r >r r@ 2@ 1+ bounds
		    rot over - r> 2! over - 1- 'range 2!
		    'range 2 cells r> r> swap $ins  EXIT
		THEN
	    THEN
	THEN
    2 cells +LOOP  2drop r> dup $@len 0  ?DO
	dup $@ I safe/string drop 2@ d0= IF
	    dup I 2 cells $del  0
	ELSE  2 cells  THEN
    +LOOP  drop ;

\ acknowledge handling

: avg! ( n addr -- )
    dup @ 0= IF  !  EXIT  THEN
    >r 2/ 2/ r@ @ dup 2/ 2/ - + r> ! ;

: net2o:firstack ( utime -- )
    job-context @ last-ack ! ;
: net2o:ack ( utime -- )
    dup job-context @ last-ack dup @ >r ! r> -
    job-context @ delta-ack avg! ;
: net2o:unacked ( addr u -- )  1+ job-context @ data-ack add-range ;
: net2o:ack-range ( addr u -- )
    ." Acknowledge range: " swap . . cr
    ." Ack delta: " job-context @ delta-ack @ . cr ;
: net2o:resend ( addr u -- )
    2dup job-context @ data-resend add-range
    ." Resend: " swap . . cr ;
: >real-range ( addr -- addr' )
    job-context @ data-map $@ drop data-raddr @ + ;
: resend$@ ( -- addr u )
    job-context @ data-resend $@  IF
	2@ swap >real-range swap
    ELSE  drop 0 0  THEN ;
: resend-dest ( -- addr )
    job-context @ data-resend $@ drop 2@ drop ;
: /resend ( u -- )  job-context @ data-resend dup $@ drop 2@ drop
    -rot del-range ;

\ file handling

: >throw ( error -- ) throw ( stub! ) ;

: ?handles ( -- )
    job-context @ file-handles @ 0= IF
	s" " job-context @ file-handles $!
    THEN ;    

\ open a file - this needs *way more checking*!

: id>file ( id -- fid )
    >r job-context @ file-handles $@ r> cells safe/string
    0= >throw  @ ;

: n2o:open-file ( addr u mode id -- )
    ?handles
    >r job-context @ file-handles $@ r@ cells safe/string
    IF    dup @ ?dup-IF  close-file >throw  THEN  dup off
    ELSE  drop r@ 1+ cells job-context @ file-handles $!len
	job-context @ file-handles $@ drop r@ cells +  THEN rdrop >r
    dup 2over ." open file: " type ."  with mode " . cr
    open-file >throw r> ! ;

: n2o:close-file ( id -- )
    ?handles
    >r job-context @ file-handles $@ r@ cells safe/string
    IF
	dup @ ?dup-IF  close-file >throw  THEN  dup off
    THEN
    drop rdrop ;

\ client initializer

: init-client ( -- )
    new-client init-route init-delivery-table ;

: init-server ( -- )
    new-server init-route init-delivery-table ;

\ send blocks of memory

: set-dest ( addr target -- )
    outbuf destination be-x!  outbuf addr be-x! ;

Variable outflag  outflag off

: set-flags ( -- )  outflag @ outbuf 1+ c! outflag off
    destsize# addrsize# or outbuf c! ;

: c+!  ( n addr -- )  dup >r c@ + r> c! ;

: outbody ( -- addr ) outbuf packet-body ;
: send-packet ( -- )
\    ." send " outbuf .header
    out-route drop
    outbuf dup packet-size
    send-a-packet drop ;

: >send ( addr n -- )  >r outbody $20 r@ lshift move r> outbuf c+! ;
: sendX ( addr taddr target n -- )
    >r set-dest  set-flags r> >send send-packet ;

\ send chunk

: net2o:get-dest ( taddr target -- )
    data-dest job-context @ return-address @ ;
: net2o:get-resend ( taddr target -- )
    resend-dest job-context @ return-address @ ;

: net2o:prep-send ( addr u dest addr -- addr taddr target n len )
    2>r  0 7 DO
	dup $20 I lshift $1F - u>= IF
	    $20 I lshift u<= IF  send-ack# outflag or!  THEN
	    I UNLOOP  2r> rot dup >r
	    $20 r> lshift   EXIT  THEN
    -1 +LOOP
    $20 u<= IF  send-ack# outflag or!  THEN
    2r> 0 $020 ;

: net2o:send-packet ( addr u dest addr -- len )
    net2o:prep-send >r sendX r> ;

: net2o:send-code-packet ( addr u dest addr -- len )  2>r
    send-ack# outflag or!
    0 7 DO
	dup $10 I lshift $-20 and u> IF
	    drop I UNLOOP  2r> rot dup >r sendX  $20 r> lshift  EXIT  THEN
    -1 +LOOP
    drop 2r>  0 sendX  $020 ;

\ synchronous sending

: data-to-send ( -- flag )  resend$@ nip 0> data-tail$@ nip 0> or ;

: net2o:send-chunk ( -- )
    resend$@ dup IF
	net2o:get-resend net2o:prep-send /resend
    ELSE
	2drop
	data-tail$@ net2o:get-dest net2o:prep-send /data-tail
    THEN
    data-to-send 0= IF  send-ack# outflag or!  THEN  sendX ;

: net2o:send-chunks-sync ( -- )  first-ack# outflag !
    BEGIN  data-to-send  WHILE  net2o:send-chunk  REPEAT ;

\ asynchronous sending

begin-structure chunks-struct
field: chunk-context
field: chunk-count
end-structure

Variable chunks s" " chunks $!
Variable chunks+
Create chunk-adder chunks-struct allot

: net2o:send-chunks ( -- )
    job-context @ chunk-adder chunk-context !
    0 chunk-adder chunk-count !
    chunk-adder chunks-struct chunks $+! ;

: send-chunks-async ( -- )
    chunks $@ chunks+ @ chunks-struct * safe/string
    IF
	dup chunk-context @ job-context !
	chunk-count dup @
	dup 0= IF  first-ack# outflag +!  THEN
	1 = IF  send-ack# outflag +!  THEN  1 swap +!
	data-to-send IF
	    net2o:send-chunk  1 chunks+ +!
	ELSE
	    chunks chunks+ @ chunks-struct * chunks-struct $del
	THEN
    ELSE  drop chunks+ off  THEN ;

: send? ( -- flag )  chunks $@len 0> ;

\ schedule delayed events

begin-structure queue-struct
field: queue-timestamp
field: queue-job
field: queue-xt
end-structure

Variable queue s" " queue $!
Create queue-adder  queue-struct allot

: add-queue ( xt us -- )
    utime drop +  queue-adder queue-timestamp !
    job-context @ queue-adder queue-job !
    queue-adder queue-xt !
    queue-adder queue-struct queue $+! ;

: eval-queue ( -- )
    queue $@len 0= ?EXIT  utime drop
    queue $@ bounds ?DO
	dup I queue-timestamp @ u> IF
	    I queue-job @ job-context !
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

environment os-type s" linux" string-prefix? [IF]
    2Variable ptimeout #10.000000 ptimeout 2! ( 10 ms )
[ELSE]
    &10 Constant ptimeout ( 10 ms )
[THEN]

Create pollfds   pollfd %size 2* allot

: poll-sock ( -- flag )
    eval-queue
    net2o-sock fileno pollfds fd l!
    net2o-sock6 fileno pollfds pollfd %size + fd l!
    POLLIN send? IF  POLLOUT or  THEN
    dup pollfds events w!  pollfds pollfd %size + events w!
[ environment os-type s" linux" string-prefix? ] [IF]
    pollfds 2 ptimeout 0 ppoll 0>
[ELSE]
    pollfds 2 ptimeout poll 0>
[THEN]
;

: read-a-packet4/6 ( -- )
    pollfds revents w@ POLLIN = IF  read-a-packet EXIT  THEN
    pollfds pollfd %size + revents w@ POLLIN = IF  read-a-packet6  THEN ;

: next-packet ( -- addr u )
    BEGIN  BEGIN  poll-sock  UNTIL
	pollfds revents w@ POLLOUT =
	pollfds pollfd %size + revents w@ POLLOUT = or
	IF  send-chunks-async  THEN
	pollfds revents w@ POLLIN =
    pollfds pollfd %size + revents w@ POLLIN = or UNTIL
    read-a-packet4/6
    sockaddr-tmp alen @ insert-address reverse64
    inbuf destination be-ux@ -$100 and or inbuf destination be-x!
    over packet-size over <> abort" Wrong packet size" ;

: next-client-packet ( -- addr u )
    BEGIN  poll-sock  UNTIL  read-a-packet4/6
    sockaddr-tmp alen @ check-address IF
	reverse64
	inbuf destination be-ux@ -$100 and or inbuf destination be-x!
	over packet-size over <> abort" Wrong packet size"
    ELSE  hex.  ." Unknown source"  THEN ;

Defer queue-command ( addr u -- )
' dump IS queue-command
Defer do-ack ( -- )
' noop IS do-ack

: handle-packet ( -- ) \ handle local packet
    >ret-addr >dest-addr
\    inbuf .header
    dest-addr @ 0= IF  inbuf packet-data queue-command
    ELSE  check-dest dup 0< IF drop  >r inbuf packet-data r@ swap move
	    do-ack
\	    job-context @ IF  inbuf packet-data swap . . cr  THEN
	    rdrop
	ELSE  0>  IF  >r inbuf packet-data r@ swap dup >r move
		r> r> swap queue-command
	    THEN
	THEN
    THEN ;

: route-packet ( -- )  inbuf dup packet-size send-a-packet ;

: server-event ( -- )
    next-packet 2drop in-route
    IF  ['] handle-packet catch IF
	    inbuf packet-data dump  THEN
    ELSE  route-packet  THEN ;

: client-event ( -- )
    poll-sock 0= ?EXIT
    next-client-packet 2drop in-check
    IF  ['] handle-packet catch
	IF  inbuf packet-data dump  THEN
    ELSE  ( drop packet )  THEN ;

: server-loop ( -- )
    BEGIN  server-event  AGAIN ;

: client-loop ( -- )
    BEGIN  poll-sock queue $@len 0<> or  WHILE  client-event  REPEAT ;

\ load net2o commands

include net2o-cmd.fs
