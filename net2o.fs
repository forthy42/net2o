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

require net2o-err.fs

\ required tools

require ansi.fs
require date.fs
require mini-oof2.fs
require user-object.fs
require rec-scope.fs
require unix/socket.fs
require unix/mmap.fs
require unix/pthread.fs
require unix/filestat.fs
require net2o-tools.fs
require 64bit.fs
require debugging.fs
require kregion.fs
require crypto-api.fs
require keccak.fs
require threefish.fs
\ require wurstkessel.fs \ self-developped crypto for early development
keccak-o crypto-o !
require rng.fs
require ed25519-donna.fs
require hash-table.fs

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

[IFDEF] cygwin
    : no-hybrid ; \ cygwin can't deal with hybrid stacks
[THEN]

\ timestasts structure

begin-structure timestats
sffield: ts-delta
sffield: ts-slack
sffield: ts-reqrate
sffield: ts-rate
sffield: ts-grow
end-structure

\ per-thread memory space

UValue inbuf    ( -- addr )
UValue tmpbuf   ( -- addr )
UValue outbuf   ( -- addr )

user-o io-mem

object class
    pollfd 4 *                     uvar pollfds \ up to four file descriptors
    sockaddr_in                    uvar sockaddr
    sockaddr_in                    uvar sockaddr1
    [IFDEF] no-hybrid
	sockaddr_in                uvar sockaddr2
    [THEN]
    file-stat                      uvar statbuf
    cell                           uvar ind-addr
    cell                           uvar task#
    \ cell                           uvar reqmask
    $10                            uvar cmdtmp
    timestats                      uvar stat-tuple
    maxdata 2/ key-salt# + key-cksum# + uvar init0buf
    maxdata                        uvar aligned$
    cell                           uvar code0-buf^
    cell                           uvar code-buf^
    cell                           uvar code-buf$^
    cell                           uvar code-key^
end-class io-buffers

[IFDEF] 64bit
    ' min! Alias 64min!
    ' max! Alias 64max!
    ' umin! Alias 64umin!
    ' umax! Alias 64umax!
    ' !@ Alias 64!@
[ELSE]
    : dumin ( ud1 ud2 -- ud3 )  2over 2over du> IF  2swap  THEN  2drop ;
    : dumax ( ud1 ud2 -- ud3 )  2over 2over du< IF  2swap  THEN  2drop ;
    : 64!@ ( value addr -- old-value )   >r r@ 64@ 64swap r> 64! ;
    : 64min! ( d addr -- )  >r r@ 64@ dmin r> 64! ;
    : 64max! ( d addr -- )  >r r@ 64@ dmax r> 64! ;
    : 64umin! ( n addr -- )   >r r@ 64@ dumin r> 64! ;
    : 64umax! ( n addr -- )   >r r@ 64@ dumin r> 64! ;
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

: bittype ( addr base n -- )  bounds +DO
	dup I bit@ '+' '-' rot select emit  LOOP  drop ;

: bit-erase ( addr off len -- )
    dup 8 u>= IF
	>r dup 7 and >r 3 rshift + r@ bits 1- over andc!
	1+ 8 r> - r> swap -
	dup 7 and >r 3 rshift 2dup erase +
	0 r> THEN
    bounds ?DO  dup I -bit  LOOP  drop ;

: bit-fill ( addr off len -- )
    dup 8 u>= IF
	>r dup 7 and >r 3 rshift + r@ bits 1- invert over orc!
	1+ 8 r> - r> swap -
	dup 7 and >r 3 rshift 2dup $FF fill +
	0 r> THEN
    bounds ?DO  dup I +bit  LOOP  drop ;

\ variable length integers

