\ net2o tests - client side

require client-tests.fs
require test-keys.fs \ we want the test keys - never use this in production!

+db stat(
+debug
%droprate
script? [IF] debug-task [THEN]

i'm alice

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

\ ?nextarg [IF] s>number drop [ELSE] 1 [THEN] c:tests

script? [IF] "bob" nat:connect c:test-rest bye [THEN]
