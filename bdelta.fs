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

require net2o-tools.fs
require mini-oof2.fs

c-library bdelta
    \c #include <bdelta.h>
    s" bdelta" add-lib

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

Variable bfile1$
Variable bfile2$

: bslurp ( addr1 u1 addr2 u2 -- a b )
    bfile2$ $slurp-file  bfile1$ $slurp-file
    bfile1$ bfile2$ ;

: bdelta-init { a b -- o }
    a $@ 0  b $@ 0  bdelta_init_alg ;

: bd-pass { bs mins flags -- } ( o:b )
    o bs mins 0. flags bdelta_pass
    o BDELTA_REMOVE_OVERLAP bdelta_clean_matches ;

: bd-passes ( o:b -- )
    997 1994 0 bd-pass
    503 1006 0 bd-pass
    127  254 0 bd-pass
    031   62 0 bd-pass
    007   14 0 bd-pass
    005   10 0 bd-pass
    003    6 0 bd-pass
    013   26 BDELTA_GLOBAL bd-pass
    007   14 0 bd-pass
    005   10 0 bd-pass ;

10 buffer: p-tmp

: .p ( x64 -- )
    p-tmp p!+ p-tmp tuck - type ;
: .ps ( x64 -- )
    p-tmp ps!+ p-tmp tuck - type ;

: .diff ( b o:b -- ) { b }
    0 dup dup 64#0 64dup 64dup
    { p1' p2' fp 64^ p1 64^ p2 64^ numr }
    o bdelta_numMatches 0 ?DO
	o i p1 p2 numr bdelta_getMatch
	p2 64@ p2' n>64 64- 64dup .p 64>n >r
	b $@ fp /string r> umin dup >r type r> fp + to fp
	p1 64@ p1' n>64 64- .ps
	numr 64@ 64dup .p
	64dup 64>n fp + to fp
	64dup p1 64@ 64+ 64>n to p1'
	p2 64@ 64+ 64>n to p2'
    LOOP
    b $@ fp /string dup IF
	dup n>64 .p type
    ELSE  2drop  THEN ;

Variable bdelta$

: b$off ( -- )
    bfile1$ $off bfile2$ $off  bdelta$ $off ;

: bdelta$2 ( a$ b$ -- )
    tuck bdelta-init >o bd-passes .diff o bdelta_done_alg o> ;

: bdelta ( addr1 u1 addr2 u2 -- addr3 u3 ) bslurp
    ['] bdelta$2 bdelta$ $exec bdelta$ $@ ;

: bpatch$2 ( a$ diff$ -- )
    0 { fp }
    $@ bounds U+DO
	I p@+ >r 64>n r> swap 2dup <info> type +
	dup I' < IF
	    ps@+ >r 64>n fp + to fp
	    dup $@ fp safe/string
	    r> p@+ >r 64>n dup fp + to fp umin <warn> type r>
	THEN
    I - +LOOP <default> drop ;

: bpatch ( addr1 u1 addr2 u2 -- addr3 u3 )
    bslurp ['] bpatch$2 bdelta$ $exec
    bdelta$ $@ ;