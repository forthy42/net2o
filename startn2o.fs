\ Start net2o on android

e? os-type s" linux-android" string-prefix? [IF] require starta.fs [THEN]
page ." loading n2o..." key? drop
warnings off \ no warnings please
require n2o.fs
:noname load-rc page get-me set-net2o-cmds
    n2o:cmd ; is bootmessage
' bye is 'quit
