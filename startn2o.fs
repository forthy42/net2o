\ Start net2o on android

require starta.fs
require net2o/n2o.fs
: n2o-greet
    ." net2o text UI, nerd edition for 32c3" cr
    ." type 'bye' to leave and 'help' for help" cr ;
:noname load-rc n2o-greet
    "~/.net2o/seckeys.k2o" file-status nip
    IF  ." Generate a new keypair:" cr
	n2o:keygen  ELSE  get-me  THEN
    n2o-cmds bye ; is bootmessage