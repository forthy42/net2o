\ distributed hash table                             16oct2013py

\ Copyright (C) 2013   Bernd Paysan

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

\ For speed reasons, the DHT is in-memory
\ we may keep a log of changes on disk if we want persistence
\ might not be saved too frequently... robustness comes from distribution
\ This is actually a PHT, a prefix hash tree; base 256 (bytes)

$200 cells Constant dht-size# \ $100 entris + $100 chains

Variable d#public

: dht@ ( bucket -- addr )  >r
    r@ @ 0= IF  dht-size# allocate throw dup r> ! dup dht-size# erase
    ELSE  r> @  THEN ;

\ keys are enumerated small integers

: enum ( n -- n+1 )  dup Constant 1+ ;

0
enum k#hash     \ hash itself is item 0
enum k#peers    \ distribution list - includes "where did I get this from"
                \ managed by the hash owner himself
enum k#owner    \ owner(s) of the object (pubkey+signature)
enum k#host     \ network id+routing from there (+signature)
enum k#map      \ peers have those parts of the object
enum k#tags     \ tags added
\ most stuff is added as tag or tag:value pair
cells Constant k#size

cmd-class class
    field: dht-hash
    field: dht-peers
    field: dht-owner
    field: dht-host
    field: dht-map
    field: dht-tags
end-class dht-class

Variable dht-table

\ map primitives
\ map layout: offset, bitmap pairs (64 bits each)
\ string array: starts with base map (32kB per bit)

\ !!TBD!!

\ hash errors

s" invalid DHT key"              throwcode !!no-dht-key!!
s" DHT permission denied"        throwcode !!dht-permission!!
s" no signature"                 throwcode !!no-sig!!
s" invalid signature"            throwcode !!wrong-sig!!

\ checks for signatures

: gen>host ( addr u -- addr u )
    2dup c:0key "host" >keyed-hash ;

: >delete ( addr u type u2 -- addr u )
    "delete" >keyed-hash ;
: >host ( addr u -- addr u )  dup sigsize# u< !!no-sig!!
    c:0key 2dup sigsize# - "host" >keyed-hash ; \ hash from address

: verify-host ( addr u -- addr u flag )
    dht-hash $@ drop date-sig? ;

\ revokation

4 datesize# + keysize 9 * + Constant revsize#

Variable revtoken

: 0oldkey ( -- ) \ pubkeys can stay
    oldskc keysize erase  oldskrev keysize erase ;

: keymove ( addr1 addr2 -- )  keysize move ;

: revoke-verify ( addr u1 pk string u2 -- addr u flag ) rot >r 2>r c:0key
    sigonlysize# - 2dup 2r> >keyed-hash
    sigdate +date
    2dup + r> ed-verify ;

: >revoke ( skrev -- )  skrev keymove  check-rev? 0= !!not-my-revsk!! ;

: +revsign ( sk pk -- )  sksig -rot ed-sign revtoken $+! bl revtoken c$+! ;

: sign-token, ( sk pk string u2 -- )
    c:0key revtoken $@ 2swap >keyed-hash
    sigdate +date +revsign ;

: revoke-key ( -- addr u )
    skc oldskc keymove  pkc oldpkc keymove  skrev oldskrev keymove
                                           \ backup keys
    oldskrev oldpkrev sk>pk                \ generate revokation pubkey
    gen-keys                               \ generate new keys
    pkc keysize 2* revtoken $!             \ my new key
    oldpkrev keysize revtoken $+!          \ revoke token
    oldskrev oldpkrev "revoke" sign-token, \ revoke signature
    skc pkc "selfsign" sign-token,         \ self signed with new key
    "!" revtoken 0 $ins                    \ "!" + oldkeylen+newkeylen to flag revokation
    revtoken $@ gen>host 2drop             \ sign host information with old key
    sigdate +date sigdate datesize# revtoken $+!
    oldskc oldpkc +revsign
    0oldkey revtoken $@ ;

