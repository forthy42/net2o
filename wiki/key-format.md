# Key Format #

The public key is the primary key (the ID) of a key.  Other fields are:

* Private key (for secret key ring) — the private key may be protected by a
  pass phrase and a pass file
* Nickname (for humans)
* Full name (for humans)
* Creation and expiration dates

## Signatures ##

Keys may be signed, we treat key signatures as separate entities.  A signature
consists of

* The pubkey of the signed key
* The pubkey of the signer
* The “digest”, the cryptographic checksum that proves that the signer has
  signed the key

This feature will be implemented when Dan Bernstein integrates signatures
into NaCl; maybe a bit earlier, since the code for these signatures is already
tested.
