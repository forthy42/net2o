\ Internet 2.0 experiments

require unix/socket.fs
require string.fs
require struct0x.fs
require nacl.fs
require wurstkessel.fs

: safe/string ( c-addr u n -- c-addr' u' )
\G protect /string against overflows.
    dup negate >r  dup 0> IF
        /string dup r> u>= IF  + 0  THEN
    ELSE
        /string dup r> u< IF  + 1+ -1  THEN
    THEN ;

: or!  ( value addr -- )   >r r@ @ or  r> ! ;
: xor! ( value addr -- )   >r r@ @ xor r> ! ;
: and! ( value addr -- )   >r r@ @ and r> ! ;
: !@ ( value addr -- old-value )   dup @ >r ! r> ;

\ debugging aids

: debug: ( -- ) Create immediate false ,  DOES>
    ]] Literal @ IF [[ ')' parse evaluate ]] THEN [[ ;

debug: timing(
debug: rate(
debug: ratex(
debug: slack(
debug: slk(

: +db ( "word" -- ) ' >body on ;

\ +db rate(
\ +db ratex(
\ +db slack(
\ +db timing(

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

$22 $10 + Constant overhead \ constant overhead
$4 Value max-size^2 \ 1k, don't fragment by default
$40 Constant min-size
min-size max-size^2 lshift overhead + Constant maxpacket

here 1+ -8 and 6 + here - allot here maxpacket allot Constant inbuf
here 1+ -8 and 6 + here - allot here maxpacket allot Constant outbuf

2 8 2Constant address%

struct
    short% field flags
    address% field destination
    address% field addr
    address% 2* field nonce
end-struct net2o-header

Variable packet4r
Variable packet4s
Variable packet6r
Variable packet6s

: read-a-packet ( -- addr u )
    net2o-sock inbuf maxpacket read-socket-from  1 packet4r +! ;

: read-a-packet6 ( -- addr u )
    net2o-sock6 inbuf maxpacket read-socket-from  1 packet6r +! ;

: send-a-packet ( addr u -- n )
    sockaddr-tmp w@ AF_INET6 = IF
	net2o-sock6  1 packet6s +!
    ELSE
	net2o-sock  1 packet4s +!
    THEN
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
    dup ai_addr @ swap ai_addrlen l@ ;

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

$C0 Constant headersize#
$00 Constant 16bit#
$40 Constant 64bit#
$0F Constant datasize#

Create header-sizes  $06 c, $22 c, $FF c, $FF c,
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

$07 Constant acks#
$06 Constant ack-class#
$01 Constant ack-toggle#
$02 Constant b2b-toggle#
$04 Constant send-ack#

\ short packet information

: chunk@ ( addr flag -- value addr' )
    IF  dup be-ux@ swap 8 +  ELSE  dup be-uw@ swap 2 +  THEN ;

: .header ( addr -- )
    dup c@ >r 2 +
    r@ datasize# and 'A' + emit
    r@ headersize# and chunk@
    r@ headersize# and chunk@
    drop rdrop swap
    ."  to " hex. ."  @ " hex. cr ;

\ packet delivery table

0 Value j^

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

: check-dest ( -- addr 1/t / f )  0 to j^
    ret-hash cells delivery-table +
    dup @ 0= IF  drop false  EXIT  THEN
    $@ bounds ?DO
	I 2@ 1- bounds dest-addr @ within
	0= IF
	    I cell+ 2@ dest-addr @ swap - +
	    I 4 cells + @ IF  1  ELSE  -1  THEN
	    I 3 cells + @ to j^
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
    j^ r@ 3 cells + !
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
\ !!!FIXME!!! needs to be split in sender/receiver

begin-structure context-struct
field: return-address
field: cmd-out
field: file-handles
field: crypto-key
field: auth-info
field: status
field: data-map
field: data-resend
field: code-map
field: ack-state
field: ack-receive
field: data-ack
field: code-ack
field: ack-addr
field: ack-time
field: sack-backlog
field: min-slack
field: send-tick
field: ps/byte
field: bandwidth-tick \ ns
field: next-tick \ ns
field: firstb-ticks
field: lastb-ticks
field: delta-ticks
field: ack-sizes
end-structure

begin-structure cmd-struct
field: cmd-accu#
field: cmd-accu
field: cmd-slot
field: cmd-extras
field: cmd-buf#
$800 +field cmd-buf
end-structure

8 Value b2b-chunk#
b2b-chunk# 2* 2* 1- Value tick-init \ ticks without ack
#1000000 Value bandwidth-init \ 1µs/byte
-1 Constant never

: ticks ( -- u )  ntime drop ;

: n2o:new-context ( -- )  context-struct allocate throw to j^
    j^ context-struct erase  return-addr @ j^ return-address !
    s" " j^ cmd-out $!
    s" " j^ data-ack $!
    s" " j^ data-resend $!
    s" " j^ code-ack $!
    s" " j^ sack-backlog $!
    wurst-key state# j^ crypto-key $!
    $7fffffffffffffff j^ min-slack !
    tick-init j^ send-tick !
    bandwidth-init j^ ps/byte !
    never          j^ next-tick !
    cmd-struct j^ cmd-out $!len
    j^ cmd-out $@ erase ;

: n2o:new-data ( addr u -- )  dup allocate throw map-source
    j^ data-map $! ;
: n2o:new-code ( addr u -- )  dup allocate throw map-source
    j^ code-map $! ;

: data$@ ( -- addr u )
    j^ data-map $@ drop >r
    r@ data-raddr @  r@ data-size @ r> data-head @ safe/string ;
: /data ( u -- )
    j^ data-map $@ drop data-head +! ;
: data-tail$@ ( -- addr u )
    j^ data-map $@ drop >r
    r@ data-raddr @  r@ data-head @ r> data-tail @ safe/string ;
: /data-tail ( u -- )
    j^ data-map $@ drop data-tail +! ;
: data-dest ( -- addr )
    j^ data-map $@ drop >r
    r@ data-vaddr @ r> data-tail @ + ;

\ code sending around

: code-dest ( -- addr )
    j^ code-map $@ drop >r
    r@ data-vaddr @ r> data-tail @ + ;

\ acknowledge map

2Variable 'range

: range+ ( addr1 addr2 addr u -- addr1' addr2' )
    1+ bounds rot umin >r umax r> tuck - 1- ;

: add-range ( addr u map -- )
    >r 1+ bounds r@ $@ bounds ?DO
	over I 2@ 1+ bounds within
	over I 2@ 1+ bounds within and 0=  IF
	    I 2@ range+ I 2!
	    I UNLOOP
	    r@ $@ drop -
	    r@ $@ 2 pick safe/string 2 cells > IF
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

\ acknowledge handling, flow control

Variable lastdiff
Variable rtdelay

: min! ( n addr -- ) >r  r@ @ min r> ! ;

: timestat ( client serv -- )
    timing( over . dup . ." acktime" cr )
    ticks over - rtdelay ! - dup lastdiff !  j^ min-slack min! ;

: net2o:ack-addrtime ( addr ntime -- ) swap
    j^ sack-backlog $@ bounds ?DO
	dup I @ = IF  drop
	    I cell+ @ timestat
	    j^ sack-backlog I over $@ drop - 2 cells $del
	    UNLOOP  EXIT  THEN
    2 cells +LOOP  2drop ( acknowledge not found ) ;

#4000000 Value slack# \ 4ms slack leads to backdrop of factor 2

: net2o:set-rate ( rate -- )
    dup rate( dup . ." clientavg" cr )
    \ negative rate means packet reordering
    lastdiff @ j^ min-slack @ - slack( dup . j^ min-slack ? ." slack" cr )
    0 max slack# */ + j^ ps/byte ! ;

: net2o:unacked ( addr u -- )  1+ j^ data-ack add-range ;
: net2o:ack-range ( addr u -- )
    ( 2dup ." Acknowledge range: " swap . . cr ) 2drop ;
: net2o:resend ( addr u -- )
    2dup j^ data-resend add-range
    ." Resend: " swap . . cr ;
: >real-range ( addr -- addr' )
    j^ data-map $@ drop data-raddr @ + ;
: resend$@ ( -- addr u )
    j^ data-resend $@  IF
	2@ swap >real-range swap
    ELSE  drop 0 0  THEN ;
: resend-dest ( -- addr )
    j^ data-resend $@ drop 2@ drop ;
: /resend ( u -- )  j^ data-resend dup $@ drop 2@ drop
    -rot del-range ;

\ file handling

: >throw ( error -- ) throw ( stub! ) ;

: ?handles ( -- )
    j^ file-handles @ 0= IF
	s" " j^ file-handles $!
    THEN ;    

\ open a file - this needs *way more checking*!

: id>file ( id -- fid )
    >r j^ file-handles $@ r> cells safe/string
    0= >throw  @ ;

: n2o:open-file ( addr u mode id -- )
    ?handles
    >r j^ file-handles $@ r@ cells safe/string
    IF    dup @ ?dup-IF  close-file >throw  THEN  dup off
    ELSE  drop r@ 1+ cells j^ file-handles $!len
	j^ file-handles $@ drop r@ cells +  THEN rdrop >r
    dup 2over ." open file: " type ."  with mode " . cr
    open-file >throw r> ! ;

: n2o:close-file ( id -- )
    ?handles
    >r j^ file-handles $@ r@ cells safe/string
    IF
	dup @ ?dup-IF  close-file >throw  THEN  dup off
    THEN
    drop rdrop ;

\ symmetric encryption and decryption

: >wurst-source ( d -- )
    wurst-source state# bounds ?DO  2dup I 2!  2 cells +LOOP  2drop ;

: >wurst-key ( -- )
    j^ dup 0= IF
	drop wurst-key state#
    ELSE
	crypto-key $@
    THEN
    wurst-state swap move ;

: wurst-outbuf-init ( -- )
    rng@ rng@ 2dup >wurst-source outbuf nonce 2! >wurst-key ;

: wurst-inbuf-init ( -- )
    inbuf nonce 2@ >wurst-source >wurst-key ;

: mem-rounds# ( size -- n )
    case
	min-size of  $22  endof
	min-size 2* of  $24  endof
	$28 swap
    endcase ;

: 2xor ( ud1 ud2 -- ud3 )  rot xor >r xor r> ;

: wurst-crc ( -- xd )
    0. wurst-state state# bounds ?DO  I 2@ 2xor 2 cells +LOOP ;

[IFDEF] nocrypt \ dummy for test
: wurst-outbuf-encrypt ;
true constant wurst-inbuf-decrypt
[ELSE]
: wurst-outbuf-encrypt ( -- )
    wurst-outbuf-init
    outbuf body-size mem-rounds# >r
    outbuf packet-data
    over roundse# rounds
    BEGIN  dup 0>  WHILE
	    over r@ rounds  r@ >reads state# * safe/string
    REPEAT
    over roundse# rounds  drop
    rdrop wurst-crc rot 2! ;

: wurst-inbuf-decrypt ( -- flag )
    wurst-inbuf-init
    inbuf body-size mem-rounds# >r
    inbuf packet-data
    over roundse# rounds
    BEGIN  dup 0>  WHILE
	    over r@ rounds-decrypt  r@ >reads state# * safe/string
    REPEAT
    over roundse# rounds  drop
    rdrop 2@ wurst-crc d= ;
[THEN]

\ public key encryption

\ these are dummy keys for testing!!!

$20 Constant keysize \ our shared secred is only 32 bytes long
\ server keys
Create pks $21982058BCCB3476. 64, $36623B3840D9F393. 64, $B4B038E18F007E95. 64, $79CAED9D9F043F9B. 64,
Create sks $EFDA8C1AE4F04358. 64, $4320CCB35C5F6C27. 64, $CE16D65418EA8575. 64, $127701E350CC537F. 64,
\ client keys
Create pkc keysize allot
Create skc keysize allot
\ shared secred
Create keypad keysize allot
Variable do-keypad

\ the theory here is that sks*pkc = skc*pks
\ we send our public key and know the server's public key.

: set-key ( addr -- )
    keysize 2* j^ crypto-key $!
    \ double key to get 512 bits
    j^ crypto-key $@ 2/ 2dup + swap move
    ( ." set key to:" j^ crypto-key $@ dump ) ;

: net2o:receive-key ( addr u -- )
    keysize <> abort" key+pubkey: expected 32 bytes"
    pkc keysize move
    keypad sks pkc crypto_scalarmult_curve25519 ;

: net2o:send-key ( pks -- pkc-addr u )
    keypad skc rot crypto_scalarmult_curve25519
    pkc keysize  do-keypad on ;

: update-key ( -- )
    do-keypad @ IF
	keypad set-key
	do-keypad off
    THEN ;

\ send blocks of memory

: set-dest ( addr target -- )
    outbuf destination be-x!  dup dest-addr !  outbuf addr be-x! ;

Variable outflag  outflag off
Variable b2b-first  b2b-first on
2Variable sack-addrtime

: set-flags ( -- )  j^ >r
    b2b-first @ IF
	ticks dest-addr @ sack-addrtime 2!
	sack-addrtime 2 cells r@ sack-backlog $+!
    THEN
    b2b-first off
    rdrop
    outflag @ outbuf 1+ c! outflag off ;

: c+!  ( n addr -- )  dup >r c@ + r> c! ;

: outbody ( -- addr ) outbuf packet-body ;
: outsize ( -- n )    outbuf packet-size ;

#90 Constant EMSGSIZE

: send-packet ( -- )
\    ." send " outbuf .header
    wurst-outbuf-encrypt
    out-route drop
    outbuf dup packet-size
    send-a-packet 0< IF
	errno EMSGSIZE = IF
	    max-size^2 1- to max-size^2
	ELSE
	    true abort" could not send"
	THEN
    THEN ;

: >send ( addr n -- )  >r  r@ 64bit# or outbuf c!
    outbody min-size r> lshift move ;

: bandwidth+ ( -- )  j^ >r
    outsize r@ ps/byte @ #1000 */ dup r@ bandwidth-tick +!
    ( dup 2/ + ) 2/ ticks + r@ bandwidth-tick @ umax r> next-tick ! ;

: sendX ( addr taddr target n -- )
    >r set-dest  r> >send  set-flags  bandwidth+  send-packet
    update-key ;

\ send chunk

: net2o:get-dest ( taddr target -- )
    data-dest j^ return-address @ ;
: net2o:get-resend ( taddr target -- )
    resend-dest j^ return-address @ ;

: send-size ( u -- n )
    0 max-size^2 DO
	dup min-size 2/ I lshift u>= IF
	    drop I  UNLOOP  EXIT
	THEN
    -1 +LOOP
    drop 0 ;

: net2o:prep-send ( addr u dest addr -- addr taddr target n len )
    2>r  dup >r send-size min-size over lshift
    dup r> u>= IF  send-ack# outflag or!  ack-toggle# outflag xor!  THEN
    2r> 2swap ;

: net2o:send-packet ( addr u dest addr -- len )
    net2o:prep-send >r sendX r> ;

\ synchronous sending

: data-to-send ( -- flag )  resend$@ nip 0> data-tail$@ nip 0> or ;

: net2o:send-chunk ( -- )
    resend$@ dup IF
\	." resending" cr
	net2o:get-resend net2o:prep-send /resend
    ELSE
	2drop
\	." sending" cr
	data-tail$@ net2o:get-dest net2o:prep-send /data-tail
    THEN
    data-to-send 0= IF
	send-ack# outflag or!  ack-toggle# outflag xor!
	sendX  never j^ next-tick !
    ELSE  sendX  THEN ;

: bandwidth? ( -- flag ) j^ >r
    ticks r> next-tick @ - 0>= ;

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
    chunks-struct %size +LOOP
    j^ chunk-adder chunk-context !
    0 chunk-adder chunk-count !
    chunk-adder chunks-struct chunks $+!
    ticks dup j^ bandwidth-tick !  j^ next-tick ! ;

: chunk-count+ ( counter -- )
    dup @
    dup 0= IF  ack-toggle# j^ ack-state xor!  THEN
    j^ ack-state @ outflag or!
    j^ send-tick @ = IF  off  ELSE  1 swap +!  THEN ;

: send-chunks-async ( -- flag )
    chunks $@ chunks+ @ chunks-struct * safe/string
    IF
	dup chunk-context @ to j^
	chunk-count
	data-to-send IF
	    { ck# } bandwidth? dup  IF
		b2b-first on  b2b-toggle# j^ ack-state xor!
		b2b-chunk# 0 +DO  ck# chunk-count+  net2o:send-chunk  LOOP
	    THEN  1 chunks+ +!
	ELSE
	    drop ." done, rate: " j^ ps/byte ? cr
	    chunks chunks+ @ chunks-struct * chunks-struct $del
	    false
	THEN
    ELSE  drop chunks+ off false  THEN ;

: next-chunk-tick ( -- tick )
    -1 chunks $@ bounds ?DO
	I chunk-context @ next-tick @ umin
    chunks-struct +LOOP ;

: send-another-chunk ( -- flag )  0 >r
    BEGIN  send-chunks-async 0= WHILE
	    chunks+ @ 0= IF  r> 1+ >r  THEN
	r@ 2 u>=  UNTIL  false  ELSE  true  THEN  rdrop ;

Variable sendflag  sendflag off
: send?  ( -- flag )  sendflag @ ;
: send-anything? ( -- flag )  chunks $@len 0> ;

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

2Variable ptimeout #1000000 ptimeout cell+ ! ( 1 ms )

Create pollfds   here pollfd %size 4 * dup allot erase

: fds!+ ( fileno flag addr -- addr' )
     >r r@ events w!  r@ fd l!  r> pollfd %size + ; 

: prep-socks ( -- )  pollfds >r
    net2o-sock  fileno POLLIN  r> fds!+ >r
    net2o-sock6 fileno POLLIN  r> fds!+ >r
    net2o-sock  fileno POLLOUT r> fds!+ >r
    net2o-sock6 fileno POLLOUT r> fds!+ drop ;

: clear-events ( -- )  pollfds
    4 0 DO  0 over revents w!  pollfd %size +  LOOP  drop ;

: poll-sock ( -- flag )
    eval-queue  clear-events
    next-chunk-tick dup -1 <> >r ticks - dup 0>= r> or
    IF    0 max ptimeout cell+ !  pollfds 2
    ELSE  drop #500000000 ptimeout cell+ !  pollfds 2  THEN
[ environment os-type s" linux" string-prefix? ] [IF]
    ptimeout 0 ppoll 0>
[ELSE]
    ptimeout cell+ @ #1000000 / poll 0>
[THEN]
;

: read-a-packet4/6 ( -- addr u )
    pollfds revents w@ POLLIN = IF  read-a-packet EXIT  THEN
    pollfds pollfd %size + revents w@ POLLIN = IF  read-a-packet6 EXIT  THEN
    0 0 ;

: next-packet ( -- addr u )
    send-anything? sendflag !
    BEGIN  poll-sock 0= WHILE  send-another-chunk sendflag !  REPEAT
    read-a-packet4/6
    sockaddr-tmp alen @ insert-address reverse64
    inbuf destination be-ux@ -$100 and or inbuf destination be-x!
    over packet-size over <> abort" Wrong packet size" ;

: next-client-packet ( -- addr u )
    BEGIN  BEGIN  poll-sock  UNTIL  read-a-packet4/6  2dup d0= WHILE
	   2drop  REPEAT
    sockaddr-tmp alen @ check-address IF
	reverse64
	inbuf destination be-ux@ -$100 and or inbuf destination be-x!
	over packet-size over <> abort" Wrong packet size"
    ELSE  hex.  ." Unknown source"  0 0  THEN ;

Defer queue-command ( addr u -- )
' dump IS queue-command
Defer do-ack ( -- )
' noop IS do-ack

: handle-packet ( -- ) \ handle local packet
    >ret-addr >dest-addr
\    inbuf .header
    dest-addr @ 0= IF
	0 to j^ \ address 0 has no job context!
	wurst-inbuf-decrypt 0= IF  ." invalid packet to 0" cr EXIT  THEN
	inbuf packet-data queue-command
    ELSE
	check-dest
	wurst-inbuf-decrypt 0= IF
	    inbuf .header
	    ." invalid packet to " dest-addr @ hex. cr
	    IF  drop  THEN  EXIT  THEN
	dup 0< IF
	    drop  >r inbuf packet-data r@ swap move
	    do-ack
\	    j^ IF  inbuf packet-data swap . . cr  THEN
	    rdrop
	ELSE
	    0>  IF
		>r inbuf packet-data r@ swap dup >r move
		r> r> swap queue-command
	    THEN
	THEN
    THEN ;

: route-packet ( -- )  inbuf dup packet-size send-a-packet drop ;

: server-event ( -- )
    next-packet 2drop  in-route
    IF  ['] handle-packet catch IF
	    inbuf packet-data dump  THEN
    ELSE  ." route a packet" cr route-packet  THEN ;

: client-event ( -- )
    poll-sock 0= ?EXIT
    next-client-packet  2drop in-check
    IF  ['] handle-packet catch
	IF  inbuf packet-data dump  THEN
    ELSE  ( drop packet )  THEN ;

: server-loop ( -- )
    BEGIN  server-event  AGAIN ;

#1000000000 Constant min-timeout

: client-loop ( -- ) ticks min-timeout + >r
    BEGIN  poll-sock queue $@len 0<> or
	ticks r@ u< or
    WHILE  client-event  REPEAT  rdrop ;

\ client/server initializer

: init-client ( -- )
    new-client init-route init-delivery-table prep-socks ;

: init-server ( -- )
    new-server init-route init-delivery-table prep-socks ;

\ load net2o commands

include net2o-cmd.fs
