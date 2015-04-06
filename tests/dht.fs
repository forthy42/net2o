\ net2o tests - client side

require ../client-tests.fs
require ../test-keys.fs \ we want the test keys - never use this in production!

+db stat( \ )
+debug
+db dht( \ )

"anonymous" >key \ get our anonymous key

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

script? [IF] 1 c:dht bye [THEN]
