\ generic rng

$400 Constant rngbuf#

s" /dev/urandom" r/o open-file throw Value rng-fd

Variable rng-buffer rngbuf# allot
rngbuf# rng-buffer !
c:key# buffer: rng-key

: rng-exec ( xt -- )  c:key@ >r  rng-key c:key!  execute  r> c:key! ;

: rng-init ( -- )
    rng-buffer cell+ rngbuf# rng-fd read-file throw drop ;

rng-init

: rng-step ( -- )
    [: rng-init
       rng-buffer cell+ rngbuf# c:encrypt
       rng-buffer off ;] rng-exec ;

\ buffered random numbers to output 64 bit at a time

: rng-step? ( n -- ) rngbuf# u> IF  rng-step  THEN ;

: rng@ ( -- x )
    rng-buffer @ 64aligned 64'+ rng-step?
    rng-buffer dup @ 64aligned dup 64'+ rng-buffer ! cell+ + 64@ ;

: rng$ ( u -- addr u ) >r
    rng-buffer @ r@ + rng-step?
    rng-buffer dup @ + cell+ r> dup rng-buffer +! ;

: >rng$ ( addr u -- )  \ fill buffer with random stuff
    bounds ?DO
	I' I - rngbuf# rng-buffer @ - umin rng$ I swap dup >r move r>
    +LOOP ;

: rng32 ( -- x )
    rng-buffer @ 4 + rng-step?
    rng-buffer dup @ + cell+ l@
    4 rng-buffer +! ;

\ init rng to be actually useful

: random-init ( -- )
    s" /dev/random" r/o open-file throw >r
    rng-key c:key# r@ read-file throw drop
    r> close-file throw ;

: read-wurstrng ( fd -- flag )  { fd }
    0. fd reposition-file throw
    rng-key c:key# fd read-file throw c:key# =  c:diffuse
    fd close-file throw ;

: write-wurstrng ( -- )
    s" ~/.wurstrng" r/w create-file throw >r
    rng-buffer cell+ c:key# r@ write-file throw
    r> close-file throw ;

: salt-init ( -- )
    s" ~/.wurstrng" r/o open-file IF  drop random-init
    ELSE  read-wurstrng  0= IF  random-init  THEN  THEN
    rng-step write-wurstrng rng-step ;

wurst-init
wurst-task-init
salt-init
