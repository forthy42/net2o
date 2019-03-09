# Encryption #

To protect privacy, everything is encrypted with the strongest encryption
available. The reasons for selecting the algorithms were:

## Key Exchange and Signatures ##

* RSA: Reasonable (128bit) strength requires at least 3kbit key size;
  factoring is not as hard as originally assumed; the algorithm are still
  above polynomial, but way below brute force, and any further breakthrough
  will require to increase the key size to unreasonable limits. With a 4kbit
  key (512 bytes per key), the connection setup information won't fit into the
  1kB packets.
* Diffie-Hellman discrete logarithm has essentially the same strength as RSA.
* Elliptic Curve Cryptography is still considered "strong", i.e. there is only
  the very generic big/little step attack, which means a 256 bit key equals
  128 bit strength.

The selection therefore was Ed25519, a Edwards form variant of Dan Bernstein's
curve25519.  Edwards form is notationally simpler and regular than other
curves, allowing more optimizations.  The parameters of this curve are
known-good, following the "nothing up my sleeve" principle.

### Key Exchange Procedure ###

The first phase of a key exchange uses ephemeral (one-time) keys. Let's call
the initiator Alice, and the connected device Bob:

1. Alice generates a key pair, and sends Bob the public key, together with a
   connection request.
1. Bob creates a key pair and sends Alice the public key. Using this public
   and secret key, he generates a shared secret1, and uses that to encrypt his
   permanent public key (used for authentication). An attacker can see the
   ephemeral key, but not the permanent pubkey.  Bob puts his state in an
   encrypted string where only Bob knows the key, and sends this "ticket" back
   to Alice.  Receiving the ticket will actually open up the connection.
1. Alice receives both keys and can now create two shared secrets: secret1 is
   the ephemeral secret, secret2 is the authentication secret.  She sends her
   authentication pubkey back to Bob encrypted with secret1.  This allows Bob
   to compute secret2.  Furthermore, Alice sends back Bob's ticket and a
   random per-connection seed for the symmetric keys; the ticket can be (in
   theory) used to open several connections to Bob with a single packet (no
   reply required).

The general formula for ECC Diffie-Hellman key exchange is _secret =
pk1\*(sk2) = pk2\*(sk1)_. For secret2, I modify this to avoid side-channel
attacks in the lengthy curve point computation, and use _secret2 =
pka\*(skb\*secret1) = pkb\*(ska\*secret1)_.  The scalar multiplication in mod
_l_ (the number of curve points) is much faster than the curve point
computation, and is much less likely to leak information.

## Symmetric Crypto ##

The requirement is AEAD: Authenticate and encrypt/decrypt
together.  Candidates were:

* AES in CGM — this has two problems.
  1. CGM is not a secure hash, and the GF(2^n) field used gives  security
     level of only about 64 bits for 128 bits checksum.
  2. AES uses a constant key, and therefore, side-channel attacks are more
     likely to succeed.
* xsalsa/salsa20+poly1305: This uses a stream cipher and a GF(p) polynom,
  which provides full 128 bit security for the 128 bit checksum, but the
  security of the checksum depends on the encryption.  There's a low risk that
  the proof here is basing on wrong assumptions.  As a stream cipher, there is
  no constant key, so side-channel attacks are more difficult.  This
  combination wins over AES/CGM.
* Keccak in duplex mode provides both encryption and strong authentication,
  which does not depend on the encryption.  The checksum is a keyed plaintext
  checksum, so it actually protects the plain text, and proves knowledge of
  the key at the same time.  Verification of the packet is possible without
  actually decrypting it (i.e. it _also_ is a ciphertext checksum).  Strength
  is >256 bits, providing a very high margin.  Furthermore, Keccak/SHA-3 is a
  universal crypto primitive, so everything needed for symmetric crypto is
  done with just one primitive.  Keccak wins over xsalsa/salsa20+poly1305.
