# Data Retention #

Germany, like many other countries, has a [data retention
law](https://dejure.org/gesetze/TKG/113b.html), which requires ISPs to
store data like the IP addresses or telephone numbers assigned to
their customers.

While I'm not providing actual interconnection infrastructure, and
therefore I'm not a direct service provider, the net2o DHT does store
data about net2o users, and it might be possible that data retention
could be necessary. However, the publically "assigned" IP address is
always the one of the forwarder node (the DHT node you use to announce
yourself), not the user's home IP itself.  Similar to a carrier grade
NAT, this information alone is useless.

## Visitor informations stored on this website ##

I store IP, accessed host, date, request, and browser ID for each
access; this is log-rotated weekly and eventually deleted.  Logs are
only analyzed occasionally, and only kept in order to analyze attacks.

## Cookies ##

Cookies are only set when you log in; the cookie is kept for one year.
If you never log in, no cookie is set.

## Trackers ##

I don't use trackers for this site.