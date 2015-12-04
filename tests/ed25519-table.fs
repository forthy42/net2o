\ test ed25519-donna basepoint multiplication

require ansi.fs
require mini-oof2.fs
require user-object.fs
require unix/mmap.fs
require ../net2o-tools.fs
require ../xtype.fs
require ../base64.fs
require ../base85.fs
require ../kregion.fs
require ../crypto-api.fs
require ../keccak.fs
require ../rng.fs
require ../ed25519-donna.fs

here negate $1F and allot
here $20 allot
here $20 allot
here $20 allot
here $20 allot
constant stpkc constant stskc constant keypad constant keypad2

: init-pkc
    ge25519-basepoint get0 $30 4 * move
    stpkc get0 ge25519-pack ;

: init-skc ( -- )
    stskc $20 erase 1 stskc c! ;

: sk*2 ( addr -- )  0 swap $20 bounds DO
	I @ tuck 2* swap - I ! 0<
    cell +LOOP  drop ;

: ?ok  keypad $20 keypad2 over str= IF ." ok"  THEN ;

: ed-dhv { sk pk dest -- secret len }
    get0 pk ge25519-unpack- 0= !!no-ed-key!!
    sct2 sk raw>sc25519
    get1 get0 sct2 ge25519*v
    dest get1 ge25519-pack
    clean-ed25519 dest $20  $80 dest $1F + xorc! ;

: 25519.all ( -- )
    init-skc init-pkc
    $100 0 DO  ( stskc sk-mask ) \ stskc $20 xtype space
	stskc stpkc keypad ed-dh 85type space
	stskc stpkc keypad2 ed-dhv 2drop ?ok
	stskc keypad2 sk>pk ?ok
	cr stskc sk*2  LOOP ;

script? [IF] 25519.all bye [THEN]