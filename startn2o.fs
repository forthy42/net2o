\ Start net2o on android

require starta.fs
require net2o/n2o.fs
: net2o-greet
    ." net2o text UI, type 'bye' to leave and 'help' for help" cr
;
: noname load-rc n2o-greet n2o-cmds ; is bootmessage