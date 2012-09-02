\ same interface as binary heap, but O(n) insert instead

Variable ins-n
: hnew  s" " here 0 , dup >r $! r> ;
: hinsert { n heap -- } n ins-n !
    heap $@ bounds ?DO  n I @ < IF
	    ins-n cell heap I heap $@ drop - $ins
	    UNLOOP  EXIT  THEN
    cell +LOOP
    ins-n cell heap $+! ;
: hdelete { heap -- n }
    heap $@ drop @
    heap 0 cell $del ;
: hsize@ ( heap -- size ) $@len cell / ;
