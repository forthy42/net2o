\ net2o tools

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

require net2o-err.fs
require unix/pthread.fs
require unix/mmap.fs
require 64bit.fs
require date.fs

\ enum

: enum ( n "name" -- n+1 )  dup Constant 1+ ;
: bit ( n "name" -- n*2 )   dup Constant 2* ;

\ argument handling that works transparent from OS and Forth command line

user-o arg-o

object class
    umethod ?nextarg
    umethod ?@nextarg
    umethod ?peekarg
end-class cmd-args-c

align cmd-args-c , here constant cmd-args^

: cmd-args ( -- )  cmd-args^ arg-o ! ;
cmd-args

:noname ( -- addr u t / f )
    argc @ 1 > IF  next-arg true  ELSE  false  THEN ; to ?nextarg
:noname ( -- addr u t / f )
    argc @ 1 > IF  1 arg true  ELSE  false  THEN ; to ?peekarg
:noname ( -- addr u t / f )
    argc @ 1 > IF
	1 arg drop c@ '@' = IF  next-arg 1 /string true  EXIT  THEN
    THEN  false ; to ?@nextarg

cmd-args-c class
end-class word-args-c

align word-args-c , here constant word-args^

: word-args ( -- )  word-args^ arg-o ! ;

word-args

: parse-name" ( -- addr u )
    >in @ >r parse-name
    over c@ '"' = IF  2drop r@ >in ! '"' parse 2drop \"-parse  THEN  rdrop ;
: ?word-nextarg ( -- addr u t / f )
    parse-name" dup 0= IF  2drop  false  ELSE  true  THEN
; lastxt to ?nextarg
:noname ( -- addr u t / f )  >in @ >r
    parse-name" dup 0= IF  2drop  false  ELSE  true  THEN  r> >in !
; to ?peekarg
:noname ( -- addr u t / f )
    >in @ >r ?word-nextarg 0= IF  rdrop false  EXIT  THEN
    over c@ '@' = IF  rdrop 1 /string true  EXIT  THEN
    r> >in ! 2drop false ; to ?@nextarg

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
: safe/string ( c-addr u n -- c-addr' u' )
\G protect /string against overflows.
    dup negate >r  dup 0> IF
        /string dup r> u>= IF  + 0  THEN
    ELSE
        /string dup r> u< IF  + 1+ -1  THEN
    THEN ;
[THEN]

[IFUNDEF] string-suffix?
    : string-suffix? ( addr1 u1 addr2 u2 -- flag )
	\G return true if addr2 u2 is a suffix of addr1 u1
	tuck 2>r over swap - 0 max /string 2r> str= ;
[THEN]

: -skip ( addr u char -- ) >r
    BEGIN  1- dup  0>= WHILE  2dup + c@ r@ <>  UNTIL  THEN  1+ rdrop ;