: revoke? ( addr u -- addr u flag )
    2dup 1 umin "!" str= over revsize# = and &&    \ verify size and prefix
    >host verify-host &&                           \ verify it's a proper host
    2dup + sigsize# - sigdate datesize# move       \ copy signing date
    2dup 1 /string sigsize# -                      \ extract actual revoke part
    over "selfsign" revoke-verify &&'              \ verify self signature
    over keysize 2* + "revoke" revoke-verify &&'   \ verify revoke signature
    over keysize 2* + pkrev keymove
    pkrev dup sk-mask  dht-hash $@ drop keysize +  keypad ed-dh
    dht-hash $@ drop keysize str= nip nip ;       \ verify revoke token

: .revoke ( addr u -- )
    ." new key: " 2dup 1 /string 2dup + 1- c@ 2* umin 85type space
    revoke? -rot .sigdates .check ;

\ higher level checks

: check-host ( addr u -- addr u )
    over c@ '!' = IF  revoke?  ELSE  >host verify-host  THEN
    0= !!wrong-sig!! ;
: >tag ( addr u -- addr u )
    dup sigpksize# u< !!no-sig!!
    c:0key dht-hash $@ "tag" >keyed-hash
    2dup sigpksize# - ':' $split 2swap >keyed-hash ;
: verify-tag ( addr u -- addr u flag )
    2dup + sigpksize# - date-sig? ;
: check-tag ( addr u -- addr u )
    >tag verify-tag 0= !!wrong-sig!! ;
: delete-tag? ( addr u -- addr u flag )
    >tag "tag" >delete verify-tag ;
: delete-host? ( addr u -- addr u flag )
    >host "host" >delete verify-host ;

\ some hash storage primitives

: d#? ( addrkey u bucket -- addr u bucket/0 )
    dup @ 0= ?EXIT
    >r 2dup r@ @ .dht-hash $@ str= IF  r> EXIT  THEN
    rdrop false ;

: d# ( addr u hash -- bucket ) { hash }
    2dup bounds ?DO
	I c@ cells hash dht@ + d#? ?dup-IF
	    nip nip UNLOOP  EXIT  THEN
	I c@ $100 + cells hash dht@ + to hash
    LOOP  true abort" dht exhausted - this should not happen" ;

: $ins[]sig ( addr u $array -- )
    \G insert O(log(n)) into pre-sorted array
    { $arr } 0 $arr $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup sigsize# - $# $arr $[]@ sigsize# - compare dup 0= IF
		drop
		2dup startdate@
		$# $arr $[]@ startdate@ 64u>=
		IF   $# $arr $[]!
		ELSE  2drop  THEN EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    0 { w^ ins$0 } ins$0 cell $arr r@ cells $ins r> $arr $[]! ;
: $del[]sig ( addr u $arrrray -- )
    \G delete O(log(n)) from pre-sorted array, check sigs
    { $arr } 0 $arr $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup sigonlysize# - $# $arr $[]@ sigonlysize# -
	    compare dup 0= IF
		$# $arr $[] $off
		$arr $# cells cell $del
		2drop EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT 2drop 2drop ; \ not found

: >d#id ( addr u -- o )
    2dup d#public d#
    dup @ 0= IF  dht-class new >o
	o swap !  dht-hash $!  dht-table @ token-table !  o o>
    ELSE  @ nip nip  THEN ;
: .tag ( addr u -- ) 2dup 2>r 
    >tag verify-tag >r sigpksize# - type r> 2r> .sigdates .check ;
: .host ( addr u -- ) over c@ '!' = IF  .revoke  EXIT  THEN  2dup 2>r
    >host 2dup + sigonlysize# - dht-hash $@ drop ed-verify >r sigsize# - .ipaddr
    r> 2r> .sigdates .check ;
