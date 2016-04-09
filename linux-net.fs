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

: wait-for-netlink ( -- )
    BEGIN  pollfds pollfd# >poll drop read-event
	pollfds [ pollfd revents ]L + w@ POLLIN and  UNTIL ;

: read-netlink ( flag -- addr u ) >r
    r@ 0= IF  wait-for-netlink  THEN
    netlink-sock netlink-buffer netlink-size# r> recv dup ?ior-again
    >r netlink-buffer netlink-buffer l@ r> umin ;

: address? ( addr u -- flag )
    drop nlmsg_type w@ RTM_NEWADDR [ RTM_DELADDR 1+ ]L within ;

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
	    0 read-netlink
	    2dup address? IF .rtaddr 20 ms
		global-ip4 .ip4a 2drop
		global-ip6 .ip6a 2drop cr
	    ELSE 2drop THEN
    REPEAT ;

\ netlink watchdog

20 constant netlink-wait#
0 Value netlink-task

: new-preferred? ( -- flag )
    netlink-wait# ms
    0 my-addr[] $[] @ >o
    global-ip4 2dup str0? >r
    global-ip6 2dup str0? r> and 0= to connected?
    host-ipv6 $10 str= >r
    host-ipv4   4 str= r> and 0=
    o>  connected? and ;
: check-addresses? ( -- flag )
    false  BEGIN  MSG_DONTWAIT read-netlink dup WHILE
	    address? or  REPEAT  2drop ;
: wait-for-address ( -- )
    BEGIN  0 read-netlink address?  UNTIL ;
: netlink-loop ( -- )
    netlink-sock 0= IF  get-netlink  THEN
    BEGIN
	check-addresses? 0= IF  wait-for-address  THEN
	new-preferred? IF  dht-beacon  THEN
    AGAIN ;
: create-netlink-task ( -- )
    ['] netlink-loop 1 net2o-task to netlink-task ;

:noname defers init-rest  create-netlink-task ; is init-rest

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