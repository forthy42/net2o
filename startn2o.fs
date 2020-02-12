\ Start net2o on android

e? os-type s" linux-android" string-prefix? [IF] require starta.fs [THEN]
page ." loading n2o..."
warnings off \ no warnings please
require n2o.fs
:noname load-rc page get-me save-net2o-cmds set-net2o-cmds ; is bootmessage
