# Threefish AEAD mode #

I use Keccak in Duplex mode, which gives both encryption and
authentication in one go (it's an AEAD cipher - authenticated
encryption and decryption).  For block ciphers, AEAD usually requires
a second function, e.g. a hash or at least a good enough checksum
protected by the cypher itself.

However, for Threefish, there's a reasonably good hash mode, with
"reasonably good" as in "was finalist in the SHA-3 competition" (as
crypto primitive for the Skein3 hash function).  None of the finalists
failed for security weaknesses; Threefish is just slower when
implementd in hardware.

Now, unlike Keccak, the Skein mode for Threefish can not be used to
encrypt and hash the plaintext in one go.  Even though the algorithm
actually does blockwise encrypt the message using Threefish, and
exchanges the key for each block.  Why?  The next block is encrypted
using the xor of plaintext and ciphertext of the previous block as
key.  With a known plaintext attack, you can deduce the key for
everything following the block where you know the plaintext (and
there, you don't need it).

And that's even though you can start Skein3 with an arbitrary key,
producing a keyed hash (good enough for HMAC).

So I added a third input into that xor for the next key: xor with the
previous key.  Now that simple known plaintext attack stops working.
As this mode for Threefish has not seen good review, I won't suggest
using it now.