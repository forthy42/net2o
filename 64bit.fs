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
    ' noop Alias u>64 immediate
    ' dup Alias 64dup
    ' over Alias 64over
    0 Constant 64#0
[ELSE]
    ' 2swap alias 64swap
    ' 2swap alias -64swap
    : 64,  swap 2, ;
    : 64@  2@ swap ; [IFDEF] macro macro [THEN]
    : 64!  >r swap r> 2! ; [IFDEF] macro macro [THEN]
    : 64+  d+ ;
    : 64-  d- ;
    : 64or rot or >r or r> ;
    : 64and rot and >r and r> ;
    : 64xor rot xor >r xor r> ;
    ' @ Alias 32@
    ' 2Variable Alias 64Variable
    ' false Alias u>64
    ' 2dup Alias 64dup
    ' 2over Alias 64over
    0. 2Constant 64#0
[THEN]
' dfloats Alias 64s
' dfloat+ Alias 64'+
' dfaligned Alias 64aligned
