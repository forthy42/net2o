\ test file for net2o - server side

require client-tests.fs \ test framework

+debug
%droprate
bg( )else( debug-task )
test-keys \ we want the test keys - never use this in production!

?nextarg [IF] s>number drop to net2o-port [THEN]

"test" >key \ use our server test key
init-server
server-loop

