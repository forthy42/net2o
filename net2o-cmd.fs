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

\ string debugging

: printable? ( addr u -- flag )
    true -rot bounds ?DO  I c@ $7F and bl < IF  drop false  LEAVE  THEN  LOOP ;

: n2o:$. ( addr u -- )
    2dup printable? IF
	.\" \"" type
    ELSE
	.\" 85\" " 85type
    THEN  '"' emit ;
: n2o.string ( $:string -- )  cr $> n2o:$. ."  $, " ;

: $.s ( $string1 .. $stringn -- )
    string-stack @+ swap bounds U+DO
	cr i 2@ n2o:$.
    2 cells +LOOP ;

\ object stack

8 cells Constant object-max#

User object-stack object-max# uallot drop

: o-pop ( o:o1 o:x -- o1 o:x )
    object-stack @ 0<= !!object-empty!!
    -1 cells object-stack +!
    object-stack @+ + @ ;
: o-push ( o1 o:x -- o:o1 o:x )
    object-stack @+ + !
    cell object-stack +!
    object-stack @ object-max# u>= !!object-full!! ;

: n:>o ( o1 o:o2 -- o:o2 o:o1 )
    >o r> o-push ;
: n:o> ( o:o2 o:o1 -- o:o2 )
    o-pop >r o> ;
: n:oswap ( o:o1 o:o2 -- o:o2 o:o1 )
    o-pop >o r> o-push ;

\ token stack - only for decompiling

User t-stack

: t-push ( addr -- )
    t-stack $[]# t-stack $[] ! ;
: t-pop ( -- addr )
    t-stack $[]# 1- dup 0< !!object-empty!!
    t-stack $[] @ t-stack $@len cell- t-stack $!len ;

\ float are stored big endian.

: pf@+ ( addr u -- addr' u' r )
    2>r 64 64#0 2r> bounds ?DO
	7 64lshift I c@ $7F and n>64 64+ 64>r 7 - 64r>
	I c@ $80 and  0= IF
	    n64-swap 64lshift
	    0e { f^ pftmp } pftmp 64! pftmp f@
	    I 1+ I' over - unloop  EXIT  THEN
    LOOP   true !!floatfit!!  ;

