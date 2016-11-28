# Data Retention #

Germany, like many other countries, has a [data retention
law](https://dejure.org/gesetze/TKG/113b.html), which requires ISPs to
store data like the IP addresses or telephone numbers assigned to
their customers.

While I'm not providing actual cables, and therefore I'm not a direct
service provider, the net2o DHT does store data about net2o users, and
it might be possible that data retention could be necessary. However,
the publically "assigned" IP address is always the one of the
forwarder node (the DHT node you use to announce yourself), not the
user's home IP itself.  Similar to a carrier grade NAT, this
information alone is useless.