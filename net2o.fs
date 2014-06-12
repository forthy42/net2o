\ net2o protocol stack

\ Copyright (C) 2010-2014   Bernd Paysan

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

: ?nextarg ( -- addr u noarg-flag )
    argc @ 1 > IF  next-arg true  ELSE  false  THEN ;

[IFUNDEF] safe/string
: safe/string ( c-addr u n -- c-addr' u' )
\G protect /string against overflows.
    dup negate >r  dup 0> IF
        /string dup r> u>= IF  + 0  THEN
    ELSE
        /string dup r> u< IF  + 1+ -1  THEN
    THEN ;
[THEN]

: or!   ( x addr -- )   >r r@ @ or   r> ! ;
: xor!  ( x addr -- )   >r r@ @ xor  r> ! ;
: and!  ( x addr -- )   >r r@ @ and  r> ! ;
: min!  ( n addr -- )   >r r@ @ min  r> ! ;
: max!  ( n addr -- )   >r r@ @ max  r> ! ;
: umin! ( n addr -- )   >r r@ @ umin r> ! ;
: umax! ( n addr -- )   >r r@ @ umax r> ! ;

: xorc! ( x c-addr -- )   >r r@ c@ xor  r> c! ;
: andc! ( x c-addr -- )   >r r@ c@ and  r> c! ;
: orc!  ( x c-addr -- )   >r r@ c@ or   r> c! ;

: max!@ ( n addr -- )   >r r@ @ max r> !@ ;
: umax!@ ( n addr -- )   >r r@ @ umax r> !@ ;

\ defined exceptions

