\ net2o tests - client side

require ../net2o.fs

+db stat(
+debug

"anonymous" >key \ get our anonymous key

init-client

!time

$8000 $100000
?nextarg [IF] net2o-host $@ [THEN] \ default
?nextarg [IF] net2o-port [ELSE] s>number drop [THEN]
insert-ip n2o:connect +flow-control +resend

." Connected, o=" o hex. cr

net2o-code
expect-reply
s" Download test" $, type cr ( see-me ) get-ip
$400 blocksize! $400 blockalign! stat( request-stats )
s" net2o.fs" s" .cache/net2o.fs" n2o:copy
s" data/2011-05-13_11-26-57-small.jpg" s" .cache/photo000s.jpg" n2o:copy
s" data/2011-05-20_17-01-12-small.jpg" s" .cache/photo001s.jpg" n2o:copy
n2o:done
send-chunks
end-code

1 client-loop n2o:close-all .time cr
2e @time f> [IF]
    ." Request more photos because it was so fast" cr
    net2o-code
    expect-reply
    s" Download test 2" $, type cr ( see-me )
    $10000 blocksize! $400 blockalign! stat( request-stats )
    "data/2011-06-02_15-02-38-small.jpg" ".cache/photo002s.jpg" n2o:copy
    "data/2011-06-03_10-26-49-small.jpg" ".cache/photo003s.jpg" n2o:copy
    "data/2011-06-15_12-27-03-small.jpg" ".cache/photo004s.jpg" n2o:copy
    "data/2011-06-24_11-26-36-small.jpg" ".cache/photo005s.jpg" n2o:copy
    "data/2011-06-27_19-33-04-small.jpg" ".cache/photo006s.jpg" n2o:copy
    "data/2011-06-27_19-55-48-small.jpg" ".cache/photo007s.jpg" n2o:copy
    "data/2011-06-28_06-54-09-small.jpg" ".cache/photo008s.jpg" n2o:copy
    n2o:done
    send-chunks
end-code

1 client-loop n2o:close-all .time cr
4e
[ELSE]
    bye
[THEN]
waitkey( fdrop 8e )
@time f> [IF]
    ." Request big photos because it was so fast" cr
    net2o-code
    expect-reply
    s" Download test 3" $, type cr  ( see-me )
    $10000 blocksize! $400 blockalign! stat( request-stats )
    s" data/2011-05-13_11-26-57.jpg" s" .cache/photo000.jpg" n2o:copy
    s" data/2011-05-20_17-01-12.jpg" s" .cache/photo001.jpg" n2o:copy
    n2o:done
    send-chunks
end-code

1 client-loop n2o:close-all .time cr
8e
[ELSE]
    bye
[THEN]
waitkey( fdrop 16e )
@time f> [IF]
    waitkey( ." Press key to continue" key drop cr )
    ." Request more big photos because it was so fast" cr
    net2o-code
    expect-reply
    s" Download test 4" $, type cr  ( see-me )
    $10000 blocksize! $400 blockalign! stat( request-stats )
    "data/2011-06-02_15-02-38.jpg" ".cache/photo002.jpg" n2o:copy
    "data/2011-06-03_10-26-49.jpg" ".cache/photo003.jpg" n2o:copy
    "data/2011-06-15_12-27-03.jpg" ".cache/photo004.jpg" n2o:copy
    "data/2011-06-24_11-26-36.jpg" ".cache/photo005.jpg" n2o:copy
    "data/2011-06-27_19-33-04.jpg" ".cache/photo006.jpg" n2o:copy
    "data/2011-06-27_19-55-48.jpg" ".cache/photo007.jpg" n2o:copy
    "data/2011-06-28_06-54-09.jpg" ".cache/photo008.jpg" n2o:copy
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

1 client-loop .time cr

." Request second stage big photos" cr
net2o-code
expect-reply
s" Download test 4a" $, type cr  ( see-me )
\ $40000 ulit, 0 ulit, track-limit
\ $30000 0 n2o:seek
-1 nlit, 0 ulit, track-limit
-1 nlit, 1 ulit, track-limit
-1 nlit, 2 ulit, track-limit
-1 nlit, 3 ulit, track-limit
-1 nlit, 4 ulit, track-limit
-1 nlit, 5 ulit, track-limit
-1 nlit, 6 ulit, track-limit
gen-total slurp-all-tracked-blocks send-chunks
end-code

\ 1 client-loop .time cr
\ 
\ ." Request third stage big photos" cr
\ net2o-code
\ expect-reply
\ s" Download test 4b" $, type cr ( see-me )
\ $30000 ulit, 0 ulit, track-limit
\ $10000 0 n2o:seek
\ gen-total slurp-all-tracked-blocks send-chunks
\ end-code

1 client-loop n2o:close-all .time cr
[THEN]

.packets .times

net2o-code s" Download end" $, type cr .time disconnect  end-code

.rec-timing

o-timeout

script? [IF] bye [THEN]
