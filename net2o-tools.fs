\ net2o tools

: ?nextarg ( -- addr u noarg-flag )
    argc @ 1 > IF  next-arg true  ELSE  false  THEN ;

[IFUNDEF] safe/string
: safe/string ( c-addr u n -- c-addr' u' )
\G protect /string against overflows.
    dup negate >r  dup 0> IF
        /string dup r> u>= IF  + 0  THEN
    ELSE
        /string dup r> u< IF  + 1+ -1  THEN
    THEN ;
[THEN]

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

: stack> ( stack -- x ) >r
    \g generic single-stack pop
    r@ $[]# dup 0<= !!object-empty!!
    1- dup r@ $[] @ swap cells r> $!len ;
: >stack ( x stack -- )
    \g generic single-stack push
    dup $[]# swap $[] ! ;

