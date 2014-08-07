\ test file for net2o - server side

require client-tests.fs \ test framework

+debug
%droprate
bg( )else( debug-task )
test-keys \ we want the test keys - never use this in production!

?nextarg [IF] s>number drop to net2o-port [THEN]

i'm test
strict-keys off \ server shouldn't have strict keys
init-server
server-loop

