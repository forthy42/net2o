\ generic crypto api for net2o

require mini-oof2.fs

\ For wurstkessel compatibility, the states are all 128 bytes
\ If the cryptosystem has more internal state, it may copy the key+iv there.
\ If it has less, it should use a suitable fraction of the key and the iv

User crypto-o

Create crypto-m 0 ,  DOES> @ crypto-o @ cell- @ + perform ;
comp: >body @ cell/ postpone u#exec crypto-o next-task - , , ;
' to-m set-to

' crypto-m to method-xt

object class
    method c:key! ( addr -- )
    \G use addr as key storage
    method c:key@ ( -- addr )
    \G obtain the key storage
    method c:key# ( -- n )
    \G obtain key storage size
    method >c:key ( addr -- )
    \G move 128 bytes from addr to the state
    method c:key> ( addr -- )
    \G get 128 bytes from the state to addr
    method c:diffuse ( -- )
    \G perform a diffuse round
    method c:encrypt ( addr u -- )
    \G Encrypt message in buffer addr u
    method c:decrypt ( addr u -- )
    \G Decrypt message in buffer addr u
    method c:encrypt+auth ( addr u -- )
    \G Encrypt message in buffer addr u
    method c:decrypt+auth ( addr u -- flag )
    \G Decrypt message in buffer addr u
    method c:hash ( addr u -- )
    \G Hash message in buffer addr u
    method c:prng ( addr u -- )
    \G Fill buffer addr u with PRNG sequence
    method c:checksum ( -- xd )
    \G compute a 128 bit checksum
    method c:cookie ( -- x )
    \G compute a different checksum
end-class crypto