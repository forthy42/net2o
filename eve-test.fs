\ eve wants to revoke her key...

require client-tests.fs

test-keys
"eve" >key
pkc keysize 2* save-mem d#hashkey 2!

\ that's Eve's secret revokation key
x" 7821DA41AFBB8F7356E2EB7059BE70321D7ADCDAD8C504998627CBB9366AB752" drop revoke-key
revoke? [IF] ." Revocation succeeded" [ELSE] ." Revokation failed!" [THEN] cr
dump

script? [IF] bye [THEN]