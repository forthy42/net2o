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

## Angaben gemäß §13 TMG ##

Alle hier erhobenen Daten werden in Deutschland gespeichert.

Für jeden Zugtriff werden IP-Adresse, Datum, Zugriffs-Host und -URL
und die Browser-ID gespeichert. Logs werden wöchentlich rotiert und
nach einigen Wochen gelöscht.

## Cookies ##

Cookies werden nur gesetzt, wenn man sich im Fossil-Repository
einloggt (auch anonym). Diese Cookies werden ein Jahr lang in dem
Browser-Directory des Nutzers selbst gespeichert, und können dort
jederzeit gelöscht werden.

## Trackers ##

Ich verwende keine Tracker wie Google Analytics. Sollte jemand einen
finden, ist das ein Bug, der formlos gemeldet werden kann.