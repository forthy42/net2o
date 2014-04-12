\ keccak wrapper

require 64bit.fs
require crypto-api.fs

\ dummy load for Android
[IFDEF] android
    s" /data/data/gnu.gforth/lib/libkeccak.so" open-lib drop
[THEN]

c-library keccak
    \    s" keccak/.libs" add-libpath
    s" keccak" add-lib
    [IFDEF] android
	s" ./keccak" add-libpath
    [THEN]
    \c #include <KeccakF-1600.h>
    \c UINT64* KeccakEncryptLoop(keccak_state state, UINT64 * data, unsigned int n)
    \c {
    \c   while(n>0) {
    \c     unsigned int p = n >= 128 ? 128 : n;
    \c     KeccakF(state);
    \c     KeccakEncrypt(state, data, p>>3);
    \c     data += p>>3; n-=p;
    \c   }
    \c   return data;
    \c }
    \c UINT64* KeccakDecryptLoop(keccak_state state, UINT64 * data, unsigned int n)
    \c {
    \c   while(n>0) {
    \c     unsigned int p = n >= 128 ? 128 : n;
    \c     KeccakF(state);
    \c     KeccakDecrypt(state, data, p>>3);
    \c     data += p>>3; n-=p;
    \c   }
    \c   return data;
    \c }

\ ------===< functions >===-------
c-function KeccakInitialize KeccakInitialize  -- void
c-function KeccakF KeccakF a -- void
c-function KeccakInitializeState KeccakInitializeState a -- void
c-function KeccakExtract KeccakExtract a a n -- void
c-function KeccakAbsorb KeccakAbsorb a a n -- void
c-function KeccakEncrypt KeccakEncrypt a a n -- void
c-function KeccakDecrypt KeccakDecrypt a a n -- void
c-function KeccakEncryptLoop KeccakEncryptLoop a a n -- a
c-function KeccakDecryptLoop KeccakDecryptLoop a a n -- a

end-c-library

25 8 * Constant keccak#
128 Constant keccak#max
24 Constant keccak#cks

UValue @keccak

: keccak0 ( -- ) @keccak KeccakInitializeState ;

: keccak* ( -- ) @keccak KeccakF ;
: >keccak ( addr u -- )  3 rshift @keccak -rot KeccakAbsorb ;
: +keccak ( addr u -- )  3 rshift @keccak -rot KeccakEncrypt ;
: -keccak ( addr u -- )  3 rshift @keccak -rot KeccakDecrypt ;
: keccak> ( addr u -- )  3 rshift @keccak -rot KeccakExtract ;

\ crypto api integration

require crypto-api.fs

crypto class
    keccak# uvar keccak-state
    keccak#cks uvar keccak-checksums
    keccak#max uvar keccak-padded
    cell uvar keccak-up
end-class keccak

: keccak-init crypto-o @ IF  keccak-up @ next-task = ?EXIT  THEN
    keccak new crypto-o ! next-task keccak-up ! keccak-state to @keccak ;

: keccak-free crypto-o @ ?dup-IF  dispose  THEN
    0 to @keccak crypto-o off ;

keccak-init

' keccak-init to c:init
:noname to @keccak ; to c:key! ( addr -- )
\G use addr as key storage
' @keccak to c:key@ ( -- addr )
\G obtain the key storage
' keccak# to c:key# ( -- n )
\G obtain key storage size
:noname keccak0 keccak#max >keccak ; to >c:key ( addr -- )
\G move 128 bytes from addr to the state
:noname keccak#max keccak> ; to c:key> ( addr -- )
\G get 128 bytes from the state to addr
' keccak* to c:diffuse ( -- )
\G perform a diffuse round
:noname ( addr u -- )
    \G Encrypt message in buffer addr u
    @keccak -rot KeccakEncryptLoop  drop
\    BEGIN  @keccak KeccakF  2dup keccak#max umin tuck +keccak
\    /string dup 0= UNTIL  2drop
; to c:encrypt
:noname ( addr u -- )
    \G Decrypt message in buffer addr u
    @keccak -rot KeccakDecryptLoop  drop
\    BEGIN  @keccak KeccakF  2dup keccak#max umin tuck -keccak
\    /string dup 0= UNTIL  2drop
; to c:decrypt ( addr u -- )
:noname ( addr u -- )
    \G Encrypt message in buffer addr u with auth
\    BEGIN  @keccak KeccakF  2dup keccak#max umin tuck +keccak
\    /string dup 0= UNTIL  drop
    @keccak -rot KeccakEncryptLoop
    @keccak KeccakF
    >r keccak-checksums keccak#cks keccak> keccak-checksums 128@ r> 128!
; to c:encrypt+auth ( addr u -- )
:noname ( addr u -- )
    \G Decrypt message in buffer addr u, with auth check
\    BEGIN  @keccak KeccakF  2dup keccak#max umin tuck -keccak
\    /string dup 0= UNTIL  drop
    @keccak -rot KeccakDecryptLoop
    @keccak KeccakF
    128@ keccak-checksums keccak#cks keccak> keccak-checksums 128@ 128=
; to c:decrypt+auth ( addr u -- flag )
:noname ( addr u -- )
\G Hash message in buffer addr u
    BEGIN  2dup keccak#max umin tuck
	dup keccak#max u< IF  keccak-padded keccak#max >padded
	    keccak-padded keccak#max  THEN  >keccak  @keccak KeccakF
    /string dup 0= UNTIL  2drop
; to c:hash
:noname ( addr u -- )
    BEGIN  @keccak KeccakF  2dup keccak#max umin tuck keccak>
    /string dup 0= UNTIL  2drop
; to c:prng
\G Fill buffer addr u with PRNG sequence
:noname @keccak KeccakF
    keccak-checksums keccak#cks keccak> keccak-checksums 128@ ; to c:checksum ( -- xd )
\G compute a 128 bit checksum
:noname keccak-checksums keccak#cks keccak> keccak-checksums $10 + 64@ ; to c:cookie ( -- x )
\G obtain a different 64 bit checksum part

static-a to allocater
keccak new Constant keccak-o
dynamic-a to allocater
