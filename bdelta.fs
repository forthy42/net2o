\ net2o template for new files

\ Copyright (C) 2016   Bernd Paysan

\ This program is free software: you can redistribute it and/or modify
\ it under the terms of the GNU Affero General Public License as published by
\ the Free Software Foundation, either version 3 of the License, or
\ (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU Affero General Public License for more details.

\ You should have received a copy of the GNU Affero General Public License
\ along with this program.  If not, see <http://www.gnu.org/licenses/>.

\ This file has been partly generated using SWIG and fsi,
\ and is already platform dependent, search for the corresponding
\ fsi-file to compile it where no one has compiled it before ;)
\ GForth has its own dynamic loader and doesn't need addional C-Code.
\ That's why this file contains normal Gforth-code( version 0.6.9 or higher )
\ and could be used directly with include or require.
\ As all comments are stripped during the compilation, please
\ insert the copyright notice of the original file here.

c-library bdelta

\ ----===< int constants ===>-----
1	constant BDELTA_GLOBAL
2	constant BDELTA_SIDES_ORDERED
1	constant BDELTA_REMOVE_OVERLAP

\ --------===< enums >===---------
0	constant BDELTA_OK
-1	constant BDELTA_MEM_ERROR
-2	constant BDELTA_READ_ERROR

\ ------===< functions >===-------
c-function bdelta_init_alg bdelta_init_alg a d a d -- a
c-function bdelta_done_alg bdelta_done_alg a -- void
c-function bdelta_pass bdelta_pass a u u d u -- void
c-function bdelta_swap_inputs bdelta_swap_inputs a -- void
c-function bdelta_clean_matches bdelta_clean_matches a u -- void
c-function bdelta_numMatches bdelta_numMatches a -- u
c-function bdelta_getMatch bdelta_getMatch a u a a a -- void
c-function bdelta_getError bdelta_getError a -- n
c-function bdelta_showMatches bdelta_showMatches a -- void

end-c-library