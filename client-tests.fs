\ Test lib clients

require ./net2o.fs

UValue test#  -1 to test#
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
    \ revoke: 58AB8F52F46E73EFAB068F6337F371E14DD589BF0894D2F0AF51AE7EBB858A68
    x" A91158F2C560ACCDFEFC05104B922E49C9DD022D0163921DAE08E6C2148A7BEBC83C71FCB345D24400D866C7FD32092C2D1EC056FD17B9537037590BD021EEBF" key:new
    x" B2578B8766DB3A60F1F4F36B276924FDA6E7F559F629716BC78D95DB1CD8D400" ke-sk $! +seckey
    "test" ke-nick $! $1367B086A24E6B10. d>64 ke-first 64! 0 ke-type !
    \ revoke: 5843E2DC055E1F8BE14570A37B0F81146040A2CEE1D6C01B97C3BB801CDED864
    x" 69D86C471E5FEED89478FB4260C898B6F69026BA4E78A9D815B53EB33CA9013A8E753EC381881FAAFFA66CD9DD47D3F2C0867E1A2B48067CA2188DF400C11074" key:new
    x" 5905350A6B4B5DE29C2CA4562BB105EF570713CE648E38F6FBBB6D076D141B0A" ke-sk $! +seckey
    "anonymous" ke-nick $! $1367B086A255C9C2. d>64 ke-first 64! 0 ke-type !
    \ revoke: 38A6FB42FF41A690A108DCA460CC0D15AE3C1C23FFFA9E92583FFD9FB16AD276
    x" 7A0FFD3D31ED822D683D685EA5689C91CB170B54A82F0E53554D34584F90DB017750513CDC1F1DC7F8F61214ED4BC801CF70C3D5FC90F716F2363038ACEE58BD" key:new
    x" AAB952DD5D1850F1B468EEF84F72552148070C3F499600FE362934970329FE04" ke-sk $! +seckey
    "alice" ke-nick $! $1367B086A25CEF70. d>64 ke-first 64! 1 ke-type !
    \ revoke: D82AF4AE7CD3DA7316CE6F26BC5792F4F5E6B36B4C14F7D60C49B421AE1D5468
    x" 1A20176C79D26402811945CFC241116BAFB52DD033492044DB5CFEECCA21E6E49F350B40A28D83B618361167D13B51A4EFCE919C7BB6BDCC570D9B7031A0428E" key:new
    x" 6B65577985D851753ACFFFFB00360C70C267420132204A17F4468D9CACDB010F" ke-sk $! +seckey
    "bob" ke-nick $! $1367B086A26436A9. d>64 ke-first 64! 1 ke-type !
    \ revoke: 7821DA41AFBB8F7356E2EB7059BE70321D7ADCDAD8C504998627CBB9366AB752
    x" 9483FBBB98A5BFE792206519FB2BAF9EE21FE863ABE981AB1C209123D40E1969EA7C68162DF5340142524D6BE3E407B065824D1E3582E6209CA03876F406EBCA" key:new
    x" 693D7EF6BF0E0CEFB0654EB95AB7C729B8799F850CAB24B1211116ED72EA3602" ke-sk $! +seckey
    "eve" ke-nick $! $1367B086A26B4E42. d>64 ke-first 64! 1 ke-type !
;

: ins-ip ( -- net2oaddr )
    net2o-host $@ net2o-port insert-ip ;
: ins-ip4 ( -- net2oaddr )
    net2o-host $@ net2o-port insert-ip4 ;
: ins-ip6 ( -- net2oaddr )
    net2o-host $@ net2o-port insert-ip6 ;

: c:connect ( code data nick u ret -- )
    [: .time ." Connect to: " dup hex. cr ;] $err
    n2o:new-context
    dest-key \ get our destination key
    n2o:connect +flow-control +resend
    [: .time ." Connected, o=" o hex. cr ;] $err ;

: c:add-me ( -- )  +addme
    net2o-code   expect-reply get-ip cookie+request  end-code
    client-loop -setip ;

