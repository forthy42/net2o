# TODO #

This is a short to-do list for things I have concrete plans

## Low-level protocol ##

* change flow control window so that it minimizes buffer fillup
  instead of using the rate for this purpose - should stabilize the
  flow control in case of severe congestion.
* Add automatic rearrangement of multichat trees (manual is there)
* Split up DHT into directory DHT and subnodes
* Add mirror&sync to DHT subnodes
* Add streaming data
* Add remote terminal
* Add onion routing
* CGNAT experiments to get CGNAT -> NAT to work

## Security ##

* Add a get-ip permission to control who can use your node to bounce
  (half done, permission is there, but not checked)
* Add a NAT traversal permission to not reveal IP addresses depending on
  peer
* Add a kill switch passphrase which erases all keys when entered
* Add a passphrase for security key export

## Binary packaging ##

* RPM builds and repository needed (through OpenSuSE build service)
* Mac OS X distributions

## High-level API ##

* Produce a Gforth that can run inside a sandbox, e.g. Linux
  namespace+chroot+capabilities jail.
* Get MINOS2 so far that it can be used as GUI inside the jail
* Create a framework for structured text and pre-formatted text
* Use streaming data for audio+video streaming and -chats

## Where can you help? ##

* Performance of crypto implementations:
  + ed25519-donna has SSE2 support on Intel, but could be faster on ARM
    with Neon instructions
  + Threefish also could benefit from a Neon implementation
  + Keccak already has one and may provide a starting point
* Testing - beat it, test it, report bugs.  I'll take every bug report
  serious.  There's a [ticket system](/net2o/reportlist)