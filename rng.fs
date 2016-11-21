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

$100 Constant rngbuf#

user-o rng-o

object uclass rng-o
    rngbuf# uvar rng-buffer
    c:key#  uvar rng-key
    cell uvar rng-pos
    cell uvar rng-pid
    cell uvar rng-task
end-class rng-c

: rng-allot ( -- )
    rng-c >osize @ kalloc rng-o !
    rngbuf# rng-pos !
    getpid rng-pid ! up@ rng-task ! ;

: rng-exec ( xt -- )
    \G run @i{xt} with activated random key
    c:key@ >r  rng-key c:key!  catch  r> c:key!  throw ;

: read-rnd ( addr u -- )
    \G read in bytes from /dev/urandom
    s" /dev/urandom" r/o open-file throw >r
    tuck r@ read-file r> close-file throw
    throw <> !!insuff-rnd!! ;

: rng-init ( -- ) \G reed seed into the buffer
    \ note that reading 256 bytes of /dev/urandom is unnecessary much
    \ but for sake of simplicity, just do it. It will produce
    \ good randomness even with a number of backdoor possibilities
    rng-buffer rngbuf# read-rnd ;

: >rng$ ( addr u -- )
    \G fill @i{addr u} with random data by encrypting it
    \G so whatever was there before is used as entropy
    \G for the PRNG.
    ['] c:encrypt rng-exec ;

: rng-step ( -- )
    \G one step of random number generation 
    rng-buffer rngbuf# >rng$ rng-pos off ;

\ init rng to be actually useful

: random-init ( -- )
    rng-key c:key# read-rnd ;

: read-initrng ( fd -- flag )  { fd }
    #0. fd reposition-file throw
    rng-key c:key# fd read-file throw c:key# =
    ['] c:diffuse rng-exec  fd close-file throw ;

: write-initrng ( -- )
    s" ~/.initrng" r/w create-file throw >r
    rng-key c:key# r@ write-file throw
    r> close-file throw ;

Sema rng-sema
User ?salt-init  ?salt-init off

: salt-init ( -- )
    s" ~/.initrng" r/o open-file IF  drop random-init
    ELSE  read-initrng  0= IF  random-init  THEN  THEN
    rng-init rng-step write-initrng rng-step
    ?salt-init on ;

\ buffered random numbers to output 64 bit at a time

: ?rng ( -- )
    \G alloc rng if not there
    rng-o @ 0= IF  rng-allot  true
    ELSE  up@ rng-task @ <> dup IF   rng-allot  THEN  THEN
    getpid rng-pid @ <> or
    IF  ['] salt-init rng-sema c-section  getpid rng-pid !  THEN
    ?salt-init @ 0= !!no-salt!! ; \ fatal

: rng-step? ( n -- )
    \G check if n bytes are available in the buffer
    \G in case the RNG is not initialized, init it.
    \G this covers forks and new threads, as the RNG key
    \G is per-thread.
    rngbuf# u> IF  rng-step  THEN ;

: rng64 ( -- x64 )
    \G return a 64 bit random number
    ?rng
    rng-pos @ 64aligned 64'+ rng-step?
    rng-pos @ 64aligned dup 64'+ rng-pos !
    rng-buffer + 64@ ;

: rng$ ( u -- addr u ) >r
    \G return a @i{u} bytes stream (@i{u} must be smaller than the
    \G buffer size}
    ?rng
    rng-pos @ r@ + rng-step?
    rng-buffer rng-pos @ + r> dup rng-pos +! ;

: rng32 ( -- x )
    \G return a 32 bit random number
    ?rng
    rng-pos @ 4 + rng-step?
    rng-pos @ rng-buffer + l@
    4 rng-pos +! ;

: rng8 ( -- c )
    \G return an 8 bit random number
    ?rng
    rng-pos @ 1+ rng-step?
    rng-pos @ rng-buffer + c@
    1 rng-pos +! ;
