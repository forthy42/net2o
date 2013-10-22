\ Interface to the ed25519 primitives from Supercop

c-library ed25519
    "ed25519" add-lib
    \c #include <fe25519.h>
    \c #include <ge25519.h>
    \c #include <sc25519.h>
    \c #include <stdint.h>
    \c int str32eq(uint64_t* a, uint64_t* b) {
    \c    uint64_t diff=((a[0]^b[0])|(a[1]^b[1])|(a[2]^b[2])|(a[3]^b[3]));
    \c    return -(diff==0);
    \c }

    c-function 32b>sc25519 sc25519_from32bytes a a -- void ( sc char[32] -- )
    c-function 64b>sc25519 sc25519_from64bytes a a -- void ( sc char[64] -- )
    c-function sc25519>32b sc25519_to32bytes a a -- void ( char[32] sc -- )
    c-function sc25519* sc25519_mul a a a -- void ( r x y -- )
    c-function sc25519+ sc25519_add a a a -- void ( r x y -- )

    c-function ge25519*base ge25519_scalarmult_base a a -- void ( ger x -- )
    c-function ge25519-pack ge25519_pack a a -- void ( r ger -- )
    c-function ge25519-unpack- ge25519_unpackneg_vartime a a -- n ( r p -- flag )
    c-function ge25519-m* ge25519_double_scalarmult_vartime a a a a -- void ( r1 p1 s1 s2 -- )
    c-function 32b= str32eq a a -- n ( addr1 addr2 -- flag )
end-c-library

$80 buffer: sct0
sct0 $20 + Constant sct1
sct1 $20 + Constant sct2
sct2 $20 + Constant sct3
$40 buffer: hashtmp
$80 buffer: get0
$80 buffer: get1
$80 buffer: get2
200 buffer: keccaktmp

: gen-sk ( sk -- ) >r
    $20 rng$ r@ swap move  r@ c@ $F8 and r@ c!
    r> $1F + dup c@ $7F and $40 or swap c! ;

: sk>pk ( sk pk -- )
    sct0 rot 32b>sc25519
    get0 sct0 ge25519*base
    get0 ge25519-pack ;

: ed-keypair ( sk pk -- )  over gen-sk sk>pk ;

: >hash ( addr u -- ) \ output is in hashtmp
    >keccak keccak* hashtmp $40 keccak> ;

: ed-sign { sk pk -- sig } \ message digest is in keccak state
    @keccak keccaktmp keccak# move \ we need this twice - move away
    sk $20 >hash \ gen random number from secret to hashtmp
    keccaktmp @keccak keccak# move \ restore state
    sct3 hashtmp 64b>sc25519
    get0 sct3 ge25519*base
    sct0 get0 ge25519-pack \ sct0 is now r
    pk sct1 $20 move
    sct0 $40 >hash \ z=r+pk+message digest
    sct1 hashtmp 64b>sc25519
    sct2 sk 32b>sc25519
    sct1 sct1 sct2 sc25519*
    sct1 sct1 sct3 sc25519+ \ s=hash*sk+r
    sct0 ;

: ed-check? { sig pk -- flag } \ message digest is in keccak state
    \ unpacked negated pk is in get1
    sig hashtmp $20 move  pk hashtmp $20 + $20 move
    hashtmp $40 >keccak keccak* hashtmp $40 keccak> \ z=r+pk+message digest
    sct2 hashtmp 64b>sc25519
    sct3 sig $20 + 32b>sc25519
    get2 get1 sct2 sct3 ge25519-m* \ s*z*-pk
    sct2 get2 ge25519-pack
    sig sct2 32b= ;

: ed-verify { sig pk -- flag } \ message digest is in keccak state
    get1 pk ge25519-unpack- IF  false  EXIT  THEN \ bad signature
    sig pk ed-check? ;

true [IF] \ test stuff
    skc pkc ed-keypair
    keccak0 "Test 123" >keccak keccak*
    skc pkc ed-sign
    dup $40 xtype cr
    keccak0 "Test 123" >keccak keccak*
    pkc ed-verify . cr
[THEN]