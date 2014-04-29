\ net2o tests - client side

require net2o.fs
require client-tests.fs

+db stat(
+debug
%droprate
script? [IF] debug-task [THEN]
test-keys \ we want the test keys - never use this in production!

"alice" >key \ get our anonymous key

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

: c:lookup ( addr u -- )
    $2000 $10000 "test" ins-ip4 c:connect
    2dup c:addme-fetch-host c:disconnect
    o-timeout n2o:dispose-context
    nick-key ke-pk $@ >d#id ;
: c:insert-host ( addr u -- )
    host>$ IF
	[: check-addr1 0= IF  2drop  EXIT  THEN
	  insert-address temp-addr ins-dest
	  ." insert host: " temp-addr $10 xtype cr
	  return-addr $10 0 skip nip 0= IF
	      temp-addr return-addr $10 move
\	      temp-addr return-address $10 move
	  THEN ;] $>sock
    ELSE  2drop  THEN ;

: n2o:lookup ( addr u -- )
    2dup c:lookup
    0 n2o:new-context dest-key  return-addr $10 erase
    d#id @ k#host cells + ['] c:insert-host $[]map ;

: nat:connect ( addr u -- )
    init-cache'
    2dup n2o:lookup dest-key
    ." trying to connect to: " return-addr $10 xtype cr
    $10000 $100000 n2o:connect-nat +flow-control +resend
    ." Connected!" cr
    c:test-rest ;

\ ?nextarg [IF] s>number drop [ELSE] 1 [THEN] c:tests

script? [IF] "bob" nat:connect bye [THEN]
