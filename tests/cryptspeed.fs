\ test encryption speed

require ../net2o.fs

: test-speed ( addr u -- )
    c:0key 2dup 0 !time c:encrypt+auth .time
    c:0key      0 !time c:decrypt+auth .time . cr ;

: test-keccak ( addr u -- )
    ." Keccak:    " keccak-o crypto-o ! test-speed ;
: test-threefish ( addr u -- )
    ." Threefish: " threefish-o crypto-o ! test-speed ;

: test-all ( addr u -- )
    2dup test-keccak test-threefish ;

25000000 Constant test-size#
test-size# $10 + allocate throw Constant test-pad

script? [IF]
    test-pad test-size# test-all
    test-pad test-size# test-all
    test-pad test-size# test-all
    bye
[THEN]