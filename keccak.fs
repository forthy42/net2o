\ keccak wrapper

\ dummy load for Android
[IFDEF] android
    s" /data/data/gnu.gforth/lib/libkeccak.so" open-lib drop
[THEN]

c-library keccak
    s" keccak" add-lib
    [IFDEF] android
	s" ./keccak" add-libpath
    [THEN]
    \c #include <KeccakF-1600.h>
    \c UINT64* KeccakEncryptLoop(keccak_state state, UINT64 * data, int n)
    \c {
    \c   while(n>0) {
    \c     unsigned int p = n >= 128 ? 128 : n;
    \c     KeccakF(state);
    \c     KeccakEncrypt(state, data, p);
    \c     data = (UINT64*)(((char*)data)+p); n-=p;
    \c   }
    \c   return data;
    \c }
    \c UINT64* KeccakDecryptLoop(keccak_state state, UINT64 * data, int n)
    \c {
    \c   while(n>0) {
    \c     unsigned int p = n >= 128 ? 128 : n;
    \c     KeccakF(state);
    \c     KeccakDecrypt(state, data, p);
    \c     data = (UINT64*)(((char*)data)+p); n-=p;
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
128 Constant keccak#cks

UValue @keccak

: keccak0 ( -- ) @keccak KeccakInitializeState ;

: keccak* ( -- ) @keccak KeccakF ;
: >keccak ( addr u -- )  @keccak -rot KeccakAbsorb ;
: +keccak ( addr u -- )  @keccak -rot KeccakEncrypt ;
: -keccak ( addr u -- )  @keccak -rot KeccakDecrypt ;
: keccak> ( addr u -- )  @keccak -rot KeccakExtract ;

: move-rep ( srcaddr u1 destaddr u2 -- )
    bounds ?DO
	I' I - umin 2dup I swap move
    dup +LOOP  2drop ;

\ crypto api integration

crypto class
    keccak# uvar keccak-state
    keccak#cks uvar keccak-checksums
    keccak#max uvar keccak-padded
end-class keccak

User keccak-t

: keccak-init ( -- )
    keccak-t @ dup crypto-o ! IF  crypto-up @ up@ = ?EXIT  THEN
    [: keccak new dup crypto-o ! keccak-t ! ;] crypto-a with-allocater
    up@ crypto-up ! keccak-state to @keccak ;

: keccak-free crypto-o @ ?dup-IF  .dispose  THEN
    0 to @keccak crypto-o off ;

keccak-init

' keccak-init to c:init
' keccak-free to c:free
:noname to @keccak ; to c:key! ( addr -- )
\G use addr as key storage
' @keccak to c:key@ ( -- addr )
\G obtain the key storage
' keccak# to c:key# ( -- n )
\G obtain key storage size
' keccak0 to c:0key ( -- )
\G set zero key
:noname keccak0 keccak#max >keccak ; to >c:key ( addr -- )
\G move 128 bytes from addr to the state
:noname keccak#max keccak> ; to c:key> ( addr -- )
\G get 128 bytes from the state to addr
' keccak* to c:diffuse ( -- )
\G perform a diffuse round
:noname ( addr u -- )
    \G Encrypt message in buffer addr u
    @keccak -rot KeccakEncryptLoop  drop
; to c:encrypt
:noname ( addr u -- )
    \G Decrypt message in buffer addr u
    @keccak -rot KeccakDecryptLoop  drop
; to c:decrypt ( addr u -- )
:noname ( addr u tag -- )
    \G Encrypt message in buffer addr u with auth
    { tag } @keccak -rot KeccakEncryptLoop
    keccak*
    >r keccak-checksums keccak#cks keccak>
    keccak-checksums tag 7 and 4 lshift + r> $10 move
; to c:encrypt+auth ( addr u tag -- )
:noname ( addr u tag -- flag )
    \G Decrypt message in buffer addr u, with auth check
    { tag } @keccak -rot KeccakDecryptLoop
    keccak*
    128@ keccak-checksums keccak#cks keccak>
    keccak-checksums tag 7 and 4 lshift + 128@ 128=
; to c:decrypt+auth ( addr u tag -- flag )
:noname ( addr u -- )
\G Hash message in buffer addr u
    BEGIN  2dup keccak#max umin tuck
	dup keccak#max u< IF
	    keccak-padded keccak#max >padded
	    keccak-padded keccak#max
	THEN  >keccak  keccak*
    /string dup 0= UNTIL  2drop
; to c:hash
:noname ( addr u -- )
\G Fill buffer addr u with PRNG sequence
    BEGIN  keccak*  2dup keccak#max umin tuck keccak>
    /string dup 0= UNTIL  2drop
; to c:prng
:noname ( addr u -- ) >keccak keccak* ;
\G absorb + hash for a message <= 64 bytes
to c:shorthash
' keccak> ( addr u -- )
    \G extract short hash (up to 64 bytes)
to c:hash@
:noname ( x128 addr u -- )
    \G set key plus tweak
    keccak-padded keccak#max dup 2/ /string move-rep
    keccak-padded keccak#max 2/ bounds DO
	64over 64over I 128!  $10 +LOOP  64drop 64drop
    keccak0 keccak-padded keccak#max >keccak ;
to c:tweakkey!
    
crypto-o @ Constant keccak-o
