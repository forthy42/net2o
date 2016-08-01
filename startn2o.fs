\ Start net2o on android

e? os-type s" linux-android" string-prefix? [IF] require starta.fs [THEN]
page ." loading n2o..."
warnings off \ no warnings please
require n2o.fs
: n2o-greet  page
    ." net2o " net2o-version type ."  text UI, nerd edition" cr
    ." type 'bye' to leave and 'help' for help" cr ;
:noname load-rc n2o-greet get-me set-net2o-cmds ; is bootmessage