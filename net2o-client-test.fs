\ net2o tests - client side

init-client

s" localhost" net2o-udp insert-ipv4 constant lserver

net2o-code S" This is a test" $, type '!' char, emit cr
end-code lserver 0 send-cmd

net2o-code new-context $8000 lit, $8000 lit, new-map
$8000 lit, $8000 lit, new-data
$10000 lit, $1000 lit, new-code
s" net2o.fs" $, r/o lit, 0 lit, open-file
s" file size: " $, type 0 lit, file-size . cr
end-code lserver 0 send-cmd

