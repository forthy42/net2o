net2o - reinventing the Internet
================================

net2o is the attempt to reinvent the Internet.  It's free software
available under the [AGPLv3](https://www.gnu.org/licenses/agpl-3.0.en.html).

Authors
-------

Principal author of net2o is

* Bernd Paysan <bernd@net2o.de>

Get it and try it
-----------------

net2o is currently under early alpha test. [Get it](get-it.md) and
[try it](try-it.md).  There are lots of things [to do](todo.md).

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
* [Threefish](https://www.schneier.com/threefish.html) as block cipher
  in ECB mode and in [Threefish AEAD mode](threefish.md) as backup for Keccak.
* ([wurstkessel](wurstkessel.wiki) from myself - now replaced by Keccak)

Documentation
-------------

The documentation is work in progress. The seven layers of net2o are not
equal to the ISO-OSI layers, but this layering provides a familiar starting
point:

1. Physical layer - this is not part of net2o itself.
2. [Topology](topology.md)
3. [Encryption](encryption.wiki)
4. [Flow Control](flow-control.md)
5. [Commands](commands.md)
6. [Distributed Data](distributed-data.wiki)
7. [Applications](applications.wiki)

Videos & Presentations
----------------------

+ [Original whitepaper (for historical purposes
  only)](https://net2o.de/internet-2.0.html)
+ [EuroForth 2009 presentation](https://net2o.de/internet-2.0.pdf)
+ [EuroForth 2010 presentation](https://net2o.de/net2o.pdf)
+ [EuroForth 2011 presentation](https://net2o.de/net2o-al.pdf)
+ [EuroForth 2012 presentation](https://net2o.de/net2o-tl2.pdf)
+ [EuroForth 2013 — net2o application layer](https://wiki.forth-ev.de/doku.php/events:euroforth-2013:n2oal)
+ [EuroForth 2014 — net2o command language](https://wiki.forth-ev.de/doku.php/events:euroforth-2014:net2ocl)
+ [31c3](31c3.md) presentation
+ [32c3](32c3.md) presentation
+ [EuroForth 2016](https://wiki.forth-ev.de/doku.php/events:euroforth-2016:using-net2o) presentation
+ [33c3](33c3.md) presentation
+ [EuroForth 2017](https://wiki.forth-ev.de/lib/exe/fetch.php/events:ef2017:minos2.mp4) presentation (MINOΣ 2)
+ [34c3](34c3.md) presentation (alternative source: [Chaos West's capture](https://media.ccc.de/v/34c3-ChaosWest-15-net2o_gui_realtime_mixnet_and_ethical_micropayment_with_efficient_blockchain))

Discussions
-----------

* The [pki](pki.md) problem
* [Client authentication](client-auth.md)
* [Handover](handover.wiki)
* [Ack cookies](ackcookies.wiki)
* [Random number seat belts](rng.md)
* [Key format](key-format.wiki)
* [Key revocation](key-revocation.md)
* [NSA backdoor](nsa-backdoor.md)
* [Data retention](data-retention.md)
* [Onion Routing](onion-routing.md)
* [Threat Model](threat-model.md)
* [What it's not for](whatnotfor.md)
* [Nettie logo](nettie.md)
* [$quid CryptoCurrency](squid.md)

[de](/net2o/wiki?name=net2o.de)
[中文](net2o.zh.md)

No Wikipedia Links
------------------

Temporarily, all links to Wikipedia are deactivated here, as reaction
to the LG Hamburg court decision which says that all links to sites
which may contain inproperly attributed photos are copyright
violations.  And Wikipedia contains inproperly attributed photos of
the suspected plaintiff of that case, Thomas Wolf.
