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

require set-compsem.fs

\ net2o commands are protobuf coded, not byte coded.

ustack string-stack
ustack object-stack
ustack t-stack
ustack nest-stack

\ command buffers

User buf-state cell uallot drop
User buf-dump  cell uallot drop

user-o cmdbuf-o

object class
    cell uvar cmdbuf#
    cell uvar cmd-reply-xt

    umethod cmdlock
    umethod cmdbuf$
    umethod cmdreset
    umethod maxstring
    umethod +cmdbuf
    umethod -cmdbuf
    umethod cmddest
end-class cmd-buf-c

: cmdbuf: ( addr -- )  Create , DOES> perform @ cmdbuf-o ! ;

: cmd-nest { xt -- }
    buf-dump 2@ 2>r buf-state 2@ 2>r cmdbuf-o @ >r
    connection dup dup >r >o IF
	validated @ >r  xt catch  r> validated !
    ELSE
	xt catch
    THEN  o> r> to connection 
    r> cmdbuf-o ! 2r> buf-state 2! 2r> buf-dump 2!
    throw ;

\ command helper

: p@ ( -- 64u ) buf-state 2@ over + >r p@+ r> over - buf-state 2! ;
: ps@ ( -- 64n ) p@ zz>n ;

: byte@ ( addr u -- addr' u' b )
    >r count r> 1- swap ;

\ use a string stack to make sure that strings can only originate from
\ a string inside the command we are just executing

: >$ ( addr u -- $:string )
    string-stack $[]# 1+ string-stack $[] cell- 2! ;
: $> ( $:string -- addr u )
    string-stack $[]# 2 -
    dup 0< !!string-empty!! dup >r
    string-stack $[] 2@
    r> cells string-stack $!len ;

: @>$ ( addr u -- $:string addr' u' )
    bounds p@+ 64n-swap 64>n bounds ( endbuf endstring startstring )
    >r 2dup u< IF  ~~ true !!stringfit!!  THEN
    dup r> over umin tuck - >$ tuck - ;

: string@ ( -- $:string )
    buf-state 2@ @>$ buf-state 2! ;

: @>$noerr ( addr u -- $:string addr' u' )
    bounds p@+ 64n-swap 64>n bounds ( endbuf endstring startstring )
    >r over umin dup r> over umin tuck - >$ tuck - ;

: string@noerr ( -- $:string )
    buf-state 2@ @>$noerr buf-state 2! ;

\ string debugging

: .black85 ( addr u -- )
    <black> reveal( 85type )else( nip 5 4 */ spaces ) <default> ;

$20 constant maxstr#

: $.maxstr ( addr u xt -- ) >r
    dup maxstr# 2* u> IF
	2dup maxstr# umin r@ execute
	." [..$" dup maxstr# 2* - 0 u.r ." ..]"
	dup maxstr# - /string r@ execute
    THEN
    r> execute ;

0 warnings !@ \ $. could be mistaken as double 0
in net2o : $. ( addr u -- )
    2dup printable? IF
	.\" \"" type \ $.maxstr
    ELSE
	.\" 85\" " 85type \ $.maxstr
    THEN  '"' emit ;
warnings !

: n2o.string ( $:string -- )  cr $> net2o:$. ."  $, " ;
: n2o.secstring ( $:string -- ) attr @ >r
    cr $> .\" 85\" " .black85 r> attr! .\" \" sec$, " ;

forward key>nick
: .?id ( addr -- ) keysize 2dup key>nick
    dup IF  type 2drop  ELSE  2drop $8 umin 85type  THEN ;
: .pk(2)sig? ( addr u -- )
    2dup pk2-sig? 0= IF
	space sigpk2size# - + .?id
	false .check ELSE
	2dup pk-sig? 0= IF
	    space sigpksize# - + .?id
	    false .check
	ELSE  2drop true .check  THEN  THEN ;
