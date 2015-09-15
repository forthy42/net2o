net2o - reinventing the Internet
================================

net2o is the attempt to reinvent the Internet.

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

Rendering is done with OpenGL ES, GUI layer is
[MINOΣ 2](https://fossil.net2o.de/minos2).

net2o has been inspired by Open Network Forth from Heinz Schnitter.

What third party technology do we use
-------------------------------------

* [ed25519](ed25519.md) from Dan Bernstein (in the donna version from
  floodyberry)
* [Keccak](http://keccak.noekeon.org/) (original reference implementation)
* ([wurstkessel](wurstkessel.wiki) from myself - now replaced by Keccak)

How to build it
---------------

You need: A Linux machine; with some luck, you get it to run on Mac OS X, as
well.

You want to have the following packets installed: git automake autoconf make
gcc libtool libltdl7 (libtool-ltdl on RedHat/Centos)

Get the [do](https://fossil.net2o.de/net2o/doc/trunk/do) file
(latest revision), put it into your net2o folder, and let it run. You
need [fossil](http://www.fossil-scm.org/index.html/doc/tip/www/index.wiki); (and
git) as SCM, get the latest from the fossil homepage.  You don't need to
cut'n'paste the whole script, just do the fossil clone+open, then you get your
own do script.  This script will ask for your root password to
install Gforth and the two libraries mentioned above.  After completion,
you can run a test:

    gforth-fast server-test.fs & gforth-fast client-test.fs localhost >timing
    gnuplot -p -e 'load "doc/timing.plt";'

Documentation
-------------

The documentation is work in progress. The seven layers of net2o are not
equal to the ISO-OSI layers, but this layering provides a familiar starting
point:

1. Physical layer - this is not part of net2o itself.
2. [Topology](topology.md)
3. [Encryption](encryption.wiki)
4. [Flow Control](flow-control.wiki)
5. [Commands](commands.md)
6. [Distributed Data](distributed-data.wiki)
7. [Applications](applications.wiki)

Videos
------

[31c3](31c3.md) presentation

Discussions
-----------

* The [pki](pki.md) problem
* [Client authentication](client-auth.wiki)
* [Handover](handover.wiki)
* [Ack cookies](ackcookies.wiki)
* [Key format](key-format.wiki)
* [Key revocation](key-revocation.wiki)
* [NSA backdoor](nsa-backdoor.wiki)
* [Onion Routing](onion-routing.wiki)
* [What it's not for](whatnotfor.wiki)

[de](/net2o/wiki?name=net2o.de)
[中文](net2o.zh.md)
