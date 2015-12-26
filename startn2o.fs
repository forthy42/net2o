\ Start net2o on android

e? os-type s" linux-android" str= [IF] require starta.fs [THEN]
." loading net2o" cr stdout flush-file throw
require n2o.fs
." net2o load ok" cr stdout flush-file throw
: n2o-greet
    ." net2o text UI, nerd edition for 32c3" cr
    ." type 'bye' to leave and 'help' for help" cr ;
:noname load-rc n2o-greet
    "~/.net2o/seckeys.k2o" file-status nip
    IF  ." Generate a new keypair:" cr
	n2o:keygen  ELSE  get-me  THEN
    set-net2o-cmds ; is bootmessage