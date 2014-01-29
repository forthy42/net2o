\ net2o tests - client side

require ../net2o.fs

+db stat(
+debug
+db dht(

"anonymous" >key \ get our anonymous key

init-client

!time

$8000 $100000
?nextarg [IF] net2o-host $! [THEN]
?nextarg [IF] s>number drop to net2o-port [THEN]

: c:connect ( -- )
    $8000 $100000
    net2o-host $@ net2o-port insert-ip n2o:connect +flow-control +resend
    [: .time ." Connected, o=" o hex. cr ;] $err ;

c:connect

+addme

net2o-code
expect-reply
s" DHT test" $, type cr get-ip
pkc keysize $, dht-id
forever "test:tag" pkc keysize gen-tag-del $, k#tags ulit, dht-value-
forever "test:tag" pkc keysize gen-tag $, k#tags ulit, dht-value+
end-code

1 client-loop
-setip

net2o-code
expect-reply
pkc keysize $, dht-id
k#host ulit, dht-value? k#tags ulit, dht-value?
nest[ add-cookie lit, set-rtdelay request-done ]nest
end-code

1 client-loop
o-timeout
bye