: c:do-replace ( -- )
    pkc keysize 2* >d#id d#id @ IF
	net2o-code   expect-reply
	pkc keysize 2* $, dht-id
	d#id @ k#host cells +
	[: sigsize# - 2dup + sigdate datesize# move
	  gen-host-del $, k#host ulit, dht-value- ;] $[]map
	end-code
    THEN ;

also net2o-base
: replaceme, ( -- )
    pkc keysize 2* $, dht-id k#host ulit, dht-value? ;
previous

: c:replace-me ( -- )  +addme
    net2o-code   expect-reply get-ip replaceme, cookie+request
    end-code
    client-loop -setip c:do-replace ;

: c:add-tag ( -- ) +addme
    net2o-code
    expect-reply
    s" DHT test" $, type cr get-ip
    pkc keysize 2* $, dht-id
    forever "test:tag" pkc keysize 2* gen-tag-del $, k#tags ulit, dht-value-
    forever "test:tag" pkc keysize 2* gen-tag $, k#tags ulit, dht-value+
    end-code  client-loop -setip ;

: c:fetch-tag ( nick u -- )
    net2o-code
    expect-reply
    0 >o nick-key ke-pk $@ o> $, dht-id
    k#host ulit, dht-value? k#tags ulit, dht-value?
    cookie+request
    end-code  client-loop ;

also net2o-base
: fetch-host, ( nick u -- )
    0 >o nick-key ke-pk $@ o> $, dht-id
    k#host ulit, dht-value? ;
previous

: c:fetch-host ( nick u -- )
    net2o-code
    expect-reply  fetch-host,
    cookie+request
    end-code  client-loop ;

: c:addme-fetch-host ( nick u -- ) +addme
    net2o-code
    expect-reply get-ip fetch-host, replaceme,
    cookie+request
    end-code  client-loop c:do-replace ;

: c:fetch-tags ( -- )
    net2o-code
    expect-reply
    0 ulit, dht-open  pkc keysize 2* $, $FE ulit, 0 ulit, dht-query
    n2o:done
    end-code  client-loop ;

: c:dhtend ( -- )    
    net2o-code s" DHT end" $, type cr .time disconnect  end-code
    o-timeout n2o:dispose-context ;

: c:dht ( n -- )  $2000 $10000 "test" ins-ip c:connect 0 ?DO
	c:add-tag "anonymous" c:fetch-tag \ c:fetch-tags
    LOOP c:dhtend ;

: c:download1 ( -- )
    [: .time ." Download test: 1 text file and 2 photos" cr ;] $err
    net2o-code
    expect-reply
    !time .time s" Download test" $, type cr ( see-me ) get-ip
    $400 blocksize! $400 blockalign! stat( request-stats )
    "net2o.fs" "net2o.fs" >cache n2o:copy
    "data/2011-05-13_11-26-57-small.jpg" "photo000s.jpg" >cache n2o:copy
    "data/2011-05-20_17-01-12-small.jpg" "photo001s.jpg" >cache n2o:copy
    n2o:done
    end-code
    client-loop n2o:close-all ['] .time $err ;

: c:download2 ( -- )
    [: ." Download test 2: 7 medium photos" cr ;] $err
    net2o-code
    expect-reply close-all \ rewind-total
    .time s" Download test 2" $, type cr ( see-me )
    $10000 blocksize! $400 blockalign! stat( request-stats )
    "data/2011-06-02_15-02-38-small.jpg" "photo002s.jpg" >cache n2o:copy
    "data/2011-06-03_10-26-49-small.jpg" "photo003s.jpg" >cache n2o:copy
    "data/2011-06-15_12-27-03-small.jpg" "photo004s.jpg" >cache n2o:copy
    "data/2011-06-24_11-26-36-small.jpg" "photo005s.jpg" >cache n2o:copy
    "data/2011-06-27_19-33-04-small.jpg" "photo006s.jpg" >cache n2o:copy
    "data/2011-06-27_19-55-48-small.jpg" "photo007s.jpg" >cache n2o:copy
    "data/2011-06-28_06-54-09-small.jpg" "photo008s.jpg" >cache n2o:copy
    n2o:done
    end-code
    client-loop n2o:close-all ['] .time $err ;

: c:download3 ( -- )
    [: ." Download test 3: 2 big photos" cr ;] $err
    net2o-code
    expect-reply close-all \ rewind-total
    .time s" Download test 3" $, type cr ( see-me )
    $10000 blocksize! $400 blockalign! stat( request-stats )
    "data/2011-05-13_11-26-57.jpg" "photo000.jpg" >cache n2o:copy
    "data/2011-05-20_17-01-12.jpg" "photo001.jpg" >cache n2o:copy
    n2o:done
    end-code
    client-loop n2o:close-all ['] .time $err ;

: c:download4 ( -- )
    [: ." Download test 4: 7 big photos, partial files" cr ;] $err
    net2o-code
    expect-reply close-all \ rewind-total
    .time s" Download test 4" $, type cr ( see-me )
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
    end-code
    client-loop ['] .time $err ;

: c:download4a ( -- )
    [: ." Download test 4a: 7 big photos, rest" cr ;] $err
    net2o-code
    expect-reply
    .time s" Download test 4a" $, type cr  ( see-me )
    -1 nlit, 0 ulit, track-limit
    -1 nlit, 1 ulit, track-limit
    -1 nlit, 2 ulit, track-limit
    -1 nlit, 3 ulit, track-limit
    -1 nlit, 4 ulit, track-limit
    -1 nlit, 5 ulit, track-limit
    -1 nlit, 6 ulit, track-limit
    n2o:done
    end-code
    client-loop n2o:close-all ['] .time $err ;

: c:disconnect ( -- )  net2o-code close-all disconnect  end-code ;

: c:downloadend ( -- )    
    net2o-code .time s" Download end" $, type cr close-all disconnect  end-code
    o-timeout ;

: c:test-rest ( -- )
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

: c:test ( -- )
    init-cache'
    $10000 $100000 "test" ins-ip c:connect c:test-rest ;

event: ->throw dup DoError throw ;

: c:test& ( n -- ) \ in background
    up@ 2 stacksize4 NewTask4 pass >r
    alloc-io ['] c:test catch ?dup-IF
	elit, ->throw drop  ELSE  elit, ->request  THEN  r> event> ;

#100 Value req-ms#

: c:tests ( n -- )  dup 0< IF  abs to test#  1  THEN
    dup to total-tests  1 over lshift 1- reqmask !
    0 ?DO  I c:test& req-ms# ms test# 1+ to test#  LOOP
    requests->0 ;

