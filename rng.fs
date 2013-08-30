\ generic rng

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

: rng-exec ( xt -- )  c:key@ >r  rng-key @ c:key!  execute  r> c:key! ;

: rng-init ( -- )
    rng-buffer @ rngbuf# rng-fd read-file throw drop ;

: rng-step ( -- )
    [: rng-init
       rng-buffer @ rngbuf# c:encrypt
       rng-pos off ;] rng-exec ;

\ init rng to be actually useful

: random-init ( -- )
    s" /dev/random" r/o open-file throw >r
    rng-key @ c:key# r@ read-file throw drop
    r> close-file throw ;

: read-initrng ( fd -- flag )  { fd }
    0. fd reposition-file throw
    rng-key @ c:key# fd read-file throw c:key# =  c:diffuse
    fd close-file throw ;

: write-initrng ( -- )
    s" ~/.initrng" r/w create-file throw >r
    rng-key @ c:key# r@ write-file throw
    r> close-file throw ;

: salt-init ( -- )
    s" ~/.initrng" r/o open-file IF  drop random-init
    ELSE  read-initrng  0= IF  random-init  THEN  THEN
    rng-step write-initrng rng-step ;

\ buffered random numbers to output 64 bit at a time

: rng-step? ( n -- )
    up@ rng-task @ <> getpid rng-pid @ <> or
    IF  rng-allot salt-init  THEN
    rngbuf# u> IF  rng-step  THEN ;

: rng@ ( -- x )
    rng-pos @ 64aligned 64'+ rng-step?
    rng-pos @ 64aligned dup 64'+ rng-pos ! rng-buffer @ + 64@ ;

: rng$ ( u -- addr u ) >r
    rng-pos @ r@ + rng-step?
    rng-buffer @ rng-pos @ + r> dup rng-pos +! ;

: rng32 ( -- x )
    rng-pos @ 4 + rng-step?
    rng-pos @ rng-buffer @ + l@
    4 rng-pos +! ;