: throwcode ( addr u -- )  exception Create ,
    [: >body @ >r ]] IF [[ r> ]] literal throw THEN [[ ;] set-compiler
  DOES> ( flag -- ) @ and throw ;

s" gap in file handles"          throwcode !!gap!!
s" invalid file id"              throwcode !!fileid!!
s" could not send"               throwcode !!send!!
s" wrong packet size"            throwcode !!size!!
s" no power of two"              throwcode !!pow2!!
s" unimplemented net2o function" throwcode !!function!!
s" too many commands"            throwcode !!commands!!
s" string does not fit"          throwcode !!stringfit!!
s" ivs must be 64 bytes"         throwcode !!ivs!!
s" net2o timed out"              throwcode !!timeout!!
s" maximum nesting reached"      throwcode !!maxnest!!
s" nesting stack empty"          throwcode !!minnest!!
s" invalid nest"                 throwcode !!nest!!
s" invalid tmpnest"              throwcode !!tmpnest!!
s" cookie recieved twice"        throwcode !!double-cookie!!
s" code destination is 0"        throwcode !!no-dest!!
s" no IP addr"                   throwcode !!no-addr!!
s" absolute path not allowed!"   throwcode !!abs-path!!
s" invalid packet destination"   throwcode !!inv-dest!!
s" key+pubkey must be 32 bytes"  throwcode !!keysize!!
s" unknown key"                  throwcode !!unknown-key!!
s" wrong key"                    throwcode !!wrong-key!!
s" no key file"                  throwcode !!nokey!!
s" invalid Ed25519 key"          throwcode !!no-ed-key!!
s" no temporary key"             throwcode !!no-tmpkey!!
s" String stack full"            throwcode !!string-full!!
s" String stack empty"           throwcode !!string-empty!!
s" Unknown crypto function"      throwcode !!unknown-crypt!!
s" Wrong revocation secret"      throwcode !!not-my-revsk!!
s" krypto mem request too big"   throwcode !!kr-size!!
s" secret storage size wrong"    throwcode !!sec-size!!

\ required tools

\ require smartdots.fs
require mini-oof2.fs
require user-object.fs
require 64bit.fs
require unix/socket.fs
require unix/mmap.fs
require unix/pthread.fs
require unix/filestat.fs
require kregion.fs
require string.fs
require struct0x.fs
require debugging.fs
require libkeccak.fs
keccak-o crypto-o !
\ require wurstkessel.fs
\ wurstkessel-o crypto-o !
require rng.fs
require ed25519-donna.fs
require hash-table.fs

\ user values

UValue inbuf    ( -- addr )
UValue outbuf   ( -- addr )
UValue cmd0buf  ( -- addr )
UValue init0buf ( -- addr )
UValue sockaddr ( -- addr )
UValue sockaddr1 ( -- addr ) \ temporary buffer
UValue aligned$
UValue statbuf

[IFDEF] 64bit
    ' min! Alias 64min!
    ' max! Alias 64max!
    ' !@ Alias 64!@
[ELSE]
    : 64!@ ( value addr -- old-value )   >r r@ 64@ 64swap r> 64! ;
    : 64min! ( d addr -- )  >r r@ 64@ dmin r> 64! ;
    : 64max! ( d addr -- )  >r r@ 64@ dmax r> 64! ;
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

[IFDEF] 64bit
    : p@+ ( addr -- u64 addr' )  >r 0
	BEGIN  7 lshift r@ c@ $7F and or r@ c@ $80 and  WHILE
		r> 1+ >r  REPEAT  r> 1+ ;
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
    : p@+ ( addr -- u64 addr' )  >r 0.
	BEGIN  7 64lshift r@ c@ $7F and 0 64or r@ c@ $80 and  WHILE
		r> 1+ >r  REPEAT  r> 1+ ;
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

: w, ( w -- )  here w! 2 allot ;

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

\ IP address stuff

0 Value net2o-sock
0 Value query-sock
Variable my-ip$

Create fake-ip4 $0000 w, $0000 w, $0000 w, $0000 w, $0000 w, $FFFF w,
\ prefix for IPv4 addresses encoded as IPv6

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
    10 myprio ! ;

default-host

: .myname ( -- )
    myprio @ IF  '0' emit myprio @ emit  THEN
    myhost $@len IF  myhost $@ dup '@' + emit type  THEN ;

Create ip6::0 here 16 dup allot erase
: .ip6::0 ( -- )  ip6::0 $10 type ;
: .ip4::0 ( -- )  ip6::0 4 type ;

: .sockaddr { addr alen -- }
    \ convert socket into net2o address token
    case addr family w@
	AF_INET of
	    '2' emit  .ip6::0  addr sin_addr 4 type addr port 2 type
	endof
	AF_INET6 of
	    addr sin6_addr 12 fake-ip4 over str= IF
		'2' emit  .ip6::0  addr sin6_addr 12 + 4 type
	    ELSE
		'2' emit addr sin6_addr $10 type  .ip4::0
	    THEN  addr sin6_port 2 type
	endof
    endcase ;

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

: .iperr ( addr len -- ) [: .time ." connected from: " .ipaddr cr ;] $err ;

: ipv4! ( ipv4 sockaddr -- ) >r
    r@ sin6_addr 12 + be-l!
    $FFFF r@ sin6_addr 8 + be-l!
    0     r@ sin6_addr 4 + l!
    0     r> sin6_addr l! ;

: sock-rest ( sockaddr -- addr u ) >r
    AF_INET6 r@ family w!
    0        r@ sin6_flowinfo l!
    0        r@ sin6_scope_id l!
    r> sockaddr_in6 %size ;

: my-port ( -- port )
    sockaddr_in6 %size alen !
    net2o-sock fileno sockaddr1 alen getsockname ?ior
    sockaddr1 port be-uw@ ;

: sock[ ( -- )  query-sock ?EXIT
    new-udp-socket46 to query-sock ;
: ]sock ( -- )  query-sock 0= ?EXIT
    query-sock closesocket 0 to query-sock ?ior ;

: 'sock ( xt -- )  sock[ catch ]sock throw ;

: ?fake-ip4 ( -- addr u )
    sockaddr1 sin6_addr dup $C fake-ip4 over
    str= IF  12 + 4  ELSE  $10   THEN ;

: check-ip4 ( ip4addr -- my-ip4addr 4 ) noipv4( 0 EXIT )
    [:  sockaddr_in6 %size alen !
	sockaddr ipv4! query-sock sockaddr sock-rest connect ?ior
	query-sock sockaddr1 alen getsockname dup 0< errno 101 = and
	IF  drop s" " \ 0 is an invalid result
	ELSE  ?ior
	    sockaddr1 family w@ AF_INET6 =
	    IF  ?fake-ip4  ELSE  sin_addr 4  THEN
	THEN ;] 'sock ;

$25DDC249 Constant dummy-ipv4 \ this is my net2o ipv4 address
Create dummy-ipv6 \ this is my net2o ipv6 address
$2A c, $03 c, $40 c, $00 c, $00 c, $02 c, $01 c, $88 c,
$0000 w, $0000 w, $0000 w, $00 c, $01 c,
Create local-ipv6
$FD c, $00 c, $0000 w, $0000 w, $0000 w, $0000 w, $0000 w, $0000 w, $0100 w,

0 Value my-port#

: check-ip6 ( dummy -- ip6addr u ) noipv6( 0 EXIT )
    \G return IPv6 address - if length is 0, not reachable with IPv6
    [:  sockaddr_in6 %size alen !
	sockaddr sin6_addr $10 move
	query-sock sockaddr sock-rest connect dup 0< errno 101 = and
	IF  drop s" "
	ELSE  ?ior
	    query-sock sockaddr1 alen getsockname ?ior
	    ?fake-ip4
	THEN ;] 'sock ;

: check-ip64 ( dummy -- ipaddr u ) noipv4( check-ip6 EXIT )
    >r r@ check-ip6 dup IF  rdrop  EXIT  THEN
    2drop r> $10 + be-ul@ check-ip4 ;

: try-ip ( addr u -- flag )
    [: query-sock -rot connect 0= ;] 'sock ;

: global-ip4 ( -- ip4addr u )  dummy-ipv4 check-ip4 ;
: global-ip6 ( -- ip6addr u )  dummy-ipv6 check-ip6 ;
: local-ip6 ( -- ip6addr u )   local-ipv6 check-ip6 over c@ $FD = and ;

\ insert into sorted string array

Variable ins$0 \ just a null pointer

: $ins[] ( addr u $array -- )
    \G insert O(log(n)) into pre-sorted array
    { $a } 0 $a $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup $# $a $[]@ compare dup 0= IF
		drop $# $a $[]! EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    ins$0 cell $a r@ cells $ins r> $a $[]! ;
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
    [:  .myname '2' emit
	dup 4 = IF ip6::0 $10 type ELSE dup $10 = IF type ip6::0 4 THEN THEN type
	my-port# 8 rshift emit my-port# $FF and emit ;] $tmp
    my-ip$ $ins[] ;

Variable $tmp2

: !my-ips ( -- )  $tmp2 $off
    global-ip6 tuck [: type global-ip4 type ;] $tmp2 $exec
    $tmp2 $@ +my-ip
    0= IF  local-ip6  +my-ip THEN ;

\ this looks ok

: && ( flag -- ) ]] dup 0= ?EXIT drop [[ ; immediate compile-only
: &&' ( addr u addr' u' flag -- addr u false / addr u addr' u' )
    ]] 0= IF 2drop false EXIT THEN [[ ; immediate compile-only

: str=?0 ( addr1 u1 addr2 u2 -- )
    2dup ip6::0 over str= >r
    2over ip6::0 over str= >r str= r> r> or or ;

: my-ip= skip-symname 2swap skip-symname { addr1 u1 addr2 u2 -- flag }
    addr1 c@ '2' = addr2 c@ '2' = and &&
    addr1 u1 $15 safe/string addr2 u2 $15 safe/string str= &&
    addr1 1+ $10 addr2 1+ over str=?0 &&
    addr1 $11 + 4 addr2 $11 + over str=?0 ;

: str>merge ( addr1 u1 addr2 u2 -- )
    2dup ip6::0 over str= IF  rot umin move  ELSE  2drop 2drop  THEN ;

: my-ip>merge  skip-symname 2swap skip-symname 2swap
    { addr1 u1 addr2 u2 -- }
    addr1 1+ $10 addr2 1+ over  str>merge
    addr1 $11 + 4 addr2 $11 + over str>merge ;

: my-ip? ( addr u -- addr u flag )
    0 my-ip$ [: rot >r 2over my-ip= r> or ;] $[]map ;
: my-ip-merge ( addr u -- addr u flag )
    0 my-ip$ [: rot >r 2over 2over my-ip= IF
	    2over 2swap my-ip>merge rdrop true  ELSE  r>  THEN ;] $[]map ;

\ Create udp socket

4242 Value net2o-port

Variable net2o-host "net2o.de" net2o-host $!

: net2o-socket ( port -- ) dup >r
    create-udp-server46 fd>file to net2o-sock
    r> ?dup-0=-IF  my-port  THEN to my-port#
    !my-ips ;

$2A Constant overhead \ constant overhead
$4 Value max-size^2 \ 1k, don't fragment by default
$40 Constant min-size
$400000 Value max-data#
$10000 Value max-code#
1 Value buffers#
min-size max-size^2 lshift Value maxdata ( -- n )
maxdata overhead + Value maxpacket
maxpacket $F + -$10 and Value maxpacket-aligned
max-size^2 6 + Value chunk-p2
$10 Constant mykey-salt#

begin-structure timestamp
64field: ts-ticks
end-structure

begin-structure reply
field: reply-len
field: reply-offset
64field: reply-dest
end-structure

m: addr>bits ( addr -- bits )
    chunk-p2 rshift ;
m: addr>bytes ( addr -- bytes )
    chunk-p2 3 + rshift ;
m: bytes>addr ( bytes addr -- )
    chunk-p2 3 + lshift ;
m: bits>bytes ( bits -- bytes )
    1- 2/ 2/ 2/ 1+ ;
m: addr>ts ( addr -- ts-offset )
    addr>bits timestamp * ;
m: addr>replies ( addr -- replies )
    addr>bits reply * ;
m: addr>keys ( addr -- keys )
    max-size^2 rshift [ min-size negate ]L and ;

sema cmd0lock

\ generic hooks and user variables

User ind-addr
User reqmask
UDefer other
UValue pollfd#  2 to pollfd#
User pollfds
pollfds pollfd %size pollfd# * dup cell- uallot drop erase

Defer init-reply

: -other        ['] noop is other ;
-other

: fds!+ ( fileno flag addr -- addr' )
    >r r@ events w!  r@ fd l!  r> pollfd %size + ; 

: prep-socks ( -- )  pollfds >r
    net2o-sock  fileno POLLIN  r> fds!+ >r
    epiper @    fileno POLLIN  r> fds!+ drop 2 to pollfd# ;

\ the policy on allocation and freeing is that both freshly allocated
\ and to-be-freed memory is erased.  This makes sure that no unwanted
\ data will be lurking in that memory, waiting to be leaked out

: alloz ( size -- addr )
    dup >r allocate throw dup r> erase ;
: freez ( addr size -- )
    \g erase and then free - for secret stuff
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

: init-statbuf ( -- )
    file-stat alloz to statbuf ;
: free-statbuf ( -- )
    statbuf file-stat freez  0 to statbuf ;

: alloc-io ( -- ) \ allocate IO and reset generic user variables
    -other  ind-addr off  reqmask off
    alloc-buf to inbuf  alloc-buf to outbuf
    maxdata allocate throw to cmd0buf
    maxdata 2/ mykey-salt# + $10 + allocate throw to init0buf
    sockaddr_in6 %size alloz to sockaddr
    sockaddr_in6 %size alloz to sockaddr1
    $400 allocate throw to aligned$
    init-statbuf
    init-ed25519 c:init ;

: free-io ( -- )
    free-ed25519 c:free
    free-statbuf
    aligned$ $400 freez
    sockaddr  sockaddr_in6 %size  freez
    sockaddr1 sockaddr_in6 %size  freez
    init0buf maxdata 2/ mykey-salt# + $10 +  freez
    cmd0buf maxdata   freez
    inbuf  free-buf
    outbuf free-buf ;

alloc-io

: net2o-pass ( params xt n task )  pass
    b-out init-reply prep-socks alloc-io catch free-io
    ?dup-IF  DoError  THEN ;
: net2o-task ( params xt n -- task )
    stacksize4 NewTask4 dup >r net2o-pass r> ;

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

MSG_WAITALL   Constant do-block
MSG_DONTWAIT  Constant don't-block

: read-a-packet ( blockage -- addr u / 0 0 )
    >r sockaddr_in6 %size alen !
    net2o-sock fileno inbuf maxpacket r> sockaddr alen recvfrom
    dup 0< IF
	errno dup 11 = IF  2drop 0. EXIT  THEN
	512 + negate throw  THEN
    inbuf swap  1 packetr +! ;

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
    net2o-sock  1 packets +!
    fileno -rot 0 sockaddr alen @ sendto +send ;

\ clients routing table

Variable routes

: init-route ( -- )  s" " routes hash@ $! ; \ field 0 is me, myself

: info>string ( addr -- addr u )
    dup ai_addr @ swap ai_addrlen l@
    over w@ AF_INET = IF
	drop >r
	r@ port be-uw@ sockaddr port be-w!
	r> sin_addr be-ul@ sockaddr ipv4!
	sockaddr sock-rest
    THEN ;

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

: insert-ip ( addr u port -- net2o-addr )  PF_UNSPEC insert-ip* ;
: insert-ip4 ( addr u port -- net2o-addr ) AF_INET   insert-ip* ;
: insert-ip6 ( addr u port -- net2o-addr ) AF_INET6  insert-ip* ;

: address>route ( -- n/-1 )
    sockaddr alen @ insert-address ;
: route>address ( n -- ) dup >r
    routes #.key dup 0= IF  ." no address: " r> hex. cr drop  EXIT  THEN
    $@ sockaddr swap dup alen ! move  rdrop ;

\ route an incoming packet

User return-addr $10 cell- uallot drop
User temp-addr   $10 cell- uallot drop

\ these are all stubs for now

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
	>r r@ get-dest  route>address  r> ins-source  false  EXIT  THEN
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

: >ret-addr ( -- )
    inbuf destination return-addr reverse$16 ;
: >dest-addr ( -- )
    inbuf addr 64@  inbuf body-size 1- invert n>64 64and dest-addr 64! ;

current-o

object class
    64field: dest-vaddr
    field: dest-size
    field: dest-raddr
    field: dest-job
    field: dest-ivs
    field: dest-ivsgen
    field: dest-ivslastgen
    field: dest-timestamps
    field: dest-replies
    field: dest-cookies
    field: dest-round \ going to be obsoleted
    \                   sender:                receiver:
    field: dest-top   \ -/-                    sender read up to here
    field: dest-head  \ read up to here        received some
    field: dest-tail  \ send from here         received all
    field: dest-back  \ flushed on destination flushed
    field: dest-end   \ -/-                    true if last chunk
    field: do-slurp
    method free-data
    method regen-ivs
    method handle
    method rewind-timestamps
    method rewind-timestamps-partial
end-class code-class
' drop code-class to regen-ivs
' noop code-class to rewind-timestamps
' drop code-class to rewind-timestamps-partial

code-class class end-class data-class

code-class class
    field: data-ackbits
    field: data-ackbits-buf
    field: data-ack#     \ fully acked bursts
    field: data-reack#   \ last polarity change was here
    field: ack-bit#      \ actual ack bit
    field: ack-advance?  \ ack is advancing state
end-class rcode-class

rcode-class class end-class rdata-class

\ job context structure

object class
end-class cmd-class \ command interpreter

cmd-class class
end-class setup-class \ setup connections

setup-class class
    field: context#
    field: wait-task
    field: resend0
    field: punch-load
    $10 +field return-address \ used as return address
    $10 +field r0-address \ used for resending 0
    64field: recv-tick
    64field: recv-addr
    field: recv-flag
    field: file-state
    field: read-file#
    field: write-file#
    field: residualread
    field: residualwrite
    field: blocksize
    field: blockalign
    field: crypto-key
    field: pubkey
    field: timeout-xt \ callback for timeout
    field: setip-xt   \ callback for set-ip
    field: ack-xt
    field: codebuf#
    field: request#
    field: filereq#
    1 pthread-mutexes +field code-lock
    1 pthread-mutexes +field filestate-lock
    
    field: code-map
    field: code-rmap
    field: data-map
    field: data-rmap

    field: data-resend
    field: data-b2b
    
    cfield: ack-state
    cfield: ack-resend~
    cfield: ack-resend#
    cfield: is-server
    field: ack-receive
    
    field: req-codesize
    field: req-datasize
    \ flow control, sender part
    64field: min-slack
    64field: max-slack
    64field: ns/burst
    64field: last-ns/burst
    64field: extra-ns
    field: window-size \ packets in flight
    64field: bandwidth-tick \ ns
    64field: next-tick \ ns
    64field: next-timeout \ ns
    field: timeouts
    64field: rtdelay \ ns
    64field: lastack \ ns
    field: flyburst
    field: flybursts
    64field: lastslack
    64field: lastdeltat
    64field: slackgrow
    64field: slackgrow'
    \ flow control, receiver part
    64field: burst-ticks
    64field: firstb-ticks
    64field: lastb-ticks
    64field: delta-ticks
    64field: max-dticks
    64field: last-rate
    \ experiment: track previous b2b-start
    64field: last-rtick
    64field: last-raddr
    field: acks
    field: rec-timing
    field: received
    \ cookies
    field: last-ackaddr
    \ statistics
    field: timing-stat
    64field: last-time
    64field: time-offset  \ make timestamps smaller
    KEYBYTES +field tpkc
    KEYBYTES +field tskc
    field: dest-pubkey  \ if not 0, connect only to this key
end-class context-class

begin-structure timestats
sffield: ts-delta
sffield: ts-slack
sffield: ts-reqrate
sffield: ts-rate
sffield: ts-grow
end-structure

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

: check-dest ( -- addr map o:job / f )
    \G return false if invalid destination
    \G return 1 if code, -1 if data, plus destination address
    dest-index 2 cells bounds ?DO
	I @ IF
	    dest-addr 64@ I @ >o dest-vaddr 64@ 64- 64>n dup
	    dest-size @ u<
	    IF
		dup addr>bits ack-bit# !
		dest-raddr @ swap dup >data-head ack-advance? ! +
		o dest-job @ o> >o rdrop
		UNLOOP  EXIT  THEN
	    drop o>
	THEN
    cell +LOOP
    false ;

\ context debugging

: .o ( -- ) context# ? ;
: o? ( -- ) ]] o 0= ?EXIT [[ ; immediate

\ Destination mapping contains
\ addr u - range of virtal addresses
\ addr' - real start address
\ context - for exec regions, this is the job context

User >code-flag

: alloc-data ( addr u -- u flag )
    dup >r dest-size ! dest-vaddr 64! r>
    dup alloc+guard dest-raddr !
    c:key# alloz dest-ivsgen !
    >code-flag @
    IF
	dup addr>replies  alloz dest-replies !
	3 dest-ivslastgen !
    ELSE
	dup addr>ts       alloz dest-timestamps !
    THEN ;

: map-data ( addr u -- o )
    o >code-flag @ IF rcode-class ELSE rdata-class THEN new >o dest-job !
    alloc-data
    >code-flag @ 0= IF
	dup addr>ts alloz dest-cookies !
	dup addr>bytes allocate-bits data-ackbits !
	s" " data-ackbits-buf $!
    THEN
    drop
    o o> ;

: map-source ( addr u addrx -- o )
    o >code-flag @ IF code-class ELSE data-class THEN new >o dest-job !
    alloc-data
    dup addr>ts alloz dest-cookies !
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
$10 cells Value resend-size#
#50.000.000 d>64 64Constant init-delay# \ 30ms initial timeout step

Variable init-context#

: init-flow-control ( -- )
    max-int64 64-2/ min-slack 64!
    max-int64 64-2/ 64negate max-slack 64!
    init-delay# rtdelay 64!
    flybursts# dup flybursts ! flyburst !
    ticks lastack 64! \ asking for context creation is as good as an ack
    bandwidth-init n>64 ns/burst 64!
    never               next-tick 64!
    64#0                extra-ns 64! ;

resend-size# buffer: resend-init

: no-timeout ( -- )  max-int64 next-timeout 64!  0 timeouts ! ;

: n2o:new-context ( addr -- )
    context-class new >o rdrop timeout( ." new context: " o hex. cr )
    init-context# @ context# !  1 init-context# +!
    dup return-addr be!  return-address be!
    resend-init resend-size# data-resend $!
    s" " crypto-key $!
    s" " file-state $!
    init-flow-control
    ['] no-timeout timeout-xt ! ['] .iperr setip-xt !
    -1 blocksize !
    1 blockalign !
    code-lock 0 pthread_mutex_init drop
    filestate-lock 0 pthread_mutex_init drop ;

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
    sockaddr1 sock-rest 2dup try-ip ;

: ping-addr1 ( -- )
    check-addr1 0= IF  2drop  EXIT  THEN
    nat( ." ping: " 2dup .address cr )
    2>r net2o-sock fileno "" 0 2r> sendto drop ;

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

\ create new maps

Variable mapstart $1 mapstart !

: server? ( -- flag )  is-server c@ negate ;
: server! ( -- )  1 is-server c! ;
: pow2? ( n -- n )  dup dup 1- and 0<> !!pow2!! ;

: n2o:new-map ( u -- addr )
    drop mapstart @ 1 mapstart +! reverse
    [ cell 4 = ] [IF]  0 swap  [ELSE] $FFFFFFFF00000000 and [THEN] ; 
: n2o:new-data pow2? { 64: addrs 64: addrd u -- }
    o 0= IF
	addrd >dest-map @ ?EXIT
	return-addr be@ n2o:new-context  server!  THEN
    msg( ." data map: " addrs $64. addrd $64. u hex. cr )
    >code-flag off
    addrd u data-rmap map-data-dest
    addrs u map-source  data-map ! ;
: n2o:new-code pow2? { 64: addrs 64: addrd u -- }
    o 0= IF
	addrd >dest-map @ ?EXIT
	return-addr be@ n2o:new-context  server!  THEN
    msg( ." code map: " addrs $64. addrd $64. u hex. cr )
    >code-flag on
    addrd u code-rmap map-code-dest
    addrs u map-source code-map ! ;

\ dispose connection

: free-code ( o:data -- ) o 0= ?EXIT dest-size @ >r
    dest-raddr r@   ?free+guard
    dest-ivsgen     c:key# ?free
    dest-replies    r@ addr>replies ?free
    dest-timestamps r@ addr>ts      ?free
    dest-cookies    r> addr>ts      ?free
    dispose ;
' free-code code-class to free-data
' free-code data-class to free-data

: free-rcode ( o:data --- )
    data-ackbits dest-size @ addr>bytes ?free
    data-ackbits-buf $off
    free-code ;
' free-rcode rdata-class to free-data
' free-rcode rcode-class to free-data

\ data sending around

: >blockalign ( n -- block )
    blockalign @ dup >r 1- + r> negate and ;
: 64>blockalign ( 64 -- block )
    blockalign @ dup >r 1- n>64 64+ r> negate n>64 64and ;

: /head ( u -- )
    >blockalign data-map @ >o dest-head +! o> ;
: /back ( u -- )
    >blockalign data-rmap @ >o dest-back +! o> ;
: /tail ( u -- )
    data-map @ >o dest-tail +! o> ;
: data-dest ( -- addr )
    data-map @ >o
    dest-vaddr 64@ dest-tail @ dest-size @ 1- and n>64 64+ o> ;

\ new data sending around stuff, with front+back

: fix-size ( offset1 offset2 -- addr len )
    over - >r dest-size @ 1- and r> over + dest-size @ umin over - ;
: fix-bitsize ( offset1 offset2 -- addr len )
    over - >r dest-size @ addr>bits 1- and r> over + dest-size @ umin over - ;
: raddr+ ( addr len -- addr' len ) >r dest-raddr @ + r> ;
: fix-size' ( base offset1 offset2 -- addr len )
    over - >r dest-size @ 1- and + r> ;
: ?residual ( addr len resaddr -- addr len' ) >r
    r@ @ umin dup negate r> +! ;
: head@ ( -- head )  data-map @ .dest-head @ ;
: data-head@ ( -- addr u )
    \g you can read into this, it's a block at a time (wraparound!)
    data-map @ >o
    dest-head @ dest-back @ dest-size @ + fix-size raddr+ o>
    residualread ?residual ;
: rdata-back@ ( -- addr u )
    \g you can write from this, also a block at a time
    data-rmap @ >o
    dest-back @ dest-tail @ fix-size raddr+ o>
    residualwrite ?residual ;
: data-tail@ ( -- addr u )
    \g you can send from this - as long as you stay block aligned
    data-map @ >o dest-raddr @ dest-tail @ dest-head @ fix-size' o> ;

: data-head? ( -- flag )
    data-map @ >o dest-head @ dest-back @ dest-size @ + u< o> ;
: data-tail? ( -- flag )
    data-map @ >o dest-tail @ dest-head @ u< o> ;
: rdata-back? ( -- flag )
    data-rmap @ >o dest-back @ dest-tail @ u< o> ;

\ code sending around

: code-dest ( -- addr )
    code-map @ >o dest-raddr @ dest-tail @ + o> ;
: code-vdest ( -- addr )
    code-map @ >o dest-vaddr 64@ dest-tail @ n>64 64+ o> ;
: code-reply ( -- addr )
    code-map @ >o dest-tail @ addr>replies dest-replies @ + o> ;

: tag-addr ( -- addr )
    dest-addr 64@ code-rmap @ >o dest-vaddr 64@ 64- 64>n
    addr>replies dest-replies @ + o> ;

reply buffer: dummy-reply

: reply[] ( index -- addr )
    code-map @ >o
    dup dest-size @ addr>bits u<
    IF  reply * dest-replies @ +  ELSE  dummy-reply  THEN  o> ;

: reply-index ( -- index )
    code-map @ >o dest-tail @ addr>bits o> ;

: code+ ( -- )
    code-map @ >o
    maxdata dest-tail +!
    dest-tail @ dest-size @ u>= IF  dest-tail off  THEN
\    cmd( ." set dest-tail to " dest-tail @ hex. cr )
    o> ;

\ aligned buffer to make encryption/decryption fast

: $>align ( addr u -- addr' u ) dup $400 u> ?EXIT
    tuck aligned$ swap move aligned$ swap ;
    
\ timing records

: net2o:track-timing ( -- ) \ initialize timing records
    s" " timing-stat $! ;

: )stats ]] THEN [[ ;
: stats( ]] timing-stat @ IF [[ ['] )stats assert-canary ; immediate

: net2o:timing$ ( -- addr u )
    stats( timing-stat $@  EXIT ) ." no timing stats" cr s" " ;
: net2o:/timing ( n -- )
    stats( timing-stat 0 rot $del ) ;

: .rec-timing ( addr u -- ) rec-timing $@ \ do some dumps
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
    timestats +LOOP  rec-timing $off ;

: net2o:rec-timing ( addr u -- )  rec-timing $+! ;

timestats buffer: stat-tuple

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
    64dup 64-0<=    IF  64drop 64drop  EXIT  THEN
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
    data-map @ dup 0= IF  drop 0 0  EXIT  THEN  >r
    r@ >o >offset  IF
	dest-tail @ o> over - 0 max addr>bits window-size !
	addr>ts r> .dest-timestamps @ swap
    ELSE  o> rdrop 0 0  THEN ;

: net2o:ack-addrtime ( ticks addr -- )
    >timestamp over  IF
	dup tick-init 1+ timestamp * u>
	IF  + dup >r  ts-ticks 64@
	    r@ tick-init 1+ timestamp * - ts-ticks 64@
	    64dup 64-0<= >r 64over 64-0<= r> or
	    IF  64drop 64drop  ELSE  64- lastdeltat 64!  THEN  r>
	ELSE  +  THEN
	ts-ticks 64@ timestat
    ELSE  2drop 64drop  THEN ;

: net2o:ack-b2btime ( ticks addr -- )
    >timestamp over  IF  + ts-ticks 64@ b2b-timestat
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
    64>n  stats( dup s>f stat-tuple ts-slack sf! )
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

: net2o:set-rate ( rate deltat -- )  rate-stat1
    64>r 64dup >extra-ns noens( 64drop )else( 64nip )
    64r> delta-t-grow# 64*/ 64min ( no more than 2*deltat )
    bandwidth-max n>64 64max rate-limit rate-stat2 ns/burst 64!@
    bandwidth-init n>64 64= IF \ first acknowledge
	net2o:set-flyburst
	net2o:max-flyburst
    THEN ;

\ acknowledge

sema resize-lock

$20 Value mask-bits#
: >mask0 ( addr mask -- addr' mask' )
    BEGIN  dup 1 and 0= WHILE  1 rshift >r maxdata + r>  dup 0= UNTIL  THEN ;
: net2o:resend-mask ( addr mask -- )
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
	I @ 0= IF  >mask0 I 2! UNLOOP EXIT  THEN
    2 cells +LOOP  2drop ;
: net2o:ack-resend ( flag -- )  resend-toggle# and ack-resend~ c! ;
: resend$@ ( -- addr u )
    data-resend $@  IF
	2@ 1 and IF  maxdata  ELSE  0  THEN
	swap data-map @ >o dest-raddr @ + o> swap
    ELSE  drop 0 0  THEN ;

: resend-dest ( -- addr )
    data-resend $@ drop 2@ drop data-map @ >o n>64 dest-vaddr 64@ 64+ o> ;
: /resend ( u -- )
    0 +DO
	data-resend $@ drop
	dup >r 2@ -2 and >mask0 tuck r> 2!
	0= IF  data-resend $@ 2 cells - >r dup 2 cells + swap r> move
	    0. data-resend $@ 2 cells - + 2!
	THEN
    maxdata +LOOP ;

\ resend third handshake

: push-reply ( addr u -- )  resend0 $!  return-addr r0-address $10 move ;

\ file handling

\ file states

Variable net2o-path
pad 200 get-dir net2o-path $!

object class
    64field: fs-size
    64field: fs-seek
    64field: fs-seekto
    64field: fs-limit
    64field: fs-time
    field: fs-fid
    field: fs-path
    method fs-read
    method fs-write
    method fs-open
    method fs-close
end-class fs-class

: >seek ( size 64to 64seek -- size' )
    64dup 64>d fs-fid @ reposition-file throw 64- 64>n umin ;

: fs-timestamp! ( mtime fileno -- ) >r
    [IFDEF] android  rdrop 64drop
    [ELSE]  \ ." Set time: " r@ . 64dup 64>d d. cr
	64>d 2dup statbuf ntime!
	statbuf 2 cells + ntime!
	r> statbuf futimens ?ior [THEN] ;

:noname ( addr u -- n )
    fs-limit 64@ fs-seekto 64@ >seek
    fs-fid @ read-file throw
    dup n>64 fs-seekto 64+!
; fs-class to fs-read
:noname ( addr u -- n )
    fs-seekto 64@ fs-seek 64@ >seek
    tuck fs-fid @ write-file throw
    dup n>64 fs-seek 64+!
; fs-class to fs-write
:noname ( -- )
    fs-fid @ 0= ?EXIT
    fs-time 64@ 64dup 64-0= IF  64drop
    ELSE
	fs-fid @ flush-file throw
	fs-fid @ fileno fs-timestamp!
    THEN
    fs-fid @ close-file throw  fs-fid off
; fs-class to fs-close
:noname ( addr u mode -- ) fs-close
    msg( dup 2over ." open file: " type ."  with mode " . cr )
    >r 2dup absolut-path?  !!abs-path!!
    net2o-path open-path-file throw fs-path $! fs-fid !
    r@ r/o <> IF  0 fs-fid !@ close-file throw
	fs-path $@ r@ open-file throw fs-fid  !  THEN  rdrop
    fs-fid @ file-size throw d>64 64dup fs-size 64! fs-limit 64!
    64#0 fs-seek 64! 64#0 fs-seekto 64! 64#0 fs-time 64!
; fs-class to fs-open

: id>addr ( id -- addr remainder )
    >r file-state $@ r> cells /string >r dup IF  @  THEN r> ;
: id>addr? ( id -- addr )
    id>addr cell < !!fileid!! ;
: new>file ( -- )
    [: fs-class new sp@ cell file-state $+! drop ;]
    filestate-lock c-section ;

: lastfile@ ( -- fs-state ) file-state $@ + cell- @ ;
: state-addr ( id -- addr )
    id>addr dup 0< !!gap!!
    0= IF  drop  new>file lastfile@  THEN ;

: dest-top! ( addr -- )
    dup dest-top !@ U+DO
	data-ackbits @ I I' fix-size dup { len }
	chunk-p2 rshift swap chunk-p2 rshift swap bit-erase
    len +LOOP ;

: dest-back! ( addr -- )
    dup dest-back !@ U+DO
	data-ackbits @ I I' fix-size dup { len }
	chunk-p2 rshift swap chunk-p2 rshift swap bit-fill
    len +LOOP ;

: size! ( 64 id -- )  state-addr >o
    64dup fs-size 64!  fs-limit 64!
    64#0 fs-seekto 64! 64#0 fs-seek 64! o> ;
: seekto! ( 64 id -- )  state-addr >o
    fs-size 64@ 64umin fs-seekto 64! o> ;
: limit! ( 64 id -- )  state-addr >o
    fs-size 64@ 64umin fs-limit 64! o> ;

: file+ ( addr -- ) >r 1 r@ +!
    r@ @ id>addr nip 0<= IF  r@ off  THEN  rdrop ;

: fstates ( -- n )  file-state $@len cell/ ;

: fstate-off ( -- )  file-state @ 0= ?EXIT
    file-state $@ bounds ?DO  I @ .dispose  cell +LOOP
    file-state $off ;
: n2o:save-block ( id -- delta )
    rdata-back@ file( 2dup 2>r data-rmap @ >o over dest-raddr @ - o>
    >r ." file write: " rot dup . -rot r> hex. )
    rot id>addr? .fs-write file( dup hex. dup
    2r> rot umin $10 umin xtype cr ) dup /back ;

Sema file-sema

: save-all-blocks ( -- )
    [: timeout( data-rmap @ >o data-ackbits @ dest-size @ addr>bytes $FF skip
	dup IF  [: dump ;] $err  ELSE  2drop  THEN  o> )
	+calc fstates 0 { size fails }
	BEGIN  rdata-back?  WHILE
		write-file# @ n2o:save-block
		IF 0 ELSE fails 1+ residualwrite off THEN to fails
		residualwrite @ 0= ( rdata-back? or )  IF
		    write-file# file+ blocksize @ residualwrite !  THEN
	    fails size u>= UNTIL  THEN msg( ." Write end" cr ) +file ;]
    file-sema c-section ;

: save-to ( addr u n -- )  state-addr >o
    r/w create-file throw fs-fid ! o> ;

\ file status stuff

: n2o:get-stat ( id -- mtime mod )
    id>addr? >o fs-fid @ fileno statbuf fstat o> ?ior
    statbuf st_mtime ntime@ d>64
    statbuf st_mode l@ $FFF and ;

: n2o:track-mod ( mod fileno -- )
    [IFDEF] android 2drop
    [ELSE] swap fchmod ?ior [THEN] ;

: n2o:set-stat ( mtime mod id -- )
    id>addr? >o fs-fid @ fileno n2o:track-mod fs-time 64! o> ;

\ open/close a file - this needs *way more checking*! !!FIXME!!

User file-reg#

: n2o:close-file ( id -- )
    id>addr? >o fs-close  o> ;

: blocksize! ( n -- )
    dup blocksize !  dup residualread !  residualwrite ! ;

: n2o:close-all ( -- )
    [: fstates 0 ?DO
	    I n2o:close-file
	LOOP  file-reg# off  fstate-off
	blocksize @ blocksize!
	read-file# off  write-file# off ;] file-sema c-section ;

: n2o:open-file ( addr u mode id -- )
    state-addr .fs-open ;

\ read in from files

: n2o:slurp-block ( id -- delta )
    data-head@ file( 2dup 2>r data-map @ >o over dest-raddr @ - o>
    >r ." file read: " rot dup . -rot r> hex. )
    rot id>addr? .fs-read file( dup hex. dup
    2r> rot umin $10 umin 2drop ( xtype ) cr ) dup /head ;

: n2o:slurp ( -- head end-flag )  data-head? 0= IF  head@ 0  EXIT  THEN
    [: +calc fstates 0
	{ states fails }
	0 BEGIN  data-head?  WHILE
		read-file# @ n2o:slurp-block IF 0 ELSE fails 1+ THEN to fails
		residualread @ 0= ( data-head? or )  IF
		    read-file# file+  blocksize @ residualread !  THEN
	    fails states u>= UNTIL  THEN msg( ." Read end" cr ) +file
	head@ fails states u>= ;]
    file-sema c-section file( dup IF  ." data end" cr  THEN ) ;
    
: n2o:track-seeks ( idbits xt -- ) { xt } ( i seeklen -- )
    8 cells 0 DO
	dup 1 and IF
	    I dup id>addr? >o fs-seek 64@ fs-seekto 64@ 64<> IF
		fs-seekto 64@ 64dup fs-seek 64! o>
		xt execute  ELSE  drop o>  THEN
	THEN  2/
    LOOP  drop ;

: n2o:track-all-seeks ( xt -- ) { xt } ( i seeklen -- )
    fstates 0 DO
	I dup id>addr? >o fs-seek 64@ fs-seekto 64@ 64<> IF
	    fs-seekto 64@ 64dup fs-seek 64! o>
	    xt execute  ELSE  drop o>  THEN
    LOOP ;

\ helpers for addresses

: -skip ( addr u char -- ) >r
    BEGIN  1- dup  0>= WHILE  2dup + c@ r@ <>  UNTIL  THEN  1+ rdrop ;
: >sockaddr ( -- addr len )
    return-address be@ routes #.key $@ ['] .sockaddr $tmp ;
: n2oaddrs ( xt -- )
    my-ip$ [: [: type return-address $10 0 -skip type ;] $tmp
      rot dup >r execute r> ;] $[]map drop ;

\ load crypto here

require net2o-crypt.fs

\ cookie stuff

: cookie! ( -- )
    c:cookie
    dest-addr 64@ >offset 0= IF  drop 64drop  EXIT  THEN
    addr>ts cookie( ." Cookie: " dup hex. >r 64dup .16 cr r> )
    dest-cookies @ + 64! ;

: send-cookie ( -- )  data-map  @ .cookie! ;
: recv-cookie ( -- )  data-rmap @ .cookie! ;

: cookie+ ( addr bitmap map -- sum ) >o
    cookie( ." cookies: " 64>r dup hex. 64r> 64dup .16 space space ) 64>r
    addr>ts dest-size @ addr>ts umin
    dest-cookies @ + { addr } 64#0 cookie( ." cookie: " )
    BEGIN  64r@ 64>n 1 and IF  addr 64@ cookie( 64dup .16 space ) 64+  THEN
    addr 64'+ to addr 64r> 1 64rshift 64dup 64>r 64-0= UNTIL
    64r> 64drop cookie( ." => " 64dup .16 space cr ) o> ;

\ send blocks of memory

: >dest ( addr -- ) outbuf destination $10 move ;
: set-dest ( target -- )
    64dup dest-addr 64!  outbuf addr 64! ;

User outflag  outflag off

: set-flags ( -- )
    outflag @ outbuf 1+ c! outflag off ;

: c+!  ( n addr -- )  dup >r c@ + r> c! ;
: cor!  ( n addr -- )  dup >r c@ or r> c! ;

: outbody ( -- addr ) outbuf packet-body ;
: outsize ( -- n )    outbuf packet-size ;

#90 Constant EMSGSIZE

: packet-to ( addr -- )  >dest
    out-route  outbuf dup packet-size
    send-a-packet 0< IF
	errno EMSGSIZE = IF
	    max-size^2 1- to max-size^2  ." pmtu/2" cr
	ELSE
	    -512 errno - throw
	THEN
    THEN ;

: send-code-packet ( -- ) +sendX
\    ." send " outbuf .header
    o IF  code-map @  ELSE  0  THEN  outbuf-encrypt
    outbuf addr 64@ 64-0= IF
	return-addr
	.time ." cmd0 to: " dup $10 xtype cr
    ELSE
	return-address
    THEN   packet-to ;

: send-data-packet ( -- ) +sendX
    data-map @  outbuf-encrypt
    send-cookie ret-addr packet-to ;

: >send ( addr n -- )
    >r  r@ [ 64bit# qos3# or ]L or outbuf c!
    outbody min-size r> lshift move ;

: bandwidth+ ( -- )  o?
    ns/burst 64@ 1 tick-init 1+ 64*/ bandwidth-tick 64+! ;

: burst-end ( flag -- flag )  data-b2b @ ?EXIT
    ticker 64@ bandwidth-tick 64@ 64max next-tick 64! drop false ;

: send-cX ( addr n -- ) +sendX2
    >send  set-flags  send-code-packet  net2o:update-key ;

: send-dX ( addr n -- ) +sendX2
    >send  set-flags  bandwidth+ send-data-packet ;

Defer punch-reply

: send-punch ( -- )
    check-addr1 0= IF  2drop  EXIT  THEN
    insert-address temp-addr ins-dest
    temp-addr return-addr $10 move  punch-load $@ punch-reply ;

: net2o:punch ( addr u -- )
    o IF
	punch-load @ IF  ['] send-punch  ELSE  ['] ping-addr1  THEN
	$>sock
    ELSE  2drop  THEN ;

\ send chunk

: net2o:get-dest ( -- taddr )
    data-dest ;
: net2o:get-resend ( -- taddr )
    resend-dest ;

\ branchless version using floating point

User <size-lb> 1 floats cell- uallot drop

: send-size ( u -- n )
    min-size umax maxdata umin 1-
    [ min-size 2/ 2/ s>f 1/f ] FLiteral fm*
    <size-lb> df!  <size-lb> 6 + c@ 4 rshift ;

64Variable last-ticks

: ts-ticks! ( addr -- )
    addr>ts dest-timestamps @ + >r last-ticks 64@ r> ts-ticks
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
    resend$@ nip 0> data-tail? or ;

: net2o:resend ( -- addr n )
    resend$@ net2o:get-resend net2o:prep-send /resend ;

: net2o:send ( -- addr n )
    data-tail@ net2o:get-dest net2o:prep-send /tail ;

: ?toggle-ack ( -- )
    data-to-send 0= IF
	resend-toggle# outflag xor!  ack-toggle# outflag xor!
	never next-tick 64!
    THEN ;

: net2o:send-chunk ( -- )  +chunk
    ack-state c@ outflag or!
    bursts# 1- data-b2b @ = IF data-tail? ELSE resend$@ nip 0= THEN
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

: do-send-chunks ( -- )
    chunks $@ bounds ?DO
	I chunk-context @ o = IF
	    UNLOOP  EXIT
	THEN
    chunks-struct +LOOP
    [: o chunk-adder chunk-context !
	0 chunk-adder chunk-count !
	chunk-adder chunks-struct chunks $+! ;]
    resize-lock c-section
    ticker 64@ ticks-init ;

: o-chunks ( -- )
    [: chunks $@len 0 ?DO
	    chunks $@ I /string drop chunk-context @ o = IF
		chunks I chunks-struct $del
		r> r> chunks-struct - 2dup >r >r = ?LEAVE
	    0  ELSE  chunks-struct  THEN  +LOOP ;]
    resize-lock c-section ;

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
	    ack-state c!  flybursts @ ack-resend# c!  THEN
	-1 flybursts +! bursts( ." bursts: " flybursts ? flyburst ? cr )
	flybursts @ 0<= IF
	    bursts( .o ." no bursts in flight " ns/burst ? data-tail@ swap hex. hex. cr )
	THEN
    THEN
    tick-init = IF  off  ELSE  1 swap +!  THEN ;

: send-a-chunk ( chunk -- flag )  >r
    data-b2b @ 0<= IF
	bandwidth? dup  IF
	    b2b-toggle# ack-state xorc!
	    bursts# 1- data-b2b !
	THEN
    ELSE
	-1 data-b2b +!  true
    THEN
    dup IF  r@ chunk-count+  net2o:send-chunk  burst-end  THEN
    rdrop  1 chunks+ +! ;

: .nosend ( -- ) ." done, "  4 set-precision
    .o ." rate: " ns/burst @ s>f tick-init chunk-p2 lshift s>f 1e9 f* fswap f/ fe. cr
    .o ." slack: " min-slack ? cr
    .o ." rtdelay: " rtdelay ? cr ;

: send-chunks-async ( -- flag )
    chunks $@ chunks+ @ chunks-struct * safe/string
    IF
	dup chunk-context @ >o rdrop
	chunk-count
	data-to-send IF
	    \ msg( ." send a chunk" cr )
	    send-a-chunk
	ELSE
	    drop msg( .nosend )
	    chunks chunks+ @ chunks-struct * chunks-struct $del
	    false
	THEN
    ELSE  drop chunks+ off false  THEN ;

: next-chunk-tick ( -- tick )
    64#-1 chunks $@ bounds ?DO
	I chunk-context @ >o next-tick 64@ o> 64umin
    chunks-struct +LOOP ;

: send-another-chunk ( -- flag )  false  0 >r  !ticks
    BEGIN  BEGIN  drop send-chunks-async dup  WHILE  rdrop 0 >r  REPEAT
	chunks+ @ 0= IF  r> 1+ >r  THEN
    r@ 2 u>=  UNTIL  rdrop ;

: send-anything? ( -- flag )  chunks $@len 0> ;

\ rewind buffer to send further packets

: clear-cookies ( -- )
    s" " data-rmap @ >o data-ackbits-buf $! o> ;

:noname ( o:map -- )
    dest-timestamps @ dest-size @ addr>ts erase ;
dup data-class to rewind-timestamps
rdata-class to rewind-timestamps

:noname ( new-back o:map -- )
    dest-back @ U+DO
	I I' fix-size dup { len }
	addr>ts swap addr>ts swap >r dest-timestamps @ + r> erase
    len +LOOP ;
dup data-class to rewind-timestamps-partial
rdata-class to rewind-timestamps-partial

: clearpages-partial ( new-back o:map -- )
    dest-back @ U+DO
	I I' fix-size raddr+ tuck clearpages
    +LOOP ;

: rewind-partial ( new-back o:map -- )
    \ dup clearpages-partial
    dup rewind-timestamps-partial regen-ivs-part ;

: rewind-buffer ( o:map -- )
    1 dest-round +!
    dest-tail off  dest-head off  dest-back off  dest-top off
    regen-ivs-all  rewind-timestamps ;

: rewind-ackbits ( o:map -- )
    data-ack# off  data-reack# off
    firstack( ." rewind firstacks" cr )
    data-ackbits @ dest-size @ addr>bytes $FF fill ;

: net2o:rewind-sender ( n -- )
    data-map @ >o dest-round @
    +DO  rewind-buffer  LOOP  o> ;

: net2o:rewind-receiver ( n -- ) cookie( ." rewind" cr )
    data-rmap @ >o dest-round @
    +DO  rewind-buffer  LOOP
    rewind-ackbits ( clear-cookies ) o> ;

: net2o:rewind-sender-partial ( new-back -- )
    flush( ." rewind partial " dup hex. cr )
    data-map @ >o dup rewind-partial dest-back ! o> ;

: net2o:rewind-receiver-partial ( new-back -- )
    flush( ." rewind partial " dup hex. cr )
    data-rmap @ >o dup rewind-partial  dest-back! o> ;

\ separate thread for loading and saving...

: net2o:save ( -- )
    data-rmap @ .dest-back @ >r save-all-blocks
    r> data-rmap @ >o dest-back !@ dup do-slurp ! o>
    net2o:rewind-receiver-partial ;

Defer do-track-seek

event: ->track ( o -- )  >o ['] do-track-seek n2o:track-all-seeks o> ;
event: ->slurp ( task o -- )  >o n2o:slurp o elit, ->track event> o> ;
event: ->save ( o -- ) .net2o:save ;

0 Value file-task

: create-file-task ( -- )
    [: BEGIN  ['] event-loop catch ?dup-IF  DoError  THEN  AGAIN ;]
    1 net2o-task to file-task ;
: net2o:save& ( -- ) file-task 0= IF  create-file-task  THEN
    o elit, ->save file-task event> ;

\ schedule delayed events

object class
field: queue-timestamp
field: queue-job
field: queue-xt
end-class queue-class
queue-class >osize @ Constant queue-struct

Variable queue s" " queue $!
queue-class >osize @ buffer: queue-adder  

: add-queue ( xt us -- )
    ticker 64@ +  o queue-adder >o queue-job !  queue-timestamp !
    queue-xt !  o queue-struct queue $+! o> ;

: eval-queue ( -- )
    queue $@len 0= ?EXIT  ticker 64@
    queue $@ bounds ?DO  I >o
	dup queue-timestamp @ u> IF
	    queue-xt @ queue-job @ .execute
	    0 queue-timestamp !
	THEN o>
    queue-struct +LOOP  drop
    0 >r BEGIN  r@ queue $@len u<  WHILE
	    queue $@ r@ safe/string drop queue-timestamp @ 0= IF
		queue r@ queue-struct $del
	    ELSE
		r> queue-struct + >r
	    THEN
    REPEAT  rdrop ;

\ poll loop

: prep-evsocks ( -- )  pollfds >r
    epiper @    fileno POLLIN  r> fds!+ drop 1 to pollfd# ;

: clear-events ( -- )  pollfds
    pollfd# 0 DO  0 over revents w!  pollfd %size +  LOOP  drop ;

: timeout! ( -- )
    sender-task dup IF  up@ =  ELSE  0=  THEN  IF
	next-chunk-tick 64dup 64#-1 64= 0= >r ticker 64@ 64- 64dup 64-0>= r> or
	IF    64>n 0 max poll-timeout# min 0 ptimeout 2!
	ELSE  64drop poll-timeout# 0 ptimeout 2!  THEN
    ELSE  poll-timeout# 0 ptimeout 2!  THEN ;

: max-timeout! ( -- ) poll-timeout# 0 ptimeout 2! ;

: poll-sock ( -- flag )
    eval-queue  clear-events  timeout!
    pollfds pollfd#
[IFDEF] ppoll
    ptimeout 0 ppoll 0>
[ELSE]
    ptimeout cell+ @ #1000000 / poll 0>
[THEN] +wait
;

: wait-send ( -- flag )
    clear-events  timeout!
    pollfds pollfd#
[IFDEF] ppoll
    ptimeout 0 ppoll 0>
[ELSE]
    ptimeout cell+ @ #1000000 / poll 0>
[THEN] ;

: read-a-packet4/6 ( -- addr u )
    pollfds revents w@ POLLIN = IF
	do-block read-a-packet  0 pollfds revents w! +rec EXIT  THEN
    don't-block read-a-packet +rec ;

: read-event ( -- )
    pollfds [ pollfd %size revents ]L + w@ POLLIN = IF
	?events  0 pollfds pollfd %size + revents w!
    THEN ;

4 Value try-read#

: try-read-packet-wait ( -- addr u / 0 0 )
    try-read# 0 ?DO
	don't-block read-a-packet
	dup IF  unloop  +rec  EXIT  THEN  2drop  LOOP
    poll-sock drop read-a-packet4/6 read-event ;

4 Value sends#
4 Value sendbs#
16 Value recvs# \ balance receive and send
Variable recvflag  recvflag off

: read-a-packet? ( -- addr u )
    don't-block read-a-packet dup IF  1 recvflag +!  THEN ;

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
    [: prep-evsocks send-loop ;] 1 net2o-task to sender-task ;

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
    >flyburst
    timeout( ." timeout? " 64. send-anything? . chunks+ ? bandwidth? . next-chunk-tick . cr )else( 64drop ) ;

\ timeout handling

: do-timeout ( -- )  timeout-xt perform ;

#2.000.000.000 d>64 64Value timeout-max# \ 2s maximum timeout
#10.000.000 d>64 64Value timeout-min# \ 10ms minimum timeout
#12 Value timeouts# \ with 30ms initial timeout, gives 4.8s cummulative timeout

Sema timeout-sema
Variable timeout-tasks s" " timeout-tasks $!
Variable timeout-o

: o+timeout ( -- ) timeout( ." +timeout: " o hex. ." task: " up@ hex. cr )
    [: timeout-tasks $@ bounds ?DO  I @ o = IF
	      UNLOOP  EXIT  THEN
      cell +LOOP
      o timeout-o !  timeout-o cell timeout-tasks $+! ;]
  timeout-sema c-section  timeout-task wake ;
: o-timeout ( -- ) timeout( ." -timeout: " o hex. ." task: " up@ hex. cr )
    [: timeout-tasks $@len 0 ?DO
	  timeout-tasks $@ I /string drop @ o =  IF
	      timeout-tasks I cell $del
	      timeout-tasks $@len drop
	      r> r> cell- 2dup >r >r = ?LEAVE
	      0  ELSE  cell  THEN
      +LOOP ;] timeout-sema c-section ;
: -timeout      ['] no-timeout  timeout-xt ! o-timeout ;

: sq2** ( 64n n -- 64n' )
    dup 1 and >r 2/ 64lshift r> IF  64dup 64-2/ 64+  THEN ;
: >next-timeout ( -- )
    rtdelay 64@ timeout-min# 64max timeouts @ sq2**
    timeout-max# 64min \ timeout( ." timeout setting: " 64dup 64. cr )
    ticker 64@ 64+ next-timeout 64! ;
: 0timeout ( -- )
    rtdelay 64@ timeout-min# 64max ticker 64@ 64+ next-timeout 64!
    0 timeouts !@ IF  timeout-task wake  THEN ;
: 64min? ( a b -- min flag )
    64over 64over 64< IF  64drop false  ELSE  64nip true  THEN ;
: next-timeout? ( -- time context ) [: 0 { ctx } max-int64
    timeout-tasks $@ bounds ?DO
	I @ .next-timeout 64@ 64min? IF  I @ to ctx  THEN
    cell +LOOP  ctx ;] timeout-sema c-section ;
: ?timeout ( -- context/0 )
    ticker 64@ next-timeout? >r 64- 64-0>= r> and ;

\ handling packets

Defer queue-command ( addr u -- )
' dump IS queue-command

User validated

$01 Constant crypt-val
$02 Constant own-crypt-val
$04 Constant login-val
$08 Constant cookie-val
$10 Constant tmp-crypt-val

: crypt?     ( -- flag )  validated @ crypt-val     and ;
: own-crypt? ( -- flag )  validated @ own-crypt-val and ;
: login?     ( -- flag )  validated @ login-val     and ;
: cookie?    ( -- flag )  validated @ cookie-val    and ;
: tmp-crypt? ( -- flag )  validated @ tmp-crypt-val and ;

: handle-cmd0 ( -- ) \ handle packet to address 0
    .time ." handle cmd0 " sockaddr alen @ .address cr
    0 >o rdrop \ address 0 has no job context!
    0 inbuf-decrypt 0= IF
	." invalid packet to 0" drop cr EXIT  THEN
    validated off \ packets to address 0 are not really validated
    inbuf packet-data queue-command ;

: handle-data ( addr -- )  dest-job @ >o
    >r inbuf packet-data r> swap move
    +inmove ack-xt perform +ack o> ;
' handle-data rdata-class to handle
' drop data-class to handle

: handle-cmd ( addr -- )  dest-job @ >o
    >r inbuf packet-data r@ swap dup >r move
    r> r> swap queue-command o IF  o>  ELSE  rdrop  THEN ;
' handle-cmd rcode-class to handle
' drop code-class to handle

: .inv-packet ( -- )
    msg( ." invalid packet to " inbuf addr 64@ ['] 64. $10 base-execute
    ." size " min-size inbuf c@ datasize# and lshift hex. cr ) ;

: handle-dest ( addr map -- ) \ handle packet to valid destinations
    ticker 64@  recv-tick 64! \ time stamp of arrival
    dup >r inbuf-decrypt 0= IF  .inv-packet  drop rdrop EXIT  THEN
    crypt-val validated ! \ ok, we have a validated connection
    return-addr return-address $10 move
    r> >o handle o IF  o>  ELSE  rdrop  THEN ;

: handle-packet ( -- ) \ handle local packet
    >ret-addr >dest-addr +desta
    dest-addr 64@ 64-0= IF  handle-cmd0
    ELSE
	check-dest dup 0= IF
	    msg( ." unhandled packet to: " dest-addr 64@ $64. cr )
	    drop  EXIT  THEN +dest
	0timeout handle-dest
    THEN ;

: route-packet ( -- ) route( ." route to: " inbuf destination $10 xtype cr )
    inbuf >r r@ get-dest route>address
    r> dup packet-size send-a-packet drop ;

\ dispose context

: n2o:dispose-context ( o:addr -- o:addr )
    [: cmd( ." Disposing context... " o hex. cr )
	timeout( ." Disposing context... " o hex. ." task: " up@ hex. cr )
	o-timeout o-chunks
	0. data-rmap @ >o dest-vaddr 64@ o> >dest-map 2!
	data-map  @ ?dup-IF  .free-data  THEN
	data-rmap @ ?dup-IF  .free-data  THEN
	code-map  @ ?dup-IF  .free-data  THEN
	code-rmap @ ?dup-IF  .free-data  THEN
	resend0 $off  fstate-off
	\ erase crypto keys
	crypto-key $@ erase  crypto-key $off
	data-resend $off  timing-stat $off
	dest-pubkey $off
	dispose
	cmd( ." disposed" cr ) ;] file-sema c-section ;

\ loops for server and client

8 cells 1- Constant maxrequest#

: next-request ( -- n )
    1 dup request# +!@ maxrequest# and tuck lshift reqmask or! ;

: packet-event ( -- )
    next-packet !ticks nip 0= ?EXIT  inbuf route?
    IF  route-packet  ELSE  handle-packet  THEN ;

event: ->request ( n -- ) 1 over lshift invert reqmask and!
    msg( ." Request completed: " . ." task: " up@ hex. cr )else( drop ) ;
event: ->reqsave ( task n -- )  elit, ->request event> ;
event: ->timeout ( -- ) reqmask off msg( ." Request timed out" cr )
    true !!timeout!! ;

: request-timeout ( -- )
    ?timeout ?dup-IF  >o rdrop
	timeout( ." do timeout: " o hex. timeout-xt @ .name cr ) do-timeout
	timeouts @ timeouts# >= wait-task @ and  ?dup-IF  ->timeout event>  THEN
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
	2>r net2o-sock fileno s" ?" 0 2r> sendto +send ;] $[]map ;

: beacon? ( -- )
    beacon-time 64@ ticker 64@ 64- 64-0< IF
	send-beacons +beacons
    THEN ;

: +beacon ( sockaddr len -- )
    beacon( ." add beacon: " 2dup .address cr )
    beacons $+[]! ;
: add-beacon ( net2oaddr -- ) route>address sockaddr alen @ +beacon ;

\ timeout loop

: .loop-err ( throw xt -- )
    .name dup . cr DoError cr ;

: event-send ( -- )
    o IF  wait-task @  ?dup-IF  event>  THEN  0 >o rdrop  THEN ;

#10000000 Constant watch-timeout# \ 10ms timeout check interval

: >next-ticks ( -- )
    next-timeout? drop beacon-time 64@ 64umin ticker 64@ 64-
    64#0 64max timeout( ." wait for " 64dup 64. ." ns" cr ) stop-ns
    timeout( ticker 64@ ) !ticks
    timeout( ticker 64@ 64swap 64- ." waited for " 64. ." ns" cr ) ;

: timeout-loop-nocatch ( -- ) !ticks
    BEGIN  >next-ticks beacon? request-timeout event-send  AGAIN ;

: catch-loop ( xt -- ) >r
    BEGIN   nothrow r@ catch ?int dup  WHILE
	    r@ .loop-err  REPEAT  drop rdrop ;

: create-timeout-task ( -- )
    [: BEGIN  ['] timeout-loop-nocatch catch-loop  AGAIN ;]
    1 net2o-task to timeout-task ;

\ event loop

: event-loop-nocatch ( -- ) \ 1 stick-to-core
    BEGIN  packet-event  event-send  AGAIN ;

: n2o:request-done ( n -- )
    request( ." Request " dup . ." done, to task: " wait-task @ hex. cr )
    file-task ?dup-IF  wait-task @ elit, elit, ->reqsave event>
    ELSE  elit, ->request  THEN ;

: create-receiver-task ( -- )
    [: BEGIN  ['] event-loop-nocatch catch-loop
	    ( wait-task @ ?dup-IF  ->timeout event>  THEN ) AGAIN ;]
    1 net2o-task to receiver-task ;

: event-loop-task ( -- )
    receiver-task 0= IF  create-receiver-task  THEN ;

: requests->0 ( -- ) BEGIN  stop reqmask @ 0= UNTIL  o-timeout ;

: client-loop ( -- )
    !ticks
    o IF  up@ wait-task !  0timeout o+timeout  THEN
    event-loop-task requests->0 ;

: server-loop ( -- )  0 >o rdrop  -1 reqmask !  client-loop ;

\ client/server initializer

: init-cache ( -- )
    s" .cache" file-status nip #-514 = IF
	s" .cache" $1FF =mkdir throw
    THEN ;

: init-rest ( port -- )  init-mykey init-mykey \ two keys
    init-timer net2o-socket init-route prep-socks
    sender( create-sender-task ) create-timeout-task ;

: init-client ( -- )  init-cache 0 init-rest ;
: init-server ( -- )  net2o-port init-rest ;

\ connection cookies

object class
    64field: cc-timeout
    field: cc-context
end-class con-cookie

con-cookie >osize @ Constant cookie-size#

Variable cookies s" " cookies $!
con-cookie >osize @ buffer: cookie-adder

#5000000000. d>64 64Constant connect-timeout#

: add-cookie ( -- cookie )
    [: o cookie-adder >o cc-context !
      ntime d>64 64dup cc-timeout 64!
      o o> cookie-size#  cookies $+! ;]
    resize-lock c-section ;

: do-?cookie ( cookie -- context true / false )
    ticker 64@ connect-timeout# 64- { 64: timeout }
      0 >r BEGIN  r@ cookies $@len u<  WHILE
	      cookies $@ r@ /string drop >o
	      cc-timeout 64@ timeout 64u< IF
		  o> cookies r@ cookie-size# $del
	      ELSE
		  64dup cc-timeout 64@ 64= IF
		      64drop cc-context @ o>
		      cookies r> cookie-size# $del
		      true EXIT
		  THEN
		  o> r> cookie-size# + >r
	      THEN
      REPEAT  64drop rdrop false ;
  
: ?cookie ( cookie -- context true / false )
    ['] do-?cookie resize-lock c-section ;

: cookie>context? ( cookie -- context true / false )
    ?cookie over 0= over and IF
	nip return-addr be@ n2o:new-context o 0 >o rdrop swap
    THEN ;

: rtdelay! ( time -- ) recv-tick 64@ 64swap 64- rtdelay 64! ;
: adjust-ticks ( time -- )  o 0= IF  64drop  EXIT  THEN
    recv-tick 64@ 64- rtdelay 64@ 64-2/
    64over 64abs 64over 64> IF  64+ tick-adjust 64!
    ELSE  64drop 64drop  THEN ;

\ load net2o plugins: first one with integraded command space

require net2o-cmd.fs
require net2o-dht.fs
require net2o-keys.fs \ extra cmd space

0 [IF]
Local Variables:
forth-local-words:
    (
     (("event:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     (("debug:" "field:" "sffield:" "dffield:" "64field:") non-immediate (font-lock-type-face . 2)
      "[ \t\n]" t name (font-lock-variable-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("event:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]
