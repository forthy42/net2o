# Handover #

Mobile communication is one of the things that didn't come to the mind of the
people who invented the Internet 30 years ago. There was over-the-air
communication even before, especially the ALOHAnet should be mentioned, but
the computers were too large to be carried around. And all stations were
within reach of each others (by using pretty strong signals — the Hawaii
islands are not that small). Handover means that an end node regularly changes
the station it's connected to. With a switching system like net2o, this means,
the address changes. The communication however should continue even when such
an address change happens frequently. And it should work even when both nodes
hop from station to station. No complicated renegotiation should happen, and
no routing server should be queried if such a thing happens on an open
connection. The idea to achieve this is fairly trivial:

* net2o addresses are unique connections
* Any reply to an open connection is sent to the last received return address
* When an end node changes the station, it will ping all open connections and
  thus inform the peers about that change
* Changing stations need a time overlap, during which the end node is
  reachable through both the old and the new address.

This overlap time is necessary if two connected nodes change station at the
same time. As the communication itself is protected by encryption from
intruders, this is save even without presenting some ticket for the
handover. The overlap time can be also achieved by temporarily forwarding
packets from the old destination — this is the preferred way to migrate
virtual machines to new hosts — the old host temporarily forwards all received
packets to the new host, and after a short time can be used for something
else. From a firewall point of view, this approach looks scary. When a system
opens a connection (which consists of some address ranges), this address range
is open to anybody. And worse yet, since the address is not encrypted,
everybody can know which are legitimate addresses, by observing the traffic
for a while. The blocking of intruders happens solely through encryption, and
that encryption is unknown to the firewall (which is the whole point of
encryption, after all).
