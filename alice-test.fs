\ net2o tests - client side

require net2o.fs
require client-tests.fs

+db stat(
+debug
%droprate
script? [IF] debug-task [THEN]
test-keys \ we want the test keys - never use this in production!

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

"alice" >key \ get our anonymous key

: c:lookup ( addr u -- )
    $2000 $10000 "test" c:connect
    2dup c:addme-fetch-host c:disconnect
    o-timeout n2o:dispose-context
    nick-key ke-pk $@ >d#id ;
: c:insert-host ( addr u -- )
    host>$ IF $>sock insert-address ret-addr ins-dest
	ret-addr $10 send-list $+[]!
    ELSE 2drop THEN ;

: n2o:lookup ( addr u -- )
    2dup c:lookup
    0 n2o:new-context dest-key
    d#id @ k#host cells + ['] c:insert-host $[]map ;
"bob" n2o:lookup

\ ?nextarg [IF] s>number drop [ELSE] 1 [THEN] c:tests

script? [IF] bye [THEN]
