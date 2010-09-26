\ net2o tests - client side

init-client

s" localhost" net2o-udp insert-ipv4 constant lserver

net2o-code S" This is a test" $, type '!' char, emit cr
end-code lserver 0 send-cmd

net2o-code new-context s" net2o.fs" $, r/o lit, 0 lit, open-file
end-code lserver 0 send-cmd
\ Net2o code generator
