\ generic crypto api for net2o

\ Copyright © 2013-2015   Bernd Paysan

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

require mini-oof2.fs
require user-object.fs

\ generic padding primitives

: >pad ( addr u u2 -- addr u2 ) \ u <= u2
    swap >r 2dup r@ safe/string r> fill ;
: >unpad ( addr u' -- addr u ) over + 1- c@ ;
: ?padded ( addr u' -- flag )
    2dup + 1- c@ dup >r safe/string r> skip nip 0= ;

: >padded { addr1 u1 addr2 u2 -- }
    addr1 u1 addr2 u2 smove
    u1 u2 u< IF  addr2 u1 u2 >pad 2drop  THEN ;

\ For wurstkessel compatibility, the states are all 128 bytes
\ If the cryptosystem has more internal state, it may copy the key+iv there.
\ If it has less, it should use a suitable fraction of the key and the iv

User-o crypto-o

object class
    $20 uvar crypto-up \ make sure @keccak is aligned by 64 bytes
    umethod c:init ( -- )
    \G initialize crypto function for a task
    umethod c:free ( -- )
    \G free crypto function for a task
    umethod c:0key ( -- )
    \G set zero key
    umethod c:key! ( addr -- )
    \G use addr as key storage
    umethod c:key@ ( -- addr )
    \G obtain the key storage
    umethod c:key# ( -- n )
    \G obtain key storage size
    umethod >c:key ( addr -- )
    \G move 128 bytes from addr to the state
    umethod c:key> ( addr -- )
    \G get 128 bytes from the state to addr
    umethod c:diffuse ( -- )
    \G perform a diffuse round
    umethod c:encrypt ( addr u -- )
    \G Encrypt message in buffer addr u
    umethod c:decrypt ( addr u -- )
    \G Decrypt message in buffer addr u
    umethod c:encrypt+auth ( addr u tag -- )
    \G Encrypt message in buffer addr u
    umethod c:decrypt+auth ( addr u tag -- flag )
    \G Decrypt message in buffer addr u
    umethod c:hash ( addr u -- )
    \G Hash message in buffer addr u
    umethod c:prng ( addr u -- )
    \G Fill buffer addr u with PRNG sequence
    umethod c:shorthash ( addr u -- )
    \G absorb + hash for a message <= 64 bytes
    umethod c:hash@ ( addr u -- )
    \G extract short hash (up to 64 bytes)
    umethod c:tweakkey! ( x128 addr u -- )
    \G set key plus tweak
end-class crypto
