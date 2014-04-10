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

\ use a string stack to make sure that strings can only originate from
\ a string inside the command we are just executing

: @+ ( addr -- n addr' )  dup @ swap cell+ ;

4 2* cells Constant string-max#
User string-stack  string-max# uallot drop

: >$ ( addr u -- $:string )
    string-stack @+ + 2!
    2 cells string-stack +!
    string-stack @ string-max# u>=  !!string-full!! ;
: $> ( $:string -- addr u )
    string-stack @ 0<= !!string-empty!!
    -2 cells string-stack +!
    string-stack @+ + 2@ ;

: @>$ ( addr u -- $:string addr' u' )
    bounds p@+ [IFUNDEF] 64bit nip [THEN]
    swap $200000 umin bounds ( endbuf endstring startstring )
    >r over umin dup r> over umin tuck - >$ tuck - ;

: string@ ( -- $:string )
    buf-state 2@ @>$ buf-state 2! ;

\ Command streams contain both commands and data
\ the dispatcher is a byte-wise dispatcher, though
\ commands come in junks of 8 bytes
\ Commands are zero-terminated

: net2o-crash hex[
    buf-state 2@ swap 8 u.r space 8 u.r ." :" buf-state 2@ drop 1- c@ 2 u.r cr
    ]hex  buf-state 2@ dump
    true !!function!! ;
' net2o-crash IS default-method

Defer cmd-table
' cmd-class IS cmd-table

cmd-class >dynamic to cmd-class

: ?cmd  ( u -- u )  dup setup-class >methods @ u>= IF  net2o-crash  THEN ;
: ?ocmd ( u -- u )  dup o cell- @ >methods @ u>= IF  net2o-crash  THEN ;

: n>cmd ( n -- addr ) cells
    o IF  ?ocmd o cell- @  ELSE  ?cmd setup-class  THEN + ;

: cmd@ ( -- u ) buf-state 2@ over + >r p@+ r> over - buf-state 2! 64>n ;

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

: n2o.string ( $:string -- )  $>
    2dup printable? IF
	.\" \"" type
    ELSE
	.\" x\" " xtype
    THEN  .\" \" $, " ;

: .net2o-name ( n -- )  cells context-class + (net2o-see) ;

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

: n2o:see ( addr u -- ) ." net2o-code " 
    BEGIN  cmd-see dup 0= UNTIL  2drop ;

: cmd-dispatch ( addr u -- addr' u' )
    buf-state 2!
    cmd@ trace( .s cr ) n>cmd
    perform buf-state 2@ ;

: >cmd ( xt u -- ) cells >r
    cmd-table r@ cell+ class-resize action-of cmd-table (int-to)
    cmd-table r> + ! ;

Defer >throw

\ commands

Defer net2o-do

: net2o-does  DOES> net2o-do ;
: net2o: ( number "name" -- )
    ['] noop over >cmd \ allocate space in table
    Create dup >r , here >r 0 , net2o-does noname :
    lastxt dup r> ! r> >cmd ;
: +net2o: ( "name" -- ) cmd-table >methods @ cell/ net2o: ;

: F also forth parse-name parser1 execute previous ; immediate

: un-cmd ( -- )  0. buf-state 2!  0 >o rdrop ;

Vocabulary net2o-base

get-current also net2o-base definitions previous

\ Command numbers preliminary and subject to change

0 net2o: end-cmd ( -- ) \ last command in buffer
    0. buf-state 2! ;
+net2o: ulit ( "u" -- u ) \ unsigned literal
    p@ ;
+net2o: slit ( "n" -- n ) \ signed literal, zig-zag encoded
    ps@ ;
+net2o: string ( "string" -- $:string ) \ string literal
    string@ ;
+net2o: dflit ( "dfloat" -- r ) \ double float literal
    buf-state 2@ over + >r dup df@ dfloat+ r> over - buf-state 2! ;
+net2o: sflit ( "sfloat" -- r ) \ double float literal
    buf-state 2@ over + >r dup sf@ sfloat+ r> over - buf-state 2! ;
+net2o: tru ( -- true ) \ true flag literal
    true ;
+net2o: fals ( -- false ) \ false flag literal
    false ;

dup set-current

' setup-class is cmd-table

setup-class cmd-class >inherit to setup-class

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
    ." see-me: " dest-addr 64@ $64.
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
    +send-cmd dest-addr 64@ 64>r set-dest
    cmd( ." send: " dest-addr 64@ $64. dup buf# n2o:see cr )
    max-size^2 1+ 0 DO
	buf# min-size I lshift u<= IF
	    I send-cX  cmdreset  UNLOOP
	    64r> dest-addr 64! EXIT  THEN
    LOOP  64r> dest-addr 64!  true !!commands!! ;

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

: acked ( -- ) \ replace key with random stuff
    state# rng$ last-ivskey @ swap move ;
: net2o:ok? ( -- )  o?
    tag-addr >r cmdbuf$ r@ 2!
    tag( ." tag: " tag-addr dup hex. 2@ swap hex. hex. F cr )
    code-vdest r> reply-dest 64! ;
: net2o:ok ( tag -- )
    timeout( ." ack: " dup hex. F cr )
    o 0= IF  drop EXIT  THEN
    resend0 $off
    nat( ." ok from: " ret-addr $10 xtype space dup .
    dup reply[] 2@ d0= IF ." acked"  THEN cr )
    send-list $[]off  acked
    0. rot reply[] 2! ; \ clear request
: net2o:expect-reply ( -- )  o?
    timeout( ." expect: " cmdbuf$ n2o:see )
    cmdbuf$ code-reply dup >r 2! code-vdest r> reply-dest 64! ;

: restore-punch ( -- )
    inbuf 1+ c@ punching# and IF
	msg( ." backup: " return-backup $10 xtype F cr )
	return-backup return-address $10 move
    THEN ;
: backup-punch ( -- )
    inbuf 1+ c@ punching# and IF
	return-address return-backup $10 move
    THEN ;
: tag-addr? ( -- flag )
    tag-addr dup >r 2@ dup IF
	cmd( dest-addr 64@ $64. ." resend canned code reply " tag-addr hex. cr )
	r> reply-dest 64@ send-cmd true
	1 packets2 +!  restore-punch
    ELSE  d0<> -1 0 r> 2!  THEN ;

Variable throwcount

: do-cmd-loop ( addr u -- )
    cmd( dest-addr 64@ $64. 2dup n2o:see )
    sp@ >r throwcount off
    [: BEGIN   cmd-dispatch  dup 0<=  UNTIL ;] catch
    dup IF   1 throwcount +!
	[: ." do-cmd-loop: " dup . .exe cr DoError ;] $err nothrow
	buf-state @ show-offset !  n2o:see-me  show-offset on
	throwcount @ 4 < IF  >throw  THEN  THEN
    drop  r> sp! 2drop +cmd ;

: cmd-loop ( addr u -- )
    string-stack off
    o IF
	cmd0source off
	tag-addr?  IF  2drop  >flyburst  1 packetr2 +!  EXIT  THEN
	backup-punch
    ELSE
	cmd0buf cmd0source !
    THEN
    [: cmdreset  do-cmd-loop  cmd-send? ;] cmdlock c-section ;

' cmd-loop is queue-command

\ nested commands

: >initbuf ( addr u -- addr' u' ) tuck
    init0buf mykey-salt# + swap move
    maxdata  BEGIN  2dup 2/ u<  WHILE  2/ dup min-size = UNTIL  THEN
    nip init0buf swap mykey-salt# + 2 64s + ;

4 Constant maxnest#
User neststart#
User neststack maxnest# cells uallot drop \ nest up to 10 levels

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
: cmdbuf+ ( n -- )  dup maxstring u>= !!stringfit!! cmdbuf# +! ;
: lit, ( u -- )  ulit cmd, ;
: slit, ( n -- )  slit n>zz cmd, ;
: nlit, ( n -- )  n>64 slit, ;
: ulit, ( u -- )  u>64 lit, ;
: sfloat, ( r -- )  sflit cmdbuf$ + sf! 1 sfloats cmdbuf+ ;
: dfloat, ( r -- )  sflit cmdbuf$ + df! 1 dfloats cmdbuf+ ;
: flag, ( flag -- ) IF tru ELSE fals THEN ;
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
+net2o: emit ( xc -- ) \ emit character on server log
    64>n xemit ;
+net2o: type ( $:string -- ) \ type string on server log
    $> F type ;
+net2o: . ( -- ) \ print number on server log
    64. ;
+net2o: cr ( -- ) \ newline on server log
    F cr ;
+net2o: see-me ( -- ) \ see received commands on server log
    n2o:see-me ;

+net2o: push-$ ( $:string -- ) \ push string into answer packet
    $> $, ;
+net2o: push-slit ( n -- ) \ push singed literal into answer packet
    slit, ;
+net2o: push-lit ( u -- ) \ push unsigned literal into answer packet
    lit, ;
' push-lit alias push-char

+net2o: push' ( "cmd" -- ) \ push command into answer packet
    p@ cmd, ;
+net2o: nest ( $:string -- ) \ nested (self-encrypted) command
    $> cmdnest ;
+net2o: tmpnest ( $:string -- ) \ nested (temporary encrypted) command
    $> cmdtmpnest ;

: ]nest  ( -- )  end-cmd cmd>nest $, push-$ push' nest ;
: ]tmpnest ( -- )  end-cmd cmd>tmpnest $, tmpnest ;

+net2o: new-data ( addr addr u -- ) \ create new data mapping
    o 0<> tmp-crypt? and own-crypt? or IF  64>n  n2o:new-data  EXIT  THEN
    64drop 64drop 64drop  un-cmd ;
+net2o: new-code ( addr addr u -- ) \ crate new code mapping
    o 0<> tmp-crypt? and own-crypt? or IF  64>n  n2o:new-code  EXIT  THEN
    64drop 64drop 64drop  un-cmd ;
+net2o: request-done ( -- ) \ signal request is completed
    own-crypt? IF  n2o:request-done  THEN ;
+net2o: set-rtdelay ( timestamp -- ) \ set round trip delay
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

+net2o: store-key ( $:string -- ) $> \ store key
    o 0= IF  ." don't store key, o=0: " .nnb F cr un-cmd  EXIT  THEN
    own-crypt? IF
	key( ." store key: o=" o hex. 2dup .nnb F cr )
	2dup do-keypad $!
	crypto-key $!
    ELSE  ." don't store key: o=" o hex. .nnb F cr  THEN ;

+net2o: map-request ( addrs ucode udata -- ) \ request mapping
    2*64>n
    nest[
    ?new-mykey ticker 64@ lit, set-rtdelay
    max-data# umin swap max-code# umin swap
    2dup + n2o:new-map n2o:create-map
    keypad keysize $, store-key  stskc KEYSIZE erase
    ]nest  n2o:create-map  neststack @ IF  ]tmpnest  THEN
    64drop 2drop 64drop ;

