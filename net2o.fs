\ Internet 2.0 experiments

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

: or!  ( x addr -- )   >r r@ @ or  r> ! ;
: xor! ( x addr -- )   >r r@ @ xor r> ! ;
: and! ( x addr -- )   >r r@ @ and r> ! ;
: min! ( n addr -- )   >r r@ @ min r> ! ;
: max! ( n addr -- )   >r r@ @ max r> ! ;

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

\ timing ticks

: ticks ( -- u )  ntime drop ;

\ debugging aids

: debug)  ]] THEN [[ ;

true [IF]
    : debug: ( -- ) Create immediate false ,  DOES>
	]] Literal @ IF [[ ['] debug) assert-canary ;
[ELSE]
    : debug: ( -- )  Create immediate DOES> postpone ( ;
[THEN]
    
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

: +db ( "word" -- ) ' >body on ;

\ +db bursts(
\ +db rate(
\ +db ratex(
\ +db slack(
\ +db timing(
\ +db deltat(
\ +db resend(
\ +db track(

\ Create udp socket

4242 Value net2o-port

0 Value net2o-sock
0 Value net2o-sock6

: new-server ( -- )
    net2o-port create-udp-server s" w+" c-string fdopen to net2o-sock
    net2o-port create-udp-server6 s" w+" c-string fdopen to net2o-sock6
;

: new-client ( -- )
    new-udp-socket s" w+" c-string fdopen to net2o-sock
    new-udp-socket6 s" w+" c-string fdopen to net2o-sock6 ;

$22 Constant overhead \ constant overhead
$4 Value max-size^2 \ 1k, don't fragment by default
$40 Constant min-size
: maxdata ( -- n ) min-size max-size^2 lshift ;
maxdata overhead + Constant maxpacket
: chunk-p2 ( -- n )  max-size^2 6 + ;

here 1+ -8 and 6 + here - allot here maxpacket allot Constant inbuf
here 1+ -8 and 6 + here - allot here maxpacket allot Constant outbuf

2 8 2Constant address%

struct
    short% field flags
    address% field destination
    address% field addr
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
Variable routes

: init-route ( -- )  s" " routes hash@ $! ; \ field 0 is me, myself

: info>string ( addr -- addr u )
    dup ai_addr @ swap ai_addrlen l@ ;

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

\ bit reversing

: bitreverse8 ( u1 -- u2 )
    0 8 0 DO  2* over 1 and + swap 2/ swap  LOOP  nip ;

Create reverse-table $100 0 [DO] [I] bitreverse8 c, [LOOP]

: reverse8 ( c1 -- c2 ) reverse-table + c@ ;
: reverse64 ( x1 -- x2 )
    0 8 0 DO  8 lshift over $FF and reverse8 or
	swap 8 rshift swap  LOOP  nip ;

\ route an incoming packet

Variable return-addr

: packet-route ( orig-addr addr -- flag ) >r
    r@ destination c@ 0= IF  drop  true  rdrop EXIT  THEN \ local packet
    r@ destination be-ux@ route>address
    reverse64 r> destination be-x!  false ;

: in-route ( -- flag )  address>route reverse64  inbuf packet-route ;
: in-check ( -- flag )  address>route -1 <> ;
: out-route ( -- flag )  0  outbuf packet-route ;

\ packet&header size

$C0 Constant headersize#
$00 Constant 16bit#
$40 Constant 64bit#
$0F Constant datasize#

Create header-sizes  $06 c, $12 c, $FF c, $FF c,
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
    IF  dup be-ux@ swap 8 +  ELSE  dup be-uw@ swap 2 +  THEN ;

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
    inbuf destination be-ux@ reverse64 return-addr ! ;
: >dest-addr ( -- )
    inbuf addr be-ux@  inbuf body-size 1- invert and dest-addr ! ;

begin-structure dest-struct
field: dest-size
field: dest-vaddr
field: dest-raddr
field: dest-job
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

: check-dest ( -- addr 1/t / f )
    \G return false if invalid destination
    \G return 1 if code, -1 if data, plus destination address
    0 to j^
    return-addr @ routes #.key dup 0= IF  drop false  EXIT  THEN
    cell+ $@ bounds ?DO
	I @ 2@ 1- bounds dest-addr @ within
	0= IF
	    I @ dest-vaddr 2@ dest-addr @ swap - +
	    I @ code-flag @ IF  1  ELSE  -1  THEN
	    I @ dest-job @ to j^
	    UNLOOP  EXIT  THEN
    cell +LOOP
    false ;

\ job context structure
\ !!!FIXME!!! needs to be split in sender/receiver

begin-structure context-struct
field: context#
field: return-address
field: cmd-out
field: file-handles
field: file-state
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

begin-structure cmd-struct
field: cmd-buf#
maxdata +field cmd-buf
end-structure

begin-structure timestamp
field: ts-ticks
end-structure

\

: .j ( -- ) j^ context# ? ;

\ Destination mapping contains
\ addr u - range of virtal addresses
\ addr' - real start address
\ context - for exec regions, this is the job context

                    \  u   addr real-addr job ivs ig  ilg tst tail code-flag
Create dest-mapping    0 , 0 ,  0 ,       0 , 0 , 0 , 0 , 0 , 0 ,  here 0 ,
                    \  ab0 ab1  fa  la
                       0 , 0 ,  0 , 0 ,
Constant >code-flag
                    \  u   addr real-addr job ivs ig  ilg tst tail head
Create source-mapping  0 , 0 ,  0 ,       0 , 0 , 0 , 0 , 0 , 0 ,  0 ,
Variable mapping-addr

: addr>ts ( addr -- ts-offset )
    chunk-p2 rshift timestamp * ;
: addr>bits ( addr -- bits )
    chunk-p2 rshift ;

: allocatez ( size -- addr )
    dup >r allocate throw dup r> erase ;
: allocateFF ( size -- addr )
    dup >r allocate throw dup r> -1 fill ;

: map-string ( addr u addrx -- addrx u2 )
    >r tuck r@ dest-size 2!
    dup allocatez r@ dest-raddr !
    state# 2* allocatez r@ dest-ivsgen !
    dup addr>ts allocatez r@ dest-timestamps !
    dup addr>bits 1- 3 rshift 1+ allocateFF r@ data-ackbits0 !
    dup addr>bits 1- 3 rshift 1+ allocateFF r@ data-ackbits1 !
    r@ data-lastack# on
    drop
    j^ r@ dest-job !
    r> rdata-struct ;

: map-source-string ( addr u addrx -- addrx u2 )
    >r tuck r@ dest-size 2!
    dup allocatez r@ dest-raddr !
    state# 2* allocatez r@ dest-ivsgen !
    dup addr>ts allocatez r@ dest-timestamps !
    drop
    j^ r@ dest-job !
    r> code-struct ;

: map-dest ( vaddr u addr -- )
    return-addr @ routes #.key cell+ >r
    r@ @ 0= IF  s" " r@ $!  THEN  >r
    dest-mapping map-string  r@ $!
    r> $@ drop mapping-addr tuck ! cell r> $+! ;

: map-source ( addr u addr' -- addr u )
    source-mapping map-source-string drop data-struct ;

: n2o:new-data ( addr u -- )  >code-flag off
    2dup  j^ data-rmap map-dest  map-source  j^ data-map $! ;
: n2o:new-code ( addr u -- )  >code-flag on
    2dup  j^ code-rmap map-dest  map-source  j^ code-map $! ;

\ create context

8 Value b2b-chunk#
b2b-chunk# 2* 2* 1- Value tick-init \ ticks without ack
#2000000 max-size^2 lshift Value bandwidth-init \ 32µs/burst=1MB/s
-1 Constant never
-1 1 rshift Constant max-int64
4 Value flybursts#

Variable init-context#

: n2o:new-context ( addr -- )
    context-struct allocate throw to j^
    j^ context-struct erase
    init-context# @ j^ context# !  1 init-context# +!
    dup return-addr !  j^ return-address !
    s" " j^ cmd-out $!
    s" " j^ data-resend $!
    wurst-key state# j^ crypto-key $!
    max-int64 2/ j^ min-slack !
    max-int64 j^ rtdelay !
    flybursts# dup j^ flybursts ! j^ flyburst !
    ticks j^ lastack ! \ asking for context creation is as good as an ack
    bandwidth-init j^ ns/burst !
    never          j^ next-tick !
    cmd-struct j^ cmd-out $!len
    j^ cmd-out $@ erase ;

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
    r@ dest-vaddr @ r@ dest-tail @ +
    maxdata r@ dest-tail +!
    r@ dest-tail @ r@ dest-size @ u>= IF  r@ dest-tail off  THEN
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
    0= IF  dup ticks-init  bursts( ." restart bursts" cr )  THEN
    dup j^ lastack !
    over - j^ rtdelay min!
    - dup lastdiff !
    lastdeltat @ 8 rshift j^ min-slack +!
    j^ min-slack min! ;

: net2o:ack-addrtime ( addr ticks -- )  swap
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

#3000000 Value slack# \ 4ms slack leads to backdrop of factor 2

: net2o:set-flyburst ( -- bursts )
    j^ rtdelay @ j^ ns/burst @ / 1+ \ flybursts# +
    bursts( dup . .j ." flybursts" cr ) dup j^ flyburst ! ;
: net2o:max-flyburst ( bursts -- ) j^ flybursts max!@
    0= IF  bursts( .j ." start bursts" cr ) THEN ;

: net2o:set-rate ( rate deltat -- )
    deltat( dup . lastdeltat ? .j ." deltat" cr )
    dup 0<> lastdeltat @ 0<> and
    IF  lastdeltat @ over max swap 2dup 2>r */ 2r> */  ELSE  drop  THEN
    rate( dup . .j ." clientavg" cr )
    \ negative rate means packet reordering
    lastdiff @ j^ min-slack @ - slack( dup . j^ min-slack ? .j ." slack" cr )
    0 max slack# 2* 2* min slack# / lshift
    j^ last-ns/burst @
    ?dup-IF  tuck 2* min swap 2/ max  THEN
    dup j^ last-ns/burst !
    rate( dup . .j ." rate" cr )
    j^ ns/burst !@ >r
    net2o:set-flyburst
    r> bandwidth-init = IF \ first acknowledge
	net2o:max-flyburst
    ELSE  drop  THEN ;

\ acknowledge

Create resend-buf  0 , 0 ,
: >mask0 ( addr mask -- addr' mask' )
    BEGIN  dup 1 and 0= WHILE  2/ >r maxdata + r>  dup 0= UNTIL  THEN ;
: net2o:resend-mask ( addr mask -- )  >mask0
    resend( ." Resend-mask: " over . dup . cr )
    resend-buf 2!  resend-buf 2 cells j^ data-resend $+! ;
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

: nogap ( -- )  abort" Gap in file handles" ;

: ?handles ( -- )
    j^ file-handles @ 0= IF  s" " j^ file-handles $!  THEN ;    

\ file states

begin-structure file-state-struct
field: fs-size
field: fs-seek
field: fs-oldseek
field: fs-fid
end-structure

: ?state ( -- )
    j^ file-state @ 0= IF  s" " j^ file-state $!  THEN ;

: state-addr ( id -- addr )  ?state
    >r j^ file-state $@ r@ file-state-struct * /string dup 0< nogap
    0= IF  drop r@ 1+ file-state-struct * j^ file-state $!len
	j^ file-state $@ drop r@ file-state-struct * +
	dup file-state-struct erase  THEN  rdrop ;

: +expected ( n -- ) j^ expected @ tuck + dup j^ expected !
    j^ data-rmap $@ drop data-ackbits0 2@  2swap
    maxdata 1- + chunk-p2 rshift 1+ swap chunk-p2 rshift +DO
	dup I -bit  over I -bit  LOOP  2drop ;

: size! ( n id -- )  over j^ total    +!  state-addr  fs-size ! ;
: seek! ( n id -- )  over >r state-addr  fs-seek !@ r> swap -
    +expected ;

: size@ ( id -- n )  state-addr  fs-size @ ;
: seek@ ( id -- n )  state-addr  fs-seek @ ;

: save-blocks ( -- ) ?state
    0  j^ data-rmap $@ drop dest-raddr @
    j^ file-state $@ bounds ?DO
	I fs-seek @ I fs-oldseek @ 2dup = IF  2drop
	ELSE
	    over I fs-oldseek ! -
	    ." flush file <" 2 pick 0 .r ." >: " dup . cr
	    I fs-fid @ IF
		2dup I fs-fid @ write-file throw
	    THEN  +
	THEN
	swap 1+ swap
    file-state-struct +LOOP 2drop ;

: save-to ( addr u n -- )  state-addr >r
    r/w create-file throw r> fs-fid ! ;

\ open a file - this needs *way more checking*!

: id>file ( id -- fid )
    >r j^ file-handles $@ r> cells safe/string
    0= throw  @ ;

: n2o:open-file ( addr u mode id -- )
    ?handles
    >r j^ file-handles $@ r@ cells /string  dup 0< nogap
    IF    dup @ ?dup-IF  close-file throw  THEN  dup off
    ELSE  drop r@ 1+ cells j^ file-handles $!len
	j^ file-handles $@ drop r@ cells +  THEN rdrop >r
    dup 2over ." open file: " type ."  with mode " . cr
    open-file throw r> ! ;

: n2o:close-file ( id -- )
    ?handles
    >r j^ file-handles $@ r@ cells safe/string
    IF
	dup @ ?dup-IF  close-file throw  THEN  dup off
    THEN
    drop rdrop ;

: n2o:slurp-block ( seek maxlen id -- nextseek )
    id>file >r over 0 r@ reposition-file throw
    data$@ rot umin r> read-file throw dup /data + ;

\ symmetric encryption and decryption

: >wurst-source' ( addr -- )  wurst-source state# move ;
: wurst-source-state> ( addr -- )  wurst-source swap state# 2* move ;
: >wurst-source-state ( addr -- )  wurst-source state# 2* move ;

: >wurst-source ( d -- )
    wurst-source state# bounds ?DO  2dup I 2!  2 cells +LOOP  2drop ;

: >wurst-key ( -- )
    j^ dup 0= IF
	drop wurst-key state#
    ELSE
	crypto-key $@
    THEN
    wurst-state swap move ;

\ regenerate ivs is a buffer swapping function:
\ regenerate half of the ivs per time, when you reach the middle of the other half
\ of the ivs buffer.

Defer regen-ivs

: ivs>code-source? ( addr -- )
    dup @ 0= IF  drop  EXIT  THEN
    $@ drop >r
    dest-addr @ r@ 2@ bounds within 0=
    IF
	dest-addr @  r@ dest-vaddr @ -  max-size^2 rshift
	r@ dest-ivs @ IF
	    r@ dest-ivs $@ 2 pick safe/string drop >wurst-source'
	    dup r@ regen-ivs
	THEN
	drop
    THEN
    rdrop ;

: ivs>source? ( addr -- )
    dup @ 0= IF  drop  EXIT  THEN
    $@ drop >r
    dest-addr @ r@ 2@ bounds within 0=
    IF
	dest-addr @  r@ dest-vaddr @ -  max-size^2 rshift
	r@ dest-ivs @ IF
	    r@ dest-ivs $@ 2 pick safe/string drop >wurst-source'
	THEN
	drop
    THEN
    rdrop ;

: wurst-outbuf-init ( flag -- )
    rnd-init >wurst-source'
    j^ IF
	IF
	    j^ code-map ivs>code-source?
	ELSE
	    j^ data-map ivs>source?
	THEN
    THEN
    >wurst-key ;

: wurst-inbuf-init ( flag -- )
    rnd-init >wurst-source'
    j^ IF
	IF
	    j^ code-rmap ivs>code-source?
	ELSE
	    j^ data-rmap ivs>source?
	THEN
    ELSE
\	." no iv mapping" cr
	drop
    THEN
    >wurst-key ;

: mem-rounds# ( size -- n )
    case
	min-size of  $22  endof
	min-size 2* of  $24  endof
	$28 swap
    endcase ;

: 2xor ( ud1 ud2 -- ud3 )  rot xor >r xor r> ;

: wurst-crc ( -- xd )
    pad roundse# rounds  \ another key diffusion round
    0. wurst-state state# bounds ?DO  I 2@ 2xor 2 cells +LOOP ;

[IFDEF] nocrypt \ dummy for test
    : encrypt-buffer  ( addr u n -- addr' 0 )  drop + 0 ;
    : wurst-outbuf-encrypt drop ;
    : wurst-inbuf-decrypt drop true ;
[ELSE]
    : encrypt-buffer ( addr u n -- addr 0 ) >r
	over roundse# rounds
	BEGIN  dup 0>  WHILE
		over r@ rounds  r@ >reads state# * safe/string
	REPEAT  rdrop ;
    
    : wurst-outbuf-encrypt ( flag -- )
	wurst-outbuf-init
	outbuf body-size mem-rounds# >r
	outbuf packet-data r@ encrypt-buffer
	rdrop drop wurst-crc rot 2! ;

    : wurst-inbuf-decrypt ( flag1 -- flag2 )
	\G flag1 is true if code, flag2 is true if decrypt succeeded
	wurst-inbuf-init
	inbuf body-size mem-rounds# >r
	inbuf packet-data
	over roundse# rounds
	BEGIN  dup 0>  WHILE
		over r@ rounds-decrypt  r@ >reads state# * safe/string
	REPEAT
	rdrop drop 2@ wurst-crc d= ;
[THEN]

\ public key encryption

\ these are dummy keys for testing!!!

$20 Constant keysize \ our shared secred is only 32 bytes long
\ server keys
Create pks
$21982058BCCB3476. 64, $36623B3840D9F393. 64, $B4B038E18F007E95. 64, $79CAED9D9F043F9B. 64,
Create sks
$EFDA8C1AE4F04358. 64, $4320CCB35C5F6C27. 64, $CE16D65418EA8575. 64, $127701E350CC537F. 64,
\ client keys
keysize buffer: pkc
keysize buffer: skc
\ shared secred
keysize buffer: keypad
Variable do-keypad

\ the theory here is that sks*pkc = skc*pks
\ we send our public key and know the server's public key.

: >wurst-key-ivs ( -- )
    j^ dup 0= IF
	drop wurst-key state# wurst-state swap move
    ELSE
	do-keypad @ IF
	    drop
	    keypad wurst-state keysize move
	    keypad wurst-state keysize + keysize move
	ELSE
	    crypto-key $@ wurst-state swap move
	THEN
    THEN ;

: regen-ivs/2 ( map -- ) >r
    r@ dest-ivsgen @ >wurst-source-state
    r@ dest-ivs $@
    r@ dest-ivslastgen @ IF  dup 2/ safe/string  ELSE  2/  THEN
    2dup erase
    dup mem-rounds# encrypt-buffer 2drop
    r@ dest-ivsgen @ wurst-source-state>
    -1 r> dest-ivslastgen xor! ;

: gen-ivs ( ivs-addr -- ) >r
    r@ $@ erase
    r@ $@ dup 2/ mem-rounds# encrypt-buffer 2drop
    r> cell+ @ wurst-source-state> ;

: regen-ivs-all ( map -- ) >r
    r@ dest-ivsgen @ >wurst-source-state
\    wurst-source state# 2* dump
    r> dest-ivs gen-ivs ;

: (regen-ivs) ( offset map -- ) >r
    dup r@ dest-ivs $@len
    r@ dest-ivslastgen @ IF \ check if in quarter 2
	2/ 2/ dup
    ELSE \ check if in quarter 4
	2/ dup 2/ dup >r + r>
    THEN  bounds within 0=  IF
\	." regenerate ivs " dup . cr
	r@ regen-ivs/2
    THEN  drop rdrop ;
' (regen-ivs) IS regen-ivs

: ivs-string ( addr u n addr -- )
    >r r@ $!len
    >wurst-key-ivs
    state# <> abort" 64 byte ivs!" >wurst-source'
    r> gen-ivs ;

: ivs-size@ ( map -- n addr ) $@ drop >r
    r@ dest-size @ max-size^2 rshift r> dest-ivs ;

: net2o:gen-data-ivs ( addr u -- )
    j^ data-map ivs-size@ ivs-string ;
: net2o:gen-code-ivs ( addr u -- )
    j^ code-map ivs-size@ ivs-string ;
: net2o:gen-rdata-ivs ( addr u -- )
    j^ data-rmap ivs-size@ ivs-string ;
: net2o:gen-rcode-ivs ( addr u -- )
    j^ code-rmap ivs-size@ ivs-string ;

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
	    errno . cr
	    true abort" could not send"
	THEN
    THEN ;

: >send ( addr n -- )  >r  r@ 64bit# or outbuf c!
    outbody min-size r> lshift move ;

: bandwidth+ ( -- )
    j^ ns/burst @ tick-init 1+ / j^ bandwidth-tick +! ;

: burst-end ( -- )  j^ data-b2b @ ?EXIT
    ticks j^ bandwidth-tick @ umax j^ next-tick ! ;

: sendX ( addr taddr target n -- )
    >r set-dest  r> >send  set-flags  bandwidth+  send-packet
    update-key ;

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
    dup >r send-size min-size over lshift
    dup r> u>= IF  ack-toggle# outflag xor!  THEN
    2r> 2swap ;

: net2o:send-packet ( addr u dest addr -- len )
    net2o:prep-send >r sendX r> ;

\ synchronous sending

: data-to-send ( -- flag )
    resend$@ nip 0> dest-tail$@ nip 0> or ;

: net2o:send-chunk ( -- )
    resend$@ dup IF
\	." resending " 
	net2o:get-resend
\	over hex. dup hex. cr
	net2o:prep-send /resend
    ELSE
	2drop
\	." sending "
	dest-tail$@ net2o:get-dest
\	over . dup . cr
	net2o:prep-send /dest-tail
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
	    bursts( ." no bursts in flight " j^ ns/burst ? cr )
	THEN
    THEN
    j^ ack-state @ outflag or!
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
	    drop .nosend
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
    r@ dest-tail off  r@ data-head off
    r> regen-ivs-all ;

: rewind-ackbits ( map -- ) >r
    r@ data-firstack0# off  r@ data-firstack1# off
    r@ data-lastack# on
    r@ dest-size @ addr>bits 1- 3 rshift 1+
    r@ data-ackbits0 @ over -1 fill
    r> data-ackbits1 @ swap -1 fill ;

: net2o:rewind-sender ( -- )
    j^ data-map $@ drop rewind-buffer ;

: net2o:rewind-receiver ( -- )
    j^ data-rmap $@ drop dup rewind-ackbits rewind-buffer ;

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

#100000000 Value poll-timeout# \ 100ms

: poll-sock ( -- flag )
    eval-queue  clear-events
    next-chunk-tick dup -1 <> >r ticks - dup 0>= r> or
    IF    0 max ptimeout cell+ !  pollfds 2
    ELSE  drop poll-timeout# ptimeout cell+ !  pollfds 2  THEN
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
    sockaddr-tmp alen @ insert-address  reverse64 inbuf destination be-x!
    over packet-size over <> abort" Wrong packet size" ;

: next-client-packet ( -- addr u )
    BEGIN  BEGIN  poll-sock  UNTIL  read-a-packet4/6  2dup d0= WHILE
	   2drop  REPEAT
    sockaddr-tmp alen @ check-address dup -1 <> IF
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
	true wurst-inbuf-decrypt 0= IF  ." invalid packet to 0" cr EXIT  THEN
	inbuf packet-data queue-command
    ELSE
	check-dest dup 0= IF  drop  EXIT  THEN
	dup 0> wurst-inbuf-decrypt 0= IF
	    inbuf .header
	    ." invalid packet to " dest-addr @ hex. cr
	    IF  drop  THEN  EXIT  THEN
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

: client-event ( -- )
    next-client-packet  2drop in-check
    IF  ['] handle-packet catch
	?dup-IF  ( inbuf packet-data dump ) DoError nothrow  THEN
    ELSE  ( drop packet )  THEN ;

\ loops for server and client

0 Value server?
Variable requests
Variable timeouts
20 timeouts ! \ 2s timeout

: server-loop ( -- )  true to server?
    BEGIN  server-event  AGAIN ;

: client-loop ( requests -- )  requests !  20 timeouts !  false to server?
    BEGIN  poll-sock  IF  client-event ELSE  -1 timeouts +!  THEN
     timeouts @ 0<=  requests @ 0= or  UNTIL ;

\ client/server initializer

: init-client ( -- )
    new-client init-route prep-socks ;

: init-server ( -- )
    new-server init-route prep-socks ;

\ load net2o commands

include net2o-cmd.fs
