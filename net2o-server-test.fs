\ test file for net2o - server side

require net2o.fs

argc @ 1 > [IF]  1 arg s>number drop to bandwidth-init shift-args [THEN]

init-server
server-loop

