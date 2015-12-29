# Flow Control #

The assumptions of TCP are wrong, so TCP's flow control is broken - that's
one of my rationale to create a new protocol. So what are my assumptions, and
what do I propose instead?

Let's first look at TCP: What does TCP assume?

TCP defines a window size. This equals the amount of data which is in
flight, so supposed there is no packet drop. &nbsp;The tail is the last
acknowledged packet, and the head is the last sent packet. &nbsp;TCP has a
"slow start", so it starts with one packet, and increases the number of
segments in the window depending on how many segments are acknowledged. &nbsp;This
gives an exponential growth phase, until there's one packet drop. &nbsp;The
assumption here is that when the sender sends too fast, packets are dropped.
&nbsp;If a packet is dropped, TCP will half the window size, and slowly grow it
by one segment per round-trip - until again a packet is lost.

This means the number of packets in flight oscillates by a factor of two.
&nbsp;So what about optimal buffer size? &nbsp;The optimal buffer size for a
TCP connection is capable to keep packets for 0.5 RTT - because that's the same
amount of packets that fit onto the wire (they take 0.5 RTT from source to
destination). &nbsp;This is something so far nobody has implemented, because it
requires that the buffering router measures the RTT for each TCP connection.
&nbsp;This measurement is possible in theory, it can be measured from the first
syn to the first ack. &nbsp;In practice, there's usually no scientific method
applied to choose the right buffer size; if you are lucky, there had been a few
experiments, selecting some buffer size on an educated guess, and the router
has a global FIFO.

The worst problem with this algorithm is on networks with poor quality, such
as wireless networks, where packet drops are relatively frequent, and have
nothing to do with the sender rate. &nbsp;The next problem is that a filled up
buffer in the router delays all other connections, including low-latency
low-bandwidth real-time protocols.

So that's not the way to go. &nbsp;We must reevaluate the assumptions and
find a solution.

## The assumptions ##

+ Network devices do have buffers, most of them "too large buffers" for TCP
  to work reasonable
+ The buffers are your friend, not your enemy, they avoid retransmissions
+ Buffers should usually stay almost empty
+ Packet drops are not related to flow control problems
+ Intermediate network hops can help fairness, by providing "fair queuing"

## The solution ##

Since network hops which may help with flow control are not likely to be
available soon (and probably also not the right solution), the solution has to
do end-to-end flow control (like TCP/IP), working with single (unfair) FIFO
queuing. The flow control should be fair (_n_ competing connections should
get _n_ of the data rate each), and it should not completely yield to
TCP, even in a buffer-bloat configuration.

The approach is the following: The sender sends short bursts of
packets (default: 8 packets per burst), and the receiver measures the
timing when these packets arrive - from earliest to latest, and
calculates the achievable data rate. The receiver sends this data rate
back to the sender, which adjusts its sending rate (to make sure the
rate is not faked, the receiver must prove it has received at least
most packets). Data rate calculation accumulates rates over several
bursts (default: 4 bursts per block), and sends only a final result,
i.e. one acknowledge per 32 packets. This is the P part of a PID
controller; the receiver constantly provides measurements of
acheivable rates, and the sender adjusts this rate on every ack
received.

The sender tracks two things: Slack and slack-growth (I and D of the PID
controller). Slack, i.e. accumulated buffer space, provides an exponential
slowdown, where a factor of two equates to either half the difference of
maximum and minimum observed slack or 20ms (whatever is larger).

Slack-growth is observed by the timing of the last burst compared with the
first burst in the four burst sequence. This tells us how excessive our data
rate is. To compensate, we need to multiply that time with the bursts in
flight, and add that as extra delay after the next burst we send. This allows
the buffer to recover.

The whole algorithm, sender and receiver side, fits into about 100 lines at
the moment, which includes the rate control and burst generation on the sender
side, but does not include all the debugging and statistics code to observe
what happens.

To get fast long-distance connections up to speed quickly, the first rate
adjustment will also up the packets in flight. Later, each ack allows for
further packets in flight (default: a maximum of two bursts, i.e. 64 packets)
before the next ack is expected. To achieve this, the sender measures round
trip delay.

This helps to detect broken connections - if the receiver goes offline or
has been suspended temporarily, the sender stops. It can't call back the
packets in flight, for sure, these will get lost, and might temporarily fill up
buffers.

The algorithm is measured as fair and sufficiently stable for several
parallel connections to the same sender, and it works together with parallel
TCP and LEDBAT traffic.

## Fair Queuing ##

Instead of using a single FIFO buffer policy, routers (or, in net2o
terminology, switches) can help fairness: Under congestion, each connection is
given its own FIFO, and all filled FIFOs of the same QoS level are served in a
round-robin fashion. &nbsp;This allows the receiver to accurately determine the
actual achievable bandwidth, and does not trigger the more heuristical
delay-optimizing strategies. &nbsp;Also, this buffer policy allows to have
different flow control algorithms on different protocols, and still have
fairness.