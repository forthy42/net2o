\ test for ed25519

100 0 [do] skc stpkc ed-dh 2drop [loop] \ warmup for the CPU

." Test signing" cr
skc pkc 2dup ed-keypair !time ed-keypair .time
keccak0 "Test 123" >keccak keccak* skc pkc ed-sign
keccak0 "Test 123" >keccak keccak*
skc pkc !time ed-sign .time
keccak0 "Test 123" >keccak keccak* dup pkc ed-verify drop
keccak0 "Test 123" >keccak keccak*
dup pkc !time ed-verify .time
[IF] ." passed" [ELSE] ." failed" [THEN] cr
keccak0 "Test 124" >keccak keccak* dup pkc ed-verify drop
keccak0 "Test 124" >keccak keccak*
dup pkc !time ed-verify .time
0= [IF] ." forge detected" [ELSE] ." failed" [THEN] cr
$40 xtype cr

." Test EdDH" cr
stskc stpkc ed-keypair
skc stpkc 2dup ed-dh 2drop ed-dh pad swap move
skc stpkc 2dup ed-dh 2drop !time ed-dh .time 2drop
stskc pkc 2dup ed-dh 2drop ed-dh
stskc pkc 2dup ed-dh 2drop !time ed-dh .time 2drop cr
2dup pad over str= [IF] ." passed"
[ELSE] ." failed" pad over cr xtype [THEN] cr
xtype cr

." Test EdDH variable speed" cr
skc stpkc 2dup ed-dhv 2drop ed-dhv pad swap move
skc stpkc 2dup ed-dhv 2drop !time ed-dhv .time 2drop
stskc pkc 2dup ed-dhv 2drop ed-dhv
stskc pkc 2dup ed-dhv 2drop !time ed-dhv .time 2drop cr
2dup pad over str= [IF] ." passed"
[ELSE] ." failed" pad over cr xtype [THEN] cr
xtype cr
