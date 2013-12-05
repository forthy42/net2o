\ net2o tests - client side

require net2o.fs

+db stat(
+debug

"anonymous" >key \ get our anonymous key

init-client

!time

$8000 $100000
?nextarg [IF] net2o-host $@ [THEN] \ default
?nextarg [IF] net2o-port [ELSE] s>number drop [THEN]
insert-ip n2o:connect +flow-control +resend

." Connected, o=" o hex. cr

+addme

net2o-code
expect-reply
s" DHT test" $, type cr get-ip
end-code

1 client-loop
-setip o-timeout
bye