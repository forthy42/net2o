\ connection setup helper

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

$Variable dhtnick "net2o-dhtroot" dhtnick $!
$Variable dhtroot-addr$
Variable dhtroot-addr

:noname defers 'cold dhtroot-addr off ; is 'cold

require dhtroot.fs

: dhtroot-addr@ ( -- addr )
    dhtroot-addr @ ?dup-IF  EXIT  THEN
    dhtroot-addr$ $@ dup IF
	>host dhtnick $@ nick>pk drop date-sig? 0= IF
	    sigsize# -  new-addr dup dhtroot-addr !
	    EXIT  THEN  THEN
    2drop 0 ;

: !0key ( -- )
    dest-0key< @ IF
	\ check for disconnected state
	ind-addr @ 0= lastaddr# and IF
	    dest-0key< sec@ lastaddr# cell+ $!  THEN
	dest-0key> @ IF  dest-0key< sec@ dest-0key> @ sec!  THEN
    THEN ;

0 value online?

: dhtroot ( -- )
    0 to lastaddr#
    dhtroot-addr@ ?dup-IF
	0 swap
	[: dup ?EXIT
	    check-addr1 IF  insert-address nip
	    ELSE  2drop  THEN ;] addr>sock
    ELSE  net2o-host $@ net2o-port insert-ip
    THEN  return-addr dup $10 erase be!
    lastaddr# 0<> to online?
    ind-addr off  !0key ;

: dhtroot-off ( --- )
    dhtroot-addr$ $off
    dhtroot-addr @ ?dup-IF  net2o:dispose-addr  THEN ;

: ins-ip ( -- net2oaddr )
    net2o-host $@ net2o-port insert-ip  ind-addr off ;
: ins-ip4 ( -- net2oaddr )
    net2o-host $@ net2o-port insert-ip4 ind-addr off ;
: ins-ip6 ( -- net2oaddr )
    net2o-host $@ net2o-port insert-ip6 ind-addr off ;

: pk:connect ( code data key u -- )
    connect( [: .time ." Connect to: " dup hex. cr ;] $err )
    net2o:new-context >o rdrop o to connection  setup!
    dest-pk \ set our destination key
    +resend-cmd net2o:connect
    +flow-control +resend
    connect( [: .time ." Connected, o=" o hex. cr ;] $err ) ;

Forward renat-all

event: :>renat ( -- )  renat-all ;
event: :>disconnect ( addr -- )  .disconnect-me ;
: dht-beacon ( addr u -- )
    <event :>renat main-up@ event> 2drop ;

: +dht-beacon ( -- )
    beacons# @ 0= IF  ret-addr be@ ['] dht-beacon 0 .add-beacon  THEN ;

: dht-connect ( -- )
    dht-connection ?dup-IF  >o o to connection rdrop  EXIT  THEN
    tick-adjust 64@ 64-0= IF  +get-time  THEN
    $8 $8 dhtnick $@ nick>pk dhtroot
    online? IF  +dht-beacon pk:connect  o to dht-connection  THEN ;
: dht-disconnect ( -- )
    0 addr dht-connection !@  ?dup-IF
	>o o to connection disconnect-me o>  THEN ;

Variable announced
: subme ( -- )  announced @ IF
	dht-connect sub-me THEN ;

: c:disconnect ( -- ) connect( [: ." Disconnecting..." cr ;] $err )
    disconnect-me connect( [: .packets profile( .times ) ;] $err ) ;

: c:fetch-id ( pubkey u -- )
    net2o-code
      expect-reply  fetch-id,
      cookie+request
    end-code| ;

: pk:fetch-host ( key u -- )
    net2o-code
      expect-reply get-ip fetch-id, cookie+request
    end-code| -setip ;

: pk:addme-fetch-host ( key u -- ) +addme
    net2o-code
      expect-reply get-ip fetch-id, replace-me,
      cookie+request
    end-code| -setip net2o:send-replace  announced on ;

\ NAT retraversal

Forward insert-addr ( o -- )

: renat ( -- )
    msg-groups [:
      cell+ $@ bounds ?DO
	  I @ >o o-beacon pings
	  \ !!FIXME!! should maybe do a re-lookup?
	  ret-addr $10 erase  dest-0key dest-0key> !
	  punch-addrs $@ bounds ?DO
	      I @ insert-addr IF
		  o to connection
		  net2o-code new-request true gen-punchload gen-punch
		  end-code
	      THEN
	  cell +LOOP o>
      cell +LOOP
    ;] #map ;

\ notification for address changes

[IFDEF] android     require android/net.fs  [ELSE]
    [IFDEF] PF_NETLINK  require linux/net.fs    [THEN]
[THEN]

\ announce and renat

: announce-me ( -- )
    \ Check for disconnected state
    dht-connect online? IF  replace-me -other  announced on  THEN ;

