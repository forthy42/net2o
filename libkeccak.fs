\ keccak wrapper

\ dummy load for Android
[IFDEF] android
    s" /data/data/gnu.gforth/lib/libkeccak.so" open-lib drop
[THEN]

c-library keccak
\    s" keccak/.libs" add-libpath
    s" keccak" add-lib
    s" ./keccak" add-libpath
    \c #include <KeccakF-1600.h>

    include keccak.fs
end-c-library

25 8 * Constant keccak#
128 Constant keccak#max
24 Constant keccak#cks

UValue @keccak
UValue keccak-state
UValue keccak-checksums

: keccak-init ( -- )
    keccak# allocate throw to keccak-state
    keccak#cks allocate throw to keccak-checksums
    keccak-state to @keccak ;

keccak-init

: keccak0 ( -- ) @keccak KeccakInitializeState ;

: keccak* ( -- ) @keccak KeccakF ;
: >keccak ( addr u -- )  3 rshift @keccak -rot KeccakAbsorb ;
: +keccak ( addr u -- )  3 rshift @keccak -rot KeccakEncrypt ;
: -keccak ( addr u -- )  3 rshift @keccak -rot KeccakDecrypt ;
: keccak> ( addr u -- )  3 rshift @keccak -rot KeccakExtract ;

\ crypto api integration

require crypto-api.fs

crypto class end-class keccak

' keccak-init keccak to c:init
:noname to @keccak ; keccak to c:key! ( addr -- )
\G use addr as key storage
' @keccak keccak to c:key@ ( -- addr )
\G obtain the key storage
' keccak# keccak to c:key# ( -- n )
\G obtain key storage size
:noname keccak-state to @keccak
    keccak0 keccak#max >keccak ; keccak to >c:key ( addr -- )
\G move 128 bytes from addr to the state
:noname keccak#max keccak> ; keccak to c:key> ( addr -- )
\G get 128 bytes from the state to addr
:noname @keccak KeccakF ; keccak to c:diffuse ( -- )
\G perform a diffuse round
:noname ( addr u -- )
    \G Encrypt message in buffer addr u
    BEGIN  @keccak KeccakF  2dup keccak#max umin tuck +keccak
    /string dup 0= UNTIL  2drop
; keccak to c:encrypt
:noname ( addr u -- )
    \G Decrypt message in buffer addr u
    BEGIN  @keccak KeccakF  2dup keccak#max umin tuck -keccak
    /string dup 0= UNTIL  2drop
; keccak to c:decrypt ( addr u -- )
:noname ( addr u -- )
    \G Encrypt message in buffer addr u with auth
    BEGIN  @keccak KeccakF  2dup keccak#max umin tuck +keccak
    /string dup 0= UNTIL  drop @keccak KeccakF
    >r keccak-checksums keccak#cks keccak> keccak-checksums 128@ r> 128!
; keccak to c:encrypt+auth ( addr u -- )
:noname ( addr u -- )
    \G Decrypt message in buffer addr u, with auth check
    BEGIN  @keccak KeccakF  2dup keccak#max umin tuck -keccak
    /string dup 0= UNTIL  drop @keccak KeccakF
    128@ keccak-checksums keccak#cks keccak> keccak-checksums 128@ 128=
; keccak to c:decrypt+auth ( addr u -- flag )
:noname ( addr u -- )
\G Hash message in buffer addr u
    BEGIN  2dup keccak#max umin tuck >keccak  @keccak KeccakF
    /string dup 0= UNTIL  2drop
; keccak to c:hash
:noname ( addr u -- )
    BEGIN  @keccak KeccakF  2dup keccak#max umin tuck keccak>
    /string dup 0= UNTIL  2drop
; keccak to c:prng
\G Fill buffer addr u with PRNG sequence
:noname @keccak KeccakF
    keccak-checksums keccak#cks keccak> keccak-checksums 128@ ; keccak to c:checksum ( -- xd )
\G compute a 128 bit checksum
:noname keccak-checksums keccak#cks keccak> keccak-checksums $10 + 64@ ; keccak to c:cookie ( -- x )
\G obtain a different 64 bit checksum part

static-a to allocater
keccak new Constant keccak-o
dynamic-a to allocater
