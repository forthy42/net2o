\ Keccak: Forth version derived from "readable keccak" by Bernd Paysan
\ 19-Nov-11  Markku-Juhani O. Saarinen <mjos@iki.fi>
\ A baseline Keccak (3rd round) implementation.

24 Value keccak-rounds

Create keccakf-rndc
    $0000000000000001 , $0000000000008082 , $800000000000808a ,
    $8000000080008000 , $000000000000808b , $0000000080000001 ,
    $8000000080008081 , $8000000000008009 , $000000000000008a ,
    $0000000000000088 , $0000000080008009 , $000000008000000a ,
    $000000008000808b , $800000000000008b , $8000000000008089 ,
    $8000000000008003 , $8000000000008002 , $8000000000000080 , 
    $000000000000800a , $800000008000000a , $8000000080008081 ,
    $8000000000008080 , $0000000080000001 , $8000000080008008 ,

Create keccakf-rotc
    1 c,  3 c,  6 c,  10 c, 15 c, 21 c, 28 c, 36 c, 45 c, 55 c, 2 c,  14 c, 
    27 c, 41 c, 56 c, 8 c,  25 c, 43 c, 62 c, 18 c, 39 c, 61 c, 20 c, 44 c,

Create keccakf-piln
    10 c, 7 c,  11 c, 17 c, 18 c, 3 c, 5 c,  16 c, 8 c,  21 c, 24 c, 4 c, 
    15 c, 23 c, 19 c, 13 c, 12 c, 2 c, 20 c, 14 c, 22 c, 9 c,  6 c,  1 c,

\ update the state with given number of rounds

5 cells buffer: bc
25 cells buffer: st

: lrot1 ( x1 -- x2 )  dup 2* swap 0< - ;
: lrot ( x1 n -- x2 )  2dup lshift >r 64 swap - rshift r> or ;
: xor! ( x addr -- )  dup >r @ xor r> ! ;

: theta1 ( -- )
    5 0 DO
	0 st i cells + 25 cells bounds DO  I @ xor  [ 5 cells ]L +LOOP
	bc i cells + !
    LOOP ;

: theta2 ( -- )
    5 0 DO
	bc I 4 + 5 mod cells + @
	bc I 1 + 5 mod cells + @ lrot1 xor
	st i cells + 25 cells bounds DO  dup I xor!  [ 5 cells ]L +LOOP
	drop
    LOOP ;

: rhopi ( -- )
    st cell+ @
    24 0 DO
	keccakf-piln I + c@
	cells st + dup @
	rot keccakf-rotc I + c@ lrot
	rot !
    LOOP drop ;

: chi ( -- )
    st 25 cells bounds DO
	I bc 5 cells move
	5 0 DO
	    bc I 1+ 5 mod cells + @ bc I 2 + 5 mod cells + @ and
	    J I cells + xor!
	LOOP
    [ 5 cells ]L +LOOP ;

: iota ( round -- )
    cells keccakf-rndc + @ st @ xor st ! ;

: keccakf ( -- )
    keccak-rounds 0 ?DO  theta1  theta2  rhopi  chi  I iota  LOOP ;

: st0 ( -- )  st 25 cells erase ;

: >sponge ( addr u -- )
    \ fill in sponge function
    st swap bounds DO  dup @ I xor!  cell+  cell +LOOP  drop ;

144 buffer: kpad

: padded>sponge ( addr u1 u2 -- )  >r
    \ pad last round
    kpad r@ erase  tuck kpad swap move
    kpad + 1 swap c!
    kpad r@ + 1- dup c@ $80 or swap c!
    kpad r> >sponge  ;