\ net2o template for new files

\ Copyright © 2015   Bernd Paysan

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

\ address interpreter

scope{ net2o-base

cmd-table $@ inherit-table address-table
\g 
\g ### address commands ###
\g 

$11 net2o: addr-pri# ( n -- ) \g priority
    64>n host:pri# ! ;
+net2o: addr-id ( $:id -- ) \g unique host id string
    $> host:id $! ;
+net2o: addr-anchor ( $:pubkey -- ) \g anchor for routing further
    $> host:anchor $! ;
+net2o: addr-ipv4 ( n -- ) \g ip address
    64>n host:ipv4 be-l! ;
+net2o: addr-ipv6 ( $:ipv6 -- ) \g ipv6 address
    $> host:ipv6 $10 smove ;
+net2o: addr-portv4 ( n -- ) \g ipv4 port
    64>n host:portv4 w! ;
+net2o: addr-portv6 ( n -- ) \g ipv6 port
    64>n host:portv4 w! ;
+net2o: addr-port ( n -- ) \g ip port, both protocols
    64>n dup host:portv4 w! host:portv6 w! ;
+net2o: addr-route ( $:net2o -- ) \g net2o routing part
    $> host:route $! ;
+net2o: addr-key ( $:addr -- ) \g key for connection setup
    $> host:key sec! ;
+net2o: addr-revoke ( $:revoke -- ) \g revocation info
    $> host:revoke $! ;
+net2o: addr-ekey ( $:ekey to -- ) \g ephemeral key
    host:ekey-to 64! $> host:ekey $! ;
}scope

address-table $save
' context-table is gen-table

: $[]o-map { addr xt -- }
    \G execute @var{xt} for all elements of the object array @var{addr}.
    \G xt is @var{( o -- )}, getting one string at a time
    addr $@ bounds ?DO  I @ xt execute  cell +LOOP ;

in net2o : new-addr ( -- o )
    address-table address-class new-tok ;
in net2o : dispose-addr ( o:addr -- o:addr )
    host:id $free host:anchor $free host:route $free  host:revoke $free
    host:key sec-free  host:ekey $free
    dispose ;
in net2o : dispose-addrs ( addr -- )
    dup >r [: .net2o:dispose-addr ;] $[]o-map
    r> $free ;
:noname ( -- )
    dest-addrs  net2o:dispose-addrs
    punch-addrs net2o:dispose-addrs
    defers extra-dispose ; is extra-dispose

: $>addr ( addr u -- o ) \G create a new address object from string
    net2o:new-addr n:>o nest-cmd-loop o n:o> ;

also net2o-base
: o-genaddr ( o -- ) >o \G create new address string from object
    host:pri# @ ulit, addr-pri#
    host:id $@ dup IF $, addr-id  ELSE  2drop  THEN
    host:anchor $@ dup IF $, addr-anchor  ELSE  2drop  THEN
    host:ipv4 be-ul@ ?dup-IF ulit, addr-ipv4  THEN
    host:ipv6 ip6? IF  host:ipv6 $10 $, addr-ipv6  THEN
    host:portv4 w@ host:portv6 w@ = IF
	host:portv4 w@ ulit, addr-port
    ELSE
	host:portv4 w@ ?dup-IF  ulit, addr-portv4  THEN
	host:portv6 w@ ?dup-IF  ulit, addr-portv6  THEN
    THEN
    host:route $@ dup IF  $, addr-route  ELSE  2drop  THEN
    host:key sec@ dup IF  sec$, addr-key  ELSE  2drop  THEN
    host:ekey $@ dup IF  $, host:ekey-to 64@ lit, addr-ekey  ELSE  2drop  THEN
    host:revoke $@ dup IF  $, addr-revoke  ELSE  2drop  THEN o> ; 
previous
: o>addr ( o -- addr u )
    cmdbuf-o @ >r code-buf$ cmdreset o-genaddr cmdbuf$ r> cmdbuf-o ! ;

: .addr ( o -- ) \G print addr
    >o
    host:pri# @ ?dup-IF  0 .r '#' emit  THEN
    host:id $@ dup IF '"' emit type '"' emit  ELSE  2drop  THEN
    host:anchor $@ dup IF ." anchor: " 85type cr  ELSE  2drop  THEN
    host:ipv6 ip6? IF  host:ipv6 $10 .ip6a 2drop
	host:portv4 w@ host:portv6 w@ <> IF  host:portv6 w@ ." :" 0 .r space THEN
    THEN
    host:ipv4 be-ul@ dup IF host:ipv4 4 .ip4a 2drop  THEN
    host:portv4 w@ host:portv6 w@ = or  IF host:portv4 w@ ." :" 0 .r  THEN
    host:route $@ dup IF  '|' emit xtype  ELSE  2drop  THEN
    host:key sec@ dup IF  '$' emit .black85  ELSE  2drop  THEN
    host:ekey $@ dup IF  '§' xemit 85type  ELSE  2drop  THEN
    o> ; 

: .nat-addrs ( -- )
    punch-addrs $@ bounds ?DO  I @ .addr cr  cell +LOOP ;

: .addr$ ( addr u -- )
    $>addr >o o .addr net2o:dispose-addr o> ;

