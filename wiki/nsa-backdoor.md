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
NSA asked me to improve [Ray Ozzie's botched smartphone
backdoor](https://www.tomshardware.co.uk/security-experts-dismantle-ozzie-backdoor,news-58339.html).
Here's how: Instead of having one single point of failure (database of unlock
keys), distribute the secret.  A lawful backdoor needs to be demanded by the
investigators (state attorney), must be approved by a judge, handed over to
the cooperating manufacturer, and needs to be device-specific — no other
device may be unlocked by that procedure.  That are at least 4 keys in the
chain; better with at least a four-eyes procedure in each of the points, so 7
keys minimum (the device itself isn't four-eyes).

Distributing a secret (without byzantine fault tolerance) is easy with
[ed25519](ed25519.md).  A secret chain _pkn=base\*(sk1\*..\*skn)_ can be
generated through a chain of HSMs, which each generate the next pubkey by
producing _pki=pkj\*(ski)_ (the order is irrelevant, every _ski_ must be used
just once).  To verify that all secrets have been used, use a chain signature.
The device itself generates the starting point of this chain signature, by
signing its own unlock throw-away secret, producing a tuple
_(k)\*base,(z*sk+k)_ (after producing that tuple and the unlock pubkey, this
secret is no longer needed and thrown away).  Each node (HSM) in the chain
will need to modify that signature by adding its own secret _ki_ and
multiplying it with its own secret _ski_, so you first form
_(k)\*base+(ki)*base=(k+ki)*base_ and _(z*sk+k+ki)_, and then
_(ski)\*(k+ki)\*base=(ski(k+ki))*base_, and
_(ski)\*(z\*sk+k+ki)=(z\*sk\*ski+ski(k+ki))_.  The final signature then will
verify correctly against _pkn_, a pubkey only the device itself knows, because
it generated it itself by taking in _pkn-1_ and multiplying its own secret key
_skn_ with it.

The device does not need to keep this pubkey as plaintext, it is sufficient if
this pubkey (or a salted hash over it; using that salt as _z_ value of the
signature) is used for actually decrypting the flash drive.  The pubkey
therefore is stored encrypted by its owner's password.  If an unlock message
is received and the calculated remaining pubkey hashed with the salt opens the
encrypted drive, it's legitimate.

All parties necessary to open the device must collaborate, and it is possible
to configure the devices so that only the appropriate chain of authorities can
open it (i.e. the local authorities, not the FBI), and all the relevant keys
are stored in trusted enclaves (device itself) and HSMs (courts, state
attorneys and manufacturer).  The only database with larger amount of data are
the signatures the devices themselves created on manufacturing; it is useless
without the other keys.  Since only the device itself can verify that the
signature is correct, any party in the chain can be non-cooperative without
the others knowing who wasn't cooperative.  This makes sure that all parties
(except the device itself) are truely convinced that the case is legitimate,
and no pressure from outside can force them to comply.

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