: p@+ ( addr -- u64 addr' )  >r 64#0 r@ 10 bounds
    DO  7 64lshift I c@ $7F and n>64 64or
	I c@ $80 and 0= IF  I 1+ UNLOOP rdrop  EXIT  THEN
    LOOP  r> 10 + ;
[IFDEF] 64bit
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

[IFUNDEF] w, : w, ( w -- )  here w! 2 allot ; [THEN]

\ bit reversing

: bitreverse8 ( u1 -- u2 )
    0 8 0 DO  2* over 1 and + swap 2/ swap  LOOP  nip ;

Create reverse-table $100 0 [DO] [I] bitreverse8 c, [LOOP]

: reverse8 ( c1 -- c2 ) reverse-table + c@ ;
: reverse ( x1 -- x2 )
    0 cell 0 DO  8 lshift over $FF and reverse8 or
       swap 8 rshift swap  LOOP  nip ;
: reverse$16 ( addrsrc addrdst -- ) { dst } dup >r
    count reverse8 r@ $F + c@ reverse8 dst     c! dst $F + c!
    count reverse8 r@ $E + c@ reverse8 dst 1+  c! dst $E + c!
    count reverse8 r@ $D + c@ reverse8 dst 2 + c! dst $D + c!
    count reverse8 r@ $C + c@ reverse8 dst 3 + c! dst $C + c!
    count reverse8 r@ $B + c@ reverse8 dst 4 + c! dst $B + c!
    count reverse8 r@ $A + c@ reverse8 dst 5 + c! dst $A + c!
    count reverse8 r@ $9 + c@ reverse8 dst 6 + c! dst $9 + c!
    c@    reverse8 r> $8 + c@ reverse8 dst 7 + c! dst $8 + c! ;

\ print time

1970 1 1 ymd2day Constant unix-day0

: fsplit ( r -- r n )  fdup floor fdup f>s f- ;

: today? ( day -- flag ) ticks 64>f 1e-9 f* 86400e f/ floor f>s = ;

: .2 ( n -- ) s>d <# # # #> type ;
: .day ( seconds -- fraction/day ) 86400e f/ fsplit
    dup today? IF  drop  EXIT  THEN
    unix-day0 + day2ymd
    rot 0 .r '-' emit swap .2 '-' emit .2 'T' emit ;
: .timeofday ( fraction/day -- )
    24e f* fsplit .2 ':' emit 60e f* fsplit .2 ':' emit
    60e f* fdup 10e f< IF '0' emit 5  ELSE  6  THEN  3 3 f.rdp 'Z' emit ;

: .ticks ( ticks -- )
    64dup 64-0= IF  ." never" 64drop EXIT  THEN
    64dup -1 n>64 64= IF  ." forever" 64drop EXIT  THEN
    64>f 1e-9 f* .day .timeofday ;

\ IP address stuff

[IFDEF] no-hybrid
    0 0 2Value net2o-sock
[ELSE]
    0 Value net2o-sock
[THEN]
0 Value query-sock
Variable my-ip$
Variable my-addr[]

Create fake-ip4  $0000 w, $0000 w, $0000 w, $0000 w, $0000 w, $FFFF w,
\ prefix for IPv4 addresses encoded as IPv6
Create nat64-ip4 $0064 w, $ff9b w, $0000 w, $0000 w, $0000 w, $0000 w,
\ prefix for IPv4 addresses via NAT64

\ convention:
\ '!' is a key revocation, it contains the new key
\ '0' is a identifier, followed by an address (must be '1' or '2')
\ '1' indicates net2o
\ '2' IPv6+IPv4
\ Tags are kept sorted, so you'll get revocations first, then net2o and IPv6+4
\ Symbolic name may start with '@'+len followed by the name

Variable myhost
Variable myprio \ lower is more important, 0 is "no priority"

: default-host ( -- )
    pad $100 gethostname drop pad cstring>sstring myhost $!
    [IFDEF] android 20 [ELSE] 10 [THEN] \ mobile has lower prio
    myprio ! ;

default-host

: .myname ( -- )
    myprio @ IF  '0' emit myprio @ emit  THEN
    myhost $@len IF  myhost $@ dup '@' + emit type  THEN ;

Create ip6::0 here 16 dup allot erase
: .ip6::0 ( -- )  ip6::0 $10 type ;
: .ip4::0 ( -- )  ip6::0 4 type ;

Create sockaddr" 2 c, $16 allot

: .sockaddr
    \ convert socket into net2o address token
    [: { addr alen -- sockaddr u } '2' emit
    case addr family w@
	AF_INET of
	    .ip6::0 addr sin_addr 4 type
	endof
	AF_INET6 of
	    addr sin6_addr 12 fake-ip4 over str= IF
		.ip6::0 addr sin6_addr 12 + 4 type
	    ELSE
		addr sin6_addr $10 type .ip4::0
	    THEN
	endof
    endcase
    addr port 2 type ;] $tmp ;

: .port ( addr len -- addr' len' )
    ." :" over be-uw@ 0 ['] .r #10 base-execute  2 /string ;
: .net2o ( addr u -- ) dup IF  ." |" xtype  ELSE  2drop  THEN ;
: .ip4b ( addr len -- addr' len' )
    over c@ 0 ['] .r #10 base-execute 1 /string ;
: .ip4a ( addr len -- addr' len' )
    .ip4b ." ." .ip4b ." ." .ip4b ." ." .ip4b ;
: .ip4 ( addr len -- )
    .ip4a .port .net2o ;
User ip6:#
: .ip6w ( addr len -- addr' len' )
    over be-uw@ [: ?dup-IF 0 .r ip6:# off  ELSE  1 ip6:# +! THEN ;] $10 base-execute
    2 /string ;

: .ip6a ( addr len -- addr' len' )
    2dup fake-ip4 12 string-prefix? IF  12 /string .ip4a  EXIT  THEN
    -1 ip6:# !
    '[' 8 0 DO  ip6:# @ 2 < IF  emit  ELSE drop  THEN .ip6w ':'  LOOP
    drop ." ]" ;
: .ip6 ( addr len -- )
    .ip6a .port .net2o ;

: .ip64 ( addr len -- )
    over $10 ip6::0 over str= IF  16 /string  ELSE  .ip6a  THEN
    over   4 ip6::0 over str= IF  4 /string   ELSE  .ip4a  THEN
    .port .net2o ;

: .address ( addr u -- )
    over w@ AF_INET6 =
    IF  drop dup sin6_addr $10 .ip6a 2drop
    ELSE  drop dup sin_addr 4 .ip4a 2drop  THEN
    port 2 .port 2drop ; 

\ NAT traversal stuff: print IP addresses

: skip-symname ( addr u -- addr' u' )
    over c@ '0' = IF  2 safe/string  THEN
    over c@ '?' - 0 max safe/string ;
: .symname ( addr u -- addr' u' )
    over c@ '0' = IF  over 1+ c@ 0 .r '#' emit  2 safe/string  THEN
    over c@ '?' - 0 max >r r@ IF   '"' emit over r@ 1 /string type '"' emit  THEN
    r> safe/string ;

: .ipaddr ( addr len -- )  .symname
    case  over c@ >r 1 /string r>
	'1' of  ." |" xtype  endof
	'2' of  .ip64 endof
	dup emit -rot dump endcase ;

: .iperr ( addr len -- ) [: info-color attr!
      .time ." connected from: " .ipaddr default-color attr! cr ;] $err ;

: ipv4! ( ipv4 sockaddr -- )
    >r    r@ sin6_addr 12 + be-l!
    $FFFF r@ sin6_addr 8 + be-l!
    0     r@ sin6_addr 4 + l!
    0     r> sin6_addr l! ;

: ipv4!nat ( ipv4 sockaddr -- )
    \ nat64 version...
    >r        r@ sin6_addr 12 + be-l!
    0         r@ sin6_addr 8 + l!
    0         r@ sin6_addr 4 + l!
    $0064ff9b r> sin6_addr be-l! ;

: sock-rest ( sockaddr -- addr u ) >r
    AF_INET6 r@ family w!
    0        r@ sin6_flowinfo l!
    0        r@ sin6_scope_id l!
    r> sockaddr_in6 ;

: my-port ( -- port )
    sockaddr_in6 alen !
    net2o-sock [IFDEF] no-hybrid drop [THEN] sockaddr1 alen getsockname ?ior
    sockaddr1 port be-uw@ ;

: sock[ ( -- )  query-sock ?EXIT
    new-udp-socket46 to query-sock ;
: ]sock ( -- )  query-sock 0= ?EXIT
    query-sock closesocket 0 to query-sock ?ior ;

: 'sock ( xt -- )  sock[ catch ]sock throw ;

: ?fake-ip4 ( -- addr u )
    sockaddr1 sin6_addr dup $C fake-ip4 over
    str= IF  12 + 4  ELSE  $10   THEN ;

128 101 machine "mips" str= select Constant ENETUNREACH
29  Constant ESPIPE

[IFDEF] no-hybrid
    : sock4[ ( -- )  query-sock ?EXIT
	new-udp-socket to query-sock ;
    : ]sock4 ( -- )  query-sock 0= ?EXIT
	query-sock closesocket 0 to query-sock ?ior ;

    : 'sock4 ( xt -- ) sock4[ catch ]sock4 throw ;

    : sock-rest4 ( sockaddr -- addr u ) >r
	AF_INET r@ family w!
	r> sockaddr_in4 ;

    : check-ip4 ( ip4addr -- my-ip4addr 4 ) noipv4( 0 EXIT )
	[:
	  sockaddr_in4 alen !  53 sockaddr port be-w!
	  sockaddr sin_addr be-l! query-sock sockaddr sock-rest4 connect
	  dup 0< errno ENETUNREACH = and  IF  drop ip6::0 4  EXIT  THEN  ?ior
	  query-sock sockaddr1 alen getsockname
	  dup 0< errno ENETUNREACH = and  IF  drop ip6::0 4  EXIT  THEN  ?ior
	  sockaddr1 family w@ AF_INET6 =
	  IF  ?fake-ip4  ELSE  sockaddr1 sin_addr 4  THEN
	;] 'sock4 ;
[ELSE]
    : check-ip4 ( ip4addr -- my-ip4addr 4 ) noipv4( 0 EXIT )
	[: sockaddr_in6 alen !  53 sockaddr port be-w!
	  sockaddr ipv4! query-sock sockaddr sock-rest connect
	  dup 0< errno ENETUNREACH = and  IF  drop ip6::0 4  EXIT  THEN  ?ior
	  query-sock sockaddr1 alen getsockname
	  dup 0< errno ENETUNREACH = and  IF  drop ip6::0 4  EXIT  THEN  ?ior
	  sockaddr1 family w@ AF_INET6 =
	  IF  ?fake-ip4  ELSE  sockaddr1 sin_addr 4  THEN
	;] 'sock ;
[THEN]

$25DDC249 Constant dummy-ipv4 \ this is my net2o ipv4 address
Create dummy-ipv6 \ this is my net2o ipv6 address
$2A c, $03 c, $40 c, $00 c, $00 c, $02 c, $01 c, $88 c,
$0000 w, $0000 w, $0000 w, $00 c, $01 c,
Create local-ipv6
$FD c, $00 c, $0000 w, $0000 w, $0000 w, $0000 w, $0000 w, $0000 w, $0100 w,

0 Value my-port#

: ip6! ( addr1 addr2 -- ) $10 move ;
: ip6? ( addr -- flag )  $10 ip6::0 over str= 0= ;

: check-ip6 ( dummy -- ip6addr u ) noipv6( 0 EXIT )
    \G return IPv6 address - if length is 0, not reachable with IPv6
    [:  sockaddr_in6 alen !  53 sockaddr port be-w!
	sockaddr sin6_addr ip6!
	query-sock sockaddr sock-rest connect
	dup 0< errno ENETUNREACH = and  IF  drop ip6::0 $10  EXIT  THEN  ?ior
	query-sock sockaddr1 alen getsockname
	dup 0< errno ENETUNREACH = and  IF  drop ip6::0 $10  EXIT  THEN  ?ior
	?fake-ip4
    ;] 'sock ;

: check-ip64 ( dummy -- ipaddr u ) noipv4( check-ip6 EXIT )
    >r r@ check-ip6 dup IF  rdrop  EXIT  THEN
    2drop r> $10 + be-ul@ check-ip4 ;

: try-ip ( addr u -- flag )
    [: query-sock -rot connect 0= ;] 'sock ;

: global-ip4 ( -- ip4addr u )  dummy-ipv4 check-ip4 ;
: global-ip6 ( -- ip6addr u )  dummy-ipv6 check-ip6 ;
: local-ip6 ( -- ip6addr u )   local-ipv6 check-ip6 over c@ $FD = and ;

\ no-hybrid stuff

[IFDEF] no-hybrid
    0 warnings !@
    : sendto { sock1 sock2 pack u1 flag addr u2 -- size }
	addr family w@ AF_INET6 =
	IF  addr sin6_addr $C fake-ip4 over str=
	    IF
		AF_INET sockaddr2 family w!
		addr port w@ sockaddr2 port w!
		addr sin6_addr $C + l@ sockaddr2 sin_addr l!
		sock2 pack u1 flag sockaddr2 sockaddr_in4 sendto
	    ELSE
		sock1 pack u1 flag addr u2 sendto
	    THEN
	ELSE
	    sock2 pack u1 flag addr u2 sendto
	THEN ;
    warnings !
[THEN]

\ insert into sorted string array

: $ins[] ( addr u $array -- )
    \G insert O(log(n)) into pre-sorted array
    { $a } 0 $a $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup $# $a $[]@ compare dup 0= IF
		drop $# $a $[]!  EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    0 { w^ ins$0 } ins$0 cell $a r@ cells $ins r> $a $[]! ;
: $del[] ( addr u $array -- )
    \G delete O(log(n)) from pre-sorted array
    { $a } 0 $a $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup $# $a $[]@ compare dup 0= IF
		drop $# $a $[] $off
		$a $# cells cell $del
		2drop EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT 2drop 2drop ; \ not found

\ add IP addresses

Variable myname

: +my-ip ( addr u -- ) dup 0= IF  2drop  EXIT  THEN
    [: .myname '2' emit
      dup 4 = IF ip6::0 $10 type ELSE dup $10 = IF type ip6::0 4 THEN THEN type
      my-port# 8 rshift emit my-port# $FF and emit ;] $tmp
    nat( ." myip: " 2dup .ipaddr cr )
    my-ip$ $ins[] ;

Variable $tmp2

: !my-ips ( -- )  $tmp2 $off nat( ." start storing myips" cr )
    global-ip6 tuck [: type global-ip4 type ;] $tmp2 $exec
    $tmp2 $@ +my-ip
    0= IF  local-ip6  +my-ip THEN ;

\ this looks ok

: && ( flag -- ) ]] dup 0= ?EXIT drop [[ ; immediate compile-only
: &&' ( addr u addr' u' flag -- addr u false / addr u addr' u' )
    ]] 0= IF 2drop false EXIT THEN [[ ; immediate compile-only

: str=?0 ( addr1 u1 addr2 u2 -- flag )
    2dup ip6::0 over str= >r
    2over ip6::0 over str= >r str= r> r> or or ;

: my-ip= skip-symname 2swap skip-symname { addr1 u1 addr2 u2 -- flag }
    addr1 c@ '2' = addr2 c@ '2' = and &&
    addr1 u1 $15 safe/string addr2 u2 $15 safe/string str= &&
    addr1 1+ $10 addr2 1+ over str=?0 &&
    addr1 $11 + 4 addr2 $11 + over str=?0 ;

: str>merge ( addr1 u1 addr2 u2 -- )
    2dup ip6::0 over str= IF  rot umin move  ELSE  2drop 2drop  THEN ;

: my-ip>merge ( addr1 u1 addr2 u2 -- )
    skip-symname 2swap skip-symname 2swap
    { addr1 u1 addr2 u2 -- }
    addr1 1+ $10 addr2 1+ over  str>merge
    addr1 $11 + 4 addr2 $11 + over str>merge ;

: my-ip? ( addr u -- addr u flag )
    0 my-ip$ [: rot >r 2over my-ip= r> or ;] $[]map ;
: my-ip-merge ( addr u -- addr u flag )
    0 my-ip$ [: rot >r 2over 2over my-ip= IF
	  2over 2swap my-ip>merge rdrop true  ELSE  2drop r>  THEN ;] $[]map ;

\ Create udp socket

4242 Value net2o-port

Variable net2o-host "net2o.de" net2o-host $!

: net2o-socket ( port -- ) dup >r
    create-udp-server46
    [IFDEF] no-hybrid 0 [THEN] to net2o-sock
    r> ?dup-0=-IF  my-port  THEN to my-port#
    [IFDEF] no-hybrid
	net2o-sock drop my-port# create-udp-server to net2o-sock
    [THEN] !my-ips ;

begin-structure reply
    field: reply-len
    field: reply-offset
    64field: reply-dest
    field: reply-timeout# \ per-reply timeout counter
    field: reply-timeout-xt \ per-reply timeout xt
end-structure

m: addr>bits ( addr -- bits )
    chunk-p2 rshift ;
m: addr>bytes ( addr -- bytes )
    [ chunk-p2 3 + ]L rshift ;
m: bytes>addr ( bytes addr -- )
    [ chunk-p2 3 + ]L lshift ;
m: bits>bytes ( bits -- bytes )
    1- 2/ 2/ 2/ 1+ ;
m: bytes>bits ( bytes -- bits )
    3 lshift ;
m: addr>ts ( addr -- ts-offset )
    addr>bits 64s ;
m: addr>64 ( addr -- ts-offset )
    [ chunk-p2 3 - ]L rshift -8 and ;
m: addr>replies ( addr -- replies )
    addr>bits reply * ;
m: addr>keys ( addr -- keys )
    max-size^2 rshift [ min-size negate ]L and ;

\ generic hooks and user variables

UDefer other
UValue pollfd#  0 to pollfd#

Defer init-reply

: -other        ['] noop is other ;
-other

: prep-socks ( -- )
    epiper @ fileno POLLIN  pollfds fds!+ >r
    net2o-sock [IFDEF] no-hybrid swap [THEN] POLLIN  r> fds!+
    [IFDEF] no-hybrid POLLIN swap fds!+ [THEN]
    pollfds - pollfd / to pollfd# ;

\ the policy on allocation and freeing is that both freshly allocated
\ and to-be-freed memory is erased.  This makes sure that no unwanted
\ data will be lurking in that memory, waiting to be leaked out

: alloz ( size -- addr )
    dup >r allocate throw dup r> erase ;
: freez ( addr size -- )
    \G erase and then free - for secret stuff
    over swap erase free throw ;
: ?free ( addr size -- ) >r
    dup @ IF  dup @ r@ freez off  ELSE  drop  THEN  rdrop ;

: allo1 ( size -- addr )
    dup >r allocate throw dup r> $FF fill ;
: allocate-bits ( size -- addr )
    dup >r cell+ allo1 dup r> + off ; \ last cell is off

\ for bigger blocks, we use use alloc+guard, i.e. mmap with a
\ guard page after the end.

: alloc-buf ( -- addr )
    maxpacket-aligned buffers# * alloc+guard 6 + ;
: free-buf ( addr -- )
    6 - maxpacket-aligned buffers# * 2dup erase free+guard ;

: ?free+guard ( addr u -- )
    over @ IF  over @ swap 2dup erase  free+guard  off
    ELSE  2drop  THEN ;

ustack string-stack
ustack object-stack
ustack t-stack
ustack nest-stack

Defer alloc-code-bufs ' noop is alloc-code-bufs
Defer free-code-bufs  ' noop is free-code-bufs

Variable task-id#

: alloc-io ( -- ) \ allocate IO and reset generic user variables
    io-buffers new io-mem !
    1 task-id# +!@ task# !
    -other
    alloc-buf to inbuf
    alloc-buf to tmpbuf
    alloc-buf to outbuf
    alloc-code-bufs
    init-ed25519 c:init ;

: free-io ( -- )
    free-ed25519 c:free
    free-code-bufs
    0 io-mem !@ .dispose
    inbuf  free-buf
    tmpbuf free-buf
    outbuf free-buf ;

alloc-io

Variable net2o-tasks

: net2o-pass ( params xt n task -- )
    dup { w^ task }
    task cell net2o-tasks $+!  pass
    alloc-io b-out op-vector @ debug-vector !
    init-reply prep-socks catch
    1+ ?dup-IF  free-io 1- ?dup-IF  DoError  THEN
    ELSE  ~~ bflush 0 (bye) ~~  THEN ;
: net2o-task ( params xt n -- task )
    stacksize4 NewTask4 dup >r net2o-pass r> ;
: net2o-kills ( -- )
    net2o-tasks $@ bounds ?DO  I @ kill  cell +LOOP  net2o-tasks $off ;

0 warnings !@
: bye  net2o-kills  1 ms bye ;
warnings !

\ net2o header structure

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
    ." IP packets send/received: " packets ? ." (" packets2 ? ." dupes)/"
    packetr ? ." (" packetr2 ? ." dupes) " cr
    packets off packetr off packets2 off packetr2 off ;

User ptimeout  cell uallot drop
#10000000 Value poll-timeout# \ 10ms, don't sleep too long
poll-timeout# 0 ptimeout 2!

User socktimeout cell uallot drop

: sock-timeout! ( socket -- )  fileno
    socktimeout 2@
    ptimeout 2@ >r 1000 / r> 2dup socktimeout 2! d<> IF
	SOL_SOCKET SO_RCVTIMEO socktimeout 2 cells setsockopt THEN
    drop ;

0             Constant do-block
MSG_DONTWAIT  Constant don't-block

: read-a-packet ( blockage -- addr u / 0 0 )
    >r sockaddr_in alen !
    net2o-sock [IFDEF] no-hybrid drop [THEN]
    inbuf maxpacket r> sockaddr alen recvfrom
    dup 0< IF
	errno dup EAGAIN =  IF  2drop 0. EXIT  THEN
	512 + negate throw  THEN
    inbuf swap  1 packetr +!
    recvfrom( ." received from: " sockaddr alen @ .address space dup . cr )
;

[IFDEF] no-hybrid
    : read-a-packet4 ( blockage -- addr u / 0 0 )
	>r sockaddr_in alen !
	net2o-sock nip
	inbuf maxpacket r> sockaddr alen recvfrom
	dup 0< IF
	    errno dup EAGAIN =  IF  2drop 0. EXIT  THEN
	THEN
	inbuf swap  1 packetr +!
	recvfrom( ." received from: " sockaddr alen @ .address space dup . cr )
    ;
[THEN]

$00000000 Value droprate#

: %droprate ( -- )
    1 arg dup 0= IF  2drop  EXIT  THEN
    + 1- c@ '%' <> ?EXIT
    1 arg prefix-number IF  1e fmin 0e fmax $FFFFFFFF fm* f>s to droprate#
	shift-args  THEN ;

: send-a-packet ( addr u -- n ) +calc
    droprate# IF  rng32 droprate# u< IF
	    \ ." dropping packet" cr
	    2drop 0  EXIT  THEN  THEN
    2>r net2o-sock 2r> 0 sockaddr alen @ sendto +send 1 packets +!
    sendto( ." send to: " sockaddr alen @ .address space dup . cr ) ;

\ clients routing table

Variable routes

: init-route ( -- )  s" " routes hash@ $! ; \ field 0 is me, myself

: ipv4>ipv6 ( addr u -- addr' u' )
    drop >r
    r@ port be-uw@ sockaddr port be-w!
    r> sin_addr be-ul@ sockaddr ipv4!
    sockaddr sock-rest ;
: ?>ipv6 ( addr u -- addr' u' )
    over family w@ AF_INET = IF  ipv4>ipv6  THEN ;
: info@ ( info -- addr u )
    dup ai_addr @ swap ai_addrlen l@ ;
: info>string ( info -- addr u )
    info@ ?>ipv6 ;

0 Value lastaddr
Variable lastn2oaddr

: insert-address ( addr u -- net2o-addr )
    address( ." Insert address " 2dup .address cr )
    lastaddr IF  2dup lastaddr over str=
	IF  2drop lastn2oaddr @ EXIT  THEN
    THEN
    2dup routes #key dup -1 = IF
	drop s" " 2over routes #!
	last# $@ drop to lastaddr
	routes #key  dup lastn2oaddr !
    ELSE
	nip nip
    THEN ;

: insert-ip* ( addr u port hint -- net2o-addr )
    >r SOCK_DGRAM >hints r> hints ai_family l!
    get-info info>string insert-address ;

: insert-ip ( addr u port -- net2o-addr )  PF_INET   insert-ip* ;
: insert-ip4 ( addr u port -- net2o-addr ) PF_INET   insert-ip* ;
: insert-ip6 ( addr u port -- net2o-addr ) PF_INET6  insert-ip* ;

: address>route ( -- n/-1 )
    sockaddr alen @ insert-address ;
: route>address ( n -- ) dup >r
    routes #.key dup 0= IF  ." no address: " r> hex. cr drop  EXIT  THEN
    $@ sockaddr swap dup alen ! move  rdrop ;

\ route an incoming packet

User return-addr $10 cell- uallot drop
User temp-addr   $10 cell- uallot drop

[IFDEF] 64bit ' be-ux@ [ELSE] ' be-ul@ [THEN] alias be@
[IFDEF] 64bit ' be-x! [ELSE] ' be-l! [THEN] alias be!

: >rpath-len ( rpath -- rpath len )
    dup $100 u< IF  1  EXIT  THEN
    dup $10000 u< IF  2  EXIT  THEN
    dup $1000000 u< IF  3  EXIT  THEN
    [IFDEF] 64bit
	dup $100000000 u< IF  4  EXIT  THEN
	dup $10000000000 u< IF  5  EXIT  THEN
	dup $1000000000000 u< IF  6  EXIT  THEN
	dup $100000000000000 u< IF  7  EXIT  THEN
	8
    [ELSE]
	4
    [THEN] ;
: >path-len ( path -- path len )
    dup 0= IF  0  EXIT  THEN
    [IFDEF] 64bit
	dup $00FFFFFFFFFFFFFF and 0= IF  1  EXIT  THEN
	dup $0000FFFFFFFFFFFF and 0= IF  2  EXIT  THEN
	dup $000000FFFFFFFFFF and 0= IF  3  EXIT  THEN
	dup $00000000FFFFFFFF and 0= IF  4  EXIT  THEN
	dup $0000000000FFFFFF and 0= IF  5  EXIT  THEN
	dup $000000000000FFFF and 0= IF  6  EXIT  THEN
	dup $00000000000000FF and 0= IF  7  EXIT  THEN
	8
    [ELSE]
	dup $00FFFFFF and 0= IF  1  EXIT  THEN
	dup $0000FFFF and 0= IF  2  EXIT  THEN
	dup $000000FF and 0= IF  3  EXIT  THEN
	4
    [THEN] ;

: <0string ( endaddr -- addr u )
    $11 1 DO  1- dup c@ WHILE  LOOP  $10  ELSE  I  UNLOOP  THEN ;

: ins-source ( addr packet -- )
    destination >r reverse
    dup >rpath-len { w^ rpath rplen } rpath be!
    r@ $10 + <0string
    over rplen - swap move
    rpath cell+ rplen - r> $10 + rplen - rplen move ;
: ins-dest ( n2oaddr destaddr -- )
    >r dup >path-len { w^ path plen } path be!
    r@ cstring>sstring over plen + swap move
    path r> plen move ;
: skip-dest ( addr -- )
    $10 2dup 0 scan nip -
    2dup bounds ?DO
	I c@ $80 u< IF
	    2dup I 1+ -rot >r 2dup - r> swap - dup >r move
	    r> /string  LEAVE  THEN
    LOOP  erase ;

: get-dest ( packet -- addr )  destination dup be@ swap skip-dest ;
: route? ( packet -- flag )  destination c@  ;

: packet-route ( orig-addr addr -- flag )
    dup route?  IF
	>r r@ get-dest  route>address
	r> ins-source  false  EXIT  THEN
    2drop true ; \ local packet

: in-check ( -- flag )  address>route -1 <> ;
: out-route ( -- )  0 outbuf packet-route drop ;

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
    addr 64@ 64. cr
    r> base ! ;

\ each source has multiple destination spaces

64User dest-addr
User dest-flags
User validated

: >ret-addr ( -- )
    inbuf destination return-addr reverse$16 ;
: >dest-addr ( -- )
    inbuf addr 64@ dest-addr 64!
    inbuf flags w@ dest-flags w! ;

require net2o-classes.fs

\ : reqmask ( -- addr )
\     task# @ reqmask[] $[] ;

\ events for context-oriented behavior

: dbg-connect ( -- )  info-color attr!
    ." connected from: " pubkey $@ 85type default-color attr! cr ;
: dbg-disconnect ( -- )  info-color attr!
    ." disconnecting: " pubkey $@ 85type default-color attr! cr ;

Defer do-connect     ' dbg-connect IS do-connect
Defer do-disconnect  ' dbg-disconnect IS do-disconnect

event: ->connect    ( connection -- ) .do-connect ;

\ check for valid destination

: >data-head ( addr o:map -- flag )  dest-size @ 1- >r
    dup dest-back @ r@ and < IF  r@ + 1+  THEN
    maxdata + dest-back @ r> invert and + dup dest-head umax!@ <> ;

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

: check-dest ( size -- addr map o:job / f )
    \G return false if invalid destination
    \G return 1 if code, -1 if data, plus destination address
    negate \ generate mask
    dest-index 2 cells bounds ?DO
	I @ IF
	    dup dest-addr 64@ I @ >o dest-vaddr 64@ 64- 64>n and dup
	    dest-size @ u<
	    IF
		dup addr>bits ack-bit# !
		dest-raddr @ swap dup >data-head ack-advance? ! +
		o parent @ o> >o rdrop
		UNLOOP  rot drop  EXIT  THEN
	    drop o>
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

: alloc-data ( addr u -- u )
    dup >r dest-size ! dest-vaddr 64! r>
    dup alloc+guard dest-raddr !
    c:key# crypt-align + alloz dest-ivsgen ! \ !!FIXME!! should be a kalloc
    >code-flag @
    IF
	dup addr>replies  alloz dest-replies !
	3 dest-ivslastgen !
    ELSE
	dup addr>ts       alloz dest-timestamps !
    THEN ;

: map-data ( addr u -- o )
    o >code-flag @ IF rcode-class ELSE rdata-class THEN new >o parent !
    alloc-data
    >code-flag @ 0= IF
	dup addr>bytes allocate-bits data-ackbits !
    THEN
    drop
    o o> ;

: map-source ( addr u addrx -- o )
    o >code-flag @ IF code-class ELSE data-class THEN new >o parent !
    alloc-data
    >code-flag @ 0= IF
	dup addr>ts alloz data-resend# !
    THEN
    drop
    o o> ;

' @ Alias m@

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
#50.000.000 d>64 64Constant init-delay# \ 30ms initial timeout step

Variable init-context#
Variable msg-groups

UValue connection

: n2o:new-log ( -- o )
    cmd-class new >o  log-table @ token-table ! o o> ;
: n2o:new-ack ( -- o )
    o ack-class new >o  parent !  ack-table @ token-table !
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
    ack-context @ ?dup-0=-IF  n2o:new-ack dup ack-context !  THEN ;
: n2o:new-msg ( -- o )
    o msg-class new >o  parent !  msg-table @ token-table ! o o> ;

: no-timeout ( -- )  max-int64 next-timeout 64!
    ack-context @ ?dup-IF  >o 0 timeouts ! o>  THEN ;

: -flow-control ['] noop         ack-xt ! ;

: n2o:new-context ( addr -- o )
    context-class new >o timeout( ." new context: " o hex. cr )
    o contexts !@ next-context !
    o to connection \ current connection
    context-table @ token-table ! \ copy pointer
    init-context# @ context# !  1 init-context# +!
    dup return-addr be!  return-address be!
    ['] no-timeout timeout-xt ! ['] .iperr setip-xt !
    -flow-control
    -1 blocksize !
    1 blockalign !
    end-semas start-semas DO  I 0 pthread_mutex_init drop
    1 pthread-mutexes +LOOP
    o o> ;

\ insert address for punching

: ret-addr ( -- addr ) o IF  return-address  ELSE  return-addr  THEN ;

: !temp-addr ( addr u -- ) dup 0<> ind-addr !
    temp-addr dup $10 erase  swap $10 umin move ;

: 6>sock ( addr u -- )
    over $10 + w@ sockaddr1 port w!
    over $10 sockaddr1 sin6_addr swap move
    $12 /string !temp-addr ;

: 4>sock ( addr u -- )
    over $4 + w@ sockaddr1 port w!
    over be-ul@ sockaddr1 ipv4!
    6 /string !temp-addr ;

: 64>6sock ( addr u -- )
    over $14 + w@ sockaddr1 port w!
    over $10 sockaddr1 sin6_addr swap move
    $16 /string !temp-addr ;

: 64>4sock ( addr u -- )
    over $14 + w@ sockaddr1 port w!
    over $10 + be-ul@ sockaddr1 ipv4!
    $16 /string !temp-addr ;

: check-addr1 ( -- addr u flag )
    sockaddr1 sock-rest 2dup try-ip
    nat( ." check: " >r 2dup .address
    r> dup IF ."  ok"  ELSE  ."  ko"  THEN  cr ) ;

: ping-addr1 ( -- )
    check-addr1 0= IF  2drop  EXIT  THEN
    nat( ." ping: " 2dup .address cr )
    2>r net2o-sock "" 0 2r> sendto drop ;

: 64-6? ( addr u -- )  $10 umin    ip6::0 over str= 0= ;
: 64-4? ( addr u -- )  $10 /string 4 umin 64-6? ;

: $>sock ( addr u xt -- ) { xt }
    skip-symname
    case  over c@ >r 1 /string r>
	'2' of
	    2dup 64-4? IF  2dup 64>4sock xt execute THEN
	    2dup 64-6? IF  2dup 64>6sock xt execute THEN
	    2drop
	endof
	!!no-addr!!  endcase ;

\ insert keys

Variable 0keys

sema 0key-sema

: ins-0key [: { w^ addr -- }
	addr cell 0keys $+! ;] 0key-sema c-section ;
: del$one ( addr1 addr2 size -- pos )
    >r over @ cell+ - tuck r> $del ;
: next$ ( pos string -- addre addrs )
    $@ rot /string bounds ;
: del$cell ( addr stringaddr -- ) { string }
    string $@ bounds ?DO
	dup I @ = IF
	    string I cell del$one
	    unloop string next$ ?DO NOPE 0
	ELSE  cell  THEN
    +LOOP drop ;
: del-0key ( addr -- )
    [: 0keys del$cell ;] 0key-sema c-section ;
: search-0key ( .. xt -- .. )
    [: { xt } 0keys $@ bounds ?DO
	    I xt execute 0= ?LEAVE
	cell +LOOP
    ;] 0key-sema c-section ;

\ create new maps

Variable mapstart $1 mapstart !

: >is-server ( -- addr )
    parent @ 0= IF  is-server  ELSE  parent @ .recurse  THEN ;
: server? ( -- flag )  >is-server c@ negate ;
: server! ( -- )  1 >is-server c! ;
: setup! ( -- )   setup-table @ token-table !  dest-0key @ ins-0key ;
: context! ( -- )
    context-table @ token-table !  dest-0key @ del-0key
    <event wait-task @ ?dup-0=-IF [ up@ ]L THEN o elit, ->connect event> ;

: n2o:new-map ( u -- addr )
    drop mapstart @ 1 mapstart +! reverse
    [ cell 4 = ] [IF]  0 swap  [ELSE] $FFFFFFFF00000000 and [THEN] ; 
: n2o:new-data ( addrs addrd u -- )
    dup max-data# u> !!mapsize!! min-size swap lshift
    { 64: addrs 64: addrd u -- }
    o 0= IF
	addrd >dest-map @ ?EXIT
	return-addr be@ n2o:new-context >o rdrop  server! setup!  THEN
    msg( ." data map: " addrs $64. addrd $64. u hex. cr )
    >code-flag off
    addrd u data-rmap map-data-dest
    addrs u map-source data-map ! ;
: n2o:new-code ( addrs addrd u -- )
    dup max-code# u> !!mapsize!! min-size swap lshift
    { 64: addrs 64: addrd u -- }
    o 0= IF
	addrd >dest-map @ ?EXIT
	return-addr be@ n2o:new-context >o rdrop  server! setup!  THEN
    msg( ." code map: " addrs $64. addrd $64. u hex. cr )
    >code-flag on
    addrd u code-rmap map-code-dest
    addrs u map-source code-map ! ;

\ dispose connection

: free-resend ( o:data ) dest-size @ addr>ts >r
    data-resend#    r@ ?free
    dest-timestamps r> ?free ;
: free-resend' ( o:data ) dest-size @ addr>ts >r
    dest-timestamps r> ?free ;
: free-code ( o:data -- ) dest-size @ >r
    dest-raddr r@   ?free+guard
    dest-ivsgen     c:key# ?free
    dest-replies    r@ addr>replies ?free
    rdrop dispose ;
' free-code code-class to free-data
:noname ( o:data -- )
    free-resend free-code ; data-class to free-data

: free-rcode ( o:data --- )
    data-ackbits dest-size @ addr>bytes ?free
    data-ackbits-buf $off
    free-code ;
:noname ( o:data -- )
    free-resend' free-rcode ; rdata-class to free-data
' free-rcode rcode-class to free-data

\ symmetric key management and searching in open connections

: search-context ( .. xt -- .. ) { xt }
    \G xt has ( .. -- .. flag ) with true to continue
    contexts  BEGIN  @ dup  WHILE  >o  xt execute
	next-context o> swap  0= UNTIL  THEN  drop ;

\ data sending around

: >blockalign ( n -- block )
    blockalign @ dup >r 1- + r> negate and ;
: 64>blockalign ( 64 -- block )
    blockalign @ dup >r 1- n>64 64+ r> negate n>64 64and ;

: /head ( u -- )
    >blockalign dup negate residualread +! data-map @ .dest-head +! ;
: /back ( u -- )
    >blockalign dup negate residualwrite +!  data-rmap @ .dest-back +! ;
: /tail ( u -- )
    data-map @ .dest-tail +! ;
: data-dest ( -- addr )
    data-map @ >o
    dest-vaddr 64@ dest-tail @ dest-size @ 1- and n>64 64+ o> ;

\ new data sending around stuff, with front+back

: fix-size ( offset1 offset2 -- addr len )
    over - >r dest-size @ 1- and r> over + dest-size @ umin over - ;
: fix-tssize ( offset1 offset2 -- addr len )
    over - >r dest-size @ addr>ts 1- and r> over +
    dest-size @ addr>ts umin over - ;
: fix-bitsize ( offset1 offset2 -- addr len )
    over - >r dest-size @ addr>bits 1- and r> over +
    dest-size @ addr>bits umin over - ;
: raddr+ ( addr len -- addr' len ) >r dest-raddr @ + r> ;
: fix-size' ( base offset1 offset2 -- addr len )
    over - >r dest-size @ 1- and + r> ;
: head@ ( -- head )  data-map @ .dest-head @ ;
: data-head@ ( -- addr u )
    \G you can read into this, it's a block at a time (wraparound!)
    data-map @ >o
    dest-head @ dest-back @ dest-size @ + fix-size raddr+ o>
    residualread @ umin ;
: rdata-back@ ( -- addr u )
    \G you can write from this, also a block at a time
    data-rmap @ >o
    dest-back @ dest-tail @ fix-size raddr+ o>
    residualwrite @ umin ;
: data-tail@ ( -- addr u )
    \G you can send from this - as long as you stay block aligned
    data-map @ >o dest-raddr @ dest-tail @ dest-head @ fix-size' o> ;

: data-head? ( -- flag )
    data-map @ >o dest-head @ dest-back @ dest-size @ + u< o> ;
: data-tail? ( -- flag )
    data-map @ >o dest-tail @ dest-head @ u< o> ;
: rdata-back? ( -- flag )
    data-rmap @ >o dest-back @ dest-tail @ u< o> ;

\ code sending around

: code-dest ( -- addr )
    code-map @ >o dest-raddr @ dest-tail @ maxdata negate and + o> ;
: code-vdest ( -- addr )
    code-map @ >o dest-vaddr 64@ dest-tail @ n>64 64+ o> ;
: code-reply ( -- addr )
    code-map @ >o dest-tail @ addr>replies dest-replies @ + o> ;

: tag-addr ( -- addr )
    dest-addr 64@ code-rmap @ >o dest-vaddr 64@ 64- 64>n
    maxdata negate and addr>replies dest-replies @ + o> ;

reply buffer: dummy-reply
' noop dummy-reply reply-timeout-xt !

: reply[] ( index -- addr )
    code-map @ >o
    dup dest-size @ addr>bits u<
    IF  reply * dest-replies @ +  ELSE  dummy-reply  THEN  o> ;

: reply-index ( -- index )
    code-map @ .dest-tail @ addr>bits ;

: code+ ( n -- )
    connection .code-map @ >o dup negate dest-tail @ and +
    dest-size @ 1- and dest-back ! o> ;

: code-update ( n -- ) drop \ to be used later
    connection .code-map @ >o dest-back @ dest-tail ! o> ;

\ aligned buffer to make encryption/decryption fast

: $>align ( addr u -- addr' u ) dup $400 u> ?EXIT
    tuck aligned$ swap move aligned$ swap ;
    
\ timing records

sema timing-sema

: net2o:track-timing ( -- ) \ initialize timing records
    s" " timing-stat $! ;

: )stats ]] THEN [[ ;
: stats( ]] timing-stat @ IF [[ ['] )stats assert-canary ; immediate

: net2o:timing$ ( -- addr u )
    stats( timing-stat $@  EXIT ) ." no timing stats" cr s" " ;
: net2o:/timing ( n -- )
    stats( timing-stat 0 rot $del ) ;

: .rec-timing ( addr u -- )
    [: ack@ >o track-timing $@ \ do some dumps
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
      timestats +LOOP
      track-timing $off o> ;] timing-sema c-section ;

: net2o:rec-timing ( addr u -- )
    [: track-timing $+! ;] timing-sema c-section ;

: stat+ ( addr -- )  stat-tuple timestats  timing-stat $+! ;

\ flow control

64User ticker

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
    64dup 64-0<= IF  64drop 64drop  EXIT  THEN
    64- lastslack 64@ 64- slackgrow 64! ;

: >offset ( addr -- addr' flag )
    dest-vaddr 64@ 64- 64>n dup dest-size @ u< ;

#5000000 Value rt-bias# \ 5ms additional flybursts allowed

: net2o:set-flyburst ( -- bursts )
    rtdelay 64@ 64>f rt-bias# s>f f+ ns/burst 64@ 64>f f/ f>s
    flybursts# +
    bursts( dup . .o ." flybursts "
    rtdelay 64@ 64. ns/burst 64@ 64. ." rtdelay" cr )
    dup flybursts-max# min flyburst ! ;
: net2o:max-flyburst ( bursts -- )  flybursts-max# min flybursts max!@
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
    parent @ .data-map @ dup 0= IF  drop 0 0  EXIT  THEN  >r
    r@ >o >offset  IF
	dest-tail @ o> over - 0 max addr>bits window-size !
	addr>ts r> .dest-timestamps @ swap
    ELSE  o> rdrop 0 0  THEN ;

: net2o:ack-addrtime ( ticks addr -- )
    >timestamp over  IF
	dup tick-init 1+ 64s u>
	IF  + dup >r  64@
	    r@ tick-init 1+ 64s - 64@
	    64dup 64-0<= >r 64over 64-0<= r> or
	    IF  64drop 64drop  ELSE  64- lastdeltat 64!  THEN  r>
	ELSE  +  THEN
	64@ timestat
    ELSE  2drop 64drop  THEN ;

: net2o:ack-b2btime ( ticks addr -- )
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
	msg( ." slack ignored: " 64dup 64. cr )
	64drop 64#0 lastslack 64@ min-slack 64!
    THEN
    64>n stats( dup s>f stat-tuple ts-slack sf! )
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
           64dup last-time 64!@ 64- 64>f stat-tuple ts-delta sf!
           64over 64>f stat-tuple ts-reqrate sf! ) ;

: rate-stat2 ( rate -- rate )
    stats( 64dup extra-ns 64@ 64+ 64>f stat-tuple ts-rate sf!
           slackgrow 64@ 64>f stat-tuple ts-grow sf! 
           stat+ ) ;

: net2o:set-rate ( rate deltat -- )
    rate-stat1
    64>r tick-init 1+ validated @ 8 rshift 1 max 64*/
    64dup >extra-ns noens( 64drop )else( 64nip )
    64r> delta-t-grow# 64*/ 64min ( no more than 2*deltat )
    bandwidth-max n>64 64max
    rate-limit  rate-stat2
    ns/burst 64!@ bandwidth-init n>64 64= IF \ first acknowledge
	net2o:set-flyburst
	net2o:max-flyburst
    THEN ;

\ acknowledge

sema resize-sema

$20 Value mask-bits#
: >mask0 ( addr mask -- addr' mask' )
    BEGIN  dup 1 and 0= WHILE  1 rshift >r maxdata + r>  dup 0= UNTIL  THEN ;
: net2o:resend-mask ( addr mask -- ) >mask0
    >r dup data-rmap @ .dest-size @ u>= IF
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
: net2o:ack-resend ( flag -- )  resend-toggle# and ack-resend~ c! ;
: resend$@ ( -- addr u )
    data-resend $@  IF
	2@ >mask0 1 and IF  maxdata  ELSE  0  THEN
	swap data-map @ .dest-raddr @ + swap
    ELSE  drop 0 0  THEN ;
: resend? ( -- flag )
    data-resend $@  IF  @ 0<>  ELSE  drop false  THEN ;

: resend-dest ( -- addr )
    data-resend $@ drop 2@ drop n>64 data-map @ .dest-vaddr 64@ 64+ ;
: /resend ( u -- )
    0 +DO
	data-resend $@ drop
	dup >r 2@ -2 and >mask0 tuck r> 2!
	0= IF  data-resend 0 2 cells $del  THEN
    maxdata +LOOP ;

\ resend third handshake

: push-reply ( addr u -- )  resend0 $!  return-addr r0-address $10 move ;

\ file handling

require net2o-file.fs

\ helpers for addresses

: -skip ( addr u char -- ) >r
    BEGIN  1- dup  0>= WHILE  2dup + c@ r@ <>  UNTIL  THEN  1+ rdrop ;
: >sockaddr ( -- addr len )
    return-address be@ routes #.key $@ .sockaddr ;
: n2oaddrs ( xt -- )
    my-ip$ [: [: type return-address $10 0 -skip type ;] $tmp
      rot dup >r execute r> ;] $[]map drop ;

\ load crypto here

require net2o-crypt.fs

\ send blocks of memory

: >dest ( addr -- ) outbuf destination $10 move ;
: set-dest ( target -- )
    64dup dest-addr 64!  outbuf addr 64! ;
: set-dest# ( resend# -- )
    n>64 64dup dest-addr 64+!  outbuf addr 64+! ;

User outflag  outflag off

: set-flags ( -- )
    0 outflag !@ outbuf 1+ c!
    outbuf w@ dest-flags w! ;

#90 Constant EMSGSIZE

: packet-to ( addr -- )  >dest
    out-route  outbuf dup packet-size
    send-a-packet 0< IF
	errno EMSGSIZE <> ?ior
	max-size^2 1- to max-size^2  ." pmtu/2" cr
    THEN ;

: send-code-packet ( -- ) +sendX
    header( ." send code " outbuf .header )
    outbuf flags 1+ c@ stateless# and IF
	outbuf0-encrypt  return-addr
	cmd0( .time ." cmd0 to: " dup $10 xtype cr )
    ELSE
	code-map @ outbuf-encrypt  return-address
    THEN   packet-to ;

: send-data-packet ( -- ) +sendX
    header( ." send data " outbuf .header )
    data-map @  outbuf-encrypt
    ret-addr packet-to ;

: >send ( addr n -- )
    >r  r@ [ 64bit# qos3# or ]L or outbuf c!  set-flags
    outbuf packet-body min-size r> lshift move ;

: bandwidth+ ( -- )
    ns/burst 64@ 1 tick-init 1+ 64*/ bandwidth-tick 64+! ;

: burst-end ( flag -- flag )
    ticker 64@ bandwidth-tick 64@ 64max next-tick 64! drop false ;

: send-cX ( addr n -- ) +sendX2
    >send  send-code-packet  net2o:update-key ;

: 64ffz< ( 64b -- u / -1 )
    \G find first zero from the right, u is bit position
    64 0 DO
	64dup 64>n 1 and 0= IF  64drop I unloop  EXIT  THEN
	64-2/
    LOOP 64drop $40 ;

: resend#+ ( addr -- n )
    dest-raddr @ - addr>64 data-resend# @ + { addr }
    rng8 $3F and { r }
    addr 64@ r 64ror 64ffz< r + $3F and to r
    64#1 r 64lshift addr 64@ 64or addr 64! 
    r ;

: resend#? ( off addr u -- n )
    0 rot 2swap \ count addr off u
    bounds dest-size @ addr>bits tuck umin >r umin r> \ limits
    64s data-resend# @ + swap
    64s data-resend# @ + swap ?DO
	dup c@ $40 u< IF
	    dup c@ >r 64#1 r> 64lshift
	    I 64@
	    64over 64invert 64over 64and I 64! \ ack only once!
	    64and 64-0= IF \ check if had been zero already
		2drop 0 UNLOOP  EXIT
	    THEN  swap 1+ swap
	THEN  1+
    8 +LOOP  drop ;

: send-dX ( addr n -- ) +sendX2
    over data-map @ .resend#+ set-dest#
    >send  ack@ .bandwidth+  send-data-packet ;

Defer punch-reply

: send-punch ( -- )
    check-addr1 0= IF  2drop  EXIT  THEN
    insert-address temp-addr ins-dest
    temp-addr return-addr $10 move
    nat( ." send punch to: " return-addr $10 xtype cr )
    punch-load $@ punch-reply ;

: net2o:punch ( addr u -- )
    o IF
	punch-load @ IF  ['] send-punch  ELSE  ['] ping-addr1  THEN
	$>sock
    ELSE  2drop  THEN ;

\ send chunk

\ branchless version using floating point

: send-size ( u -- n )
    min-size umax maxdata umin 1-
    [ min-size 2/ 2/ s>f 1/f ] FLiteral fm*
    { f^ <size-lb> }  <size-lb> 6 + c@ 4 rshift ;

64Variable last-ticks

: ts-ticks! ( addr -- )
    addr>ts dest-timestamps @ + >r last-ticks 64@ r>
    dup 64@ 64-0= IF  64!  EXIT  THEN  64on 64drop 1 packets2 +! ;
\ set double-used ticks to -1 to indicate unkown timing relationship

: net2o:send-tick ( addr -- )
    data-map @ >o
    dest-raddr @ - dup dest-size @ u<
    IF  ts-ticks!  ELSE  drop  THEN  o> ;

: net2o:prep-send ( addr u dest -- addr n len )
    set-dest  over  net2o:send-tick
    send-size min-size over lshift ;

\ synchronous sending

: data-to-send ( -- flag )
    resend? data-tail? or ;

: net2o:resend ( -- addr n )
    resend$@ resend-dest net2o:prep-send /resend ;

: net2o:send ( -- addr n )
    data-tail@ data-dest net2o:prep-send /tail ;

: ?toggle-ack ( -- )
    data-to-send 0= IF
	resend-toggle# outflag xor!  ack-toggle# outflag xor!
	never ack@ .next-tick 64!
    THEN ;

: net2o:send-chunk ( -- )  +chunk
    ack-state c@ outflag or!
    bursts# 1- data-b2b @ = IF data-tail? ELSE resend? 0= THEN
    IF  net2o:send  ELSE  net2o:resend  THEN
    dup 0= IF  2drop 2drop  EXIT  THEN
    ?toggle-ack send-dX ;

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
0 Value timeout-task

: do-send-chunks ( -- ) data-to-send 0= ?EXIT
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

event: ->send-chunks ( o -- ) .do-send-chunks ;

: net2o:send-chunks  sender-task 0= IF  do-send-chunks  EXIT  THEN
    <event o elit, ->send-chunks sender-task event> ;

: chunk-count+ ( counter -- )
    dup @
    dup 0= IF
	ack-toggle# ack-state xorc!
	ack-resend# c@
	ack-resend~ c@ ack-state c@ xor resend-toggle# and 0<> +
	0 max dup ack-resend# c!
	0= IF  ack-resend~ c@ ack-state c@ resend-toggle# invert and or
	    ack-state c!  ack@ .flybursts @ ack-resend# c!  THEN
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
    .o ." slack: " min-slack ? cr
    .o ." rtdelay: " rtdelay ? cr o> ;

: send-chunks-async ( -- flag )
    chunks $@ dup 0= IF  nip  EXIT  THEN
    chunks+ @ chunks-struct * safe/string
    IF
	dup chunk-context @ >o rdrop
	chunk-count
	data-to-send IF
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

:noname ( o:map -- ) dest-size @ addr>ts 
    dest-timestamps @ over erase
    data-resend# @ swap erase ;
data-class to rewind-timestamps
:noname ( o:map -- ) dest-size @ addr>ts
    dest-timestamps @ over erase ;
rdata-class to rewind-timestamps

: rewind-bits-partial ( new-back addr o:map -- )
    { addr } addr>bits dest-back @ addr>bits U+DO
	I I' fix-bitsize { len } addr + len $FF fill
    len +LOOP ;
: rewind-ts-partial ( new-back addr o:map -- )
    { addr } addr>ts dest-back @ addr>ts U+DO
	I I' fix-tssize { len } addr + len erase
    len +LOOP ;
:noname ( new-back o:map -- )
    dup data-resend# @ rewind-ts-partial
    dup dest-timestamps @ rewind-ts-partial
    regen-ivs-part ;
data-class to rewind-partial
:noname ( new-back o:map -- )
    dup dest-timestamps @ rewind-ts-partial
    regen-ivs-part ;
rdata-class to rewind-partial

: clearpages-partial ( new-back o:map -- )
    dest-back @ U+DO
	I I' fix-size raddr+ tuck clearpages
    +LOOP ;

: rewind-buffer ( o:map -- )
    1 dest-round +!
    dest-tail off  dest-head off  dest-back off  dest-top off
    regen-ivs-all  rewind-timestamps ;

: rewind-ackbits ( o:map -- )
    data-ack# off
    firstack( ." rewind firstacks" cr )
    data-ackbits @ dest-size @ addr>bytes $FF fill ;

: net2o:rewind-sender ( n -- )
    data-map @ >o dest-round @
    +DO  rewind-buffer  LOOP o> ;

: net2o:rewind-receiver ( n -- ) cookie( ." rewind" cr )
    data-rmap @ >o dest-round @
    +DO  rewind-buffer  LOOP
    rewind-ackbits o> ;

: net2o:rewind-sender-partial ( new-back -- )
    data-map @ >o dest-back @ umax dup rewind-partial dest-back ! o> ;

\ separate thread for loading and saving...

: net2o:save ( -- )
    data-rmap @ .dest-back @ >r n2o:spit
    r> data-rmap @ >o dest-back !@
    dup rewind-partial  dup  dest-back!  do-slurp !@ drop o> ;

Defer do-track-seek

event: ->track ( o -- )  >o ['] do-track-seek n2o:track-all-seeks o> ;
event: ->slurp ( task o -- )  >o n2o:slurp o elit, ->track event> o> ;
event: ->save ( o -- ) .net2o:save ;

0 Value file-task

: create-file-task ( -- )
    [:  ." created file task " up@ hex. cr
	BEGIN  ['] event-loop catch dup -1 <> WHILE
	    ?dup-IF  DoError  THEN  REPEAT  drop ;]
    1 net2o-task to file-task ;
: net2o:save& ( -- ) file-task 0= IF  create-file-task  THEN
    o elit, ->save file-task event> ;

\ schedule delayed events

object class
64field: queue-timestamp
field: queue-job
field: queue-xt
end-class queue-class
queue-class >osize @ Constant queue-struct

Variable queue s" " queue $!
queue-class >osize @ buffer: queue-adder  

: add-queue ( xt us -- )
    ticker 64@ +  o queue-adder >o queue-job !  queue-timestamp 64!
    queue-xt !  o queue-struct queue $+! o> ;

: eval-queue ( -- )
    queue $@len 0= ?EXIT  ticker 64@
    queue $@ bounds ?DO  I >o
	64dup queue-timestamp 64@ 64u> IF
	    queue-xt @ queue-job @ .execute o>
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
    0. sendbs# 0 DO
	2drop  send-anything?
	sends# 0 ?DO
	    0= IF  try-read-packet-wait
		dup IF  UNLOOP  UNLOOP  EXIT  THEN  2drop  THEN
	    send-another-chunk  LOOP  drop
    read-a-packet? dup ?LEAVE LOOP ;

: send-loop ( -- )
    send-anything?
    BEGIN  0= IF   wait-send drop read-event  THEN
	send-another-chunk  AGAIN ;

: create-sender-task ( -- )
    [:  ." created sender task " up@ hex. cr
	prep-evsocks send-loop ;] 1 net2o-task to sender-task ;

Defer handle-beacon

: next-packet ( -- addr u )
    sender-task 0= IF  send-read-packet  ELSE  try-read-packet-wait  THEN
    dup minpacket# u>= IF
	sockaddr alen @ insert-address inbuf ins-source
	over packet-size over <> !!size!! +next
	EXIT
    THEN
    dup 1 = IF  drop c@ handle-beacon   0 0  EXIT  THEN
;

0 Value dump-fd

: net2o:timeout ( ticks -- ) \ print why there is nothing to send
    ack@ .>flyburst net2o:send-chunks
    timeout( ." timeout? " .ticks space
    resend? . data-tail? . data-head? . fstates .
    chunks+ ? ack@ .bandwidth? . next-chunk-tick .ticks cr )else( 64drop ) ;

\ timeout handling

#2.000.000.000 d>64 64Value timeout-max# \ 2s maximum timeout
#30.000.000 d>64 64Value timeout-min# \ 30ms minimum timeout
#14 Value timeouts# \ with 30ms initial timeout, gives 4.8s cummulative timeout

Sema timeout-sema
Variable timeout-tasks s" " timeout-tasks $!

: 0timeout ( -- )
    ack@ .rtdelay 64@ timeout-min# 64max ticker 64@ 64+ next-timeout 64!
    0 ack@ .timeouts !@ IF  timeout-task wake  THEN ;
: do-timeout ( -- )  timeout-xt perform ;

: o+timeout ( -- )  0timeout
    timeout( ." +timeout: " o hex. ." task: " task# ? cr )
    [: timeout-tasks $@ bounds ?DO  I @ o = IF
	      UNLOOP  EXIT  THEN
      cell +LOOP
      o { w^ timeout-o }  timeout-o cell timeout-tasks $+! ;]
  timeout-sema c-section  timeout-task wake ;
: o-timeout ( -- )
    0timeout  timeout( ." -timeout: " o hex. ." task: " task# ? cr )
    [: o timeout-tasks del$cell ;] timeout-sema c-section ;

: sq2** ( 64n n -- 64n' )
    dup 1 and >r 2/ 64lshift r> IF  64dup 64-2/ 64+  THEN ;
: +timeouts ( -- timeout ) 
    rtdelay 64@ timeout-min# 64max timeouts @ sq2**
    timeout-max# 64min \ timeout( ." timeout setting: " 64dup 64. cr )
    ticker 64@ 64+ ;
: >next-timeout ( -- )  ack@ .+timeouts next-timeout 64! ;
: 64min? ( a b -- min flag )
    64over 64over 64< IF  64drop false  ELSE  64nip true  THEN ;
: next-timeout? ( -- time context ) [: 0 { ctx } max-int64
    timeout-tasks $@ bounds ?DO
	I @ .next-timeout 64@ 64min? IF  I @ to ctx  THEN
    cell +LOOP  ctx ;] timeout-sema c-section ;
: ?timeout ( -- context/0 )
    ticker 64@ next-timeout? >r 64- 64-0>= r> and ;

: -timeout      ['] no-timeout  timeout-xt ! o-timeout ;

\ handling packets

Defer queue-command ( addr u -- )
' dump IS queue-command

$01 Constant crypt-val
$02 Constant own-crypt-val
$04 Constant login-val
$08 Constant cookie-val
$10 Constant tmp-crypt-val
$20 Constant signed-val

: crypt?     ( -- flag )  validated @ crypt-val     and ;
: own-crypt? ( -- flag )  validated @ own-crypt-val and ;
: login?     ( -- flag )  validated @ login-val     and ;
: cookie?    ( -- flag )  validated @ cookie-val    and ;
: tmp-crypt? ( -- flag )  validated @ tmp-crypt-val and ;
: signed?    ( -- flag )  validated @ signed-val    and ;

: !!signed?  ( -- ) signed? 0= !!unsigned!! ;
: !!<order?   ( n -- )  dup c-state @ u>  !!inv-order!! c-state or! ;
: !!>order?   ( n -- )  dup c-state @ u<= !!inv-order!! c-state or! ;
: !!>=order?   ( n -- )  dup c-state @ u< !!inv-order!! c-state or! ;
: !!<>order?   ( n1 n2 -- )  dup >r
    c-state @ -rot swap within !!inv-order!! r> c-state or! ;
: !!<>=order?   ( n1 n2 -- )  dup >r 1+
    c-state @ -rot swap within !!inv-order!! r> c-state or! ;

: handle-cmd0 ( -- ) \ handle packet to address 0
    cmd0( .time ." handle cmd0 " sockaddr alen @ .address cr )
    0 >o rdrop \ address 0 has no job context!
    inbuf0-decrypt 0= IF
	." invalid packet to 0" drop cr EXIT  THEN
    validated off \ we have no validated encryption
    stateless# outflag !  inbuf packet-data queue-command ;

: handle-data ( addr -- )  parent @ >o  o to connection
    msg( ." Handle data to addr: " dup hex. cr )
    >r inbuf packet-data r> swap move
    +inmove ack-xt perform +ack 0timeout o> ;
' handle-data rdata-class to handle
' drop data-class to handle

: handle-cmd ( addr -- )  parent @ >o
    msg( ." Handle command to addr: " dup hex. cr )
    outflag off
    maxdata negate and >r inbuf packet-data r@ swap dup >r move
    r> r> swap queue-command o IF  ( 0timeout ) o>  ELSE  rdrop  THEN ;
' handle-cmd rcode-class to handle
' drop code-class to handle

: .inv-packet ( -- )
    ." invalid packet to "
    dest-addr 64@ o IF  dest-vaddr 64@ 64-  THEN  $64.
    ." size " min-size inbuf c@ datasize# and lshift hex. cr ;

: handle-dest ( addr map -- ) \ handle packet to valid destinations
    ticker 64@  ack@ .recv-tick 64! \ time stamp of arrival
    dup >r inbuf-decrypt 0= IF
	invalid( r> >o .inv-packet o>  drop )else( rdrop drop ) EXIT
    THEN
    crypt-val validated ! \ ok, we have a validated connection
    return-addr return-address $10 move
    r> >o handle o IF  o>  ELSE  rdrop  THEN ;

: handle-packet ( -- ) \ handle local packet
    >ret-addr >dest-addr +desta
    dest-flags 1+ c@ stateless# and  IF
	handle-cmd0
    ELSE
	inbuf body-size check-dest dup 0= IF
	    msg( ." unhandled packet to: " dest-addr 64@ $64. cr )
	    drop  EXIT  THEN +dest
	handle-dest
    THEN ;

: route-packet ( -- )
    inbuf >r r@ get-dest route>address
    route( ." route to: " sockaddr alen @ .address space
           inbuf destination $10 xtype cr )
    r> dup packet-size send-a-packet 0< ?ior ;

\ dispose context

: unlink-ctx ( next hit ptr -- )
    next-context @ o contexts
    BEGIN  2dup @ <> WHILE  @ dup .next-context swap 0= UNTIL
	2drop drop EXIT  THEN  nip ! ;
: ungroup-ctx ( -- )
    msg-groups [: >r o r> cell+ del$cell ;] #map ;

: n2o:dispose-context ( o:addr -- o:addr )
    [: cmd( ." Disposing context... " o hex. cr )
	timeout( ." Disposing context... " o hex. ." task: " task# ? cr )
	o-timeout o-chunks
	0. data-rmap @ .dest-vaddr 64@ >dest-map 2!
	dest-0key @ del-0key
	end-maps start-maps DO  I @ ?dup-IF  .free-data  THEN  cell +LOOP
	end-strings start-strings DO  I $off     cell +LOOP
	end-secrets start-secrets DO  I sec-off  cell +LOOP
	fstate-off
	\ erase crypto keys
	log-context @ ?dup-IF  .dispose  THEN
	ack-context @ ?dup-IF
	    >o timing-stat $off track-timing $off dispose o>
	THEN
	msg-context @ ?dup-IF  .dispose  THEN
	unlink-ctx  ungroup-ctx
	end-semas start-semas DO  I pthread_mutex_destroy drop
	1 pthread-mutexes +LOOP
	dispose  0 to connection
	cmd( ." disposed" cr ) ;] file-sema c-section ;

event: ->disconnect ( connection -- ) >o do-disconnect n2o:dispose-context o> ;

\ loops for server and client

8 cells 1- Constant maxrequest#

: next-request ( -- n )
    1 dup request# +!@ maxrequest# and tuck lshift reqmask or!
    request( ." Request added: " dup . ." o " o hex. ." task: " task# ? cr ) ;

: packet-event ( -- )
    next-packet !ticks nip 0= ?EXIT  inbuf route?
    IF  route-packet  ELSE  handle-packet  THEN ;

event: ->request ( n o -- ) >o 1 over lshift invert reqmask and!
    request( ." Request completed: " . ." o " o hex. ." task: " task# ? cr )else( drop )
    reqmask @ 0= IF  request( ." Remove timeout" cr ) -timeout
    ELSE  request( ." Timeout remains: " reqmask @ hex. cr ) THEN o> ;
event: ->reqsave ( task n o -- )  <event swap elit, elit, ->request event> ;
event: ->timeout ( o -- )
    >o 0 reqmask !@ >r -timeout r> o> msg( ." Request timed out" cr )
    r> 0<> !!timeout!! ;

: timeout-expired? ( -- flag )
    ack@ .timeouts @ timeouts# >= ;
: push-timeout ( o:connection -- )
    timeout-expired? wait-task @ and  ?dup-IF
	o elit, ->timeout event>  THEN ;
	
: request-timeout ( -- )
    ?timeout ?dup-IF  >o rdrop
	timeout( ." do timeout: " o hex. timeout-xt @ .name cr )
	do-timeout
    THEN ;

\ beacons
\ UDP connections through a NAT close after timeout,
\ typically after a minute or so.
\ To keep connections alive, you have to send a "beacon" a bit before
\ the connection would expire to refresh the NAT window.
\ beacons are send regularly regardless if you have any other traffic,
\ because that's easier to do.
\ beacons are one-byte packets, either space (no-reply) or '?' (reply if new)

#55.000.000.000 d>64 64Value beacon-ticks# \ 55s beacon tick rate
64Variable beacon-time ticks beacon-time 64!

: +beacons ( -- )
    beacon-time 64@ beacon-ticks# 64+ beacon-time 64! ;

+beacons

Variable beacons \ destinations to send beacons to

: send-beacons ( -- )
    beacons [: beacon( ." send beacon to: " 2dup .address cr )
	2>r net2o-sock s" ?" 0 2r> sendto +send ;] $[]map ;

: beacon? ( -- )
    beacon-time 64@ ticker 64@ 64- 64-0< IF
	send-beacons +beacons
    THEN ;

: +beacon ( sockaddr len -- )
    beacon( ." add beacon: " 2dup .address cr )
    beacons $ins[] ;
: -beacon ( sockaddr len -- )
    beacon( ." remove beacon: " 2dup .address cr )
    beacons $del[] ;
: add-beacon ( net2oaddr -- ) route>address sockaddr alen @ +beacon ;
: sub-beacon ( net2oaddr -- ) route>address sockaddr alen @ -beacon ;
: ret+beacon ( -- )  ret-addr be@ add-beacon ;
: ret-beacon ( -- )  ret-addr be@ sub-beacon ;

\ timeout loop

: .loop-err ( throw xt -- )
    .name dup . cr DoError cr ;

: event-send ( -- )
    o IF  wait-task @  ?dup-IF  event>  THEN  0 >o rdrop  THEN ;

#10000000 Constant watch-timeout# \ 10ms timeout check interval

: >next-ticks ( -- )
    next-timeout? drop beacon-time 64@ 64umin ticker 64@ 64-
    64#0 64max timeout( ." wait for " 64dup 64. ." ns" cr )
    stop-64ns
    timeout( ticker 64@ ) !ticks
    timeout( ticker 64@ 64swap 64- ." waited for " 64. ." ns" cr ) ;

: timeout-loop-nocatch ( -- ) !ticks
    BEGIN  >next-ticks beacon? request-timeout event-send  AGAIN ;

: catch-loop { xt -- flag }
    BEGIN   nothrow xt catch dup -1 = ?EXIT
	?int dup  WHILE  xt .loop-err  REPEAT
    drop false ;

: create-timeout-task ( -- )  timeout-task ?EXIT
    [:  \ ." created timeout task " task# ? cr
	['] timeout-loop-nocatch catch-loop drop ;]
    1 net2o-task to timeout-task ;

\ event loop

: event-loop-nocatch ( -- ) \ 1 stick-to-core
    BEGIN  packet-event  event-send  AGAIN ;

: n2o:request-done ( n -- )
    file-task ?dup-IF  <event swap wait-task @ elit, elit, o elit, ->reqsave event>
    ELSE  elit, o elit, ->request  THEN ;

0 value core-wanted

: create-receiver-task ( -- )
    [:  \ ." created receiver task " task# ? cr
	[IFDEF] stick-to-core  core-wanted stick-to-core drop  [THEN]
	['] event-loop-nocatch catch-loop drop
	    ( wait-task @ ?dup-IF  ->timeout event>  THEN ) ;]
    1 net2o-task to receiver-task ;

: event-loop-task ( -- )
    receiver-task 0= IF  create-receiver-task  THEN ;

: requests->0 ( -- ) request( ." wait reqmask=" o IF reqmask @ hex. THEN cr )
    BEGIN  stop
    o IF  reqmask @ 0=  ELSE  false  THEN  UNTIL
    o IF  o-timeout  THEN  request( ." wait done" cr ) ;

: client-loop ( -- )
    !ticks
    connection >o
    o IF  up@ wait-task !  o+timeout  THEN
    event-loop-task requests->0 o> ;

: server-loop ( -- )
    1 to core-wanted  0 >o rdrop  BEGIN  client-loop  AGAIN ;

\ client/server initializer

: init-cache ( -- )
    s" .cache" file-status nip #-514 = IF
	s" .cache" $1FF =mkdir throw
    THEN ;

: init-rest ( port -- )  init-mykey init-mykey \ two keys
    \ hash-init-rng
    init-timer net2o-socket init-route prep-socks
    sender( create-sender-task ) create-timeout-task ;

Variable initialized

: init-client ( -- )  true initialized !@ 0= IF  init-cache 0 init-rest  THEN ;
: init-server ( -- )  true initialized !@ 0= IF  net2o-port init-rest  THEN ;

\ connection cookies

object class
    64field: cc-timeout
    field: cc-context
end-class con-cookie

con-cookie >osize @ Constant cookie-size#

Variable cookies

#5000000000. d>64 64Constant connect-timeout#

: add-cookie ( -- cookie64 )
    [: ticks 64dup [IFUNDEF] 64bit swap [THEN] o
	{ 64^ cookie-adder w^ cookie-o }
	cookie-adder cookie-size#  cookies $+! ;]
    resize-sema c-section ;

: do-?cookie ( cookie -- context true / false )
    ticker 64@ connect-timeout# 64- { 64: timeout }
    cookies $@ bounds ?DO
	I .cc-timeout 64@ timeout 64u< IF
	    cookies I cookie-size# del$one
	    unloop cookies next$ ?DO  NOPE  0
	ELSE
	    64dup I .cc-timeout 64@ 64= IF
		64drop I .cc-context @
		cookies I cookie-size# del$one drop
		unloop  true  EXIT
	    THEN
	    cookie-size#  THEN
    +LOOP  64drop 0 ;
  
: ?cookie ( cookie -- context true / false )
    ['] do-?cookie resize-sema c-section ;

: cookie>context? ( cookie -- context true / false )
    ?cookie over 0= over and IF
	nip return-addr be@ n2o:new-context swap
    THEN ;

: rtdelay! ( time -- ) recv-tick 64@ 64swap 64- rtdelay 64! ;
: adjust-ticks ( time -- )  o 0= IF  64drop  EXIT  THEN
    recv-tick 64@ 64- rtdelay 64@ 64dup 64-0<> >r 64-2/
    64over 64abs 64over 64> r> and IF
	64+ adjust-timer( ." adjust timer: " 64dup 64. forth:cr )
	tick-adjust 64!
    ELSE
	64+ adjust-timer( ." don't adjust timer: " 64dup 64. forth:cr )
	64drop  THEN ;

\ load net2o plugins: first one with integraded command space

require net2o-cmd.fs
require net2o-connect.fs
require net2o-connected.fs
require net2o-log.fs
require net2o-keys.fs
require net2o-dht.fs
require net2o-addr.fs
require net2o-msg.fs
\ require net2o-term.fs

\ connection setup helper

: ins-ip ( -- net2oaddr )
    net2o-host $@ net2o-port insert-ip ;
: ins-ip4 ( -- net2oaddr )
    net2o-host $@ net2o-port insert-ip4 ;
: ins-ip6 ( -- net2oaddr )
    net2o-host $@ net2o-port insert-ip6 ;

: pk:connect ( code data key u ret -- )
    [: .time ." Connect to: " dup hex. cr ;] $err
    n2o:new-context >o rdrop o to connection  setup!
    dest-pk \ set our destination key
    n2o:connect
    +flow-control +resend
    [: .time ." Connected, o=" o hex. cr ;] $err ;

: c:disconnect ( -- ) [: ." Disconnecting..." cr ;] $err
    disconnect-me [: .packets profile( .times ) ;] $err ;

: c:fetch-id ( pubkey u -- )
    net2o-code
      expect-reply  fetch-id,
      cookie+request
    end-code| ;

: pk:addme-fetch-host ( key u -- ) +addme
    net2o-code
      expect-reply get-ip fetch-id, replace-me,
      cookie+request
    end-code| -setip n2o:send-replace ;

Variable dhtnick "net2o-dhtroot" dhtnick $!

: announce-me ( -- )
    tick-adjust 64@ 64-0= IF  +get-time  THEN
    $8 $8 dhtnick $@ nick>pk ins-ip
    dup add-beacon pk:connect replace-me disconnect-me
    -other ;

: pk-lookup ( addr u -- )
    $A $E dhtnick $@ nick>pk ins-ip pk:connect
    2dup pk:addme-fetch-host
    BEGIN  >d#id >o 0 dht-host $[]@ o> 2dup d0= !!host-notfound!!
	over c@ '!' =  WHILE
	    replace-key o> >o ke-pk $@ ." replace key: " 2dup 85type cr
	    o o> >r 2dup c:fetch-id r> >o
    REPEAT  2drop disconnect-me ;
: insert-host ( o addr u -- o )
    2 pick >o ." check host: " 2dup .host cr
    host>$ o> IF
	[: check-addr1 0= IF  2drop  EXIT  THEN
	    insert-address temp-addr ins-dest
	    ." insert host: " temp-addr $10 xtype cr
	    return-addr $10 0 skip nip 0= IF
		temp-addr return-addr $10 move
	    THEN ;] $>sock
    ELSE  2drop  THEN ;

: n2o:pklookup ( addr u -- )
    2dup >d#id { id }
    id .dht-host $[]# 0= IF  2dup pk-lookup  2dup >d#id to id  THEN
    0 n2o:new-context >o rdrop 2dup dest-pk  return-addr $10 erase
    id dup .dht-host ['] insert-host $[]map drop 2drop ;

: search-connect ( key u -- o/0 )
    0 [: drop 2dup pubkey $@ str= o and  dup 0= ;] search-context
    nip nip  dup to connection ;

:noname ( addr u cmdlen datalen -- )
    2>r n2o:pklookup 2r>
    cmd0( ." attempt to connect to: " return-addr $10 xtype cr )
    n2o:connect +flow-control +resend ; is pk-connect

: nick-connect ( addr u cmdlen datalen -- )
    2>r nick>pk 2r> pk-connect ;

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     (("64field:") non-immediate (font-lock-type-face . 2)
      "[ \t\n]" t name (font-lock-variable-name-face . 3))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]
