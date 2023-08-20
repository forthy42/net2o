\ test for ed25519 - first fuzzed, then deterministic test

require ../net2o.fs

Variable test$

: >sksig ( -- )
    pkc keysize skc keysize
    c:0key >keyed-hash sksig $20 keccak> ;
: gen-pairs ( -- )
    skc pkc ed-keypair
    stskc stpkc ed-keypair  >sksig ;
: gen-sig ( -- addr )
    c:0key test$ $@ c:hash sksig skc pkc ed-sign drop ;
: check-sig ( addr -- flag )
    c:0key test$ $@ c:hash pkc ed-verify ;
: check-sig0 ( addr -- flag )
    c:0key test$ $@ 1- c:hash pkc ed-verify ;
: 16aligned ( addr -- addr' ) $F + -$10 and ;
: check-dh ( -- flag )
    skc stpkc pad 16aligned ed-dh stskc pkc pad $20 + 16aligned ed-dh str= ;

: do-fuzz ( -- )  s" A" test$ $+! gen-pairs
    gen-sig dup check-sig swap check-sig0 0= and check-dh and
    IF <info> ." +" ELSE <err> ." -" THEN <default> ;
: fuzzes ( n -- ) 0 ?DO  do-fuzz  LOOP ;
: fuzzl ( n -- )  0 ?DO  cols I' I - umin fuzzes cols +LOOP ;
: sigs ( n -- )   0 ?DO  gen-sig drop  LOOP ;
: checksigs ( n -- )  gen-sig swap  0 ?DO  dup check-sig drop  LOOP  drop ;
!time 1000 fuzzl cr .time ."  for 1000 fuzzed sigs+checks" cr
!time 1000 sigs  .time ."  for generating 1000 sigs" cr
!time 1000 checksigs  .time ."  for checking 1000 sigs" cr

\ deterministic tests

$40 buffer: testpk
: >test ( addr i -- addr' ) testpk + tuck $20 move ;

hex" E09657D8C066FBAAD009A1189B3A7E418CE2002E73E6C799DA7A6F5D86CA5B76" skc swap move
hex" C8B0514857E50524DEC94FB1157EF0BB0B89FFADA3A281FF2AE06F4BBD7EE671" stskc swap move
skc pkc sk>pk pkc $20 hex" 148777AA913CA970AD23E1C71B6B5C650B0448BA6DACEA5587ADFE13BA9262BB" str= 0= [IF] ." incorrect pubkey " pkc $20 xtype cr [THEN]
stskc stpkc sk>pk stpkc $20 hex" 301C3345E9756348DD442B03AAE186A73272ECF145D63C3A01DD7BBF7A3F24D7" str= 0= [IF] ." incorrect pubkey " stpkc $20 xtype cr [THEN]
>sksig

100 0 [do] skc stpkc pad ed-dh 2drop [loop] \ warmup for the CPU

." Test keypair "
skc pkc 2dup sk>pk !time sk>pk .time cr
." Test signing "
c:0key "Test 123" c:hash sksig skc pkc ed-sign
hex" 422D393D79E24CFC1CBE42D8043F97057630D1E56DD7E8B57CE5FB8D483AE2A1D86EE12500F5856B559BFD781FE9D442CD502618FA94A69C9A41109AEB3E4B0C" str= 0= [IF] ." in" [THEN] ." correct sig "
c:0key "Test 123" c:hash
sksig skc pkc !time ed-sign drop .time cr
c:0key "Test 123" c:hash dup pkc ed-verify drop
c:0key "Test 123" c:hash
." Test verify "
dup pkc ed-verify
>r dup pkc !time ed-verify .time drop r>
[IF] <info> ."  passed" [ELSE] <err> ."  failed" [THEN] <default> cr
." Test forge "
c:0key "Test 124" c:hash dup pkc ed-verify drop
c:0key "Test 124" c:hash
dup pkc !time ed-verify .time
0= [IF] <info> ."  passed" [ELSE] <err> ."  failed" [THEN] <default> cr
$40 xtype cr

: test-eddh ( -- )
    ." Test EdDH "
    $20 0 DO
	stskc stpkc sk>pk
	skc stpkc I >test 2dup pad ed-dh 2drop pad ed-dh pad $20 + swap move
	skc stpkc I >test 2dup pad ed-dh 2drop pad ed-dh 2drop
	stskc pkc I >test 2dup pad ed-dh 2drop pad ed-dh
	stskc pkc I >test 2dup pad ed-dh 2drop pad
	I 0= IF  !time ed-dh .time  ELSE  ed-dh  THEN  2drop
	2dup hex" B5BB3B6663A992A29A75852AD4925085109E96485A770EDF7A8A945128F42BD2"
	str= I 0= IF  IF ."  correct" ELSE ."  incorrect" THEN
	ELSE  IF  <info> '+'  ELSE  <err>  '-'  THEN  emit <default>  THEN
	2dup pad $20 + over str= IF
	    <info> I 0= IF  ."  passed"  ELSE  '+' emit  THEN
	ELSE <err> ."  failed" pad over cr xtype THEN  <default>
	I 0= IF  cr xtype cr  ELSE  2drop  THEN
    LOOP cr ;
test-eddh

[IFDEF] ed-dhv
    ." Test EdDH variable speed "
    skc stpkc 2dup pad ed-dhv 2drop pad ed-dhv pad $20 + swap move
    skc stpkc 2dup pad ed-dhv 2drop pad ed-dhv 2drop
    stskc pkc 2dup pad ed-dhv 2drop pad ed-dhv
    stskc pkc 2dup pad ed-dhv 2drop !time pad ed-dhv .time 2drop
    2dup hex" B5BB3B6663A992A29A75852AD4925085109E96485A770EDF7A8A945128F42BD2" str= [IF] ."  correct" [ELSE] ."  incorrect" [THEN]
    2dup pad $20 + over str= [IF] <info> ."  passed"
    [ELSE] <err> ."  failed" pad over cr xtype [THEN] <default> cr
    xtype cr
[THEN]
script? [IF] bye [THEN]
