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
    : u>n ( 64u -- 64n )
	dup 2/ swap 1 and IF negate THEN ;
    : n>u ( 64n -- 64u )
	dup 0< 1 and swap abs 2* or ;
[ELSE]
    : u>n ( 64u -- 64n )
	2dup d2/ 2swap drop 1 and IF  dnegate  THEN ;
    : n>u ( 64n -- 64u )
	dup 0< 1 and -rot dabs d2* >r or r> ;
[THEN]
    
: ps!+ ( 64n addr -- addr' )
    >r n>u r> p!+ ;
: ps@+ ( addr -- 64n addr' )
    p@+ >r u>n r> ;

: p@ ( -- 64u ) buf-state 2@ over + >r p@+ r> over - buf-state 2! ;
: ps@ ( -- 64n ) p@ u>n ;

: byte@ ( addr u -- addr' u' b )
    >r count r> 1- swap ;

: string@ ( -- addr u )
    buf-state 2@ over + >r
    p@+ [IFUNDEF] 64bit nip [THEN] swap 2dup + r> over - buf-state 2! ;

\ Command streams contain both commands and data
\ the dispatcher is a byte-wise dispatcher, though
\ commands come in junks of 8 bytes
\ Commands are zero-terminated

: net2o-crash  base @ >r hex
    buf-state 2@ swap 8 u.r 8 u.r ." :" buf-state 2@ drop 1- c@ 2 u.r cr
    r> base !  buf-state 2@ dump
    !!function!! throw ;

Create cmd-base-table 256 0 [DO] ' net2o-crash , [LOOP]

: cmd@ ( -- u ) buf-state 2@ byte@ >r buf-state 2! r> ;

: (net2o-see) ( addr -- )  @ 2 cells - body> >name .name ;

: printable? ( addr u -- flag )
    true -rot bounds ?DO  I c@ bl < IF  drop false  LEAVE  THEN  LOOP ;

: xtype ( addr u -- )  base @ >r hex
    bounds ?DO  I c@ 0 <# # # #> type  LOOP  r> base ! ;

: n2o.string ( addr u -- )
    2dup printable? IF
	.\" s\" " type
    ELSE
	.\" x\" " xtype
    THEN  .\" \" $, " ;

: net2o-see ( -- )
    case
	0 of  ." end-code" cr 0. buf-state 2!  endof
	1 of  p@ 64. ." lit, "  endof
	2 of  ps@ 64. ." slit, " endof
	3 of  string@ n2o.string  endof
	cells cmd-base-table + (net2o-see)
	0 endcase ;

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

Vocabulary net2o-base

forth also net2o-base definitions previous

\ Command numbers preliminary and subject to change

0 net2o: end-cmd ( -- ) 0. buf-state 2! ;
1 net2o: ulit ( -- x ) p@ ;
2 net2o: slit ( -- x ) ps@ ;
3 net2o: string ( -- addr u )  string@ ;

\ these functions are only there to test the server

4 net2o: emit ( xc -- ) 64>n xemit ;
5 net2o: type ( addr u -- )  type ;
6 net2o: . ( -- ) 64. ;
7 net2o: cr ( -- ) cr ;

definitions

\ net2o assembler

maxdata buffer: cmd0buf
maxdata 2/ mykey-salt# + 2 cells + buffer: init0buf

Variable cmd0source
Variable cmd0buf#

: cmdbuf     ( -- addr )  cmd0source @ IF  code-dest    ELSE  cmd0buf  THEN ;
: cmdbuf#     ( -- addr ) cmd0source @ IF  j^ cmd-buf#  ELSE  cmd0buf#  THEN ;
: cmdbuf$ ( -- addr u )   cmdbuf cmdbuf# @ ;
: endcmdbuf  ( -- addr' ) cmdbuf maxdata + ;

: cmdreset  cmdbuf# off ;

: cmd, ( 64n -- )  cmdbuf$ + dup >r p!+ r> - cmdbuf# +! ;

: net2o, @ n>64 cmd, ;

: net2o-code   cmd0source on   ['] net2o, IS net2o-do also net2o-base ;
: net2o-code0  cmd0source off  ['] net2o, IS net2o-do also net2o-base ;
net2o-code0 previous

: send-cmd ( addr dest -- )
    cmd( ." send: " cmdbuf$ n2o:see cr )
    code-packet on
    j^ IF  j^ return-address  ELSE  return-addr  THEN  @
    max-size^2 1+ 0 DO
	cmdbuf# @ min-size I lshift u<= IF  I sendX  cmdreset  UNLOOP  EXIT  THEN
    LOOP  !!commands!! throw ;

: cmddest ( -- dest ) cmd0source @ IF  code-vdest  ELSE  0  THEN ;

: cmd ( -- )  cmdbuf cmddest send-cmd
    cmd0source @ IF  code+  THEN ;

also net2o-base

Defer expect-reply?
' end-cmd IS expect-reply?

: cmd-send? ( -- )
    cmdbuf# @ IF  expect-reply?  cmd  THEN ;

previous

: tag-addr ( -- addr )
    dest-addr @ j^ code-rmap $@ drop >r r@ dest-vaddr @ -
    addr>replies r> dest-timestamps @ + ;

: net2o:tag-reply ( -- )  j^ 0= ?EXIT
    tag-addr >r cmdbuf$ r> 2! ;
: net2o:ack-reply ( index -- )  j^ 0= IF  drop EXIT  THEN
    0. rot reply[] 2! ; \ clear request
: net2o:expect-reply ( -- )  j^ 0= ?EXIT
    cmd( ." expect: " cmdbuf$ n2o:see cr )
    cmdbuf$ code-reply 2! ;

: tag-addr? ( -- flag )  j^ 0= IF  false  EXIT  THEN
    tag-addr 2@ 2dup d0<> IF
	cmdbuf# ! code-vdest send-cmd  true
	." Resend canned code reply" cr
    ELSE  2drop  false  THEN ;

: do-cmd-loop ( addr u -- )
    cmd( 2dup n2o:see )
    sp@ >r
    TRY  BEGIN  cmd-dispatch  dup 0=  UNTIL
	IFERROR  dup DoError nothrow >throw  THEN  ENDTRY
    drop  r> sp! 2drop ;

: cmd-loop ( addr u -- )
    tag-addr?  ?EXIT
    j^ IF  cmd0source on  ELSE  cmd0source off  THEN
    cmdreset  do-cmd-loop  cmd-send? ;

' cmd-loop is queue-command

\ nested commands

: >initbuf ( addr u -- addr' u' ) tuck
    init0buf mykey-salt# + swap move
    maxdata  BEGIN  2dup 2/ u<  WHILE  2/ dup min-size = UNTIL  THEN
    nip init0buf swap mykey-salt# + 2 cells + 2dup wurst-encrypt$ ;

Variable neststart#

: nest[ ( -- )  cmdbuf# @ neststart# ! ;

: cmd>init ( -- addr u )
    init0buf mykey-salt# + maxdata 2/ erase
    cmdbuf$ neststart# @ safe/string  >initbuf
    neststart# @ cmdbuf# ! ;

: cmdnest ( addr u -- )
    wurst-decrypt$ 0= IF  2drop ." Invalid nest" cr ( invalid >throw )  EXIT  THEN
    buf-state 2@ 2>r validated @ >r  own-crypt-val validated or!  do-cmd-loop
    r> validated ! 2r> buf-state 2! ;

\ net2o assembler stuff

also net2o-base definitions

: maxstring ( n -- )  endcmdbuf cmdbuf$ + - ;
: $, ( addr u -- )  string  >r r@ cmd,
    r@ maxstring u>= !!stringfit!! and throw
    cmdbuf$ + r@ move   r> cmdbuf# +! ;
: lit, ( u -- )  ulit cmd, ;
: slit, ( n -- )  slit n>u cmd, ;
: nlit, ( n -- )  n>64 slit, ;
: ulit, ( n -- )  u>64 lit, ;
: end-code ( -- ) end-cmd previous cmd ;

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

10 net2o: push-$    $, ;
11 net2o: push-slit slit, ;
12 net2o: push-char lit, ;
' push-char alias push-lit

13 net2o: push'     p@ cmd, ;
14 net2o: nest ( addr u -- )  cmdnest ;

: ]nest  ( -- )  end-cmd cmd>init $, push-$ push' nest ;

15 net2o: new-context ( -- ) return-addr @ n2o:new-context ;
16 net2o: new-data ( addr addr u -- )  3*64>n  n2o:new-data ;
17 net2o: new-code ( addr addr u -- )  3*64>n  n2o:new-code ;
18 net2o: request-done ( -- )  -1 requests +! ;
19 net2o: set-j^ ( addr -- ) 64>n own-crypt? IF  to j^  ELSE  drop  THEN ;

: n2o:create-map ( addrs ucode udata addrd -- addrs ucode udata addrd ) >r
    2 pick ulit, r@ ulit, over ulit, new-code
    2 pick 2 pick + ulit, 2dup swap r@ + ulit, ulit, new-data
    r> ;

20 net2o: map-request ( addrs ucode udata -- )  3*64>n
    nest[
    new-context
    max-data# umin swap max-code# umin swap
    2dup + n2o:new-map n2o:create-map
    ]nest  >r rot r> swap >r -rot r> n2o:create-map
    2drop 2drop ;

net2o-base

21 net2o: open-file ( addr u mode id -- )  2*64>n  n2o:open-file ;
22 net2o: close-file ( id -- )  64>n n2o:close-file ;
23 net2o: file-size ( id -- size )  id>addr? fs-size @ u>64 ;
24 net2o: slurp-chunk ( id -- ) 64>n id>file data$@ rot read-file throw /data ;
25 net2o: send-chunk ( -- ) net2o:send-chunk ;
26 net2o: send-chunks ( -- ) net2o:send-chunks ;
27 net2o: set-blocksize ( n -- )  64>n j^ blocksize ! ;
28 net2o: set-blockalign ( n -- )  64>n pow2?  j^ blockalign ! ;

: blocksize! ( n -- )  dup ulit, set-blocksize j^ blocksize ! ;
: blockalign! ( n -- )  dup ulit, set-blockalign pow2? j^ blockalign ! ;

\ flow control functions

30 net2o: ack-addrtime ( time addr -- ) 64>n  net2o:ack-addrtime ;
31 net2o: ack-resend ( flag -- ) 64>n  net2o:ack-resend ;
32 net2o: set-rate ( ticks1 ticks2 -- )
    cookie? IF  net2o:set-rate
    ELSE  64drop 64drop j^ ns/burst dup @ 2* 2* swap !  THEN ;
33 net2o: resend-mask ( addr mask -- )
    [IFUNDEF] 64bit  64>r 64>n 64r> [THEN]
    net2o:resend-mask net2o:send-chunks ;
34 net2o: track-timing ( -- )  net2o:track-timing ;
35 net2o: rec-timing ( addr u -- )  net2o:rec-timing ;
36 net2o: send-timing ( -- )  net2o:timing$ maxstring $10 - -$10 and umin $,
    rec-timing ;
37 net2o: >time-offset ( n -- )  j^ time-offset 64! ;
: time-offset! ( -- )  ticks 64dup lit, >time-offset j^ time-offset 64! ;
38 net2o: ack-b2btime ( time addr -- ) 64>n  net2o:ack-b2btime ;
39 net2o: set-rtdelay ( time -- )  j^ recv-tick @ swap - j^ rtdelay ! ;
40 net2o: ack-cookies ( cookie addr mask -- )
    [IFUNDEF] 64bit 64>r 64>n 64r> [THEN]
    map@ cookie+ 64= cookie-val validated or! ;

\ crypto functions

50 net2o: receive-key ( addr u -- )  net2o:receive-key  do-keypad on ;
51 net2o: gen-data-ivs ( addr u -- ) net2o:gen-data-ivs ;
52 net2o: gen-code-ivs ( addr u -- ) net2o:gen-code-ivs ;
53 net2o: gen-rdata-ivs ( addr u -- ) net2o:gen-rdata-ivs ;
54 net2o: gen-rcode-ivs ( addr u -- ) net2o:gen-rcode-ivs ;
55 net2o: key-request ( -- addr u )  pkc keysize $, receive-key ;
56 net2o: update-key ( -- )  net2o:update-key ;

\ create commands to send back

: data-ivs ( -- ) \ two IV seeds for send and receive data
    rng$ 2dup $, gen-data-ivs net2o:gen-rdata-ivs
    rng$ 2dup $, gen-rdata-ivs net2o:gen-data-ivs ;
: code-ivs ( -- ) \ two IV seeds for send and receive code
    rng$ 2dup $, gen-code-ivs net2o:gen-rcode-ivs
    rng$ 2dup $, gen-rcode-ivs net2o:gen-code-ivs ;

\ better slurping

60 net2o: slurp-block ( seek maxlen id -- nextseek )
          n2o:slurp-block ;
61 net2o: track-size ( size id -- )
          track( 2dup ." file <" 0 .r ." > size: " F . F cr ) size! ;
62 net2o: track-seek ( seek id -- )
          track( 2dup ." file <" 0 .r ." > seek: " F . F cr ) seek! ;
63 net2o: open-tracked-file ( addr u mode id -- )
          dup >r n2o:open-file
          r@ id>file F file-size throw drop lit, r> lit, track-size ;
64 net2o: slurp-tracked-block ( id -- )
          dup >r n2o:slurp-block lit, r> lit, track-seek ;
65 net2o: slurp-tracked-blocks ( idbits -- )
          dup >r n2o:slurp-blocks
          r> [: swap lit, lit, track-seek ;] n2o:track-seeks ;
66 net2o: slurp-all-tracked-blocks ( -- )
          n2o:slurp-all-blocks
          [: lit, lit, track-seek ;] n2o:track-all-seeks ;
67 net2o: rewind-sender ( n -- )  net2o:rewind-sender ;
68 net2o: rewind-receiver ( n -- )  net2o:rewind-receiver ;

\ acknowledges

70 net2o: timeout ( ticks -- ) net2o:timeout ;
71 net2o: ack-reply ( tag -- ) net2o:ack-reply ;
72 net2o: tag-reply ( tag -- ) net2o:tag-reply lit, ack-reply ;

\ profiling

80 net2o: !time ( -- ) init-timer ;
81 net2o: .time ( -- ) .times ;

: rewind ( -- )  j^ data-rmap $@ drop dest-round @ 1+
    dup net2o:rewind-receiver lit, rewind-sender ;

\ safe initialization

\ This must be defined last, otherwise dangerous name-clash!

8 net2o: throw ( error -- )  throw ;

net2o-base

: lit<   lit, push-lit ;
: slit<  slit, push-slit ;
:noname  server? IF
	dup  IF  dup lit, throw end-cmd cmd  THEN
    THEN  F throw ; IS >throw

previous definitions

also net2o-base

Variable file-reg#

: n2o:copy ( addrsrc us addrdest ud -- )
    2swap $, r/o lit, file-reg# @ lit, open-tracked-file
\    file-reg# @ lit, slurp-tracked-block
    file-reg# @ save-to
    1 file-reg# +! ;

: n2o:done ( -- )
    slurp-all-tracked-blocks
    file-reg# off ;

file-reg# off

previous

\ client side timing

: ack-size ( -- )  1 j^ acks +!  j^ recv-tick @ j^ lastb-ticks ! ;
: ack-first ( -- )
    j^ lastb-ticks @ ?dup-IF  j^ firstb-ticks @ - j^ delta-ticks +!  THEN
    j^ recv-tick @ j^ firstb-ticks !  j^ lastb-ticks off
    j^ recv-tick @ j^ last-rtick !  j^ recv-addr @ j^ last-raddr ! ;

: ack-timing ( n -- )  ratex( dup 3 and s" .[+(" drop + c@ emit )
    b2b-toggle# and  IF  ack-first  ELSE  ack-size  THEN ;

: .rate ( n -- n ) dup . ." rate" cr ;
: .eff ( n -- n ) dup . ." eff" cr ;
also net2o-base
: >rate ( -- )  j^ delta-ticks 2@ 0= swap 0= or ?EXIT
    j^ recv-tick @ dup j^ burst-ticks !@ dup IF
	- rate( .eff ) >r
	j^ delta-ticks @ tick-init 1+ j^ acks @ */
\	j^ last-rate @
\	\ do not change requested rate by more than a factor 2
\	?dup-IF  tuck 2* min swap 2/ max  THEN
\	dup j^ last-rate !
	rate( .rate ) lit, r> lit, set-rate
    ELSE
	2drop
    THEN
    j^ delta-ticks off  j^ acks off ;

: net2o:acktime ( -- )
    j^ recv-addr @ j^ recv-tick @ j^ time-offset @ -
    timing( 2dup F . F . ." acktime" F cr )
    lit, lit, ack-addrtime ;
: net2o:b2btime
    j^ last-raddr @ j^ last-rtick 64@ 64dup 64#0 64=
    IF  64drop drop
    ELSE  j^ time-offset 64@ 64- lit, ulit, ack-b2btime  THEN ;

\ ack bits, new code

: ack-cookie, ( map n bits -- ) >r [ 8 cells ]L * maxdata * r>
    2dup 2>r rot cookie+ lit, 2r> swap lit, lit, ack-cookies ;

: net2o:ack-cookies ( -- )  rmap@ { map }
    map data-ackbits-buf $@
    bounds ?DO  map I 2@ swap ack-cookie,  2 cells +LOOP
    s" " map data-ackbits-buf $! ;

\ client side acknowledge

: net2o:gen-resend ( -- )
    j^ recv-flag @ invert resend-toggle# and lit, ack-resend ;
: net2o:genack ( -- )
    net2o:ack-cookies  net2o:b2btime  net2o:acktime  >rate ;

: receive-flag ( -- flag )  j^ recv-flag @ resend-toggle# and 0<> ;
: data-ackbit ( flag -- addr )
    IF  data-ackbits1  ELSE  data-ackbits0  THEN ;
: data-firstack# ( flag -- addr )
    IF  data-firstack0#  ELSE  data-firstack1#  THEN ;
: net2o:do-resend ( flag -- )
    j^ 0= IF  drop EXIT  THEN  j^ data-rmap @ 0= IF  drop EXIT  THEN
    j^ recv-high @ -1 = IF  drop  EXIT  THEN
    j^ data-rmap $@ drop { dmap }
    \ we have not yet received anything
    dmap data-lastack# @ 0< IF  drop  EXIT  THEN
    j^ recv-high @ dmap dest-vaddr @ - addr>bits
    swap IF  mask-bits# - 0 max  THEN
    dmap receive-flag data-ackbit @
    over bits>bytes dmap receive-flag data-firstack# @ +DO
	dup I + l@ $FFFFFFFF = IF
	    I dmap receive-flag data-firstack# !
	    firstack( ." data-firstack" receive-flag negate 1 .r ." # = " I F . F cr )
	ELSE
	    LEAVE
	THEN
    4 +LOOP
    over bits>bytes dmap receive-flag data-firstack# @ +DO
	dup I + l@ $FFFFFFFF <> IF
    	    dup I + l@ $FFFFFFFF xor
	    I chunk-p2 3 + lshift dmap dest-vaddr @ +
	    resend( ." resend: " dup hex. over hex. F cr )
	    lit, lit, resend-mask
	THEN
    4 +LOOP
    2drop ;

: do-expect-reply ( -- )
    reply-index lit, tag-reply  end-cmd  net2o:expect-reply
    ['] end-cmd IS expect-reply? ;

: expect-reply ( -- ) ['] do-expect-reply IS expect-reply? ;

: restart-transfer ( -- )
    slurp-all-tracked-blocks send-chunks ;

0 Value request-stats?

: rewind-transfer ( -- )
    j^ expected @ negate j^ total +!
    j^ expected off  j^ received off
    rewind  restart-transfer
    request-stats? IF
	send-timing  track-timing
    THEN
    j^ total @ 0<= IF
	msg( ." Chunk transfer done!" F cr )
	-1 requests +!  EXIT
    THEN ;

: request-stats   true to request-stats?  track-timing ;

: expected? ( -- )
    j^ received @ j^ expected @ tuck u>= and IF
	msg( ." Block transfer done!" F cr )
	save-all-blocks  rewind-transfer  expect-reply
    THEN ;

cell 8 = [IF] 6 [ELSE] 5 [THEN] Constant cell>>

2 cells buffer: new-ackbit

: +ackbit ( bit rmap -- ) { rmap }
    dup cell>> rshift swap [ 8 cells 1- ]L and
    rmap data-ackbits-buf $@ bounds ?DO
	over I @ = IF
	    I cell+ swap +bit  drop unloop  EXIT  THEN
    2 cells +LOOP
    0 rot new-ackbit 2! new-ackbit cell+ swap +bit
    new-ackbit 2 cells rmap data-ackbits-buf $+! ;

: received! ( -- )
    j^ recv-addr @ -1 = ?EXIT
    j^ data-rmap $@ drop >r
    j^ recv-addr @ r@ dest-vaddr @ - addr>bits dup r@ +ackbit
    \ set bucket as received in current polarity bitmap
    r@ receive-flag data-ackbit @ over +bit@
    r> swap >r >r
    dup r@ data-lastack# @ > IF
	\ if we are at head, fill other polarity with 1s
	dup r@ data-lastack# !@
	r> receive-flag 0= data-ackbit @ -rot
	+DO  dup I 1+ +bit  LOOP
    ELSE
	\ otherwise, set only this specific bucket
	r> receive-flag 0= data-ackbit @ over +bit@
	r> and >r
    THEN
    drop r> 0= IF  maxdata j^ received +!  expected?  THEN ;

: recv-high! ( -- )
    dest-addr @ j^ recv-high
    j^ recv-high @ -1 = IF  !  ELSE  umax!  THEN ;

: net2o:do-ack ( -- )
    dest-addr @ j^ recv-addr ! \ last received packet
    recv-high!  recv-cookie
    inbuf 1+ c@ j^ recv-flag ! \ last receive flag
    cmd0source on  cmdreset
    inbuf 1+ c@ acks# and
    dup j^ ack-receive !@ xor >r
    r@ resend-toggle# and IF  true net2o:do-resend  THEN
    r@ ack-toggle# and IF  net2o:gen-resend  net2o:genack  THEN
    received!  cmd-send?
    r> ack-timing ;

: +flow-control ['] net2o:do-ack j^ ack-xt ! ;
: -flow-control ['] noop         j^ ack-xt ! ;

\ higher level functions

: map-request, ( ucode udata -- )
    2dup + n2o:new-map lit, swap lit, lit,
    map-request ;

: gen-request ( -- )
    net2o-code0  nest[ j^ lit, set-j^ ticks lit, set-rtdelay request-done ]nest
    j^ req-codesize @  j^ req-datasize @ map-request,
    key-request
    end-code ;

: ?j ]] j^ 0= ?EXIT  j^ code-map @ 0= ?EXIT [[ ; immediate

: cmd-resend? ( -- )
    j^ code-map $@ drop >r
    r@ dest-timestamps @
    r@ dest-size @ addr>replies bounds ?DO
	I 2@ d0<> IF
	    resend( ." resend: " I 2@ n2o:see F cr )
	    I 2@ cmdbuf# ! code-vdest send-cmd
	THEN
    reply +LOOP
    rdrop ;

: .expected ( -- )
    ." expected/received: " j^ recv-addr @ hex.
    j^ data-rmap $@ drop receive-flag data-firstack# @ hex.
    j^ expected @ hex. j^ received @ hex. F cr
    \ j^ data-rmap $@ drop { dmap }
    \ dmap receive-flag data-ackbit @
    \ dmap dest-size @ addr>bits bits>bytes dump
;

: transfer-keepalive? ( -- )
    j^ received @ j^ expected @ u>= ?EXIT
    resend-toggle# j^ recv-flag xor! timeout( .expected )
    cmdreset  ticks lit, timeout  false net2o:do-resend  net2o:genack
    cmd-send? ;

: connecting-timeout ( -- ) gen-request ;
: connected-timeout ( -- )  cmd-resend? transfer-keepalive? ;

: +connecting   ['] connecting-timeout j^ timeout-xt ! ;
: +resend       ['] connected-timeout  j^ timeout-xt ! ;
: -timeout      ['] noop               j^ timeout-xt ! ;

: n2o:connect ( ucode udata return-addr -- )
    n2o:new-context
    j^ req-datasize !  j^ req-codesize !
    gen-request
    [: pkc keysize $, receive-key update-key code-ivs end-cmd
      ['] end-cmd IS expect-reply? ;]  IS expect-reply?
    +connecting
    1 client-loop
    timeouts @ 0<= !!contimeout!! and F throw
    -timeout ;

: rewind? ( -- )
    j^ data-rmap $@ drop dest-round @ lit, rewind-sender ;

: net2o:do-timeout ( -- )  j^ 0= ?EXIT  j^ timeout-xt perform ;
' net2o:do-timeout IS do-timeout

previous

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z]+(" immediate (font-lock-comment-face . 1)
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