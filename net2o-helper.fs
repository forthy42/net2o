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

Variable dhtnick "net2o-dhtroot" dhtnick $!

: ins-ip ( -- net2oaddr )
    net2o-host $@ net2o-port insert-ip  ind-addr off ;
: ins-ip4 ( -- net2oaddr )
    net2o-host $@ net2o-port insert-ip4 ind-addr off ;
: ins-ip6 ( -- net2oaddr )
    net2o-host $@ net2o-port insert-ip6 ind-addr off ;

: pk:connect ( code data key u ret -- )
    connect( [: .time ." Connect to: " dup hex. cr ;] $err )
    n2o:new-context >o rdrop o to connection  setup!
    dest-pk \ set our destination key
    +resend-cmd n2o:connect
    +flow-control +resend
    connect( [: .time ." Connected, o=" o hex. cr ;] $err ) ;

: dht-connect' ( xt -- ) >r
    $8 $8 dhtnick $@ nick>pk ins-ip r> execute pk:connect ;
: dht-connect ( -- )  ['] noop dht-connect' ;

: subme ( -- )
    pub-addr$ $[]# 0= ?EXIT  dht-connect sub-me disconnect-me ;

: c:disconnect ( -- ) connect( [: ." Disconnecting..." cr ;] $err )
    disconnect-me connect( [: .packets profile( .times ) ;] $err ) ;

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

\ NAT retraversal

Defer insert-addr ( o -- )

: renat ( -- )
    msg-groups [:
      cell+ $@ bounds ?DO
	  I @ >o ret-beacon ping-addrs
	  [: net2o-base:nop ret+beacon ;] punch-done-xt !
	  \ !!FIXME!! should maybe do a re-loopup?
	  ret-addr $10 erase
	  0 punch-addrs $[] @ insert-addr
	  o to connection
	  net2o-code new-punchload gen-punchload gen-punch end-code o>
      cell +LOOP
    ;] #map ;

Defer dht-beacon

false Value beacon-added?

: announce-me ( -- )
    tick-adjust 64@ 64-0= IF  +get-time  THEN
    beacon-added? IF  dht-connect
    ELSE  [: dup ['] dht-beacon add-beacon true to beacon-added? ;] dht-connect'
    THEN
    replace-me disconnect-me -other ;

: renat-all ( -- )  !my-addr announce-me renat ;

[IFDEF] android
    also android also jni
    :noname  defers android-network
	network-info ?dup-IF  ]xref renat-all  THEN ;
    is android-network
    previous previous
[THEN]

scope{ /chat
: renat ( addr u -- ) 2drop renat-all ;
}scope

event: ->renat ( -- )  renat-all ;
:noname <event ->renat [ up@ ]l event> ; is dht-beacon

\ beacon handling

event: ->do-beacon ( addr -- )
    beacon( ." ->do-beacon" forth:cr )
    { beacon } beacon cell+ $@ 1 64s /string bounds ?DO
	beacon $@ I perform
    cell +LOOP ;

: do-beacon ( addr -- )  \ sign on, and do a replace-me
    <event elit, ->do-beacon ?query-task event> ;

: ?-beacon ( -- )
    \G if we don't know that address, send a reply
    net2o-sock
    sockaddr alen @ routes #key -1 = IF  s" !"  ELSE  s" ."  THEN
    beacon( ticks .ticks ."  Send '" 2dup type ." ' reply to: " sockaddr alen @ .address forth:cr )
    0 sockaddr alen @ sendto +send ;
: !-beacon ( -- )
    \G I got a reply, my address is unknown
    beacon( ticks .ticks ."  Got unknown reply: " sockaddr alen @ .address forth:cr )
    sockaddr alen @ beacons #@ d0<> IF  last# do-beacon  THEN ;
: .-beacon ( -- )
    \G I got a reply, my address is known
    beacon( ticks .ticks ."  Got known reply: " sockaddr alen @ .address forth:cr )
    sockaddr alen @ beacons #@ IF
	>r r@ 64@ ticks 64umin beacon-ticks# 64+ r> 64!
    THEN ;
: >-beacon ( -- )
    \G I got a punch
    nat( ticks .ticks ."  Got punch: " sockaddr alen @ .address forth:cr ) ;

:noname ( char -- )
    case
	'?' of  ?-beacon  endof
	'!' of  !-beacon  endof
	'.' of  .-beacon  endof
	'>' of  >-beacon  endof
    endcase ; is handle-beacon

: replace-loop ( addr u -- flag )
    BEGIN  key2| >d#id >o dht-host $[]# IF  0 dht-host $[]@  ELSE  0.  THEN o>
	2dup d0<> WHILE
	    over c@ '!' = WHILE
		replace-key o>
		connect( >o ke-pk $@ ." replace key: " 2dup 85type cr o o> )
		>r 2dup c:fetch-id r> >o  REPEAT  THEN  d0<> ;

: pk-query ( addr u xt -- flag ) >r
    dht-connect  2dup r> execute  replace-loop  disconnect-me ;

: pk-lookup ( addr u -- )
    ['] pk:addme-fetch-host pk-query 0= !!host-notfound!! ;

:noname ( pk u -- flag )  ['] c:fetch-id pk-query ; is pk-peek?

User host$ \ check for this hostname

: check-host? ( o addr u -- o addr' u flag )
    2 pick >o host>$ o> ;

: host= ( o -- flag )
    host$ $@len IF  .host-id $@ host$ $@ str=  ELSE  drop true  THEN ;

:noname ( o -- )
    connect( ." check addr: " dup .addr cr )
    [: check-addr1 0= IF  2drop  EXIT  THEN
	insert-address temp-addr ins-dest
	connect( ." insert host: " temp-addr .addr-path cr )
	ret-addr $10 0 skip nip 0= IF
	    temp-addr ret-addr $10 move
	THEN ;] addr>sock ; is insert-addr

: insert-addr$ ( addr u -- )
    new-addr dup insert-addr .n2o:dispose-addr ;

: insert-host ( addr u -- )
    new-addr  dup host=  IF  dup insert-addr  THEN  .n2o:dispose-addr ;

: insert-host? ( o addr u -- o )
    check-host? IF  insert-host  ELSE  2drop  THEN ;

: make-context ( pk u -- )
    0 n2o:new-context >o rdrop dest-pk ;

: n2o:pklookup ( addr u -- )
    2dup keysize2 safe/string host$ $! key2|
    2dup >d#id { id }
    id .dht-host $[]# 0= IF  2dup pk-lookup  2dup >d#id to id  THEN
    2dup make-context
    id dup .dht-host ['] insert-host? $[]map drop 2drop ;

:noname ( addr u cmdlen datalen -- )
    2>r n2o:pklookup
    cmd0( ." attempt to connect to: " return-addr .addr-path cr )
    2r> n2o:connect +flow-control +resend ; is pk-connect

:noname ( addr+key u cmdlen datalen -- )
    2>r over + 1- dup c@ dup >r -
    2dup u>= !!keysize!!
    dup r> make-context
    over - insert-addr$
    2r> n2o:connect +flow-control +resend ; is addr-connect

: nick-connect ( addr u cmdlen datalen -- )
    2>r host.nick>pk 2r> pk-connect ;

\ search keys

User search-key$

: search-keys ( -- )
    dht-connect
    net2o-code  expect-reply
    search-key$ [: $, dht-id dht-owner? endwith ;] $[]map
    cookie+request end-code| disconnect-me ;

: insert-keys ( -- )
    defaultkey @ >storekey !
    import#dht import-type !
    search-key$ [: >d#id >o
      0 dht-owner $[]@ nip sigsize# u> IF
	  64#-1 key-read-offset 64!
	  [: 0 dht-owner $[]@ 2dup sigsize# - tuck type /string
	    dht-hash $. type ;] $tmp
	  key:nest-sig 0= IF  do-nestsig
	      perm%default ke-mask ! n:o>  ELSE  2drop  THEN
      THEN
      o> ;] $[]map ;

:noname ( pk u -- )
    dup 4 < IF  2drop  EXIT  THEN
    search-key$ $off search-key$ $+[]!
    search-keys insert-keys save-pubkeys ; is dht-nick?

\ connect, disconnect debug

: dbg-connect ( -- )  connect( <info>
    ." connected from: " pubkey $@ .key-id <default> cr ) ;
: dbg-disconnect ( -- ) connect( <info>
    ." disconnecting: " pubkey $@ .key-id <default> cr ) ;
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