\ Internet 2.0 experiments

require unix/socket.fs
require string.fs

\ Create udp socket

4242 Constant net2o-udp

0 Value net2o-sock
0 Value net2o-srv

: new-server ( -- )
    net2o-udp create-udp-server s" w+" c-string fdopen to net2o-srv ;

: new-client ( -- )
    new-udp-socket to net2o-sock ;

$81A Constant maxpacket

here 1+ -8 and 6 + here - allot here maxpacket allot Constant inbuf
here 1+ -8 and 6 + here - allot here maxpacket allot Constant outbuf

2 8 2Constant address%

struct
    short% field flags
    address% field dest
    address% field addr
    address% field junk
end-struct net2o-header

: read-a-packet ( -- addr u )
    net2o-srv inbuf maxpacket read-socket-from ;

: send-a-packet ( addr u -- n )
    net2o-sock -rot 0 sockaddr-tmp 16 sendto ;

\ clients routing table

8 Value route-bits
8 Constant /address
' dfloats Alias addresses
0 Value routes

: init-route ( -- )
    routes IF  routes free  0 to routes  throw  THEN
    /address route-bits lshift dup allocate throw to routes
    routes swap erase ;

: route-hash ( addr -- hash )
    /address route-bits (hashkey1) ;

: sock-route" ( -- addr dest u hash )
    sockaddr-tmp dup route-hash dup >r addresses routes + /address r> ;
: insert-address ( -- net2o-addr )
     sock-route" >r move r> $38 lshift ;
\ FIXME: doesn't check for collissons

: host:port ( addr u port -- )
    -rot host>addr swap sockaddr-tmp >inetaddr ;

: insert-ipv4 ( addr u port -- net2o-addr )
    host:port insert-address ;

: address>route ( -- n/-1 )
    sock-route" >r tuck str= 0= IF  rdrop -1  ELSE  r>  THEN ;
: route>address ( n -- )
    addresses routes + sockaddr-tmp /address move ;

\ bit reversing

: bitreverse8 ( u1 -- u2 )
    0 8 0 DO  2* over 1 and + swap 2/ swap  LOOP  nip ;

Create reverse-table $100 0 [DO] [I] bitreverse8 c, [LOOP]

: reverse8 ( c1 -- c2 ) reverse-table + c@ ;
: reverse64 ( x1 -- x2 )
    0 8 0 DO  8 lshift over $FF and reverse8 or
	swap 8 rshift swap  LOOP ;

\ route an incoming packet

: packet-route ( orig-addr addr -- flag ) >r
    r@ dest c@ 0= IF  true  rdrop EXIT  THEN \ local packet
    r@ dest c@ route>address
    r@ dest dup 1+ swap /address 1- move
    r> dest /address 1- + c!  false ;

: in-route ( -- flag )  address>route reverse8  inbuf packet-route ;
: out-route ( -- flag )  0  outbuf packet-route ;

\ packet&header size

$80 Constant destsize#
$40 Constant addrsize#
$20 Constant junksize#
$06 Constant datasize#

: (header-size ( x -- u ) >r 2
    r@ destsize# and IF  8  ELSE  2  THEN +
    r@ addrsize# and IF  8  ELSE  2  THEN +
    r@ junksize# and IF  8  ELSE  0  THEN +
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

\ packet delivery table

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
    inbuf dest @ reverse64 return-addr ! ;
: >dest-addr ( -- )
    inbuf addr be-x@  inbuf body-size 1- invert and dest-addr ! ;

: ret-hash ( -- n )  return-addr 1 cells delivery-bits (hashkey1) ;

: check-dest ( -- addr t / f )
    ret-hash cells delivery-table +
    dup @ 0= IF  drop false  EXIT  THEN
    $@ bounds ?DO
	I 2@ 1- bounds dest-addr @ within
	0= IF  I cell+ 2@ dest-addr @ swap - + true UNLOOP  EXIT  THEN
    3 cells +LOOP
    false ;

Create dest-mapping  0 , 0 , 0 ,

: map-dest ( addr u addr' -- )
    ret-hash cells delivery-table + >r
    r@ @ 0= IF  s" " r@ $!  THEN
    dest-mapping 2 cells + ! dest-mapping 2!
    dest-mapping 3 cells r> $+! ;

: new-map ( addr u -- )  dup allocate throw map-dest ;

\ client initializer

: init-client ( -- )
    new-client init-route init-delivery-table ;

: init-server ( -- )
    new-server new-client init-route init-delivery-table ;

\ send blocks of memory

: set-dest ( addr target -- )
    outbuf dest be-x!  outbuf addr be-x! ;

: set-flags ( -- )  0 outbuf 1+ c!  destsize# addrsize# or outbuf c! ;

: c+!  ( n addr -- )  dup >r c@ + r> c! ;

: outbody ( -- addr ) outbuf packet-body ;
: send-packet ( -- )  out-route outbuf dup packet-size send-a-packet ;

: >sendA ( addr -- )  outbody $020 move ;
: >sendB ( addr -- )  outbody $080 move $2 outbuf c+! ;
: >sendC ( addr -- )  outbody $200 move $4 outbuf c+! ;
: >sendD ( addr -- )  outbody $800 move $6 outbuf c+! ;

: sendA ( addr taddr target -- )  set-dest  set-flags  >sendA send-packet ;
: sendB ( addr taddr target -- )  set-dest  set-flags  >sendB send-packet ;
: sendC ( addr taddr target -- )  set-dest  set-flags  >sendC send-packet ;
: sendD ( addr taddr target -- )  set-dest  set-flags  >sendD send-packet ;

\ poll loop

100 Value ptimeout \ milliseconds

Create pollfds   pollfd %size allot

: poll-srv ( -- flag )  net2o-srv fileno pollfds fd l!
    POLLIN pollfds events w!
    pollfds 1 ptimeout poll 0> ;

: next-srv-packet ( -- addr u )
    BEGIN  poll-srv  UNTIL  read-a-packet
    over packet-size over <> abort" Wrong packet size" ;

Defer queue-command ( addr u -- )
' dump IS queue-command

: handle-packet ( -- ) \ handle local packet
    >ret-addr >dest-addr
    dest-addr @ 0= IF  inbuf packet-data queue-command
    ELSE  check-dest  IF  >r inbuf packet-data r> swap move  THEN
    THEN ;

: route-packet ( -- )  inbuf dup packet-size send-a-packet ;

: server-loop ( -- )
    BEGIN  next-srv-packet 2drop in-route
	IF  handle-packet  ELSE  route-packet  THEN
    AGAIN ;

\ load net2o commands

include net2o-cmd.fs
