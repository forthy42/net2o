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
require ansi.fs

\ enum

: enum ( n "name" -- n+1 )  dup Constant 1+ ;
: bit ( n "name" -- n*2 )   dup Constant 2* ;

\ argument handling that works transparent from OS and Forth command line

Defer ?nextarg
Defer ?@nextarg
Defer ?peekarg

: ?cmd-nextarg ( -- addr u t / f )
    argc @ 1 > IF  next-arg true  ELSE  false  THEN ;
: ?cmd-peekarg ( -- addr u t / f )
    argc @ 1 > IF  1 arg true  ELSE  false  THEN ;
: ?cmd-@nextarg ( -- addr u t / f )
    argc @ 1 > IF
	1 arg drop c@ '@' = IF  next-arg 1 /string true  EXIT  THEN
    THEN  false ;

: cmd-args ( -- )
    ['] ?cmd-nextarg IS ?nextarg
    ['] ?cmd-peekarg IS ?peekarg
    ['] ?cmd-@nextarg IS ?@nextarg ;

: parse-name" ( -- addr u )
    >in @ >r parse-name
    over c@ '"' = IF  2drop r@ >in ! '"' parse 2drop \"-parse  THEN  rdrop ;
: ?word-nextarg ( -- addr u t / f )
    parse-name" dup 0= IF  2drop  false  ELSE  true  THEN ;
: ?word-peekarg ( -- addr u t / f )  >in @ >r
    parse-name" dup 0= IF  2drop  false  ELSE  true  THEN  r> >in ! ;
: ?word-@nextarg ( -- addr u t / f )
    >in @ >r ?word-nextarg 0= IF  rdrop false  EXIT  THEN
    over c@ '@' = IF  rdrop 1 /string true  EXIT  THEN
    r> >in ! 2drop false ;

: word-args ( -- )
    ['] ?word-nextarg IS ?nextarg
    ['] ?word-peekarg IS ?peekarg
    ['] ?word-@nextarg IS ?@nextarg ;
word-args

: arg-loop { xt -- }
    begin  ?nextarg  while  xt execute  repeat ;
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

: basename ( addr u -- addr' u' )
    2dup '/' -scan nip /string ;

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

\ bit vectors, lsb first

: bits ( n -- n ) 1 swap lshift ;

: >bit ( addr n -- c-addr mask ) 8 /mod rot + swap bits ;
: +bit ( addr n -- )  >bit over c@ or swap c! ;
: +bit@ ( addr n -- flag )  >bit over c@ 2dup and >r
    or swap c! r> 0<> ;
: -bit ( addr n -- )  >bit invert over c@ and swap c! ;
: -bit@ ( addr n -- flag )  >bit over c@ 2dup and >r
    invert or invert swap c! r> 0<> ;
: bit! ( flag addr n -- ) rot IF  +bit  ELSE  -bit  THEN ;
: bit@ ( addr n -- flag )  >bit swap c@ and 0<> ;

: bittype ( addr base n -- )  bounds +DO
	dup I bit@ '+' '-' rot select emit  LOOP  drop ;

: bit-erase ( addr off len -- )
    dup 8 u>= IF
	>r dup 7 and >r 3 rshift + r@ bits 1- over andc!
	1+ 8 r> - r> swap -
	dup 7 and >r 3 rshift 2dup erase +
	0 r> THEN
    bounds ?DO  dup I -bit  LOOP  drop ;

: bit-fill ( addr off len -- )
    dup 8 u>= IF
	>r dup 7 and >r 3 rshift + r@ bits 1- invert over orc!
	1+ 8 r> - r> swap -
	dup 7 and >r 3 rshift 2dup $FF fill +
	0 r> THEN
    bounds ?DO  dup I +bit  LOOP  drop ;

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

[IFUNDEF] w, : w, ( w -- )  here w! 2 allot ; [THEN]

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

: scope{ ( "vocabulary" -- addr )
    get-current also ' execute definitions ;
: }scope ( addr -- )
    previous set-current ;
: scope: ( "vocabulary" -- addr )
    vocabulary get-current also lastxt execute definitions ;

: with ( "vocabulary" -- )
    also ' execute postpone >o ; immediate restrict
: endwith ( -- )
    postpone o> previous ; immediate restrict

\ config stuff

require config.fs

\ net2o specific configurations

#-514 Constant no-file#

: init-dir ( addr u mode -- flag ) >r
    \G create a directory with access mode,
    \G return true if the dictionary is new, false if it already existed
    2dup file-status nip no-file# = IF
	r> =mkdir throw  true
    ELSE  2drop rdrop  false  THEN ;

scope{ config

Variable .net2o$
Variable keys$
Variable chats$
Variable date#

}scope

also config

"~/.net2o" .net2o$ $!
"~/.net2o/keys" keys$ $!
"~/.net2o/chats" chats$ $!
2 date# !

: .net2o/ ( addr u -- addr' u' ) [: .net2o$ $. '/' emit type ;] $tmp ;
: .keys/  ( addr u -- addr' u' ) [: keys$   $. '/' emit type ;] $tmp ;
: .chats/ ( addr u -- addr' u' ) [: chats$  $. '/' emit type ;] $tmp ;

: ?.net2o ( -- )  .net2o$ $@ $1FF init-dir drop ;
: ?.net2o/keys ( -- flag ) ?.net2o keys$ $@ $1C0 init-dir ;
: ?.net2o/chats ( -- ) ?.net2o chats$ $@ $1FF init-dir drop ;

Variable config-file$  "~/.net2o/config" config-file$ $!

: ?.net2o-config ( -- )
    config-file$ $@ 2dup file-status nip
    no-file# = IF  write-config  ELSE  read-config  THEN ;

: init-dirs ( -- ) ?.net2o ?.net2o-config ;

previous

\ print time

64Variable tick-adjust
: ticks ( -- u64 )  ntime d>64 tick-adjust 64@ 64+ ;

: ticks-u ( -- u )  ticks 64>n ;

1970 1 1 ymd2day Constant unix-day0

: fsplit ( r -- r n )  fdup floor fdup f>s f- ;

: date? ( -- n )  config:date# @ ;

: today? ( day -- flag )
    ticks 64>f 1e-9 f* 86400e f/ floor f>s = ;

: .ns ( r -- )  1e-9 f*
    fdup 1e-6 f< IF  1e9 f* 10 0 0 f.rdp ." ns"  EXIT  THEN
    fdup 1e-3 f< IF  1e6 f* 10 3 0 f.rdp ." µs"  EXIT  THEN
    fdup 1e   f< IF  1e3 f* 10 6 0 f.rdp ." ms"  EXIT  THEN
    10 6 0 f.rdp 's' emit ;

: .2 ( n -- ) s>d <# # # #> type ;
: >day ( seconds -- fraction day )
    86400e f/ fsplit ;
: .day ( seconds -- fraction/day )
    unix-day0 + day2ymd
    rot 0 .r '-' emit swap .2 '-' emit .2 'T' emit ;
: .timeofday ( fraction/day -- )
    24e f* fsplit .2 ':' emit 60e f* fsplit .2
    date? 3 < IF  fdrop  ELSE  ':' emit
	60e f* date? 1 and IF  f>s .2
	ELSE  fdup 10e f< IF '0' emit 5  ELSE  6  THEN  3 3 f.rdp  THEN
    THEN  'Z' emit ;
: .deg ( degree -- )
    fdup f0< IF ." -" fnegate THEN
    fsplit 0 .r '°' xemit  60e f*
    fsplit .2   ''' xemit  60e f*
    fsplit .2   '"' xemit 100e f*
    f>s .2 ;
: .never ( -- )
    date? 3 and 1 > IF ." never" ELSE 'n' emit THEN ;
: .forever ( -- )
    date? 3 and 1 > IF ." forever" ELSE 'f' emit THEN ;

: .ticks ( ticks -- )  date? 0= IF  64drop  EXIT  THEN
    64dup 64-0= IF  .never 64drop EXIT  THEN
    64dup -1 n>64 64= IF  .forever 64drop EXIT  THEN
    64>f 1e-9 f* >day
    dup today? date? 4 and 0= and
    date? dup >r 3 and config:date# !
    IF
	drop .timeofday
    ELSE
	.day date? 1 > IF .timeofday ELSE fdrop THEN
    THEN  r> config:date# ! ;

\ insert into sorted string array, discarding n bytes at the end

: $ins[]# ( addr u $array n -- )
    \G insert O(log(n)) into pre-sorted array
    { $a rest } 0 $a $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup rest - $# $a $[]@ rest - compare dup 0= IF
		drop $# $a $[]@ smove \ overwrite in place
		EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    0 { w^ ins$0 } ins$0 cell $a r@ cells $ins r> $a $[]! ;
: $del[]# ( addr u $array offset -- )
    \G delete O(log(n)) from pre-sorted array
    { $a rest } 0 $a $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup rest - $# $a $[]@ rest - compare dup 0= IF
		drop $# $a $[] $off
		$a $# cells cell $del
		2drop EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT 2drop 2drop ; \ not found

\ insert into sorted string array

: $ins[] ( addr u $array -- ) 0 $ins[]# ;
    \G insert O(log(n)) into pre-sorted array
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

: $ins[]sig# ( addr u $array n -- )
    \G insert O(log(n)) into pre-sorted array if sigdate is newer
    { $a rest } 0 $a $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup rest - $# $a $[]@ rest - compare dup 0= IF
		drop
		2dup rest - + le-64@
		$# $a $[]@ rest - + le-64@ 64u>=
		IF   $# $a $[]@ smove \ overwrite in place
		ELSE  2drop  THEN EXIT  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    0 { w^ ins$0 } ins$0 cell $a r@ cells $ins r> $a $[]! ;

: $ins[]sig ( addr u $array -- ) sigsize# $ins[]sig# ;
: $del[]sig ( addr u $array -- ) sigsize# $del[]# ;
: $rep[]sig ( addr u $array -- ) >r
    \G replace if newer in one-element array
    r@ $[]# IF
	2dup startdate@ 0 r@ $[]@ startdate@ 64u<
	IF  2drop rdrop  EXIT  THEN
    THEN
    0 r> $[]! ;

\ list sorted by sig date

: $ins[]date ( addr u $array -- )
    \G insert O(log(n)) into pre-sorted array
    { $a } 0 $a $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup startdate@ $# $a $[]@ startdate@ 64- 64dup 64-0= IF
		64drop 2drop \ don't overwrite if already exists!
		EXIT  THEN
	    64-0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    0 { w^ ins$0 } ins$0 cell $a r@ cells $ins r> $a $[]! ;
: $del[]date ( addr u $array -- )
    \G delete O(log(n)) from pre-sorted array
    { $a } 0 $a $[]#
    BEGIN  2dup <  WHILE  2dup + 2/ { left right $# }
	    2dup startdate@ $# $a $[]@ startdate@ 64- 64dup 64-0= IF
		64drop $# $a $[] $off
		$a $# cells cell $del
		2drop EXIT  THEN
	    64-0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT 2drop 2drop ; \ not found

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

\ file name sanitizer

$20 buffer: filechars
filechars $20 $FF fill
0 filechars l! \ ctrl chars are all illegal
filechars '/' -bit
filechars #del -bit
"\\:?*\q<>|" 2Constant no-fat-chars

?.net2o no-fat-chars .net2o/ r/w create-file [IF] drop
    no-fat-chars bounds [?DO] filechars [I] c@ -bit [LOOP]
[ELSE]
    close-file throw no-fat-chars .net2o/ delete-file throw
[THEN]

: sane-type ( addr u -- )
    [: bounds ?DO
	  I c@ filechars over bit@
	  IF  emit  ELSE  '%' emit .2  THEN
      LOOP ;] $10 base-execute ;

: fn-sanitize ( addr u -- addr' u' )
    ['] sane-type $tmp ;

\ copy files

: throw?exists ( throwcode -- )  dup no-file# <> and throw ;

$1F to tmps# \ need more temporaries

: >backup ( addr u -- )
    2dup 2dup [: type '~' emit ;] $tmp rename-file throw?exists
    2dup [: type '+' emit getpid 0 .r ;] $tmp 2swap rename-file throw ;

: >new ( addr u -- fd )
    [: type '+' emit getpid 0 .r ;] $tmp r/w create-file throw ;

: >copy ( addr u -- fd )
    2dup >new { fd1 }
    r/o open-file dup no-file# = IF
	2drop
    ELSE
	throw 0 { fd0 w^ cpy }
	0. fd0 reposition-file throw
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
comp: loadfilename 2@ postpone sliteral :, ;

: .cmd ( addr u -- addr u )
    source 2over nip /string type cr ;

\ single quoted string with escapes of single quote for shell invocation

: 'type' ( addr u -- ) ''' emit
    bounds ?DO  I c@ ''' = IF  .\" '\"'\"'"  ELSE  I c@ emit  THEN  LOOP
    ''' emit ;

\ insert and remove keys

Variable 0keys

Sema 0key-sema

: ins-0key [: { w^ addr -- }
	addr cell 0keys $+! ;] 0key-sema c-section ;
: del$one ( addr1 addr2 size -- pos )
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
: del-0key ( addr -- )
    [: 0keys del$cell ;] 0key-sema c-section ;
: search-0key ( .. xt -- .. )
    [: { xt } 0keys $@ bounds ?DO
	    I xt execute 0= ?LEAVE
	cell +LOOP
    ;] 0key-sema c-section ;

\ unique list of cells

Sema resize-sema

: unique$cell? ( x addr -- flag )
    $@ bounds ?DO  dup I @ = IF  drop false unloop  EXIT  THEN
    cell +LOOP  drop true ;

: +unique$ ( x addr -- )
    [: 2dup unique$cell? IF
	  >r { w^ x } x cell r> $+!
      ELSE  2drop  THEN ;] resize-sema c-section ;

\ xchar tool

: *-width ( addr u -- n )
    0 -rot bounds ?DO  I c@ $C0 $80 within -  LOOP ;

\ catch loop

: ?int ( throw-code -- throw-code )  dup -28 = IF  bye  THEN ;

: .loop-err ( throw xt -- )
    .name dup . cr DoError cr ;

: catch-loop { xt -- flag }
    BEGIN   nothrow xt catch dup -1 = ?EXIT
	?int dup  WHILE  xt .loop-err  REPEAT
    drop false ;

[IFDEF] EAGAIN
    : ?ior-again ( n -- )
	errno EAGAIN <> and ?ior ;
[THEN]