\ IP address stuff

\ Copyright (C) 2015   Bernd Paysan

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

\ IP address stuff

[IFDEF] no-hybrid
    0 0 2Value net2o-sock
[ELSE]
    0 Value net2o-sock
[THEN]
UValue query-sock
Variable my-addr[] \ object based hosts
Variable my-addr$ \ string based hosts (with sigs)
Variable pub-addr$ \ publicated addresses (with sigs)
Variable priv-addr$ \ unpublished addresses (with sigs)

Create fake-ip4  $0000 w, $0000 w, $0000 w, $0000 w, $0000 w, $FFFF w,
\ prefix for IPv4 addresses encoded as IPv6
Create nat64-ip4 $0064 w, $ff9b w, $0000 w, $0000 w, $0000 w, $0000 w,
\ prefix for IPv4 addresses via NAT64

: >alen ( addr u -- alen )
    sockaddr_in6 sockaddr_in4 rot family w@ AF_INET6 = select ;

\ convention:
\ '!' is a key revocation, it contains the new key
\ Tags are kept sorted, so you'll get revocations first, then net2o and IPv6+4
\ Symbolic name may start with '@'+len followed by the name

Variable host$

: get-host$ ( -- )
    pad $100 gethostname drop pad cstring>sstring host$ $! ;
: skip.site ( -- )
    host$ $@ s" .site" string-suffix? IF
	host$ dup $@len 5 - 5 $del
    THEN ;
: replace-host ( -- )
    config:orighost$ $@ host$ $@ str= IF
	config:host$ $@ host$ $!
    ELSE
	config:host$ $free  host$ $@ config:orighost$ $!
    THEN ;

: default-host ( -- )
    get-host$ skip.site replace-host
    [IFDEF] android 20 [ELSE] 10 [THEN] \ mobile has lower prio
    config:prio# ! ;

Create ip6::0 here 16 dup allot erase
: .ip6::0 ( -- )  ip6::0 $10 type ;
: .ip4::0 ( -- )  ip6::0 4 type ;

Create sockaddr" 2 c, $16 allot

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

Forward .addr$

: .iperr ( addr len -- )
    connect( [: <info> .time ." connected from: " .addr$ <default> cr ;] $err
    )else( 2drop ) ;

: ipv4! ( ipv4 sockaddr -- )
    ipv6(
    >r    r@ sin6_addr 12 + be-l!
    $FFFF r@ sin6_addr 8 + be-l!
    0     r@ sin6_addr 4 + l!
    0     r> sin6_addr l!
    )else( sin_addr be-l! ) ;

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

: sock-rest4 ( sockaddr -- addr u ) >r
    AF_INET r@ family w!
    r> sockaddr_in4 ;

: my-port ( -- port )
    ipv6( )else( sockaddr_in4 )else( sockaddr_in6 ) alen !
    net2o-sock [IFDEF] no-hybrid drop [THEN] sockaddr1 alen getsockname ?ior
    sockaddr1 port be-uw@ ;

: sock[ ( -- )  query-sock ?EXIT
    ipv4( ipv6( new-udp-socket46 )else( new-udp-socket ) )else( new-udp-socket6 )
    to query-sock ;
: ]sock ( -- )  query-sock 0= ?EXIT
    query-sock closesocket 0 to query-sock ?ior ;

: 'sock ( xt -- )  sock[ catch ]sock throw ;

: fake-ip4? ( addr -- flag ) sin6_addr $C fake-ip4 over str= ;
: ?fake-ip4 ( -- addr u )
    sockaddr1 sin6_addr dup $C fake-ip4 over str= IF  12 + 4  ELSE  $10   THEN ;

29  Constant ESPIPE

