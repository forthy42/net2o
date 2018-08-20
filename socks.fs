\ net2o template for new files

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

\ generic hooks and user variables

Variable packetr
Variable packets
Variable packetr2 \ double received
Variable packets2 \ double send

: .packets ( -- )
    ." IP packets send/received: " packets ? ." (" packets2 ? ." dupes)/"
    packetr ? ." (" packetr2 ? ." dupes) " cr
    packets off packetr off packets2 off packetr2 off ;

UValue pollfd#  0 to pollfd#

: prep-socks ( -- )
    epiper @ fileno POLLIN  pollfds fds!+ >r
    net2o-sock [IFDEF] no-hybrid swap [THEN] POLLIN  r> fds!+
    [IFDEF] no-hybrid POLLIN swap fds!+ [THEN]
    pollfds - pollfd / to pollfd# ;

User ptimeout  cell uallot drop
#999999999 Value poll-timeout# \ 1s, don't sleep too long
poll-timeout# 0 ptimeout 2!

User socktimeout cell uallot drop

: sock-timeout! ( socket -- )  fileno
    socktimeout 2@
    ptimeout 2@ >r #1000 / r> 2dup socktimeout 2! d<> IF
	SOL_SOCKET SO_RCVTIMEO socktimeout 2 cells setsockopt THEN
    drop ;

0             Constant do-block
MSG_DONTWAIT  Constant don't-block

$00000000 Value rec-droprate#

: ?drop-inc ( addr u -- addr u / 0 0 )
    rec-droprate# IF  rng32 rec-droprate# u< IF
	    resend( ." dropping incoming packet" cr )
	    2drop #0.  THEN  THEN ;

: read-a-packet ( blockage -- addr u / 0 0 )
    >r sockaddr_in alen !
    net2o-sock [IFDEF] no-hybrid drop [THEN]
    inbuf maxpacket r> sockaddr< alen recvfrom
    dup 0< IF
	errno dup EAGAIN =  IF  2drop #0. EXIT  THEN
	#512 + negate throw  THEN
    inbuf swap  1 packetr +!  ?drop-inc
    recvfrom( ." received from: " sockaddr< alen @ .address space dup . cr )
;

[IFDEF] no-hybrid
    : read-a-packet4 ( blockage -- addr u / 0 0 )
	>r sockaddr_in alen !
	net2o-sock nip
	inbuf maxpacket r> sockaddr< alen recvfrom
	dup 0< IF
	    errno dup EAGAIN =  IF  2drop #0. EXIT  THEN
	THEN
	inbuf swap  1 packetr +!  ?drop-inc
	recvfrom( ." received from: " sockaddr< alen @ .address space dup . cr )
    ;
[THEN]

$00000000 Value droprate#

