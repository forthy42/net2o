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
	ind-addr @ 0= IF  dest-0key< sec@ lastaddr# cell+ $!  THEN
	dest-0key> @ IF  dest-0key< sec@ dest-0key> @ sec!  THEN
    THEN ;

: dhtroot ( -- )
    dhtroot-addr@ ?dup-IF  0 swap
	[: dup ?EXIT
	  check-addr1 IF  insert-address nip
	  ELSE  2drop  THEN ;] addr>sock
    ELSE  net2o-host $@ net2o-port insert-ip
    THEN  return-addr dup $10 erase be!
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

: dht-connect' ( xt -- ) >r
    $8 $8 dhtnick $@ nick>pk dhtroot r> execute pk:connect ;
: dht-connect ( -- )  ['] noop dht-connect' ;

Variable announced
: subme ( -- )  announced @ IF
	dht-connect sub-me disconnect-me  THEN ;

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

true Value connected?

Forward dht-beacon
Forward renat-all

[IFDEF] android     require android/net.fs  [ELSE]
    [IFDEF] PF_NETLINK  require linux/net.fs    [THEN]
[THEN]

\ announce and renat

event: :>renat ( -- )  renat-all ;
: dht-beacon <event :>renat main-up@ event> 2drop ;

: announce-me ( -- )
    tick-adjust 64@ 64-0= IF  +get-time  THEN
    beacons @ IF  dht-connect
    ELSE  [: return-addr be@ ['] dht-beacon 0 .add-beacon ;] dht-connect'
    THEN
    replace-me disconnect-me -other  announced on ;

: renat-all ( -- ) beacon( ." remove all beacons" cr )
    [IFDEF] renat-complete [: [THEN]
    beacons #offs !my-addr announce-me renat
    [IFDEF] renat-complete ;] catch renat-complete throw [THEN]
    beacon( ." done renat" cr ) ;

scope{ /chat
: renat ( addr u -- ) 2drop renat-all ;
}scope

\ beacon handling

event: :>do-beacon ( addr -- )
    beacon( ." :>do-beacon" forth:cr )
    { beacon } beacon cell+ $@ 1 64s /string bounds ?DO
	beacon $@ I 2@ .execute
    2 cells +LOOP ;

: do-beacon ( addr -- )  \ sign on, and do a replace-me
    <event elit, :>do-beacon ?query-task event> ;

: ?-beacon ( has-hash -- )
    \G if we don't know that address, send a reply
    invert need-beacon# @ and ?EXIT
    net2o-sock
    sockaddr alen @ routes# #key -1 = IF  s" !"  ELSE  s" ."  THEN
    beacon( ticks .ticks ."  Send '" 2dup type ." ' reply to: " sockaddr alen @ .address forth:cr )
    0 sockaddr alen @ sendto drop +send ;
: !-beacon ( has-hash -- ) drop
    \G I got a reply, my address is unknown
    beacon( ticks .ticks ."  Got unknown reply: " sockaddr alen @ .address forth:cr )
    sockaddr alen @ beacons #@ d0<> IF  last# do-beacon  THEN ;
: .-beacon ( has-hash -- ) drop
    \G I got a reply, my address is known
    beacon( ticks .ticks ."  Got known reply: " sockaddr alen @ .address forth:cr )
    sockaddr alen @ beacons #@ IF
	>r r@ 64@ ticks 64umin beacon-ticks# 64+ r> 64!
    THEN ;
: >-beacon ( has-hash -- ) drop
    \G I got a punch
    nat( ticks .ticks ."  Got punch: " sockaddr alen @ .address forth:cr ) ;

: handle-beacon ( has-hash char -- )
    case
	'?' of  ?-beacon  endof
	'!' of  !-beacon  endof
	'.' of  .-beacon  endof
	'>' of  >-beacon  endof
    endcase ;

Variable my-beacon

: my-beacon-hash ( -- hash u )
    my-beacon $@ dup ?EXIT  2drop
    my-0key sec@ "beacon" keyed-hash#128 2/ my-beacon $!
    my-beacon $@ ;

: check-beacon-hash ( addr u -- flag )
    my-beacon-hash str= ;

: handle-beacon+hash ( addr u -- )
    2dup over c@ >r 1 /string check-beacon-hash
    IF    true r> handle-beacon
	beacon( ." hashed by " 85type cr )else( 2drop )
    ELSE  false r> handle-beacon
	beacon( ticks .ticks ."  wrong beacon hash" cr )
	." wrong hash: " 85type ."  instead of " my-beacon $@ 85type cr
    THEN ; \ !!FIXME!! we ignore wrong hashes for now, until that is fixed

: replace-loop ( addr u -- flag )
    BEGIN  key2| >d#id >o dht-host $[]# IF  0 dht-host $[]@  ELSE  #0.  THEN o>
	2dup d0<> WHILE
	    over c@ '!' = WHILE
		replace-key o>
		connect( >o ke-pk $@ ." replace key: " 2dup 85type cr o o> )
		>r 2dup c:fetch-id r> >o  REPEAT  THEN  d0<> ;

: pk-query ( addr u xt -- flag ) >r
    dht-connect  2dup r> execute  replace-loop  disconnect-me ;

: pk-lookup ( addr u -- )
    ['] pk:fetch-host  ['] pk:addme-fetch-host  announced @ select
    pk-query 0= !!host-notfound!! ;

: pk-peek? ( pk u -- flag )  ['] pk:fetch-host pk-query ;

User hostc$ \ check for this hostname

: check-host? ( o addr u -- o addr' u flag )
    2 pick .host>$ ;

: host= ( o -- flag )
    >o hostc$ $@ dup IF  host-id $@ str=  ELSE  2drop true  THEN  o> ;

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
    new-addr  dup host=  IF
	msg( ." insert: " dup .host-id $@ type cr )
	dup insert-addr  ELSE  false  THEN
    swap .net2o:dispose-addr ;

: insert-host? ( flag o addr u -- flag' o )
    3 pick IF  2drop  EXIT  THEN
    check-host? IF  insert-host  ELSE  2drop false  THEN
    rot or swap ;

: make-context ( pk u -- )
    ret0 net2o:new-context >o rdrop dest-pk ;

in net2o : pklookup ( pkaddr u -- )
    2dup keysize2 safe/string hostc$ $! key2|
    2dup >d#id { id }
    id .dht-host $[]# 0= IF  2dup pk-lookup  2dup >d#id to id  THEN
    2dup make-context
    false id dup .dht-host ['] insert-host? $[]map drop
    0= !!no-address!!  2drop ;

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
    cookie+request end-code| disconnect-me ;

: search-addrs ( -- )
    dht-connect
    net2o-code  expect-reply
    search-key[] [: $, dht-id dht-host? end-with ;] $[]map
    cookie+request end-code| disconnect-me ;

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
