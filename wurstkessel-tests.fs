\ wurstkessel tests

: test-hash
    s" wurstkessel.fs" wurst-file roundse# roundsh# wurst-hash ;
: test-encrypt
    s" wurstkessel.fs" wurst-file s" wurstkessel.wurst" wurst-outfile roundse# rounds# wurst-encrypt ;
: test-decrypt
    s" wurstkessel.wurst" wurst-file s" wurstkessel.fs2" wurst-outfile roundse# rounds# wurst-decrypt ;
: test-rng ( n -- ) s" wurst.random" wurst-outfile rng-init
    rounds# >reads state# * swap
    0 ?DO
	rounds# wurst-rng
	message over wurst-out write-file throw
	message over erase  LOOP wurst-close ;
: out-rng ( n -- ) stdout to wurst-out \ rng-init
    rounds# >reads state# * swap
    0 ?DO
	rounds# wurst-rng
	message over wurst-out write-file throw
	message over erase  LOOP wurst-close ;

\ test for quality

[IFDEF] 'rounds
: wurst-break  s" wurstkessel.fs" wurst-file s" wurstkessel.wurst2" wurst-outfile roundse# roundsh# wurst-encrypt
    s" wurstkessel.fs" wurst-file roundsh# read-first drop
    s" wurstkessel.wurst2" wurst-file
    wurst-source state# wurst-in read-file throw drop
    s" wurstkessel.wurst2" wurst-file
    wurst-source state# wurst-in read-file throw drop
    wurst-state state# wurst-in read-file throw drop
    wurst-state wurst-source state# xors
    message wurst-source state# xors
    wurst-source wurst-state state# xors
    wurst-state wurst-source state# xors
    wurst-state state# wurst-in read-file throw drop
    wurst-source wurst-state state# xors
    message state# + wurst-state state# xors
    message wurst-source state# xors
    state# 0 wurst-in reposition-file throw
    s" wurstkessel.fs3" wurst-outfile roundsh# >r
    r@ encrypt-read
    r@  message swap  dup $F and 8 umin 0 ?DO
	I 0> IF 'rounds I cells + @ execute THEN
	dup 'round-flags Ith and IF
	    swap -entropy swap
	THEN
    LOOP 2drop
    r@ .xormsg-size
    BEGIN  0>  WHILE
	r@ encrypt-read
	r@ rounds-decrypt  r@ message>'
    REPEAT
    rdrop  wurst-close ;
[THEN]

Create rng-histogram $100 0 [DO] 0 , [LOOP]
: time-rng ( n -- ) rng-init
    0 ?DO  rounds# wurst-rng  LOOP ;
: eval-rng ( n -- )
    0 ?DO  rounds# wurst-rng
	wurst-state state# bounds ?DO
	    1 I c@ cells rng-histogram + +!  LOOP
    LOOP
    state# 0 DO rng-histogram I cells + @ . cr LOOP ;

: wurst-test test-hash test-encrypt test-decrypt ;

Create wurst-tmp state# allot

: find-same ( d -- )
    $100 0 DO
	$100 I DO
	    j rngs i rngs rot xor -rot xor swap
	    8 0 DO 2over 2over d= IF I . J . K . cr THEN 0. wurst
	    LOOP 2drop
	LOOP
    LOOP 2drop ;

s" gforth" environment? [IF] 2drop
    require fft.fs
[THEN]
s" bigforth" environment? [IF] 2drop
    include fft.fb
[THEN]

: 32>f dup $80000000 and negate or s>f 4.6566128731E-10 f* ;

: rng-fft-test ( n -- ) dup points rng-init
    rounds# >reads state# * swap
    dup 0 ?DO
	rounds# wurst-rng
	I message 2 pick bounds ?DO
	    I     32@ 32>f
	    I 4 + 32@ 32>f dup values z! 1+
	8 +LOOP drop
	message over erase
    8 +LOOP
    fft #points s>f 1/f fsqrt fftscale ;

: rngs-fft-test ( -- ) $100 points
    'rngs $100 64s bounds ?DO
	    I     32@ 32>f
	    I 4 + 32@ 32>f dup values z! 1+
    8 +LOOP
    fft #points s>f 1/f fsqrt fftscale ;

Create fft-test-2d here $1000 cells dup allot erase

: >test-2d ( -- )
    #points 0 ?DO
	I values z@
	$8 fm* 32e f+ f>s $8 fm* 32e f+ f>s 6 lshift + cells fft-test-2d + 1 swap +!
    LOOP ;

: .test-2d ( -- )
    $40 0 DO
	$40 0 DO
	    J 6 lshift I + cells fft-test-2d + ?
	LOOP cr
    LOOP ;

: >test-1d ( -- )
    #points 0 ?DO
	I values z@
	$8 fm* 32e f+ f>s cells fft-test-2d + 1 swap +!
	$8 fm* 32e f+ f>s cells fft-test-2d + 1 swap +!
    LOOP ;

: .test-1d ( -- )
    $40 0 DO
	I cells fft-test-2d + ?
    LOOP ;

\ check for dupes

: test32 ( n -- )  message $20 erase base @ >r hex
    0 ?DO  hash-init I message ! roundsh# rounds32 roundse# rounds32
	.source32 space .state32 space I 8 u.r cr LOOP
    r> base ! ;

Variable lastx

root definitions
: x? ( -- )
    2 pick lastx @ = IF  pad count type source type cr  THEN  2drop lastx !
    source pad place ;
forth definitions