\ test file for net2o - server side

require net2o.fs

argc @ 1 > [IF]  1 arg s>number drop to net2o-port shift-args [THEN]

init-server
server-loop

