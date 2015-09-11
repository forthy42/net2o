\ keccak wrapper

\ Copyright (C) 2012-2015   Bernd Paysan

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

\c #include <KeccakF-1600.h>
\c UINT64* KeccakEncryptLoop(keccak_state state, UINT64 * data, int n, int rounds)
\c {
    \c   while(n>0) {
\c     unsigned int p = n >= 128 ? 128 : n;
\c     KeccakF(state, rounds);
\c     KeccakEncrypt(state, data, p);
\c     data = (UINT64*)(((char*)data)+p); n-=p;
\c   }
\c   return data;
\c }
\c UINT64* KeccakDecryptLoop(keccak_state state, UINT64 * data, int n, int rounds)
\c {
\c   while(n>0) {
\c     unsigned int p = n >= 128 ? 128 : n;
\c     KeccakF(state, rounds);
\c     KeccakDecrypt(state, data, p);
\c     data = (UINT64*)(((char*)data)+p); n-=p;
\c   }
\c   return data;
\c }

\ ------===< functions >===-------
c-function KeccakInitialize KeccakInitialize  -- void
c-function KeccakF KeccakF a n -- void
c-function KeccakInitializeState KeccakInitializeState a -- void
c-function KeccakExtract KeccakExtract a a n -- void
c-function KeccakAbsorb KeccakAbsorb a a n -- void
c-function KeccakEncrypt KeccakEncrypt a a n -- void
c-function KeccakDecrypt KeccakDecrypt a a n -- void
c-function KeccakEncryptLoop KeccakEncryptLoop a a n n -- a
c-function KeccakDecryptLoop KeccakDecryptLoop a a n n -- a

