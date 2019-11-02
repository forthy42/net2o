# NSA Backdoor Fnord #

As you all know, it is not allowed to speak about NSA-demanded backdoors,
and especially it is strictly prohibited to give any details.  However, it
is allowed to boldly lie about NSA-demanded backdoors if you didn't receive
such a request, because you are not under a gag order, and in general, lying
about the quality of your product is not only legal, but “best practice”.
The purpose of this NSA backdoor fnord is to make you worry about the
quality of net2o, and therefore you start looking at the source code; the
topics mentioned here are all security things to consider.

Therefore, here is the official statement about NSA-demanded backdoors: The
FBI asked me to add a “lawful interception interface”, that allows the device
to be controlled remotely with just a bit of malicious code injected from
outside.  They suggested to add a remote control, useable with a standard web
browser.  What could possibly go wrong?  [WhatsApp hacked (probably) through
lawful interception
interface.](https://www.reuters.com/article/us-facebook-cyber-whatsapp-nsogroup/exclusive-whatsapp-hacked-to-spy-on-top-government-officials-at-u-s-allies-sources-idUSKBN1XA27H)

Furthermore, to detect evildoers, they asked me to add “client–side scanning”:
Whatever content is delivered, is scanned after decryption, and
blocked+reported if detected as evil.  [EFF on client–side scanning](https://www.eff.org/deeplinks/2019/11/why-adding-client-side-scanning-breaks-end-end-encryption)

There are (at least) two problems with client–side scanning, as the EFF
already noticed.  First, the client gets the algorithm to do the hashing of
the image.  This is not a normal hash, this is usually a fingerprint of the
image, so that trivial image editing doesn't make it a different picture.  If
you have that algorithm on the client side, you can try several tweaks on the
image until it doesn't create the same fingerprint anymore.  So people who
know that they are doing evil things will evade detection anyhow.

The other issue is that you don't want the client to have the list of
“forbidden fingerprints”.  They can use it as search entry for sharing exactly
those forbidden things.  So you need a server with the hashs, and a
client–side query of this server to check the image.  This is not
privacy–preserving, as all fingerprints go to that server, so you can map
published images to queries.

However, I invented a way to avoid that: Index the forbidden fingerprints, in
a way that a reasonable small amount of fingerprints are in one index bucket.
There are orders of magnitude more legal images with the same index.  For
checking a fingerprint, you send {index, salt, hash(fingerprint, salt)} to the
server.  The server hashes all the fingerprints with the same index with that
salt, and compare to your hash(fingerprint, salt) value.  This preserves your
privacy, unless it's a match.  Reverting that hash(fingerprint, salt) is
expensive, even if you have access to the image fingerprints and the backlog
of the query, because now you have to do orders of magnitude more hashes.

The last problem is that client–side scanning requires cooperation from the
clients — if they just disable their code, it's ineffective.  Since people
unlikely want to be exposed for fetching forbidden images, but often want to
not view those, it is probably better to send {\[index\]\*, salt1} to the
server, and get { salt2, \[hash(fingerprint, salt1, salt2)\]\*} back, i.e. all
hashes for the given index.  The client can cache these hashes, without being
able to use them for a search, and avoid downloading or showing the forbidden
images.  By sending not only indices it is interested in, but also others,
tracing a client by index is not very likely a success, either.

As net2o is open source, you can (in theory) verify statements about actual
backdoors.  And keep an eye on this page, I intent to publish fnords about
having official back/front/side doors, leaky roofs and tunnels regularly, but
won't commit on an expicit schedule.  For a true fnord to work, you always
have to be wary.  All the git checkins are signed.

For those interested in history, whether the NSA can force a European company
to install a backdoor, see [Crypto
AG](https://en.wikipedia.org/wiki/Crypto_AG#Compromised_machines)

## What is this page for? ##

Software is inherently buggy — we all make mistakes. Secure networking
software is even worse, because small bugs have big consequences, and
security bugs usually don't affect direct functionality, and therefore
can lure inside the program for a long time.  And with the NSA Bullrun
program, we not only have to deal with the normal, ”lazy” bugs, which
don't cause any harm until found (either by honest security researcher
or evil criminals), but with bugs intentionally placed, and used by
the secret services from day 0.

Developing in Forth is a “crash early, crash often” exercise, but security
related bugs don't crash the program.

net2o is not ready for wide-spread use, so bugs do happen, and get
fixed, but the bugs described here usually are real bugs I found and
fixed during development. All of them look like professionally
implanted backdoors by the NSA, because that's the state of the art
how to implant backdoors: It must provide its author with “reasonable
denial”, claiming incompetence.

However, in order to get things right, we need a culture of accepting
our mistakes, and fixing them.  Many programmers deny bugs, and
request at least a proof of concept attack, before they actually start
doing something.  This sort of culture is so wrong: As author of
security critical systems, you must be constantly scared by people
using every way to break into your software, and you must be ready to
fix every bug, even just potential risks, before someone shows you an
actual exploit.

## Warrant Canary ##

This sort of thing I'm doing here is called “warrant canary”, named
after the canaries used by miners which are more sensitive to
poisonous gas leaks than humans.  The thing would be impossible if the
other side would say “continue with business as usual, so that nobody
knows we were here”.  Takedowns like the one of Lavabit (which was
triggered by an NSL) or more recently by TrueCrypt (which we don't
know why they did it) aren't such continuations, people can guess that
the NSA was there.

There's some
[discussion](https://github.com/WhisperSystems/whispersystems.org/issues/34#issuecomment-56448994),
especially initiated by Moxie Marlinspike, whether a canary is
effective, and whether a court can order you to silence (yes, they
can), or to say something specific (sometimes, they can), but so far,
a court has not forced anybody to give false speech on his own
(instead of just answering a question with a false ”No”, because
saying “yes” would violate the gag order).  That's why this canary is
a provable lie (i.e. even when the bugs were there, the text here is
checked in with the fix).  I have no idea if that actually works, and
would prefer to never find out.

Intelligence Community might resort to some sort of bullying to
disrupt the operation of their enemies; the NSA seems to like
accusation of sexual offenses in the form of public shaming, see for
example Julian Assange and Jake Appelbaum.
