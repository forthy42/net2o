\ net2o tests - client side

require client-tests.fs

+db stat(
+debug
%droprate
debug-task
test-keys \ we want the test keys - never use this in production!

i'm bob

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

: c:revoke-bob ( -- )
    o >o me>d#id o> now>never
    x" D82AF4AE7CD3DA7316CE6F26BC5792F4F5E6B36B4C14F7D60C49B421AE1D5468"
    revoke-me ;

: c:bob ( -- ) 
    $2000 $10000 "test" ins-ip dup add-beacon c:connect
    ." Bob connected" cr
    c:revoke-bob
    ." Bob revoked" cr
    o >o me>d#id o> replace-me
    ." Bob replaced" cr
    do-disconnect ;

c:bob server-loop
\ ?nextarg [IF] s>number drop [ELSE] 1 [THEN] c:tests

script? [IF] bye [THEN]
