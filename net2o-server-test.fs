\ test file for net2o - server side

require net2o.fs

+debug

argc @ 1 > [IF]  1 arg s>number drop to net2o-port shift-args [THEN]

"test" >key-name ?keypair \ use our server test key
init-server
server-loop

