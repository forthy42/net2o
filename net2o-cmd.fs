\ generic net2o command interpreter

\ Copyright (C) 2011,2012   Bernd Paysan

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

2Variable buf-state

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
    dup ['] net2o-crash <> IF  4 cells - body>  THEN  >name .name ;

: printable? ( addr u -- flag )
    true -rot bounds ?DO  I c@ $7F and bl < IF  drop false  LEAVE  THEN  LOOP ;

: n2o.string ( addr u -- )
    2dup printable? IF
	.\" s\" " type
    ELSE
	.\" x\" " xtype
    THEN  .\" \" $, " ;

: net2o-see ( -- ) hex[
    case
	0 of  ." end-code" cr 0. buf-state 2!  endof
	1 of  p@ 64. ." lit, "  endof
	2 of  ps@ s64. ." slit, " endof
	3 of  string@  n2o.string  endof
	cells cmd-base-table + (net2o-see)
	0 endcase ]hex ;

: cmd-see ( addr u -- addr' u' )
    byte@ >r buf-state 2! r> net2o-see buf-state 2@ ;

: n2o:see ( addr u -- )  ." net2o-code " 
    BEGIN  cmd-see  dup 0= UNTIL  2drop ;

: cmd-dispatch ( addr u -- addr' u' )
    byte@ >r buf-state 2! r> cells cmd-base-table + perform buf-state 2@ ;

: extend-cmds ( -- xt ) noname Create lastxt $100 0 DO ['] net2o-crash , LOOP
  DOES>  >r cmd@ cells r> + perform ;

10 buffer: 'cmd-buf

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

forth also net2o-base definitions previous

\ Command numbers preliminary and subject to change

0 net2o: end-cmd ( -- ) 0. buf-state 2! ;
1 net2o: ulit ( -- x ) p@ ;
2 net2o: slit ( -- x ) ps@ ;
3 net2o: string ( -- addr u )  string@ ;

definitions

\ net2o assembler

User cmd0source
User cmdbuf#

: cmdbuf     ( -- addr )  cmd0source @ dup 0= IF  drop code-dest  THEN ;
: cmdlock    ( -- addr )  cmd0source @ IF  cmd0lock  ELSE
	code-map @ >o dest-lock o>
    THEN ;
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
: net2o-code0   cmd0buf cmd0source !   cmdlock lock
    cmdreset ['] net2o, IS net2o-do also net2o-base ;
' net2o, IS net2o-do

: send-cmd ( addr dest -- )  +send-cmd dest-addr 64@ 64>r
    cmd(
    o IF  ." key: " crypto-key $@ .nnb cr  THEN
    ." send: " dup hex. over cmdbuf# @ n2o:see cr )
    code-packet on
    o IF  return-address  ELSE  return-addr  THEN  @
    max-size^2 1+ 0 DO
	cmdbuf# @ min-size I lshift u<= IF
	    I sendX  cmdreset  UNLOOP
	    64r> dest-addr 64! EXIT  THEN
    LOOP  64r> dest-addr 64!  true !!commands!! ;

: cmddest ( -- dest ) cmd0source @ IF  0  ELSE  code-vdest  THEN ;

: cmd ( -- )  cmdbuf cmddest send-cmd
    cmd0source @ 0= IF  code+  THEN ;

also net2o-base

UDefer expect-reply?
' end-cmd IS expect-reply?

:noname  ['] end-cmd IS expect-reply? ; is init-reply

: cmd-send? ( -- )
    cmdbuf# @ IF  expect-reply? cmd  THEN ;

previous

: net2o:tag-reply ( -- )  j?
    tag-addr >r cmdbuf$ r@ 2!
    tag( ." tag: " tag-addr dup hex. 2@ swap hex. hex. F cr )
    code-vdest r> reply-dest ! ;
: net2o:ack-reply ( index -- )  o 0= IF  drop EXIT  THEN
    resend0 @ IF  resend0 $off  THEN
    0. rot reply[] 2! ; \ clear request
: net2o:expect-reply ( -- )  j?
    cmd( ." expect: " cmdbuf$ n2o:see )
    cmdbuf$ code-reply dup >r 2! code-vdest r> reply-dest ! ;

: tag-addr? ( -- flag )
    tag-addr dup >r 2@ dup IF
	cmd( dest-addr 64@ 64. ." resend canned code reply " tag-addr hex. cr )
	cmdbuf# ! r> reply-dest @ send-cmd true
	1 packets2 +!
    ELSE  d0<> -1 0 r> 2!  THEN ;

Variable throwcount

: do-cmd-loop ( addr u -- )
    cmd( 2dup dest-addr 64@ 64. n2o:see )
    sp@ >r throwcount off
    [: BEGIN   cmd-dispatch  dup 0=  UNTIL ;] catch
    dup IF   1 throwcount +! dup s" do-cmd-loop: " etype DoError nothrow
	n2o:see-me  throwcount @ 4 < IF  >throw  THEN  THEN
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

$10 Constant maxnest#
Variable neststart#
Variable neststack maxnest# cells allot \ nest up to 10 levels

: @+ ( addr -- n addr' )  dup @ swap cell+ ;
: nest[ ( -- ) neststart# @ neststack @+ swap cells + !
    1 neststack +! neststack @ maxnest# u>= !!maxnest!!
    cmdbuf# @ neststart# ! ;

: cmd> ( -- addr u )
    init0buf mykey-salt# + maxdata 2/ erase
    cmdbuf$ neststart# @ safe/string neststart# @ cmdbuf# !
    -1 neststack +! neststack @ 0< !!minnest!!
    neststack @+ swap cells + @ neststart# ! ;

: cmd>init ( -- addr u ) cmd> >initbuf 2dup wurst-encrypt$ ;
: cmd>tmpnest ( -- addr u ) cmd> >initbuf 2dup tmpkey@ keysize umin encrypt$ ;

: do-nest ( addr u flag -- )
    buf-state 2@ 2>r validated @ >r  validated or!  do-cmd-loop
    r> validated ! 2r> buf-state 2! ;

: cmdnest ( addr u -- )  wurst-decrypt$
    0= IF  2drop ." Invalid nest" cr  EXIT  THEN own-crypt-val do-nest ;

: cmdtmpnest ( addr u -- )  $>align tmpkey@ keysize umin decrypt$
    0= IF  2drop ." Invalid tmpnest: o=" o hex. tmpkey@ .nnb cr  EXIT  THEN tmp-crypt-val do-nest ;

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
: ulit, ( n -- )  u>64 lit, ;
: end-code ( -- ) expect-reply? previous cmd  cmdlock unlock ;

previous definitions

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
\ 8 is throw, but will be defined last
9 net2o: see-me ( -- ) n2o:see-me ;

10 net2o: push-$    $, ;
11 net2o: push-slit slit, ;
12 net2o: push-char lit, ;
' push-char alias push-lit

13 net2o: push'     p@ cmd, ;
14 net2o: nest ( addr u -- )  cmdnest ;
15 net2o: tmpnest ( addr u -- )  cmdtmpnest ;

: ]nest  ( -- )  end-cmd cmd>init $, push-$ push' nest ;
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
	    ticks recv-tick 64! rtdelay! \ time stamp of arrival
	    EXIT
	ELSE \ just check if timeout didn't expire
	    ticks connect-timeout# 64- 64u< 0= ?EXIT
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
    ( 0 >o add-cookie o> ) ticks lit, set-rtdelay
    max-data# umin swap max-code# umin swap
    2dup + n2o:new-map n2o:create-map
    keypad keysize $, store-key  stskc KEYSIZE erase
    ]nest  n2o:create-map  neststack @ IF  ]tmpnest  THEN
    64drop 2drop 64drop ;

22 net2o: disconnect ( -- )  o 0= ?EXIT n2o:dispose-context un-cmd ;

net2o-base

30 net2o: open-file ( addr u mode id -- )  2*64>n  n2o:open-file ;
31 net2o: close-file ( id -- )  64>n n2o:close-file ;
32 net2o: file-size ( id -- size )  id>addr? fs-size 64@ ;
33 net2o: slurp-chunk ( id -- ) 64>n id>file data-head@ rot read-file throw /data ;
34 net2o: send-chunk ( -- ) net2o:send-chunk ;
35 net2o: send-chunks ( -- ) net2o:send-chunks ;
36 net2o: set-blocksize ( n -- )  64>n blocksize ! ;
37 net2o: set-blockalign ( n -- )  64>n pow2?  blockalign ! ;

: blocksize! ( n -- )  dup ulit, set-blocksize blocksize ! ;
: blockalign! ( n -- )  dup ulit, set-blockalign pow2? blockalign ! ;

\ flow control functions

40 net2o: ack-addrtime ( time addr -- ) 64>n net2o:ack-addrtime ;
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
48 net2o: ack-b2btime ( time addr -- ) 64>n  net2o:ack-b2btime ;
49 net2o: ack-cookies ( cookie addr mask -- )
    [IFUNDEF] 64bit 64>r 64>n 64r> [THEN]
    data-map @ cookie+ 64over 64over 64= 0= IF
	." cookies don't match!" 64over .16 space 64dup .16 F cr
    THEN
    64= cookie-val and validated or! ;
50 net2o: ack-flush ( addr -- )  net2o:rewind-sender-partial ;

\ crypto functions

60 net2o: receive-key ( addr u -- )
    crypt( ." Received key: " tmpkey@ .nnb F cr )
    tmp-crypt? IF  net2o:receive-key  ELSE  2drop  THEN ;
61 net2o: gen-data-ivs ( addr u -- ) data-map ivs-string ;
62 net2o: gen-code-ivs ( addr u -- ) code-map ivs-string ;
63 net2o: gen-rdata-ivs ( addr u -- ) data-rmap ivs-string ;
64 net2o: gen-rcode-ivs ( addr u -- ) code-rmap ivs-string ;
65 net2o: key-request ( -- addr u )
    crypt( ." Nested key: " tmpkey@ .nnb F cr )
    nest[ pkc keysize $, receive-key ;
66 net2o: update-key ( -- )  net2o:update-key ;

\ create commands to send back

: data-ivs ( -- ) \ two IV seeds for send and receive data
    state# rng$ 2dup $, gen-data-ivs data-rmap ivs-string
    state# rng$ 2dup $, gen-rdata-ivs data-map ivs-string ;
: code-ivs ( -- ) \ two IV seeds for send and receive code
    state# rng$ 2dup $, gen-code-ivs code-rmap ivs-string
    state# rng$ 2dup $, gen-rcode-ivs code-map ivs-string ;

67 net2o: gen-reply ( -- )
    [: crypt( ." Reply key: " tmpkey@ .nnb F cr )
      nest[ pkc keysize $, receive-key update-key code-ivs ]tmpnest end-cmd
      ['] end-cmd IS expect-reply? cmdbuf$ push-reply ;]  IS expect-reply? ;
68 net2o: receive-tmpkey ( addr u -- ) net2o:receive-tmpkey ;
69 net2o: tmpkey-request ( -- ) stpkc keysize $, receive-tmpkey ;

\ better slurping

70 net2o: slurp-block ( id -- 64nextseek )
    64>n n2o:slurp-block' ;
71 net2o: track-size ( size id -- )
    64>n track( >r ." file <" r@ 0 .r ." > size: " 64dup 64. F cr r> ) size! ;
72 net2o: track-seek ( seek id -- )
    64>n track( >r ." file <" r@ 0 .r ." > seek: " 64dup 64. F cr r> ) seekto! ;
73 net2o: track-limit ( seek id -- )
    64>n track( >r ." file <" r@ 0 .r ." > seek to: " 64dup 64. F cr r> ) limit! ;
74 net2o: set-stat ( mtime mod id -- ) n2o:set-stat ;
75 net2o: get-stat ( id -- ) 64>n { fd }
    fd n2o:get-stat >r lit, r> ulit, fd ulit, set-stat ;
76 net2o: open-tracked-file ( addr u mode id -- )
    2*64>n dup >r n2o:open-file
    r@ id>file F file-size throw
    d>64 lit, r@ ulit, track-size
    r@ n2o:get-stat >r lit, r> ulit, r> ulit, set-stat ;
77 net2o: slurp-tracked-block ( id -- )
    64>n dup >r n2o:slurp-block' 64drop lit, r> ulit, track-seek ;
78 net2o: slurp-tracked-blocks ( idbits -- )
    64>n dup >r n2o:slurp-blocks
    r> [: lit, ulit, track-seek ;] n2o:track-seeks ;
79 net2o: slurp-all-tracked-blocks ( -- )
    n2o:slurp-all-blocks
    [: lit, ulit, track-seek ;] n2o:track-all-seeks ;
80 net2o: rewind-sender ( n -- )  64>n net2o:rewind-sender ;
81 net2o: rewind-receiver ( n -- )  64>n net2o:rewind-receiver ;

82 net2o: set-total ( u -- )  write-file# off residualwrite off 64>n total! ;
83 net2o: gen-total ( -- ) read-file# off residualread off net2o:gen-total lit, set-total ;

\ acknowledges

90 net2o: set-head ( offset -- ) data-rmap @ >o dest-head umax! o> ;
91 net2o: timeout ( ticks -- ) net2o:timeout data-map @ >o dest-tail @ o> ulit, set-head ;
92 net2o: ack-reply ( tag -- ) net2o:ack-reply ;
93 net2o: tag-reply ( tag -- ) net2o:tag-reply lit, ack-reply ;

\ ids 100..120 reserved for key exchange/strage

\ profiling

120 net2o: !time ( -- ) init-timer ;
121 net2o: .time ( -- ) .packets .times ;

: rewind ( -- )  data-rmap @ >o dest-round @ 1+ o>
    dup net2o:rewind-receiver ulit, rewind-sender ;

\ safe initialization

\ This must be defined last, otherwise dangerous name-clash!

8 net2o: throw ( error -- )  F throw ;

net2o-base

: lit<   lit, push-lit ;
: slit<  slit, push-slit ;
:noname
    server? IF
	dup  IF  dup nlit, throw end-cmd
	    ['] end-cmd IS expect-reply? also end-code  THEN
	F throw  THEN  drop ; IS >throw

previous definitions

also net2o-base

Variable file-reg#

: n2o:copy ( addrsrc us addrdest ud -- )
    2swap $, r/o ulit, file-reg# @ ulit, open-tracked-file
    file-reg# @ save-to
    1 file-reg# +! ;

: n2o:seek ( pos id -- )
    2dup state-addr fs-seek !  swap ulit, ulit, track-seek ;

: n2o:done ( -- )
    gen-total slurp-all-tracked-blocks ;

: n2o:close-all ( -- )
    file-reg# @ 0 ?DO
	I n2o:close-file
    LOOP  file-reg# off  file-state $off ;

file-reg# off

previous

\ client side timing

: ack-size ( -- )  1 acks +!  recv-tick 64@ lastb-ticks 64! ;
: ack-first ( -- )
    lastb-ticks 64@ 64dup 64-0= 0= IF
	firstb-ticks 64@ 64- delta-ticks 64+!
    ELSE  64drop  THEN
    recv-tick 64@ firstb-ticks 64!  lastb-ticks 64off
    recv-tick 64@ last-rtick 64!  recv-addr 64@ last-raddr 64! ;

: ack-timing ( n -- )  ratex( dup 3 and s" .[+(" drop + c@ emit )
    b2b-toggle# and  IF  ack-first  ELSE  ack-size  THEN ;

: .rate ( n -- n ) dup . ." rate" cr ;
: .eff ( n -- n ) dup . ." eff" cr ;
also net2o-base
: setrate-limit ( rate -- rate' )
    \ do not change requested rate by more than a factor 4
    last-rate 64@ 64>n
    ?dup-IF  tuck 2* min swap 2/ max  THEN
    dup n>64 last-rate 64! ;

: >rate ( -- )  delta-ticks 64@ 64-0= acks @ 0= or ?EXIT
    recv-tick 64@ 64dup burst-ticks 64!@ 64dup 64-0= 0= IF
	64- 64>n rate( .eff ) >r
	delta-ticks 64@ 64>n tick-init 1+ acks @ */
	setrate-limit
	rate( .rate ) ulit, r> ulit, set-rate
    ELSE
	64drop 64drop
    THEN
    delta-ticks 64off  acks off ;

: net2o:acktime ( -- )
    recv-addr @ recv-tick 64@ time-offset 64@ 64-
    timing( 64>r dup hex. 64r> 64dup 64. ." acktime" F cr )
    lit, ulit, ack-addrtime ;
: net2o:b2btime
    last-raddr 64@ last-rtick 64@ 64dup 64#0 64=
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

: net2o:gen-resend ( -- )
    recv-flag @ invert resend-toggle# and ulit, ack-resend ;
: net2o:ackflush ( -- )
    data-rmap @ >o dest-back @ o> ulit, ack-flush ;
: net2o:flush-blocks ( -- )
    data-rmap @ >o dest-back @ dest-tail @ over - dest-size @ o> 2/ 2/ > IF
	flush( ." flush partial " dup hex. data-rmap @ >o dest-tail @ hex. o> F cr )
	flush1( save-all-blocks )
	flush2( data-rmap @ >o dest-back !@ o>
	net2o:rewind-receiver-partial )else( drop )
	flush3( net2o:ackflush
	slurp-all-tracked-blocks )
	flush( ." partial rewind completed " data-rmap @ >o dest-back @ hex. o>  F cr )
    ELSE
	drop
    THEN ;
: net2o:genack ( -- )
    net2o:ack-cookies  net2o:b2btime  net2o:acktime  >rate
    net2o:flush-blocks ;

: !rdata-tail ( -- )
    data-rmap @ >o data-firstack0# @ data-firstack1# @ umin
    chunk-p2 3 + lshift dest-head @ umin dest-tail ! o> ;
: receive-flag ( -- flag )  recv-flag @ resend-toggle# and 0<> ;
: data-ackbit ( flag -- bit )
    IF  data-ackbits1  ELSE  data-ackbits0  THEN @ ;
: data-firstack# ( flag -- addr )
    IF  data-firstack0#  ELSE  data-firstack1#  THEN ;

4 Value max-resend#

: net2o:do-resend ( flag -- )
    o 0= IF  drop EXIT  THEN  data-rmap @ 0= IF  drop EXIT  THEN
    receive-flag { rf } data-rmap @ >o
    \ we have not yet received anything
    data-lastack# @ 0< IF  drop o>  EXIT  THEN
    dest-head @ addr>bits
    swap IF  mask-bits# - 0 max  THEN  bits>bytes
    rf data-ackbit 
    dest-size @ chunk-p2 3 + rshift 1- { acks ackm }
    acks 0= IF ." ackzero: " o hex. rf F . acks hex. hex. F cr o>  EXIT  THEN
    rf data-firstack# { first-ack# }
    0 swap first-ack# @ o>
    +DO
	acks I ackm and + l@ ack( ." acks: " acks hex. I hex. dup hex. F cr )
	$FFFFFFFF <> IF
    	    acks I ackm and + l@ $FFFFFFFF xor
	    I chunk-p2 3 + lshift
	    resend( ." resend: " dup hex. over hex. F cr )
	    ulit, ulit, resend-mask  1+
	ELSE
	    dup 0= IF  I 4 + first-ack# !
		firstack( ." data-firstack" receive-flag negate 1 .r ." # = " I F . F cr )
	    THEN
	THEN
	dup max-resend# >= ?LEAVE \ no more than x resends
    4 +LOOP  drop !rdata-tail ;
: resend-all ( -- )
    resend-toggle# recv-flag xor!  false net2o:do-resend
    resend-toggle# recv-flag xor!  false net2o:do-resend ;

: do-expect-reply ( -- )
    reply-index ulit, tag-reply  end-cmd  net2o:expect-reply
    msg( ." Expect reply" F cr )
    ['] end-cmd IS expect-reply? ;

: expect-reply ( -- ) ['] do-expect-reply IS expect-reply? ;

: restart-transfer ( -- )
    slurp-all-tracked-blocks send-chunks ;

0 Value request-stats?

: update-rtdelay ( -- )
    ticks lit, push-lit push' set-rtdelay ;

: rewind-transfer ( -- )
    expected @ negate total +!
    expected off  received off
    rewind  total @ 0> IF
	restart-transfer
    THEN
    request-stats? IF
	send-timing
    THEN
    total @ 0<= IF
	msg( ." Chunk transfer done!" F cr )
	n2o:request-done  EXIT
    THEN ;

: request-stats   true to request-stats?  track-timing ;

: expected? ( -- )
    received @ expected @ tuck u>= and IF
	net2o-code
	resend-all
	msg( ." check: " data-rmap @ >o dest-back @ hex. dest-tail @ hex. dest-head @ hex.
	data-ackbits0 @ data-firstack0# @ dup hex. + l@ hex.
	data-ackbits1 @ data-firstack1# @ dup hex. + l@ hex.
	o> received @ hex. F cr )
	msg( ." Block transfer done: " received @ hex. expected @ hex. F cr )
	save-all-blocks  net2o:ack-cookies  rewind-transfer
	expect-reply
	end-code
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

: +cookie ( -- bit flag map )
    recv-addr @ -1 = IF  0 0 0 EXIT  THEN
    recv-addr @ receive-flag { rf } data-rmap @ >o
    dest-vaddr @ - addr>bits dup +ackbit
    \ set bucket as received in current polarity bitmap
    rf data-ackbit over +bit@
    dup IF  1 packetr2 +!  THEN o o> ;

: received! ( bit flag map -- ) dup 0= IF  2drop drop  EXIT  THEN
    receive-flag 0= { !rf }
    swap >r >o
    dup data-lastack# @ > IF
	\ if we are at head, fill other polarity with 1s
	dup data-lastack# !@
	!rf data-ackbit -rot
	+DO  dup I 1+ +bit  LOOP o>
    ELSE
	\ otherwise, set only this specific bucket
	!rf data-ackbit over +bit@ o>
	r> and >r
    THEN
    drop r> 0= IF  maxdata received +!  expected?  THEN ;

: net2o:do-ack ( -- ) 
    dest-addr 64@ recv-addr 64! \ last received packet
    recv-cookie
    inbuf 1+ c@ recv-flag ! \ last receive flag
    inbuf 1+ c@ acks# and
    dup ack-receive !@ xor >r
    r@ [ resend-toggle# ack-toggle# or ]L and  IF
	cmd0source off  cmdlock lock  cmdreset
	r@ resend-toggle# and IF  true net2o:do-resend  THEN
	r@ ack-toggle# and IF  net2o:gen-resend  net2o:genack  THEN
	cmd-send?  cmdlock unlock
    THEN
    +cookie received!
    r> ack-timing ;

: +flow-control ['] net2o:do-ack ack-xt ! ;
: -flow-control ['] noop         ack-xt ! ;

\ higher level functions

: map-request, ( ucode udata -- )
    2dup + n2o:new-map ulit, swap ulit, ulit,
    map-request ;

: gen-request ( -- )  gen-tmpkeys
    net2o-code0
    ['] end-cmd IS expect-reply?
    tpkc keysize $, receive-tmpkey
    nest[ add-cookie lit, set-rtdelay gen-reply request-done ]nest
    tmpkey-request key-request
    req-codesize @  req-datasize @  map-request,
    end-code ;

: ?j ]] j?  code-map @ 0= ?EXIT [[ ; immediate

: 0-resend? ( -- )
    resend0 @ IF
	\ ." Resend to 0" cr
	resend0 $@ cmdbuf swap move
	cmdbuf 0 send-cmd 1 packets2 +!
    THEN ;

: map-resend? ( -- )
    code-map @ ?dup-IF  >o
	dest-replies @
	dest-size @ addr>replies bounds o> ?DO
	    I 2@ d0<> IF
		timeout( ." resend: " I 2@ n2o:see F cr )
		I 2@ cmdbuf# ! I reply-dest @ send-cmd
		1 packets2 +!
	    THEN
	reply +LOOP
    THEN ;

: cmd-resend? ( -- )
    0-resend? map-resend? ;

: .expected ( -- )
    ." expected/received: " recv-addr @ hex.
    data-rmap @ >o dest-head @ hex.
    false data-firstack# @ hex. true data-firstack# @ hex. o>
    expected @ hex. received @ hex. F cr
    \ receive-flag data-rmap @ >o
    \ data-ackbit dest-size @ addr>bits bits>bytes dump o>
;

: transfer-keepalive? ( -- )
    received @ expected @ u>= ?EXIT
    timeout( .expected )
    cmdreset update-rtdelay  ticks lit, timeout
    resend-all  net2o:genack  cmd-send? ;

\ : connecting-timeout ( -- )
\     F .time ."  connecting timeout" F cr
\     cmdbuf 0 send-cmd  1 packets2 +! ;
: connected-timeout ( -- )
    F .time ."  connected timeout, o=" o hex.
    received @ hex. expected @ hex. F cr
    cmd-resend? transfer-keepalive? ;

\ : +connecting   ['] connecting-timeout timeout-xt ! ;
: +resend       ['] connected-timeout  timeout-xt ! ;
: -timeout      ['] noop               timeout-xt ! ;

: n2o:connect ( ucode udata return-addr -- )
    n2o:new-context
    req-datasize !  req-codesize !
    gen-request cmdbuf$ resend0 $!
    +resend
    1 client-loop
    -timeout tskc KEYBYTES erase ;

: rewind? ( -- )
    data-rmap @ >o dest-round @ o> lit, rewind-sender ;

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