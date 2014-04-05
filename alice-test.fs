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
	$>sock 2dup try-ip
	IF insert-address ret-addr ins-dest
	    ret-addr $10 nat( ." host: " 2dup xtype cr )
	    send-list $+[]!  EXIT
	THEN
    THEN  2drop ;

: n2o:lookup ( addr u -- )
    2dup c:lookup
    0 n2o:new-context dest-key
    d#id @ k#host cells + ['] c:insert-host $[]map ;

: nat:connect ( addr u -- )
    init-cache'
    2dup n2o:lookup dest-key
    0 send-list $[]@ return-addr swap move
    0 send-list $[]@ return-address swap move
    ." trying to connect to: " 0 send-list $[]@ xtype cr
    0 send-list $[]@ ret-addr swap move  send-list $[]off
    $10000 $100000 n2o:connect +flow-control +resend
    ." Connected!" cr
    c:test-rest ;

\ ?nextarg [IF] s>number drop [ELSE] 1 [THEN] c:tests

script? [IF] "bob" nat:connect bye [THEN]