: pf!+ ( addr r -- addr' ) { f^ pftmp }
    BEGIN
	pftmp 64@ 57 64rshift 64>n
	pftmp 64@ 7 64lshift 64dup pftmp 64!
	64-0<> WHILE  $80 or over c! 1+  REPEAT
    over c! 1+ ;

: pf@ ( -- r )
    buf-state 2@ pf@+ buf-state 2! ;

\ Command streams contain both commands and data
\ the dispatcher is a byte-wise dispatcher, though
\ commands come in junks of 8 bytes
\ Commands are zero-terminated

: net2o-crash true !!function!! ;

Defer gen-table
' cmd-table IS gen-table

: $freeze ( addr -- )
    \G copy string to dictionary
    >r r@ $@  align here r> !
    dup , here swap dup allot move align ;

: n>cmd ( n -- addr ) cells >r
    o IF  token-table  ELSE  setup-table  THEN
    $@ r@ u<= !!function!! r> + ;

: cmd@ ( -- u ) buf-state 2@ over + >r p@+ r> over - buf-state 2! 64>n ;

-5 cells 0 +field net2o.name
drop

: >net2o-name ( addr -- addr' u )
    net2o.name body> name>string ;

: (net2o-see) ( addr -- )  @
    dup 0<> IF
	net2o.name
	dup 2 cells + @ ?dup-IF  @ token-table @ t-push token-table !  THEN
	body>
    ELSE  drop ['] net2o-crash  THEN  .name ;

: .net2o-num ( off -- )  cell/ '<' emit 0 .r '>' emit space ;
: .net2o-name ( n -- )  cells >r
    o IF  token-table  ELSE  setup-table  THEN $@ r@ u<=
    IF  drop r> .net2o-num  EXIT  THEN  r> + (net2o-see) ;
: .net2o-name' ( n -- )  cells >r
    o IF  token-table  ELSE  setup-table  THEN $@ r@ u<=
    IF  drop r> .net2o-num  EXIT  THEN  r> + @
    dup 0<> IF
	net2o.name body>
    ELSE  drop ['] net2o-crash  THEN  .name ;

: net2o-see ( cmd -- ) hex[
    case
	0 of  ." end-code" cr 0. buf-state 2!  endof
	1 of  p@ 64. ." lit, "  endof
	2 of  ps@ s64. ." slit, " endof
	3 of  string@  n2o.string  endof
	4 of  pf@ f. ." float, " endof
	5 of  ." endwith " cr  t-pop  token-table !  endof
	6 of  ." oswap " cr token-table @ t-pop token-table ! t-push  endof
	.net2o-name
	0 endcase ]hex ;

Variable show-offset  show-offset on

: cmd-see ( addr u -- addr' u' )
    dup show-offset @ = IF  ." <<< "  THEN
    buf-state 2! p@ 64>n net2o-see buf-state 2@ ;

: n2o:see ( addr u -- ) ." net2o-code "  t-stack $off
    o IF  token-table @ >r  THEN
    [: BEGIN  cmd-see dup 0= UNTIL ;] catch
    o IF  r> token-table !  THEN  throw  2drop ;

: cmd-dispatch ( addr u -- addr' u' )
    buf-state 2!
    cmd@ trace( dup IF dup .net2o-name' THEN >r .s r> $.s cr ) n>cmd
    @ ?dup-IF  execute  ELSE
	trace( ." crashing" cr cr ) net2o-crash  THEN  buf-state 2@ ;

: >cmd ( xt u -- ) gen-table $[] ! ;

Defer >throw

\ commands

User cmd0source
User cmdbuf#

: cmdbuf     ( -- addr )  cmd0source @ dup 0= IF
	drop connection .code-dest  THEN ;
: cmdlock    ( -- addr )  cmd0source @ IF  cmd0lock  ELSE
	connection .code-lock THEN ;
: cmdbuf$ ( -- addr u )   cmdbuf cmdbuf# @ ;
: endcmdbuf  ( -- addr' ) cmdbuf maxdata + ;
: maxstring ( -- n )  endcmdbuf cmdbuf$ + - ;
: cmdbuf+ ( n -- )
    dup maxstring u>= !!stringfit!! cmdbuf# +! ;

: cmd, ( 64n -- )  cmdbuf$ + dup >r p!+ r> - cmdbuf+ ;
: flit, ( 64n -- )  cmdbuf$ + dup >r pf!+ r> - cmdbuf+ ;

: net2o, @ n>64 cmd, ;

0 Value last-2o

: net2o-does  DOES> net2o, ;
: net2o: ( number "name" -- )
    ['] noop over >cmd \ allocate space in table
    Create  here to last-2o
    dup >r , here >r 0 , 0 , net2o-does noname :
    lastxt dup r> ! r> >cmd ;
: +net2o: ( "name" -- ) gen-table $[]# net2o: ;
: >table ( table -- )  last-2o 2 cells + ! ;
: net2o' ( "name" -- ) ' >body @ ;

: F also forth parse-name parser1 execute previous ; immediate

: un-cmd ( -- )  0. buf-state 2!  0 >o rdrop ;

Defer net2o:words

: inherit-table ( addr u "name" -- )
    ' dup IS gen-table  execute $! ;

Vocabulary net2o-base

get-current also net2o-base definitions previous

\ Command numbers preliminary and subject to change

0 net2o: dummy ( -- ) ; \ alias
0 net2o: end-cmd ( -- ) 0 buf-state ! ;
+net2o: ulit ( #u -- u ) \ unsigned literal
    p@ ;
+net2o: slit ( #n -- n ) \ signed literal, zig-zag encoded
    ps@ ;
+net2o: string ( #string -- $:string ) \ string literal
    string@ ;
+net2o: flit ( #dfloat -- r ) \ double float literal
    pf@ ;
+net2o: endwith ( o:object -- ) \ last command in buffer
    n:o> ;
+net2o: oswap ( o:nest o:current -- o:current o:nest )
    n:oswap ;
+net2o: tru ( -- f:true ) \ true flag literal
    true ;
+net2o: fals ( -- f:false ) \ false flag literal
    false ;
+net2o: words ( ustart -- ) \ reflection
    64>n net2o:words ;

dup set-current

gen-table $freeze
gen-table $@ inherit-table reply-table

\ net2o assembler

: n2o:see-me ( -- )
    buf-state 2@ 2>r
    ." see-me: " dest-addr 64@ $64.
    \ tag-addr dup hex. 2@ swap hex. hex. F cr
    inbuf packet-data n2o:see
    2r> buf-state 2! ;

: cmdreset  cmdbuf# off ;

: net2o-code    cmd0source off  cmdlock lock
    cmdreset 1 code+ also net2o-base ;
comp: :, also net2o-base ;
: net2o-code0   cmd0buf cmd0source !   cmdlock lock
    cmdreset also net2o-base ;
comp: :, also net2o-base ;

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
    connection >o cmdbuf cmdbuf# @ cmddest send-cmd
    cmd0source @ 0= IF  code-update punch-load $off  THEN o> ;

also net2o-base

UDefer expect-reply?
' end-cmd IS expect-reply?

:noname  ['] end-cmd IS expect-reply? ; is init-reply

: cmd-send? ( -- )
    cmdbuf# @ IF  expect-reply? cmd connection IF  code-update THEN  THEN ;

previous

: acked ( -- ) \ replace key with random stuff
    state# rng$ last-ivskey @ swap move ;
: net2o:ok? ( -- )  o?
    tag-addr >r cmdbuf$ r@ 2!
    tag( ." tag: " tag-addr dup hex. 2@ swap hex. hex. F cr )
    code-vdest r> reply-dest 64! ;
: net2o:ok ( tag -- )
    timeout( cmd( ." ack: " dup hex. F cr ) )
    o 0= IF  drop EXIT  THEN
    resend0 $off
    nat( ." ok from: " ret-addr $10 xtype space dup .
    dup reply[] 2@ d0= IF ." acked"  THEN cr )
    acked  0. rot reply[] 2! ; \ clear request
: net2o:expect-reply ( -- )  o?
    timeout( cmd( ." expect: " cmdbuf$ n2o:see ) )
    cmdbuf$
    connection >o code-reply dup >r 2! code-vdest r> reply-dest 64! o> ;

: tag-addr? ( -- flag )
    tag-addr dup >r 2@
    ?dup-IF
	cmd( dest-addr 64@ $64. ." resend canned code reply " tag-addr hex. cr )
	r> reply-dest 64@ send-cmd true
	1 packets2 +!
    ELSE  dest-addr 64@ [ cell 4 = ] [IF] 0<> - [THEN] dup 0 r> 2! u>=  THEN ;

Variable throwcount

: do-cmd-loop ( addr u -- )
    cmd( dest-addr 64@ $64. 2dup n2o:see )
    sp@ >r throwcount off
    [: BEGIN   cmd-dispatch dup 0<=  UNTIL ;] catch
    dup IF   1 throwcount +!
	[: ." do-cmd-loop: " dup . .exe cr ;] $err
	dup DoError  nothrow
	buf-state @ show-offset !  n2o:see-me  show-offset on
	un-cmd  throwcount @ 4 < IF  >throw  THEN  THEN
    r> sp! 2drop +cmd ;

: cmd-loop ( addr u -- )
    string-stack off  object-stack off  o to connection
    o IF
	maxdata code+
	cmd0source off
	tag-addr? IF
	    2drop  >flyburst  1 packetr2 +!  EXIT  THEN
    ELSE
	cmd0buf cmd0source !
    THEN
    [: cmdreset  do-cmd-loop  cmd-send? ;] cmdlock c-section ;

' cmd-loop is queue-command

\ nested commands

: >initbuf ( addr u -- addr' u' ) tuck
    init0buf mykey-salt# + swap move dfaligned
    \ maxdata  BEGIN  2dup 2/ u<  WHILE  2/ dup $20 = UNTIL  THEN  nip
    init0buf swap mykey-salt# + 2 64s + ;

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
    r> validated !
    2r> buf-state cell+ @ IF  buf-state 2!  ELSE  2drop  THEN ;

: cmdnest ( addr u -- )  mykey-decrypt$
    IF  own-crypt-val do-nest  ELSE  un-cmd  THEN ;

: cmdtmpnest ( addr u -- )
    $>align tmpkey@ drop keysize decrypt$
    IF  tmp-crypt-val do-nest  ELSE  un-cmd  THEN ;

\ net2o assembler stuff

also net2o-base definitions

: maxtiming ( -- n )  maxstring timestats - dup timestats mod - ;
: $, ( addr u -- )  string dup >r n>64 cmd,
    r@ maxstring u>= !!stringfit!!
    cmdbuf$ + r@ move   r> cmdbuf# +! ;
: lit, ( u -- )  ulit cmd, ;
: slit, ( n -- )  slit n>zz cmd, ;
: nlit, ( n -- )  n>64 slit, ;
: ulit, ( u -- )  u>64 lit, ;
: float, ( r -- )  flit flit, ;
: flag, ( flag -- ) IF tru ELSE fals THEN ;
: (end-code) ( -- ) expect-reply? cmd  cmdlock unlock ;
: end-code ( -- ) (end-code) previous ;
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
$10 net2o: <req ( -- ) ; \ stub: push own id in reply
+net2o: push-lit ( u -- ) \ push unsigned literal into answer packet
    lit, ;
' push-lit alias push-char
+net2o: push-slit ( n -- ) \ push singed literal into answer packet
    slit, ;
+net2o: push-$ ( $:string -- ) \ push string into answer packet
    $> $, ;
+net2o: push-float ( r -- ) \ push floating point number
    float, ;
+net2o: push' ( #cmd -- ) \ push command into answer packet
    p@ cmd, ;
+net2o: ok ( utag -- ) \ tagged response
    64>n net2o:ok ;
+net2o: ok? ( utag -- ) \ request tagged response
    net2o:ok? lit, ok ;
\ Use ko instead of throw for not acknowledge (kudos to Heinz Schnitter)
+net2o: ko ( uerror -- ) \ receive error message
    throw ;
+net2o: nest ( $:string -- ) \ nested (self-encrypted) command
    $> cmdnest ;
+net2o: req> ( -- ) \ end of request
    endwith ;
+net2o: request-done ( ureq -- ) 64>n \ signal request is completed
    o 0<> own-crypt? and IF  n2o:request-done  ELSE  drop  THEN ;

\ inspection

+net2o: token ( $:token n -- ) 64drop $> 2drop ; \ stub

:noname ( start -- )
    token-table $@ 2 pick cells safe/string bounds U+DO
	I @ ?dup-IF
	    >net2o-name dup $A0 + maxstring < IF
		2 pick ulit, [: type ." (-)" ;] $tmp $, token
	    ELSE  2drop  THEN
	THEN  1+
    cell +LOOP  drop ; IS net2o:words

gen-table $freeze

\ log dump class

gen-table $@ inherit-table log-table

net2o' token net2o: log-token ( $:token n -- )
    64>n 0 .r ." :" $> F type space ;

$20 net2o: emit ( xc -- ) \ emit character on server log
    64>n xemit ;
+net2o: type ( $:string -- ) \ type string on server log
    $> F type ;
+net2o: . ( u -- ) \ print number on server log
    64. ;
+net2o: f. ( -- ) \ print fp number on server log
    F f. ;
+net2o: cr ( -- ) \ newline on server log
    F cr ;
+net2o: .time ( -- ) \ print timer to server log
    F .time .packets profile( .times ) ;
+net2o: !time ( -- ) \ start timer
    F !time init-timer ;

gen-table $freeze

\ setup connection class

reply-table $@ inherit-table setup-table

$20 net2o: log ( -- o:log ) log-context @ n:>o ;
log-table >table

+net2o: tmpnest ( $:string -- ) \ nested (temporary encrypted) command
    $> cmdtmpnest ;

: ]nest$  ( -- )  end-cmd cmd>nest $, ;
: ]nest  ( -- )  ]nest$ push-$ push' nest ;
: ]tmpnest ( -- )  end-cmd cmd>tmpnest $, tmpnest ;

+net2o: new-data ( addr addr u -- ) \ create new data mapping
    o 0<> tmp-crypt? and own-crypt? or IF  64>n  n2o:new-data  EXIT  THEN
    64drop 64drop 64drop  un-cmd ;
+net2o: new-code ( addr addr u -- ) \ crate new code mapping
    o 0<> tmp-crypt? and own-crypt? or IF  64>n  n2o:new-code  EXIT  THEN
    64drop 64drop 64drop  un-cmd ;
+net2o: set-rtdelay ( utimestamp -- ) \ set round trip delay
    o IF  rtdelay!  EXIT  THEN
    own-crypt? IF
	64dup cookie>context?
	IF  >o rdrop  o to connection
	    ticker 64@ recv-tick 64! rtdelay! \ time stamp of arrival
	    EXIT
	ELSE \ just check if timeout didn't expire
	    ticker 64@ connect-timeout# 64- 64u< 0= ?EXIT
	THEN
    ELSE  64drop  THEN  un-cmd ;
+net2o: disconnect ( -- ) \ close connection
    o 0= ?EXIT n2o:dispose-context un-cmd ;

: n2o:create-map
    { 64: addrs ucode udata 64: addrd -- addrd ucode udata addrs }
    addrs lit, addrd lit, ucode ulit, new-code
    addrs ucode n>64 64+ lit, addrd ucode n>64 64+ lit, udata ulit, new-data
    addrd ucode udata addrs ;

+net2o: store-key ( $:string -- ) $> \ store key
    o 0= IF  ." don't store key, o=0: " .nnb F cr un-cmd  EXIT  THEN
    own-crypt? IF
	key( ." store key: o=" o hex. 2dup .nnb F cr )
	2dup do-keypad sec!
	crypto-key sec!
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

+net2o: set-tick ( uticks -- ) \ adjust time
    adjust-ticks ;
+net2o: get-tick ( -- ) \ request time adjust
    ticks lit, set-tick ;

net2o-base

\ crypto functions

+net2o: receive-key ( $:key -- ) $> \ receive a key
    crypt( ." Received key: " tmpkey@ .nnb F cr )
    tmp-crypt? IF  net2o:receive-key  ELSE  2drop  THEN ;
+net2o: receive-tmpkey ( $:key -- ) $> \ receive emphemeral key
    net2o:receive-tmpkey ;
+net2o: key-request ( -- ) \ request a key
    crypt( ." Nested key: " tmpkey@ .nnb F cr )
    pkc keysize $, receive-key ;
+net2o: tmpkey-request ( -- ) \ request ephemeral key
    stpkc keysize $, receive-tmpkey nest[ ;
+net2o: keypair ( $:yourkey $:mykey -- ) $> $> 2swap \ select a pubkey
    tmp-crypt? IF  net2o:keypair  ELSE  2drop 2drop  THEN ;
+net2o: update-key ( -- ) \ update secrets
    net2o:update-key ;
+net2o: gen-ivs ( $:string -- ) \ generate IVs
    $> ivs-strings receive-ivs ;

\ nat traversal functions

+net2o: punch ( $:string -- ) \ punch NAT traversal hole
    $> net2o:punch ;
+net2o: punch-load, ( $:string -- ) \ use for punch payload: nest it
    $> punch-load $! ;
+net2o: punch-done ( -- ) \ punch received
    o 0<> own-crypt? and IF
	return-addr return-address $10 move  resend0 $off
    THEN ;

: cookie, ( -- )  add-cookie lit, set-rtdelay ;
: request, ( -- )  next-request ulit, request-done ;

: gen-punch ( -- )
    my-ip$ [: $, punch ;] $[]map ;

: cookie+request ( -- )  nest[ cookie, request, ]nest ;

: gen-punchload ( -- )
    nest[ cookie, punch-done request, ]nest$ punch-load, ;

+net2o: punch? ( -- ) \ Request punch addresses
    gen-punch ;
+net2o: set-ip ( $:string -- ) \ set address information
    $> setip-xt perform ;
+net2o: get-ip ( -- ) \ request address information
    >sockaddr $, set-ip [: $, set-ip ;] n2oaddrs ;

\ create commands to send back

: all-ivs ( -- ) \ Seed and gen all IVS
    state# rng$ 2dup $, gen-ivs ivs-strings send-ivs ;

+net2o: >time-offset ( n -- ) \ set time offset
    o IF  time-offset 64!  ELSE  64drop  THEN ;
: time-offset! ( -- )  ticks 64dup lit, >time-offset time-offset 64! ;
: reply-key, ( -- )
    nest[ pkc keysize $, dest-pubkey @ IF
	dest-pubkey $@ $, keypair
	dest-pubkey $@ drop skc key-stage2
    ELSE  receive-key  THEN
    update-key all-ivs ;

+net2o: gen-reply ( -- ) \ generate a key request reply reply
    own-crypt? 0= ?EXIT
    [: crypt( ." Reply key: " tmpkey@ .nnb F cr )
      reply-key, cookie+request time-offset! ]tmpnest
      push-cmd ;]  IS expect-reply? ;

+net2o: gen-punch-reply ( -- )  o? \ generate a key request reply reply
    [: crypt( ." Reply key: " tmpkey@ .nnb F cr )
      reply-key, gen-punchload gen-punch time-offset! ]tmpnest
      push-cmd ;]  IS expect-reply? ;

\ everything that follows here can assume to have a connection context

gen-table $freeze
gen-table $@ inherit-table context-table

\ file functions

$40 net2o: file-id ( uid -- o:file )
    64>n state-addr n:>o ;
fs-table >table

reply-table $@ inherit-table fs-table

net2o' <req net2o: <req-file ( -- ) fs-id @ ulit, file-id ;
net2o' emit net2o: open-file ( $:string mode -- ) \ open file with mode
    64>n $> rot fs-open ;
+net2o: close-file ( -- ) \ close file
    fs-close ;
+net2o: set-size ( size -- ) \ set size attribute of current file
    track( ." file <" fs-id @ 0 .r ." > size: " 64dup 64. F cr ) size! ;
+net2o: set-seek ( useek -- ) \ set seek attribute of current file
    track( ." file <" fs-id @ 0 .r ." > seek: " 64dup 64. F cr ) seekto! ;
+net2o: set-limit ( ulimit -- ) \ set limit attribute of current file
    track( ." file <" fs-id @ 0 .r ." > seek to: " 64dup 64. F cr ) limit-min! ;
+net2o: set-stat ( umtime umod -- ) \ set time and mode of current file
    64>n n2o:set-stat ;
+net2o: get-size ( -- )
    fs-size 64@ lit, set-size ;
+net2o: get-stat ( -- ) \ request stat of current file
    n2o:get-stat >r lit, r> ulit, set-stat ;

gen-table $freeze
' context-table is gen-table

+net2o: set-blocksize ( n -- ) \ set blocksize
    64>n blocksize! ;
+net2o: set-blockalign ( n -- ) \ set block alignment
    64>n pow2?  blockalign ! ;
+net2o: close-all ( -- ) \ close all files
    n2o:close-all ;

: blocksize! ( n -- )  dup ulit, set-blocksize blocksize! ;
: blockalign! ( n -- )  pow2? dup ulit, set-blockalign blockalign ! ;

\ better slurping

:noname ( uid useek -- ) 64>r ulit, file-id
    64r> lit, set-seek endwith ; is do-track-seek

+net2o: set-top ( utop flag -- ) \ set top, flag is true when all data is sent
    >r 64>n r> data-rmap @ >o over dest-top @ <> and dest-end or! dest-top! o> ;
+net2o: slurp ( -- ) \ slurp in tracked files
    n2o:slurp swap ulit, flag, set-top
    ['] do-track-seek n2o:track-all-seeks net2o:send-chunks ;

\ flow control functions

$50 net2o: ack ( -- )  ack-context @ n:>o ;
ack-table >table

reply-table $@ inherit-table ack-table

net2o' <req net2o: <req-ack ( -- )  ack ;
net2o' req> net2o: ack-req> ( -- )
    cmdbuf# @ 1 = IF  cmdbuf# off  ELSE  endwith  THEN ;
net2o' emit net2o: ack-addrtime ( utime addr -- ) \ packet at addr received at time
    parent @ .net2o:ack-addrtime ;
+net2o: ack-resend ( flag -- ) \ set resend toggle flag
    64>n  parent @ .net2o:ack-resend ;
+net2o: set-rate ( urate udelta-t -- ) \ set rate 
    parent @ >o cookie? IF  net2o:set-rate
    ELSE  64drop 64drop ns/burst dup @ 2* 2* swap !  THEN o> ;
+net2o: resend-mask ( addr umask -- ) \ resend mask blocks starting at addr
    2*64>n parent @ >o net2o:resend-mask net2o:send-chunks o> ;
+net2o: track-timing ( -- ) \ track timing
    net2o:track-timing ;
+net2o: rec-timing ( $:string -- ) \ recorded timing
    $> net2o:rec-timing ;
+net2o: send-timing ( -- ) \ request recorded timing
    net2o:timing$ maxtiming umin tuck $, net2o:/timing rec-timing ;
+net2o: ack-b2btime ( utime addr -- ) \ burst-to-burst time at packet addr
    parent @ .net2o:ack-b2btime ;
+net2o: ack-cookies ( ucookie addr umask -- ) \ acknowledge cookie
    [IFUNDEF] 64bit 64>r 64>n 64r> [THEN]
    parent @ >o data-map @ cookie+ 64over 64over 64= 0= IF
	." cookies don't match! " 64over $64. 64dup $64. F cr
    THEN
    64= cookie-val and validated or! o> ;
+net2o: ack-flush ( addr -- ) \ flushed to addr
    64>n parent @ .net2o:rewind-sender-partial ;
+net2o: set-head ( addr -- ) \ set head
    64>n parent @ .data-rmap @ .dest-head umax! ;
+net2o: timeout ( uticks -- ) \ timeout request
    parent @ >o net2o:timeout  data-map @ .dest-tail @ ulit, set-head o> ;

\ profiling, nat traversal

gen-table $freeze
' context-table is gen-table

: net2o:gen-resend ( -- )
    recv-flag @ invert resend-toggle# and ulit, ack-resend ;
: net2o:ackflush ( n -- ) ulit, ack-flush ;
: n2o:done ( -- )  slurp next-request filereq# ! ;

: rewind ( -- )
    data-rmap @ >o dest-back @ do-slurp @ umax o> net2o:ackflush ;

\ safe initialization

net2o-base

: lit<   lit, push-lit ;
: slit<  slit, push-slit ;
:noname ( throwcode -- )
    connection @ .server? IF
	dup  IF  dup nlit, ko end-cmd
	    ['] end-cmd IS expect-reply? (end-code)  THEN
    THEN  throw ; IS >throw

set-current previous

also net2o-base

: open-tracked-file ( addr u mode --)
    open-file <req get-size get-stat req> ;

: n2o:copy ( addrsrc us addrdest ud -- )
    file-reg# @ ulit, file-id
    2swap $, r/o ulit, open-tracked-file  endwith
    file-reg# @ save-to
    1 file-reg# +! ;

: seek! ( pos id -- ) >r d>64
    64dup r@ state-addr .fs-seek 64!
    r> ulit, file-id lit, set-seek endwith ;

: limit! ( pos id -- ) >r d>64
    r@ ulit, file-id 64dup lit, set-limit endwith
    r> init-limit! ;

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

: ack-cookie, ( map bits n -- ) [ 8 cells ]L * maxdata *
    2dup 2>r rot >r swap u>64 r> cookie+
    lit, 2r> ulit, ulit, ack-cookies ;

: net2o:ack-cookies ( -- )  data-rmap @ { map }
    map .data-ackbits-buf $@
    bounds ?DO
	\ map I 2@ ack-cookie,
	I 2 cells + 64@ lit,
	I 2@ [ 8 cells ]L * maxdata * ulit, ulit, ack-cookies
    [ 2 cells 64'+ ]L +LOOP
    map .data-ackbits-buf $off ;

\ client side acknowledge

: net2o:genack ( -- )
    net2o:ack-cookies  net2o:b2btime  net2o:acktime  >rate ;

: !rdata-tail ( -- )
    data-rmap @ >o
    data-ack# @ bytes>addr dest-top 2@ umin umin
    dest-tail @ umax dup dest-tail !@ o>
    u> IF  net2o:save& 64#0 burst-ticks 64!  THEN ;
: receive-flag ( -- flag )  recv-flag @ resend-toggle# and 0<> ;

8 Value max-resend#

: prepare-resend ( flag -- end start acks ackm taibits )
    data-rmap @ >o
    IF    dest-head @ addr>bits bits>bytes -4 and
    ELSE  dest-head @ 1- addr>bits bits>bytes 1+  THEN 0 max
    dest-tail @ addr>bytes -4 and dup data-ack# umin!
    data-ackbits @ dest-size @ addr>bytes 1-
    dest-tail @ addr>bits o> ;

: net2o:do-resend ( flag -- )
    o 0= IF  drop EXIT  THEN  data-rmap @ 0= IF  drop EXIT  THEN
    0 swap  prepare-resend { acks ackm tailbits }
    +DO
	acks I ackm and + l@
	I bytes>bits tailbits u< IF
	    -1 tailbits I bytes>bits - lshift invert or
	THEN
	dup $FFFFFFFF <> IF
	    resend( ." resend: " dup hex. over hex. F cr )
	    I ackm and bytes>addr ulit, $FFFFFFFF xor ulit, resend-mask  1+
	ELSE
	    drop dup 0= IF  I 4 + data-rmap @ .data-ack# !  THEN
	THEN
	dup max-resend# >= ?LEAVE \ no more than x resends
    4 +LOOP  drop !rdata-tail ;

: do-expect-reply ( -- )
    reply-index ulit, ok?  end-cmd  net2o:expect-reply
    msg( ." Expect reply" F cr )
    ['] end-cmd IS expect-reply? ;

: expect-reply ( -- ) cmd( ." expect reply:" F cr )
    ['] do-expect-reply IS expect-reply?  maxdata code+ ;

: resend-all ( -- )
    ticker 64@ resend-all-to 64@ u>= IF
	false net2o:do-resend
	+timeouts resend-all-to 64!
    THEN ;

0 Value request-stats?

: update-rtdelay ( -- )
    ticks lit, push-lit push' set-rtdelay ;

: data-end? ( -- flag )
    0 data-rmap @ .dest-end !@ ;

: rewind-transfer ( -- flag )
    rewind data-end? IF  filereq# @ n2o:request-done  false
    ELSE  true  THEN ;

: request-stats   F true to request-stats?  ack track-timing endwith ;

: expected@ ( -- head top )
    o IF  data-rmap @ >o
	o IF  dest-tail @ dest-top @  ELSE  0.  THEN o>
    ELSE  0.  THEN  ;

: expected? ( -- flag )
    expected@ tuck u>= and IF
	expect-reply
	msg( ." check: " data-rmap @ >o dest-back @ hex. dest-tail @ hex. dest-head @ hex.
	data-ackbits @ data-ack# @ dup hex. + l@ hex.
	o> F cr ." Block transfer done: " expected@ hex. hex. F cr )
	net2o:ack-cookies  rewind-transfer
	64#0 burst-ticks 64!
    ELSE  false  THEN ;

cell 8 = [IF] 6 [ELSE] 5 [THEN] Constant cell>>

: +ackbit ( bit -- ) 0. c:cookie { d^ new-ackbit 64^ new-cookie }
    dup  [ 8 cells 1- ]L and swap cell>> rshift
    data-ackbits-buf $@ bounds ?DO
	dup I @ = IF drop
	    cookie( ." cookie+ " I @ cell>> chunk-p2 + lshift hex. dup hex. new-cookie 64@ $64. )
	    I cell+ swap +bit
	    new-cookie 64@ I 2 cells + 64+!
	    cookie( I 2 cells + 64@ $64. I cell+ @ hex. F cr )
	    unloop EXIT  THEN
    [ 2 cells 64'+ ]L +LOOP
    cookie( ." cookie= " dup cell>> chunk-p2 + lshift hex. over hex. new-cookie 64@ $64. F cr )
    new-ackbit !
    new-ackbit cell+ swap +bit
    new-ackbit [ 2 cells 64'+ ]L data-ackbits-buf $+! ;

: +cookie ( -- )
    data-rmap @ >o  ack-bit# @ >r
    data-ackbits @ r@ +bit@  dup 0= IF  r@ +ackbit  THEN  rdrop
    o> negate packetr2 +! ;

: +expected ( -- flag )
    data-rmap @ >o dest-head @ dest-top @ u>= ack-advance? @ and o>
    IF   resend-all  THEN  expected? ;

\ higher level functions

: map-request, ( ucode udata -- )
    2dup + n2o:new-map lit, swap ulit, ulit,
    map-request ;

: gen-request ( -- )
    cmd( ind-addr @ IF  ." in" THEN ." direct connect" F cr )
    net2o-code0
    ['] end-cmd IS expect-reply?
    gen-tmpkeys $, receive-tmpkey
    nest[ cookie, ind-addr @ IF  gen-punch-reply
    ELSE  gen-reply request,  THEN ]nest
    tmpkey-request
    dest-pubkey @ 0= IF  key-request  THEN
    ind-addr @  IF  punch?  THEN  other
    req-codesize @  req-datasize @  map-request,
    ['] push-cmd IS expect-reply?
    end-code ;

:noname ( addr u -- )
    cmd0buf cmd0source ! cmdreset also net2o-base
    [ also net2o-base ]
    ['] end-cmd IS expect-reply?
    $, nest end-code
; is punch-reply

: 0-resend? ( -- )
    resend0 @ IF
	\ ." Resend to 0" cr
	cmd0buf cmd0source !
	[: resend0 $@ >r cmdbuf r@ move
	  r0-address return-addr $10 move
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
    data-rmap @ .data-ack# @ hex.
    expected@ hex. hex. F cr ;

\ acknowledge toplevel

: net2o:ack-code ( ackflag -- ackflag' )
    false { slurp? }
    net2o-code  ack <req ['] end-cmd IS expect-reply?
    dup ack-receive !@ xor >r
    r@ ack-toggle# and IF
	net2o:gen-resend  net2o:genack
	r@ resend-toggle# and IF
	    true net2o:do-resend
	THEN
	0 data-rmap @ .do-slurp !@
	?dup-IF  net2o:ackflush request-stats? IF  send-timing  THEN
	    true to slurp?  THEN
    THEN  +expected slurp? or to slurp?
    req> endwith  cmdbuf# @ 4 = IF  cmdbuf# off  THEN
    slurp? IF  slurp  THEN
    end-code r> dup ack-toggle# and IF  map-resend?  THEN ;

: net2o:do-ack ( -- )
    dest-addr 64@ recv-addr 64! \ last received packet
    ( recv-cookie ) +cookie
    inbuf 1+ c@ dup recv-flag ! \ last receive flag
    acks# and data-rmap @ .ack-advance? @
    IF  net2o:ack-code   ELSE  ack-receive @ xor  THEN  ack-timing
    ack( ." ack expected: " recv-addr 64@ $64. expected@ hex. hex. F cr )
;

: +flow-control ['] net2o:do-ack ack-xt ! ;
: -flow-control ['] noop         ack-xt ! ;

\ keepalive

also net2o-base
: .keepalive ( -- )  ." transfer keepalive " expected@ hex. hex.
    data-rmap @ >o dest-tail @ hex. dest-back @ hex. o>
    F cr ;
: transfer-keepalive? ( -- )  o to connection
    timeout( .keepalive )
    rewind-transfer 0= IF  .keepalive  EXIT  THEN
    expected@ tuck u>= and IF  net2o-code
	ack <req +expected req> endwith IF  slurp  THEN  end-code  EXIT  THEN
    net2o-code  expect-reply
    update-rtdelay  ack <req net2o:genack
    resend-all ticks lit, timeout rewind req> endwith slurp  end-code ;
previous

: connected-timeout ( -- ) timeout( ." connected timeout" F cr )
    \ timeout( .expected )
    1 timeouts +! >next-timeout
    packets2 @ cmd-resend? packets2 @ = IF  transfer-keepalive?  THEN ;

\ : +connecting   ['] connecting-timeout timeout-xt ! ;
: +resend       ['] connected-timeout  timeout-xt ! o+timeout ;

: +get-time     ['] get-tick is other ;

: reqsize! ( ucode udata -- )  req-datasize !  req-codesize ! ;
: tail-connect ( -- )   +resend  client-loop
    -timeout tskc KEYBYTES erase resend0 $off ;

: n2o:connect ( ucode udata -- )
    reqsize!  gen-request  tail-connect ;

: end-code| ( -- )  ]] end-code client-loop [[ ; immediate compile-only

previous

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z\-0-9]+(" immediate (font-lock-comment-face . 1)
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