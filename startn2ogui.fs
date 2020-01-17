\ Start net2o on android

e? os-type s" linux-android" string-prefix? [IF] require starta.fs [THEN]
page ." loading n2o..."
warnings off \ no warnings please
require n2o.fs
page
:noname load-rc save-net2o-cmds set-net2o-cmds n2o:gui ; is bootmessage