: addr>6sock ( -- )
    host:key @ dest-0key< !
    host:portv6 w@ sockaddr1 port be-w!
    host:ipv6 sockaddr1 sin6_addr ip6!
    host:route $@ !temp-addr ;

: addr>4sock ( -- )
    host:key @ dest-0key< !
    host:portv4 w@ sockaddr1 port be-w!
    host:ipv4 be-ul@ sockaddr1 ipv4!
    host:route $@ !temp-addr ;

: addr>sock ( o xt -- ) { xt } >o
    ipv64(
    ipv6( host:ipv6 ip6?   IF  addr>6sock o o> >r xt execute  r> >o THEN )
    ipv4( host:ipv4 be-ul@ IF  addr>4sock o o> >r xt execute  r> >o THEN )
    )else(
    ipv4( host:ipv4 be-ul@ IF  addr>4sock o o> >r xt execute  r> >o THEN )
    ipv6( host:ipv6 ip6?   IF  addr>6sock o o> >r xt execute  r> >o THEN )
    )
    o> ;

: +my-0key ( -- )
    my-0key @ IF  my-0key sec@ host:key sec!  THEN
    my-ekey-pk @ IF
	my-ekey-pk $@ host:ekey $!
	my-ekey-to 64@ host:ekey-to 64!
    THEN ;
: +my-id ( -- )
    config:prio# @ host:pri# !
    host$ $@ host:id $!  +my-0key ;

: +my-addrs ( port o:addr -- )
    +my-id
    dup host:portv4 w!  host:portv6 w!
    o my-addr[] >stack ;

: !my-addrs ( -- ) net2o:new-addr >o
    global-ip6 tuck host:ipv6 $10 smove
    global-ip4 IF  be-ul@ host:ipv4 be-l!  ELSE  drop  THEN
    my-port# +my-addrs o>
    0= IF  ipv6( local-ip6  IF
	    net2o:new-addr >o  host:ipv6 ip6!  my-port# +my-addrs  o>
	ELSE  drop  THEN )
    THEN ;

: addrs-off ( -- )
    \G dispose all addresses
    my-addr[] [: .net2o:dispose-addr ;] $[]o-map
    my-addr[] $free
    my-addr$ $[]free
    pub-addr$ $[]free
    priv-addr$ $[]free ;

: !my-addr$ ( -- )
    my-key-default 0= ?EXIT  my-addr$ $[]free
    now>never  my-addr[] [:
	nat( ." insert into my-addr$: " dup .addr forth:cr )
	dup .host:ekey-to 64@ 64dup 64-0= n>64 64+
	sigdate 64'+ le-64!
	o>addr gen-host my-addr$ $ins[]sig drop ;] $[]o-map ;

: .my-addrs ( -- )
    my-addr[] [: .addr cr ;] $[]o-map ;
: .my-addr$s ( -- )
    my-addr$ [: sigsize# - .addr$ cr ;] $[]map ;
: .pub-addr$s ( -- )
    pub-addr$ [: sigsize# - .addr$ cr ;] $[]map ;
: .priv-addr$s ( -- )
    priv-addr$ [: sigsize# - .addr$ cr ;] $[]map ;

: !my-addr ( -- )
    addrs-off !my-addrs !my-addr$
    dns64? `xlat464( >body ! ;

\ merge addresses

: my-addr= ( o1 o:o2 -- ) { o1 }
    o1 .host:portv4 2 host:portv4 over str=?0 &&
    o1 .host:portv6 2 host:portv6 over str=?0 &&
    o1 .host:route $@ host:route $@  str=?0 &&
    o1 .host:ipv4   4 host:ipv4 over str=?0 &&
    o1 .host:ipv6 $10 host:ipv6 over str=?0 ;

: my-addr? ( o -- flag )
    false my-addr[] [: >o over my-addr= o> or ;] $[]o-map nip ;

: my-addr-merge1 ( o1 o:o2 -- ) { o1 }
    o1 .host:ipv4   4 host:ipv4 over str>merge
    o1 .host:ipv6 $10 host:ipv6 over str>merge ;

: my-addr-merge ( o -- flag )
    false swap
    my-addr[] [: >o dup my-addr= IF dup my-addr-merge1
	nip true swap THEN o> ;] $[]o-map
    drop ;

\ sockaddr conversion

also net2o-base
: .sockaddr ( addr alen -- sockaddr u )
    \ convert socket into net2o address token
    [: { addr alen }
	case addr family w@
	    AF_INET of
		addr sin_addr be-ul@ ulit, addr-ipv4
	    endof
	    AF_INET6 of
		addr fake-ip4? IF
		    .ip6::0 addr sin6_addr 12 + be-ul@ ulit, addr-ipv4
		ELSE
		    addr sin6_addr $10 $, addr-ipv6
		THEN
	    endof
	endcase
	addr port be-uw@ ulit, addr-port
    ;] gen-cmd$ ;
: sockaddr+return ( addr len -- addr' len' )
    [: cmd$ $! return-address $10 0 -skip $, addr-route ;] gen-cmd$ ;
previous
: >sockaddr ( -- addr len )
    return-address be@ routes# #.key >r
    r@ cell+ $@len 0= IF  s" ." r@ cell+ $!  THEN \ set route to known
    r> $@ .sockaddr ;

\\\
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
