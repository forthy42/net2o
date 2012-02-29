\ net2o tests - client side

require net2o.fs

pkc skc crypto_box_keypair \ create a random key pair

init-client

next-arg argc @ 1 > [IF] next-arg s>number drop [ELSE] net2o-port [THEN]
insert-ip n2o:new-context

\ 10 ms
." Sending first test" cr
net2o-code S" This is the first test" $, type '!' lit, emit cr
end-code 0cmd

\ 10 ms
." Sending second test" cr
net2o-code S" This is the second test" $, type '!' lit, emit cr
end-code 0cmd

\ 10 ms
." Sending more complex test" cr
( rng@ $20 lshift ) 0 Constant uniq#
uniq# $10000 + Constant code#
uniq# $20000 + Constant data#

net2o-code new-context
S" This is a test" $, type '!' lit, emit cr
pks send-key
data# lit, $400000 lit, new-data
code# lit, $2000 lit, new-code
end-code 0cmd

data# $400000 n2o:new-data
code# $2000 n2o:new-code
data# $400000 net2o:unacked

net2o-code
data-ivs code-ivs
s" net2o.fs" $, r/o lit, 0 lit, open-file
s" file size: " $, type 0 lit, file-size . cr
0 lit, slurp-chunk send-chunks
0 lit, close-file
s" data/2011-05-13_11-26-57.jpg" $, r/o lit, 0 lit, open-file
s" file size: " $, type 0 lit, file-size . cr
0 lit, slurp-chunk send-chunks
0 lit, close-file
end-code code# send-cmd

ticks client-loop ticks - negate poll-timeout# - s>f 1e-9 f* f. ." s" cr
." IP4 packets received: " packet4r ? cr
." IP4 packets sent:     " packet4s ? cr
." IP6 packets received: " packet6r ? cr
." IP6 packets sent:     " packet6s ? cr

bye
