\ Test lib clients

require ../net2o.fs

UValue test#  0 to test#
1 Value total-tests

: .test# ( -- ) test# 0>= IF  test# 0 .r  THEN ;

: >timing ( -- )
    [: ." timing" .test# ;] $tmp w/o create-file throw >r
    ['] .rec-timing r@ outfile-execute
    r> close-file throw ;

: >cache ( addr u -- addr' u' ) [: ." .cache" .test# ." /" type ;] $tmp ;

: init-cache' ( -- )
    "" >cache 1- file-status nip no-file# = IF
	"" >cache 1- $1FF =mkdir throw
    THEN ;

: c:add-me ( -- )  +addme
    net2o-code   expect-reply get-ip cookie+request  end-code| -setip ;

: c:add-tag ( -- ) +addme
    net2o-code
      expect-reply
      log s" DHT test" $, type cr endwith get-ip
      pkc keysize 2* $, dht-id
      forever "test:tag" pkc keysize 2* gen-tag-del $, dht-tags-
      forever "test:tag" pkc keysize 2* gen-tag $, dht-tags+
      endwith end-code| -setip ;

: c:fetch-tag ( nick u -- )
    net2o-code
      expect-reply
      nick-key .ke-pk $@ $, dht-id dht-host? dht-tags?
      endwith cookie+request
    end-code| ;

: c:fetch-host ( nick u -- )
    net2o-code
      expect-reply  fetch-host,
      cookie+request
    end-code| ;

\ : c:fetch-tags ( -- )
\     net2o-code
\       expect-reply
\       0 ulit, dht-open  pkc keysize 2* $, $FE ulit, 0 ulit, dht-query
\       n2o:done
\     end-code| ;

Variable connect-nick  "test" connect-nick $!

: c:dht ( n -- )  $8 $8 connect-nick $@ nick>pk ins-ip pk:connect 0 ?DO
	c:add-tag "anonymous" c:fetch-tag \ c:fetch-tags
    LOOP  disconnect-me ;

: std-block ( -- ) $10 blocksize! $A blockalign! ;

: c:download1 ( -- )
    [: .time ." Download test: 1 text file and 2 photos" cr ;] $err
    net2o-code
      expect-reply
      log !time .time s" Download test " $, type 1 ulit, . pi float, f. cr endwith
      get-ip
      std-block stat( request-stats )
      "net2o.fs" "net2o.fs" >cache n2o:copy
      "data/2011-05-13_11-26-57-small.jpg" "photo000s.jpg" >cache n2o:copy
      "data/2011-05-20_17-01-12-small.jpg" "photo001s.jpg" >cache n2o:copy
      n2o:done push' log 0 ulit, words push' cr push' endwith
    end-code| n2o:close-all ['] .time $err ;

: c:download2 ( -- )
    [: ." Download test 2: 7 medium photos" cr ;] $err
    net2o-code
      expect-reply close-all \ rewind-total
      log .time s" Download test 2" $, type cr endwith
      std-block stat( request-stats )
      "data/2011-06-02_15-02-38-small.jpg" "photo002s.jpg" >cache n2o:copy
      "data/2011-06-03_10-26-49-small.jpg" "photo003s.jpg" >cache n2o:copy
      "data/2011-06-15_12-27-03-small.jpg" "photo004s.jpg" >cache n2o:copy
      "data/2011-06-24_11-26-36-small.jpg" "photo005s.jpg" >cache n2o:copy
      "data/2011-06-27_19-33-04-small.jpg" "photo006s.jpg" >cache n2o:copy
      "data/2011-06-27_19-55-48-small.jpg" "photo007s.jpg" >cache n2o:copy
      "data/2011-06-28_06-54-09-small.jpg" "photo008s.jpg" >cache n2o:copy
      n2o:done push' log log $20 ulit, words push' cr endwith
    end-code| n2o:close-all ['] .time $err ;

: c:download3 ( -- )
    [: ." Download test 3: 2 big photos" cr ;] $err
    net2o-code
      expect-reply close-all \ rewind-total
      log .time s" Download test 3" $, type cr endwith
      std-block stat( request-stats )
      "data/2011-05-13_11-26-57.jpg" "photo000.jpg" >cache n2o:copy
      "data/2011-05-20_17-01-12.jpg" "photo001.jpg" >cache n2o:copy
      n2o:done 0 ulit, file-id
      push' endwith push' log $20 ulit, words push' cr endwith
    end-code| n2o:close-all ['] .time $err ;

: c:download4 ( -- )
    [: ." Download test 4: 7 big photos, partial files" cr ;] $err
    net2o-code
      expect-reply close-all \ rewind-total
      log .time s" Download test 4" $, type cr endwith
      std-block stat( request-stats )
      "data/2011-06-02_15-02-38.jpg" "photo002.jpg" >cache n2o:copy
      "data/2011-06-03_10-26-49.jpg" "photo003.jpg" >cache n2o:copy
      "data/2011-06-15_12-27-03.jpg" "photo004.jpg" >cache n2o:copy
      "data/2011-06-24_11-26-36.jpg" "photo005.jpg" >cache n2o:copy
      "data/2011-06-27_19-33-04.jpg" "photo006.jpg" >cache n2o:copy
      "data/2011-06-27_19-55-48.jpg" "photo007.jpg" >cache n2o:copy
      "data/2011-06-28_06-54-09.jpg" "photo008.jpg" >cache n2o:copy
      $10000. 0 limit!
      $20000. 1 limit!
      $30000. 2 limit!
      $40000. 3 limit!
      $50000. 4 limit!
      $60000. 5 limit!
      $70000. 6 limit!
      n2o:done "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" $, dht-id
      push' endwith push' log $20 ulit, words push' cr endwith
    end-code| ['] .time $err ;

: c:download4a ( -- )
    [: ." Download test 4a: 7 big photos, rest" cr ;] $err
    net2o-code
      expect-reply
      log .time s" Download test 4a" $, type cr endwith
      7 0 DO  -1. I limit!  LOOP
      n2o:done
    end-code| ['] .time $err n2o:close-all ;

: c:test-rest ( -- )
    c:download1
    7e @time f> IF c:download2
	waitkey( 8e )else( 15e ) @time f> IF  c:download3
	    waitkey( 16e )else( 20e ) @time f> IF
		waitkey( ." Press key to continue" key drop cr )
		c:download4
		c:download4a
	    THEN
	THEN
    THEN
    >timing c:disconnect ;

: c:test ( -- )
    init-cache'
    $a $e connect-nick $@ nick>pk ins-ip pk:connect c:test-rest ;

Variable reqdone#
event: ->reqdone -1 reqdone# +! ;

: c:test& ( n -- ) \ in background
    up@ 2 stacksize4 NewTask4 pass >r
    alloc-io ['] c:test catch ?dup-IF
	elit, ->throw drop
    ELSE  drop ->reqdone  THEN  r> event> ;

#100 Value req-ms#

: c:tests ( n -- )  dup 0< IF  abs to test#  1  THEN
    dup to total-tests  dup reqdone# !
    0 ?DO  I c:test& req-ms# ms test# 1+ to test#  LOOP
    BEGIN  stop reqdone# @ 0= UNTIL ;

\ lookup for other users

: nat:connect ( addr u -- )  $A $E nick-connect
    ." Connected!" cr ;

\ some more helpers

: sha-3 ( addr u -- ) c:0key
    slurp-file 2dup c:hash drop free throw pad c:key> ;

: sha-3-256 ( addr u -- )  sha-3 pad $20 85type ;
: sha-3-512 ( addr u -- )  sha-3 pad $40 85type ;

: sha-3-256s ( -- )
    [: 2dup sha-3-256 space type cr ;] arg-loop ;

: sha-3-512s ( -- )
    [: 2dup sha-3-512 space type cr ;] arg-loop ;

\ terminal connection

: c:terminal ( -- )
    $a $e connect-nick $@ nick>pk ins-ip pk:connect
    [: .time ." Terminal test: connect to server" cr ;] $err
    tc-permit# fs-class-permit or to fs-class-permit
    net2o-code
    expect-reply
      log .time "Terminal test" $, type cr endwith
      std-block stat( request-stats )
      [: 3 ulit, file-type  "" $, 0 ulit, open-file
	state-addr >o 2 fs-class! o> ;] n2o>file
    end-code| ['] .time $err ;