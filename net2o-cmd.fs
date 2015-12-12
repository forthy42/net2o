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

ustack string-stack
ustack object-stack
ustack t-stack
ustack nest-stack

\ command buffers

user-o cmdbuf-o

object class
    cell uvar cmdbuf#
    cell uvar cmd-reply-xt
    2 cells uvar buf-state
    2 cells uvar buf-dump

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
    connection >o validated @ >r
    xt execute
    r> validated ! o>
    r> cmdbuf-o ! 2r> buf-state 2! 2r> buf-dump 2! ;

\ command helper

: zz>n ( 64zz -- 64n )
    64dup 1 64rshift 64swap 64>n 1 and negate n>64 64xor ;
: n>zz ( 64n -- 64zz )
    64dup 64-0< n>64 64swap 64-2* 64xor ;
    
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

: >$ ( addr u -- $:string )
    string-stack $[]# 1+ string-stack $[] cell- 2! ;
: $> ( $:string -- addr u )
    string-stack $[]# 2 -
    dup 0< !!string-empty!! dup >r
    string-stack $[] 2@
    r> cells string-stack $!len ;

: @>$ ( addr u -- $:string addr' u' )
    bounds p@+ 64n-swap 64>n bounds ( endbuf endstring startstring )
    >r 2dup u< IF  true !!stringfit!!  THEN
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

: printable? ( addr u -- flag )
    true -rot bounds ?DO  I c@ $7F and bl < IF  drop false  LEAVE  THEN  LOOP ;

: n2o:$. ( addr u -- )
    2dup printable? IF
	.\" \"" type
    ELSE
	.\" 85\" " 85type
    THEN  '"' emit ;
: n2o.string ( $:string -- )  cr $> n2o:$. ."  $, " ;
: n2o.secstring ( $:string -- )
    cr $> .\" 85\" " .black85 .\" \" sec$, " ;

: $.s ( $string1 .. $stringn -- )
    string-stack $@ bounds U+DO
	cr i 2@ n2o:$.
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
    \ copy string to dictionary
    >r r@ $@  align here r> !
    dup , here swap dup allot move align ;

: n>cmd ( n -- addr ) cells >r
    o IF  token-table  ELSE  setup-table  THEN
    $@ r@ u<= !!function!! r> + ;

: cmd@ ( -- u ) buf-state 2@ over + >r p@+ r> over - buf-state 2! 64>n ;

-7 dup 1+ 1 and cell 4 = and - cells 0 +field net2o.name
drop

