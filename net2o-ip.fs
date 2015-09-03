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
0 Value query-sock
Variable my-ip$
Variable my-addr[] \ object based hosts
Variable my-addr$ \ string based hosts (with sigs)

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
    myhost $@ s" .site" string-suffix? IF
	myhost dup $@len 5 - 5 $del
    THEN
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

Defer .addr$

: .iperr ( addr len -- ) [: info-color attr!
	.time ." connected from: "
	new-addr( .addr$ )else( .ipaddr ) default-color attr! cr ;] $err ;

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

\ new address handling is in net2o-addr.fs, loaded later

Defer !my-addr

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

\ insert address for punching

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
