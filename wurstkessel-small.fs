\ scaled down versions of Wurstkessel for testing

\ wurstkessel32 primitives

: wurst32 ( u1 u2 -- u3 )  >r 2* dup 16 rshift 1 and or $FFFF and r> xor ;
: rngs32 2* 'rngs + w@ ;

: mix2bytes32 ( index n k -- b1 .. b2 index' n ) wurst-state + 2 0 DO
	>r over wurst-source + c@ r@ c@ xor -rot dup >r + $3 and r> r> 2 + LOOP
    drop ;

: bytes2sum32 ( ud b1 .. b2 -- ud' ) >r >r
    r> rngs32 wurst32  r> rngs32 wurst32 ;

: update-state32 ( -- )
    wurst-state wurst-source state#32 xors
    nextstate wurst-state state#32 +!s ;

Create permut#32 1 , 0 , \ permut length 2
DOES> swap 1 and cells + @ ;

: round32 ( n -- ) dup 1- swap  2 0 DO
	wurst-state I permut#32 2* + w@ -rot
	I mix2bytes32 2>r bytes2sum32 2r> rot nextstate I 2* + w!
    LOOP 2drop update-state32 ;

: +entropy32 ( message -- message' )
    dup wurst-source state#32 xors  wurst-source over state#32 move
    state#32 + ;

\ wurstkessel16 primitives - really degenerated case

: wurst16 ( u1 u2 -- u3 )  >r 2* dup 8 rshift 1 and or $FF and r> xor ;
: rngs16 'rngs + c@ ;

: mix2bytes16 ( index n k -- b index' n ) wurst-state +
    >r over wurst-source + c@ r> c@ xor -rot dup >r + $0 and r> ;

: bytes2sum16 ( ud b -- ud' ) rngs16 wurst16 ;

: update-state16 ( -- )
    wurst-state c@ wurst-source c@ xor wurst-source c!
    nextstate c@ wurst-state c@ + wurst-state c! ;

: round16 ( n -- ) dup 1- swap
    wurst-state c@ -rot
    0 mix2bytes16 2>r bytes2sum16 2r> rot nextstate c!
    2drop update-state16 ;

: +entropy16 ( message -- message' )
    dup c@ wurst-source c@ xor wurst-source c!  wurst-source c@ over c!
    state#16 + ;

\ 32 bit rounds

[IFUNDEF] 'round-flags
    Create 'round-flags
    $10 , $30 , $10 , $70 , $10 , $30 , $10 , $F0 ,
[THEN]

: rounds32 ( addr n -- )  dup $F and 8 umin 0 ?DO
	I round# round32
	dup 'round-flags I cells + @ and IF
	    swap +entropy32 swap
	THEN
    LOOP 2drop ;

\ 32 bit wurst for testing

: wurst-size32 ( -- )
    [ cell 4 = ] [IF]
	size? drop message !
    [ELSE]
	size? drop message l!
    [THEN] ;
: encrypt-read32 ( flags -- n )  >reads >r
    message state#32 r> * 2dup erase  wurst-in read-file throw ;
: read-first32 ( flags -- n )  wurst-size32  >reads >r
    message state#32 r> * 2 2* /string wurst-in read-file throw  2 2* + ;
: .4h ( u -- )
    0 base @ >r hex <<# # # # # #> type #>> r> base ! ;

: .source32 ( -- ) 2 0 DO  wurst-source I 2* + w@ .4h  LOOP ;
: .state32  ( -- ) 2 0 DO  wurst-state I 2* + w@ .4h  LOOP ;

: wurst-hash32 ( final-rounds rounds -- )
    hash-init dup read-first32
    BEGIN  0>  WHILE
	    message over rounds32
	    dup encrypt-read32
    REPEAT
    drop message swap rounds32 .source32 wurst-close ;

