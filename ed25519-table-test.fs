\ test ed25519-donna basepoint multiplication

: xorc! ( x c-addr -- )   >r r@ c@ xor  r> c! ;

require base85.fs
require ed25519-donna.fs

here negate $1F and allot
here $20 allot
here $20 allot
here $20 allot
constant stpkc constant stskc constant keypad

: init-pkc
    ge25519-basepoint get0 $30 4 * move
    stpkc get0 ge25519-pack ;

: init-skc ( -- )
    stskc $20 erase 1 stskc c! ;

: sk*2 ( addr -- )  0 swap $20 bounds DO
	I @ tuck 2* - I ! 0<
    cell +LOOP  drop ;

: 25519.all ( -- )
    init-skc init-pkc
    $100 0 DO  stskc stpkc keypad ed-dh 85type space stskc keypad sk>pk keypad $20 85type
	cr stskc sk*2  LOOP ;

script? [IF] 25519.all bye [THEN]