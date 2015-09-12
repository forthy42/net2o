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
    net2o-host $@ net2o-port insert-ip ;
: ins-ip4 ( -- net2oaddr )
    net2o-host $@ net2o-port insert-ip4 ;
: ins-ip6 ( -- net2oaddr )
    net2o-host $@ net2o-port insert-ip6 ;

: pk:connect ( code data key u ret -- )
    [: .time ." Connect to: " dup hex. cr ;] $err
    n2o:new-context >o rdrop o to connection  setup!
    dest-pk \ set our destination key
    n2o:connect
    +flow-control +resend
    [: .time ." Connected, o=" o hex. cr ;] $err ;

: dht-connect ( -- )
    $8 $8 dhtnick $@ nick>pk ins-ip pk:connect ;

: subme ( -- )
    pub-addr$ $[]# 0= ?EXIT  dht-connect sub-me disconnect-me ;

: c:disconnect ( -- ) [: ." Disconnecting..." cr ;] $err
    disconnect-me [: .packets profile( .times ) ;] $err ;

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

: announce-me ( -- )
    tick-adjust 64@ 64-0= IF  +get-time  THEN
    $8 $8 dhtnick $@ nick>pk ins-ip
    dup add-beacon pk:connect replace-me disconnect-me
    -other ;

: pk-lookup ( addr u -- )
    dht-connect
    2dup pk:addme-fetch-host
    BEGIN  >d#id >o 0 dht-host $[]@ o> 2dup d0= !!host-notfound!!
	over c@ '!' =  WHILE
	    replace-key o> >o ke-pk $@ ." replace key: " 2dup 85type cr
	    o o> >r 2dup c:fetch-id r> >o
    REPEAT  2drop disconnect-me ;

User host$ \ check for this hostname

: insert-host ( o addr u -- o )
    2 pick >o host>$ o> IF
	new-addr  dup .host-id $@
	host$ $@ str= host$ $@len 0= or IF
	    ." check addr: " dup .addr cr dup >r
	    [: check-addr1 0= IF  2drop  EXIT  THEN
	      insert-address temp-addr ins-dest
	      ." insert host: " temp-addr .addr-path cr
	      return-addr $10 0 skip nip 0= IF
		  temp-addr return-addr $10 move
	      THEN ;] addr>sock r>
	THEN
	>o n2o:dispose-addr o>
    ELSE  2drop  THEN ;

: n2o:pklookup ( addr u -- )
    2dup keysize2 /string host$ $! key2|
    2dup >d#id { id }
    id .dht-host $[]# 0= IF  2dup pk-lookup  2dup >d#id to id  THEN
    0 n2o:new-context >o rdrop 2dup dest-pk  return-addr $10 erase
    id dup .dht-host ['] insert-host $[]map drop 2drop ;

: search-connect ( key u -- o/0 )
    0 [: drop 2dup pubkey $@ str= o and  dup 0= ;] search-context
    nip nip  dup to connection ;

:noname ( addr u cmdlen datalen -- )
    2>r n2o:pklookup 2r>
    cmd0( ." attempt to connect to: " return-addr .addr-path cr )
    n2o:connect +flow-control +resend ; is pk-connect

: nick-connect ( addr u cmdlen datalen -- )
    2>r nick>pk 2r> pk-connect ;

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