\ This file has been generated using SWIG and fsi,
\ and is already platform dependent, search for the corresponding
\ fsi-file to compile it where no one has compiled it before ;)
\ Forth systems have their own own dynamic loader and don't need addional C-Code.
\ That's why this file will just print normal forth-code once compiled
\ and can be used directly with include or require.
\ As all comments are stripped during the compilation, please
\ insert the copyright notice of the original file here.

\ ----===< prefix >===-----
machine "amd64" str= [IF]
    cpu? avx512 [IF]
	c-library keccak_AVX512
	    s" keccak_AVX512" add-lib
    [ELSE]
    cpu? avx2 0 and [IF] \ AVX2 may or may not gain something, needs benchmarking
	c-library keccak_AVX2
	    s" keccak_AVX2" add-lib
	[ELSE]
	c-library keccak_x86_64
	    s" keccak_x86_64" add-lib
	[THEN]
    [THEN]
[ELSE]
    machine "arm64" str= [IF]
	c-library keccak_ARMv8A
	    s" keccak_ARMv8A" add-lib
    [ELSE]
	machine "arm" str= [IF]
	    c-library keccak_ARMv7A_NEON
		s" keccak_ARMv7A_NEON" add-lib
	[ELSE]
	    cell 8 = [IF]
		c-library keccak_64
		    s" keccak_64" add-lib
	    [ELSE]
		c-library keccak_32
		    s" keccak_32" add-lib
	    [THEN]
	[THEN]
    [THEN]
[THEN]
\c #include <KeccakP-1600-SnP.h>

\ ----===< int constants >===-----
#200	constant KeccakP1600_stateSizeInBytes
#64	constant KeccakP1600_stateAlignment

\ ------===< functions >===-------
c-function KeccakP1600_Initialize KeccakP1600_Initialize a -- void	( state -- )
c-function KeccakP1600_AddByte KeccakP1600_AddByte a u u -- void	( state data offset -- )
c-function KeccakP1600_AddBytes KeccakP1600_AddBytes a a u u -- void	( state data offset length -- )
c-function KeccakP1600_OverwriteBytes KeccakP1600_OverwriteBytes a a u u -- void	( state data offset length -- )
c-function KeccakP1600_OverwriteWithZeroes KeccakP1600_OverwriteWithZeroes a u -- void	( state byteCount -- )
c-function KeccakP1600_Permute_Nrounds KeccakP1600_Permute_Nrounds a u -- void	( state nrounds -- )
c-function KeccakP1600_Permute_12rounds KeccakP1600_Permute_12rounds a -- void	( state -- )
c-function KeccakP1600_Permute_24rounds KeccakP1600_Permute_24rounds a -- void	( state -- )
c-function KeccakP1600_ExtractBytes KeccakP1600_ExtractBytes a a u u -- void	( state data offset length -- )
c-function KeccakP1600_ExtractAndAddBytes KeccakP1600_ExtractAndAddBytes a a a u u -- void	( state input output offset length -- )

\ ----===< postfix >===-----
end-c-library
