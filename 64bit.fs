\ portable functions for 64 bit numbers


cell 8 = [IF]
    : 64bit ;
    : 64, drop , ;
    ' @ Alias 64@
    ' ! Alias 64!
    ' rot Alias 64swap
    ' -rot Alias -64swap
    ' + Alias 64+
    ' - Alias 64-
    ' or Alias 64or
    ' and Alias 64and
    ' xor Alias 64xor
    ' l@ Alias 32@
    ' Variable Alias 64Variable
    ' Constant Alias 64Constant
    ' noop Alias u>64 immediate
    ' dup Alias 64dup
    ' over Alias 64over
    ' 2/ Alias 64-2/
    ' negate Alias 64negate
    0 Constant 64#0
    -1 Constant 64#-1
    ' rshift Alias 64rshift
    ' lshift Alias 64lshift
    -1 1 64rshift Constant max-int64
[ELSE]
    ' 2swap alias 64swap
    ' 2swap alias -64swap
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
    ' false Alias u>64
    ' 2dup Alias 64dup
    ' 2over Alias 64over
    ' d2/ Alias 64-2/
    ' dnegate Alias 64negate
    0. 2Constant 64#0
    1-. 2Constant 64#-1
    : 64lshift ( u64 u -- u64' )  >r
	r@ lshift over 8 cells r@ - rshift or
	swap r> lshift swap ;
    : 64rshift ( u64 u -- u64' )  >r swap
	r@ rshift over 8 cells r@ - lshift or
	swap r> rshift ;
    -1. 1 64rshift 64Constant max-int64
[THEN]
' dfloats Alias 64s
' dfloat+ Alias 64'+
' dfaligned Alias 64aligned
