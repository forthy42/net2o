The Trust Problem
=================

Cryptography gives the promise of privacy. A communication is
secret for everybody except those who have the key to decrypt the
message. So Alice and Bob, the two communication partners used in
cryptography examples, have a shared secret, which they use to
exchange messages. Eve, the eavesdropper, does not know this secret,
and therefore can not read the messages, nor manipulate the
communication without being noticed (she can always stop the
communication by cutting the line, and she still may be able to know
that it's Alice and Bob, who are communicating, by looking at the
routing information of the packets she sees).

Key Exchange
-------------

Now, how do Alice and Bob establish a shared secret? This is a crucial problem
to cryptography, the [key
exchange](http://en.wikipedia.org/wiki/Key_exchange). The English Wikipedia
article tells you how they could do that: If Alice and Bob wish to exchange
encrypted messages, each must be equipped to encrypt messages to be sent and
decrypt messages received. The nature of the equipping they require depends on
the encryption technique they might use. If they use a code, both will require
a copy of the same codebook. If they use a cipher, they will need appropriate
keys. If the cipher is a symmetric key cipher, both will need a copy of the
same key. If an asymmetric key cipher with the public/private key property,
both will need the other's public key. For the cases, where both parties need
the same thing, they need a secure channel to exchange this. Now, if they
already have a secure channel, they might as well exchange the message using
this secure channel—the only advantage cryptography has then, is that the
secure channel might be costly, or rarely available (e.g. a personal meeting
is required to set up the system).

Diffie-Hellman
--------------

Now, with public key cryptography, the Diffie-Hellman
key exchange promises to solve this problem. The key is split into two parts,
one of which can be made public, but only when both are used together, a shared
secret can be established. There is only one drawback of the Diffie-Hellman
exchange: The two parties who wish to establish a connection don't know their
identity. Is it really Alice and Bob, or is it Eve, who cut the line in the
middle, and attacks the connection by performing a Man-in-the-middle attack,
pretending to Alice that she's Bob, and pretending to Bob that she's Alice? To
solve this, various attempts at creating a PKI have been started. The most
widely used PKI attempt is that of SSL, and it is a failure. I need to explain
what SSL does to ensure that identities are correct:

SSL's PKI attempt
-----------------

SSL uses Certificate Authorities (CAs) to sign public
keys. The message of this signature is "someone gave us some money, told us he
has this domain, and he gave us this public key." The "premium" signatures
usually mean "he gave us more money". This is big business, so you can expect
that the most trustworthy members drop out earliest—because someone paid them
a lot of money (Mark Shuttleworth sold Thawte, the first CA, for $500M to
VeriSign in 1999). However, the actual trustworthyness of the CAs itself is not
the real problem. The real problem is that any CA can sign any combination of
domain name and public key, as they like. And any intruder into one of the CAs,
who get access to the signing script can do the same. This is what happened
with DigiNotar. An intruder used DigiNotar's signing key to create a
*.google.com certificate. Iran used this certificate to spy on users who used
Google. This came to light, because Google does not really trust the SSL
scheme, and Chrome has a priori knowledge over the google.com domain
signatures, which are signed by Google's own CA. Iran needed to intrude some
other CAs like DigiNotar, because they don't have their own CA, while e.g.
China or the USA have one. Now you have that trust problem again: You don't
know which of the 600 CAs are trustworthy and which are not. And it is
sufficient if <b>one</b> of them is not, even when the vast majority would be
ok. Oh shit!

The Broken Promise
------------------

It turns out that the promise of Diffie-Hellman does not hold. To
verify the identity of your communication partner, you still need a
secure channel—this time it's a channel Alice ⇔ CA ⇔ Bob, which
allows Alice and Bob to verify their identities. If this channel was
really secure, they could exchange their keys directly, without the
Diffie-Hellman key exchange. The advantage of the SSL approach is that
the CAs aren't involved in the actual key exchange, only in signing
the public keys. But this has to be a secure channel.

Looking for a Solution
----------------------

Now, how to solve that problem? Society always had problems with
people not being trustworthy, and the Chinese approach to this problem
is called "关系" (pinyin: guānxì, Relationship). You don't talk to
strangers, you only talk to people you already know. To create new
relationships, you need to use "connections", i.e. people who know
both you **and** the other side.  That is similar to the CA relation
above, but this time, the trust model is slightly different: You
delegate trust to the people you know.  For person-to-person meetings,
direct key exchange shall be facilitated by using QR-codes and
scanners (smartphone cameras) and search for key prefixes to avoid
having to type in too many cryptic characters.

More important howerver is to directly use the public key whenever possible.
Net2o uses the pubkeys as handles.  For human readability, these names are
converted to nick- and petnames (names you have assigned to other people) when
displayed.  In many cases, [Zooko's
Triangle](https://en.wikipedia.org/wiki/Zooko%27s_triangle) then doesn't
apply.  It's decentralized, as you make connections through peers.  The
connection between keys and nick/petnames is local, in your "address book".
Nick- and petnames are memorizable.

If you get a key, you are able to obtain a nickname corresponding to
that key, self-signed.  This is secure, and still human meaningful.
Many people can have the same nickname, conflicts are resolved by
numbering the nicknames; you can choose petnames to disambiguate.

Why Still Use a PKI?
--------------------

What advantages does a public key system still offer?

* In the client-server communication case, it is sufficient that the client
verifies the server, the other way round doesn't matter. Thus, while every
client should verify the identity of the server, it is not necessary that the
server verifies the identity of the client. It therefore also does not need to
store the client's identity, and—that is more important—maybe the client wants
to change its presented public key frequently to avoid being tracked
(anonymity)
* When we allow one indirection in the server's identity, we can
have temporary server keys, signed by a constant identity—this allows to
reduce the risk that an intruder on the server can steal the keys—they are
temporary, the permanent keys are only needed for off-line signatures, so an
intruder can not get more than he can get anyways—the data while he's in,
before he's discovered.
* We can easily replicate public keys and store them before we even
need them (e.g. based on popularity), reducing the costs when we
actually need to create a connection.

Summary
-------

In summary: Diffie-Hellman does not solve the key exchange
problem. You still need a secure channel, now to validate identity,
not to exchange keys. The problem however remains the same. The
evaluation, whether a PKI is secure now is identical to evaluate
whether you can use it to exchange symmetric keys. There are still
advantages of public keys to not abandon them completely.
