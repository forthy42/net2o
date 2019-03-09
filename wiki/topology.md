# Topology

net2o assumes a hierarchical topology, i.e. a tree topology.  There may
be multiple paths reaching the same destination, so this doesn't exclude that
parts of the tree are actually mesh networks.  This reflects reality in
the current Internet, and the expensive layer 1 infrastructure isn't likely to
be replaced soon.

Most connections send a larger number of packets, so routing each packet is
wasteful, drives up costs and lowers speed.  Therefore the decision is to
switch packets, and route connections — at the source.  I call this
combination

## Path Switching

The path is a 128 bit field in the packet, the switching algorithm is as
follows:

* Take the first _n_ bits of the path field and use those to select
  the destination
* Shift the path field by _n_ bits to the left
* Insert the bit-reversed _n_ bit source into the rear end of the
  path field to mark the way back

The receiver bit-reverses the entire path, and thereby gets a way back to
the sender.  This makes spoofing impossible, and eases
[handover](handover.wiki), as only the device that
switches networks needs to calculate a new path; the receiver will accept any
properly authenticated packet and use the new path to send data back.

## Packet Format

Packets have a power-of-two size from 64 bytes to 2MB data. Assuming network
speed to grow by a factor 1000 in 20 years, going from a million 1k packets on
a 10Gb Ethernet now to a billion 1M packets in 40 years means this has enough
headroom for the next 40 years.

The packet contains these elements:

1. 2 bytes flags: 2 bits QoS (00 highest to 11 lowest), 2 bits
   protocol version (default is now 01), 4 bits packet size
   (64\*2^_n_), 2 bit switch flags (broadcast, multicast), 3 bits
   reserved, 3 bits for flow control (resend-toggle, burst-toggle,
   ack-toggle).
2. 16 bytes path (rough Internet 1.0 equivalent: “address”)
3. 8 bytes address: this is the address in the destination buffer where the
   packet will be stored (roughly equivalent to port+sequence number)
4. 64\*2^_size_ bytes data
5. 16 bytes authentication data (keyed cryptographic checksum)

The “abstraction” at packet level is shared memory; the model is read
locally and write remotely (you can't read remotely, you can ask for the other
side to send you packets).  Of course, the addresses are virtual, so you
can't write into arbitrary memory — only into the buffers provided by the other
side.

## Why Source Routing?

There are three possible schemes:

1. switched circuit (POTS, virtual: ATM, MPLS)
2. unique identifier (IP)
3. source routing

I want to separate computers and network devices; source routing allows to
use simple, fast, stateless equipment for switching (or at least equipment with
a small amount of state: A small mapping table is helpful to give a bit
anonymity, by regularly changing the mapping).
