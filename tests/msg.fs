\ net2o tests - msg

require ../client-tests.fs

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

: c:msg-test ( -- )
    [: .time ." Message test" cr ;] $err
    "This is a test message" send-text
    ['] .time $err
    do-disconnect [: .packets profile( .times ) ;] $err ;

script? [IF] "bob" nat:connect c:msg-test bye [THEN]
