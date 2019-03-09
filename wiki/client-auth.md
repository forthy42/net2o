# Client Authentication #

As discussed in the [pki](pki.md) section, it is quite difficult to
solve the trust problem in a PKI system where the client has to trust
the server. The centralized Certification Authority scheme is broken,
and a distributed system is hard.

However, let's take a step back, and look **when** do we really want a
secure connection? When we have private data on a server, when we want to
authenticate us, and send our credentials over the Internet.

## Inverted Trust ##

The case we have here thus is the other way round: The server wants to know
our identity, and will present us data only when it trusts us. This is a case
where public keys still help us: it is sufficient when one side of a
communication verifies the identity of the partner, as a man in the middle
attack has to replace both identities to actually intercept the communication
(if there is an identity exchange).

So what happens if we use the public key as sign-in to the server? The user
presents its public key, which establishes a shared secret that allows to
verify that this user is legitimate (i.e. knows his secret key), **and** at
the same time allows to establish a secure connection.

The trust model is again "we know each other" model. This time, we have a
much better position than in the "unknown server case": The first time an
unknown new user accesses a server is when he creates his account. This is
still critical, you don't want to create an account while a man-in-the-middle
attack is ongoing (this is a "captive environment" problem; if you never leave
that environment, the MitM can go on with that attack forever.  A captive
environment usually is inside a company).  But you can invest some effort
into verifying the identity of the server, and the fact that the other side is
a human.

## Identity Captcha ##

And, as a captcha is usually part of signing onto a server, you can test the
connection with something that is hard for an automatic interception system to
emulate. You send a normal captcha (a distorted image, voice, a program that
generates the text through some non-trivial logic, etc.), which the automatic
interception is not able to process. You encrypt the answer to this captcha on
the client side using the shared secret, and sending **only** the encryption
checksum of this answer. The intercepting system can not generate the answer to
the captcha itself, and therefore can not generate the correct checksum. The
intercepted client does not have the correct shared secret, and therefore can't
create the correct checksum even though the user correctly answers the captcha.

This verifies that when creating the account, a secure, non-intercepted
connection was present. The server now stores the user's public key as primary
user ID. Each time the user logs in, his identity is verified. I don't talk
about name and address, what's verified is that the connection was initially
not intercepted, and therefore, when presenting the same public key and using
the same shared secret, it's now also not intercepted.

This is where a public key still is very useful.

## When does this fail? ##

This approach still fails in an environment, where all connections are
intercepted, and the miniluv spends the effort to solve captcha
puzzles, or whatever computationally expensive task (but easy enough
to solve for humans) is done to reduce that risk.

If identity providers like Google, Yahoo, or Facebook would simply use
client certificates to verify the identity of their customers (self-singed
client certificates are perfect, what matters is that the certificate **does
not change**), much of the SSL dilemma would already be solved in a practical
way. No trust chain with the weakest CA as link anymore.