: renat-all ( -- ) beacon( ." remove all beacons" cr )
    [IFDEF] renat-complete [: [THEN]
	0 .!my-addr dht-disconnect \ old DHT may be stale
	announce-me \ if we succeed here, we can try the rest
	beacons# #frees
	0 >o dhtroot +dht-beacon o>
	renat
    [IFDEF] renat-complete ;] catch renat-complete throw [THEN]
    beacon( ." done renat" cr ) ;

scope{ /chat
: /renat ( addr u -- ) renat-all /nat ;
}scope

\ beacon handling

event: :>do-beacon ( addr -- )
    beacon( ." :>do-beacon" forth:cr )
    { beacon } beacon cell+ $@ 1 64s /string bounds ?DO
	beacon $@ I 2@ .execute
    2 cells +LOOP ;

: do-beacon ( addr -- )  \ sign on, and do a replace-me
    <event elit, :>do-beacon ?query-task event> ;


Variable my-beacon

: my-beacon-hash ( -- hash u )
    my-beacon $@ dup ?EXIT  2drop
    my-0key sec@ "beacon" keyed-hash#128 2/ my-beacon $!
    my-beacon $@ ;

: check-beacon-hash ( addr u -- flag )
    my-beacon-hash str= ;

: check-punch-hash ( addr u -- connection/false )
\    2dup dump
    dup $18 < IF  2drop false  EXIT  THEN
    over le-64@ >dest-map @ dup IF  .parent >o
	8 /string punch# over key| str= o and o>
    ELSE  nip nip  THEN ;


: ?-beacon ( addr u -- )
    \G if we don't know that address, send a reply
    need-beacon# @ IF
	2dup check-beacon-hash 0= IF
	    beacon( ticks .ticks ."  wrong beacon hash"
	    85type ."  instead of " my-beacon $@ 85type cr )else( 2drop )  EXIT
	THEN
    THEN  2drop
    net2o-sock
    sockaddr< alen @ routes# #@ nip 0= IF  "!"  ELSE  "."  THEN
    beacon( ticks .ticks ."  Send '" 2dup 2dup printable? IF  type  ELSE  85type  THEN
    ." ' reply to: " sockaddr< alen @ .address forth:cr )
    0 sockaddr< alen @ sendto drop +send ;
: !-beacon ( addr u -- ) 2drop
    \G I got a reply, my address is unknown
    beacon( ticks .ticks ."  Got unknown reply: " sockaddr< alen @ .address forth:cr )
    sockaddr< alen @ beacons# #@ d0<> IF  last# do-beacon  THEN ;
: .-beacon ( addr u -- ) 2drop
    \G I got a reply, my address is known
    beacon( ticks .ticks ."  Got known reply: " sockaddr< alen @ .address forth:cr )
    sockaddr< alen @ beacons# #@ IF
	>r r@ 64@ ticks 64umin beacon-ticks# 64+ r> 64!
    ELSE  drop  THEN ;
: >-beacon ( addr u -- )
    \G I got a punch
    nat( ticks .ticks ."  Got punch: " sockaddr< alen @ .address forth:cr )
    check-punch-hash ?dup-IF
	\ !!FIXME!! accept only two: one IPv4, one IPv6.
	\ !!FIXME!! and try merging the two into existent
	>o sockaddr< alen @ nat( ." +punch " 2dup .address forth:cr )
	.sockaddr new-addr punch-addrs >stack o>
    THEN ;

: handle-beacon ( addr u char -- )
    case
	'?' of  ?-beacon  endof
	'!' of  !-beacon  endof
	'.' of  .-beacon  endof
	'>' of  >-beacon  endof
	nip
    endcase ;

: handle-beacon+hash ( addr u -- )
    dup IF  over c@ >r 1 /string r> handle-beacon  ELSE  2drop  THEN ;

: replace-loop ( addr u -- flag )
    BEGIN  key2| >d#id >o dht-host $[]# IF  0 dht-host $[]@  ELSE  #0.  THEN o>
	2dup d0<> WHILE
	    over c@ '!' = WHILE
		replace-key o>
		connect( >o ke-pk $@ ." replace key: " 2dup 85type cr o o> )
		>r 2dup c:fetch-id r> >o  REPEAT  THEN  d0<> ;

: pk-query ( addr u xt -- flag ) >r
    dht-connect online? IF  2dup r> execute  replace-loop
    ELSE  2drop rdrop false  THEN ;

: pk-lookup ( addr u -- )
    ['] pk:fetch-host  ['] pk:addme-fetch-host  announced @ select
    pk-query 0= !!host-notfound!! ;

