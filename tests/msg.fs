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
    [: .time ." Download test: 1 text file and 2 photos" cr ;] $err
    net2o-code
      expect-reply
      log !time .time s" Message test" $, type cr endwith
      msg ticks lit, msg-at
      "This is a test message" $, msg-text endwith
      cookie+request
    end-code| ['] .time $err
    >timing do-disconnect [: .packets profile( .times ) ;] $err ;

script? [IF] "bob" nat:connect c:msg-test bye [THEN]
