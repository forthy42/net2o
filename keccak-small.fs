\ Keccak: Forth version derived from "readable keccak" by Bernd Paysan
\ 19-Nov-11  Markku-Juhani O. Saarinen <mjos@iki.fi>
\ A baseline Keccak (3rd round) implementation.

24 Value keccak-rounds
5 cells constant kcol#
25 cells constant kkey#

: carray  Create  DOES> + c@ ;
: array   Create  DOES> swap cells + @ ;

array keccakf-rndc
    $0000000000000001 , $0000000000008082 , $800000000000808a ,
    $8000000080008000 , $000000000000808b , $0000000080000001 ,
    $8000000080008081 , $8000000000008009 , $000000000000008a ,
    $0000000000000088 , $0000000080008009 , $000000008000000a ,
    $000000008000808b , $800000000000008b , $8000000000008089 ,
    $8000000000008003 , $8000000000008002 , $8000000000000080 , 
    $000000000000800a , $800000008000000a , $8000000080008081 ,
    $8000000000008080 , $0000000080000001 , $8000000080008008 ,

carray keccakf-rotc
    1 c,  3 c,  6 c,  10 c, 15 c, 21 c, 28 c, 36 c, 45 c, 55 c, 2 c,  14 c, 
    27 c, 41 c, 56 c, 8 c,  25 c, 43 c, 62 c, 18 c, 39 c, 61 c, 20 c, 44 c,

: cc,  cells c, ;

carray keccakf-piln
10 cc, 7 cc,  11 cc, 17 cc, 18 cc, 3 cc,
5 cc,  16 cc, 8 cc,  21 cc, 24 cc, 4 cc, 
15 cc, 23 cc, 19 cc, 13 cc, 12 cc, 2 cc,
20 cc, 14 cc, 22 cc, 9 cc,  6 cc,  1 cc,

carray mod5
0 cc, 1 cc, 2 cc, 3 cc, 4 cc,
0 cc, 1 cc, 2 cc, 3 cc, 4 cc,

\ update the state with given number of rounds

kcol# buffer: bc
kkey# buffer: st

: lrot1 ( x1 -- x2 )  dup 2* swap 0< - ;
: lrot ( x1 n -- x2 )  2dup lshift >r 64 swap - rshift r> or ;
: xor! ( x addr -- )  dup >r @ xor r> ! ;

: theta1 ( -- )
    5 0 DO
	0 st i cells + kkey# bounds DO  I @ xor  kcol# +LOOP
	bc i cells + !
    LOOP ;

: theta2 ( -- )
    5 0 DO
	bc I 4 + mod5 + @
	bc I 1 + mod5 + @ lrot1 xor
	st i cells + kkey# bounds DO  dup I xor!  kcol# +LOOP
	drop
    LOOP ;

: rhopi ( -- )
    st cell+ @
    24 0 DO
	I keccakf-piln st + dup @
	rot I keccakf-rotc lrot
	rot !
    LOOP drop ;

: chi ( -- )
    st kkey# bounds DO
	I bc kcol# move
	5 0 DO
	    bc I 1+ mod5 + @ invert bc I 2 + mod5 + @ and
	    J I cells + xor!
	LOOP
    kcol# +LOOP ;

: iota ( round -- )
    keccakf-rndc st xor! ;

: oneround ( round -- )
    theta1  theta2  rhopi  chi  iota ;

: keccakf ( -- )
    keccak-rounds 0 ?DO  I oneround  LOOP ;

: st0 ( -- )  st kkey# erase ;

: >sponge ( addr u -- )
    \ fill in sponge function
    st swap bounds DO  dup @ I xor!  cell+  cell +LOOP  drop ;

: >duplex ( addr u -- )
    \ duplex in sponge function: encrypt
    st swap bounds DO  dup @ I @ xor dup I ! over !  cell+  cell +LOOP drop ;

: duplex> ( addr u -- )
    \ duplex out sponge function: decrypt
    st swap bounds DO  dup @ I @ xor over @ I ! over !  cell+  cell +LOOP drop ;

\ for test, we pad with Keccak's padding function

144 buffer: kpad

: padded>sponge ( addr u1 u2 -- )  >r
    \ pad last round
    kpad r@ erase  tuck kpad swap move
    kpad + 1 swap c!
    kpad r@ + 1- dup c@ $80 or swap c!
    kpad r> >sponge  ;

0 [IF]
    \ tests - we check only for the first 64 bit
    \ but repeat keccakf 4 times. The input pattern is
    \ from an official Keccak test, the output as well.
    ." Test "
    st0 s" SX{9" $80 padded>sponge 0 st 4 + c!
    keccakf st @ $466624B803BF072F =
    keccakf st @ $993340D7F9153F02 = and
    keccakf st @ $6EAAAE36BE8E36D3 = and
    keccakf st @ $1B4AEC08DA6A8BA6 = and
    [IF] ." succeeded" [ELSE] ." failed" [THEN] cr
[THEN]