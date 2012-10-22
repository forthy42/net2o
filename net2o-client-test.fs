\ net2o tests - client side

require net2o.fs

+debug

gen-keys \ create a random key pair

init-client

$8000 $100000
next-arg argc @ 1 > [IF] next-arg s>number drop [ELSE] net2o-port [THEN]
insert-ip n2o:connect +flow-control +resend

." Connected" cr

net2o-code
expect-reply
data-ivs time-offset!
s" Download test" $, type cr !time
$400 blocksize! $400 blockalign! stat( request-stats )
s" net2o.fs" s" .cache/net2o.fs" n2o:copy
s" data/2011-05-13_11-26-57.jpg" s" .cache/photo000.jpg" n2o:copy
s" data/2011-05-20_17-01-12.jpg" s" .cache/photo001.jpg" n2o:copy
n2o:done
send-chunks
end-code

ticks 1 client-loop ticks 64- 64negate 64>f 1e-9 f* f. ." s" cr
." IP packets send/received: " packets ? packetr ? cr
.times

net2o-code .time end-code

bye