+net2o: disconnect ( -- ) \ close connection
    o 0= ?EXIT n2o:dispose-context un-cmd ;
+net2o: set-tick ( ticks -- ) \ adjust time
    adjust-ticks ;
+net2o: get-tick ( -- ) \ request time adjust
    ticks lit, set-tick ;

net2o-base

\ crypto functions

+net2o: receive-key ( $:string -- ) $> \ receive a key
    crypt( ." Received key: " tmpkey@ .nnb F cr )
    tmp-crypt? IF  net2o:receive-key  ELSE  2drop  THEN ;
+net2o: key-request ( -- ) \ request a key
    crypt( ." Nested key: " tmpkey@ .nnb F cr )
    nest[ pkc keysize $, receive-key ;
+net2o: receive-tmpkey ( $:string -- ) $> \ receive emphemeral key
    net2o:receive-tmpkey ;
+net2o: tmpkey-request ( -- ) \ request ephemeral key
    stpkc keysize $, receive-tmpkey ;
+net2o: update-key ( -- ) \ update secrets
    net2o:update-key ;
+net2o: gen-ivs ( $:string -- ) \ generate IVs
    $> ivs-strings receive-ivs ;

\ nat traversal functions

+net2o: punch ( $:string -- ) \ punch NAT traversal hole
    $> net2o:punch ;

: gen-punch ( -- ) my-ip$ [: $, punch ;] $[]map ;

+net2o: punch? ( -- ) \ Request punch addresses
    gen-punch ;

\ everything that follows here can assume to have a connection context

' context-class is cmd-table

context-class setup-class >inherit to context-class

\ file functions

40 net2o: open-file ( $:string mode id -- ) \ open file id at path "addr u" with mode
    2*64>n 2>r $> 2r> n2o:open-file ;
+net2o: close-file ( id -- ) \ close file
    64>n n2o:close-file ;
+net2o: file-size ( id -- size ) \ obtain file size
    id>addr? fs-size 64@ ;
+net2o: send-chunks ( -- ) \ start sending chunks
    net2o:send-chunks ;
+net2o: set-blocksize ( n -- ) \ set blocksize
    64>n blocksize! ;
+net2o: set-blockalign ( n -- ) \ set block alignment
    64>n pow2?  blockalign ! ;
+net2o: close-all ( -- ) \ close all files
    n2o:close-all ;

: blocksize! ( n -- )  dup ulit, set-blocksize blocksize! ;
: blockalign! ( n -- )  dup ulit, set-blockalign pow2? blockalign ! ;

\ flow control functions

50 net2o: ack-addrtime ( time addr -- ) \ packet at addr received at time
    net2o:ack-addrtime ;
+net2o: ack-resend ( flag -- ) \ set resend toggle flag
    64>n  net2o:ack-resend ;
+net2o: set-rate ( rate delta-t -- ) \ set rate 
    cookie? IF  net2o:set-rate
    ELSE  64drop 64drop ns/burst dup @ 2* 2* swap !  THEN ;
+net2o: resend-mask ( addr mask -- ) \ resend mask blocks starting at addr
    2*64>n net2o:resend-mask net2o:send-chunks ;
+net2o: track-timing ( -- ) \ track timing
    net2o:track-timing ;
+net2o: rec-timing ( $:string -- ) \ recorded timing
    $> net2o:rec-timing ;
+net2o: send-timing ( -- ) \ request recorded timing
    net2o:timing$ maxtiming umin tuck $,
    net2o:/timing rec-timing ;
+net2o: >time-offset ( n -- ) \ set time offset
    time-offset 64! ;
: time-offset! ( -- )  ticks 64dup lit, >time-offset time-offset 64! ;
+net2o: ack-b2btime ( time addr -- ) \ burst-to-burst time at packet addr
    net2o:ack-b2btime ;
+net2o: ack-cookies ( cookie addr mask -- ) \ acknowledge cookie
    [IFUNDEF] 64bit 64>r 64>n 64r> [THEN]
    data-map @ cookie+ 64over 64over 64= 0= IF
	." cookies don't match!" 64over .16 space 64dup .16 F cr
    THEN
    64= cookie-val and validated or! ;
+net2o: ack-flush ( addr -- ) \ flushed to addr
    64>n net2o:rewind-sender-partial ;
+net2o: set-head ( addr -- ) \ set head
    64>n data-rmap @ >o dest-head umax! o> ;
+net2o: timeout ( ticks -- ) \ timeout request
    net2o:timeout  data-map @ >o dest-tail @ o> ulit, set-head ;
+net2o: set-top ( top flag -- ) \ set top, flag is true when all data is sent
    >r 64>n r> data-rmap @ >o over dest-top @ <> and dest-end or! dest-top! o> ;

+net2o: ok ( tag -- ) \ tagged response
    64>n net2o:ok ;
+net2o: ok? ( tag -- ) \ request tagged response
    net2o:ok? lit, ok ;
\ Use ko instead of throw for not acknowledge (kudos to Heinz Schnitter)
+net2o: ko ( error -- ) \ receive error message
    throw ;

\ create commands to send back

: all-ivs ( -- ) \ Seed and gen all IVS
    state# rng$ 2dup $, gen-ivs ivs-strings send-ivs ;

+net2o: gen-reply ( -- ) \ generate a key request reply reply
    [: crypt( ." Reply key: " tmpkey@ .nnb F cr )
      nest[ pkc keysize $, receive-key update-key all-ivs gen-punch time-offset! ]tmpnest
      push-cmd ;]  IS expect-reply? ;

\ better slurping

70 net2o: track-size ( size id -- ) \ set size attribute of file id
    64>n track( >r ." file <" r@ 0 .r ." > size: " 64dup 64. F cr r> ) size! ;
+net2o: track-seek ( seek id -- ) \ set seek attribute of file id
    64>n track( >r ." file <" r@ 0 .r ." > seek: " 64dup 64. F cr r> ) seekto! ;
+net2o: track-limit ( limit id -- ) \ set limit attribute of file id
    64>n track( >r ." file <" r@ 0 .r ." > seek to: " 64dup 64. F cr r> ) limit! ;

:noname ( id seek -- ) lit, ulit, track-seek ; is do-track-seek

+net2o: set-stat ( mtime mod id -- ) \ set time and mode of file id
    2*64>n n2o:set-stat ;
+net2o: get-stat ( id -- ) \ request stat of file id
    64>n { fd }
    fd n2o:get-stat >r lit, r> ulit, fd ulit, set-stat ;
+net2o: open-tracked-file ( $:string mode id -- ) \ open file in tracked mode
    2*64>n 2>r $> 2r> dup >r n2o:open-file
    r@ id>addr? >o fs-size 64@ o> lit, r@ ulit, track-size
    r@ n2o:get-stat >r lit, r> ulit, r> ulit, set-stat ;
+net2o: slurp ( -- ) \ slurp in tracked files
    n2o:slurp swap ulit, flag, set-top
    ['] do-track-seek n2o:track-all-seeks ;
+net2o: rewind-sender ( n -- ) \ rewind buffer
    64>n net2o:rewind-sender ;

\ ids 100..120 reserved for key exchange/storage

\ profiling, nat traversal

120 net2o: !time ( -- ) \ start timer
    F !time init-timer ;
+net2o: .time ( -- ) \ print timer to server log
    F .time .packets profile( .times ) ;

+net2o: set-ip ( $:string -- ) \ set address information
    $> setip-xt perform ;
+net2o: get-ip ( -- ) \ request address information
    >sockaddr $, set-ip [: $, set-ip ;] n2oaddrs ;

: net2o:gen-resend ( -- )
    recv-flag @ invert resend-toggle# and ulit, ack-resend ;
: net2o:ackflush ( n -- ) ulit, ack-flush ;
: n2o:done ( -- )  slurp ;

: rewind-total ( -- )
    data-rmap @ >o dest-round @ 1+ o> dup net2o:rewind-receiver
    ulit, rewind-sender ;

: rewind-flush ( -- )
    data-rmap @ >o dest-back @ do-slurp @ umax o> net2o:ackflush ;

: rewind ( -- )
    save( rewind-flush )else( rewind-total ) ;

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
    timing( 64>r 64dup $64. 64r> 64dup 64. ." acktime" F cr )
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
    save( u> IF  net2o:save& 64#0 burst-ticks 64!  THEN )else( 2drop ) ;
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
    data-rmap @ >o 0 dest-end !@ o> ;

: rewind-transfer ( -- )
    rewind data-end? IF  n2o:request-done  ELSE  restart-transfer  THEN
    save( )else( request-stats? IF  send-timing  THEN ) ;

: request-stats   F true to request-stats?  track-timing ;

: expected@ ( -- head top )
    o IF  data-rmap @ >o
	o IF  dest-tail @ dest-top @  ELSE  0.  THEN o>
    ELSE  0.  THEN  ;

: expected? ( -- )
    expected@ tuck u>= and IF
	expect-reply
	msg( ." check: " data-rmap @ >o dest-back @ hex. dest-tail @ hex. dest-head @ hex.
	data-ackbits @ data-ack# @ dup hex. + l@ hex.
	o> F cr )
	msg( ." Block transfer done: " expected@ hex. hex. F cr )
	save-all-blocks  net2o:ack-cookies  rewind-transfer
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

: +cookie ( -- )
    data-rmap @ >o ack-bit# @ dup +ackbit
    \ set bucket as received in current polarity bitmap
    data-ackbits @ swap +bit@ o> negate packetr2 +! ;

: +expected ( -- )
    data-rmap @ >o dest-head @ dest-top @ u>= ack-advance? @ and o>
    IF   expect-reply resend-all  THEN  expected? ;

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
    tmpkey-request key-request punch? other-xt perform
    req-codesize @  req-datasize @  map-request,
    ['] push-cmd IS expect-reply?
    end-code ;

: 0-resend? ( -- )
    resend0 @ IF
	\ ." Resend to 0" cr
	cmd0buf cmd0source !
	[: resend0 $@ >r cmdbuf r@ move
	  cmdbuf r> 64#0 send-cmd 1 packets2 +! ;]
	cmdlock c-section
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

: net2o:ack-code ( ackflag -- ackflag' )
    net2o-code
    dup ack-receive !@ xor >r
    r@ ack-toggle# and IF
	r@ resend-toggle# and IF
	    data-rmap @ >o dest-head @ addr>bits data-reack# ! o>
	    true net2o:do-resend
	THEN
	data-rmap @ >o 0 do-slurp !@ o>
	?dup-IF  net2o:ackflush slurp request-stats? IF  send-timing  THEN THEN
	net2o:gen-resend  net2o:genack	map-resend?
    THEN  +expected
    end-code r> ;

: net2o:do-ack ( -- )
    dest-addr 64@ recv-addr 64! \ last received packet
    recv-cookie +cookie
    inbuf 1+ c@ dup recv-flag ! \ last receive flag
    acks# and data-rmap @ >o ack-advance? @ o>
    IF  net2o:ack-code   ELSE  ack-receive @ xor  THEN  ack-timing ;

: +flow-control ['] net2o:do-ack ack-xt ! ;
: -flow-control ['] noop         ack-xt ! ;

\ keepalive

also net2o-base
: transfer-keepalive? ( -- )
    expected@ u>= ?EXIT
    net2o-code  expect-reply
    update-rtdelay  ticks lit, timeout
    save( slurp send-chunks )  resend-all  net2o:genack end-code ;
previous

: connected-timeout ( -- )
    timeout( .expected )
    packets2 @ cmd-resend? packets2 @ = IF  transfer-keepalive?  THEN ;

\ : +connecting   ['] connecting-timeout timeout-xt ! ;
: +resend       ['] connected-timeout  timeout-xt ! ;

: +get-time     ['] get-tick other-xt ! ;
: -other        ['] noop other-xt ! ;

: n2o:connect ( ucode udata -- )
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
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
     (("[:") (0 . 1) (0 . 1) immediate)
     ((";]") (-1 . 0) (0 . -1) immediate)
    )
End:
[THEN]