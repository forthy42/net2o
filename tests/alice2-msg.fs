\ net2o tests - msg

require ../client-tests.fs
require ../test-keys.fs \ we want the test keys - never use this in production!

+db stat(
script? [IF] +debug %droprate [THEN]

i'm alice

85" kQusJzA;7*?t=uy@X}1GWr!+0qqp_Cn176t4(dQ*;Ag<!JwrhY<K}^(4I?5Sb;b1QBLRrRm~58+bTllW" key:new >o "bernd" ke-nick $! o>

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

: c:msg-test ( -- )
    [: .time ." Message test" cr ;] $err
    "Hi Bob!" send-text o-timeout
    BEGIN  pad 100 accept cr dup WHILE  pad swap send-text  REPEAT
    drop ['] .time $err ;

script? [IF] c:announce-me ." connect bernd?" key drop
    "bernd" nat:connect c:msg-test c:disconnect bye [THEN]
