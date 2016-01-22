\ net2o tests - client side

require net2o.fs

+db stat(
+debug

"anonymous" >key \ get our anonymous key

init-client

s" .cache" file-status nip #-514 = [IF]
    s" .cache" $1FF =mkdir throw
[THEN]

!time


?nextarg [IF] net2o-host $@ [THEN] \ default
?nextarg [IF] net2o-port [ELSE] s>number drop [THEN]
insert-ip n2o:new-context "test" dest-key
$8000 $100000 n2o:connect +flow-control +resend

." Connected" cr

net2o-code
expect-reply
data-ivs time-offset!
s" Download test" $, type cr  see-me
$400 blocksize! $400 blockalign! stat( request-stats )
s" data/android-ndk-r8e-linux-x86.tar.bz2" s" ~/Downloads/android-ndk-r8e-linux-x86.tar.bz2" n2o:copy
n2o:done
send-chunks
end-code

1 client-loop .time cr

bye