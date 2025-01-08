\ binary heap

require mini-oof2.fs

[defined] ntime 0= [IF]
    library: librt.so.1
    extern: int clock_gettime ( int , int );

    2Variable timespec
    : ntime ( -- d )  0 timespec clock_gettime drop
	timespec 2@ #1000000000 um* rot 0 d+ ;
[THEN]

\ a binary heap is a structure to keep a partially sorted set
\ so that you can easily insert elements, and extract the least element

object class
    cell var harray
    cell var hsize
    cell var hmaxsize
    method hless
    method hswap
    method hcell
    method heap@
    method heap!
    method .h
end-class heap

heap :method hless ( i1 i2 -- flag )
    harray @ tuck + @ >r + @ r> < ;
heap :method hswap ( i1 i2 -- )
    harray @ tuck + >r + r> { i1 i2 }
    i1 @ i2 @  i1 ! i2 ! ;
heap :method .h . ;
heap :method hcell cell ;
heap :method heap@ @ ;
heap :method heap! ! ;

: hnew ( -- heap )
    heap new dup
    >r hcell dup r@ hmaxsize ! 0 r@ hsize !
    allocate throw r@ harray ! r> ;

: hresize> ( heap -- ) dup
    >r hmaxsize @ r@ hsize @ u< IF
	r@ harray @
	r@ hmaxsize @ 2* dup r@ hmaxsize ! resize throw
	r@ harray !
    THEN r> drop ;

: hresize< ( heap -- ) dup
    >r hmaxsize @ 2/ r@ hsize @ u> IF
	r@ harray @
	r@ hmaxsize @ 2/ dup r@ hmaxsize ! resize throw
	r@ harray !
    THEN r> drop ;

: bubble-up ( index heap -- )
    dup hcell 0 { index heap size index/2 }
    BEGIN
	index size / 1- 2/ size * dup to index/2 0< 0= WHILE
	    index index/2 heap hless  WHILE
		index index/2 heap hswap
		index index/2 to index
    0= UNTIL  THEN THEN ;

: hinsert ( ... heap -- ) { heap }
    heap hsize @ dup >r
    heap hcell heap hsize +! heap hresize>
    heap harray @ + heap heap!
    r> heap bubble-up ;

: bubble-down ( heap -- ) 0 swap
    dup hcell over hsize @ 0 { index heap size hsize index*2 }
    BEGIN
	index dup 2* size + to index*2
	index*2 hsize u<  WHILE
	    index index*2 heap hless 0= IF
		drop index*2  THEN
	    index*2 size + hsize u<  IF
		dup index*2 size + heap hless 0= IF
		    drop index*2 size +  THEN  THEN
	    index over  heap hswap
	    dup index = swap to index
	UNTIL  EXIT  THEN drop ;

: hdelete ( heap -- ... ) dup
    >r hsize @ 0= abort" heap empty"
    r@ harray @ r@ heap@
    r@ hcell negate r@ hsize +!
    r@ harray @ r@ hsize @ + r@ heap@ r@ harray @ r@ heap!
    r@ hresize<
    r> bubble-down ;

: hsize@ ( heap -- )
    dup hsize @ swap hcell / ;

: .heap { heap -- }
    heap harray @ heap hsize @ bounds ?DO
	I heap heap@ heap .h
    heap hcell +LOOP ;
