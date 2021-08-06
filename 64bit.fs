\ portable functions for 64 bit numbers

: min!  ( n addr -- )   >r r@ @ min  r> ! ;
: max!  ( n addr -- )   >r r@ @ max  r> ! ;
: umin! ( n addr -- )   >r r@ @ umin r> ! ;
: umax! ( n addr -- )   >r r@ @ umax r> ! ;

1 pad ! pad c@ negate constant le?

cell 8 = [IF]
    : 64bit ;
    synonym 64@ @
    synonym 64! !
    synonym le-64@ le-ux@
    synonym le-64! le-x!
    synonym be-64@ be-ux@
    synonym be-64! be-x!
    synonym 64>< noop
    synonym n64-swap swap
    synonym 64n-swap swap
    synonym 64dup dup
    synonym 64over over
    synonym 64drop drop
    synonym 64nip nip
    synonym 64swap swap
    synonym 64tuck tuck
    synonym 64+ +
    synonym 64- -
    synonym 64or or
    synonym 64and and
    synonym 64xor xor
    synonym 64Variable Variable
    synonym 64User User
    synonym 64Constant Constant
    synonym 64Value Value
    synonym 64-2/ 2/
    synonym 64-2* 2*
    synonym 64negate negate
    synonym 64invert invert
    0 Constant 64#0
    1 Constant 64#1
    -1 Constant 64#-1
    synonym 64literal literal
    synonym 64rshift rshift
    synonym 64lshift lshift
    synonym 64rol rol
    synonym 64ror ror
    synonym 64>f s>f
    synonym f>64 f>s
    synonym 64= =
    synonym 64<> <>
    -1 1 64rshift Constant max-int64
    synonym u64. u.
    synonym s64. .
    synonym 64>n noop immediate
    synonym n>64 noop immediate
    synonym u>64 noop immediate
    synonym 64>d s>d
    synonym d>64 drop
    synonym 64>r >r
    synonym 64r> r>
    synonym 64-0= 0=
    synonym 64-0<> 0<>
    synonym 64-0>= 0>=
    synonym 64-0<= 0<=
    synonym 64-0< 0<
    synonym 64< <
    synonym 64> >
    synonym 64u< u<
    synonym 64u> u>
    synonym 64u<= u<=
    synonym 64u>= u>=
    synonym 64on on
    synonym 64+! +!
    synonym 64min min
    synonym 64max max
    synonym 64umin umin
    synonym 64umax umax
    synonym 64abs abs
    synonym 64off off
    synonym 64*/ */
    synonym 64* *
    : 128@ ( addr -- d ) 2@ swap ;
    : 128! ( d addr -- ) >r swap r> 2! ;
    synonym 128+ d+ \ 128 bit addition
    synonym 128- d- \ 128 bit addition
    synonym stop-64ns stop-ns
    also locals-types definitions
    synonym 64: w:
    synonym 64^ w^
    previous definitions
    synonym 64min! min!
    synonym 64max! max!
    synonym 64umin! umin!
    synonym 64umax! umax!
    synonym 64!@ !@
    synonym be@ be-ux@
    synonym be! be-x!
