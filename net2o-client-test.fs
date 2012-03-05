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
S" Connection init" $, type cr
pks send-key
data# $200000 data-map,
code# $2000   code-map,
code-ivs
end-code 0cmd

net2o-code
cmd:
data-ivs
s" Download test" $, type cr
s" net2o.fs" $, r/o lit, 0 lit, open-tracked-file
0 lit, -1 slit, 0 lit, slurp-tracked-block
s" data/2011-05-13_11-26-57.jpg" $, r/o lit, 1 lit, open-tracked-file
0 lit, -1 slit, 1 lit, slurp-tracked-block
send-chunks
cmd;
end-code scmd

ticks client-loop ticks - negate poll-timeout# - s>f 1e-9 f* f. ." s" cr
." Data total/expected/received: " j^ total ? j^ expected ? j^ received ? cr
." IP4 packets send/received: " packet4s ? packet4r ? cr
." IP6 packets send/received: " packet6s ? packet6r ? cr

bye