: >net2o-name ( addr -- addr' u )
    net2o.name body> name>string ;
: >net2o-sig ( addr -- addr' u )
    net2o.name 3 cells + $@ ;
: .net2o-num ( off -- )  cell/ '<' emit 0 .r '>' emit space ;

: (net2o-see) ( addr index -- )  dup >r + @
    dup 0<> IF
	net2o.name
	dup 2 cells + @ ?dup-IF  @ token-table @ t-push token-table !  THEN
	body> .name
    ELSE  drop r@ .net2o-num  THEN  rdrop ;

: .net2o-name ( n -- )  cells >r
    o IF  token-table  ELSE  setup-table  THEN $@ r@ u<=
    IF  drop r> .net2o-num  EXIT  THEN  r> (net2o-see) ;
: .net2o-name' ( n -- )  cells >r
    o IF  token-table  ELSE  setup-table  THEN $@ r@ u<=
    IF  drop r> .net2o-num  EXIT  THEN  r@ + @
    dup 0<> IF
	net2o.name body> .name
    ELSE  drop r@ .net2o-num  THEN  rdrop ;

: net2o-see ( cmd -- ) hex[
    case
	0 of  ." end-code" cr 0. buf-state 2!  endof
	1 of  p@ 64. ." lit, "  endof
	2 of  ps@ s64. ." slit, " endof
	3 of  string@noerr  n2o.string  endof
	4 of  pf@ f. ." float, " endof
	5 of  ." endwith " cr  t# IF  t-pop  token-table !  THEN  endof
	6 of  ." oswap " cr token-table @ t-pop token-table ! t-push  endof
	11 of  string@noerr  n2o.secstring  endof
	$10 of ." push' " p@ 64>n .net2o-name  endof
	.net2o-name
	0 endcase ]hex ;

Variable show-offset  show-offset on

sema see-sema

: cmd-see ( addr u -- addr' u' )
    dup show-offset @ = IF  ." <<< "  THEN
    buf-state 2! p@ 64>n net2o-see buf-state 2@ ;

: n2o:see ( addr u -- )
    [: ." net2o-code"  dest-flags 1+ c@ stateless# and IF  '0' emit  THEN
      dup hex. t-stack $off
      o IF  token-table @ >r  THEN
      [: BEGIN  cmd-see dup 0= UNTIL ;] catch
      o IF  r> token-table !  THEN  throw  2drop ;] see-sema c-section ;

: .dest-addr ( flag -- )
    1+ c@ stateless# and 0= IF dest-addr 64@ $64. THEN ;

: n2o:see-me ( -- )
    buf-state 2@ 2>r
    ." see-me: "
    inbuf hdrflags .dest-addr
    \ tag-addr dup hex. 2@ swap hex. hex. forth:cr
    buf-dump 2@ n2o:see
    2r> buf-state 2! ;

: cmd-dispatch ( addr u -- addr' u' )
    buf-state 2!
    cmd@ trace( dup IF dup .net2o-name' THEN >r .s r> $.s cr ) n>cmd
    @ ?dup-IF  execute  ELSE
	trace( ." crashing" cr cr ) net2o-crash  THEN  buf-state 2@ ;

: >cmd ( xt u -- ) gen-table $[] ! ;

: un-cmd ( -- )  0. buf-state 2!  0 >o rdrop ;

Defer >throw
Variable throwcount

: cmd-throw ( error -- )
    1 throwcount +!
    [: ." do-cmd-loop: " dup . .exe cr ;] $err
    dup DoError  nothrow
    buf-state @ show-offset !  n2o:see-me  show-offset on
    un-cmd  throwcount @ 4 < IF  >throw  THEN ;
: do-cmd-loop ( addr u -- )  2dup buf-dump 2!
    cmd( dest-flags .dest-addr 2dup n2o:see )
    sp@ >r throwcount off
    [: BEGIN   cmd-dispatch dup 0<=  UNTIL ;] catch
    trace( ." cmd loop done" .s cr )
    dup IF   cmd-throw  THEN
    r> sp! 2drop +cmd ;
: nest-cmd-loop ( addr u -- )
    buf-dump 2@ 2>r buf-state 2@ 2>r ['] do-cmd-loop catch
    2r> buf-state 2@ d0<> IF  buf-state 2!  ELSE  2drop  THEN
    2r> buf-dump 2! ?dup-IF  cmd-throw  THEN ;

cmd-buf-c new code-buf^ !
' code-buf^ cmdbuf: code-buf

code-buf

:noname ( -- )  cmdbuf# off  o IF  req? off  THEN ; to cmdreset
:noname ( -- addr )   connection .code-sema ; to cmdlock
:noname ( -- addr u ) connection .code-dest cmdbuf# @ ; to cmdbuf$
:noname ( -- n )  maxdata cmdbuf# @ - ; to maxstring
:noname ( addr u -- ) dup maxstring u> IF  ~~ true  !!stringfit!!  THEN
    tuck cmdbuf$ + swap move cmdbuf# +! ; to +cmdbuf
:noname ( n -- )  cmdbuf# +! ; to -cmdbuf
:noname ( -- 64dest ) code-vdest 64dup 64-0= !!no-dest!! ; to cmddest

sema cmd0lock

cmd-buf-c class
    maxdata uvar cmd0buf
end-class cmd-buf0

cmd-buf0 new code0-buf^ !
' code0-buf^ cmdbuf: code0-buf

\ command buffer in a string

Sema cmd$lock

cmd-buf-c class
    cell uvar cmd$
end-class cmd-buf$

cmd-buf$ new code-buf$^ !
' code-buf$^ cmdbuf: code-buf$

code-buf$

' cmd$lock to cmdlock
:noname  cmd$ $@ ; to cmdbuf$
:noname  cmd$ $off ; to cmdreset
' true to maxstring \ really maxuint = -1 = true
:noname ( addr u -- ) cmd$ $+! ; to +cmdbuf
:noname ( n -- )  cmd$ $@len + cmd$ $!len ; to -cmdbuf
:noname ( -- 64dest ) 64#0 ; to cmddest

: gen-cmd$ ( xt -- addr u )
    cmdbuf-o @ >r code-buf$ cmd$ @ >r cmd$ off
    execute  cmdbuf$ tmp$ $!  cmdreset  r> cmd$ ! r> cmdbuf-o !
    tmp$ $@ ;

code0-buf \ reset default

:noname ( -- addr u ) cmd0buf cmdbuf# @ ; to cmdbuf$
' cmd0lock to cmdlock
' rng64 to cmddest

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

Defer net2o:words

: inherit-table ( addr u "name" -- )
    ' dup IS gen-table  execute $! ;

Vocabulary net2o-base

Defer do-req>

: do-nestsig ( addr u -- )
    signed-val validated or! nest-cmd-loop
    signed-val invert validated and! ;

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
comp: drop cmdsig @ IF  ')' parse 2drop  EXIT  THEN
    doc(gen (>sig ;

0 net2o: dummy ( -- ) ;

[IFDEF] docgen
    :noname ( -- )
	>in @ >r ')' parse ."  ( " type ." )" cr r> >in ! ; is doc(gen
    :noname ( n "name" -- )
	." + " dup hex. >in @ >r parse-name type r> >in ! ; is .n-name
[THEN]

\g Commands
\g ========
\g 
\g net2o separates data and commands.  Data is pass through to higher
\g layers, commands are interpreted when they arrive.  For connection
\g requests, the address 0 is always mapped as connectionless code
\g address.
\g 
\g The command interpreter is a stack machine with two data types: 64
\g bit integers and strings.  Encoding of commands, integers and string
\g length follows protobuf, strings are just sequences of bytes
\g (interpretation can vary).  Command blocks contain a sequence of
\g commands; there are no conditionals and looping instructions.
\g 
\g Strings can contain encrypted nested commands, used during
\g communication setup.
\g 
\g List of Commands
\g ----------------
\g 
\g ### base commands ###
\g 

0 net2o: end-cmd ( -- ) \g end command buffer
    0 buf-state ! ;
+net2o: ulit ( #u -- u ) \g unsigned literal
    p@ ;
+net2o: slit ( #n -- n ) \g signed literal, zig-zag encoded
    ps@ ;
+net2o: string ( #string -- $:string ) \g string literal
    string@ ;
+net2o: flit ( #dfloat -- r ) \g double float literal
    pf@ ;
+net2o: endwith ( o:object -- ) \g end scope
    do-req> n:o> ;
:noname o IF  req? @ 0<  IF  endwith req? off  THEN  THEN ; is do-req>
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
}scope

gen-table $freeze
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
comp: :, also net2o-base ;
: net2o-code0
    \G start a stateless command
    cmd0!  cmdlock lock
    cmdreset init-reply also net2o-base ;
comp: :, also net2o-base ;

: send-cmd ( addr u dest -- size )  n64-swap { buf# }
    +send-cmd dest-addr 64@ 64>r set-dest
    cmd( ." send: " outflag .dest-addr dup buf# n2o:see cr )
    max-size^2 1+ 0 DO
	buf# min-size I lshift u<= IF
	    I send-cX  cmdreset  min-size I lshift  UNLOOP
	    64r> dest-addr 64! EXIT  THEN
    LOOP  64r> dest-addr 64!  true !!commands!! ;

: cmd ( -- )  cmdbuf# @ 2 u< ?EXIT \ don't send if cmdbuf is empty
    connection >o outflag @ >r cmdbuf$ cmddest
    msg( ." send cmd to: " 64dup $64. forth:cr ) send-cmd
    r> stateless# and 0= IF  code-update punch-load $off
    ELSE  drop  THEN o> ;

also net2o-base

: cmd-send? ( -- )
    cmdbuf# @ IF  expect-reply? cmd  THEN ;

previous

: net2o:ok? ( -- )  o?
    tag-addr >r cmdbuf$ r@ 2!
    tag( ." tag: " tag-addr dup hex. 2@ swap hex. hex. forth:cr )
    code-vdest r> reply-dest 64! ;
: net2o:ok ( tag -- )
    timeout( ." ok: " dup hex. forth:cr )
    o 0= IF  drop EXIT  THEN
    request( ." request acked: " dup . cr )
    resend0 $off
    nat( ." ok from: " ret-addr .addr-path space dup .
    dup reply[] 2@ d0= IF ." acked"  THEN cr )
    0. 2 pick reply[] dup >r 2!
    ticks r@ reply-time 64@ 64- ack@ .rtdelay 64!
    0 r> reply-xt !@ dup IF  execute  ELSE  2drop  THEN ; \ clear request
: net2o:expect-reply ( -- )
    o 0= IF  msg( ." fail expect reply" forth:cr )  EXIT  THEN
    timeout( cmd( ." expect: " cmdbuf$ n2o:see ) )
    msg( ." Expect reply" outflag @ stateless# and IF ."  stateless" THEN forth:cr )
    connection >o
    cmdbuf$ code-reply dup >r 2! code-vdest r@ reply-dest 64!
    ticks r@ reply-time 64!
    cmd-reply-xt @ r> reply-xt ! o> ;

: tag-addr? ( -- flag )
    tag-addr dup >r 2@
    ?dup-IF
	cmd( dest-addr 64@ $64. ." resend canned code reply " r@ hex. forth:cr )
	resend( ." resend canned code reply " r@ hex. forth:cr )
	r> reply-dest 64@ send-cmd drop true
	1 packets2 +!
    ELSE  dest-addr 64@ [ cell 4 = ] [IF] 0<> - [THEN] dup 0 r> 2! u>=  THEN ;

: cmd-loop ( addr u -- )
    string-stack $off
    object-stack $off
    nest-stack $off
    o to connection
    o IF
	maxdata code+
	code-buf
	tag-addr? IF
	    2drop  ack@ .>flyburst  1 packetr2 +!  EXIT  THEN
    ELSE
	cmd0!
    THEN
    [: outflag @ >r cmdreset  do-cmd-loop
      r> outflag ! cmd-send? ;] cmdlock c-section ;

' cmd-loop is queue-command

\ nested commands

User neststart#
User nest-string cell uallot drop
2 Constant fwd# \ maximum 14 bits = 16kB

: nest$ ( -- addr u )  cmdbuf$ neststart# @ safe/string ;

: cmd-resolve> ( -- addr u )
    nest$ over >r dup n>64 cmdtmp$ dup fwd# u> !!cmdfit!!
    r> over - swap move
    nest-stack stack> neststart# ! ;

also net2o-base

: sign[ ( -- ) neststart# @ nest-stack >stack
    string "\x80\x80" +cmdbuf cmdbuf$ nip neststart# ! ;
: nest[ ( -- ) sign[
    "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0" +cmdbuf ; \ add space for IV
: ']sign ( xt -- )
    c:0key nest$
\    ." sign: " 2dup xtype forth:cr
    c:hash $tmp +cmdbuf
    cmd-resolve> nest-string 2! nestsig ;
: ]sign ( -- ) ['] .sig ']sign ;
: ]pksign ( -- ) [: .pk .sig ;] ']sign ;

previous

: cmd> ( -- addr u )
    "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0" +cmdbuf \ add space for checksum
    cmd-resolve> ;

: cmd>nest ( -- addr u ) cmd> 2dup mykey-encrypt$ ;
: cmd>tmpnest ( -- addr u )
    cmd> 2dup tmpkey@ keysize umin
    key( ." tmpnest key: " 2dup 85type forth:cr ) encrypt$ ;

: do-nest ( addr u flag -- )
    validated @ >r  validated or!  
    nest-cmd-loop  r> validated ! ;

: cmdnest ( addr u -- )  mykey-decrypt$
    IF  own-crypt-val do-nest  ELSE  un-cmd  THEN ;

: cmdtmpnest ( addr u -- )
    $>align tmpkey@ drop keysize
    key( ." tmpnest key: " 2dup 85type forth:cr ) decrypt$
    IF  tmp-crypt-val do-nest  ELSE  trace( ." tmpnest failed!" forth:cr )  un-cmd  THEN ;

\ net2o assembler stuff

scope{ net2o-base

: maxtiming ( -- n )  maxstring timestats - dup timestats mod - ;
: string, ( addr u -- )  dup n>64 cmd, +cmdbuf ;
: $, ( addr u -- )  string string, ;
: sec$, ( addr u -- )  secstring string, ;
: lit, ( 64u -- )  ulit cmd, ;
: slit, ( 64n -- )  slit n>zz cmd, ;
: nlit, ( n -- )  n>64 slit, ;
: ulit, ( u -- )  u>64 lit, ;
: float, ( r -- )  flit cmdtmp pf!+ cmdtmp tuck - +cmdbuf ;
: flag, ( flag -- ) IF tru ELSE fals THEN ;
: (end-code) ( -- ) expect-reply? cmd  cmdlock unlock ;
: end-code ( -- ) (end-code) previous ;
comp: :, previous ;
: push-cmd ( -- )
    end-cmd ['] end-cmd IS expect-reply? cmdbuf$ push-reply ;

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
+net2o: push-slit ( n -- ) \g push singed literal into answer packet
    slit, ;
+net2o: push-$ ( $:string -- ) \g push string into answer packet
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
+net2o: request-done ( ureq -- ) 64>n \g signal request is completed
    o 0<> own-crypt? and IF  n2o:request-done  ELSE  drop  THEN ;
+net2o: set-cookie ( utimestamp -- ) \g cookies and round trip delays
    own-crypt? IF  trace( ." owncrypt " )
	64dup cookie>context?
	IF  trace( ." context " forth:cr ) >o rdrop  o to connection
	    ack@ >o ticker 64@ recv-tick 64! rtdelay! o> \ time stamp of arrival
	    EXIT
	ELSE \ just check if timeout didn't expire
	    trace( ." ticker " forth:cr )
	    64dup context-ticker 64!
	    ticker 64@ 64swap 64- connect-timeout# 64< ?EXIT
	THEN
    ELSE  64drop  THEN  un-cmd ;

\ inspection

+net2o: token ( $:token n -- ) 64drop $> 2drop ; \ stub

:noname ( start -- )
    token-table $@ 2 pick cells safe/string bounds U+DO
	I @ ?dup-IF
	    dup >net2o-sig 2>r >net2o-name
	    dup $A0 + maxstring u< IF
		2 pick ulit, 2r> 2swap [: type type ;] $tmp $, token
	    ELSE  2drop rdrop rdrop  THEN
	THEN  1+
    cell +LOOP  drop ; IS net2o:words

gen-table $freeze

: ]nest  ( -- )  ]nest$ push-$ push' nest ;

}scope

0 [IF]
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
