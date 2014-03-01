\ net2o tests - client side

require net2o.fs

+db stat(
+debug
%droprate
key-task

"anonymous" >key \ get our anonymous key

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

0 UValue test#
1 Value total-tests

: .test# ( -- ) total-tests 1 > IF  test# 0 .r  THEN ;

: >timing ( -- )
    [: ." timing" .test# ;] $tmp w/o create-file throw >r
    ['] .rec-timing r@ outfile-execute r> close-file throw ;

: >cache ( addr u -- addr' u' ) [: ." .cache" .test# ." /" type ;] $tmp ;

: init-cache' ( -- )
    "" >cache 1- file-status nip #-514 = IF
	"" >cache 1- $1FF =mkdir throw
    THEN ;

: c:connect ( -- )
    $10000 $100000
    net2o-host $@ net2o-port insert-ip n2o:connect +flow-control +resend
    [: .time ." Connected, o=" o hex. cr ;] $err ;

: c:download1 ( -- )
    [: .time ." Download test: 1 text file and 2 photos" cr ;] $err
    net2o-code
    expect-reply
    s" Download test" $, type cr ( see-me ) get-ip
    $400 blocksize! $400 blockalign! stat( request-stats )
    "net2o.fs" "net2o.fs" >cache n2o:copy
    "data/2011-05-13_11-26-57-small.jpg" "photo000s.jpg" >cache n2o:copy
    "data/2011-05-20_17-01-12-small.jpg" "photo001s.jpg" >cache n2o:copy
    n2o:done
    send-chunks
    end-code
    1 client-loop n2o:close-all ['] .time $err ;

: c:download2 ( -- )
    [: ." Download test 2: 7 medium photos" cr ;] $err
    net2o-code
    expect-reply close-all \ rewind-total
    s" Download test 2" $, type cr ( see-me )
    $10000 blocksize! $400 blockalign! stat( request-stats )
    "data/2011-06-02_15-02-38-small.jpg" "photo002s.jpg" >cache n2o:copy
    "data/2011-06-03_10-26-49-small.jpg" "photo003s.jpg" >cache n2o:copy
    "data/2011-06-15_12-27-03-small.jpg" "photo004s.jpg" >cache n2o:copy
    "data/2011-06-24_11-26-36-small.jpg" "photo005s.jpg" >cache n2o:copy
    "data/2011-06-27_19-33-04-small.jpg" "photo006s.jpg" >cache n2o:copy
    "data/2011-06-27_19-55-48-small.jpg" "photo007s.jpg" >cache n2o:copy
    "data/2011-06-28_06-54-09-small.jpg" "photo008s.jpg" >cache n2o:copy
    n2o:done
    send-chunks
    end-code
    1 client-loop n2o:close-all ['] .time $err ;

: c:download3 ( -- )
    [: ." Download test 3: 2 big photos" cr ;] $err
    net2o-code
    expect-reply close-all \ rewind-total
    s" Download test 3" $, type cr  ( see-me )
    $10000 blocksize! $400 blockalign! stat( request-stats )
    s" data/2011-05-13_11-26-57.jpg" s" .cache/photo000.jpg" n2o:copy
    s" data/2011-05-20_17-01-12.jpg" s" .cache/photo001.jpg" n2o:copy
    n2o:done
    send-chunks
    end-code
    1 client-loop n2o:close-all ['] .time $err ;

: c:download4 ( -- )
    [: ." Download test 4: 7 big photos, partial files" cr ;] $err
    net2o-code
    expect-reply close-all \ rewind-total
    s" Download test 4" $, type cr  ( see-me )
    $10000 blocksize! $400 blockalign! stat( request-stats )
    "data/2011-06-02_15-02-38.jpg" "photo002.jpg" >cache n2o:copy
    "data/2011-06-03_10-26-49.jpg" "photo003.jpg" >cache n2o:copy
    "data/2011-06-15_12-27-03.jpg" "photo004.jpg" >cache n2o:copy
    "data/2011-06-24_11-26-36.jpg" "photo005.jpg" >cache n2o:copy
    "data/2011-06-27_19-33-04.jpg" "photo006.jpg" >cache n2o:copy
    "data/2011-06-27_19-55-48.jpg" "photo007.jpg" >cache n2o:copy
    "data/2011-06-28_06-54-09.jpg" "photo008.jpg" >cache n2o:copy
    $10000 ulit, 0 ulit, track-limit
    $20000 ulit, 1 ulit, track-limit
    $30000 ulit, 2 ulit, track-limit
    $40000 ulit, 3 ulit, track-limit
    $50000 ulit, 4 ulit, track-limit
    $60000 ulit, 5 ulit, track-limit
    $70000 ulit, 6 ulit, track-limit
    n2o:done
    send-chunks
    end-code
    1 client-loop ['] .time $err ;

: c:download4a ( -- )
    [: ." Download test 4a: 7 big photos, rest" cr ;] $err
    net2o-code
    expect-reply
    s" Download test 4a" $, type cr  ( see-me )
    -1 nlit, 0 ulit, track-limit
    -1 nlit, 1 ulit, track-limit
    -1 nlit, 2 ulit, track-limit
    -1 nlit, 3 ulit, track-limit
    -1 nlit, 4 ulit, track-limit
    -1 nlit, 5 ulit, track-limit
    -1 nlit, 6 ulit, track-limit
    slurp send-chunks
    end-code
    1 client-loop n2o:close-all ['] .time $err ;

: c:downloadend ( -- )    
    net2o-code s" Download end" $, type cr .time close-all disconnect  end-code ;

: c:test
    init-cache'
    c:connect
    c:download1
    3e @time f> IF c:download2
	waitkey( 8e )else( 6e ) @time f> IF  c:download3
	    waitkey( 16e )else( 8e ) @time f> IF
		waitkey( ." Press key to continue" key drop cr )
		c:download4
		c:download4a
	    THEN
	THEN
    THEN
    c:downloadend [: .packets .times cr ;] $err
    >timing n2o:dispose-context ;

event: ->throw dup DoError throw ;

: c:test& ( -- ) \ in background
    up@ 1 stacksize4 NewTask4 pass >r
    alloc-io ['] c:test catch ?dup-IF
	elit, ->throw  ELSE  ->request  THEN  r> event> ;

#100 Value req-ms#

: c:tests ( n -- )
    dup to total-tests  dup requests !
    0 ?DO  c:test& req-ms# ms test# 1+ to test#  LOOP
    requests->0 ;

?nextarg [IF] s>number drop [ELSE] 1 [THEN] c:tests

script? [IF] bye [THEN]
