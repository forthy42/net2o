\ test file for net2o - server side

require client-tests.fs \ test framework
require test-keys.fs \ we want the test keys - never use this in production!

+debug
%droprate
\ bg( )else( debug-task )

?nextarg [IF] s>number drop to net2o-port [THEN]

i'm test
strict-keys off \ server shouldn't have strict keys
init-server
server-loop

