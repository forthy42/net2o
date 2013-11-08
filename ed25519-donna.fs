\ Interface to the ed25519 primitives from donna     23oct2013py
\ The high level stuff is all in Forth

\ dummy load for Android
[IFDEF] android
    s" /data/data/gnu.gforth/lib/libed25519-prims.so" open-lib drop
[THEN]

c-library ed25519_donna
    "ed25519-prims" add-lib
    \c #include <stdint.h>
    \c #include <ed25519-prims.h>
    \c int str32eq(uint64_t* a, uint64_t* b) {
    \c    uint64_t diff=((a[0]^b[0])|(a[1]^b[1])|(a[2]^b[2])|(a[3]^b[3]));
    \c    return -(diff==0);
    \c }

    c-function raw>sc25519 expand_raw256_modm a a -- void ( sc char[32] -- )
    c-function nb>sc25519 expand256_modm a a n -- void ( sc char[64] -- )
    c-function sc25519>32b contract256_modm a a -- void ( char[32] sc -- )
    c-function sc25519* mul256_modm a a a -- void ( r x y -- )
    c-function sc25519+ add256_modm a a a -- void ( r x y -- )

    c-function ge25519*base ge25519_scalarmult_base a a -- void ( ger x -- )
    c-function ge25519-pack ge25519_pack a a -- void ( r ger -- )
    c-function ge25519-unpack- ge25519_unpack_negative_vartime a a -- n ( r p -- flag )
    c-function ge25519*+ ge25519_double_scalarmult_vartime a a a a -- void ( r p s1 s2 -- )
    c-function ge25519*v ge25519_scalarmult_vartime a a a -- void ( r p s -- )
    c-function ge25519* ge25519_scalarmult a a a -- void ( r p s -- )
    c-function 32b= str32eq a a -- n ( addr1 addr2 -- flag )
end-c-library

: 32b>sc25519 32 nb>sc25519 ;
: 64b>sc25519 64 nb>sc25519 ;

$20 Constant KEYBYTES

$40 12 * $60 + $40 + 200 + $10 + Constant edbuf#

here edbuf# allot $F + -$10 and \ align for SSE

dup Constant sct0 $40 +
dup Constant sct1 $40 +
dup Constant sct2 $40 +
dup Constant sct3 $40 + \ can be between $20 and $30
dup Constant get0 $100 +
dup Constant get1 $100 + \ can be between $80 and $C0
dup Constant sigbuf $60 +
dup Constant hashtmp $40 +
Constant keccaktmp

: gen-sk ( sk -- ) >r
    \G generate a secret key with the right bits set and cleared
    $20 rng$ r@ swap move  r@ c@ $F8 and r@ c!
    r> $1F + dup c@ $7F and $40 or swap c! ;

: sk>pk ( sk pk -- )
    \G convert a secret key to a public key
    sct0 rot raw>sc25519
    get0 sct0 ge25519*base
    get0 ge25519-pack ;

: ed-keypair ( sk pk -- )
    \G generate a keypair
    over gen-sk sk>pk ;

: >hash ( addr u -- )
    \G absorb a short string, perform a hash round
    \G and output 64 bytes to hashtmp
    >keccak keccak* hashtmp $40 keccak> ;

: ed-sign { sk pk -- sig u }
    \G sign a message: the keccak state contains the hash of the message.
    @keccak keccaktmp keccak# move \ we need this twice - move away
    sk $20 >hash \ gen "random number" from secret to hashtmp
    keccaktmp @keccak keccak# move \ restore state
    sct3 hashtmp 64b>sc25519
    get0 sct3 ge25519*base   \ sct3 is k
    sigbuf get0 ge25519-pack   \ sct0 is r=k*base
    pk sigbuf $20 + $20 move
    sigbuf $40 >hash           \ z=hash(r+pk+message)
    sct1 hashtmp 64b>sc25519 \ sct1 is z
    sct2 sk raw>sc25519      \ sct2 is sk
    sct1 sct1 sct2 sc25519*
    sct1 sct1 sct3 sc25519+  \ s=z*sk+k
    sigbuf $20 + sct1 sc25519>32b
    sigbuf $40 ; \ r,s

: ed-check? { sig pk -- flag }
    \G check a message: the keccak state ontaints the hash of the message.
    \G The unpacked pk is in get0, so this word can be used for batch checking.
    sig hashtmp $20 move  pk hashtmp $20 + $20 move
    hashtmp $40 >keccak keccak* hashtmp $40 keccak> \ z=hash(r+pk+message)
    sct2 hashtmp 64b>sc25519       \ sct2 is z
    sct3 sig $20 + raw>sc25519     \ sct3 is s
    get1 get0 sct2 sct3 ge25519*+  \ base*s-pk*z
    sigbuf $40 + get1 ge25519-pack         \ =r
    sig sigbuf $40 + 32b= ;

: ed-verify { sig pk -- flag } \ message digest is in keccak state
    get0 pk ge25519-unpack- 0=  IF  false EXIT  THEN \ bad pubkey
    sig pk ed-check? ;

: ed-dh { sk pk -- secret len }
    get0 pk ge25519-unpack- 0= !!no-ed-key!!
    sct2 sk raw>sc25519
    get1 get0 sct2 ge25519*
    sct0 get1 ge25519-pack
    sct0 $20 ;

: ed-dhv { sk pk -- secret len }
    get0 pk ge25519-unpack- 0= !!no-ed-key!!
    sct2 sk raw>sc25519
    get1 get0 sct2 ge25519*v
    sct0 get1 ge25519-pack
    sct0 $20 ;
