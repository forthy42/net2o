\ test file vault

require ../net2o-vault.fs
require ../test-keys.fs

\ +db trace(

Variable key-list
i'm eve

vpks-off
+pk alice
+pk bob

"data/2011-05-13_11-26-57-small.jpg" vkey-list encrypt-file

i'm bob
"data/2011-05-13_11-26-57-small.jpg.v2o" decrypt-file

bye