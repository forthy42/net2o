\ net2o tests - client side

require net2o.fs

pkc skc crypto_box_keypair \ create a random key pair

init-client

$8000 $100000
next-arg argc @ 1 > [IF] next-arg s>number drop [ELSE] net2o-port [THEN]
insert-ip n2o:connect

." Connected" cr

net2o-code
data-ivs
s" Download test" $, type cr
s" net2o.fs" $, r/o lit, 0 lit, open-tracked-file
0 lit, -1 slit, 0 lit, slurp-tracked-block
s" .cache/net2o.fs" 0 save-to
s" data/2011-05-13_11-26-57.jpg" $, r/o lit, 1 lit, open-tracked-file
0 lit, -1 slit, 1 lit, slurp-tracked-block
s" .cache/photo.jpg" 1 save-to
send-chunks
end-code

ticks 1 client-loop ticks - negate s>f 1e-9 f* f. ." s" cr
." IP4 packets send/received: " packet4s ? packet4r ? cr
." IP6 packets send/received: " packet6s ? packet6r ? cr

bye
