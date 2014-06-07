\ eve wants to revoke her key...

require client-tests.fs

test-keys
i'm eve
pkc keysize 2* save-mem d#hashkey 2!

\ that's Eve's secret revokation key
x" 7821DA41AFBB8F7356E2EB7059BE70321D7ADCDAD8C504998627CBB9366AB752" drop
>revoke revoke-key
revoke? [IF] ." Revocation succeeded" [ELSE] ." Revokation failed!!!" [THEN] cr
dump

i'm eve
\ that's not quite Eve's secret revokation key can you spot the difference?
x" 7821DA41AFBB8F7356E2EB7159BE70321D7ADCDAD8C504998627CBB9366AB752" drop
' >revoke catch ' !!not-my-revsk!! >body @ = [IF] ." Check failed, as expected" [ELSE] ." Check wrongly ok!!!" [THEN] drop cr
revoke-key
revoke? [IF] ." Revocation check false positive!!!" [ELSE] ." Revokation failed, as expected" [THEN] cr 2drop

script? [IF] bye [THEN]