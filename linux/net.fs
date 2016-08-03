\ Linux specific network stuff

\ Copyright (C) 2016   Bernd Paysan

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

$200 Constant netlink-size#
0 Value netlink-sock
sockaddr_nl buffer: netlink-addr
netlink-size# buffer: netlink-buffer

AF_NETLINK netlink-addr nl_family w!
0          netlink-addr nl_pad w!
$00d8607f5 netlink-addr nl_groups l!

: prep-netlink ( -- )
    epiper @ fileno POLLIN  pollfds fds!+ >r
    netlink-sock POLLIN  r> fds!+
    pollfds - pollfd / to pollfd# ;

: get-netlink ( -- )
    PF_NETLINK SOCK_DGRAM NETLINK_ROUTE socket dup ?ior to netlink-sock
    getpid     [ netlink-addr nl_pid ]L l!
    netlink-sock netlink-addr sockaddr_nl bind ?ior
    prep-netlink ;

: netlink? ( -- flag )
    pollfds pollfd# >poll drop read-event
    pollfds [ pollfd revents ]L + w@ POLLIN and ;

: wait-for-netlink ( -- )
    BEGIN  netlink?  UNTIL ;

: read-netlink ( -- addr u )
    netlink-sock netlink-buffer netlink-size# MSG_DONTWAIT recv dup ?ior-again
    netlink-buffer netlink-buffer l@ rot umin ;

: read-netlink? ( -- addr u )
    poll-timeout# 0 ptimeout 2!  wait-for-netlink
    read-netlink ;

: address? ( addr u -- flag )
    0= IF  drop false  EXIT  THEN
    nlmsg_type w@ RTM_NEWLINK [ RTM_DELADDR 1+ ]L within ;

\ debugging stuff to see what kind of things are going on

: .rtaddr4 ( addr -- ) $C + 4 .ip4a 2drop ;
: .rtaddr6 ( addr -- ) $C + $10 .ip6a 2drop ;
: .ifam-flags ( n -- )
    ifa-f$ bounds DO
	dup 1 and IF  I c@ emit  THEN  2/
    LOOP  drop ;
: .ifam-addr ( addr -- )
    case  dup ifam_family c@
	AF_INET  of  .rtaddr4  endof
	AF_INET6 of  .rtaddr6  endof
	nip endcase ;
: .rtmsg ( addr -- )
    case nlmsg_type w@
	RTM_NEWADDR of ." add " endof
	RTM_DELADDR of ." del " endof
    endcase ;
: .rtaddr ( addr u -- ) drop
    dup .rtmsg  nlmsghdr +
    dup ifam_index l@ 0 .r ." : "
    dup ifam_flags c@ .ifam-flags .ifam-addr
    cr ;
: netlink-test ( -- )
    netlink-sock 0= IF  get-netlink  THEN
    BEGIN  key? 0= WHILE
	    read-netlink
	    2dup address? IF .rtaddr 20 ms
		global-ip4 .ip4a 2drop
		global-ip6 .ip6a 2drop cr
	    ELSE 2drop THEN
    REPEAT ;

\ renat handshale

0 Value netlink-task
Variable netlink-done?   netlink-done? on
Variable netlink-again?  netlink-again? off

event: ->netlink ( -- )
    netlink-again? @ IF
	 netlink-done? off netlink-again? off dht-beacon
    ELSE  netlink-done? on  THEN ;
: renat-complete ( -- )
    <event ->netlink netlink-task event> ;

\ netlink watchdog

2 constant netlink-wait#

: check-preferred? ( -- flag )
    0 my-addr[] $[] @ >o
    global-ip6 2dup str0? { v6z } host-ipv6 $10 str= >r
    global-ip4 2dup str0? { v4z } host-ipv4   4 str= r> and 0=
    v6z v4z and 0= to connected?
    o>  connected? and ;

: new-preferred? ( -- flag )
    netlink-wait# ptimeout ! \ 3s wait in total
    BEGIN  netlink? WHILE  read-netlink
	nat( 2dup address? IF  2dup .rtaddr THEN )  2drop
    REPEAT
    check-preferred? ;
: wait-for-address ( -- )
    BEGIN  read-netlink?
	nat( 2dup address? IF  2dup .rtaddr THEN )
    address? check-preferred? or  UNTIL ;
: netlink-loop ( -- )
    netlink-sock 0= IF  get-netlink  THEN
    BEGIN
	wait-for-address
	new-preferred? IF
	    nat( ." new preferred IP: " )
	    netlink-done? @ IF
		nat( ." dht-beacon" cr )
		netlink-done? off netlink-again? off dht-beacon
	    ELSE
		nat( ." netlink-again" cr ) netlink-again? on
	    THEN
	THEN
    AGAIN ;
: create-netlink-task ( -- )
    ['] netlink-loop 1 net2o-task to netlink-task ;

:noname defers init-rest
    [IFUNDEF] mslinux create-netlink-task [THEN] ; is init-rest

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