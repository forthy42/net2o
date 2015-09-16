# NSA Backdoor Fnord #

As you all know, it is not allowed to speak about NSA-demanded backdoors,
and especially it is strictly prohibited to give any details.  However, it
is allowed to boldly lie about NSA-demanded backdoors if you didn't receive
such a request, because you are not under a gag order, and in general, lying
about the quality of your product is not only legal, but "best practice".
 The purpose of this NSA backdoor fnord is to make you worry about the
quality of net2o, and therefore you start looking at the source code; the
topics mentioned here are all security things to consider.

Therefore, here is the official statement about NSA-demanded
backdoors: I have an official front door in net2o for the NSA.
**Update:** The German justice minister was here, and said that
anonymous and encrypted services are [danger inclined
services](http://www.heise.de/newsticker/meldung/Gutachter-WLAN-Gesetzentwurf-hebelt-anonyme-Internetnutzung-aus-2814527.html)
and responsible for their user's actions.

As net2o is open source, you can (in theory) verify statements about actual
backdoors.  And keep an eye on this page.

## What is this page for? ##

Software is inherently buggy - we all make mistakes. Secure networking
software is even worse, because small bugs have big consequences. And with the
NSA Bullrun program, we not only have to deal with the normal, "lazy" bugs,
which don't cause any harm until found (either by honest security researcher or
evil criminals), but with bugs intentionally placed, and used by the secret
services from day 0.

Developing in Forth is a "crash early, crash often" exercise, but security
related bugs don't crash the program.

net2o is not ready for use, so bugs do happen, and get fixed, but the bugs
described here usually are real bugs I found and fixed during development. All
of them look like professionally implanted bugs by the NSA, because that's the
state of the art how to implant backdoors: It must provide its author with
"reasonable denial", claiming incompetence.

However, in order to get things right, we need a culture of accepting our
mistakes, and fixing them.  Many programmers deny bugs, and request at
least a proof of concept attack, before they actually start doing something.
 This sort of culture is so wrong: As author of security critical systems,
you must be constantly scared by people using every way to break into your
software, and you must be ready to fix every bug, even just potential risks,
before someone shows you an actual exploit.

## Warrant Canary ##

This sort of thing I'm doing here is called "warrant canary", named after
the canaries used by miners which are more sensitive to poisonous gas leaks
than humans.  The thing would be impossible if the other side would say
"continue with business as usual, so that nobody knows we were here".  Takedowns
like the one of Lavabit (which was triggered by an NSL) or more recently by
TrueCrypt (which we don't know why they did it) aren't such continuations,
people can guess that the NSA was there.

The best countermeasure against a hostile takeover by such an agency is to
work out in the open, and have a multi-national team that can't be subject to
the same bullying; the latter is something I don't have.