\ Interface to the ed25519 primitives from donna     23oct2013py
\ The high level stuff is all in Forth

\ dummy load for Android
[IFDEF] android
    s" /data/data/gnu.gforth/lib/libed25519prims.so" open-lib drop
[THEN]

c-library ed25519_donna
    "ed25519prims" add-lib
    [IFDEF] android
	s" ./shlibs/ed25519-donna/.libs" add-libpath
    [THEN]
    \c #include <stdint.h>
    \c #include <ed25519-prims.h>
    \c int str32eq(long* a, long* b) {
    \c   long diff=0;
    \c   switch(sizeof(long)) {
    \c     case 4:
    \c       diff|=((a[4]^b[4])|(a[5]^b[5])|(a[6]^b[6])|(a[7]^b[7]));
    \c     case 8:
    \c       diff|=((a[0]^b[0])|(a[1]^b[1])|(a[2]^b[2])|(a[3]^b[3]));
    \c   }
    \c   return -(diff==0);
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
    c-variable ge25519-basepoint ge25519_basepoint ( --  addr )
    c-variable ge25519-niels*[] ge25519_niels_sliding_multiples ( -- addr )
end-c-library

: 32b>sc25519 32 nb>sc25519 ;
: 64b>sc25519 64 nb>sc25519 ;

$20 Constant KEYBYTES

user-o edbuf

object class
    $60 uvar sigbuf
    $30 uvar sct0
    $30 uvar sct1
    $30 uvar sct2
    $30 uvar sct3
    $C0 uvar get0
    $C0 uvar get1
    $40 uvar hashtmp
    keccak# uvar hstatetmp
    cell uvar task-id
end-class edbuf-c

: init-ed25519
    edbuf @ IF  task-id @ up@ = ?EXIT  THEN
    [: edbuf-c new edbuf ! ;] crypto-a with-allocater
    up@ task-id ! ;

init-ed25519

: free-ed25519 ( -- )
    edbuf @ ?dup-IF  .dispose  THEN  edbuf off ;

: clean-ed25519 ( -- )
    \g do this every time you computed using something secret
    sct0 task-id over - erase ;

: sk-mask ( sk -- )  dup c@ $F8 and over c!
    $1F + dup c@ $7F and $40 or swap c! ;

: gen-sk ( sk -- ) >r
    \G generate a secret key with the right bits set and cleared
    $20 rng$ r@ swap move r> sk-mask ;

: sk>pk ( sk pk -- )
    \G convert a secret key to a public key
    sct0 rot raw>sc25519
    get0 sct0 ge25519*base
    get0 ge25519-pack clean-ed25519 ;

: ed-keypair ( sk pk -- )
    \G generate a keypair
    over gen-sk sk>pk ;

: ed-keypairx { sk1 pkrev skc pkc -- }
    sct2 sk1 raw>sc25519
    pkrev sk-mask  sct1 pkrev raw>sc25519
    sk1 KEYBYTES erase  pkrev KEYBYTES erase \ things we don't need anymore
    sct2 sct2 sct1 sc25519*
    skc sct2 sc25519>32b
    skc pkc sk>pk ; \ this also cleans up temp stuff

: >hash ( addr u -- )
    \G absorb a short string, perform a hash round
    \G and output 64 bytes to hashtmp
    c:shorthash hashtmp $40 c:hash@ ;

: ed-sign { skh sk pk -- sig u }
    \G sign a message: the keccak state contains the hash of the message.
    c:key@ hstatetmp c:key# move \ we need this twice - move away
    skh $20 >hash \ gen "random number" from secret to hashtmp
    hstatetmp c:key@ c:key# move \ restore state
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
    clean-ed25519 sigbuf $40 ; \ r,s

: ed-check? { sig pk -- flag }
    \G check a message: the keccak state contains the hash of the message.
    \G The unpacked pk is in get0, so this word can be used for batch checking.
    sig hashtmp $20 move  pk hashtmp $20 + $20 move
    hashtmp $40 c:shorthash hashtmp $40 c:hash@ \ z=hash(r+pk+message)
    sct2 hashtmp 64b>sc25519       \ sct2 is z
    sct3 sig $20 + raw>sc25519     \ sct3 is s
    get1 get0 sct2 sct3 ge25519*+  \ base*s-pk*z
    sigbuf $40 + get1 ge25519-pack         \ =r
    sig sigbuf $40 + 32b= ;

: ed-verify { sig pk -- flag } \ message digest is in keccak state
    get0 pk ge25519-unpack- 0=  IF  false EXIT  THEN \ bad pubkey
    sig pk ed-check? ;

: ed-dh { sk pk dest -- secret len }
    get0 pk ge25519-unpack- 0= !!no-ed-key!!
    sct2 sk raw>sc25519
    get1 get0 sct2 ge25519*
    dest get1 ge25519-pack
    clean-ed25519 dest $20  $80 dest $1F + xorc! ;

: ed-dhx { offset sk pk dest -- secret len }
    get0 pk ge25519-unpack- 0= !!no-ed-key!!
    sct2 sk raw>sc25519
    sct1 offset 32b>sc25519
    sct2 sct2 sct1 sc25519*
    get1 get0 sct2 ge25519*
    dest get1 ge25519-pack
    clean-ed25519 dest $20  $80 dest $1F + xorc! ;

: ed-dhv { sk pk dest -- secret len }
    get0 pk ge25519-unpack- 0= !!no-ed-key!!
    sct2 sk raw>sc25519
    get1 get0 sct2 ge25519*v
    dest get1 ge25519-pack
    clean-ed25519 dest $20  $80 dest $1F + xorc! ;
