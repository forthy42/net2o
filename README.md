net2o - reinventing the Internet
================================

net2o is the attempt to reinvent the Internet.

Get it and try it
-----------------

net2o is currently under early alpha test. [Get
it](https://fossil.net2o.de/net2o/doc/trunk/wiki/get-it.md) and [try
it](https://fossil.net2o.de/net2o/doc/trunk/wiki/try-it.md).  There
are lots of things [to
do](https://fossil.net2o.de/net2o/doc/trunk/wiki/todo.md).

What's broken?
--------------

* The internet bases on assumptions 20-30 years old
* These assumptions are wrong today
* Much of it followed the "good enough" principle
* There's a huge pile of accumulated cruft
* Fixing this mess one-by-one is the wrong attempt

What for?
---------

I've a dream: A peer-to-peer network, where services like search engines or
social networks aren't offered by big companies, who in turn need to make money
by selling the privacy of their users. Where all data is encrypted, so that
access is only possible for people who have the key and really are authorized.
Which layman can use without cryptic user interfaces. Where the browser is a
platform for running useful applications without the mess of Flash and
JavaScript. Without the lag of "buffer bloat" and without the speed problems of
a protocol not designed to be assisted by hardware.

What needs to be fixed?
-----------------------

* IP: Use switch-based simple routing, don't route every packet
* TCP: Most data just needs to be obtained reliable, the order doesn't
  matter. And TCP's flow control is broken (delay minimized is the way to go).
* Encryption everywhere: This is no longer an expensive operation (and for
  symmetric encryption, fast hardware implementation is feasible)
* P2P instead of client-server
* A new API (render layer, markup language, scripting) - the browser is there
  for serious applications now.

Rendering is done with OpenGL ES, GUI layer is MINOΣ 2.

net2o has been inspired by Open Network Forth from Heinz Schnitter.
