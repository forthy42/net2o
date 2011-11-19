\ Interface to the NaCL library

c-library nacl
    s" nacl" add-lib
    \c #include <amd64/crypto_box.h>
    c-function crypto_box_keypair crypto_box_keypair a a -- void ( pk sk -- )
    c-function crypto_box crypto_box a a n a a a -- void ( c m mlen n pk sk -- )
    c-function crypto_box_beforenm crypto_box_beforenm a a a -- void ( k pk sk -- )
    c-function crypto_box_open crypto_box_open a a n a a a -- void ( m c clen n pk sk -- )
    \c #include <amd64/crypto_secretbox.h>
    c-function crypto_secretbox crypto_secretbox a a n a a -- void ( c m mlen n k -- )
    c-function crypto_secretbox_open crypto_secretbox_open a a n a a -- void ( m c clen n k -- )
    \c #include <amd64/crypto_stream.h>
    c-function crypto_stream crypto_stream a n a a -- void ( c clen n k -- )
    c-function crypto_stream_xor crypto_stream_xor a a n a a -- void ( c m mlen n k -- )
    \c #include <amd64/crypto_hash.h>
    c-function crypto_hash crypto_hash a a n -- void ( h m mlen -- )
    \c #include <amd64/randombytes.h>
    c-function randombytes randombytes a n -- void ( addr n -- )
end-c-library

32 Constant PUBLICKEYBYTES
32 Constant SECRETKEYBYTES
32 Constant KEYBYTES
32 Constant BEFORENMBYTES
24 Constant NONCEBYTES
32 Constant ZEROBYTES
16 Constant BOXZEROBYTES

