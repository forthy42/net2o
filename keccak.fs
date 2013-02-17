\ This file has been generated using SWIG and fsi,
\ and is already platform dependent, search for the corresponding
\ fsi-file to compile it where no one has compiled it before ;)
\ GForth has its own dynamic loader and doesn't need addional C-Code.
\ That's why this file contains normal Gforth-code( version 0.6.9 or higher )
\ and could be used directly with include or require.
\ As all comments are stripped during the compilation, please
\ insert the copyright notice of the original file here.

\ ------===< functions >===-------
c-function KeccakInitialize KeccakInitialize  -- void
c-function KeccakF KeccakF a -- void
c-function KeccakInitializeState KeccakInitializeState a -- void
c-function KeccakExtract KeccakExtract a a n -- void
c-function KeccakAbsorb KeccakAbsorb a a n -- void
c-function KeccakEncrypt KeccakEncrypt a a n -- void
c-function KeccakDecrypt KeccakDecrypt a a n -- void
