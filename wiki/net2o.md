# net2o — reinventing the Internet #

net2o is the attempt to reinvent the Internet.  It's free software
available under the [AGPLv3](https://www.gnu.org/licenses/agpl-3.0.en.html).

## Authors ##

Principal author of net2o is

* Bernd Paysan <bernd@net2o.de>

## Get it and try it ##

net2o is currently under early alpha test. [Get it](get-it.md) and
[try it](try-it.md).  There are lots of things [to do](todo.md).

## What's broken? ##

* The internet bases on assumptions 20-30 years old
* These assumptions are wrong today
* Much of it followed the “good enough” principle
* There's a huge pile of accumulated cruft
* Fixing this mess one-by-one is the wrong attempt

## What for? ##

I've a dream: A peer-to-peer network, where services like search engines or
social networks aren't offered by big companies, who in turn need to make money
by selling the privacy of their users. Where all data is encrypted, so that
access is only possible for people who have the key and really are authorized.
Which layman can use without cryptic user interfaces. Where the browser is a
platform for running useful applications without the mess of Flash and
JavaScript. Without the lag of “buffer bloat” and without the speed problems of
a protocol not designed to be assisted by hardware.

[Richard Stallman on how to fix the surveillance
systems](https://www.theguardian.com/commentisfree/2018/apr/03/facebook-abusing-data-law-privacy-big-tech-surveillance)

[Cory Doctorow on how to destroy surveillance capitalism](https://onezero.medium.com/how-to-destroy-surveillance-capitalism-8135e6744d59)

## What needs to be fixed? ##

* IP: Use switch-based simple routing, don't route every packet
* TCP: Most data just needs to be obtained reliable, the order doesn't
  matter. And TCP's flow control is broken (delay minimized is the way to go).
* Encryption everywhere: This is no longer an expensive operation (and for
  symmetric encryption, fast hardware implementation is feasible)
* P2P instead of client-server
* A new API (render layer, markup language, scripting) — the browser is there
  for serious applications now.

Rendering is done with OpenGL ES, GUI layer is
[MINOΣ 2](https://fossil.net2o.de/minos2).

net2o has been inspired by Open Network Forth from Heinz Schnitter.

## What third party technology do we use ##

* [ed25519](ed25519.md) from Dan Bernstein (in the donna version from
  floodyberry)
* [Keccak](http://keccak.noekeon.org/) (original reference implementation)
* [Threefish](https://www.schneier.com/threefish.html) as block cipher
  in ECB mode and in [Threefish AEAD mode](threefish.md) as backup for Keccak.
* ([wurstkessel](wurstkessel.wiki) from myself — now replaced by Keccak)
* [Gforth](https://gforth.org/) GNU Forth

## Documentation ##

The documentation is work in progress. The seven layers of net2o are not
equal to the ISO-OSI layers, but this layering provides a familiar starting
point:

1. Physical layer — this is not part of net2o itself.
2. [Topology](topology.md)
3. [Encryption](encryption.md)
4. [Flow Control](flow-control.md)
5. [Commands](commands.md)
6. [Distributed Data](distributed-data.md)
7. [Applications](applications.wiki)

## Videos & Presentations ##

* EuroForth
  * [Original whitepaper (for historical purposes only)](https://net2o.de/internet-2.0.html)
  * [EuroForth 2009 presentation](https://net2o.de/internet-2.0.pdf)
  * [EuroForth 2010 presentation](https://net2o.de/net2o.pdf)
  * [EuroForth 2011 presentation](https://net2o.de/net2o-al.pdf)
  * [EuroForth 2012 presentation](https://net2o.de/net2o-tl2.pdf)
  * [EuroForth 2013 — net2o application
    layer](https://wiki.forth-ev.de/doku.php/events:euroforth-2013:n2oal)
  * [EuroForth 2014 — net2o command
    language](https://wiki.forth-ev.de/doku.php/events:euroforth-2014:net2ocl)
  * [EuroForth
    2016](https://wiki.forth-ev.de/doku.php/events:euroforth-2016:using-net2o)
    presentation
  * [EuroForth
    2017](https://wiki.forth-ev.de/lib/exe/fetch.php/events:ef2017:minos2.mp4)
    presentation (MINOΣ 2)
  * [EuroForth 2018](https://wiki.forth-ev.de/doku.php/events:ef2018:net2o)
    presentation (MINOΣ 2 + $quid)
  * [EuroForth 2019](https://wiki.forth-ev.de/doku.php/events:ef2019:net2o)
    presentation (Social network in net2o)
* Chaos Communication Congress
  * [31c3](31c3.md) presentation (protocol stack)
  * [32c3](32c3.md) presentation (application layer considerations)
  * [33c3](33c3.md) presentation (progress report + onion routing plan)
  * [34c3](34c3.md) presentation ($quid cyptocurrency) (alternative source: [Chaos West's
    capture](https://media.ccc.de/v/34c3-ChaosWest-15-net2o_gui_realtime_mixnet_and_ethical_micropayment_with_efficient_blockchain))
  * [35c3](https://media.ccc.de/v/35c3chaoswest-21-cloudcalypse-it-looks-like-you-ve-reached-the-end-how-to-take-your-data-into-net2o)
	presentation (Social network in net2o — the plan) (only Chaos West capture this time, as they did a very
	professional job).
  * [36c3](https://media.ccc.de/v/36c3-oio-162-cloudcalypse-2-social-network-with-net2o-ybti-wefixthenet-session-)
    presentation (Social network in net2o — the progress) (Open Infrastructure
    Orbit capture)

## Discussions ##

* The [pki](pki.md) problem
* [Client authentication](client-auth.md)
* [Handover](handover.md)
* [Ack cookies](ackcookies.md)
* [Random number seat belts](rng.md)
* [Key format](key-format.md)
* [Key revocation](key-revocation.md)
* [NSA backdoor](nsa-backdoor.md)
* [Data retention](data-retention.md)
* [Onion Routing](onion-routing.md)
* [Threat Model](threat-model.md)
* [What it's not for](whatnotfor.md)
* [Nettie logo](nettie.md)
* [$quid CryptoCurrency](squid.md)
* [Guidelines of Conduct](guidelines.md)
* [Social network](social.md)
* [Search](search.md)
* [Video conference](videoconference.md)

[de](/net2o/wiki?name=net2o.de)
[中文](net2o.zh.md)
