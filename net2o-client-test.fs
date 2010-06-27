\ net2o tests - client side

init-client

s" localhost" net2o-udp insert-ipv4 constant lserver

s" Dies ist ein Test" drop 0 lserver sendA

\ Net2o code generator

net2o-code S" blabl" $, type 'a' char, emit end-code

cmdbuf @+ swap cmd-loop cr \ must display "blabla"