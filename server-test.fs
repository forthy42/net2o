\ test file for net2o - server side

require net2o.fs

+debug

?nextarg [IF] s>number drop to net2o-port [THEN]

"test" >key \ use our server test key
init-server
server-loop

