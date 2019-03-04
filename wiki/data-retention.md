# Data Retention #

## General Data Protection Regulation ##

The GPDR wasn't created with decentralized or P2P services in mind, because
those don't matter much in the current Internet.  However, the principles
certainly apply, and a P2P system gives you way more control over your data
than any centralized system.  So this section explains what happens to your
data when you use net2o.

### DHT servers ###

First, in order to be accessible, you announce yourself to a DHT server.  This
announcement is done either explicit or implicit in other functions that
require an announcemt.

An announcement does the following things:

1. It sends a very brief information about your net2o ID to the DHT;
   essentially this is your nick, your pubkey, and a signature of your nick.
   An avatar image is optional.  The index to get to your nick is your pubkey.
2. It establishs a lightweight UDP connection to this DHT node that allows to
   route addresses through the DHT node to you.  The DHT node creates a path
   from itself to you and keeps that as long as the connection is up.
3. It announces the path from the DHT to you.

DHT nodes are redundant and distribute these information as appropriate.  As
long as you are connected using said lightweight UDP connection, the path is
retained.  The static information about you is retained for longer.  DHT nodes
may purge those information infrequently; usually during a restart after an
update.  The entire design supposes that a DHT can forget everything and will
be repopulated by the users.  Therefore, DHTs don't store anything
permanently.

### Chat logs ###

Every chat partner keeps a chat log.  Chat logs remember all the chat messages
except those who are set to OTR (“off the record”).  OTR logs are kept in main
memory, but not stored permanently.  Chat logs of group chats can be synced
with any group member.  OTR logs can't be synced; they are only seen by people
who are active at the time of their sending.

In near future, you can `/otrify` your own messages; if honored by your
peers, they will disappear.  Note that only active peers in a group will be
able to see an `/otrify` request.  This honors your right to be
forgotten.

### DVCS projects ###

All your DVCS projects create an immutable chain of commits which are signed
by you and the other contributors.  History revision is only possible when all
signers agree to it, and everybody who holds a copy accepts the recall.

### $quid payments ###

$quid payments are stored in a hypercubemesh style BlockChain.  All signers
committed to never create an alternative revision of this chain.  This is on
purpose.  $quid payments happen under pseudonymous IDs which are not tied to
your connection pubkey; but people can record that association and make it
public.  You can move all your coins to other pubkeys, which is
indistinguishable from spending them.