: %droprate ( -- )
    ?peekarg 0= IF  EXIT  THEN
    + 1- c@ '%' <> ?EXIT
    ?nextarg drop prefix-number IF
	4 set-precision
	1e fmin -1e fmax $FFFFFFFF fm* f>d
	0< IF  negate to rec-droprate#
	    [: ." Set rec drop rate to "
	      rec-droprate# s>f 42949672.96e f/ f. ." %" cr ;] do-debug
	ELSE
	    to droprate#
	    [: ." Set drop rate to "
	      droprate# s>f 42949672.96e f/ f. ." %" cr ;] do-debug
	THEN
    THEN ;

: send-a-packet ( addr u -- n ) +calc
    droprate# IF  rng32 droprate# u< IF
	    resend( ." dropping packet" cr )
	    1 packets +! 2drop 0  EXIT  THEN  THEN
    2>r net2o-sock 2r> 0 sockaddr> alen @ sendto +send 1 packets +!
    sendto( ." send to: " sockaddr> alen @ .address space dup . cr ) ;

\ clients routing table

: init-route ( -- )  s" " routes# hash@ $! ; \ field 0 is me, myself

: ipv4>ipv6 ( addr u dest -- addr' u' )
    >r drop
    dup port be-uw@ swap sin_addr be-ul@
    r@ ipv4! r@ port be-w! r> sock-rest ;
: ?>ipv6 ( addr u -- addr' u' )
    over family w@ AF_INET = IF  sockaddr> ipv4>ipv6  THEN ;
: ?<ipv6 ( addr u -- addr' u' )
    over family w@ AF_INET = IF  sockaddr< ipv4>ipv6  THEN ;
: info@ ( info -- addr u )
    dup ai_addr @ swap ai_addrlen l@ ;
: info>string ( info -- addr u )
    info@ ?>ipv6 ;

: -$split ( addr u char -- addr1 u1 addr2 u2 ) \ gforth-string string-split
    \G divides a string into two, with one char as separator (e.g. '?'
    \G for arguments in an HTML query)
    >r 2dup r> -scan dup >r dup IF  1-  THEN
    2swap r> /string ;
: ping ( "addr:port" -- )
    net2o-sock ">" 0
    parse-name ':' -$split s>unumber? 2drop >r
    over c@ '[' = negate /string 2dup + 1- c@ ']' = +
    r> SOCK_DGRAM >hints 0 hints ai_family l!
    get-info dup >r info@ sendto
    r> freeaddrinfo ?ior ;

0 Value lastaddr#
Variable lastn2oaddr

: insert-address ( addr u -- net2o-addr ) ?<ipv6
    address( ." Insert address " 2dup .address cr )
    lastaddr# IF  2dup lastaddr# $@ str=
	IF  2drop lastn2oaddr @ EXIT  THEN
    THEN
    2dup routes# #key dup -1 = IF
	drop s" " 2over routes# #!
	last# to lastaddr#
	routes# #key  dup lastn2oaddr !
    ELSE
	nip nip
    THEN ;

: dns>string ( addr u port hint -- info net2o-addr u )
    >r SOCK_DGRAM >hints r> hints ai_family l!
    get-info dup info>string ;

: insert-ip* ( addr u port hint -- net2o-addr )
    dns>string rot >r insert-address r> freeaddrinfo ;

: insert-ip ( addr u port -- net2o-addr )  0         insert-ip* ;
: insert-ip4 ( addr u port -- net2o-addr ) PF_INET   insert-ip* ;
: insert-ip6 ( addr u port -- net2o-addr ) PF_INET6  insert-ip* ;

: route>address ( n -- flag )
    routes# #.key dup 0= ?EXIT
    $@ sockaddr> over alen ! sockaddr_in smove true ;

\ route an incoming packet

: >rpath-len ( rpath -- rpath len )
    dup 0= IF  0  EXIT  THEN
    [IFDEF] 64bit
	dup $100000000 u< IF
	    dup $10000 u< IF
		dup $100 u< 2 +  EXIT
	    ELSE
		dup $1000000 u< 4 + EXIT
	    THEN
	ELSE
	    dup $1000000000000 u< IF
		dup $10000000000 u< 6 +  EXIT
	    ELSE
		dup $100000000000000 u< 8 +  EXIT
	    THEN
	THEN
    [ELSE]
	dup $10000 u< IF
	    dup $100 u< 2 +  EXIT
	ELSE
	    dup $1000000 u< 4 + EXIT
	THEN
    [THEN] ;
: >path-len ( path -- path len )
    dup 0= IF  0  EXIT  THEN
    [IFDEF] 64bit
	dup     $00000000FFFFFFFF and IF
	    dup $000000000000FFFF and IF
		dup $00000000000000FF and 0= 8 +  EXIT
	    ELSE
		dup $0000000000FFFFFF and 0= 6 +  EXIT
	    THEN
	ELSE
	    dup $0000FFFFFFFFFFFF and IF
		dup $000000FFFFFFFFFF and 0= 4 +  EXIT
	    ELSE
		dup $00FFFFFFFFFFFFFF and 0= 2 +  EXIT
	    THEN
	THEN
    [ELSE]
	dup     $0000FFFF and IF
	    dup $000000FF and 0= 4 +  EXIT
	ELSE
	    dup $00FFFFFF and 0= 2 +  EXIT
	THEN
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
    2dup pathc+ { addr1 u1 addr2 u2 } \ better use locals here
    addr2 addr1 u2 move
    addr1 u1 u2 /string erase ;

: get-dest ( packet -- addr )  destination dup be@ swap skip-dest ;
: route? ( packet -- flag )  destination c@  ;

: packet-route ( orig-addr addr -- flag )
    dup route?  IF
	>r r@ get-dest  route>address  IF  r@ ins-source  ELSE  drop  THEN
	rdrop false  EXIT  THEN
    2drop true ; \ local packet

: out-route ( -- )  0 outbuf packet-route drop ;

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
