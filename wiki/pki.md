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

Now, how do Alice and Bob establish a shared secret? This is a
crucial problem to cryptography, the [key exchange](http://en.wikipedia.org/wiki/Key_exchange). The
English Wikipedia article tells you how they could do that: If Alice
and Bob wish to exchange encrypted messages, each must be equipped to
encrypt messages to be sent and decrypt messages received. The nature
of the equipping they require depends on the encryption technique they
might use. If they use a code, both will require a copy of the same
codebook. If they use a cipher, they will need appropriate keys. If
the cipher is a symmetric key cipher, both will need a copy of the
same key. If an asymmetric key cipher with the public/private key
property, both will need the other's public key. For the cases, where
both parties need the same thing, they need a secure channel to
exchange this. Now, if they already have a secure channel, they might
as well exchange the message using this secure channel - the only
advantage cryptography has then, is that the secure channel might be
costly, or rarely available (e.g. a personal meeting is required to
set up the system).

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
that the most trustworthy members drop out earliest - because someone paid them
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

It turns out that the promise of Diffie-Hellman does
not hold. To verify the identity of your communication partner, you still need
a secure channel - this time it's a channel Alice ⇔ CA ⇔ Bob, which allows
Alice and Bob to verify their identities. If this channel was really secure,
they could exchange their keys directly, without the Diffie-Hellman key
exchange. The advantage of the SSL approach is that the CAs aren't involved in
the actual key exchange, only in signing the public keys. But this has to be a
secure channel.

Looking for a Solution
----------------------

Now, how to solve that problem? Society always
had problems with people not being trustworthy, and the Chinese approach to
this problem is called "关系" (pinyin: guānxì, Relationship). You don't
talk to strangers, you only talk to people you already know. To create new
relationships, you need to use connections, i.e. people who know both you **and**
the other side. Now how do we transfer this model to a PKI? Fairly simple: Each
client stores the relation "domain, public key" it sees. This is something that
needs a costly secure channel, so we need to cache as much as we can, and not
do this every time again when we open a connection. When we want to connect to
a new site, we ask peers we already know about the public key of this site.
These peers can be search engines, name servers, distributed hash tables spread
over our peers, or Facebook "friends", and - this is important - it should be
several, the more, the better - maybe we have troubles when we start to create
our networks, but the browser manufacturer will provide us with a usable seed
(e.g. search engines). If they do agree, we are ok. If not, we are in trouble.
We can fall back on majority decisions, and raise flags, i.e. make this
discrepancy public, and trigger more careful examination. However, remember
what I said about that this is a secure channel? We could actually forget about
doing a Diffie-Hellman key exchange, and use this quest to exchange an actual
key. No, we don't want the search engines and our Facebook "friends" to know
our actual key, but we don't need to tell them. We have to use several of those
connections, anyways, because a single one is too weak and too easy to corrupt.
So we add enough forward error correction codes to our secret (to be shared)
that it can be recovered with a given amount of bit errors, e.g. as long as
there are more than 80%, it's recoverable, otherwise, not. Then we split it
into parts, which contain less information, and send different portions over
different channels - partly overlapping. The other side now can compose the
secret out of the parts, and even check if it can trust each connection:
manipulations are obvious, because each key part contains common parts with
others, and those have to match. Once we have assembled enough key parts to
recover the remaining errors, we are done, we can establish a direct, secure
connection.

Why Still Use a PKI?
--------------------

What advantages does a public key system still
offer? * In the client-server communication case, it is sufficient that the
client verifies the server, the other way round doesn't matter. Thus, while
every client should verify the identity of the server, it is not necessary that
the server verifies the identity of the client. It therefore also does not need
to store the client's identity, and - that is more important - maybe the client
wants to change its presented public key frequently to avoid being tracked
(anonymity) * When we allow one indirection in the server's identity, we can
have temporary server keys, signed by a constant identity - this allows to
reduce the risk that an intruder on the server can steal the keys - they are
temporary, the permanent keys are only needed for off-line signatures, so an
intruder can not get more than he can get anyways - the data while he's in,
before he's discovered. * We can easily replicate public keys and store them
before we even need them (e.g. based on popularity), reducing the costs when we
actually need to create a connection.

Summary
-------

In summary: Diffie-Hellman does not solve the key exchange
problem. You still need a secure channel, now to validate identity, not to
exchange keys. The problem however remains the same. The evaluation, whether a
PKI is secure now is identical to evaluate whether you can use it to exchange
symmetric keys. There are still advantages of public keys to not abandon them
completely.