[ELSE]
    synonym n64-swap rot
    synonym 64n-swap -rot
    synonym 64drop 2drop
    synonym 64nip 2nip
    synonym 64dup 2dup
    synonym 64over 2over
    synonym 64swap 2swap
    synonym 64tuck 2tuck
    synonym 64>< swap
    : 64@  2@ 64>< ; [IFDEF] macro macro [THEN]
    : 64!  >r 64>< r> 2! ; [IFDEF] macro macro [THEN]
    synonym le-64@ le-uxd@
    synonym le-64! le-xd!
    synonym be-64@ be-uxd@
    synonym be-64! be-xd!
    synonym 64+ d+
    synonym 64- d-
    : 64or rot or >r or r> ;
    : 64and rot and >r and r> ;
    : 64xor rot xor >r xor r> ;
    synonym 64Variable 2Variable
    : 64User  User cell uallot drop ;
    synonym 64Constant 2Constant
    synonym 64Value 2Value
    synonym 64-2/ d2/
    synonym 64-2* d2*
    synonym 64negate dnegate
    : 64invert invert swap invert swap ;
    #0. 2Constant 64#0
    #1. 2Constant 64#1
    #-1. 2Constant 64#-1
    synonym 64literal 2literal
    synonym 64lshift dlshift
    synonym 64rshift drshift
    synonym 64rol drol
    synonym 64ror dror
    synonym 64>f d>f
    synonym f>64 f>d
    synonym 64= d=
    synonym 64<> d<>
    #-1. 1 64rshift 64Constant max-int64
    synonym u64. ud.
    synonym s64. d.
    synonym 64>n drop
    synonym 64>d noop immediate
    synonym d>64 noop immediate
    synonym n>64 s>d
    synonym u>64 false
    synonym 64>r 2>r
    synonym 64r> 2r>
    synonym 64-0= d0=
    synonym 64-0<> d0<>
    synonym 64-0>= d0>=
    synonym 64-0<= d0<=
    synonym 64-0< d0<
    synonym 64< d<
    synonym 64> d>
    synonym 64u< du<
    synonym 64u> du>
    synonym 64u<= du<=
    synonym 64u>= du>=
    : 64on ( addr -- )  >r 64#-1 r> 64! ;
    : 64+!  ( 64n addr -- )  dup >r 64@ 64+ r> 64! ;
    synonym 64min dmin
    synonym 64max dmax
    : 64umin  2over 2over du> IF  2swap  THEN  2drop ;
    : 64umax  2over 2over du< IF  2swap  THEN  2drop ;
    synonym 64abs dabs
    : 64off #0. rot 64! ;
    synonym 64*/ m*/
    : 64* ( d1 d2 -- d3 ) { l1 h1 l2 h2 }
	l1 l2 um* l1 h2 um* l2 h1 um* d+ drop + ;
    : 128@ ( addr -- x1..x4 )
	>r
	r@ @
	r@ cell+ @
	r@ 2 cells + @
	r> 3 cells + @ ;
    : 128! ( x1..x4 addr -- )
	>r
	r@ 3 cells + !
	r@ 2 cells + !
	r@ cell+ !
	r> ! ;
    synonym stop-64ns stop-dns
    : compile-pushlocal-64 ( a-addr -- ) ( run-time: w1 w2 -- )
	locals-size @ alignlp-w cell+ cell+ dup locals-size !
	swap !
	postpone >l postpone >l ;
    also locals-types definitions
    synonym 64: d:
    : 64^ ( "name" -- a-addr xt ) \ net2o 64-caret
	W^ drop ['] compile-pushlocal-64 ;
    previous definitions
    : dumin ( ud1 ud2 -- ud3 )  2over 2over du> IF  2swap  THEN  2drop ;
    : dumax ( ud1 ud2 -- ud3 )  2over 2over du< IF  2swap  THEN  2drop ;
    : 64!@ ( value addr -- old-value )   >r r@ 64@ 64swap r> 64! ;
    : 64min! ( d addr -- )  >r r@ 64@ dmin r> 64! ;
    : 64max! ( d addr -- )  >r r@ 64@ dmax r> 64! ;
    : 64umin! ( n addr -- )   >r r@ 64@ dumin r> 64! ;
    : 64umax! ( n addr -- )   >r r@ 64@ dumin r> 64! ;
    synonym be@ be-ul@
    synonym be! be-l!
    : 128+ ( 128a 128b -- 128c ) \ 128 bit addition
	{ d: a1 d: a2 d: b1 d: b2 }
	a1 b1 d+ a2 b2 d+ 2over a1 du< s>d d- ;
    : 128- ( 128a 128b -- 128c ) \ 128 bit addition
	{ d: a1 d: a2 d: b1 d: b2 }
	a1 b1 d- a2 b2 d- 2over a1 du> s>d d+ ;
[THEN]
\ independent of cell size, using dfloats:
synonym 64s dfloats
synonym 64'+ dfloat+
synonym 64aligned dfaligned
synonym 64field: dffield:
: x64. ( 64n -- ) ['] u64. $10 base-execute ;
: le-128@ ( addr -- d )
    dup >r le-64@ r> 64'+ le-64@ ;
: le-128! ( d addr -- )
    dup >r 64'+ le-64! r> le-64! ;
: be-128@ ( addr -- d )
    dup >r 64'+ be-64@ r> be-64@ ;
: be-128! ( d addr -- )
    dup >r be-64! r> 64'+ be-64! ;
: 64>128 ( 64 -- 128 ) 64dup 64-0< n>64 ;
Create 64!-table ' 64! , ' 64+! ,
1 64s ' 64aligned ' 64@ 64!-table wrap+value: 64value: ( u1 "name" -- u2 )
