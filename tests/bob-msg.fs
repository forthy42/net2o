\ net2o tests - msg

require ../client-tests.fs
require ../test-keys.fs \ we want the test keys - never use this in production!

+db stat(
script? [IF] +debug %droprate [THEN]

i'm bob

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

: c:msg-test ( -- )
    [: .time ." Message test" cr ;] $err
    "Hi Alice!" "alice" send-text-to o-timeout
    BEGIN  pad 100 accept cr dup WHILE  pad swap send-text  REPEAT
    drop ['] .time $err ;

script? [IF] announce-me ." connect alice?" key drop
    "alice" nat:connect c:msg-test c:disconnect bye [THEN]
