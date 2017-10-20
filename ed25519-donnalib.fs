\ Interface to the ed25519 primitives from donna     23oct2013py

\ Copyright (C) 2013-2015   Bernd Paysan

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

\c #include <stdint.h>
\c #include <ed25519-prims.h>
\c int str32eq(long* a, long* b) {
\c   long diff=0;
\c   switch(sizeof(long)) {
\c     case 4:
\c       diff|=((a[4]^b[4])|(a[5]^b[5])|(a[6]^b[6])|(a[7]^b[7]));
\c     case 8:
\c       diff|=((a[0]^b[0])|(a[1]^b[1])|(a[2]^b[2])|(a[3]^b[3]));
\c   }
\c   return -(diff==0);
\c }

c-function raw>sc25519 expand_raw256_modm a a -- void ( sc char[32] -- )
c-function nb>sc25519 expand256_modm a a n -- void ( sc char[64] n -- )
c-function sc25519>32b contract256_modm a a -- void ( char[32] sc -- )
c-function sc25519* mul256_modm a a a -- void ( r x y -- )
c-function sc25519+ add256_modm a a a -- void ( r x y -- )

c-function ge25519*base ge25519_scalarmult_base a a -- void ( ger x -- )
c-function ge25519-pack ge25519_pack a a -- void ( r ger -- )
c-function ge25519+ ge25519_add a a a -- void ( a a a -- )
c-function ge25519-unpack- ge25519_unpack_negative_vartime a a -- n ( r p -- flag )
c-function ge25519*+ ge25519_double_scalarmult_vartime a a a a -- void ( r p s1 s2 -- )
c-function ge25519*v ge25519_scalarmult_vartime a a a -- void ( r p s -- )
c-function ge25519* ge25519_scalarmult a a a -- void ( r p s -- )
c-function 32b= str32eq a a -- n ( addr1 addr2 -- flag )
c-variable ge25519-basepoint ge25519_basepoint ( --  addr )
\ c-variable ge25519-niels*[] ge25519_niels_sliding_multiples ( -- addr )
