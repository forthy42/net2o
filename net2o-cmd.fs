\ generic net2o command interpreter

\ Copyright (C) 2011-2014   Bernd Paysan

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

\ net2o commands are protobuf coded, not byte coded.

\ command helper

User buf-state cell uallot drop

[IFDEF] 64bit
    : zz>n ( zigzag -- n )
	dup 1 rshift swap 1 and negate xor ;
    : n>zz ( n -- zigzag )
	dup 0< swap 2* xor ;
[ELSE]
    : zz>n ( 64u -- 64n )
	64dup 1 64rshift 64swap 64>n 1 and negate n>64 64xor ;
    : n>zz ( 64n -- 64u )
	64dup 64-0< >r 64dup 64+ r> n>64 64xor ;
[THEN]
    
: ps!+ ( 64n addr -- addr' )
    >r n>zz r> p!+ ;
: ps@+ ( addr -- 64n addr' )
    p@+ >r zz>n r> ;

: p@ ( -- 64u ) buf-state 2@ over + >r p@+ r> over - buf-state 2! ;
: ps@ ( -- 64n ) p@ zz>n ;

: byte@ ( addr u -- addr' u' b )
    >r count r> 1- swap ;

: string@ ( -- addr u )
    buf-state 2@ over + >r
    p@+ [IFUNDEF] 64bit nip [THEN] swap 2dup + r> over - buf-state 2! ;

\ Command streams contain both commands and data
\ the dispatcher is a byte-wise dispatcher, though
\ commands come in junks of 8 bytes
\ Commands are zero-terminated

: net2o-crash hex[
    buf-state 2@ swap 8 u.r space 8 u.r ." :" buf-state 2@ drop 1- c@ 2 u.r cr
    ]hex  buf-state 2@ dump
    true !!function!! ;

Create cmd-base-table 256 0 [DO] ' net2o-crash , [LOOP]

: cmd@ ( -- u ) buf-state 2@ byte@ >r buf-state 2! r> ;

: (net2o-see) ( addr -- )  @
    dup ['] net2o-crash <> IF
	[ 4 cell = ] [IF]
	    5 cells - body>
	[ELSE]
	    4 cells - body>
	[THEN]
    THEN  .name ;

: printable? ( addr u -- flag )
    true -rot bounds ?DO  I c@ $7F and bl < IF  drop false  LEAVE  THEN  LOOP ;

: n2o.string ( addr u -- )
    2dup printable? IF
	.\" s\" " type
    ELSE
	.\" x\" " xtype
    THEN  .\" \" $, " ;

: .net2o-name ( n -- )
    dup $80 < IF
	cells cmd-base-table +
    ELSE
	dup 7 rshift $80 + cells cmd-base-table + @ >body
	swap $7F and cells +
    THEN  (net2o-see) ;

: net2o-see ( -- ) hex[
    case
	0 of  ." end-code" cr 0. buf-state 2!  endof
	1 of  p@ 64. ." lit, "  endof
	2 of  ps@ s64. ." slit, " endof
	3 of  string@  n2o.string  endof
	.net2o-name
	0 endcase ]hex ;

Variable show-offset  show-offset on

: cmd-see ( addr u -- addr' u' )
    dup show-offset @ = IF  ." <<< "  THEN
    buf-state 2! p@ 64>n net2o-see buf-state 2@ ;

: n2o:see ( addr u -- )  ." net2o-code " 
    BEGIN  cmd-see dup 0= UNTIL  2drop ;

: cmd-dispatch ( addr u -- addr' u' )
    byte@ >r buf-state 2! trace( r@ dup . .net2o-name .s cr )
    r> cells cmd-base-table + perform buf-state 2@ ;

: extend-cmds ( -- xt ) noname Create lastxt $100 0 DO ['] net2o-crash , LOOP
  DOES>  >r cmd@ cells r> + perform ;

User 'cmd-buf $10 cell- uallot drop

: >cmd ( xt u -- ) u>64 'cmd-buf p!+  'cmd-buf tuck -
    cmd-base-table >r
    BEGIN  dup 1 >  WHILE  over c@ >r 1 /string r>
	    cells r> + dup @ ['] net2o-crash = IF
		extend-cmds over !
	    THEN
	    @ >body >r
    REPEAT
    drop c@ cells r> + ! ;

Defer >throw

\ commands

Defer net2o-do
: net2o-exec  cell+ perform ;

: net2o-does  DOES> net2o-do ;
: net2o: ( number "name" -- )
    ['] noop over >cmd \ allocate space in table
    Create dup >r , here >r 0 , net2o-does noname :
    lastxt dup r> ! r> >cmd ;

: F also forth parse-name parser1 execute previous ; immediate

: un-cmd ( -- )  0. buf-state 2!  0 >o rdrop ;

Vocabulary net2o-base

get-current also net2o-base definitions previous

\ Command numbers preliminary and subject to change

0 net2o: dummy ( -- ) ; \ just as dummy... never used
0 net2o: end-cmd ( -- ) 0. buf-state 2! ;
1 net2o: ulit ( -- x ) p@ ;
2 net2o: slit ( -- x ) ps@ ;
3 net2o: string ( -- addr u )  string@ ;

dup set-current

\ net2o assembler

User cmd0source
User cmdbuf#

: cmdbuf     ( -- addr )  cmd0source @ dup 0= IF  drop code-dest  THEN ;
\ : cmdbuf#    ( -- addr )  cmd0source @ IF  cmd0buf#  ELSE  codebuf#  THEN ;
: cmdlock    ( -- addr )  cmd0source @ IF  cmd0lock  ELSE  code-lock  THEN ;
: cmdbuf$ ( -- addr u )   cmdbuf cmdbuf# @ ;
: endcmdbuf  ( -- addr' ) cmdbuf maxdata + ;
: n2o:see-me ( -- )
    buf-state 2@ 2>r
    ." see-me: " dest-addr 64@ ['] 64. $10 base-execute
    \ tag-addr dup hex. 2@ swap hex. hex. F cr
    inbuf packet-data n2o:see
    2r> buf-state 2! ;

: cmdreset  cmdbuf# off ;

: cmd, ( 64n -- )  cmdbuf$ + dup >r p!+ r> - cmdbuf# +! ;

: net2o, @ n>64 cmd, ;

: net2o-code    cmd0source off  cmdlock lock
    cmdreset ['] net2o, IS net2o-do also net2o-base ;
comp: :, also net2o-base ;
: net2o-code0   cmd0buf cmd0source !   cmdlock lock
    cmdreset ['] net2o, IS net2o-do also net2o-base ;
comp: :, also net2o-base ;
' net2o, IS net2o-do

: send-cmd ( addr u dest -- ) n64-swap { buf# }
    +send-cmd dest-addr 64@ 64>r
    cmd( ." send: " 64dup ['] 64. $10 base-execute 64>r
    dup buf# n2o:see cr 64r> )
    o IF  code-map  ELSE  0  THEN  code-packet !  ret-addr
    max-size^2 1+ 0 DO
	buf# min-size I lshift u<= IF
	    I sendX  cmdreset  UNLOOP
	    64r> dest-addr 64! EXIT  THEN
    LOOP  64r> dest-addr 64!  true !!commands!! ;

: net2o:punch ( addr u -- )
    $>sock insert-address  ret-addr ins-dest ;

: cmddest ( -- dest ) cmd0source @ IF  64#0  ELSE  code-vdest
    64dup 64-0= !!no-dest!! THEN ;

: cmd ( -- )  cmdbuf# @ 2 u< ?EXIT \ don't send if cmdbuf is empty
    cmdbuf cmdbuf# @ cmddest send-cmd
    cmd0source @ 0= IF  code+  THEN ;

also net2o-base

UDefer expect-reply?
' end-cmd IS expect-reply?

:noname  ['] end-cmd IS expect-reply? ; is init-reply

: cmd-send? ( -- )
    cmdbuf# @ IF  expect-reply? cmd  THEN ;

previous

: net2o:ok? ( -- )  o?
    tag-addr >r cmdbuf$ r@ 2!
    tag( ." tag: " tag-addr dup hex. 2@ swap hex. hex. F cr )
    code-vdest r> reply-dest 64! ;
: net2o:ok ( tag -- )
    timeout( ." ack: " dup hex. F cr )
    o 0= IF  drop EXIT  THEN
    resend0 @ IF  resend0 $off  THEN
    0. rot reply[] 2! ; \ clear request
: net2o:expect-reply ( -- )  o?
    timeout( ." expect: " cmdbuf$ n2o:see )
    cmdbuf$ code-reply dup >r 2! code-vdest r> reply-dest 64! ;

: tag-addr? ( -- flag )
    tag-addr dup >r 2@ dup IF
	cmd( dest-addr 64@ 64. ." resend canned code reply " tag-addr hex. cr )
	r> reply-dest 64@ send-cmd true
	1 packets2 +!
    ELSE  d0<> -1 0 r> 2!  THEN ;

Variable throwcount

: do-cmd-loop ( addr u -- )
    cmd( dest-addr 64@ ['] 64. $10 base-execute 2dup n2o:see )
    sp@ >r throwcount off
    [: BEGIN   cmd-dispatch  dup 0=  UNTIL ;] catch
    dup IF   1 throwcount +!
	[: ." do-cmd-loop: " dup . .exe cr DoError ;] $err nothrow
	buf-state @ show-offset !  n2o:see-me  show-offset on
	throwcount @ 4 < IF  >throw  THEN  THEN
    drop  r> sp! 2drop +cmd ;

: cmd-loop ( addr u -- )
    o IF
	cmd0source off
	tag-addr?  IF  2drop  >flyburst  1 packetr2 +!  EXIT  THEN
    ELSE
	cmd0buf cmd0source !
    THEN
    cmdreset  do-cmd-loop  cmd-send? ;

' cmd-loop is queue-command

\ nested commands

: >initbuf ( addr u -- addr' u' ) tuck
    init0buf mykey-salt# + swap move
    maxdata  BEGIN  2dup 2/ u<  WHILE  2/ dup min-size = UNTIL  THEN
    nip init0buf swap mykey-salt# + 2 64s + ;

4 Constant maxnest#
User neststart#
User neststack maxnest# cells uallot drop \ nest up to 10 levels

: @+ ( addr -- n addr' )  dup @ swap cell+ ;
: nest[ ( -- ) neststart# @ neststack @+ swap cells + !
    1 neststack +! neststack @ maxnest# u>= !!maxnest!!
    cmdbuf# @ neststart# ! ;

: cmd> ( -- addr u )
    init0buf mykey-salt# + maxdata 2/ erase
    cmdbuf$ neststart# @ safe/string neststart# @ cmdbuf# !
    -1 neststack +! neststack @ 0< !!minnest!!
    neststack @+ swap cells + @ neststart# ! ;

: cmd>nest ( -- addr u ) cmd> >initbuf 2dup mykey-encrypt$ ;
: cmd>tmpnest ( -- addr u )
    cmd> >initbuf 2dup tmpkey@ keysize umin encrypt$ ;

: do-nest ( addr u flag -- )
    buf-state 2@ 2>r validated @ >r  validated or!  do-cmd-loop
    r> validated !  2r> buf-state 2! ;

: cmdnest ( addr u -- )  mykey-decrypt$
    IF  own-crypt-val do-nest  ELSE  un-cmd  THEN ;

: cmdtmpnest ( addr u -- )
    $>align tmpkey@ drop keysize decrypt$
    IF  tmp-crypt-val do-nest  ELSE  un-cmd  THEN ;

\ net2o assembler stuff

also net2o-base definitions

: maxstring ( -- n )  endcmdbuf cmdbuf$ + - ;
: maxtiming ( -- n )  maxstring timestats - dup timestats mod - ;
: $, ( addr u -- )  string  >r r@ n>64 cmd,
    r@ maxstring u>= !!stringfit!!
    cmdbuf$ + r@ move   r> cmdbuf# +! ;
: lit, ( u -- )  ulit cmd, ;
: slit, ( n -- )  slit n>zz cmd, ;
: nlit, ( n -- )  n>64 slit, ;
: ulit, ( u -- )  u>64 lit, ;
: (end-code) ( -- ) expect-reply? cmd  cmdlock unlock ;
: end-code ( -- )  (end-code) previous ;
comp: :, previous ;
: push-cmd ( -- )
    end-cmd ['] end-cmd IS expect-reply? cmdbuf$ push-reply ;

dup set-current previous

[IFDEF] 64bit
    ' noop Alias 2*64>n immediate
    ' noop Alias 3*64>n immediate
[ELSE]
    : 2*64>n ( 64a 64b -- na nb ) 64>n >r 64>n r> ;
    : 3*64>n ( 64a 64b 64c -- na nb nc ) 64>n >r 64>n >r 64>n r> r> ;
[THEN]

\ commands to read and write files

also net2o-base definitions
4 net2o: emit ( xc -- ) 64>n xemit ;
5 net2o: type ( addr u -- )  F type ;
6 net2o: . ( -- ) 64. ;
7 net2o: cr ( -- ) F cr ;
\ Use ko instead of throw for not acknowledge (kudos to Heinz Schnitter)
8 net2o: ko ( error -- )  throw ;
9 net2o: see-me ( -- ) n2o:see-me ;

10 net2o: push-$    $, ;
11 net2o: push-slit slit, ;
12 net2o: push-char lit, ;
' push-char alias push-lit

13 net2o: push'     p@ cmd, ;
14 net2o: nest ( addr u -- )  cmdnest ;
15 net2o: tmpnest ( addr u -- )  cmdtmpnest ;

: ]nest  ( -- )  end-cmd cmd>nest $, push-$ push' nest ;
: ]tmpnest ( -- )  end-cmd cmd>tmpnest $, tmpnest ;

16 net2o: new-data ( addr addr u -- )
    o 0<> tmp-crypt? and own-crypt? or IF  64>n  n2o:new-data  EXIT  THEN
    64drop 64drop 64drop  un-cmd ;
17 net2o: new-code ( addr addr u -- )
    o 0<> tmp-crypt? and own-crypt? or IF  64>n  n2o:new-code  EXIT  THEN
    64drop 64drop 64drop  un-cmd ;
18 net2o: request-done ( -- )  own-crypt? IF  n2o:request-done  THEN ;
19 net2o: set-rtdelay ( timestamp -- )
    o IF  rtdelay!  EXIT  THEN
    own-crypt? IF
	64dup cookie>context?
	IF  >o rdrop
	    ticker 64@ recv-tick 64! rtdelay! \ time stamp of arrival
	    EXIT
	ELSE \ just check if timeout didn't expire
	    ticker 64@ connect-timeout# 64- 64u< 0= ?EXIT
	THEN
    ELSE  64drop  THEN  un-cmd ;

: n2o:create-map
    { 64: addrs ucode udata 64: addrd -- addrd ucode udata addrs }
    addrs lit, addrd lit, ucode ulit, new-code
    addrs ucode n>64 64+ lit, addrd ucode n>64 64+ lit, udata ulit, new-data
    addrd ucode udata addrs ;

20 net2o: store-key ( addr u -- )
    o 0= IF  ." don't store key, o=0: " .nnb F cr un-cmd  EXIT  THEN
    own-crypt? IF
	key( ." store key: o=" o hex. 2dup .nnb F cr )
	2dup do-keypad $!
	crypto-key $!
    ELSE  ." don't store key: o=" o hex. .nnb F cr  THEN ;

21 net2o: map-request ( addrs ucode udata -- )  2*64>n
    nest[
    ?new-mykey ticker 64@ lit, set-rtdelay
    max-data# umin swap max-code# umin swap
    2dup + n2o:new-map n2o:create-map
    keypad keysize $, store-key  stskc KEYSIZE erase
    ]nest  n2o:create-map  neststack @ IF  ]tmpnest  THEN
    64drop 2drop 64drop ;

22 net2o: disconnect ( -- )  o 0= ?EXIT n2o:dispose-context un-cmd ;
23 net2o: set-tick ( ticks -- )  adjust-ticks ;
24 net2o: get-tick ( -- )  ticks lit, set-tick ;

net2o-base

30 net2o: open-file ( addr u mode id -- )  2*64>n  n2o:open-file ;
31 net2o: close-file ( id -- )  64>n n2o:close-file ;
32 net2o: file-size ( id -- size )  id>addr? fs-size 64@ ;
33 net2o: send-chunks ( -- ) net2o:send-chunks ;
34 net2o: set-blocksize ( n -- )  64>n blocksize! ;
35 net2o: set-blockalign ( n -- )  64>n pow2?  blockalign ! ;
36 net2o: close-all ( -- )  n2o:close-all ;

: blocksize! ( n -- )  dup ulit, set-blocksize blocksize! ;
: blockalign! ( n -- )  dup ulit, set-blockalign pow2? blockalign ! ;

\ flow control functions

40 net2o: ack-addrtime ( time addr -- ) net2o:ack-addrtime ;
41 net2o: ack-resend ( flag -- ) 64>n  net2o:ack-resend ;
42 net2o: set-rate ( ticks1 ticks2 -- )
    cookie? IF  net2o:set-rate
    ELSE  64drop 64drop ns/burst dup @ 2* 2* swap !  THEN ;
43 net2o: resend-mask ( addr mask -- )
    2*64>n net2o:resend-mask net2o:send-chunks ;
44 net2o: track-timing ( -- )  net2o:track-timing ;
45 net2o: rec-timing ( addr u -- )  net2o:rec-timing ;
46 net2o: send-timing ( -- )
    net2o:timing$ maxtiming umin tuck $,
    net2o:/timing rec-timing ;
47 net2o: >time-offset ( n -- )  time-offset 64! ;
: time-offset! ( -- )  ticks 64dup lit, >time-offset time-offset 64! ;
48 net2o: ack-b2btime ( time addr -- ) net2o:ack-b2btime ;
49 net2o: ack-cookies ( cookie addr mask -- )
    [IFUNDEF] 64bit 64>r 64>n 64r> [THEN]
    data-map @ cookie+ 64over 64over 64= 0= IF
	." cookies don't match!" 64over .16 space 64dup .16 F cr
    THEN
    64= cookie-val and validated or! ;
50 net2o: ack-flush ( addr -- )  64>n net2o:rewind-sender-partial ;
51 net2o: set-head ( offset -- ) 64>n data-rmap @ >o dest-head umax! o> ;
52 net2o: timeout ( ticks -- ) net2o:timeout  data-map @ >o dest-tail @ o> ulit, set-head ;
53 net2o: ok ( tag -- ) 64>n net2o:ok ;
54 net2o: ok? ( tag -- ) net2o:ok? lit, ok ;
55 net2o: set-top ( top flag -- ) 2*64>n
    data-rmap @ >o dest-end ! dest-top! o> ;

\ crypto functions

60 net2o: receive-key ( addr u -- )
    crypt( ." Received key: " tmpkey@ .nnb F cr )
    tmp-crypt? IF  net2o:receive-key  ELSE  2drop  THEN ;
61 net2o: key-request ( -- addr u )
    crypt( ." Nested key: " tmpkey@ .nnb F cr )
    nest[ pkc keysize $, receive-key ;
62 net2o: receive-tmpkey ( addr u -- ) net2o:receive-tmpkey ;
63 net2o: tmpkey-request ( -- ) stpkc keysize $, receive-tmpkey ;
64 net2o: update-key ( -- )  net2o:update-key ;
65 net2o: gen-ivs ( addr u -- )  ivs-strings receive-ivs ;

\ create commands to send back

: all-ivs ( -- ) \ Seed and gen all IVS
    state# rng$ 2dup $, gen-ivs ivs-strings send-ivs ;

66 net2o: gen-reply ( -- )
    [: crypt( ." Reply key: " tmpkey@ .nnb F cr )
      nest[ pkc keysize $, receive-key update-key all-ivs time-offset! ]tmpnest
      push-cmd ;]  IS expect-reply? ;

\ better slurping

70 net2o: track-size ( size id -- )
    64>n track( >r ." file <" r@ 0 .r ." > size: " 64dup 64. F cr r> ) size! ;
71 net2o: track-seek ( seek id -- )
    64>n track( >r ." file <" r@ 0 .r ." > seek: " 64dup 64. F cr r> ) seekto! ;
72 net2o: track-limit ( seek id -- )
    64>n track( >r ." file <" r@ 0 .r ." > seek to: " 64dup 64. F cr r> ) limit! ;

:noname ( id seek -- ) lit, ulit, track-seek ; is do-track-seek

73 net2o: set-stat ( mtime mod id -- ) 2*64>n n2o:set-stat ;
74 net2o: get-stat ( id -- ) 64>n { fd }
    fd n2o:get-stat >r lit, r> ulit, fd ulit, set-stat ;
75 net2o: open-tracked-file ( addr u mode id -- )
    2*64>n dup >r n2o:open-file
    r@ id>addr? >o fs-size 64@ o> lit, r@ ulit, track-size
    r@ n2o:get-stat >r lit, r> ulit, r> ulit, set-stat ;
76 net2o: slurp ( -- )
    n2o:slurp swap ulit, slit, set-top
    ['] do-track-seek n2o:track-all-seeks ;
77 net2o: rewind-sender ( n -- )  64>n net2o:rewind-sender ;

\ ids 100..120 reserved for key exchange/strage

\ profiling, nat traversal

120 net2o: !time ( -- ) init-timer ;
121 net2o: .time ( -- ) .packets .times ;
122 net2o: set-ip ( addr u -- ) setip-xt perform ;
123 net2o: get-ip ( -- ) >sockaddr $, set-ip [: $, set-ip ;] n2oaddrs ;
124 net2o: punch ( addr u -- )  net2o:punch ;

: net2o:gen-resend ( -- )
    recv-flag @ invert resend-toggle# and ulit, ack-resend ;
: net2o:ackflush ( n -- ) ulit, ack-flush ;
: n2o:done ( -- )  slurp ;

: rewind-total ( -- )
    data-rmap @ >o dest-round @ 1+ o> dup net2o:rewind-receiver
    ulit, rewind-sender ;

: rewind ( -- )
    save( data-rmap @ >o dest-back @ do-slurp @ umax o> net2o:ackflush )else(
    rewind-total ) ;

\ ids 130..140 reserved for DHT

\ safe initialization

net2o-base

: lit<   lit, push-lit ;
: slit<  slit, push-slit ;
:noname
    server? IF
	dup  IF  dup nlit, ko end-cmd
	    ['] end-cmd IS expect-reply? (end-code)  THEN
	throw  THEN  drop ; IS >throw

set-current previous

also net2o-base

: n2o:copy ( addrsrc us addrdest ud -- )
    2swap $, r/o ulit, file-reg# @ ulit, open-tracked-file
    file-reg# @ save-to
    1 file-reg# +! ;

: n2o:seek ( pos id -- )
    2dup state-addr fs-seek !  swap ulit, ulit, track-seek ;

file-reg# off

previous

\ client side timing

: ack-size ( -- )  1 acks +!
    recv-tick 64@ 64dup lastb-ticks 64!@ 64- max-dticks 64max! ;
: ack-first ( -- )
    lastb-ticks 64@ firstb-ticks 64@ 64- delta-ticks 64+!
    recv-tick 64@ 64dup firstb-ticks 64!  64dup lastb-ticks 64!
    last-rtick 64!  recv-addr 64@ last-raddr 64! ;

: ack-timing ( n -- )
    b2b-toggle# and  IF  ack-first  ELSE  ack-size  THEN ;

also net2o-base

: setrate-limit ( rate -- rate' )
    \ do not change requested rate by more than a factor 2
    last-rate 64@
    64dup 64-0<> IF  64tuck 64-2* 64min 64swap 64-2/ 64max  ELSE  64drop  THEN
    64dup last-rate 64! ;

: >rate ( -- )  delta-ticks 64@ 64-0= acks @ 0= or ?EXIT
    recv-tick 64@ 64dup burst-ticks 64!@ 64dup 64-0<> IF
	64- max-dticks 64@ tick-init 1+ n>64 64* 64max 64>r
	delta-ticks 64@ tick-init 1+ acks @ 64*/ setrate-limit
	lit, 64r> lit, set-rate
    ELSE
	64drop 64drop
    THEN
    delta-ticks 64off  max-dticks 64off  acks off ;

: net2o:acktime ( -- )
    recv-addr 64@ recv-tick 64@ time-offset 64@ 64-
    timing( 64>r 64dup ['] 64. $10 base-execute 64r> 64dup 64. ." acktime" F cr )
    lit, lit, ack-addrtime ;
: net2o:b2btime ( -- )
    last-raddr 64@ last-rtick 64@ 64dup 64-0=
    IF  64drop 64drop
    ELSE  time-offset 64@ 64- lit, lit, ack-b2btime  THEN ;

\ ack bits, new code

: ack-cookie, ( map n bits -- ) >r [ 8 cells ]L * maxdata * r>
    2dup 2>r rot >r u>64 r> cookie+ cookie( ." cookie, " 64dup .16 space )
    lit, 2r> swap cookie( 2dup hex. hex. F cr ) ulit, ulit, ack-cookies ;

: net2o:ack-cookies ( -- )  data-rmap @ { map }
    map >o data-ackbits-buf $@ o>
    bounds ?DO  map I 2@ swap ack-cookie,  2 cells +LOOP
    clear-cookies ;

\ client side acknowledge

: net2o:genack ( -- )
    net2o:ack-cookies  net2o:b2btime  net2o:acktime  >rate ;

: !rdata-tail ( -- )
    data-rmap @ >o
    data-ack# @ bytes>addr dest-top 2@ umin umin dup dest-tail !@ o>
    save( u> IF  save&  THEN )else( 2drop ) ;
: receive-flag ( -- flag )  recv-flag @ resend-toggle# and 0<> ;

4 Value max-resend#

: net2o:do-resend ( flag -- )
    o 0= IF  drop EXIT  THEN  data-rmap @ 0= IF  drop EXIT  THEN
    data-rmap @ >o
    data-ackbits @ dest-size @ addr>bytes 1- { acks ackm }
    IF    data-reack# @ mask-bits# -
    ELSE  dest-head @ 1- addr>bits  THEN  bits>bytes 0 max
    0 swap data-ack# @
    \ save( ." resend: " dest-head @ hex. dest-back @ hex.
    \ 2dup hex. hex. acks ackm 1+ xtype F cr )
    o> +DO
	acks I ackm and + l@
	dup $FFFFFFFF <> IF
	    resend( ." resend: " dup hex. over hex. F cr )
	    I ackm and bytes>addr ulit, $FFFFFFFF xor ulit, resend-mask  1+
	ELSE
	    drop dup 0= IF  I 4 + data-rmap @ >o data-ack# ! o>  THEN
	THEN
	dup max-resend# >= ?LEAVE \ no more than x resends
    4 +LOOP  drop !rdata-tail ;

: do-expect-reply ( -- )
    reply-index ulit, ok?  end-cmd  net2o:expect-reply
    msg( ." Expect reply" F cr )
    ['] end-cmd IS expect-reply? ;

: expect-reply ( -- ) ['] do-expect-reply IS expect-reply? ;

: resend-all ( -- )
    false net2o:do-resend ;

: restart-transfer ( -- )
    slurp send-chunks ;

0 Value request-stats?

: update-rtdelay ( -- )
    ticks lit, push-lit push' set-rtdelay ;

: data-end? ( -- flag )
    data-rmap @ >o dest-end @ o> ;

: rewind-transfer ( -- )
    rewind data-end? IF  n2o:request-done  ELSE  restart-transfer  THEN
    request-stats? IF  send-timing  THEN ;

: request-stats   true to request-stats?  track-timing ;

: expected@ ( -- head top )
    o IF  data-rmap @ >o
	o IF  dest-tail @ dest-top @  ELSE  0.  THEN o>
    ELSE  0.  THEN  ;

: expected? ( -- )
    expected@ tuck u>= and IF
	net2o-code
	expect-reply
	msg( ." check: " data-rmap @ >o dest-back @ hex. dest-tail @ hex. dest-head @ hex.
	data-ackbits @ data-ack# @ dup hex. + l@ hex.
	o> F cr )
	msg( ." Block transfer done: " expected@ hex. hex. F cr )
	save-all-blocks  net2o:ack-cookies  rewind-transfer
	end-code
	64#0 burst-ticks 64!
    THEN ;

cell 8 = [IF] 6 [ELSE] 5 [THEN] Constant cell>>

2 cells buffer: new-ackbit

: +ackbit ( bit -- )
    dup cell>> rshift swap [ 8 cells 1- ]L and
    data-ackbits-buf $@ bounds ?DO
	over I @ = IF
	    I cell+ swap +bit  drop unloop EXIT  THEN
    2 cells +LOOP
    0 rot new-ackbit 2! new-ackbit cell+ swap +bit
    new-ackbit 2 cells data-ackbits-buf $+! ;

: +cookie ( -- flag )
    data-rmap @ >o ack-bit# @ dup +ackbit
    \ set bucket as received in current polarity bitmap
    data-ackbits @ swap +bit@
    IF  1 packetr2 +! o> \ increment duplicate received flag
    ELSE
	dest-head @ dest-top @ u>= o>
	IF  net2o-code resend-all end-code  THEN  expected?
    THEN ;

: bit>stream ( bit -- streambit )  dup
    dest-back @ addr>bits dest-size @ addr>bits dup >r 1-
    2dup invert and >r and u< IF  r> r@ + >r  THEN
    r> + rdrop ;

\ higher level functions

: map-request, ( ucode udata -- )
    2dup + n2o:new-map lit, swap ulit, ulit,
    map-request ;

User other-xt ' noop other-xt !

: gen-request ( -- )
    net2o-code0
    ['] end-cmd IS expect-reply?
    gen-tmpkeys $, receive-tmpkey
    nest[ add-cookie lit, set-rtdelay gen-reply request-done ]nest
    tmpkey-request key-request other-xt perform
    req-codesize @  req-datasize @  map-request,
    ['] push-cmd IS expect-reply?
    end-code ;

: 0-resend? ( -- )
    resend0 @ IF
	\ ." Resend to 0" cr
	cmd0buf cmd0source !
	resend0 $@ >r cmdbuf r@ move
	cmdbuf r> 64#0 send-cmd 1 packets2 +!
    THEN ;

: map-resend? ( -- )
    code-map @ ?dup-IF  >o
	dest-replies @
	dest-size @ addr>replies bounds o> U+DO
	    I @ 0<> IF
		timeout( ." resend: " I 2@ n2o:see F cr )
		I 2@ I reply-dest 64@ send-cmd
		1 packets2 +!
	    THEN
	reply +LOOP
    THEN ;

: cmd-resend? ( -- )
    0-resend? map-resend? ;

: .expected ( -- )
    F .time ." expected/received: " recv-addr @ hex.
    data-rmap @ >o data-ack# @ hex. o>
    expected@ hex. hex. F cr ;

\ acknowledge toplevel

: net2o:do-ack ( -- )
\    net2o-code
    dest-addr 64@ recv-addr 64! \ last received packet
    recv-cookie
    inbuf 1+ c@ dup recv-flag ! \ last receive flag
    acks# and dup ack-receive !@ xor >r
    r@ ack-toggle# and IF
	net2o-code
	r@ resend-toggle# and IF
	    data-rmap @ >o dest-head @ addr>bits data-reack# ! o>
	    true net2o:do-resend
	THEN
	data-rmap @ >o 0 do-slurp !@ o>
	?dup-IF  net2o:ackflush slurp request-stats? IF  send-timing  THEN THEN
	net2o:gen-resend  net2o:genack
	end-code
	map-resend?
    THEN
    +cookie r> ack-timing ;

: +flow-control ['] net2o:do-ack ack-xt ! ;
: -flow-control ['] noop         ack-xt ! ;

\ keepalive

also net2o-base
: transfer-keepalive? ( -- )
    expected@ u>= ?EXIT
    net2o-code
    update-rtdelay  ticks lit, timeout
    resend-all  net2o:genack end-code ;
previous

: connected-timeout ( -- )
    timeout( .expected )
    cmd-resend? transfer-keepalive? ;

\ : +connecting   ['] connecting-timeout timeout-xt ! ;
: +resend       ['] connected-timeout  timeout-xt ! ;

: +get-time     ['] get-tick other-xt ! ;
: -other        ['] noop other-xt ! ;

: n2o:connect ( ucode udata return-addr -- )
    n2o:new-context
    req-datasize !  req-codesize !
    gen-request
    +resend
    1 client-loop
    -timeout tskc KEYBYTES erase ;

previous

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:") (0 . 2) (0 . 2) non-immediate)
     (("[:") (0 . 1) (0 . 1) immediate)
     ((";]") (-1 . 0) (0 . -1) immediate)
    )
End:
[THEN]