: pk-peek? ( pk u -- flag )  ['] pk:fetch-host pk-query ;

User hostc$ \ check for this hostname

: check-host? ( o addr u -- o addr' u flag )
    2 pick .host>$ ;

0 Value ?myself

: myhost= ( o -- flag )
    .host:id $@ host$ $@ str= ?myself and ;
    
: host= ( o -- flag )
    >o hostc$ $@ dup IF  host:id $@ str=  ELSE  2drop true  THEN  o> ;

: insert-addr ( o -- flag )
    connect( ." check addr: " dup .addr cr )  false swap
    [: check-addr1 0= IF  2drop EXIT  THEN
      insert-address temp-addr ins-dest
      connect( ." insert host: " temp-addr .addr-path cr )
      ret-addr $10 0 skip nip 0= IF
	  temp-addr ret-addr $10 move
      THEN  !0key  drop true ;] addr>sock ;

: insert-addr$ ( addr u -- flag )  dest-0key dest-0key> !
    new-addr dup insert-addr swap .net2o:dispose-addr ;

: insert-host ( addr u -- flag )  dest-0key dest-0key> !
    new-addr  dup host=  over myhost= 0= and  IF
	msg( ." insert: " dup .host:id $@ type cr )
	dup insert-addr  ELSE  false  THEN
    swap .net2o:dispose-addr ;

: insert-host? ( flag o addr u -- flag' o )
    3 pick IF  2drop  EXIT  THEN
    check-host? IF  insert-host  ELSE  2drop false  THEN
    rot or swap ;

: make-context ( pk u -- )
    ret0 net2o:new-context >o rdrop dest-pk ;

in net2o : pklookup? ( pkaddr u -- flag )
    2dup keysize2 safe/string hostc$ $! key2| 2dup pkc over str= to ?myself
    2dup >d#id { id }
    id .dht-host $[]# 0= IF  2dup pk-lookup  2dup >d#id to id  THEN
    2dup make-context
    false id dup .dht-host ['] insert-host? $[]map drop
    nip nip ;
in net2o : pklookup ( pkaddr u -- )
    net2o:pklookup? 0= !!no-address!! ;

: ?nat-done ( n -- )
    nat( ." req done, issue nat request" forth:cr )
    connect-rest +flow-control +resend ?nat ;
: no-nat-done ( n -- )
    nat( ." req done, finished" forth:cr )
    connect-rest +flow-control +resend ;
: direct-connect ( cmdlen datalen -- )
    cmd0( ." attempt to connect to: " return-addr .addr-path cr )
    ['] ?nat-done ['] no-nat-done ind-addr @ select rqd?
    net2o:connect nat( ." connected" forth:cr ) ;

: pk-connect ( addr u cmdlen datalen -- )
    2>r net2o:pklookup 2r> direct-connect ;
: pk-connect? ( addr u cmdlen datalen -- flag )
    2>r net2o:pklookup? dup IF   2r> direct-connect  ELSE  2rdrop  THEN ;

: addr-connect ( addr+key u cmdlen datalen xt -- )
    -rot 2>r >r over + 1- dup c@ dup >r -
    2dup u>= !!keysize!!
    dup r> make-context
    over - insert-addr$ 0= !!no-address!!
    r> execute 2r> net2o:connect ;

: nick-connect ( addr u cmdlen datalen -- )
    2>r host.nick>pk 2r> pk-connect ;

\ search keys

User search-key[]
User pings[]

: search-keys ( -- )
    dht-connect
    net2o-code  expect-reply
    search-key[] [: $, dht-id dht-owner? end-with ;] $[]map
    cookie+request end-code| ;

: search-addrs ( -- )
    dht-connect
    net2o-code  expect-reply
    search-key[] [: $, dht-id dht-host? end-with ;] $[]map
    cookie+request end-code| ;

: insert-keys ( -- )
    defaultkey @ >storekey !
    import#dht import-type !
    search-key[] [: >d#id >o
      0 dht-owner $[]@ nip sigsize# u> IF
	  64#-1 key-read-offset 64!
	  [: 0 dht-owner $[]@ 2dup sigsize# - tuck type /string
	    dht-hash $. type ;] $tmp
	  key:nest-sig 0= IF  do-nestsig
	      perm%default ke-mask ! n:o>  ELSE  2drop  THEN
      THEN
      o> ;] $[]map ;

: send-ping ( addr u -- ) sigsize# - new-addr dup >r
    [: ret-addr $10 erase
	check-addr1 IF
	    2dup .address forth:cr
	    insert-address ret-addr ins-dest
	    net2o-code0 net2o-version $, version?
	    end-code
	ELSE  2drop  THEN ;] addr>sock
    r> .net2o:dispose-addr ;

: receive-pings ( -- )
    requests->0 ;

: dht-nick? ( pk u -- )
    dup 4 < IF  2drop  EXIT  THEN
    search-key[] $off search-key[] $+[]!
    search-keys insert-keys save-pubkeys ;

\ connect, disconnect debug

: dbg-connect ( -- )  connect( <info>
    ." connected from: " .con-id <default> cr ) ;
: dbg-disconnect ( -- ) connect( <info>
    ." disconnecting: " .con-id <default> cr ) ;
' dbg-connect IS do-connect
' dbg-disconnect IS do-disconnect

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
