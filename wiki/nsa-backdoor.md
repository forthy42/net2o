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
outside. [WhatsApp hacked through lawful interception interface](https://www.reuters.com/article/us-facebook-cyber-whatsapp-nsogroup/exclusive-whatsapp-hacked-to-spy-on-top-government-officials-at-u-s-allies-sources-idUSKBN1XA27H)

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
