\ test for ed25519

100 0 [do] skc stpkc ed-dh 2drop [loop] \ warmup for the CPU

." Test keypair "
skc pkc 2dup ed-keypair !time ed-keypair .time cr
." Test signing "
keccak0 "Test 123" >keccak keccak* skc pkc ed-sign 2drop
keccak0 "Test 123" >keccak keccak*
skc pkc !time ed-sign drop .time cr
keccak0 "Test 123" >keccak keccak* dup pkc ed-verify drop
keccak0 "Test 123" >keccak keccak*
." Test verify "
dup pkc ed-verify
>r dup pkc !time ed-verify .time drop r>
[IF] ."  passed" [ELSE] ."  failed" [THEN] cr
." Test forge "
keccak0 "Test 124" >keccak keccak* dup pkc ed-verify drop
keccak0 "Test 124" >keccak keccak*
dup pkc !time ed-verify .time
0= [IF] ."  passed" [ELSE] ."  failed" [THEN] cr
$40 xtype cr

." Test EdDH "
stskc stpkc ed-keypair
skc stpkc 2dup ed-dh 2drop ed-dh pad swap move
skc stpkc 2dup ed-dh 2drop ed-dh 2drop
stskc pkc 2dup ed-dh 2drop ed-dh
stskc pkc 2dup ed-dh 2drop !time ed-dh .time 2drop
2dup pad over str= [IF] ."  passed"
[ELSE] ."  failed" pad over cr xtype [THEN] cr
xtype cr

." Test EdDH variable speed "
skc stpkc 2dup ed-dhv 2drop ed-dhv pad swap move
skc stpkc 2dup ed-dhv 2drop ed-dhv 2drop
stskc pkc 2dup ed-dhv 2drop ed-dhv
stskc pkc 2dup ed-dhv 2drop !time ed-dhv .time 2drop
2dup pad over str= [IF] ."  passed"
[ELSE] ."  failed" pad over cr xtype [THEN] cr
xtype cr
