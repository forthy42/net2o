\ net2o tests - client side

require net2o.fs

+debug

gen-keys \ create a random key pair

init-client

s" .cache" file-status nip #-514 = [IF]
    s" .cache" $1FF =mkdir throw
[THEN]

!time

$8000 $100000
argc @ 1 > [IF] next-arg [ELSE] "net2o.de" [THEN] \ default
argc @ 1 > [IF] next-arg s>number drop [ELSE] net2o-port [THEN]
insert-ip n2o:connect +flow-control +resend

." Connected" cr

net2o-code
expect-reply
data-ivs time-offset!
s" Download test" $, type cr
$400 blocksize! $400 blockalign! stat( request-stats )
s" net2o.fs" s" .cache/net2o.fs" n2o:copy
s" data/2011-05-13_11-26-57-small.jpg" s" .cache/photo000s.jpg" n2o:copy
s" data/2011-05-20_17-01-12-small.jpg" s" .cache/photo001s.jpg" n2o:copy
n2o:done
send-chunks
end-code

1 client-loop .time cr
@time 2e f< [IF]
    ." Request more photos because it was so fast" cr
    net2o-code
    expect-reply
    s" Download test 2" $, type cr
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

1 client-loop .time cr
[THEN]

@time 5e f< [IF]
    ." Request big photos because it was so fast" cr
    net2o-code
    expect-reply
    s" Download test 3" $, type cr
    $10000 blocksize! $400 blockalign! stat( request-stats )
    s" data/2011-05-13_11-26-57.jpg" s" .cache/photo000.jpg" n2o:copy
    s" data/2011-05-20_17-01-12.jpg" s" .cache/photo001.jpg" n2o:copy
    n2o:done
    send-chunks
end-code

1 client-loop .time cr
[THEN]


." IP packets send/received: " packets ? packetr ? cr
.times

net2o-code .time end-code

bye
