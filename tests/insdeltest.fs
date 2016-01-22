\ test insert and delete
: []. [: type ." , " ;] $[]map cr ;

Variable foo
"hi" foo $ins[] foo [].
"ho" foo $ins[] foo [].
"ha" foo $ins[] foo [].
"he" foo $ins[] foo [].
"a" foo $ins[] foo [].
"c" foo $ins[] foo [].
"e" foo $ins[] foo [].
"z" foo $ins[] foo [].

"hi" foo $del[] foo [].
"ho" foo $del[] foo [].
"ha" foo $del[] foo [].
"he" foo $del[] foo [].
"a" foo $del[] foo [].
"c" foo $del[] foo [].
"e" foo $del[] foo [].
"z" foo $del[] foo [].