: -scan ( addr u char -- addr u' ) >r
    BEGIN  dup  WHILE  1- 2dup + c@ r@ =  UNTIL  1+  THEN  rdrop ;

[IFUNDEF] basename
: basename ( addr u -- addr' u' )
    2dup '/' -scan nip /string ;
[THEN]

: str0? ( addr u -- flag )
    \ check if string is all zero
    0 scan nip 0= ;

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

: or!   ( x addr -- )    >r r@ @ or   r> ! ;
: xor!  ( x addr -- )    >r r@ @ xor  r> ! ;
: and!  ( x addr -- )    >r r@ @ and  r> ! ;

: xorc! ( x c-addr -- )  >r r@ c@ xor r> c! ;
: andc! ( x c-addr -- )  >r r@ c@ and r> c! ;
: orc!  ( x c-addr -- )  >r r@ c@ or  r> c! ;

: max!@ ( n addr -- )    >r r@ @ max  r> !@ ;
: umax!@ ( n addr -- )   >r r@ @ umax r> !@ ;

\ user stack, automatic init+dispose

: ustack ( "name" -- )
    \G generate user stack, including initialization and free on thread
    \G start and termination
    User  latestxt >r
    :noname  action-of thread-init compile,
    r@ compile, postpone off postpone ;
    is thread-init
    :noname  r> compile, postpone $off  action-of kill-task  compile,
    postpone ;
    is kill-task ;

[IFUNDEF] NOPE
    : NOPE ( c:sys -- )
	\G removes a control structure sys from the stack
	drop 2drop ; immediate restrict
[THEN]

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

\ aliases for old Gforth (pre 20161202)

[IFDEF] >deque  ' >deque alias >stack [THEN]
[IFDEF] deque>  ' deque> alias stack> [THEN]
[IFDEF] deque<  ' deque< alias >back [THEN]
[IFDEF] <deque  ' <deque alias back> [THEN]
[IFDEF] deque@  ' deque@ alias get-stack [THEN]
[IFDEF] deque!  ' deque! alias set-stack [THEN]

\ scoping

Variable scope<>
: scope{ ( "vocabulary" -- scope:addr )
    get-current scope<> >stack also ' execute definitions ;
: }scope ( scope:addr -- )
    previous scope<> stack> set-current ;
: scope: ( "vocabulary" -- scope:addr )
    vocabulary get-current scope<> >stack also lastxt execute definitions ;

: with ( "vocabulary" -- )
    also ' execute postpone >o ; immediate restrict
: endwith ( -- )
    postpone o> previous ; immediate restrict

\ file name sanitizer

: printable? ( addr u -- flag )
    true -rot bounds ?DO  I c@ $80 u>= IF
	    I ['] u8@+ catch IF  drop 0 true
	    ELSE  drop dup I - swap I' u>  THEN
	ELSE  1 I c@ $7F bl within  THEN
	IF  2drop false  LEAVE  THEN  +LOOP ;

: ?sane-file ( addr u -- addr u )
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

: .## ( n -- ) s>d <# # # #> type ;
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

: init-dir ( addr u mode -- flag ) >r
    \G create a directory with access mode,
    \G return true if the dictionary is new, false if it already existed
    2dup file-status nip no-file# = IF
	r> mkdir-parents throw  true
    ELSE  2drop rdrop  false  THEN ;

scope{ config

Variable passmode#
Variable logsize#
2Variable savedelta&
2Variable patchlimit&
Variable rootdirs$
Variable prio#
Variable host$
Variable date#
$Variable objects$
$Variable chats$
$Variable keys$
$Variable .net2o$
$Variable invite$

}scope

also config

"~/.net2o" .net2o$ $!
"~/.net2o/keys" keys$ $!
"~/.net2o/chats" chats$ $!
"~/.net2o/objects" objects$ $!
#2 date# !
#20 logsize# !
pad $400 get-dir rootdirs$ $!
"Hello!" invite$ $!
[defined] android 1 and passmode# ! \ default is all entry is masked out

$1000.0000. patchlimit& 2! \ 256MB patch limit size
#10.000.000.000. savedelta& 2! \ 10 seconds deltat

: .net2o/ ( addr u -- addr' u' ) [: .net2o$ $. '/' emit type ;] $tmp ;
: .keys/  ( addr u -- addr' u' ) [: keys$   $. '/' emit type ;] $tmp ;
: .chats/ ( addr u -- addr' u' )
    chat-sanitize [: chats$  $. '/' emit type ;] $tmp ;
: .objects/ ( addr u -- addr' u' )
    hash-sanitize [: objects$  $. '/' emit type ;] $tmp ;
: objects/.no-fat-file ( -- addr u )
    [: '.' emit no-fat-chars type ;] $tmp .objects/ ;
: chats/.no-fat-file ( -- addr u )
    [: '.' emit no-fat-chars type ;] $tmp .chats/ ;

: ?.net2o ( -- )  .net2o$ $@ $1FF init-dir drop ;
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

$Variable config-file$  "~/.net2o/config" config-file$ $!
Variable configured?

:noname defers 'cold
    configured? off
    pad $400 get-dir rootdirs$ $!
; is 'cold
:noname ( -- )
    config:host$ $off
    config:rootdirs$ $off
    defers 'image ; is 'image

: rootdirs>path ( -- )
    config:rootdirs$ $@ bounds ?DO  I c@ ':' = IF 0 I c! THEN LOOP ;

: ?.net2o-config ( -- )  true configured? !@ ?EXIT
    "NET2O_CONF" getenv ?dup-IF  config-file$ $!  ELSE  drop  THEN
    config-file$ $@ 2dup file-status nip  ['] config >body swap
    no-file# = IF  ?.net2o write-config  ELSE  read-config ?.net2o  THEN
    rootdirs>path ;

: init-dirs ( -- ) ?.net2o-config fsane-init ;

previous

\ print time

64Variable tick-adjust
: ticks ( -- u64 )  ntime d>64 tick-adjust 64@ 64+ ;

: ticks-u ( -- u )  ticks 64>n ;

1970 1 1 ymd2day Constant unix-day0

: fsplit ( r -- r n )  fdup floor fdup f>s f- ;

: date? ( -- n )  config:date# @ ;
: datehms? ( -- n )  config:date# @ 7 and ;
$8 Constant #today
$10 Constant #splitdate
$20 Constant #splithour
$40 Constant #splitminute
-1 Value last-day
-1 Value last-hour
-1 Value last-minute

: reset-time ( -- )
    -1 to last-day  -1 to last-hour  -1 to last-minute ;
: today? ( day -- flag )
    ticks 64>f 1e-9 f* 86400e f/ floor f>s = ;

: .ns ( r -- )  1e-9 f*
    fdup 1e-6 f< IF  1e9 f* 10 0 0 f.rdp ." ns"  EXIT  THEN
    fdup 1e-3 f< IF  1e6 f* 10 3 0 f.rdp ." µs"  EXIT  THEN
    fdup 1e   f< IF  1e3 f* 10 6 0 f.rdp ." ms"  EXIT  THEN
    10 6 0 f.rdp 's' emit ;

: >day ( seconds -- fraction day )
    86400e f/ fsplit ;
: .day ( seconds -- fraction/day )
    unix-day0 + day2ymd
    rot 0 .r '-' emit swap .## '-' emit .## 'T' emit ;
: .timeofday ( fraction/day -- )
    24e f* fsplit
    date? #splithour and IF
	dup last-hour <> IF  ." ==== " dup .## ." Z ====" cr  THEN  to last-hour
    ELSE  .##  THEN
    datehms? 2 < IF  fdrop  ELSE  60e f* fsplit
    date? #splitminute and IF
	dup last-minute <> IF  ." === :" dup .## ." m ===" cr  THEN  to last-minute
	ELSE  ':' emit .##  THEN
	datehms? 3 < IF  fdrop  ELSE  ':' emit
	    60e f* datehms? 4 < IF  f>s .##
	    ELSE  fdup 10e f< IF '0' emit 2  ELSE  3  THEN
		datehms? 1+ 7 min 3 and 3 * dup >r + r@ r> f.rdp  THEN
	THEN  THEN  date? #splithour and 0= IF  'Z' emit  THEN ;
: .deg ( degree -- )
    fdup f0< IF ." -" fnegate THEN
    fsplit 0 .r  $B0 ( '°' ) xemit  60e f*
    fsplit .##   ''' xemit  60e f*
    fsplit .##   '.' xemit 100e f*
    f>s .##      '"' xemit ;
: .never ( -- )
    datehms? 1 > IF ." never" ELSE 'n' emit THEN ;
: .forever ( -- )
    datehms? 1 > IF ." forever" ELSE 'f' emit THEN ;

: f.ticks ( rticks -- )
    1e-9 f* >day
    dup today? date? #today and 0= and
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
    64>f f.ticks ;

\ insert into sorted string array, discarding n bytes at the end

: $ins[]# ( addr u $array n -- pos )
    \G insert O(log(n)) into pre-sorted array
    \G @var{pos} is the insertion offset or -1 if not inserted
    { a[] rest } 0 a[] $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup rest - $# a[] $[]@ rest - compare dup 0= IF
		drop $# a[] $[]@ smove \ overwrite in place
		$# EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    0 { w^ ins$0 } ins$0 cell a[] r@ cells $ins r@ a[] $[]! r> ;
: $del[]# ( addr u $array offset -- )
    \G delete O(log(n)) from pre-sorted array
    { a[] rest } 0 a[] $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup rest - $# a[] $[]@ rest - compare dup 0= IF
		drop $# a[] $[] $off
		a[] $# cells cell $del
		2drop EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT 2drop 2drop ; \ not found

\ insert into sorted string array, discarding n bytes at the start

: $ins[]/ ( addr u $array n -- pos )
    \G insert O(log(n)) into pre-sorted array
    \G @var{pos} is the insertion offset or -1 if not inserted
    { a[] rest } 0 a[] $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup rest safe/string $# a[] $[]@ rest safe/string compare dup 0= IF
		drop $# a[] $[]@ smove \ overwrite in place
		$# EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    0 { w^ ins$0 } ins$0 cell a[] r@ cells $ins r@ a[] $[]! r> ;
: $del[]/ ( addr u $array offset -- )
    \G delete O(log(n)) from pre-sorted array
    { a[] rest } 0 a[] $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup rest safe/string $# a[] $[]@ rest safe/string compare dup 0= IF
		drop $# a[] $[] $off
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

: $ins[]sig# ( addr u $array n -- pos )
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
    0 { w^ ins$0 } ins$0 cell a[] r@ cells $ins r@ a[] $[]! r> ;

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

: $ins[]date ( addr u $array -- pos )
    \G insert O(log(n)) into pre-sorted array
    \G @var{pos} is the insertion offset or -1 if not inserted
    { a[] } 0 a[] $[]#
    BEGIN  2dup u<  WHILE  2dup + 2/ { left right $# }
	    2dup startdate@ $# a[] $[]@ startdate@ 64over 64over 64= IF
		64drop 64drop
		2dup $# a[] $[]@ compare dup 0= IF  drop 2drop  -1  EXIT  THEN
		0<  ELSE  64u<  THEN
	    IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    0 { w^ ins$0 } ins$0 cell a[] r@ cells $ins r@ a[] $[]!  r> ;
: $search[]date ( ticks $array -- pos )
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
    REPEAT  drop >r r@ a[] $[]@ ?dup-IF
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
		idx addr $[] $off
		addr idx cells cell $del
	    THEN
    REPEAT ;

: $[]map? { addr xt -- }
    \G execute @var{xt} for all elements of the string array @var{addr}.
    \G xt is @var{( addr u -- flag )}, getting one string at a time
    addr $[]# 0 ?DO  I addr $[]@ xt execute ?LEAVE  LOOP ;

\ colors

: <default> default-color attr! ;
: <warn>    warn-color attr! ;
: <info>    info-color attr! ;
: <err>     err-color attr! ;
: <black>   [ black >fg black >bg or ]l attr! ;

\ Memory words

\ the policy on allocation and freeing is that both freshly allocated
\ and to-be-freed memory is erased.  This makes sure that no unwanted
\ data will be lurking in that memory, waiting to be leaked out

: alloz ( size -- addr )
    dup >r allocate throw dup r> erase ;
: freez ( addr size -- )
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

: append-file ( addr u fd -- 64pos ) >r
    r@ file-size throw d>64 64dup { 64: pos } r> write@pos-file pos ;

: touch ( addr u -- )
    w/o create-file throw close-file throw ;

\ copy files

: throw?exists ( throwcode -- )  dup no-file# <> and throw ;

$1F to tmps# \ need more temporaries

: >backup ( addr u -- )
    2dup 2dup [: type '~' emit ;] $tmp rename-file >r
    2dup [: type '+' emit getpid 0 .r ;] $tmp 2swap rename-file
    r> throw?exists throw?exists ;

: >new ( addr u -- fd )
    [: type '+' emit getpid 0 .r ;] $tmp r/w create-file throw ;

: >copy ( addr u -- fd )
    2dup >new { fd1 }
    r/o open-file dup no-file# = IF
	2drop
    ELSE
	throw 0 { fd0 w^ cpy }
	#0. fd0 reposition-file throw
	fd0 cpy $slurp fd0 close-file throw
	cpy $@ fd1 write-file throw cpy $off
	fd1 flush-file throw
    THEN  fd1 ;

: save-file ( addr u xt -- )
    \G save file @var{addr u} by making a copy first,
    \G applying xt ( fd -- ) on that copy, and then
    \G moving the existing file to backup ("~" appended to filename)
    \G and the copy ("+" appended to filename) to the original name.
    >r 2dup >copy r> over >r execute r> close-file throw >backup ;

: new-file ( addr u xt -- )
    \G save file @var{addr u} by making an empty first,
    \G applying xt ( fd -- ) on that file, and then
    \G moving the existing file to backup ("~" appended to filename)
    \G and the new ("+" appended to filename) to the original name.
    >r 2dup >new r> over >r execute r> close-file throw >backup ;

\ help display

: search-help ( pattern xt file-addr u -- )
    open-fpath-file throw
    [: >r BEGIN  refill  WHILE
	      source 2over string-prefix? IF  r@ execute  THEN
      REPEAT rdrop 2drop ;] execute-parsing-named-file ;
comp: sourcefilename postpone sliteral :, ;

: .cmd ( addr u -- addr u )
    source 2over nip /string type cr ;

\ single quoted string with escapes of single quote for shell invocation

: 'type' ( addr u -- ) ''' emit
    bounds ?DO  I c@ ''' = IF  .\" '\"'\"'"  ELSE  I c@ emit  THEN  LOOP
    ''' emit ;

\ insert and remove single cell items

: del$one ( addr1 addr2 size -- pos )
    \G @var{pos} is the deletion offset
    >r over @ cell+ - tuck r> $del ;
: next$ ( pos string -- addre addrs )
    $@ rot /string bounds ;
: del$cell ( addr stringaddr -- ) { string }
    string $@ bounds ?DO
	dup I @ = IF
	    string I cell del$one
	    unloop string next$ ?DO NOPE 0
	ELSE  cell  THEN
    +LOOP drop ;

\ unique list of cells

Sema resize-sema

: unique$cell? ( x addr -- flag )
    $@ bounds ?DO  dup I @ = IF  drop false unloop  EXIT  THEN
    cell +LOOP  drop true ;

: +unique$ ( x addr -- )
    [: 2dup unique$cell? IF
	  >r { w^ x } x cell r> $+!
      ELSE  2drop  THEN ;] resize-sema c-section ;

\ string values

[IFUNDEF] $value:
    cell      ' aligned   ' $@  ' $!  wrap+value: $value: ( u1 "name" -- u2 )
[THEN]

\ xchar tool

: *-width ( addr u -- n )
    0 -rot bounds ?DO  I c@ $C0 $80 within -  LOOP ;

e? max-xchar $100 u< [IF]
    : >utf8$ ( addr u -- addr' u' )
	[: bounds ?DO  I c@ u8emit  LOOP ;] $tmp ;
    : $utf8> ( addr u -- addr' u' )
	[: bounds ?DO  I u8@+ '?' over $100 u< select emit
	  I - +LOOP ;] $tmp ;
[ELSE]
    ' noop alias >utf8$ immediate
    ' noop alias $utf8> immediate
[THEN]

\ accept* derivative

[IFDEF] mslinux '*' [ELSE]
    e? max-xchar $100 < [IF] '*' [ELSE] '•' [THEN] [THEN] Constant pw*

Variable *insflag

: *type ( addr u -- )  config:passmode# @ 2 = IF  type  EXIT  THEN
    *-width 0 ?DO  pw* xemit  LOOP ;
: *type1 ( addr u -- )  config:passmode# @ 0= IF  *type  ELSE  type  THEN ;
: *type2 ( addr u -- )  config:passmode# @ 1 <> IF  *type  EXIT  THEN
    dup IF  2dup over + xchar- over - dup >r 2swap r> /string 2swap
    ELSE  0 0 2swap  THEN
    *-width 0 ?DO  pw* xemit  LOOP
    dup IF  type  ELSE  2drop  THEN ;
: *-width0 ( addr u -- )
    config:passmode# @ 0 = IF  *-width  ELSE  x-width  THEN ;
: *-width1 ( addr u -- )
    config:passmode# @ 2 = IF  x-width  ELSE  *-width  THEN ;
: *-width2 ( addr u -- )
    case  config:passmode# @
	2 of  x-width  endof
	1 of
	    dup IF  2dup over + xchar- over - dup >r 2swap r> /string
		x-width >r *-width r> +  ELSE  nip  THEN  endof
	0 of  *-width  endof
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
    dup IF  ['] *type1 setstring-color color-execute  ELSE  2drop  THEN
    >edit-rest *type  edit-linew @ edit-curpos !  ;
: .*rest ( span addr pos -- span addr pos )
    xedit-startpos
    2dup *insflag @ IF  *-width2  ELSE  *-width1  THEN  edit-curpos !
    2dup *insflag @ IF  *type2  ELSE  *type  THEN ;
: *edit-update ( -- )
    .*resizeline .*all .*rest ;
: (*xins)  *insflag on (xins) ;
: *kill-prefix  *insflag off kill-prefix ;

edit-terminal-c class
end-class *edit-terminal-c

*edit-terminal-c ' new static-a with-allocater Constant *edit-terminal

*edit-terminal edit-out !

' (*xins) is insert-char
' *kill-prefix is everychar
' *edit-update  is edit-update

edit-terminal edit-out !

: accept* ( addr u -- u' )
    \G accept-like input, but types * instead of the character
    \G don't save into history
    get-order n>r history >r  edit-out @ >r  *edit-terminal edit-out !
    0 to history  0 set-order
    ['] accept catch
    r> edit-out !  r> to history  nr> set-order
    throw space ;

\ catch loop

: ?int ( throw-code -- throw-code )  dup -28 = IF  bye  THEN ;

: .loop-err ( throw xt -- )
    .name dup . cr DoError cr ;

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

: !wrapper ( val addr xt -- .. ) { addr xt -- .. }
    addr !@ >r xt catch r> addr ! throw ;

\ blocking event, also available in most recent Gforth

[IFUNDEF] event|
    event: :>restart ( task -- ) restart ;
    
    : event| ( task -- )
	\G send an event and block
	dup up@ = IF \ don't block, just eval if we send to ourselves
	    event> ?events
	ELSE
	    up@ elit, :>restart event> stop
	THEN ;
[THEN]
