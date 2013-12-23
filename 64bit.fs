\ portable functions for 64 bit numbers

cell 8 = [IF]
    : 64bit ;
    : 64, drop , ;
    ' @ Alias 64@
    ' ! Alias 64!
    ' rot Alias 64rot
    ' -rot Alias -64rot
    ' swap alias n64-swap
    ' swap alias 64n-swap
    ' dup Alias 64dup
    ' over Alias 64over
    ' drop Alias 64drop
    ' nip Alias 64nip
    ' swap Alias 64swap
    ' over Alias over64 ( n 64 -- n 64 n )
    ' + Alias 64+
    ' - Alias 64-
    ' or Alias 64or
    ' and Alias 64and
    ' xor Alias 64xor
    ' l@ Alias 32@
    ' Variable Alias 64Variable
    ' User Alias 64User
    ' Constant Alias 64Constant
    ' Value Alias 64Value
    ' 2/ Alias 64-2/
    ' 2* Alias 64-2*
    ' negate Alias 64negate
    0 Constant 64#0
    -1 Constant 64#-1
    ' rshift Alias 64rshift
    ' lshift Alias 64lshift
    ' s>f Alias 64>f
    ' f>s Alias f>64
    ' = Alias 64=
    ' <> Alias 64<>
    -1 1 64rshift Constant max-int64
    ' u. alias 64.
    ' . alias s64.
    ' noop Alias 64>n immediate
    ' noop Alias n>64 immediate
    ' noop Alias u>64 immediate
    ' s>d Alias 64>d
    ' drop Alias d>64
    ' >r Alias 64>r
    ' r@ Alias 64r@
    ' r> Alias 64r>
    ' 0= Alias 64-0=
    ' 0<> Alias 64-0<>
    ' 0>= Alias 64-0>=
    ' 0<= Alias 64-0<=
    ' 0< Alias 64-0<
    ' < Alias 64<
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
    : 128xor ( ud1 ud2 -- ud3 )  rot xor >r xor r> ;
    : 128@ ( addr -- d ) 2@ swap ;
    ' d= Alias 128= ( d1 d2 -- flag )
    : 128! ( d addr -- ) >r swap r> 2! ;
    also locals-types definitions
    ' w: alias 64:
    previous definitions
[ELSE]
    ' 2swap alias 64rot
    ' 2swap alias -64rot
    ' rot alias n64-swap
    ' -rot alias 64n-swap
    ' 2drop alias 64drop
    ' 2nip alias 64nip
    ' 2dup Alias 64dup
    ' 2over Alias 64over
    ' 2swap Alias 64swap
    : over64 ( n 64 -- n 64 n ) 2 pick ;
    : 64,  swap 2, ;
    : 64@  2@ swap ; [IFDEF] macro macro [THEN]
    : 64!  >r swap r> 2! ; [IFDEF] macro macro [THEN]
    ' d+ Alias 64+
    ' d- Alias 64-
    : 64or rot or >r or r> ;
    : 64and rot and >r and r> ;
    : 64xor rot xor >r xor r> ;
    ' @ Alias 32@
    ' 2Variable Alias 64Variable
    : 64User  User cell uallot drop ;
    ' 2Constant Alias 64Constant
    ' 2Value Alias 64Value
    ' d2/ Alias 64-2/
    ' d2* Alias 64-2*
    ' dnegate Alias 64negate
    0. 2Constant 64#0
    -1. 2Constant 64#-1
    : 64lshift ( u64 u -- u64' )  >r
	r@ lshift over 8 cells r@ - rshift or
	swap r> lshift swap ;
    : 64rshift ( u64 u -- u64' )  >r swap
	r@ rshift over 8 cells r@ - lshift or
	swap r> rshift ;
    ' d>f Alias 64>f
    ' f>d Alias f>64
    ' d= Alias 64=
    ' d<> Alias 64<>
    -1. 1 64rshift 64Constant max-int64
    ' ud. alias 64.
    ' d. alias s64.
    ' drop Alias 64>n
    ' noop Alias 64>d immediate
    ' noop Alias d>64 immediate
    ' s>d Alias n>64
    ' false Alias u>64
    ' 2>r Alias 64>r
    ' 2r> Alias 64r>
    ' 2r@ Alias 64r@
    ' d0= Alias 64-0=
    ' d0<> Alias 64-0<>
    ' d0>= Alias 64-0>=
    ' d0<= Alias 64-0<=
    ' d0< Alias 64-0<
    ' d< Alias 64<
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
    : 64off 0. rot 64! ;
    ' m*/ Alias 64*/
    : 128xor { x1 x2 x3 x4 y1 y2 y3 y4 -- z1 z2 z3 z4 }
	x1 y1 xor  x2 y2 xor  x3 y3 xor  x4 y4 xor ;
    : 128@ ( addr -- x1..x4 )
	>r
	r@ 3 cells + @
	r@ 2 cells + @
	r@ cell+ @
	r> @ ;
    : 128= ( x1..y4 y1..y4 -- flag )  128xor  or or or 0= ;
    : 128! ( x1..x4 addr -- )
	>r
	r@ !
	r@ cell+ !
	r@ 2 cells + !
	r> 3 cells + ! ;
    also locals-types definitions
    ' d: alias 64:
    previous definitions
[THEN]
\ independent of cell size, using dfloats:
' dfloats Alias 64s
' dfloat+ Alias 64'+
' dfaligned Alias 64aligned
' dffield: Alias 64field:
