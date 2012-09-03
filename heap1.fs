\ binary heap

\ a binary heap is a structure to keep a partially sorted set
\ so that you can easily insert elements, and extract the least element

[defined] ntime 0= [IF]
    library: librt.so.1
    extern: int clock_gettime ( int , int );

    2Variable timespec
    : ntime ( -- d )  0 timespec clock_gettime drop
	timespec 2@ #1000000000 um* rot 0 d+ ;
[THEN]

begin-structure heap
field: harray
field: hsize
field: hmaxsize
end-structure

: hless ( i1 i2 heap -- flag )
    harray @ tuck + @ >r + @ r> < ;
: hswap ( i1 i2 heap -- )
    harray @ tuck + >r + r> { i1 i2 }
    i1 @ i2 @  i1 ! i2 ! ;

: hnew ( -- heap )
    heap allocate throw >r
    cell dup r@ hmaxsize ! 0 r@ hsize !
    allocate throw r@ harray ! r> ;

: hresize> ( heap -- ) >r
    r@ hmaxsize @ r@ hsize @ u< IF
	r@ harray @
	r@ hmaxsize @ 2* dup r@ hmaxsize ! resize throw
	r@ harray !
    THEN r> drop ;

: hresize< ( heap -- ) >r
    r@ hmaxsize @ 2/ r@ hsize @ u> IF
	r@ harray @
	r@ hmaxsize @ 2/ dup r@ hmaxsize ! resize throw
	r@ harray !
    THEN r> drop ;

: bubble-up ( index heap -- )
    0 { index heap index/2 }
    BEGIN
	index cell / 1- 2/ cells dup to index/2 0< 0= WHILE
	    index index/2 heap hless  WHILE
		index index/2 heap hswap
		index index/2 to index
    0= UNTIL  THEN THEN ;

: hinsert ( ... heap -- ) { heap }
    heap hsize @ dup >r
    cell heap hsize +! heap hresize>
    heap harray @ + !
    r> heap bubble-up ;

: bubble-down ( heap -- ) 0 swap
    cell over hsize @ 0 { index heap size hsize index*2 }
    BEGIN
	index dup 2* cell+ to index*2
	index*2 hsize u<  WHILE
	    index index*2 heap hless 0= IF
		drop index*2  THEN
	    index*2 size + hsize u<  IF
		dup index*2 cell+ heap hless 0= IF
		    drop index*2 cell+  THEN  THEN
	    index over  heap hswap
	    dup index = swap to index
	UNTIL  EXIT  THEN drop ;

: hdelete ( heap -- ... ) >r
    r@ hsize @ 0= abort" heap empty"
    r@ harray @ @
    cell negate r@ hsize +!
    r@ harray @ r@ hsize @ + @ r@ harray @ !
    r@ hresize<
    r> bubble-down ;

: hsize@ ( heap -- )
    hsize @ cell / ;

: .heap { heap -- }
    heap harray @ heap hsize @ bounds ?DO
	I ?
    cell +LOOP ;
