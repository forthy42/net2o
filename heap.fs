\ binary heap

require string.fs
require mini-oof.fs

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

:noname { i1 i2 heap -- flag }
    i1 heap harray @ + @
    i2 heap harray @ + @ < ; heap defines hless
:noname ( i1 i2 heap -- ) harray @ tuck + >r + r> { i1 i2 }
    i1 @ i2 @  i1 ! i2 ! ; heap defines hswap
:noname drop . ; heap defines .h
:noname drop cell ; heap defines hcell
:noname drop @ ; heap defines heap@
:noname drop ! ; heap defines heap!

: hnew ( -- heap )
    heap new >r
    r@ hcell dup r@ hmaxsize ! 0 r@ hsize !
    allocate throw r@ harray ! r> ;

: hresize> ( heap -- ) >r
    r@ hmaxsize @ r@ hsize @ u< IF
	r@ harray @
	r@ hmaxsize @ 2* dup r@ hmaxsize ! resize throw
	r@ harray !
    THEN rdrop ;

: hresize< ( heap -- ) >r
    r@ hmaxsize @ 2/ r@ hsize @ u> IF
	r@ harray @
	r@ hmaxsize @ 2/ dup r@ hmaxsize ! resize throw
	r@ harray !
    THEN rdrop ;

: bubble-up ( index heap -- )
    dup hcell { index heap size }
    BEGIN
	index size / 1- 2/ size * dup { index/2 } 0>= WHILE
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
    dup hcell over hsize @ { index heap size hsize }
    BEGIN
	index dup 2* size + { index*2 }
	index*2 hsize u<  WHILE
	    index index*2 heap hless 0= IF
		drop index*2  THEN
	    index*2 size + hsize u<  IF
		dup index*2 size + heap hless 0= IF
		    drop index*2 size +  THEN  THEN
	    index over  heap hswap
	    dup index = swap to index
	UNTIL  EXIT  THEN drop ;

: hdelete ( heap -- ... ) >r
    r@ hsize @ 0= abort" heap empty"
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
