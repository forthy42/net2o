\ net2o tools

\ Copyright © 2015   Bernd Paysan

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

require err.fs
require unix/pthread.fs
require unix/mmap.fs
require struct-val.fs
require 64bit.fs
require date.fs
require mini-oof2.fs
require forward.fs
require set-compsem.fs
require hash-table.fs

\ events API change

[defined] estring, 0= [defined] e$, and [IF]
    synonym estring, e$,
[THEN]

\ enum

: enum ( n "name" -- n+1 )  dup Constant 1+ ;
: enums ( start n "name1" .. "namen" -- )
    0 ?DO enum LOOP drop ;
: bit ( n "name" -- n*2 )   dup Constant 2* ;
: bits: ( start n "name1" .. "namen" -- )
    0 ?DO bit LOOP drop ;

\ argument handling that works transparent from OS and Forth command line

user-o arg-o

object uclass arg-o
    umethod ?nextarg
    umethod ?@nextarg
    umethod ?peekarg
end-class cmd-args-c

align cmd-args-c , here constant cmd-args^

: cmd-args ( -- )  cmd-args^ arg-o ! ;
cmd-args

:noname ( -- addr u t / f )
    argc @ 1 > IF  next-arg true  ELSE  false  THEN ; is ?nextarg
:noname ( -- addr u t / f )
    argc @ 1 > IF  1 arg true  ELSE  false  THEN ; is ?peekarg
:noname ( -- addr u t / f )
    argc @ 1 > IF
	1 arg drop c@ '@' = IF  next-arg 1 /string true  EXIT  THEN
    THEN  false ; is ?@nextarg

cmd-args-c uclass arg-o
end-class word-args-c

align word-args-c , here constant word-args^

: word-args ( -- )  word-args^ arg-o ! ;

word-args

: parse-name" ( -- addr u )
    >in @ >r parse-name
    over c@ '"' = IF  2drop r@ >in ! '"' parse 2drop \"-parse  THEN  rdrop ;
: ?word-nextarg ( -- addr u t / f )
    parse-name" dup 0= IF  2drop  false  ELSE  true  THEN
; latestxt is ?nextarg
:noname ( -- addr u t / f )  >in @ >r
    parse-name" dup 0= IF  2drop  false  ELSE  true  THEN  r> >in !
; is ?peekarg
:noname ( -- addr u t / f )
    >in @ >r ?word-nextarg 0= IF  rdrop false  EXIT  THEN
    over xc@ '@' = IF  rdrop 1 /string true  EXIT  THEN
    r> >in ! 2drop false ; is ?@nextarg

: arg-loop { xt -- }
    begin  ?nextarg  while  xt execute  repeat ;
: arg-loop# ( n xt -- ) { xt -- }
    0 ?DO  ?nextarg 0= ?LEAVE  xt execute  LOOP ;
: @arg-loop { xt -- }
    begin  ?@nextarg  while  xt execute  repeat ;

\ string

: smove ( a-from u-from a-to u-to -- )
    rot 2dup u< IF
	drop move -9 throw
    ELSE
	nip move
    THEN ;

[IFUNDEF] safe/string
: safe/string ( c-addr u n -- c-addr' u' ) \ net2o
\G protect /string against overflows.
    dup negate >r  dup 0> IF
        /string dup r> u>= IF  + 0  THEN
    ELSE
        /string dup r> u< IF  + 1+ -1  THEN
    THEN ;
[THEN]

[IFUNDEF] string-suffix?
    : string-suffix? ( addr1 u1 addr2 u2 -- flag ) \ net2o
	\G return true if addr2 u2 is a suffix of addr1 u1
	tuck 2>r over swap - 0 max /string 2r> str= ;
[THEN]

: -skip ( addr u char -- ) >r
    BEGIN  1- dup  0>= WHILE  2dup + c@ r@ <>  UNTIL  THEN  1+ rdrop ;
