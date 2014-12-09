\ This file has been generated using SWIG and fsi,
\ and is already platform dependent, search for the corresponding
\ fsi-file to compile it where no one has compiled it before ;)
\ GForth has its own dynamic loader and doesn't need addional C-Code.
\ That's why this file contains normal Gforth-code( version 0.6.9 or higher )
\ and could be used directly with include or require.
\ As all comments are stripped during the compilation, please
\ insert the copyright notice of the original file here.

\ ----===< int constants ===>-----
1	constant _THREEFISH_H_

\ -------===< structs >===--------
\ tf_ctx
begin-structure tf_ctx
	drop 0 72 +field tf_ctx-key
	drop 72 24 +field tf_ctx-tweak
drop 96 end-structure

\ ------===< functions >===-------
c-function tf_init tf_init a -- void
c-function tf_prep tf_prep a -- void
c-function tf_encrypt tf_encrypt a a a n -- void
c-function tf_decrypt tf_decrypt a a a -- void
c-function rot_l64 rot_l64 d n -- d
c-function rot_r64 rot_r64 d n -- d
