\ net2o tests - client side

require client-tests.fs
require test-keys.fs \ we want the test keys - never use this in production!

+db stat( \ )
+debug
%droprate
debug-task

i'm anonymous

init-client

"test" connect-nick $!

!time
cmd-args
?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]
?nextarg [IF] s>number drop [ELSE] 1 [THEN] c:tests

script? [IF] bye [THEN]
