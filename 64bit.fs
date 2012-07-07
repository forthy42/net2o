\ portable functions for 64 bit numbers


cell 8 = [IF]
    : 64bit ;
    : 64, drop , ;
    ' @ Alias 64@
    ' ! Alias 64!
    ' rot Alias 64rot
    ' -rot Alias -64rot
    ' dup Alias 64dup
    ' over Alias 64over
    ' drop Alias 64drop
    ' swap Alias 64swap
    ' + Alias 64+
    ' - Alias 64-
    ' or Alias 64or
    ' and Alias 64and
    ' xor Alias 64xor
    ' l@ Alias 32@
    ' Variable Alias 64Variable
    ' Constant Alias 64Constant
    ' 2/ Alias 64-2/
    ' negate Alias 64negate
    0 Constant 64#0
    -1 Constant 64#-1
    ' rshift Alias 64rshift
    ' lshift Alias 64lshift
    ' s>f Alias 64>f
    ' = Alias 64=
    -1 1 64rshift Constant max-int64
    ' . alias 64.
    ' noop Alias 64>n immediate
    ' noop Alias n>64 immediate
    ' noop Alias u>64 immediate
    ' s>d Alias 64>d
    ' >r Alias 64>r
    ' r> Alias 64r>
[ELSE]
    ' 2swap alias 64rot
    ' 2swap alias -64rot
    ' 2drop alias 64drop
    ' 2dup Alias 64dup
    ' 2over Alias 64over
    ' 2swap Alias 64swap
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
    ' 2Constant Alias 64Constant
    ' d2/ Alias 64-2/
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
    ' d= Alias 64=
    -1. 1 64rshift 64Constant max-int64
    ' d. alias 64.
    ' drop Alias 64>n
    ' noop Alias 64>d immediate
    ' s>d Alias n>64
    ' false Alias u>64
    ' 2>r Alias 64>r
    ' 2r> Alias 64r>
[THEN]
' dfloats Alias 64s
' dfloat+ Alias 64'+
' dfaligned Alias 64aligned
