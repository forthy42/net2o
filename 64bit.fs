\ portable functions for 64 bit numbers

: min!  ( n addr -- )   >r r@ @ min  r> ! ;
: max!  ( n addr -- )   >r r@ @ max  r> ! ;
: umin! ( n addr -- )   >r r@ @ umin r> ! ;
: umax! ( n addr -- )   >r r@ @ umax r> ! ;

1 pad ! pad c@ negate constant le?

cell 8 = [IF]
    : 64bit ;
    ' @ Alias 64@
    ' ! Alias 64!
    ' le-ux@ Alias le-64@
    ' le-x! Alias le-64!
    ' be-ux@ Alias be-64@
    ' be-x! Alias be-64!
    ' noop Alias 64><
    ' swap alias n64-swap
    ' swap alias 64n-swap
    ' dup Alias 64dup
    ' over Alias 64over
    ' drop Alias 64drop
    ' nip Alias 64nip
    ' swap Alias 64swap
    ' tuck Alias 64tuck
    ' + Alias 64+
    ' - Alias 64-
    ' or Alias 64or
    ' and Alias 64and
    ' xor Alias 64xor
    ' Variable Alias 64Variable
    ' User Alias 64User
    ' Constant Alias 64Constant
    ' Value Alias 64Value
    ' 2/ Alias 64-2/
    ' 2* Alias 64-2*
    ' negate Alias 64negate
    ' invert Alias 64invert
    0 Constant 64#0
    1 Constant 64#1
    -1 Constant 64#-1
    ' literal Alias 64literal immediate
    ' rshift Alias 64rshift
    ' lshift Alias 64lshift
    ' rol Alias 64rol
    ' ror Alias 64ror
    ' s>f Alias 64>f
    ' f>s Alias f>64
    ' = Alias 64=
    ' <> Alias 64<>
    -1 1 64rshift Constant max-int64
    ' u. alias u64.
    ' . alias s64.
    ' noop Alias 64>n immediate
    ' noop Alias n>64 immediate
    ' noop Alias u>64 immediate
    ' s>d Alias 64>d
    ' drop Alias d>64
    ' >r Alias 64>r
    ' r> Alias 64r>
    ' 0= Alias 64-0=
    ' 0<> Alias 64-0<>
    ' 0>= Alias 64-0>=
    ' 0<= Alias 64-0<=
    ' 0< Alias 64-0<
    ' < Alias 64<
    ' > Alias 64>
    ' u< Alias 64u<
    ' u> Alias 64u>
    ' u<= Alias 64u<=
    ' u>= Alias 64u>=
    ' on Alias 64on
    ' +! Alias 64+!
    ' min Alias 64min
    ' max Alias 64max
    ' umin Alias 64umin
    ' umax Alias 64umax
    ' abs Alias 64abs
    ' off Alias 64off
    ' */ Alias 64*/
    ' * Alias 64*
    : 128@ ( addr -- d ) 2@ swap ;
    : 128! ( d addr -- ) >r swap r> 2! ;
    ' d+ Alias 128+ \ 128 bit addition
    ' d- Alias 128- \ 128 bit addition
    ' stop-ns alias stop-64ns
    also locals-types definitions
    ' w: alias 64:
    ' w^ alias 64^
    previous definitions
    ' min! Alias 64min!
    ' max! Alias 64max!
    ' umin! Alias 64umin!
    ' umax! Alias 64umax!
    ' !@ Alias 64!@
    ' be-ux@ Alias be@
    ' be-x! Alias be!
[ELSE]
    ' rot alias n64-swap
    ' -rot alias 64n-swap
    ' 2drop alias 64drop
    ' 2nip alias 64nip
    ' 2dup Alias 64dup
    ' 2over Alias 64over
    ' 2swap Alias 64swap
    ' 2tuck Alias 64tuck
    ' swap Alias 64><
    : 64@  2@ 64>< ; [IFDEF] macro macro [THEN]
    : 64!  >r 64>< r> 2! ; [IFDEF] macro macro [THEN]
    ' le-uxd@ Alias le-64@
    ' le-xd! Alias le-64!
    ' be-uxd@ Alias be-64@
    ' be-xd! Alias be-64!
    ' d+ Alias 64+
    ' d- Alias 64-
    : 64or rot or >r or r> ;
    : 64and rot and >r and r> ;
    : 64xor rot xor >r xor r> ;
    ' 2Variable Alias 64Variable
    : 64User  User cell uallot drop ;
    ' 2Constant Alias 64Constant
    ' 2Value Alias 64Value
    ' d2/ Alias 64-2/
    ' d2* Alias 64-2*
    ' dnegate Alias 64negate
    : 64invert invert swap invert swap ;
    #0. 2Constant 64#0
    #1. 2Constant 64#1
    #-1. 2Constant 64#-1
    ' 2literal Alias 64literal immediate
    ' dlshift Alias 64lshift
    ' drshift Alias 64rshift
    ' drol Alias 64rol
    ' dror Alias 64ror
    ' d>f Alias 64>f
    ' f>d Alias f>64
    ' d= Alias 64=
    ' d<> Alias 64<>
    #-1. 1 64rshift 64Constant max-int64
    ' ud. alias u64.
    ' d. alias s64.
    ' drop Alias 64>n
    ' noop Alias 64>d immediate
    ' noop Alias d>64 immediate
    ' s>d Alias n>64
    ' false Alias u>64
    ' 2>r Alias 64>r
    ' 2r> Alias 64r>
    ' d0= Alias 64-0=
    ' d0<> Alias 64-0<>
    ' d0>= Alias 64-0>=
    ' d0<= Alias 64-0<=
    ' d0< Alias 64-0<
    ' d< Alias 64<
    ' d> Alias 64>
    ' du< Alias 64u<
    ' du> Alias 64u>
    ' du<= Alias 64u<=
    ' du>= Alias 64u>=
    : 64on ( addr -- )  >r 64#-1 r> 64! ;
    : 64+!  ( 64n addr -- )  dup >r 64@ 64+ r> 64! ;
    ' dmin Alias 64min
    ' dmax Alias 64max
    : 64umin  2over 2over du> IF  2swap  THEN  2drop ;
    : 64umax  2over 2over du< IF  2swap  THEN  2drop ;
    ' dabs Alias 64abs
    : 64off #0. rot 64! ;
    ' m*/ Alias 64*/
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
    ' stop-dns alias stop-64ns
    : compile-pushlocal-64 ( a-addr -- ) ( run-time: w1 w2 -- )
	locals-size @ alignlp-w cell+ cell+ dup locals-size !
	swap !
	postpone >l postpone >l ;
    also locals-types definitions
    ' d: alias 64:
    : 64^ ( "name" -- a-addr xt ) \ net2o 64-caret
	create-local
	['] compile-pushlocal-64
      does> ( Compilation: -- ) ( Run-time: -- w )
	postpone laddr# @ lp-offset, ;
    previous definitions
    : dumin ( ud1 ud2 -- ud3 )  2over 2over du> IF  2swap  THEN  2drop ;
    : dumax ( ud1 ud2 -- ud3 )  2over 2over du< IF  2swap  THEN  2drop ;
    : 64!@ ( value addr -- old-value )   >r r@ 64@ 64swap r> 64! ;
    : 64min! ( d addr -- )  >r r@ 64@ dmin r> 64! ;
    : 64max! ( d addr -- )  >r r@ 64@ dmax r> 64! ;
    : 64umin! ( n addr -- )   >r r@ 64@ dumin r> 64! ;
    : 64umax! ( n addr -- )   >r r@ 64@ dumin r> 64! ;
    ' be-ul@ alias be@
    ' be-l! alias be!
    : 128+ ( 128a 128b -- 128c ) \ 128 bit addition
	{ d: a1 d: a2 d: b1 d: b2 }
	a1 b1 d+ a2 b2 d+ 2over a1 du< s>d d- ;
    : 128- ( 128a 128b -- 128c ) \ 128 bit addition
	{ d: a1 d: a2 d: b1 d: b2 }
	a1 b1 d- a2 b2 d- 2over a1 du> s>d d+ ;
[THEN]
\ independent of cell size, using dfloats:
' dfloats Alias 64s
' dfloat+ Alias 64'+
' dfaligned Alias 64aligned
' dffield: Alias 64field:
: x64. ( 64n -- ) ['] u64. $10 base-execute ;
: le-128@ ( addr -- d )
    dup >r le-64@ r> 64'+ le-64@ ;
: le-128! ( d addr -- )
    dup >r 64'+ le-64! r> le-64! ;
: be-128@ ( addr -- d )
    dup >r 64'+ be-64@ r> be-64@ ;
: be-128! ( d addr -- )
    dup >r be-64! r> 64'+ be-64! ;
Create 64!-table ' 64! , ' 64+! ,
1 64s ' 64aligned ' 64@ 64!-table wrap+value: 64value: ( u1 "name" -- u2 )
