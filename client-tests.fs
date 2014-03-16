\ Test lib clients

require ./net2o.fs

-1 UValue test#
1 Value total-tests

: .test# ( -- ) test# 0>= IF  test# 0 .r  THEN ;

: >timing ( -- )
    [: ." timing" .test# ;] $tmp w/o create-file throw >r
    ['] .rec-timing r@ outfile-execute r> close-file throw ;

: >cache ( addr u -- addr' u' ) [: ." .cache" .test# ." /" type ;] $tmp ;

: init-cache' ( -- )
    "" >cache 1- file-status nip #-514 = IF
	"" >cache 1- $1FF =mkdir throw
    THEN ;

: test-keys ( -- ) \ yes, use these keys *only* for testing!
    x" 21C1630881777E0EE03D49465F954AC894EC4D76C025BEF314BAC285DDD545B5" key:new
    x" 103D146E41B255702B33852CC6EFD93ED0F741D75D3676FDF987F269B69BCE77" ke-sk $! +seckey
    "test" ke-nick $! $133377705D76F4EA. d>64 ke-first 64!
    x" F2C614B1780CB86B6407D0AA0DE99449A7EA484AC1D2D0ADA8B9D092C7B3C497" key:new
    x" F8901E8A71FEA32B4E18656E68986EC414A5782935F5CF504998E79B507F1B59" ke-sk $! +seckey
    "anonymous" ke-nick $! $133377705D8E53AD. d>64 ke-first 64!
    x" 9D7D7A65175FCC38384C3B413FC8C0917684C059D3C43C3DE870E075E66C02C9" key:new
    x" C0A687714048267E1CD78AEC595FBD9481A4175508DDED52B6A37188C0ABD75F" ke-sk $! +seckey
    "alice" ke-nick $! $135BB8EA6D3F85E5. d>64 ke-first 64!
    x" 952D6361FFB37EFADA457E32501469B9C4383CC278B7B5D35A87888F7BE7718B" key:new
    x" 503B4FCBD059094C153BCCA74772E97B24234D3C41CE285A34391B7F14241B4C" ke-sk $! +seckey
    "bob" ke-nick $! $135BB8FC1BDC81C5. d>64 ke-first 64!
    x" 4999BFE87C16A2F205C248520CB649852B44AE45707A77958743C22F671892A3" key:new
    x" D8813EAB8FD4C2028CCBFF29F88325E915C8620190D94BB467F9341D918B4362" ke-sk $! +seckey
    "eve" ke-nick $! $135BB908EA37210E. d>64 ke-first 64!
;

: c:connect ( code data nick u -- )
    net2o-host $@ net2o-port insert-ip
    [: .time ." Connect to: " dup hex. cr ;] $err
    n2o:new-context
    dest-key \ get our destination key
    n2o:connect +flow-control +resend
    [: .time ." Connected, o=" o hex. cr ;] $err ;

: c:add-me ( -- )  +addme
    net2o-code   expect-reply get-ip  end-code
    1 client-loop -setip o-timeout ;

: c:add-tag ( -- ) +addme
    net2o-code
    expect-reply
    s" DHT test" $, type cr get-ip
    pkc keysize $, dht-id
    forever "test:tag" pkc keysize gen-tag-del $, k#tags ulit, dht-value-
    forever "test:tag" pkc keysize gen-tag $, k#tags ulit, dht-value+
    end-code  1 client-loop -setip o-timeout ;

: c:fetch-tag ( nick u -- )
    net2o-code
    expect-reply
    0 >o nick-key ke-pk $@ o> $, dht-id
    k#host ulit, dht-value? k#tags ulit, dht-value?
    nest[ add-cookie lit, set-rtdelay request-done ]nest
    end-code  1 client-loop o-timeout ;

: c:fetch-host ( nick u -- )
    net2o-code
    expect-reply
    0 >o nick-key ke-pk $@ o> $, dht-id
    k#host ulit, dht-value?
    nest[ add-cookie lit, set-rtdelay request-done ]nest
    end-code  1 client-loop o-timeout ;

: c:addme-fetch-host ( nick u -- ) +addme
    net2o-code
    expect-reply get-ip
    0 >o nick-key ke-pk $@ o> $, dht-id
    k#host ulit, dht-value?
    nest[ add-cookie lit, set-rtdelay request-done ]nest
    end-code  1 client-loop o-timeout ;

: c:fetch-tags ( -- )
    net2o-code
    expect-reply
    0 ulit, dht-open  pkc keysize $, $FE ulit, 0 ulit, dht-query
    slurp send-chunks
    end-code  1 client-loop o-timeout ;

: c:dhtend ( -- )    
    net2o-code s" DHT end" $, type cr .time disconnect  end-code
    o-timeout n2o:dispose-context ;

: c:dht ( n -- )  $2000 $10000 "test" c:connect 0 ?DO
	c:add-tag "anonymous" c:fetch-tag \ c:fetch-tags
    LOOP c:dhtend ;

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

: c:disconnect ( -- )  net2o-code close-all disconnect  end-code ;

: c:downloadend ( -- )    
    net2o-code s" Download end" $, type cr .time close-all disconnect  end-code ;

: c:test
    init-cache'
    $10000 $100000 "test" c:connect
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
    c:downloadend [: .packets profile( .times ) ;] $err
    >timing n2o:dispose-context ;

event: ->throw dup DoError throw ;

: c:test& ( -- ) \ in background
    up@ 1 stacksize4 NewTask4 pass >r
    alloc-io ['] c:test catch ?dup-IF
	elit, ->throw  ELSE  ->request  THEN  r> event> ;

#100 Value req-ms#

: c:tests ( n -- )  dup 0< IF  abs to test#  1  THEN
    dup to total-tests  dup requests !
    0 ?DO  c:test& req-ms# ms test# 1+ to test#  LOOP
    requests->0 ;

