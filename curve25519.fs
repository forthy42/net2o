\ Interface to the Curve25519 primitive of the NaCL library

\ for ease to deploy, there is just the crypto_scalarmult primitive
\ everything else is done with Wurstkessel, anyways

c-library curve25519
    s" ./curve25519/smult.c" ' slurp-file catch
    0= [IF] save-c-prefix-line [ELSE] 2drop [THEN]

    c-function crypto_scalarmult crypto_scalarmult a a a -- void ( s pk sk -- )
end-c-library

32 Constant KEYBYTES

KEYBYTES buffer: base9
base9 KEYBYTES erase 9 base9 c!


