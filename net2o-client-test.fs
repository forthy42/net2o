\ net2o tests - client side

require net2o.fs

pkc skc crypto_box_keypair \ create a random key pair

init-client

1 arg net2o-udp insert-ipv4 constant lserver
shift-args
lserver return-addr !
n2o:new-context constant lcontext
lcontext job-context !

\ 10 ms
." Sending first test" cr
net2o-code S" This is the first test" $, type '!' char, emit cr
end-code lserver 0 send-cmd

\ 10 ms
." Sending second test" cr
net2o-code S" This is the second test" $, type '!' char, emit cr
end-code lserver 0 send-cmd

\ 10 ms
." Sending more complex test" cr
net2o-code new-context
S" This is a test" $, type '!' char, emit cr
pks send-key
$80000 lit, $80000 lit, new-map
$10000 lit, $1000 lit, new-code-map
$80000 lit, $80000 lit, new-data
$10000 lit, $1000 lit, new-code
s" net2o.fs" $, r/o lit, 0 lit, open-file
s" file size: " $, type 0 lit, file-size . cr
0 lit, slurp-chunk send-chunks
0 lit, close-file
s" doc/internet-2.0.pdf" $, r/o lit, 0 lit, open-file
s" file size: " $, type 0 lit, file-size . cr
0 lit, slurp-chunk send-chunks
0 lit, close-file
end-code lserver 0 send-cmd

$80000 $80000 n2o:new-map
$10000 $1000 n2o:new-code-map
$80000 $80000 n2o:new-data
$10000 $1000 n2o:new-code
$80000 $80000 net2o:unacked
client-loop
." IP4 packets received: " packet4r ? cr
." IP4 packets sent:     " packet4s ? cr
." IP6 packets received: " packet6r ? cr
." IP6 packets sent:     " packet6s ? cr

bye
