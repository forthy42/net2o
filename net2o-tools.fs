\ net2o tools

Defer ?nextarg
Defer ?@nextarg

: ?cmd-nextarg ( -- addr u t / f )
    argc @ 1 > IF  next-arg true  ELSE  false  THEN ;
: ?cmd-@nextarg ( -- addr u t / f )
    argc @ 1 > IF
	1 arg drop c@ '@' = IF  next-arg 1 /string true  EXIT  THEN
    THEN  false ;

: cmd-args ( -- )
    ['] ?cmd-nextarg IS ?nextarg
    ['] ?cmd-@nextarg IS ?@nextarg ;
cmd-args

: parse-name" ( -- addr u )
    >in @ >r parse-name
    over c@ '"' = IF  2drop r@ >in ! '"' parse 2drop \"-parse  THEN  rdrop ;
: ?word-nextarg ( -- addr u t / f )
    parse-name" dup 0= IF  2drop  false  ELSE  true  THEN ;
: ?word-@nextarg ( -- addr u t / f )
    >in @ >r ?word-nextarg 0= IF  rdrop false  EXIT  THEN
    over c@ '@' = IF  rdrop 1 /string true  EXIT  THEN
    r> >in ! 2drop false ;

: word-args ( -- )
    ['] ?word-nextarg IS ?nextarg
    ['] ?word-@nextarg IS ?@nextarg ;

: arg-loop { xt -- }
    begin  ?nextarg  while  xt execute  repeat ;
: @arg-loop { xt -- }
    begin  ?@nextarg  while  xt execute  repeat ;

\ string

[IFUNDEF] safe/string
: safe/string ( c-addr u n -- c-addr' u' )
\G protect /string against overflows.
    dup negate >r  dup 0> IF
        /string dup r> u>= IF  + 0  THEN
    ELSE
        /string dup r> u< IF  + 1+ -1  THEN
    THEN ;
[THEN]

: -scan ( addr u char -- addr u' ) >r
    BEGIN  dup  WHILE  1- 2dup + c@ r@ =  UNTIL  1+  THEN  rdrop ;

\ logic memory modifiers

: or!   ( x addr -- )   >r r@ @ or   r> ! ;
: xor!  ( x addr -- )   >r r@ @ xor  r> ! ;
: and!  ( x addr -- )   >r r@ @ and  r> ! ;
: min!  ( n addr -- )   >r r@ @ min  r> ! ;
: max!  ( n addr -- )   >r r@ @ max  r> ! ;
: umin! ( n addr -- )   >r r@ @ umin r> ! ;
: umax! ( n addr -- )   >r r@ @ umax r> ! ;

: xorc! ( x c-addr -- )   >r r@ c@ xor  r> c! ;
: andc! ( x c-addr -- )   >r r@ c@ and  r> c! ;
: orc!  ( x c-addr -- )   >r r@ c@ or   r> c! ;

: max!@ ( n addr -- )   >r r@ @ max r> !@ ;
: umax!@ ( n addr -- )   >r r@ @ umax r> !@ ;

\ generic stack using string array primitives

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

: stack> ( stack -- x ) >r
    \G generic single-stack pop
    r@ $[]# dup 0<= !!stack-empty!!
    1- dup r@ $[] @ swap cells r> $!len ;
: >stack ( x stack -- )
    \G generic single-stack push
    dup $[]# swap $[] ! ;

: stack@ ( stack -- x1 .. xn n )
    \G fetch everything from the generic stack to the data stack
    $@ dup cell/ >r bounds ?DO  I @  cell +LOOP  r> ;
: stack! ( x1 .. xn n stack -- )
    \G set the generic stack with values from the data stack
    >r cells r@ $!len
    r> $@ bounds cell- swap cell- -DO  I !  cell -LOOP ;

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