: n2o.sigstring ( $:string -- )
    cr $> 2dup net2o:$. ."  ( " 2dup ['] .sigdates #10 base-execute
    2drop \ .pk(2)sig?
    ."  ) $, " ;

: $.s ( $string1 .. $stringn -- )
    string-stack $@ bounds U+DO
	cr i 2@ net2o:$.
    2 cells +LOOP ;

\ object stack

: o-pop ( o:o1 o:x -- o1 o:x ) object-stack stack> ;
: o-push ( o1 o:x -- o:o1 o:x ) object-stack >stack ;

: n:>o ( o1 o:o2 -- o:o2 o:o1 )
    >o r> o-push  o IF  1 req? !  THEN ;
: n:o> ( o:o2 o:o1 -- o:o2 )
    o-pop >r o> ;
: n:oswap ( o:o1 o:o2 -- o:o2 o:o1 )
    o-pop >o r> o-push ;

\ token stack - only for decompiling

: t-push ( addr -- )  t-stack >stack ;
: t-pop ( -- addr )   t-stack stack> ;
: t# ( -- n ) t-stack $[]# ;

\ float are stored big endian.

: pf@+ ( addr u -- addr' u' r )
    2>r 64 64#0 2r> bounds ?DO
	7 64lshift I c@ $7F and n>64 64+ 64>r 7 - 64r>
	I c@ $80 and  0= IF
	    n64-swap 64lshift
	    0e { f^ pftmp } pftmp 64! pftmp f@
	    I 1+ I' over - unloop  EXIT  THEN
    LOOP   true !!floatfit!!  ;

: pf!+ ( r:float addr -- addr' ) { f^ pftmp }
    BEGIN
	pftmp 64@ 57 64rshift 64>n
	pftmp 64@ 7 64lshift 64dup pftmp 64!
	64-0<> WHILE  $80 or over c! 1+  REPEAT
    over c! 1+ ;

: pf@ ( -- r )
    buf-state 2@ pf@+ buf-state 2! ;

: net2o-crash true !!function!! ;

Defer gen-table
' cmd-table IS gen-table

: n>cmd ( n -- addr ) cells >r
    o IF  token-table  ELSE  setup-table  THEN
    $@ r@ u<= !!function!! r> + ;

: cmd@ ( -- u ) buf-state 2@ over + >r p@+ r> over - buf-state 2! 64>n ;

standard:field
-7 cells 0 +field net2o.name
drop

: >net2o-name ( addr -- addr' u )
    net2o.name body> name>string ;
: >net2o-sig ( addr -- addr' u )
    net2o.name 3 cells + $@ ;
: .net2o-num ( off -- )  cell/ '<' emit 0 .r '>' emit space ;

User see:table \ current token table for see only

: (net2o-see) ( addr index -- )  dup >r + @
    dup 0<> IF
	net2o.name
	dup 2 cells + @ ?dup-IF  @ see:table @ t-push see:table !  THEN
	body> .name
    ELSE  drop r@ .net2o-num  THEN  rdrop ;

: .net2o-name ( n -- )  cells >r
    see:table $@ r@ u<=
    IF  drop r> .net2o-num  EXIT  THEN  r> (net2o-see) ;
: .net2o-name' ( n -- )  cells >r
    see:table $@ r@ u<=
    IF  drop r> .net2o-num  EXIT  THEN  r@ + @
    dup 0<> IF
	net2o.name body> .name
    ELSE  drop r@ .net2o-num  THEN  rdrop ;

: net2o-see ( cmd -- ) hex[
    case
	0 of  ." end-code" cr #0. buf-state 2!  endof
	1 of  p@          u64. ." lit, "  endof
	2 of  p@ 64invert s64. ." lit, "  endof
	3 of  string@noerr buf-state 2@ drop p@+ drop 64>n 10 =
	    IF    n2o.sigstring  ELSE  n2o.string  THEN  endof
	4 of  pf@ f. ." float, " endof
	5 of  ." end-with " cr  t# IF  t-pop see:table !  THEN  endof
	6 of  ." oswap " cr see:table @ t-pop see:table ! t-push  endof
	11 of  string@noerr n2o.secstring  endof
	13 of  '"' emit p@ 64>n xemit p@ 64>n xemit p@ 64>n xemit .\" \" 4cc, "
	endof
	14 of  string@noerr  2drop  endof
	$10 of ." push' " p@ 64>n .net2o-name  endof
	.net2o-name
	0 endcase ]hex ;

User show-offset  show-offset on

Sema see-sema

: cmd-see ( addr u -- addr' u' )
    dup show-offset @ = IF  ." <<< "  THEN
    buf-state 2! p@ 64>n net2o-see buf-state 2@ ;

in net2o : (see) ( addr u -- )
    buf-state 2@ 2>r
    [: ." net2o-code"  dest-flags 1+ c@ stateless# and IF  '0' emit  THEN
      dup hex. t-stack $off
      [: BEGIN  cmd-see dup 0= UNTIL ;] catch
      ."  end-code" cr throw  2drop ;] see-sema c-section
    2r> buf-state 2! ;

: >see-table ( -- )
    o IF  token-table  ELSE  setup-table  THEN  @ see:table ! ;

in net2o : see ( addr u -- )
    >see-table net2o:(see) ;

: .dest-addr ( flag -- )
    1+ c@ stateless# and 0= IF dest-addr 64@ x64. THEN ;

in net2o : see-me ( -- )
    ." see-me: "  inbuf hdrflags .dest-addr  buf-dump 2@ net2o:see ;

: cmd-dispatch ( addr u -- addr' u' )
    buf-state 2!
    cmd@ trace( dup IF dup >see-table .net2o-name' THEN >r .s r> $.s cr ) n>cmd
    @ ?dup-IF  execute  ELSE
	trace( ." crashing" cr cr ) net2o-crash  THEN  buf-state 2@ ;

: >cmd ( xt u -- ) gen-table $[] ! ;

: un-cmd ( -- )  #0. buf-state 2!  0 >o rdrop ;

Defer >throw

: cmd-throw ( error -- )
    cmd( true )else( remote? @ 0= ) IF
	[: ." do-cmd-loop: " dup . .exe cr ;] $err
	dup DoError
	buf-state @ show-offset !  <err> cr net2o:see-me <default> show-offset on
    THEN
    un-cmd >throw ;
: do-cmd-loop ( addr u -- )  2dup buf-dump 2!
    cmd( <warn> dest-flags .dest-addr 2dup net2o:see <default> )
    sp@ >r
    [: BEGIN   cmd-dispatch dup 0<=  UNTIL ;] catch
    trace( ." cmd loop done" .s cr )
    ?dup-IF   cmd-throw  THEN
    r> sp! 2drop +cmd ;
: nest-cmd-loop ( addr u -- )
    buf-dump 2@ 2>r buf-state 2@ 2>r ['] do-cmd-loop catch
    2r> buf-state 2@ d0<> IF  buf-state 2!  ELSE  2drop  THEN
    2r> buf-dump 2! ?dup-IF  throw  THEN ;

cmd-buf-c ' new static-a with-allocater code-buf^ !
' code-buf^ cmdbuf: code-buf

code-buf

:noname ( -- )  cmdbuf# off  connection >o
	req? off  ['] send-cX code-reply is send-xt o> ; to cmdreset
:noname ( -- addr )   connection .code-sema ; to cmdlock
:noname ( -- addr u ) connection .code-dest cmdbuf# @ ; to cmdbuf$
:noname ( -- n )  maxdata cmdbuf# @ - ; to maxstring
:noname ( addr u -- ) dup maxstring u> IF
	cmdbuf$ ~~ net2o:see true !!cmdfit!!  THEN
    tuck cmdbuf$ + swap move cmdbuf# +! ; to +cmdbuf
:noname ( n -- )  cmdbuf# +! ; to -cmdbuf
:noname ( -- 64dest ) code-vdest 64dup 64-0= !!no-dest!! ; to cmddest

Sema cmd0lock

cmd-buf-c class
    maxdata uvar cmd0buf
end-class cmd-buf0

cmd-buf0 ' new static-a with-allocater code0-buf^ !
' code0-buf^ cmdbuf: code0-buf

\ command buffer in a string

Sema cmd$lock

cmd-buf-c class
    cell uvar cmd$
end-class cmd-buf$

cmd-buf$ ' new static-a with-allocater code-buf$^ !
' code-buf$^ cmdbuf: code-buf$

code-buf$

' cmd$lock to cmdlock
:noname  cmd$ $@ ; to cmdbuf$
:noname  cmd$ $off ; to cmdreset
' true to maxstring \ really maxuint = -1 = true
:noname ( addr u -- ) cmd$ $+! ; to +cmdbuf
:noname ( n -- )  cmd$ $@len + cmd$ $!len ; to -cmdbuf
:noname ( -- 64dest ) 64#0 ; to cmddest

: gen-cmd ( xt -- $addr )
    cmdbuf-o @ >r code-buf$ 0 cmd$ !@ >r cmdbuf# @ >r
    catch
    r> cmdbuf# !  r> cmd$ !@ r> cmdbuf-o !  swap throw ;
: gen-cmd$ ( xt -- addr u )
    gen-cmd  1 tmp$# +!  tmp$ $!buf  tmp$ $@ ;

code0-buf \ reset default

:noname ( -- addr u ) cmd0buf cmdbuf# @ ; to cmdbuf$
' cmd0lock to cmdlock
' rng64 to cmddest
:noname ( -- )  cmdbuf# off  o IF  req? off  THEN ; to cmdreset

:noname ( -- )
    cmd-buf0 new code0-buf^ !
    cmd-buf-c new code-buf^ !
    cmd-buf$ new code-buf$^ ! ; is alloc-code-bufs
:noname
    code0-buf^ @ .dispose
    code-buf^ @ .dispose
    code-buf$^ @ >o cmd$ $off dispose o> ; is free-code-bufs

\ stuff into code buffers

: do-<req ( -- )  o IF  req? @ 0> IF  req? on start-req  THEN  THEN ;
: cmdtmp$ ( 64n -- addr u )  cmdtmp p!+ cmdtmp tuck - ;
: cmd, ( 64n -- )  do-<req cmdtmp$ +cmdbuf ;

: net2o, @ n>64 cmd, ;

\ net2o doc production

Defer .n-name  ' noop is .n-name
[IFDEF] docgen
    false warnings !@
    : \g ( rest-of-line -- )
	source >in @ /string over 2 - c@ 'g' = >r
	>in @ 3 > r@ and 2 and spaces
	dup >in +!
	r> IF  type cr  ELSE  2drop  THEN ; immediate
    warnings !
[THEN]

\ net2o command definition

0 Value last-2o

: net2o-does  DOES> net2o, ;
: net2o: ( number "name" -- )
    .n-name
    ['] noop over >cmd \ allocate space in table
    Create  here to last-2o
    dup >r , here >r 0 , 0 , 0 , net2o-does noname :
    lastxt dup r> ! r> >cmd ;
: +net2o: ( "name" -- ) gen-table $[]# net2o: ;
: >table ( table -- )  last-2o 2 cells + ! ;
: cmdsig ( -- addr )  last-2o 3 cells + ;
: net2o' ( "name" -- ) ' >body @ ;

Forward net2o:words

: inherit-table ( addr u "name" -- )
    ' dup IS gen-table  execute $! ;

Vocabulary net2o-base

Forward do-req>

: do-nest ( addr u flag -- )
    dup >r  validated or!  ['] nest-cmd-loop catch
    r> invert validated and!  throw ;

: do-nestsig ( addr u -- )
    signed-val do-nest ;

: cmd:nestsig ( addr u -- )
    nest-sig dup 0= IF  drop do-nestsig  ELSE  !!sig!!  THEN ;

scope{ net2o-base

\ Command numbers preliminary and subject to change

Defer doc(gen  ' noop is doc(gen

: (>sig ( "comments"* ']' -- )
    s" (" cmdsig $!
    BEGIN  parse-name dup  WHILE  over c@ cmdsig c$+!
	s" )" str= UNTIL  ELSE  2drop  THEN ;

: ( ( "type"* "--" "type"* "rparen" -- ) ')' parse 2drop ;
compsem: cmdsig @ IF  ')' parse 2drop  EXIT  THEN
    doc(gen (>sig ;

0 net2o: dummy ( -- ) ;

[IFDEF] docgen
    :noname ( -- )
	>in @ >r ')' parse ."  ( " type ." )" cr r> >in ! ; is doc(gen
    :noname ( n "name" -- )
	." * " dup hex. >in @ >r parse-name type r> >in ! ; is .n-name
[THEN]

: ?version ( addr u -- )
    net2o-version 2over str< IF
	<err> ." Other side has more recent net2o version: " forth:type
	<warn> ." , ours: " net2o-version forth:type <default> forth:cr
    ELSE  2drop  THEN ;

\g # Commands #
\g 
\g Version @VERSION@.
\g 
\g net2o separates data and commands.  Data is passed through to higher
\g layers, commands are interpreted when they arrive.  For connection
\g requests, a special bit is set, and the address then isn't used as
\g address, but as IV for the opportunistic encoding.
\g 
\g The command interpreter is a stack machine with two data types: 64
\g bit integers and strings (floats are also suppored, but used
\g infrequently).  Encoding of commands, integers and string length
\g follows protobuf conceptually (but MSB first, not LSB first as with
\g protobuf, to simplify scanning), strings are just sequences of
\g bytes (interpretation can vary).  Command blocks contain a sequence
\g of commands; there are no conditionals and looping instructions.
\g 
\g Strings can contain encrypted nested commands, used during
\g communication setup.
\g 
\g ## List of Commands ##
\g 
\g Commands are context-sensitive in an OOP method hierarchy sense.
\g 
\g ### base commands ###
\g 

0 net2o: end-cmd ( -- ) \g end command buffer
    0 buf-state ! ;
+net2o: lit ( #u -- u ) \g literal
    p@ ;
+net2o: -lit ( #n -- n ) \g negative literal, inverted encoded
    p@ 64invert ;
+net2o: string ( #string -- $:string ) \g string literal
    string@ ;
+net2o: flit ( #dfloat -- r ) \g double float literal
    pf@ ;
+net2o: end-with ( o:object -- ) \g end scope
    do-req> n:o> ;
+net2o: oswap ( o:nest o:current -- o:current o:nest )
    do-req> n:oswap ;
+net2o: tru ( -- f:true ) \g true flag literal
    true ;
+net2o: fals ( -- f:false ) \g false flag literal
    false ;
+net2o: words ( ustart -- ) \g reflection
    64>n net2o:words ;
+net2o: nestsig ( $:cmd+sig -- ) \g check sig+nest
    $> cmd:nestsig ; \ balk on all wrong signatures
+net2o: secstring ( #string -- $:string ) \g secret string literal
    string@ ;
+net2o: nop ( -- ) nat( ." nop" forth:cr ) ; \g do nothing
+net2o: 4cc ( #3letter -- )
    \g At the beginning of a file, this can be used as FourCC code
    buf-state 2@ 3 /string dup 0< !!stringfit!! buf-state 2! ;
+net2o: padding ( #len -- )
    \g add padding to align fields
    string@ $> 2drop ;
+net2o: version ( $:version -- ) \g version check
    $> ?version ;
}scope

cmd-table $save

also net2o-base
: do-req> o IF  req? @ 0<  IF  end-with req? off  THEN  THEN ;
previous

gen-table $@ inherit-table reply-table

\ net2o assembler

: cmd0! ( -- )
    \G initialize a stateless command
    code0-buf  stateless# outflag ! ;
: cmd! ( -- )
    \G initialize a statefull command
    code-buf  outflag off ;

also net2o-base

UDefer expect-reply?
' end-cmd IS expect-reply?

: init-reply  ['] end-cmd IS expect-reply?  ['] drop cmd-reply-xt ! ;

previous

: net2o-code ( -- )
    \G start a statefull command
    cmd!  cmdlock lock
    cmdreset init-reply 1 code+ also net2o-base ;
compsem: ['] net2o-code compile, also net2o-base ;
: net2o-code0
    \G start a stateless command
    cmd0!  cmdlock lock
    cmdreset init-reply also net2o-base ;
compsem: ['] net2o-code0 compile, also net2o-base ;

: punch-out ( -- )
    check-addr1 0= ind-addr @ or IF  2drop  EXIT  THEN
    nat( ticks .ticks ."  punch-cmd: " 2dup .address cr )
    2>r net2o-sock outbuf dup packet-size 0 2r> sendto drop ;

: ?punch-cmds ( -- )
    o IF
	punch-addrs @ IF
	    [:
	      outbuf destination $10 erase \ only direct packets
	      punch-addrs $@ bounds ?DO
		  I @ ['] punch-out addr>sock
	      cell +LOOP  ;] punch-wrap
	THEN
    THEN ;

: send-cmd ( addr u dest -- size ) n64-swap { buf# }
    +send-cmd dest-addr 64@ 64>r set-dest
    cmd( <info> ." send: " outflag .dest-addr dup buf# net2o:see <default> cr )
    max-size^2 1+ 0 DO
	buf# min-size I lshift u<= IF
	    I outflag @ stateless# and IF  send-cX ?punch-cmds
	    ELSE
		send-reply >r over buf# r@ 2!
		r> action-of send-xt ?dup-IF  execute
		ELSE  2drop <err> ." send-xt is 0" cr <default>  THEN
	    THEN
	    min-size I lshift  UNLOOP
	    64r> dest-addr 64! EXIT  THEN
    LOOP  64r> dest-addr 64!  true !!commands!! ;

[IFUNDEF] ftime
    : ftime ( -- r ) ntime d>f 1e-9 f* ;
[THEN]

: cmd ( -- )  cmdbuf# @ 1 u<= ?EXIT \ don't send if cmdbuf is empty
    connection >o outflag @ >r cmdbuf$ cmddest
    avalanche( ." send cmd: " ftime 1000e fmod (.time) 64dup x64. 64>r dup hex. 64r> cr )
    msg( ." send cmd to: " 64dup x64. forth:cr ) send-cmd
    r> stateless# and 0= IF  code-update  ELSE  drop  THEN o> ;

also net2o-base

: cmd-send? ( -- )
    cmdbuf# @ 1 u> IF  expect-reply? cmd  THEN ;

previous

in net2o : ok? ( -- )  o?
    tag-addr >r cmdbuf$ r@ 2!
    tag( ." tag: " tag-addr dup hex. 2@ swap hex. hex. forth:cr )
    code-vdest r@ reply-dest 64!
    r> code-reply dup off  to reply-tag ;
in net2o : ok ( tag -- ) \ ." ok" forth:cr
\    timeout( ." ok: " dup hex. forth:cr )
    o 0= IF  drop EXIT  THEN
    request( ." request acked: " dup . cr )
    resend0 $off
    nat( ." ok from: " ret-addr .addr-path space dup .
    dup reply[] 2@ d0= IF ." acked"  THEN cr )
    #0. 2 pick reply[] dup >r 2!
    ticks r@ reply-time 64@ 64- ack@ >o
    rtd( ." rtdelay ok: " 64dup 64>f .ns cr )
    0 timeouts !@ rtd( dup . ) 1 u> IF  rtdelay 64@ 64umax
	rtd( ." rtdelay t-o: " 64dup 64>f .ns cr )  THEN
    rtdelay 64!  o>
    -1 reqcount +!@ 1 = IF
	wait-task @ ?dup-IF  wake# 's @ 1+ elit, :>wake  THEN
    THEN
    0 r> addr reply-xt !@ dup IF  execute  ELSE  2drop  THEN ; \ clear request
: net2o:expect-reply ( -- )
    o 0= IF  msg( ." fail expect reply" forth:cr )  EXIT  THEN
    timeout( cmd( ." expect: " cmdbuf$ net2o:see ) )
    msg( ." Expect reply" outflag @ stateless# and IF ."  stateless" THEN forth:cr )
    connection >o code-reply >r
    r@ reply-tag ?dup-IF  off  0 r@ to reply-tag  tHEN
    code-vdest     r@ reply-dest 64!
    ticks          r@ reply-time 64!
    cmd-reply-xt @ r> is reply-xt
    1 reqcount +!@ drop o> ;

: take-ret ( -- )
\    nat( ." take ret: " return-addr .addr-path space ."  -> " return-address .addr-path forth:cr )
    return-addr return-address $10 move ;

: tag-addr? ( -- flag )
    tag-addr dup >r 2@
    ?dup-IF
	cmd( dest-addr 64@ x64. ." resend canned code reply " r@ hex. forth:cr )
	resend( ." resend canned code reply " r@ hex. forth:cr )
	take-ret
	r> reply-dest 64@ send-cmd drop true
	1 packets2 +!
    ELSE  dest-addr 64@ [ cell 4 = ] [IF] 0<> - [THEN] dup 0 r> 2! u>=  THEN ;

: cmd-exec ( addr u -- )
    o to connection
    o IF
	maxdata code+  cmd!
	tag-addr? IF
	    2drop  ack@ .>flyburst  1 packetr2 +!  EXIT  THEN
	take-ret
    ELSE
	cmd0!
    THEN
    string-stack $free  object-stack $free  nest-stack $free
    [: outflag @ >r cmdreset init-reply do-cmd-loop
      r> outflag ! cmd-send? ;] cmdlock c-section ;

\ nested commands

User neststart#
User last-signed cell uallot drop
: +last-signed ( addr -- ) drop last-signed cell+ +! ;

2 Constant fwd# \ maximum 14 bits = 16kB

: nest$ ( -- addr u )  cmdbuf$ neststart# @ safe/string ;

: cmd-resolve> ( -- addr u )
    nest$ over >r dup n>64 cmdtmp$ dup fwd# u> !!stringfit!!
    r> over - swap move
    nest-stack stack> neststart# ! ;

also net2o-base

: +zero16 ( -- ) "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0" +cmdbuf ;
: sign[ ( -- ) neststart# @ nest-stack >stack
    string "\x80\x00" +cmdbuf cmdbuf$ nip neststart# ! ;
: nest[ ( -- ) sign[ +zero16 ; \ add space for IV
: ']sign ( xt -- )
    c:0key nest$
\    ." sign: " 2dup xtype forth:cr
    c:hash $tmp +cmdbuf
    cmd-resolve>  >r cmdbuf$ drop - r> last-signed 2!  nestsig ;
: ]sign ( -- ) ['] .sig ']sign ;
: ]pksign ( -- ) [: .pk .sig ;] ']sign ;

previous

: cmd> ( -- addr u )
    +zero16 \ add space for checksum
    cmd-resolve> ;

: cmd>nest ( -- addr u ) cmd> 2dup mykey-encrypt$ ;
: cmd>tmpnest ( -- addr u )
    cmd> 2dup tmpkey@ keysize umin
    key( ." tmpnest key: " 2dup 85type forth:cr ) encrypt$ ;
: cmd>encnest ( -- addr u )
    cmd> 2dup tmpkey@
    key( ." tmpnest key: " 2dup 85type forth:cr ) encrypt$ ;

: cmdnest ( addr u -- )  mykey-decrypt$
    IF  own-crypt-val do-nest  ELSE
	<err> ." cmdnest: no owncrypt, un-cmd" <default> forth:cr
	un-cmd  THEN ;

: cmdtmpnest ( addr u -- )
    $>align tmpkey@ key| dup IF
	key( ." tmpnest key: " 2dup 85type forth:cr ) decrypt$
	IF    tmp-crypt-val do-nest
	ELSE
	    <err> ." tmpnest failed, uncmd" <default> forth:cr
	    net2o:see-me 2drop un-cmd  THEN
    ELSE  2drop 2drop un-cmd  THEN ;
: cmdencnest ( addr u -- )
    $>align tmpkey@ dup IF
	key( ." encnest key: " 2dup 85type forth:cr ) decrypt$
	IF    enc-crypt-val do-nest  [ qr-tmp-val invert ]L validated and!
	ELSE <err> ." encnest failed, uncmd" <default> forth:cr
	    2drop un-cmd  THEN
    ELSE  <err> ." encnest: no tmpkey" <default> forth:cr
	2drop 2drop un-cmd  THEN ;

\ net2o assembler stuff

wordlist constant suffix-list
get-current suffix-list set-current
' vault-table alias v2o
' key-entry-table alias n2o
set-current

: 4cc>table ( addr u -- ) \ really is just 3 characters
    suffix-list find-name-in ?dup-IF  name>int execute @
    ELSE  see:table @  THEN ;
: suffix>table ( addr u -- )
    2dup '.' -scan nip /string 4cc>table ;

scope{ net2o-base

: maxtiming ( -- n )  maxstring timestats - dup timestats mod - ;
: string, ( addr u -- )  dup n>64 cmd, +cmdbuf ;
: $, ( addr u -- )  string
    dup maxstring u> IF  ~~ true !!stringfit!!  THEN
    \ extra test to give meaningful error messages
    string, ;
: sec$, ( addr u -- )  secstring string, ;
: lit, ( 64n -- )  dup 0< IF  -lit 64invert ELSE lit THEN cmd, ;
: nlit, ( n -- )  n>64 lit, ;
: ulit, ( u -- )  u>64 lit, ;
: 4cc, ( addr u -- ) 2dup *-width 3 <> !!4cc!! drop
    4cc xc@+ n>64 cmd, xc@+ n>64 cmd, xc@+ n>64 cmd, drop ;
: float, ( r -- )  flit cmdtmp pf!+ cmdtmp tuck - +cmdbuf ;
: flag, ( flag -- ) IF tru ELSE fals THEN ;
: (end-code) ( -- ) expect-reply? cmd  cmdlock unlock ;
: end-code ( -- ) (end-code) previous ;
compsem: ['] end-code compile, previous ;
: push-cmd ( -- )
    end-cmd cmdbuf$ push-reply ;

: ]nest$  ( -- )  cmd>nest 2drop ;
: ]nest$!  ( addr -- )
    neststart# @ >r cmd>nest rot $!
    r> fwd# - 1- cmdbuf$ nip - -cmdbuf ;
}scope

[IFDEF] 64bit
    ' noop Alias 2*64>n immediate
    ' noop Alias 3*64>n immediate
[ELSE]
    : 2*64>n ( 64a 64b -- na nb ) 64>n >r 64>n r> ;
    : 3*64>n ( 64a 64b 64c -- na nb nc ) 64>n >r 64>n >r 64>n r> r> ;
[THEN]

\ commands to reply

scope{ net2o-base
\g 
\g ### reply commands ###
\g 
$10 net2o: push' ( #cmd -- ) \g push command into answer packet
    p@ cmd, ;
+net2o: push-lit ( u -- ) \g push unsigned literal into answer packet
    lit, ;
' push-lit alias push-char
$13 net2o: push-$ ( $:string -- ) \g push string into answer packet
    $> $, ;
+net2o: push-float ( r -- ) \g push floating point number
    float, ;
+net2o: ok ( utag -- ) \g tagged response
    64>n net2o:ok ;
+net2o: ok? ( utag -- ) \g request tagged response
    lit, ok net2o:ok? ;
\ Use ko instead of throw for not acknowledge (kudos to Heinz Schnitter)
+net2o: ko ( uerror -- ) \g receive error message
    remote? off throw ;
+net2o: nest ( $:string -- ) \g nested (self-encrypted) command
    $> cmdnest ;
\ inspection
+net2o: token ( $:token n -- ) 64drop $> 2drop ; \g generic inspection token
+net2o: error-id ( $:errorid -- ) \g error-id string
    $> $error-id $! ;
+net2o: version? ( $:version -- ) \g version cross-check
    string-stack $[]# IF  $> ?version  THEN \ accept query-only
    net2o-version $, version ;

: ]nest  ( -- )  ]nest$ push-$ push' nest ;

}scope

reply-table $save

also net2o-base

: net2o:words ( start -- )
    token-table $@ 2 pick cells safe/string bounds U+DO
	I @ ?dup-IF
	    dup >net2o-sig 2>r >net2o-name
	    dup $A0 + maxstring u< IF
		2 pick ulit, 2r> 2swap [: type type ;] $tmp $, token
	    ELSE  2drop rdrop rdrop  THEN
	THEN  1+
    cell +LOOP  drop ;

previous

\\\
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:" "event:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     (("debug:" "field:" "2field:" "sffield:" "dffield:" "64field:" "uvar" "uvalue") non-immediate (font-lock-type-face . 2)
      "[ \t\n]" t name (font-lock-variable-name-face . 3))
     ("[a-z\-0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
     (("event:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]
