\ Start net2o on android

e? os-type s" linux-android" string-prefix? [IF] require starta.fs [THEN]
page ." loading n2o..."
require n2o.fs
: n2o-greet  page
    ." net2o " net2o-version type ."  text UI, nerd edition" cr
    ." type 'bye' to leave and 'help' for help" cr ;
:noname load-rc n2o-greet
    "~/.net2o/seckeys.k2o" file-status nip
    IF  ." Generate a new keypair:" cr
	['] n2o:keygen catch ?dup-IF  DoError  THEN
    ELSE  ['] get-me catch ?dup-IF  DoError  THEN  THEN
    set-net2o-cmds ; is bootmessage