\ Start net2o on android

e? os-type s" linux-android" string-prefix? [IF] require starta.fs [THEN]
page ." loading n2o..." key? drop
warnings off \ no warnings please
require n2o.fs
:noname
    page ." loading n2o:gui..." key? drop
    n2o:gui ; is bootmessage
' bye is 'quit
