\ net2o tests - client side

require ../client-tests.fs

+db stat(
+debug
+db dht(

test-keys \ we want the test keys - never use this in production!
"anonymous" >key \ get our anonymous key

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

script? [IF] 1 c:dht bye [THEN]