[IFUNDEF] -scan
    : -scan ( addr u char -- addr u' ) >r
	BEGIN  dup  WHILE  1- 2dup + c@ r@ =  UNTIL  1+  THEN  rdrop ;
[THEN]

[IFUNDEF] basename
: basename ( addr u -- addr' u' )
    2dup '/' -scan nip /string ;
[THEN]

: str0? ( addr u -- flag )
    \ check if string is all zero
    0 scan nip 0= ;

: string-postfix? ( addr1 u1 addr2 u2 -- )
    tuck 2>r - + 2r> tuck str= ;

\ set debugging

debug: dummy(

: +-?( ( addr u -- flag )
    2dup +-? IF [: 1 /string type '(' emit ;] $tmp find-name dup
	IF  >does-code ['] dummy( >does-code =  THEN
    ELSE  2drop false  THEN ;

[IFUNDEF] set-debug
    : set-debug ( addr u -- )
	debug-eval $!
	s" db " debug-eval 1 $ins
	s" (" debug-eval $+!
	debug-eval $@ evaluate ;
[THEN]

: ++debug ( -- )
    BEGIN  ?peekarg  WHILE  +-?(  WHILE  ?nextarg drop set-debug  REPEAT  THEN ;

\ logic memory modifiers

[IFUNDEF] or!   : or!   ( x addr -- )    dup >r @ or   r> ! ; [THEN]
[IFUNDEF] xor!  : xor!  ( x addr -- )    dup >r @ xor  r> ! ; [THEN]
[IFUNDEF] and!  : and!  ( x addr -- )    dup >r @ and  r> ! ; [THEN]

[IFUNDEF] cxor! : cxor! ( x c-addr -- )  dup >r c@ xor r> c! ; [THEN]
[IFUNDEF] cand! : cand! ( x c-addr -- )  dup >r c@ and r> c! ; [THEN]
[IFUNDEF] cor!  : cor!  ( x c-addr -- )  dup >r c@ or  r> c! ; [THEN]

: max!@ ( n addr -- )    dup >r @ max  r> !@ ;
: umax!@ ( n addr -- )   dup >r @ umax r> !@ ;

\ user stack, automatic init+dispose

: ustack ( "name" -- ) \ net2o
    \G generate user stack, including initialization and free on thread
    \G start and termination
    User  latestxt >r
    :noname  action-of thread-init compile,
    r@ compile, postpone off postpone ;
    is thread-init
    :noname  r> compile, postpone $free  action-of kill-task  compile,
    postpone ;
    is kill-task ;

[IFUNDEF] NOPE
    : NOPE ( c:sys -- )
	\G removes a control structure sys from the stack
	drop 2drop ; immediate restrict
[THEN]
: replace-loop ( end start -- )
    ]] unloop U+DO NOPE [[ ; immediate restrict

require bits.fs

\ variable length integers, similar to protobuf, but MSB first

: p@+ ( addr -- u64 addr' )  >r 64#0 r@ 10 bounds
    DO  7 64lshift I c@ $7F and n>64 64or
	I c@ $80 and 0= IF  I 1+ UNLOOP rdrop  EXIT  THEN
    LOOP  r> 10 + ;
[IFDEF] 64bit
    : p-size ( u64 -- n ) \ to speed up: binary tree comparison
	\ flag IF  1  ELSE  2  THEN  equals  flag 2 +
	dup    $FFFFFFFFFFFFFF u<= IF
	    dup       $FFFFFFF u<= IF
		dup      $3FFF u<= IF
		    $00000007F u<= 2 +  EXIT  THEN
		$00000001FFFFF u<= 4 +  EXIT  THEN
	    dup   $3FFFFFFFFFF u<= IF
		$00007FFFFFFFF u<= 6 +  EXIT  THEN
	    $00001FFFFFFFFFFFF u<= 8 +  EXIT  THEN
	$000007FFFFFFFFFFFFFFF u<= 10 + ;
    : p!+ ( u64 addr -- addr' )  over p-size + dup >r >r
	dup $7F and r> 1- dup >r c!  7 rshift
	BEGIN  dup  WHILE  dup $7F and $80 or r> 1- dup >r c! 7 rshift  REPEAT
	drop rdrop r> ;
[ELSE]
    : p-size ( x64 -- n ) \ to speed up: binary tree comparison
	\ flag IF  1  ELSE  2  THEN  equals  flag 2 +
	2dup   $FFFFFFFFFFFFFF. du<= IF
	    2dup      $FFFFFFF. du<= IF
		2dup     $3FFF. du<= IF
		    $00000007F. du<= 2 +  EXIT  THEN
		$00000001FFFFF. du<= 4 +  EXIT  THEN
	    2dup  $3FFFFFFFFFF. du<= IF
		$00007FFFFFFFF. du<= 6 +  EXIT  THEN
	    $00001FFFFFFFFFFFF. du<= 8 +  EXIT  THEN
	$000007FFFFFFFFFFFFFFF. du<= 10 + ;
    : p!+ ( u64 addr -- addr' )  >r 2dup p-size r> + dup >r >r
	over $7F and r> 1- dup >r c!  7 64rshift
	BEGIN  2dup or  WHILE  over $7F and $80 or r> 1- dup >r c! 7 64rshift  REPEAT
	2drop rdrop r> ;
[THEN]

: zz>n ( 64zz -- 64n )
    64dup 1 64rshift 64swap 64>n 1 and negate n>64 64xor ;
: n>zz ( 64n -- 64zz )
    64dup 64-0< n>64 64swap 64-2* 64xor ;

: ps!+ ( 64n addr -- addr' )
    >r n>zz r> p!+ ;
: ps@+ ( addr -- 64n addr' )
    p@+ >r zz>n r> ;

\ compact representation of mostly power-of-two numbers

: p2@+ ( addr -- 64bit addr' )
    count dup >r $C0 u>= IF
	64#1 r> $3F and 64lshift n64-swap  EXIT  THEN
    r@ $0F and u>64 r> 4 rshift 8 umin 0 ?DO
	8 64lshift 64>r count u>64 64r> 64+
    LOOP  n64-swap ;
: p2$+! ( 64bit addr -- )
    >r
    64dup $F u>64 64u> IF
	64dup 64dup 64#1 64- 64and 64-0= IF
	    64>f fdup f* { | w^ ff1 }
	    ff1 sf! ff1 [ 3 pad ! pad c@ ]L + c@ $3F - $C0 or
	    r> c$+!  EXIT  THEN
	THEN
    0 >r <#
    BEGIN   64dup $F u>64 64u>  WHILE
	    64dup 64>n $FF and hold 8 64rshift
	    r> $10 + >r
    REPEAT
    64>n r> or hold #0. #> r> $+! ;

\ bit reversing

: bitreverse8 ( u1 -- u2 )
    0 8 0 DO  2* over 1 and + swap 2/ swap  LOOP  nip ;

Create reverse-table $100 0 [DO] [I] bitreverse8 c, [LOOP]

: reverse8 ( c1 -- c2 ) reverse-table + c@ ;
: reverse ( x1 -- x2 )
    0 cell 0 DO  8 lshift over $FF and reverse8 or
       swap 8 rshift swap  LOOP  nip ;
: reverse$16 ( addrsrc addrdst -- ) { dst } dup >r
    count reverse8 r@ $F + c@ reverse8 dst     c! dst $F + c!
    count reverse8 r@ $E + c@ reverse8 dst 1+  c! dst $E + c!
    count reverse8 r@ $D + c@ reverse8 dst 2 + c! dst $D + c!
    count reverse8 r@ $C + c@ reverse8 dst 3 + c! dst $C + c!
    count reverse8 r@ $B + c@ reverse8 dst 4 + c! dst $B + c!
    count reverse8 r@ $A + c@ reverse8 dst 5 + c! dst $A + c!
    count reverse8 r@ $9 + c@ reverse8 dst 6 + c! dst $9 + c!
    c@    reverse8 r> $8 + c@ reverse8 dst 7 + c! dst $8 + c! ;

\ scoping

require scope.fs

: with ( "vocabulary" -- )
    also ' execute postpone >o ; immediate restrict
: endwith ( -- )
    postpone o> previous ; immediate restrict

Vocabulary net2o

\ file name and URL sanitizer

: rework-% ( addr u -- addr' u' )
    [: [: bounds ?DO
	    I c@ '%' = delta-I 3 u>= and IF
		#0. I 1+ I' over - 2 umin >number nip nip 0= IF
		    dup bl 1+ #del within IF
			drop I c@ emit 1
		    ELSE
			emit 3
		    THEN
		ELSE  drop I c@ emit 1  THEN
	    ELSE
		I c@ emit 1
	    THEN
	+LOOP ;] $tmp ;] $10 base-execute ;
: encode-% ( addr u -- addr' u' )
    [: [: bounds ?DO
		I c@ dup bl 1+ #del within IF
		    emit
		ELSE
		    '%' emit 0 <# # # #> type
		THEN
	    LOOP ;] $tmp ;] $10 base-execute ;

: printable? ( addr u -- flag )
    true -rot bounds ?DO  I c@ $80 u>= IF
	    I ['] u8@+ catch IF  nothrow drop 0 true
	    ELSE  drop dup I - swap I' u>  THEN
	ELSE  1 I c@ $7F bl within  THEN
	IF  2drop false  LEAVE  THEN  +LOOP ;

: ?sane-file ( addr u -- addr u ) \ net2o
    \G check if file name is sane, and if not, fail
    dup 1- $FFF u>= !!filename!!             \ check nullstring+maxpath
    2dup printable? 0= !!filename!!          \ must be printable
    [IFDEF] cygwin                           \ rules for Windows
	2dup '\' scan nip 0<>   !!filename!! \ no backslash allowed
	2dup ':' scan nip 0<>   !!filename!! \ no colon allowed
    [THEN]
    s" /../"     search         !!filename!! \ no embedded .. allowed
    s" /./"      search         !!filename!! \ no embedded . allowed
    s" //"       search         !!filename!! \ no double slash allowed
    2dup s" ../" string-prefix? !!filename!! \ no parent directory allowed
    2dup s" ./"  string-prefix? !!filename!! \ no same directory allowed
    over c@ '/' =               !!filename!! \ no absolute filename allowed
    2dup '/' scan nip IF
	over c@ '~' =           !!filename!! \ no tilde allowed if it's a path
    THEN ;

$20 buffer: filechars
filechars $20 $FF fill
0 filechars l! \ ctrl chars are all illegal
filechars '/' -bit
filechars #del -bit
: no-fat-chars ( addr u -- ) "\\:?*\q<>|%" ;
no-fat-chars bounds [?DO] filechars [I] c@ -bit [LOOP]

\ '%' is allowed, but we use '%' to replace the others

[IFUNDEF] .##
    : .## ( u -- ) 0 <# # # #> type ;
[THEN]
: sane-type ( addr u -- )
    [: bounds ?DO
	  I c@ filechars over bit@
	  IF  emit  ELSE  '%' emit .##  THEN
      LOOP ;] $10 base-execute ;

: fn-sanitize ( addr u -- addr' u' )
    ['] sane-type $tmp ;

false Value chat-sanitize?
false Value hash-sanitize?

: chat-sanitize ( addr u -- addr' u' )
    chat-sanitize? IF  fn-sanitize  THEN ;
: hash-sanitize ( addr u -- addr' u' )
    hash-sanitize? IF  fn-sanitize  THEN ;

\ utf8 sanitizer

: utf8-sanitize ( addr u -- )
    bounds ?DO
	I ['] xc@+ catch IF
	    [ xc-vector @ fixed-width = ] [IF] '?' [ELSE] '�' [THEN] xemit
	    drop  I delta-I
	    ['] x-size catch IF  2drop  1  THEN
	ELSE
	    dup #tab = IF  drop ."         "  ELSE  xemit  THEN
	    I -  THEN
    +LOOP  nothrow ;

\ config stuff

require config.fs

\ net2o specific configurations

[IFUNDEF] no-file#
    2 Constant ENOENT
    #-512 ENOENT - Constant no-file#
[THEN]
[IFUNDEF] file-exist#
    17 Constant EEXIST
    #-512 EEXIST - Constant file-exist#
[THEN]

[IFUNDEF] init-dir
    : init-dir ( addr u mode -- flag ) \ net2o
	\G create a directory with access mode,
	\G return true if the dictionary is new, false if it already existed
	>r 2dup file-status nip no-file# = IF
	    r> mkdir-parents drop  true
	ELSE  2drop rdrop  false  THEN ;
[THEN]

\ dirstack

$10 stack: dirstack

: >dir ( -- )
    s" " $make { w^ dir }
    $4000 dir $!len dir $@ get-dir dir $!len drop
    dir @ dirstack >stack ;
: dir> ( -- )
    dirstack $@len 0= ?EXIT
    dirstack stack> { w^ dir }
    dir $@ set-dir throw  dir $free ;
: dir@ ( -- addr u )
    dirstack $[]# 1- dirstack $[]@ ;

scope: log
1 5 bits: num date end len perm
}scope

scope{ config

Variable hops#
2Variable beacon-ticks&
2Variable beacon-short-ticks&
2Variable dht-cleaninterval&
2Variable ekey-timeout&
Variable timeouts#
Variable passmode#
Variable logsize#
Variable logmask-tui#
Variable logmask-gui#
2Variable savedelta&
2Variable patchlimit&
Variable rootdirs$
Variable prio#
Variable host$		\g set host to this name
Variable port#          \g if not zero, use this port
Variable orighost$	\g if the host is orighost$
Variable date#
$Variable objects$
$Variable chats$
$Variable keys$
$Variable .net2o$
$Variable .net2o-config$
$Variable .net2o-cache$
$Variable invite$
$Variable chat-format$

}scope

$Variable config-file$  "~/.config/net2o/config" config-file$ $!

also config
log:date logmask-tui# !

: .net2o/ ( addr u -- addr' u' ) [: .net2o$ $. '/' emit type ;] $tmp ;
: subdir-config ( -- )
    "keys"    .net2o/ keys$ $!
    "chats"   .net2o/ chats$ $!
    "objects" .net2o/ objects$ $! ;
: $set ( xt addr -- ) dup $free $exec ;
: xdg-config ( env u addr -- ) >r
    getenv 2dup d0= IF  rdrop 2drop  EXIT  THEN
    [: type ." /net2o" ;] r> $set ;
: xdg-dir-config ( -- )
    "XDG_DATA_HOME"   .net2o$        xdg-config
    "XDG_CONFIG_HOME" .net2o-config$ xdg-config
    "XDG_CACHE_HOME"  .net2o-cache$  xdg-config ;
: default-dir-config ( -- )
    "SNAP_USER_COMMON" getenv 2dup d0= IF  2drop
	"~/.local/share/net2o" .net2o$ $!
	"~/.config/net2o"      .net2o-config$ $!
	"~/.cache/net2o"       .net2o-cache$ $!
    ELSE
	.net2o$ $!
	.net2o$ $@ .net2o-config$ $!
	[: .net2o$ $. ." /cache" ;] .net2o-cache$ dup $free $exec
	[: .net2o-config$ $. ." /config" ;] config-file$ dup $free $exec
    THEN
    xdg-dir-config
    subdir-config ;
default-dir-config

#2 date# !
#20 logsize# !
pad $400 get-dir rootdirs$ $!
"Hello!" invite$ $!
[defined] android 1 and passmode# ! \ default is all entry is masked out
#14 timeouts# !
#5 hops# ! \ redistribute across 5 hops

$1000.0000. patchlimit& 2! \ 256MB patch limit size
#10.000.000.000. savedelta& 2! \ 10 seconds deltat
#3600.000.000.000. ekey-timeout& 2! \ one hour ekey timeout
#60.000.000.000. dht-cleaninterval& 2! \ one minute dht clean interval
#50.000.000.000. beacon-ticks& 2!
#2.000.000.000. beacon-short-ticks& 2!

\ "*/-_`" chat-format$ $!
"" chat-format$ $! \ by default don't format

: ]path ( addr u -- )
    2dup 2>r
    ['] file>fpath catch dup
    IF
	lit, ]] throw [[ 2drop
	true
	2r> [{: d: file :}l ." file '" file type ." ' not found in ]path" ;]
	?warning
    ELSE  2rdrop drop ]] SLiteral [[ THEN  ] ;

: .net2o-config/ ( addr u -- addr' u' ) [: .net2o-config$ $. '/' emit type ;] $tmp ;
: .net2o-cache/ ( addr u -- addr' u' ) [: .net2o-cache$ $. '/' emit type ;] $tmp ;
: ~net2o-cache/ ( addr u -- )
    .net2o-cache/ 2dup $1FF init-dir drop set-dir throw ;
: ~net2o-cache/.. ( addr u -- )
    .net2o-cache/ 2dup $1FF init-dir drop dirname set-dir throw ;
Variable keys-file$
: .keys/  ( addr u -- addr' u' ) [: keys$   $. '/' emit type ;] keys-file$ $set keys-file$ $@ ;
: objects/.no-fat-file ( -- addr u )
    [: objects$  $. ." /." no-fat-chars type ;] $tmp ;
: chats/.no-fat-file ( -- addr u )
    [: chats$  $. ." /." no-fat-chars type ;] $tmp ;

: ?.net2o ( -- )
    .net2o$ $@ $1FF init-dir drop
    .net2o-config$ $@ $1FF init-dir drop
    .net2o-cache$ $@ $1FF init-dir drop ;
: ?.net2o/keys ( -- flag ) ?.net2o keys$ $@ $1C0 init-dir ;
: ?.net2o/chats ( -- ) ?.net2o chats$ $@ $1FF init-dir drop ;
: ?.net2o/objects ( -- ) ?.net2o objects$ $@ $1FF init-dir drop ;

: ?create-file ( addr u -- flag )
    2dup file-status IF  drop
	r/w create-file  IF  drop false  ELSE  close-file throw  true  THEN
    ELSE  drop 2drop true  THEN ;

: fsane-init ( -- )
    false to hash-sanitize?  false to chat-sanitize?
    ?.net2o/objects objects/.no-fat-file ?create-file
    0= to hash-sanitize?
    ?.net2o/chats   chats/.no-fat-file   ?create-file
    0= to chat-sanitize? ;

Variable configured?

:noname defers 'cold
    configured? off
    pad $400 get-dir rootdirs$ $!
; is 'cold
:noname ( -- )
    config:host$ $free
    config:rootdirs$ $free
    defers 'image ; is 'image

: rootdirs>path ( -- )
    config:rootdirs$ $@ bounds ?DO  I c@ ':' = IF 0 I c! THEN LOOP ;

forward default-host

: !wrapper ( val addr xt -- .. ) { a xt -- .. }
    a !@ >r xt catch r> a ! throw ;

: wrap-config ( addr u wid -- )
    config-throw >r  0 to config-throw
    ['] read-config catch  r> to config-throw  throw
    default-host ;

: ?old-config ( addr u wid -- ) \ net2o
    \G check if we have an old config; then keep it.
    "~/.net2o/config" file-status nip no-file# <> IF
	"~/.net2o" 2dup .net2o$ $! .net2o-config$ $!
	subdir-config
	nip nip "~/.net2o/config" rot
	wrap-config
    ELSE
	?.net2o default-host ['] write-config catch drop
    THEN ;

: ?.net2o-config ( -- )  true configured? !@ ?EXIT
    "NET2O_CONF"  getenv ?dup-IF  config-file$ $!  ELSE  drop  THEN
    config-file$ $@ 2dup file-status nip ['] config >wordlist swap
    no-file# = IF  ?old-config  ELSE  wrap-config  THEN  rootdirs>path ;

: init-dirs ( -- ) ?.net2o-config fsane-init ;

previous

\ print time

[IFUNDEF] ftime
    : ftime ( -- r ) ntime d>f 1e-9 f* ;
[THEN]

64Variable tick-adjust
: ticks ( -- u64 )  ntime d>64 tick-adjust 64@ 64+ ;

: ticks-u ( -- u )  ticks 64>n ;

1970 1 1 ymd2day Constant unix-day0

: fsplit ( r -- r n )  fdup floor fdup f>s f- ;
: fsplitd ( r -- r d )  fdup floor fdup f>d f- ;

: date? ( -- n )  config:date# @ ;
: datehms? ( -- n )  config:date# @ 7 and ;
$8 Constant #today
$10 Constant #splitdate
$20 Constant #splithour
$40 Constant #splitminute
$80 Constant #localtime
$100 Constant #timezone
$Variable $tz
0 Value tz-off
0 Value tz-flag

: >fticks ( time -- rftime ) 64>f 1n f* ;
: fticks ( -- rftime ) ticks >fticks ;
: fticks>dtms-local ( rftime -- fns s m h dm y )
    fsplitd >time&date&tz $tz $! to tz-off to tz-flag ;
: fticks>dtms-zulu ( rftime -- fns s m h dm y )
    fsplitd #86400 um/mod >r
    #60 /mod #60 /mod #24 mod r> unix-day0 + day2ymd swap rot ;
: fticks>dtms ( rftime -- fns s m h dm y )
    date? #localtime and IF  fticks>dtms-local  ELSE  fticks>dtms-zulu  THEN ;
: fticks>day ( rftime -- fraction day )
    fticks>dtms swap rot ymd2day unix-day0 - >r
    #60 * + #60 * + s>f f+ #86400 fm/ r> ;

-1 Value last-day
-1 Value last-hour
-1 Value last-minute

: reset-time ( -- )
    -1 to last-day  -1 to last-hour  -1 to last-minute ;
: today? ( day -- flag )
    fticks fticks>day fdrop = ;

: .ns ( r -- )  1e-9 f*
    fdup 1e-6 f< IF  1e9 f* 10 0 0 f.rdp ." ns"  EXIT  THEN
    fdup 1e-3 f< IF  1e6 f* 10 3 0 f.rdp ." µs"  EXIT  THEN
    fdup 1e   f< IF  1e3 f* 10 6 0 f.rdp ." ms"  EXIT  THEN
    10 6 0 f.rdp 's' emit ;

: .day ( day -- )
    unix-day0 + day2ymd
    rot 0 .r '-' emit swap .## '-' emit .## 'T' emit ;
: .tz ( -- )
    date? #localtime and IF
	date? #timezone and IF  $tz $.
	ELSE  tz-flag IF  'S' emit  THEN  THEN
    ELSE  'Z' emit  THEN ;
: .timeofday ( fraction/day -- )
    24 fm* fsplit
    date? #splithour and IF
	dup last-hour <> IF  ." ==== " dup .## .tz
	    ."  ====" cr  THEN  to last-hour
    ELSE  .##  THEN
    datehms? 2 < IF  fdrop  ELSE  60 fm* fsplit
    date? #splitminute and IF
	dup last-minute <> IF  ." === :" dup .## ." m ===" cr  THEN  to last-minute
	ELSE  ':' emit .##  THEN
	datehms? 3 < IF  fdrop  ELSE  ':' emit
	    60 fm* datehms? 4 < IF  f>s .##
	    ELSE  fdup 10e f< IF '0' emit 2  ELSE  3  THEN
		datehms? 1+ 7 min 3 and 3 * dup >r + r@ r> f.rdp  THEN
	THEN  THEN  date? #splithour and 0= IF  .tz  THEN ;
: .deg ( degree -- )
    fdup f0< IF ." -" fnegate THEN
    fsplit 0 .r  $B0 ( '°' ) xemit  60 fm*
    fsplit .##   ''' xemit  60 fm*
    fsplit .##   '.' xemit 100 fm*
    f>s .##      '"' xemit ;
: .never ( -- )
    datehms? 1 > IF ." never" ELSE 'n' emit THEN ;
: .forever ( -- )
    datehms? 1 > IF ." forever" ELSE 'f' emit THEN ;

: f.ticks ( rticks -- )
    fticks>day
    dup today? date? #today #splitdate or and 0= and
    IF
	drop .timeofday
    ELSE
	date? #splitdate and IF
	    dup last-day <> IF
		." ===== " dup .day ."  =====" cr
	    THEN  to last-day
	ELSE  .day  THEN
	datehms? IF .timeofday ELSE fdrop THEN
    THEN ;

: .ticks ( ticks -- )  date? 0= IF  64drop  EXIT  THEN
    64dup 64-0= IF  .never 64drop EXIT  THEN
    64dup -1 n>64 64= IF  .forever 64drop EXIT  THEN
    >fticks f.ticks ;

\ insert into sorted string array, discarding n bytes at the end

: $ins[]# ( addr u $array[] rest -- pos ) \ net2o
    \G insert O(log(n)) into pre-sorted array
    \G @var{pos} is the insertion offset or -1 if not inserted
    \G @var{rest} is the rest of the array chopped off for comparison
    { a[] rest } 0 a[] $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup rest - $# a[] $[]@ rest - compare dup 0= IF
		drop $# a[] $[]@ smove \ overwrite in place
		$# EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    { | w^ ins$0 } ins$0 cell a[] r@ cells $ins r@ a[] $[]! r> ;
: $del[]# ( addr u $array[] rest -- ) \ net2o
    \G delete O(log(n)) from pre-sorted array
    { a[] rest } 0 a[] $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup rest - $# a[] $[]@ rest - compare dup 0= IF
		drop $# a[] $[] $free
		a[] $# cells cell $del
		2drop EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT 2drop 2drop ; \ not found

\ insert into sorted string array, discarding n bytes at the start

: $ins[]/ ( addr u $array n -- pos ) \ net2o
    \G insert O(log(n)) into pre-sorted array
    \G @var{pos} is the insertion offset or -1 if not inserted
    { a[] rest } 0 a[] $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup rest safe/string $# a[] $[]@ rest safe/string compare dup 0= IF
		drop $# a[] $[]@ smove \ overwrite in place
		$# EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    { | w^ ins$0 } ins$0 cell a[] r@ cells $ins r@ a[] $[]! r> ;
: $del[]/ ( addr u $array offset -- ) \ net2o
    \G delete O(log(n)) from pre-sorted array
    { a[] rest } 0 a[] $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup rest safe/string $# a[] $[]@ rest safe/string compare dup 0= IF
		drop $# a[] $[] $free
		a[] $# cells cell $del
		2drop EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT 2drop 2drop ; \ not found

\ insert into sorted string array

: $ins[] ( addr u $array -- pos ) 0 $ins[]# ;
    \G insert O(log(n)) into pre-sorted array
    \G @var{pos} is the insertion offset or -1 if not inserted
: $del[] ( addr u $array -- ) 0 $del[]# ;
    \G delete O(log(n)) from pre-sorted array

\ hash with array of unique strings

: #!ins[]? ( addr1 u1 addr-key u-key hash -- flag )
    third third third >r 2>r
    #@ d0= IF
	$make { w^ s } s cell 2r> r> #! true
    ELSE
	last# cell+ $ins[] -1 <> rdrop 2rdrop
    THEN ;
: #!ins[] ( addr1 u1 addr-key u-key hash -- )
    #!ins[]? drop ;

\ same with signatures; newest signature replaces older

$41 Constant sigonlysize#
$51 Constant sigsize#
$71 Constant sigpksize#
$91 Constant sigpk2size#
$10 Constant datesize#

: startdate@ ( addr u -- date ) + sigsize# - le-64@ ;
: enddate@ ( addr u -- date ) + sigsize# - 64'+ le-64@ ;
: sigonly@ ( addr u -- addr' u' ) + sigonlysize# - [ sigonlysize# 1- ]L ;
: sigdate@ ( addr u -- addr' u' ) + sigsize# - [ sigsize# 1- ]L ;

: $ins[]sig# ( addr u $array n -- pos ) \ net2o
    \G insert O(log(n)) into pre-sorted array if sigdate is newer
    \G @var{pos} is the insertion offset or -1 if not inserted
    { a[] rest } 0 a[] $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup rest - $# a[] $[]@ rest - compare dup 0= IF
		drop
		2dup rest - + le-64@
		$# a[] $[]@ rest - + le-64@ 64u>=
		IF   $# a[] $[]@ smove  $# \ overwrite in place
		ELSE  2drop  -1  THEN EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    { | w^ ins$0 } ins$0 cell a[] r@ cells $ins r@ a[] $[]! r> ;

: $ins[]sig ( addr u $array -- pos ) sigsize# $ins[]sig# ;
    \G @var{pos} is the insertion offset or -1 if not inserted
: $del[]sig ( addr u $array -- ) sigsize# $del[]# ;
: $rep[]sig ( addr u $array -- ) >r
    \G replace if newer in one-element array
    r@ $[]# IF
	2dup startdate@ 0 r@ $[]@ startdate@ 64u<
	IF  2drop rdrop  EXIT  THEN
    THEN
    0 r> $[]! ;

\ list sorted by sig date

: $ins[]date ( addr u $array -- pos ) \ net2o
    \G insert O(log(n)) into pre-sorted array
    \G @var{pos} is the insertion offset or -1 if not inserted
    { a[] } 0 a[] $[]#
    BEGIN  2dup u<  WHILE  2dup + 2/ { left right $# }
	    2dup startdate@ $# a[] $[]@ startdate@ 64over 64over 64= IF
		64drop 64drop
		2dup $# a[] $[]@ compare dup 0= IF
		    drop 2drop  $# invert  EXIT  THEN
		0<  ELSE  64u<  THEN
	    IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    { | w^ ins$0 } ins$0 cell a[] r@ cells $ins r@ a[] $[]!  r> ;
: $search[]date ( ticks $array -- pos ) \ net2o
    \G search O(log(n)) in pre-sorted array
    \G @var{pos} is the first location of the item >= the requested date
    { a[] } 0 a[] $[]#
    BEGIN  2dup u<  WHILE  2dup + 2/ { left right $# }
	    64dup $# a[] $[]@ startdate@ 64over 64over 64= IF
		64drop 64drop
		0 $# 1- -DO
		    64dup I a[] $[]@ startdate@ 64<> ?LEAVE
		    I to $#
		1 -LOOP
		64drop $#  EXIT  THEN
	    64u< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop dup >r a[] $[]@ ?dup-IF
	startdate@ 64u> negate r> +
    ELSE  drop 64drop r>  THEN ;

\ filter entries out of a string array

: $[]filter { addr xt -- }
    \G execute @var{xt} for all elements of the string array @var{addr}.
    \G xt is @var{( addr u -- flag )}, getting one string at a time,
    \G if flag is false, delete the corresponding string.
    0 { idx }  BEGIN  idx addr $[]# <  WHILE
	    idx addr $[]@ xt execute IF
		idx 1+ to idx
	    ELSE
		idx addr $[] $free
		addr idx cells cell $del
	    THEN
    REPEAT ;

: $[]map? { addr xt -- }
    \G execute @var{xt} for all elements of the string array @var{addr}.
    \G xt is @var{( addr u -- flag )}, getting one string at a time
    addr $[]# 0 ?DO  I addr $[]@ xt execute ?LEAVE  LOOP ;

\ colors

synonym <default> default-color
synonym <warn>    warning-color
synonym <info>    info-color
synonym <err>     error-color
synonym <success> success-color

theme-color: <black>
theme-color: <white>
theme-color: <dim>

current-theme

light-mode

<a black >fg black >bg a> to <black>
<a white >fg white >bg bold a> to <white>
<a white >fg black >bg dim a> to <dim>

dark-mode

<a black >fg black >bg a> to <black>
<a white >fg white >bg bold a> to <white>
<a white >fg black >bg dim a> to <dim>

to current-theme

\ Memory words

\ the policy on allocation and freeing is that both freshly allocated
\ and to-be-freed memory is erased.  This makes sure that no unwanted
\ data will be lurking in that memory, waiting to be leaked out

: alloz ( size -- addr )
    dup >r allocate throw dup r> erase ;
: freez ( addr size -- ) \ net2o
    \G erase and then free - for secret stuff
    over swap erase free throw ;
: ?free ( addr size -- ) >r
    dup @ IF  dup @ r@ freez off  ELSE  drop  THEN  rdrop ;

: allo1 ( size -- addr )
    dup >r allocate throw dup r> $FF fill ;
: allocate-bits ( size -- addr )
    dup >r cell+ allo1 dup r> + off ; \ last cell is off

: ?free+guard ( addr u -- )
    over @ IF  over @ swap 2dup erase  free+guard  off
    ELSE  2drop  THEN ;

\ file stuff

: ?fd ( fd addr u -- fd' ) { addr u } dup ?EXIT drop
    ?.net2o
    addr u r/w open-file dup no-file# = IF
	2drop addr u r/w create-file
    THEN  throw ;

: write@pos-file ( addr u 64pos fd -- ) >r
    64>d r@ reposition-file throw
    r@ write-file throw r> flush-file throw ;

: append-file ( addr u fd -- 64pos ) dup
    >r file-size throw d>64 64dup { 64: pos } r> write@pos-file pos ;

: touch ( addr u -- )
    w/o create-file throw close-file throw ;

\ copy files

: throw?exists ( throwcode -- )  dup no-file# <> and throw ;

$1F to tmps# \ need more temporaries

Variable tmp-file$
: $tmp-file ( addr u -- addr' u' )
    [: type '+' emit getpid 0 .r ;] tmp-file$ $set
    tmp-file$ $@ ;

: >backup ( addr u -- )
    2dup 2dup [: type '~' emit ;] $tmp rename-file >r
    tmp-file$ $@ 2swap rename-file
    r> throw?exists throw?exists ;

: >new ( addr u -- fd )
    $tmp-file r/w create-file throw ;

: >copy ( addr u -- fd )
    2dup >new { fd1 }
    r/o open-file dup no-file# = IF
	2drop
    ELSE
	throw 0 { fd0 w^ cpy }
	#0. fd0 reposition-file throw
	fd0 cpy $slurp fd0 close-file throw
	cpy $@ fd1 write-file throw cpy $free
	fd1 flush-file throw
    THEN  fd1 ;

: save-file { d: file xt: do-save -- }
    \G save file @var{addr u} by making a copy first,
    \G applying xt ( fd -- ) on that copy, and then
    \G moving the existing file to backup ("~" appended to filename) \ net2o
    \G and the copy ("+" appended to filename) to the original name.
    file >copy dup >r do-save r> close-file throw file >backup ;

: new-file ( addr u xt -- ) \ net2o
    \G save file @var{addr u} by making an empty first,
    \G applying xt ( fd -- ) on that file, and then
    \G moving the existing file to backup ("~" appended to filename) \ net2o
    \G and the new ("+" appended to filename) to the original name.
    >r 2dup >new r> over >r execute r> close-file throw >backup ;

\ help display

: search-help ( pattern xt file-addr u -- )
    open-fpath-file throw
    [: >r BEGIN  refill  WHILE
	      source 2over string-prefix? IF  r@ execute  THEN
      REPEAT rdrop 2drop ;] execute-parsing-named-file ;
compsem: sourcefilename postpone sliteral ['] search-help compile, ;

: .cmd ( addr u -- addr u )
    source 2over nip /string type cr ;

\ single quoted string with escapes of single quote for shell invocation

: 'type' ( addr u -- ) ''' emit
    bounds ?DO  I c@ ''' = IF  .\" '\"'\"'"  ELSE  I c@ emit  THEN  LOOP
    ''' emit ;

\ insert and remove single cell items

: del$one ( addr1 addr2 size -- pos ) \ net2o
    \G @var{pos} is the deletion offset
    >r over @ cell+ - tuck r> $del ;
: next$ ( pos string -- addre addrs )
    $@ rot /string bounds ;
: del$cell ( addr stringaddr -- ) { string }
    string $@ bounds U+DO
	dup I @ = IF
	    string I cell del$one
	    string next$ replace-loop 0
	ELSE  cell  THEN
    +LOOP drop ;

\ unique list of cells

Sema resize-sema

: unique$cell? ( x addr -- flag )
    $@ bounds ?DO  dup I @ = IF  drop false unloop  EXIT  THEN
    cell +LOOP  drop true ;

: +unique$ ( x addr -- )
    [: 2dup unique$cell? IF  >stack  ELSE  2drop  THEN ;]
    resize-sema c-section ;

\ xchar tool

: *-width ( addr u -- n )
    0 -rot bounds ?DO  I c@ $C0 $80 within -  LOOP ;

e? max-xchar $100 u< [IF]
    : utf8emit ( xchar -- )
	 '?' over $100 u< select emit ;
    : >utf8$ ( addr u -- addr' u' )
	[: bounds ?DO  I c@ u8emit  LOOP ;] $tmp ;
    : $utf8> ( addr u -- addr' u' )
	[: bounds ?DO  I u8@+ utf8emit
	  I - +LOOP ;] $tmp ;
[ELSE]
    ' xemit alias utf8emit
    ' noop alias >utf8$ immediate
    ' noop alias $utf8> immediate
[THEN]

\ accept* derivative

e? max-xchar $100 < [IF] '*' [ELSE] ( '●' ) '•' [THEN] Value pw*

Variable *insflag

: *type ( addr u -- )
    config:passmode# @ dup 0< IF  drop 2drop  EXIT  THEN
    2 = IF  type  EXIT  THEN
    *-width 0 ?DO  pw* xemit  LOOP ;
: *type1 ( addr u -- )  config:passmode# @ 0<= IF  *type  ELSE  type  THEN ;
: *type2 ( addr u -- )  config:passmode# @ 1 <> IF  *type  EXIT  THEN
    dup IF  2dup over + xchar- over - dup >r 2swap r> /string 2swap
    ELSE  0 0 2swap  THEN
    *-width 0 ?DO  pw* xemit  LOOP
    dup IF  type  ELSE  2drop  THEN ;
: *-width0 ( addr u -- w )
    config:passmode# @ dup 0< IF  drop 2drop 0  EXIT  THEN
    0= IF  *-width  ELSE  x-width  THEN ;
: *-width1 ( addr u -- w )
    config:passmode# @ dup 0< IF  drop 2drop 0  EXIT  THEN
    2 = IF  x-width  ELSE  *-width  THEN ;
: *-width2 ( addr u -- w )
    case  config:passmode# @
	2 of  x-width  endof
	1 of
	    dup IF  2dup over + xchar- over - dup >r 2swap r> /string
		x-width >r *-width r> +  ELSE  nip  THEN  endof
	0 of  *-width  endof
	-1 of  2drop 0  endof
	drop swap
    endcase ;
: .*resizeline ( span addr pos -- span addr pos )
    2dup *insflag @ IF  *-width2  ELSE  *-width1  THEN >r
    setstring$ $@ *-width0 >r
    >edit-rest *-width1 r> r> + +
    dup >r edit-linew @ u< IF
	xedit-startpos  edit-linew @ spaces  edit-linew @ edit-curpos !
    THEN
    r> edit-linew ! ;
: .*all ( span addr pos -- span addr pos )
    xedit-startpos  2dup *insflag @ IF  *type2  ELSE  *type  THEN
    setstring$ $@
    dup IF  setstring-color *type1 input-color  ELSE  2drop  THEN
    >edit-rest *type  edit-linew @ edit-curpos !  ;
: .*rest ( span addr pos -- span addr pos )
    xedit-startpos
    2dup *insflag @ IF  *-width2  ELSE  *-width1  THEN  edit-curpos !
    2dup *insflag @ IF  *type2  ELSE  *type  THEN ;
: *edit-update ( -- )
    .*resizeline .*all .*rest ;
: (*xins)  *insflag on (xins) ;
: *kill-prefix  *insflag off kill-prefix ;

edit-terminal-c uclass edit-out
end-class *edit-terminal-c

*edit-terminal-c ' new static-a with-allocater Constant *edit-terminal

*edit-terminal edit-out !

' (*xins) is insert-char
' *kill-prefix is everychar
' *edit-update  is edit-update

edit-terminal edit-out !

: accept* ( addr u -- u' ) \ net2o
    \G accept-like input, but types * instead of the character
    \G don't save into history
    get-order n>r history >r  edit-out @ >r  *edit-terminal edit-out !
    0 to history  0 set-order
    ['] accept catch
    r> edit-out !  r> to history  nr> set-order
    throw space ;

\ catch loop

0 Value terminating?

: ?int ( throw-code -- throw-code )  dup -28 = terminating? or
    IF  kill-task  THEN ;

: .loop-err ( throw xt -- )
    [: ." Task: " id. dup . cr DoError cr ;] do-debug ;

: catch-loop { xt -- flag }
    BEGIN   xt catch dup -1 = ?EXIT
	?int dup  WHILE  xt .loop-err  REPEAT
    drop false ;

[IFDEF] EAGAIN
    : ?ior-again ( n -- )
	errno EAGAIN <> and ?ior ;
[THEN]

\ !wrapper: generic wrapper to store a value in a variable
\ and restore it after catching the xt

\ evaluate in

: evaluate-in ( addr u voc-addr -- )
    >wordlist ['] forth-recognize ['] evaluate wrap-xt ;
