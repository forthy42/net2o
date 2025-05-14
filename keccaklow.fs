\ This file has been generated using SWIG and fsi,
\ and is already platform dependent, search for the corresponding
\ fsi-file to compile it where no one has compiled it before ;)
\ Forth systems have their own own dynamic loader and don't need addional C-Code.
\ That's why this file will just print normal forth-code once compiled
\ and can be used directly with include or require.
\ As all comments are stripped during the compilation, please
\ insert the copyright notice of the original file here.

\ ----===< prefix >===-----
cell 8 = [IF] "64" [ELSE] "32" [THEN]
machine "amd64" str= [IF]
    cpu? avx512dq cpu? svm 0= and [IF] \ avx512dq on Intel only if available
	2drop "AVX512"
    [ELSE]
	cpu? avx2 cpu? svm 0= cpu? avx512dq or and [IF]
	    \ AVX2 only on Intel, except if avx512dq is available, then on AMD, too
	    2drop "AVX2"
	[ELSE]
	    2drop "x86_64"
	[THEN]
    [THEN]
[ELSE]
    machine "386" str= [IF]
	cpu? xop [IF]
	    2drop "XOP"
	[ELSE]
	    cpu? ssse3 [IF]
		2drop "ssse3"
	    [THEN]
	[THEN]
    [ELSE]
	machine "arm64" str= 0 and [IF] \ GCC compiled generic code is better
	    2drop "ARMv8A"
	[ELSE]
	    machine "arm" str= [IF]
		cpu? neon cpu? asimd or [IF]
		    2drop "ARMv7A_NEON"
		[THEN]
	    [THEN]
	[THEN]
    [THEN]
[THEN]
"mode" replaces
"c-library keccak_%mode% \"keccak_%mode%\" add-lib" $substitute 0 min throw evaluate
\c #include <stdint.h>
\c #include <KeccakP-1600-SnP.h>

\ ----===< int constants >===-----
#200	constant KeccakP1600_stateSizeInBytes
#64	constant KeccakP1600_stateAlignment

\ ------===< functions >===-------
c-function KeccakP1600_Initialize KeccakP1600_Initialize a -- void	( state -- )
c-function KeccakP1600_AddBytes KeccakP1600_AddBytes a a u u -- void	( state data offset length -- )
c-function KeccakP1600_OverwriteBytes KeccakP1600_OverwriteBytes a a u u -- void	( state data offset length -- )
c-function KeccakP1600_OverwriteWithZeroes KeccakP1600_OverwriteWithZeroes a u -- void	( state byteCount -- )
c-function KeccakP1600_Permute_Nrounds KeccakP1600_Permute_Nrounds a u -- void	( state nrounds -- )
c-function KeccakP1600_Permute_12rounds KeccakP1600_Permute_12rounds a -- void	( state -- )
c-function KeccakP1600_Permute_24rounds KeccakP1600_Permute_24rounds a -- void	( state -- )
c-function KeccakP1600_ExtractBytes KeccakP1600_ExtractBytes a a u u -- void	( state data offset length -- )
c-function KeccakP1600_ExtractAndAddBytes KeccakP1600_ExtractAndAddBytes a a a u u -- void	( state input output offset length -- )
\c static inline void xor_lanes(unsigned long long* addr1, unsigned long long* addr2, Cell u) {
\c   while(u >= 0x40) {
\c     addr2[0] ^= addr1[0];
\c     addr2[1] ^= addr1[1];
\c     addr2[2] ^= addr1[2];
\c     addr2[3] ^= addr1[3];
\c     addr2[4] ^= addr1[4];
\c     addr2[5] ^= addr1[5];
\c     addr2[6] ^= addr1[6];
\c     addr2[7] ^= addr1[7];
\c     u -= 0x40;
\c     addr1 += 8;
\c     addr2 += 8;
\c   }
\c   if(u & 0x20) {
\c     addr2[0] ^= addr1[0];
\c     addr2[1] ^= addr1[1];
\c     addr2[2] ^= addr1[2];
\c     addr2[3] ^= addr1[3];
\c     addr1 += 4;
\c     addr2 += 4;
\c   }
\c   if(u & 0x10) {
\c     addr2[0] ^= addr1[0];
\c     addr2[1] ^= addr1[1];
\c     addr1 += 2;
\c     addr2 += 2;
\c   }
\c   if(u & 0x8) {
\c     *addr2++ ^= *addr1++;
\c   }
\c   if(u & 0x4) {
\c     *(unsigned int *)addr2 ^= *(unsigned int *)addr1;
\c     addr1 = (unsigned long long*)(((intptr_t)addr1)+4);
\c     addr2 = (unsigned long long*)(((intptr_t)addr2)+4);
\c   }
\c   if(u & 0x2) {
\c     *(unsigned short *)addr2 ^= *(unsigned short *)addr1;
\c     addr1 = (unsigned long long*)(((intptr_t)addr1)+2);
\c     addr2 = (unsigned long long*)(((intptr_t)addr2)+2);
\c   }
\c   if(u & 0x1) {
\c     *(unsigned char *)addr2 ^= *(unsigned char *)addr1;
\c     addr1 = (unsigned long long*)(((intptr_t)addr1)+1);
\c     addr2 = (unsigned long long*)(((intptr_t)addr2)+1);
\c   }
\c }
c-function xor_lanes xor_lanes a a u -- void ( from to count -- )

\ ----===< postfix >===-----
end-c-library
