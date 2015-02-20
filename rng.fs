\ generic rng

\ Copyright (C) 2010-2014   Bernd Paysan

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

\ The general idea here is: You use an entropy source to initialize your PRNG.
\ The entropy source used here is /dev/urandom.  After initializing the PRNG,
\ you make sure that no observer can influence your PRNG, i.e. you stop
\ reading entropy.

\ The PRNG itself works by repeatedly encrypting the same block of memory
\ using Keccak in duplex mode.  This is a huge amount of state (1.2kB),
\ of which the 200 bytes Keccak state is kept hidden.

require unix/pthread.fs

$400 Constant rngbuf#

s" /dev/urandom" r/o open-file throw Value rng-fd

User rng-pos
User rng-buffer
User rng-pid
User rng-task
User rng-key
rngbuf# rng-pos !

: rng-allot ( -- )
    rngbuf# allocate throw rng-buffer !
    c:key# allocate throw rng-key !
    rngbuf# rng-pos !
    getpid rng-pid ! up@ rng-task ! ;

: rng-exec ( xt -- )
    c:key@ >r  rng-key @ c:key!  catch  r> c:key!  throw ;

: rng-init ( -- )
    rng-buffer @ rngbuf# rng-fd read-file throw drop ;

: rng-step ( -- )
    [: ( rng-init ) \ djb advices *not* to do this here
       rng-buffer @ rngbuf# c:encrypt
       rng-pos off ;] rng-exec ;

 : >rng$ ( addr u -- )
     ['] c:encrypt rng-exec ;
 
\ init rng to be actually useful

: random-init ( -- )
    rng-key @ c:key# rng-fd read-file throw drop ;

: read-initrng ( fd -- flag )  { fd }
    0. fd reposition-file throw
    rng-key @ c:key# fd read-file throw c:key# =
    c:diffuse  fd close-file throw ;

: write-initrng ( -- )
    s" ~/.initrng" r/w create-file throw >r
    rng-key @ c:key# r@ write-file throw
    r> close-file throw ;

: salt-init ( -- )
    s" ~/.initrng" r/o open-file IF  drop random-init
    ELSE  read-initrng  0= IF  random-init  THEN  THEN
    rng-init rng-step write-initrng rng-init rng-step ;

\ buffered random numbers to output 64 bit at a time

: rng-step? ( n -- )
    up@ rng-task @ <> getpid rng-pid @ <> or
    IF  rng-allot salt-init  THEN
    rngbuf# u> IF  rng-step  THEN ;

: rng@ ( -- x )
    rng-pos @ 64aligned 64'+ rng-step?
    rng-pos @ 64aligned dup 64'+ rng-pos !
    rng-buffer @ + 64@ ;

: rng$ ( u -- addr u ) >r
    rng-pos @ r@ + rng-step?
    rng-buffer @ rng-pos @ + r> dup rng-pos +! ;

: rng32 ( -- x )
    rng-pos @ 4 + rng-step?
    rng-pos @ rng-buffer @ + l@
    4 rng-pos +! ;

: rng8 ( -- c )
    rng-pos @ 1+ rng-step?
    rng-pos @ rng-buffer @ + c@
    1 rng-pos +! ;
