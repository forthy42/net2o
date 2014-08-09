\ net2o tests - msg

require ../client-tests.fs

+db stat(
script? [IF] +debug %droprate [THEN]
test-keys \ we want the test keys - never use this in production!

i'm alice

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

: c:msg-test ( -- )
    [: .time ." Message test" cr ;] $err
    "Hi Bob!" send-text o-timeout
    BEGIN  pad 100 accept cr dup WHILE  pad swap send-text  REPEAT
    drop ['] .time $err ;

script? [IF] c:announce-me ." connect bob?" key drop
    "bob" nat:connect c:msg-test c:disconnect bye [THEN]
