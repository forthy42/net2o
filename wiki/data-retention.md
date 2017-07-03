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
nach einigen Wochen gelöscht. Die IP-Adresse wird auf die ersten 3
(IPv4, /24) bzw. die ersten 5 Bytes (IPv6, /40) verkürzt und dann
gespeichert. Damit ist eine individuelle Zuordnung nicht mehr
verhältnismäßig möglich. Auch 6to4-Adressen werden zumindest um das
letzte IPv4-Byte reduziert.

Nach einem Monat werden die Logs regulär gelöscht.

## Cookies ##

Cookies werden nur gesetzt, wenn man sich im Fossil-Repository
einloggt (auch anonym). Diese Cookies werden ein Jahr lang in dem
Browser-Directory des Nutzers selbst gespeichert, und können dort
jederzeit gelöscht werden.

## Trackers ##

Ich verwende keine Tracker wie Google Analytics. Sollte jemand doch
einen finden, ist das ein Bug, der formlos gemeldet werden kann.
