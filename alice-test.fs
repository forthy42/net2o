\ net2o tests - client side

require net2o.fs
require client-tests.fs

+db stat(
+debug
%droprate
script? [IF] debug-task [THEN]
test-keys \ we want the test keys - never use this in production!

i'm alice

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

\ ?nextarg [IF] s>number drop [ELSE] 1 [THEN] c:tests

script? [IF] "bob" nat:connect c:test-rest bye [THEN]
