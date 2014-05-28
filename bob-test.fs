\ net2o tests - client side

require client-tests.fs

+db stat(
+debug
%droprate
debug-task
test-keys \ we want the test keys - never use this in production!

"bob" >key \ get our bob key

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

: c:bob ( -- ) 
    $2000 $10000 "test" ins-ip4 dup add-beacon c:connect
    c:replace-me -timeout ;

c:bob server-loop
\ ?nextarg [IF] s>number drop [ELSE] 1 [THEN] c:tests

script? [IF] bye [THEN]
