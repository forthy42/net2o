\ generic rng

\ Copyright © 2010-2014   Bernd Paysan

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
\ using Keccak in duplex mode.  This is a huge amount of state (256+200
\ bytes), of which the 200 bytes Keccak state is kept hidden.  Since each
\ round replaces the previous Keccak state, this is (of course!) a key-erasing
\ RNG.  Even the saved .initrng cannot be used to reconstruct the stream, as
\ it is not the actual key which is written there, but generated random data.
\ The buffer used therefore must be bigger than the state needed.

require unix/pthread.fs

debug: rng( \ debug rng stuff

$100 Constant rngbuf#

user-o rng-o

object uclass rng-o
    rngbuf# uvar rng-buffer
    c:key#  uvar rng-key
    cell uvar rng-pos
    cell uvar rng-pid
    cell uvar rng-task
end-class rng-c

: rng-exec ( xt -- ) \ net2o
    \G run @i{xt} with activated random key
    c:key@ >r  rng-key c:key!  catch  r> c:key!  throw ;

: read-urnd ( addr u -- ) \ net2o
    \G legacy version of read-rnd
    s" /dev/urandom" r/o open-file throw >r
    tuck r@ read-file r> close-file throw
    throw <> !!insuff-rnd!! ;

: read-rnd ( addr u -- ) \ net2o
    \G read in entropy bytes from the systems entropy source
    [ [defined] getrandom [defined] linux and [IF]
	"getrandom" "libc.so.6" open-lib lib-sym 0<>
    [ELSE] false [THEN] ]
    [IF]
	bounds U+DO \ getrandom reads $100 bytes at maximum
	    I delta-I $100 umin 0 getrandom
	    dup -1 = IF  errno #38 = IF  drop
		    \ oops, we don't have getentropy in the kernel
		    I delta-I $100 umin read-urnd
		ELSE  BUT  THEN \ resolve the other IF
		?ior  THEN
	$100 +LOOP
    [ELSE]
	read-urnd
    [THEN] ;

: rng-init ( -- ) \G reed seed into the buffer
    \ note that reading 256 bytes of /dev/urandom is unnecessary much
    \ but for sake of simplicity, just do it. It will produce
    \ good randomness even with a number of backdoor possibilities
    rng-buffer rngbuf# read-rnd ;

: >rng$ ( addr u -- ) \ net2o
    \G fill @i{addr u} with random data by encrypting it
    \G so whatever was there before is used as entropy
    \G for the PRNG.
    ['] c:encrypt rng-exec ;

: rng-step ( -- ) \ net2o
    \G one step of random number generation 
    rng-buffer rngbuf# >rng$ rng-pos off ;

\ init rng to be actually useful

$Variable init-rng$
"initrng" .net2o-config/ init-rng$ $!

: random-init ( -- )
    rng-key c:key# read-rnd ;

: read-initrng ( fd -- flag )  { fd }
    #0. fd reposition-file throw
    rng-key c:key# fd read-file throw c:key# =
    ['] c:diffuse rng-exec  fd close-file throw ;

: write-initrng ( -- )
    init-rng$ $@ dirname '/' -skip $1FF init-dir drop
    init-rng$ $@ r/w create-file throw >r
    rng-buffer c:key# r@ write-file throw
    r> close-file throw  rng-step ;

\ Sanity check

$Variable check-rng$
Variable check-old$
"checkrng" .net2o-config/ check-rng$ $!
$10 cells buffer: rngstat-buf

: rngstat ( addr u -- float ) \ net2o
    \G produces a normalized number, gauss distributed around 0
    rngstat-buf $10 cells erase  dup 3 rshift { e }
    bounds ?DO
	1 I c@ 4 rshift cells rngstat-buf + +!
	1 I c@ $F and   cells rngstat-buf + +!
    LOOP
    0 $10 0 DO  rngstat-buf I cells + @ e - dup * +  LOOP
    s>f e fm/ 0.0625e f* -1e fexp f**
    [ 16e 1e fexp f- 16e f/ -1e fexp f** ] FLiteral f- ;

: ?check-rng ( -- ) \ net2o
    \G Check the RNG state for being deterministic (would be fatal).
    \G Check whenever you feel it is important enough, not limited to
    \G salt setup.
    check-old$ $free
    rng-key $20 \ check only first 256 bits
    check-rng$ $@ file-status nip no-file# <> IF
	check-rng$ $@ check-old$ $slurp-file
	check-old$ $@ 2over search nip nip !!bad-rng!!
	check-rng$ $@ w/o open-file throw dup
	>r file-size throw r@ reposition-file throw
    ELSE
	check-rng$ $@ dirname '/' -skip $1FF init-dir drop
	check-rng$ $@ w/o create-file throw >r
    THEN
    2dup check-old$ $+!
    r@ write-file throw  r> close-file throw
    check-old$ $@ rngstat fdup .9e f>
    IF    f. cr check-old$ $@ dump true !!bad-rng!!
    ELSE  health( ." RNG seeded, health check passed " f. cr )else( fdrop )
    THEN  rng-step rng-step ;
\ after checking, we need to make a step
\ to make sure the next check can be done
\ and a second one, to make sure the saved randomness
\ does not leak anything important

: .rngstat ( addr u -- ) \ net2o
    \G print a 16 bins histogram chisq test of the random data
    rngstat
    ." health - χ² normalized (|x|<0.9): "
    fdup fabs .9e f<= IF  <info>  ELSE  <err>  THEN
    6 4 1 f.rdp <default> cr ;
\    $10 0 DO  rngstat I cells + ?  LOOP cr

\ init salt

Sema rng-sema
User ?salt-init  ?salt-init off

: salt-init ( -- )
    rng( [: cr ." init salt: " up@ h. (getpid) . ;] do-debug )
    init-rng$ $@ r/o open-file IF  drop random-init
    ELSE  read-initrng  0= IF  random-init  THEN  THEN
    rng-init rng-step ?check-rng write-initrng
    \ never do this stuff below without having checked the RNG:
    ?salt-init on  getpid rng-pid !  up@ rng-task ! ;

[IFDEF] atfork:
    :noname ['] salt-init rng-sema c-section ; atfork:
    Constant salt-init-atfork
[THEN]

: rng-allot ( -- )
    rng-c >osize @ kalloc rng-o !
    rngbuf# rng-pos !
    ['] salt-init rng-sema c-section
    [IFDEF] pthread_atfork
	0 0 salt-init-atfork pthread_atfork errno-throw
    [THEN]
;

\ buffered random numbers to output 64 bit at a time

: ?rng ( -- ) \ net2o
    \G alloc rng if not there
    rng-o @ 0= IF  rng-allot
    ELSE  up@ rng-task @ <> IF   rng-allot  THEN  THEN
    [IFUNDEF] pthread_atfork
	getpid rng-pid @ <>
	IF  ['] salt-init rng-sema c-section  THEN
    [THEN]
    ?salt-init @ 0= !!no-salt!! ; \ fatal

: rng-step? ( n -- ) \ net2o
    \G check if n bytes are available in the buffer
    \G in case the RNG is not initialized, init it.
    \G this covers forks and new threads, as the RNG key
    \G is per-thread.
    rngbuf# u> IF  rng-step  THEN ;

: rng64 ( -- x64 ) \ net2o
    \G return a 64 bit random number
    ?rng
    rng-pos @ 64aligned 64'+ rng-step?
    rng-pos @ 64aligned dup 64'+ rng-pos !
    rng-buffer + 64@ ;

: rng$ ( u -- addr u ) \ net2o
    \G return a @i{u} bytes stream (@i{u} must be smaller than the
    \G buffer size}
    { u } ?rng
    rng-pos @ u + rng-step?
    rng-buffer rng-pos @ + u dup rng-pos +! ;

: rng32 ( -- x ) \ net2o
    \G return a 32 bit random number
    ?rng
    rng-pos @ 4 + rng-step?
    rng-pos @ rng-buffer + l@
    4 rng-pos +! ;

: rng ( u -- x )
    [IFDEF] 64bit rng64 [ELSE] rng32 [THEN] um* nip ;

: rng8 ( -- c ) \ net2o
    \G return an 8 bit random number
    ?rng
    rng-pos @ 1+ rng-step?
    rng-pos @ rng-buffer + c@
    1 rng-pos +! ;

:noname defers 'image check-old$ $free ?salt-init off rng-o off
    rngstat-buf $10 cells erase ; is 'image
