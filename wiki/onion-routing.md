# Onion Routing #

Anonymity is somewhat orthogonal to encryption: The routing information in
your message reveals who's talking to whom.  The TOR project suggests that
anonymity can be achieved by using several hops, and decrypting the message
blocks on each hop, routing them forward to another hop.

The requirements for onion routing cryptography are different from the rest
of net2o cryptography. The intermediate hops don't need to authenticate the
blocks; in fact, it is better when they don't even know who's sending them.

## TOR design problems ##

TOR has some design problems, one is the mostly centralized
dictionary.  Everybody asking the directory server is exposed as
obvious TOR user.  This can be easily mitigated by using a DHT.
Another problem is not related to TOR itself, but to the current
internet: The traffic from TOR exit nodes and beyond is often
unencrypted; as TOR exit nodes are easy to spot (they are listed in
the directory server), they are very likely to get special treatment,
and all traffic is monitored.  Scaring TOR exit node operators into
shutting down their services reduces bandwidth, and makes TOR
difficult to use.

Therefore, anonymous relays only work well when many participants distribute
the load, and can't easily be scared to turn it off.

## net2o onion routing ##

*This is not yet implemented*

As onion routing uses cryptography on already encrypted and
authenticated packets, and should not increase the size of the
packets, I will use a block cipher, with an AES-XEX variant or
Threefish when the cost of AES is too high.  The destination memory
address and the second flag byte will also be encrypted, using ECB
(taking the first part of the message to fill the 16 bytes), the
decrypted memory address is the sector index for AES-XEX or the tweak
for Threefish. This encryption is not tamper-proof, but tampered
packets will be filtered out at the legitimate destination.  The
requirement here is that it is harder to correlate input and output of
a relay through decryption than through other means.

The most interesting problem however here is how to not expose the
routing field, because it contains the path through the onion routing
network, and also selects the proper key.  So the routing field needs
to be encrypted and it is used to identify the connection while
encrypted.  The routing field is still used to forward packets, though
the routing within the onion network is set up before, so it's mostly
a switched circuit network.  Handovers should be negotiated separately.

The good news is that it doesn't need to be ultra reliable.  If we
find that two keys possibly match, we can just decrypt and forward the
packet with two keys to two different destinations.  It will be thrown
away if it doesn't match further, at least at the endpoints.  I'm not
convinced that the normal 16 byte path is sufficient for onion
routing, especially, as the normal path is needed to get from one
onion router hop to the next, too.  So onion routing needs an
additional, longer, constant-size path within the onion network.  Like
the rest of the message, the onion path is encrypted/decrypted on each
hop, and the end node just flips the path so that the inserted
pathlets can be used as return path.

Each element of the path is per-hop encrypted/decrypted in ECB mode,
so encryption and decryption are interchangeable operations (4 times
decrypted with key1 to key4 and then encrypted with key4 to key1 gives
us the plaintext again).  To find the correct key, the router tries
all available keys from a single source, and stops if the decrypted
destination looks legit; since there is no requirement for
ultra-reliability, a short IV and a simple checksum is sufficient
(e.g. 32 bit each).  One bit in the address is used to determine
whether that's the endpoint of the onion routing or not.  If you are
endpoint, you keep the return path and create a normal net2o packet;
the inserted return path allows to identify the onion routing path.

The relay uses the same key for both directions, one direction uses
encryption, the other decryption.  The originator needs to know all
keys, and encrypt with all of them on sending, and decrypt with all of
them on receiving - so the originator actually computes those keys.
Therefore, a fast, hardware-accelerated algorithm is important here;
security concerns about AES weaknesses and mode problems are
secondary.  This is only making it more difficult to compute a
correlation, breaking the encryption therefore should only be harder
than any other mean to correlate packets (e.g. using timing attacks).
The content encryption is below that layer; using an entirely
different algorithm provides additional security: Attackers of relayed
traffic need to break two encryption schemes.  You can setup a relay
to itself on your destination to add an AES encryption on top of
Keccak encryption, the destination then only receives packets which
look like relayed packets.

The relay may slow down packet rates to make correlation harder, but is not
allowed to speed up bursts, because that would break net2o's flow control
(partial speed up inside a burst is allowed, though).  Relays shall do
fair queuing to help the flow control.

Relay generation is just one single command:

    create-relay ( $key algo -- )

Relays automatically are shut down after 1 minute of inactivity;
relays are created with only one-time keys, because you don't want the
relay to know who you are, but you want to know that the relay is the
one you asked, so you want its pubkey, and use another one-time key to
generate the shared secret.  There's no full connection setup with a
relay, setting up a relay should take three messages only.

You can create relay trees (with the root in the target
you talk to), and switch between leaves randomly, as net2o provides perfect
handover support. Different paths shall have different sets of keys, you will
know which path has been used.

## Downsides of Onion Routing ##

Onion routing increases latency and traffic. If you want to minimize
on that, use relays close to you (in terms of network distance). As
big internet exchanges are much more likely to be monitored, local
relays also provide less means to collect correlations.
