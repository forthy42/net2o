\ net2o tests - msg

require ../client-tests.fs
require ../test-keys.fs \ we want the test keys - never use this in production!

+db stat(
script? [IF] +debug %droprate [THEN]

i'm alice

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

: c:msg-test ( -- )
    [: .time ." Message test" cr ;] $err
    "This is a test message" send-text
    "This is a second test message" send-text
    pad 100 accept pad swap send-text
    ['] .time $err ;

script? [IF] $a $e "test" ins-ip c:connect c:msg-test c:disconnect bye [THEN]
