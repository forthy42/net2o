\ net2o tests - client side

require net2o.fs
require client-tests.fs

+db stat(
+debug
%droprate
debug-task
test-keys \ we want the test keys - never use this in production!

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

"alice" >key \ get our anonymous key

: c:alice ( -- )
    $2000 $10000 "test" c:connect
    c:add-me "bob" c:fetch-host ;

c:alice
\ ?nextarg [IF] s>number drop [ELSE] 1 [THEN] c:tests

script? [IF] bye [THEN]
