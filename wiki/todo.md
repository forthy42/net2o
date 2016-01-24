# TODO #

This is a short to-do list for things I have concrete plans

## Low-level protocol ##

* change flow control window so that it minimizes buffer fillup
  instead of using the rate for this purpose - should stabilize the
  flow control in case of severe congestion.
* Add automatic and manual rearangement of multichat trees
* Split up DHT into directory DHT and subnodes
* Add sync operations to chat logs
* Add mirror&sync to DHT subnodes
* Add hashed files distribution
* Add version control for files
* Add streaming data
* Add remote terminal

## High-level API ##

* Produce a Gforth that can run inside a sandbox, like Linux
  namespace+chroot+capabilities jail.
* Get MINOS2 so far that it can be used as GUI inside the jail
* Create a framework for structured text and pre-formatted text
* Use streaming data for audio+video streaming and -chats