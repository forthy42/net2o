\ net2o tests - client side

init-client

s" localhost" net2o-udp insert-ipv4 constant lserver

net2o-code S" This is a test" $, type '!' char, emit cr end-code

cmdbuf cell+ 0 lserver sendA

\ Net2o code generator
