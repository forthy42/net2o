# Commands #

Version 0.9.9-20250205.

net2o separates data and commands.  Data is passed through to higher
layers, commands are interpreted when they arrive.  For connection
requests, a special bit is set, and the address then isn't used as
address, but as IV for the opportunistic encoding.

The command interpreter is a stack machine with two data types: 64
bit integers and strings (floats are also suppored, but used
infrequently).  Encoding of commands, integers and string length
follows protobuf conceptually (but MSB first, not LSB first as with
protobuf, to simplify scanning), strings are just sequences of
bytes (interpretation can vary).  Command blocks contain a sequence
of commands; there are no conditionals and looping instructions.

Strings can contain encrypted nested commands, used during
communication setup.

## List of Commands ##

Commands are context-sensitive in an OOP method hierarchy sense.

### base commands ###

* $0 end-cmd ( -- )
  end command buffer
* $1 lit ( #u -- u )
  literal
* $2 -lit ( #n -- n )
  negative literal, inverted encoded
* $3 string ( #string -- $:string )
  string literal
* $4 flit ( #dfloat -- r )
  double float literal
* $5 end-with ( o:object -- )
  end scope
* $6 oswap ( o:nest o:current -- o:current o:nest )
* $7 tru ( -- f:true )
  true flag literal
* $8 fals ( -- f:false )
  false flag literal
* $9 words ( ustart -- )
  reflection
* $A nestsig ( $:cmd+sig -- )
  check sig+nest
* $B secstring ( #string -- $:string )
  secret string literal
* $C nop ( -- )
  do nothing
* $D 4cc ( #3letter -- )
  At the beginning of a file, this can be used as FourCC code
* $E padding ( #len -- )
  add padding to align fields
* $F version ( $:version -- )
  version check

### reply commands ###

* $10 push' ( #cmd -- )
  push command into answer packet
* $11 push-lit ( u -- )
  push unsigned literal into answer packet
* $13 push-$ ( $:string -- )
  push string into answer packet
* $14 push-float ( r -- )
  push floating point number
* $15 ok ( utag -- )
  tagged response
* $16 ok? ( utag -- )
  request tagged response
* $17 ko ( uerror -- )
  receive error message
* $18 nest ( $:string -- )
  nested (self-encrypted) command
* $19 token ( $:token n -- )
  generic inspection token
* $1A error-id ( $:errorid -- )
  error-id string
* $1B version? ( $:version -- )
  version cross-check

### connection generic commands ###

* $20 request-done ( ureq -- )
  signal request is completed
* $21 set-cookie ( utimestamp -- )
  cookies and round trip delays
* $22 punch-load, ( $:string -- )
  use for punch payload: nest it
* $23 punch ( $:string -- )
  punch NAT traversal hole
* $24 punch-done ( -- )
  punch received

### connection setup commands ###

* $30 tmpnest ( $:string -- )
  nested (temporary encrypted) command
* $31 encnest ( $:string -- )
  nested (completely encrypted) command
* $32 close-tmpnest ( -- )
  cose a opened tmpnest, and add the necessary stuff
* $33 close-encnest ( -- )
  cose a opened encnest, and add the necessary stuff
* $34 new-data ( addr addr u -- )
  create new data mapping
* $35 new-code ( addr addr u -- )
  crate new code mapping
* $36 store-key ( $:string -- )
  store key
* $37 map-request ( addrs ucode udata -- )
  request mapping
* $38 set-tick ( uticks -- )
  adjust time
* $39 get-tick ( -- )
  request time adjust
* $3A receive-tmpkey ( $:key -- )
  receive emphemeral key
* $3B tmpkey-request ( -- )
  request ephemeral key
* $3C keypair ( $:yourkey $:mykey -- )
  select a pubkey
* $3D update-key ( -- )
  update secrets
* $3E gen-ivs ( $:string -- )
  generate IVs
* $3F addr-key! ( $:string -- )
  set key for cmd0-reply
* $40 punch? ( -- )
  Request punch addresses
* $41 >time-offset ( n -- )
  set time offset
* $42 context ( -- )
  make context active
* $43 gen-reply ( -- )
  generate a key request reply
* $44 gen-punch-reply ( -- )
* $45 invite ( $:nick+sig $:pk -- )
  invite someone
* $46 request-invitation ( -- )
  ask for an invitation as second stage of invitation handshake
* $47 sign-invite ( $:signature -- )
  send you a signature
* $48 request-qr-invitation ( -- )
  ask for an invitation as second stage of invitation handshake
* $49 tmp-secret, ( -- )
* $4A qr-challenge ( $:challenge $:respose -- )
* $4B invite-result ( flag -- )
* $4C set-host ( $:host -- )
* $4D get-host ( -- )

### connection commands ###

* $25 disconnect ( -- )
  close connection
* $26 set-ip ( $:string -- )
  set address information
* $27 get-ip ( -- )
  request address information
* $28 set-blocksize ( n -- )
  set blocksize to 2^n
* $29 set-blockalign ( n -- )
  set block alignment to 2^n
* $2A close-all ( -- )
  close all files
* $2B set-top ( utop flag -- )
  set top, flag is true when all data is sent
* $2C slurp ( -- )
  slurp in tracked files
* $2D ack-reset ( -- )
  reset ack state
* $2E slurped ( $slurped -- )
  respond to slurped stuff

### file commands ###

* $30 file-id ( uid -- o:file )
  choose a file object
* $20 open-file ( $:string mode -- )
  open file with mode
* $21 file-type ( n -- )
  choose file type
* $22 close-file ( -- )
  close file
* $23 set-size ( size -- )
  set size attribute of current file
* $24 set-seek ( useek -- )
  set seek attribute of current file
* $25 set-limit ( ulimit -- )
  set limit attribute of current file
* $26 set-stat ( umtime umod -- )
  set time and mode of current file
* $27 get-size ( -- )
  request file size
* $28 get-stat ( -- )
  request stat of current file
* $29 set-form ( w h -- )
  if file is a terminal, set size
* $2A get-form ( -- )
  if file is a terminal, request size
* $2B poll-request ( ulimit -- )
  poll a file to check for size changes

### ack commands ###

* $31 ack ( -- o:acko )
  ack object
* $20 ack-addrtime ( utime addr -- )
  packet at addr received at time
* $21 ack-resend ( flag -- )
  set resend toggle flag
* $22 set-rate ( urate udelta-t -- )
  set rate 
* $23 resend-mask ( addr umask -- )
  resend mask blocks starting at addr
* $24 track-timing ( -- )
  track timing
* $25 rec-timing ( $:string -- )
  recorded timing
* $26 send-timing ( -- )
  request recorded timing
* $27 ack-b2btime ( utime addr -- )
  burst-to-burst time at packet addr
* $28 ack-resend# ( addr $:string -- )
  resend numbers
* $29 ack-flush ( addr -- )
  flushed to addr
* $2C set-rtdelay ( ticks -- )
  set round trip delay only
* $2D seq# ( n -- )
  set the ack number and check for smaller

### log commands ###

* $19 log-token ( $:token n -- )
* $20 emit ( utf8 -- )
  emit character on server log
* $21 type ( $:string -- )
  type string on server log
* $22 cr ( -- )
  newline on server log
* $23 . ( n -- )
  print number on server log
* $24 f. ( r -- )
  print fp number on server log
* $25 .time ( -- )
  print timer to server log
* $26 !time ( -- )
  start timer
* $32 log ( -- o:log )
  free all parts of the subkey

### key storage commands ###
* $2 slit ( #lit -- )
  deprecated slit version
* $F kversion ( $:string -- )
  key version
* $11 privkey ( $:string -- )
  private key
* $12 keytype ( n -- )
  key type (0: anon, 1: user, 2: group)
* $13 keynick ( $:string -- )
  key nick
* $14 keyprofile ( $:string -- )
  key profile (hash of a resource)
* $15 keymask ( x -- )
  key access right mask
* $16 keygroups ( $:groups -- )
  access groups
* $17 +keysig ( $:string -- )
  add a key signature
* $18 keyimport ( n -- )
* $19 rskkey ( $:string --- )
  revoke key, temporarily stored
* $1A keypet ( $:string -- )
* $1B walletkey ( $:seed -- )
* $1C avatar ( $:string -- )
  key avatar profile (hash of a resource)
* $1D key-string1 ( $:string -- )
* $1E key-string2 ( $:string -- )
* $1F key-string3 ( $:string -- )
* $20 key-string4 ( $:string -- )
* $21 key-dhtsecs ( $:string -- )
  persistent secrets for DHT roots, can not be changed regularly
* $22 key-sec2 ( $:string -- )
* $23 key-num1 ( 64n -- )
* $24 key-num2 ( 64n -- )
* $25 key-num3 ( 64n -- )
* $26 key-num4 ( 64n -- )
  read a nested key into sample-key

### address commands ###

* $11 addr-pri# ( n -- )
  priority
* $12 addr-id ( $:id -- )
  unique host id string
* $13 addr-anchor ( $:pubkey -- )
  anchor for routing further
* $14 addr-ipv4 ( n -- )
  ip address
* $15 addr-ipv6 ( $:ipv6 -- )
  ipv6 address
* $16 addr-portv4 ( n -- )
  ipv4 port
* $17 addr-portv6 ( n -- )
  ipv6 port
* $18 addr-port ( n -- )
  ip port, both protocols
* $19 addr-route ( $:net2o -- )
  net2o routing part
* $1A addr-key ( $:addr -- )
  key for connection setup
* $1B addr-revoke ( $:revoke -- )
  revocation info
* $1C addr-ekey ( $:ekey to -- )
  ephemeral key

### dht commands ###

* $33 dht-id ( $:string -- o:o )
  set DHT id for further operations on it
* $20 dht-host+ ( $:string -- )
  add host to DHT
* $21 dht-host- ( $:string -- )
  delete host from DHT
* $22 dht-host? ( -- )
  query DHT host
* $23 dht-tags+ ( $:string -- )
  add tags to DHT
* $24 dht-tags- ( $:string -- )
  delete tags from DHT
* $25 dht-tags? ( -- )
  query DHT tags
* $26 dht-owner+ ( $:string -- )
  add owner to DHT
* $27 dht-owner- ( $:string -- )
  delete owner from DHT
* $28 dht-owner? ( -- )
  query DHT owner
* $29 dht-have+ ( $:string -- )
  add have to DHT
* $2A dht-have- ( $:string -- )
  delete have from DHT
* $2B dht-have? ( -- )
  query DHT have

### vault commands ###

* $20 dhe ( $:pubkey -- )
  start diffie hellman exchange
* $21 vault-keys ( $:keys -- )
  vault keys can be opened with the dhe secret; each key is IV+session key+checksum
* $22 vault-file ( $:content -- )
  this is the actual content of the vault
  if blockwise, there may be multiple parts
* $23 vault-sig ( $:sig -- )
  the signature of the vault, using the keyed hash over the file
* $24 vault-crypt ( n -- )
  set encryption mode and key wrap size
* $25 vault-auth ( $:auth -- )
  block authentication, 64 byte block
* $26 vault-dhe-keys ( $:dhe+keys -- )

### message commands ###

* $20 msg-start ( $:pksig -- )
  start message
* $21 msg-tag ( $:tag -- )
  tagging (can be anywhere)
* $22 msg-id ( $:id -- )
  a hash id
* $23 msg-chain ( $:dates,sighash -- )
  chained to message[s]
* $24 msg-signal ( $:pubkey -- )
  signal message to one person
* $25 msg-re ( $:hash )
  relate to some object
* $26 msg-text ( $:msg -- )
  specify message string
* $27 msg-object ( $:object type -- )
  specify an object, e.g. an image
* $28 msg-action ( $:msg -- )
  specify action string
* $29 msg-payment ( $:contract -- )
  payment transaction
* $2A msg-otrify ( $:date+sig $:newdate+sig -- )
  turn a past message into OTR
* $2B msg-coord ( $:gps -- )
  GPS coordinates
* $2C msg-url ( $:url -- )
  specify message URL
* $2D msg-like ( xchar -- )
  add a like
* $2E msg-lock ( $:key -- )
  lock down communciation
* $2F msg-unlock ( -- )
  unlock communication
* $30 msg-perms ( $:pk perm -- )
  permissions
* $31 msg-vote ( xchar -- )
  add a vote tag; votes are set by likes
* $32 msg-text+format ( $text format -- )
* $60 msg-silent-start ( $:pksig -- )
  silent message tag
* $61 msg-hashs ( $:hashs -- )
  ihave part 1 within signed message
* $62 msg-hash-id ( $:id -- )
  ihave part 2 within signed message
* $63 msg-otrify2 ( $:date+sig $:newdate+sig -- )
  turn a past message into OTR, silent version
* $64 msg-updates ( $:fileinfo $:hash -- )
  Files got an update.
  The fileinfo string contains fileno:len tuples in command encoding.
  Each additional context is hashed to a 64 byte hash, and all the hashs
  are hashed together sequentially in the same order as the fileinfo
  describes.

### group description commands ###

* $20 group-name ( $:name -- )
  group symbolic name
* $21 group-id ( $:group -- )
  group id, is a pubkey
* $22 group-member ( $:memberkey -- )
  add member key
* $23 group-admin ( $:adminkey -- )
  set admin key
* $24 group-perms ( 64u -- )
  permission/modes bitmask

### messaging commands ###

* $34 message ( -- o:msg )
  push a message object
* $21 msg-group ( $:group -- )
  set group
* $22 msg-join ( $:group -- )
  join a chat group
* $23 msg-leave ( $:group -- )
  leave a chat group
* $24 msg-reconnect ( $:pubkey+addr -- )
  rewire distribution tree
* $25 msg-last? ( start end n -- )
  query messages time start:end, n subqueries
* $26 msg-last ( $:[tick0,msgs,..tickn] n -- )
  query result
* $27 msg-want ( $:[hash0,...,hashn] -- )
  request objects
* $28 msg-ihave ( $:[hash0,...,hashn] $:[id] -- )
  show what objects you have
* $A msg-nestsig ( $:cmd+sig -- )
  check sig+nest
* $5 msg-end-with ( -- )
  push out avalanche

### DVCS patch commands ###

DVCS metadata is stored in messages, containing message text, refs
and patchset objects. Patchset objects are constructed in a way
that makes identical transactions have the same hash.

* $20 dvcs-read ( $:hash -- )
  read in an object
* $21 dvcs-rm ( $:hash+name -- )
  delete file
* $22 dvcs-rmdir ( $:name -- )
  delete directory
* $23 dvcs-patch ( $:diff len -- )
  apply patch, len is the size of the result
* $24 dvcs-write ( $:perm+name size -- )
  write out file
* $25 dvcs-unzip ( $:diffgz size algo -- $:diff )
  unzip an object
* $26 dvcs-ref ( $:hash+perm+name -- )
  external hash reference

### payment commands ###

* $20 pay-source ( $:source -- )
  source, pk[+hash] for lookup
* $21 pay-sink ( n $:sig -- )
  sink, signature
* $22 pay-asset ( asset -- )
  select global asset type
* $23 pay-obligation ( $:enc-asset -- )
  select per-contract obligation
* $24 pay-amount ( 64amount -- )
  add/subtract amount to current asset
* $25 pay-damount ( 128amount -- )
  add/subtract 128 bit amount
* $26 pay-comment ( $:enc-comment -- )
  comment, encrypted for selected key
* $27 pay-balance ( u -- )
  select&balance asset
* $28 pay-#source ( u -- )
  select source

### Contracts ###

Contracts are state changes to wallets.  A serialized wallet is a contract
that contains all the changes from an empty wallet to fill it; it is not
checked for balance.

A dumb contract is checked for balance.  It consists of several selectors
(source/account, asset), transactions (amounts added or subtracted from an
asset), comments (encoded for the receiver, with a ephermeral pubkey as
start and a HMAC as end). Comments are fixed 64 bytes, either plain text or
hashes to files.  Transactions have to balance, which is facilitated with
the balance command, which balances the selected asset.

The signature of a contract signs the wallet's state (serialized in
normalized form) after the contract has been executed.  The current
contract's hash is part of the serialization.