: host>$ ( addr u -- addr u' flag )
    >host 2dup + sigonlysize# - dht-hash $@ drop ed-verify >r sigsize# -
    r> ;
: d#. ( -- )
    dht-hash $@ 85type ." :" cr
    k#size cell DO
	I cell/ 0 .r ." : "
	dht-hash I +  I k#host cells = IF
	    [: cr .host ." ," ;]
	ELSE
	    [: cr .tag ." , " ;]
	THEN $[]map cr
    cell +LOOP ;

: d#host+ ( addr u -- ) \ with sanity checks
    check-host dht-host $ins[]sig dht( d#. ) ;
: d#tags+ ( addr u -- ) \ with sanity checks
    check-tag dht-tags $ins[]sig dht( d#. ) ;
: d#host- ( addr u -- ) \ with sanity checks
    delete-host? IF  dht-host $del[]sig dht( d#. )  ELSE  2drop  THEN ;
: d#tags- ( addr u -- ) \ with sanity checks
    delete-tag?  IF  dht-tags $del[]sig dht( d#. )  ELSE  2drop  THEN ;

\ commands for DHT

get-current also net2o-base definitions

$33 net2o: dht-id ( $:string -- o:o )
    $> >d#id dht( ." set dht to: " dup hex. F cr ) n:>o ;
\g set dht id for further operations on it
dht-table >table

reply-table $@ inherit-table dht-table

:noname dht-hash $@ $, dht-id ; dht-class to start-req
net2o' emit net2o: dht-host+ ( $:string -- ) $> d#host+ ;
+net2o: dht-host- ( $:string -- ) $> d#host- ;
+net2o: dht-tags+ ( $:string -- ) $> d#tags+ ;
+net2o: dht-tags- ( $:string -- ) $> d#tags- ;

set-current

\ queries

: d#host? ( -- )  dht-host
    [: dup $A0 + maxstring < IF  $, dht-host+  ELSE  2drop  THEN ;] $[]map ;
: d#tags? ( -- )  dht-tags
    [: dup $A0 + maxstring < IF  $, dht-tags+  ELSE  2drop  THEN ;] $[]map ;

fs-class class
    field: dht-queries
end-class dht-file-class

: d#c, ( addr u c -- addr' u' ) -rot xc!+? drop ; 
: d#$, ( addr1 u1 addr2 u2 -- addr' u' )
    2swap 2 pick d#c, 2swap
    2over rot umin dup >r move r> /string ;
: d#id, ( addr u -- addr' u' )
    0 d#c, dht-hash $@ d#$, ;
: d#values, ( addr u mask -- addr' u' ) { mask }
    k#size cell/ 1 DO
	mask 1 and IF
	    I dup cells dht-hash dht( ." access dht: " dup hex. over . F cr ) +
	    [: { k# a# u# } k# d#c, a# u# d#$, k# ;] $[]map drop
	THEN  mask 2/ to mask
    LOOP ;

:noname $FFFFFFFF n>64 64dup fs-limit 64! fs-size 64! ; dht-file-class to fs-open
:noname ( addr u -- n )  dup >r
    dht-queries $@ bounds ?DO
	I 1+ I c@ 2dup >d#id >o + c@ >r
	d#id, r> d#values, o>
    I c@ 2 + +LOOP  nip r> swap - ; dht-file-class to fs-read

: new>dht ( -- )
    [: dht-file-class new { w^ fs-ins } fs-ins cell file-state $+! drop ;]
    filestate-lock c-section ;

: d#open ( fid -- )  new>dht lastfile@ .fs-open ;
: d#query ( addr u mask fid -- )  state-addr >o
    >r dup dht-queries c$+! dht-queries $+! r> dht-queries c$+! o> ;

get-current definitions

+net2o: dht-host? ( -- ) d#host? ;
+net2o: dht-tags? ( -- ) d#tags? ;
\ +net2o: dht-open ( fid -- ) 64>n d#open ;
\ +net2o: dht-query ( addr u mask fid -- ) 2*64>n d#query ;

previous set-current

\ value reading requires constructing answer packet

gen-table $freeze
' context-table is gen-table

\ facility stuff

: host$ ( addr u -- hostaddr host-u ) [: type .sig ;] $tmp ;
: gen-host ( addr u -- addr' u' )
    gen>host host$ ;
: gen-host-del ( addr u -- addr' u' )
    gen>host "host" >delete host$ ;

: gen>tag ( addr u hash-addr uh -- addr u )
    c:0key "tag" >keyed-hash
    2dup ':' $split 2swap >keyed-hash ;
: tag$ ( addr u -- tagaddr tag-u ) [: type .pk .sig ;] $tmp ;
: hash-sig ( addr u -- sig u )
    c:0key c:hash [: .pk .sig ;] $tmp ;

: gen-tag ( addr u hash-addr uh -- addr' u' )
    gen>tag tag$ ;
: gen-tag-del ( addr u hash-addr uh -- addr' u' )
    gen>tag "tag" >delete tag$ ;

\ addme stuff

also net2o-base

: pub? ( addr u -- addr u flag )  skip-symname
    over c@ '2' = IF  dup $17 u<=  ELSE  false  THEN ;

false Value add-myip

: addme-end ( -- )
    add-myip IF
	my-ip$ [: gen-host $, dht-host+ ;] $[]map
    THEN
    endwith request,  end-cmd
    ['] end-cmd IS expect-reply? ;
: addme ( addr u -- ) 2dup .iperr
    pub? IF
	my-ip-merge IF  2drop  EXIT  THEN
	my-ip$ $ins[]  EXIT  THEN
\    2dup my-ip? 0= IF  2dup my-ip$ $ins[]  THEN
    now>never
    what's expect-reply? ['] addme-end <> IF
	expect-reply pkc keysize 2* $, dht-id
    THEN
    gen-host $, dht-host+
    ['] addme-end IS expect-reply? ;
previous

: +addme ['] addme setip-xt ! ;
: -setip ['] .iperr setip-xt ! ;

\ replace me stuff

also net2o-base
: replace-me, ( -- )
    pkc keysize 2* $, dht-id dht-host? endwith ;

: remove-me, ( -- )
    dht-host dup >r
    [: sigsize# - 2dup + sigdate datesize# move
      gen-host-del $, dht-host- ;] $[]map
    r> $[]off ;
previous

: me>d#id ( -- ) pkc keysize 2* >d#id ;

: n2o:send-replace ( -- )
    me>d#id >o dht-host $[]# IF
	net2o-code   expect-reply
	  pkc keysize 2* $, dht-id remove-me, endwith
	  cookie+request
	end-code|
    THEN o> ;

: set-revocation ( addr u -- )
    dht-host $ins[]sig ;

Defer renew-key

: n2o:send-revoke ( addr u -- )
    keysize <> !!keysize!! >revoke
    me>d#id >o
    net2o-code  expect-reply
      dht-hash $@ $, dht-id remove-me,
      revoke-key 2dup set-revocation
      2dup $, dht-host+ endwith
      cookie+request
    end-code| \ send revocation upstrem
    dht-hash $@ renew-key drop o> ; \ replace key in key storage

: replace-me ( -- )  +addme
    net2o-code   expect-reply get-ip replace-me, cookie+request
    end-code| -setip
    n2o:send-replace ;

: revoke-me ( addr u -- )
    \G give it your revocation secret
    +addme
    net2o-code   expect-reply replace-me, cookie+request  end-code|
    -setip n2o:send-revoke ;

: do-disconnect ( -- )
    net2o-code log .time s" Disconnect" $, type cr endwith
      close-all disconnect  end-code msg( ." disconnected" F cr )
    n2o:dispose-context msg( ." Disposed context" F cr ) ;

: beacon-replace ( -- )  \ sign on, and do a replace-me
    sockaddr alen @ save-mem
    [: over >r insert-address r> free throw
      n2o:new-context $1000 $1000 n2o:connect msg( ." beacon: connected" F cr )
      replace-me msg( ." beacon: replaced" F cr )
      do-disconnect ;] 3 net2o-task drop ;

\ beacon handling

:noname ( char -- )
    case '?' of \ if we don't know that address, send a reply
	    replace-beacon( true )else( sockaddr alen @ 2dup routes #key -1 = ) IF
		beacon( ." Send reply to: " sockaddr alen @ .address F cr )
		net2o-sock fileno s" !" 0 sockaddr alen @ sendto +send
	    THEN
	endof
	'!' of \ I got a reply, my address is unknown
	    beacon( ." Got reply: " sockaddr alen @ .address F cr )
	    sockaddr alen @ false beacons [: rot >r 2over str= r> or ;] $[]map
	    IF
		beacon( ." Try replace" cr )
		beacon-replace
	    THEN
	    2drop
	endof
    endcase ; is handle-beacon

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
     (("[:" "net2o-code") (0 . 1) (0 . 1) immediate)
     ((";]" "end-code" "end-code|") (-1 . 0) (0 . -1) immediate)
    )
End:
[THEN]