: unavail? ( n -- flag )
    0< IF
	errno >r
	[IFDEF] EPFNOSUPPORT  r@ EPFNOSUPPORT  = [ELSE] 0 [THEN]
	[IFDEF] EAFNOSUPPORT  r@ EAFNOSUPPORT  = or [THEN]
	[IFDEF] EADDRNOTAVAIL r@ EADDRNOTAVAIL = or [THEN]
	[IFDEF] ENETUNREACH   r> ENETUNREACH   = or [ELSE] rdrop [THEN]
    ELSE
	false
    THEN ;

[IFDEF] no-hybrid
    : sock4[ ( -- )  query-sock ?EXIT
	new-udp-socket to query-sock ;
    : ]sock4 ( -- )  query-sock 0= ?EXIT
	query-sock closesocket 0 to query-sock ?ior ;

    : 'sock4 ( xt -- ) sock4[ catch ]sock4 throw ;

    : check-ip4 ( ip4addr -- my-ip4addr 4 ) ipv4(
	[: sockaddr_in4 alen !  53 sockaddr port be-w!
	  sockaddr sin_addr be-l! query-sock
	  sockaddr sock-rest4 connect
	  dup unavail?  IF  drop ip6::0 4  EXIT  THEN  ?ior
	  query-sock sockaddr1 alen getsockname
	  dup unavail?  IF  drop ip6::0 4  EXIT  THEN  ?ior
	  sockaddr1 family w@ AF_INET6 =
	  IF  ?fake-ip4  ELSE  sockaddr1 sin_addr 4  THEN
	;] 'sock4 )else( 0 ) ;
[ELSE]
    : check-ip4 ( ip4addr -- my-ip4addr 4 ) ipv4(
	[:  ipv6( sockaddr_in6 )else( sockaddr_in4 ) alen !
	    53 sockaddr port be-w!
	    sockaddr ipv4! query-sock
	    sockaddr ipv6( sock-rest )else( sock-rest4 ) connect
	    dup unavail?  IF  drop ip6::0 4  EXIT  THEN  ?ior
	    query-sock sockaddr1 alen getsockname
	    dup unavail?  IF  drop ip6::0 4  EXIT  THEN  ?ior
	    sockaddr1 family w@ AF_INET6 =
	    IF  ?fake-ip4  ELSE  sockaddr1 sin_addr 4  THEN
	;] 'sock )else( 0 ) ;
[THEN]

$25DDC249 Constant dummy-ipv4 \ this is my net2o ipv4 address
Create dummy-ipv6 \ this is googles ipv6 address
$2a c, $00 c, $14 c, $50 c, $40 c, $01 c, $08 c, $15 c,
$0000 w, $0000 w, $0000 w, $00 c, $01 c,
Create local-ipv6
$FD c, $00 c, $0000 w, $0000 w, $0000 w, $0000 w, $0000 w, $0000 w, $00 c, $01 c,

0 Value my-port#

: ip6! ( addr1 addr2 -- ) $10 move ;
: ip6? ( addr -- flag )  $10 ip6::0 over str= 0= ;

: check-ip6 ( dummy -- ip6addr u ) ipv6(
    \G return IPv6 address - if length is 0, not reachable with IPv6
    [:  sockaddr_in6 alen !  53 sockaddr port be-w!
	sockaddr sin6_addr ip6!
	query-sock sockaddr sock-rest connect
	dup unavail?  IF  drop ip6::0 $10  EXIT  THEN  ?ior
	query-sock sockaddr1 alen getsockname
	dup unavail?  IF  drop ip6::0 $10  EXIT  THEN  ?ior
	?fake-ip4
    ;] 'sock )else( 0 ) ;

: check-ip64 ( dummy -- ipaddr u ) ipv4(
    >r r@ check-ip6 dup IF  rdrop  EXIT  THEN
    2drop r> $10 + be-ul@ check-ip4 )else( check-ip6 ) ;

: sock-connect? ( addr u -- flag ) query-sock -rot connect 0= ;

[IFDEF] no-hybrid
    : fake6>ip4 ( addr u -- addr u' )
	drop >r
	AF_INET r@ family w!
	r@ sin6_addr $C + l@ r@ sin_addr l!
	r> sockaddr_in4 ;

    : try-ip ( addr u -- flag )
	ipv6(
	over fake-ip4? IF
	    fake6>ip4
	    ['] sock-connect? 'sock4
	ELSE
	    ['] sock-connect? 'sock
	THEN )else( ['] sock-connect? 'sock4 ) ;
[ELSE]
    : try-ip ( addr u -- flag )
	['] sock-connect? 'sock ;
[THEN]

: global-ip4 ( -- ip4addr u )  dummy-ipv4 check-ip4 ;
: global-ip6 ( -- ip6addr u )  dummy-ipv6 check-ip6 ;
: local-ip6 ( -- ip6addr u )   local-ipv6 check-ip6
    IF  c@ $FD =  ELSE  drop false  THEN ;

\ no-hybrid stuff

[IFDEF] no-hybrid
    0 warnings !@
    : sendto { sock1 sock2 pack u1 flag addr u2 -- size }
	addr family w@ AF_INET6 =
	IF  addr fake-ip4?
	    IF
		sock2 pack u1 flag addr u2 fake6>ip4 sendto
	    ELSE
		sock1 pack u1 flag addr u2 sendto
	    THEN
	ELSE
	    sock2 pack u1 flag addr u2 sendto
	THEN ;
    warnings !
[THEN]

Variable myname

\ new address handling is in addr.fs, loaded later

Forward !my-addr ( -- )

\ this looks ok

: && ( flag -- ) ]] dup 0= ?EXIT drop [[ ; immediate compile-only
: &&' ( addr u addr' u' flag -- addr u false / addr u addr' u' )
    ]] 0= IF 2drop false EXIT THEN [[ ; immediate compile-only

: str=?0 ( addr1 u1 addr2 u2 -- flag )
    2dup ip6::0 over str= >r
    2over ip6::0 over str= >r str= r> r> or or ;

: str>merge ( addr1 u1 addr2 u2 -- )
    2dup ip6::0 over str= IF  smove  ELSE  2drop 2drop  THEN ;

\ insert address for punching

: !temp-addr ( addr u -- ) dup 0<> ind-addr !
    temp-addr dup $10 erase  $10 smove ;

: check-addr1 ( -- addr u flag )
    sockaddr1 ipv6( sock-rest )else( sock-rest4 ) 2dup try-ip ;

: ping-addr1 ( -- )
    check-addr1 0= IF  nat( ticks .ticks ." don't ping: " 2dup .address cr )
	2drop  EXIT  THEN
    nat( ticks .ticks ."  ping: " 2dup .address cr )
    2>r net2o-sock
    [: 64#0 { 64^ x } '>' emit code-map .mapc:dest-vaddr x le-64!
	x 8 type punch# $10 type ;] $tmp 2dup dump
    0 2r> sendto drop ;

: pathc+ ( addr u -- addr' u' )
    BEGIN  dup  WHILE  over c@ $80 < >r 1 /string r>  UNTIL  THEN ;

: .addr-path ( addr -- )
    dup be@ routes# #.key dup 0= IF  drop $10 xtype  ELSE
	$@ .address
	$10 pathc+ 0 -skip dup IF  '|' emit  THEN xtype  THEN ;

\ Create udp socket

4242 Value net2o-port \ fix server port

Variable net2o-host "net2o.de" net2o-host $!

: net2o-socket ( port -- )
    BEGIN  dup
	  ipv6( ipv4( [IFDEF] no-hybrid
	  ['] create-udp-server6 [ELSE] ['] create-udp-server46 [THEN]
	  )else( ['] create-udp-server6 )
	  )else( ['] create-udp-server )
	catch WHILE  drop 1+  REPEAT
    [IFDEF] no-hybrid 0 [THEN] to net2o-sock
    ?dup-0=-IF  my-port  THEN to my-port#
    [IFDEF] no-hybrid
	ipv4( net2o-sock drop my-port# create-udp-server to net2o-sock )
    [THEN]
    !my-addr ;

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]
