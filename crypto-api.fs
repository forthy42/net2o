\ generic crypto api for net2o

require mini-oof2.fs
require user-object.fs

\ For wurstkessel compatibility, the states are all 128 bytes
\ If the cryptosystem has more internal state, it may copy the key+iv there.
\ If it has less, it should use a suitable fraction of the key and the iv

User-o crypto-o

object class
    umethod c:init ( -- )
    \G initialize crypto function for a task
    umethod c:free ( -- )
    \G free crypto function for a task
    umethod c:key! ( addr -- )
    \G use addr as key storage
    umethod c:key@ ( -- addr )
    \G obtain the key storage
    umethod c:key# ( -- n )
    \G obtain key storage size
    umethod >c:key ( addr -- )
    \G move 128 bytes from addr to the state
    umethod c:key> ( addr -- )
    \G get 128 bytes from the state to addr
    umethod c:diffuse ( -- )
    \G perform a diffuse round
    umethod c:encrypt ( addr u -- )
    \G Encrypt message in buffer addr u
    umethod c:decrypt ( addr u -- )
    \G Decrypt message in buffer addr u
    umethod c:encrypt+auth ( addr u -- )
    \G Encrypt message in buffer addr u
    umethod c:decrypt+auth ( addr u -- flag )
    \G Decrypt message in buffer addr u
    umethod c:hash ( addr u -- )
    \G Hash message in buffer addr u
    umethod c:prng ( addr u -- )
    \G Fill buffer addr u with PRNG sequence
    umethod c:checksum ( -- xd )
    \G compute a 128 bit checksum
    umethod c:cookie ( -- x )
    \G compute a different checksum
end-class crypto