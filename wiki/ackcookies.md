# Ack cookies #

net2o has a very low bandwidth acknowledge protocol (one ack for 32 packets,
the ack is a small packet, 64 or at most 128 bytes, while the data is
32k). And net2o's flow control relies on the receiver to signal correct time
stamps.

So a malicious receiver can just spoof some answers and drive the sender to
create a lot of traffic. To prevent acknowledge spoofing, we require the
receiver to compute a "cookie" for every packet transmitted — this cookie is
something that proves he has received and correctly decrypted the packet, but
the cookie itself is actually never sent around. We use Keccak's hidden state
to create this cookie — reduced to a 64 bit number (this is more than
sufficient — anything an attacker can create is bandwidth). We xor all cookies
of one acknowledge lump together.

A malicious receiver who creates excessive traffic now will not receive the
packets anymore, which prevents him from creating a legit acknowledge.
