\ net2o tests - client side

require ../net2o.fs

+db stat(
+debug
+db dht(

test-keys \ we want the test keys - never use this in production!
"anonymous" >key \ get our anonymous key

init-client

!time

?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

: c:connect ( -- ) +get-time
    net2o-host $@ net2o-port insert-ip
    n2o:new-context "test" dest-key
    $2000 $10000
    n2o:connect +flow-control +resend
    o-timeout -other [: .time ." Connected, o=" o hex. cr ;] $err ;

: c:add-tag ( -- ) +addme
    net2o-code
    expect-reply
    s" DHT test" $, type cr get-ip
    pkc keysize $, dht-id
    forever "test:tag" pkc keysize gen-tag-del $, k#tags ulit, dht-value-
    forever "test:tag" pkc keysize gen-tag $, k#tags ulit, dht-value+
    end-code  1 client-loop -setip o-timeout ;

: c:fetch-tag ( -- )
    net2o-code
    expect-reply
    pkc keysize $, dht-id
    k#host ulit, dht-value? k#tags ulit, dht-value?
    nest[ add-cookie lit, set-rtdelay request-done ]nest
    end-code  1 client-loop o-timeout ;

: c:fetch-tags ( -- )
    net2o-code
    expect-reply
    0 ulit, dht-open  pkc keysize $, $FE ulit, 0 ulit, dht-query
    slurp send-chunks
    end-code  1 client-loop o-timeout ;

: c:dhtend ( -- )    
    net2o-code s" DHT end" $, type cr .time disconnect  end-code
    o-timeout n2o:dispose-context ;

: c:dht ( n -- ) c:connect 0 ?DO
	c:add-tag c:fetch-tag \ c:fetch-tags
    LOOP c:dhtend ;

script? [IF] 1 c:dht bye [THEN]
