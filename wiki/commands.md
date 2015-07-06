+ $0 dummy ( -- )
Commands
========

net2o separates data and commands.  Data is pass through to higher
layers, commands are interpreted when they arrive.  For connection
requests, the address 0 is always mapped as connectionless code
address.

The command interpreter is a stack machine with two data types: 64
bit integers and strings.  Encoding of commands, integers and string
length follows protobuf, strings are just sequences of bytes
(interpretation can vary).  Command blocks contain a sequence of
commands; there are no conditionals and looping instructions.

Strings can contain encrypted nested commands, used during
communication setup.

List of Commands
----------------

### base commands ###

+ $0 end-cmd ( -- )
  end command buffer
+ $1 ulit ( #u -- u )
  unsigned literal
+ $2 slit ( #n -- n )
  signed literal, zig-zag encoded
+ $3 string ( #string -- $:string )
  string literal
+ $4 flit ( #dfloat -- r )
  double float literal
+ $5 endwith ( o:object -- )
  end scope
+ $6 oswap ( o:nest o:current -- o:current o:nest )
+ $7 tru ( -- f:true )
  true flag literal
+ $8 fals ( -- f:false )
  false flag literal
+ $9 words ( ustart -- )
  reflection
+ $A nestsig ( $:cmd+sig -- )
  check sig+nest

### reply commands ###

+ $10 push' ( #cmd -- )
  push command into answer packet
+ $11 push-lit ( u -- )
  push unsigned literal into answer packet
+ $12 push-slit ( n -- )
  push singed literal into answer packet
+ $13 push-$ ( $:string -- )
  push string into answer packet
+ $14 push-float ( r -- )
  push floating point number
+ $15 ok ( utag -- )
  tagged response
+ $16 ok? ( utag -- )
  request tagged response
+ $17 ko ( uerror -- )
  receive error message
+ $18 nest ( $:string -- )
  nested (self-encrypted) command
+ $19 request-done ( ureq -- )
  signal request is completed
+ $1A token ( $:token n -- )

### connection setup commands ###

+ $20 tmpnest ( $:string -- )
  nested (temporary encrypted) command
+ $21 new-data ( addr addr u -- )
  create new data mapping
+ $22 new-code ( addr addr u -- )
  crate new code mapping
+ $23 set-cookie ( utimestamp -- )
  cookie and round trip delay
+ $24 store-key ( $:string -- )
  store key
+ $25 map-request ( addrs ucode udata -- )
  request mapping
+ $26 set-tick ( uticks -- )
  adjust time
+ $27 get-tick ( -- )
  request time adjust
+ $28 receive-key ( $:key -- )
  receive a key
+ $29 receive-tmpkey ( $:key -- )
  receive emphemeral key
+ $2A key-request ( -- )
  request a key
+ $2B tmpkey-request ( -- )
  request ephemeral key
+ $2C keypair ( $:yourkey $:mykey -- )
  select a pubkey
+ $2D update-key ( -- )
  update secrets
+ $2E gen-ivs ( $:string -- )
  generate IVs
+ $2F punch ( $:string -- )
  punch NAT traversal hole
+ $30 punch-load, ( $:string -- )
  use for punch payload: nest it
+ $31 punch-done ( -- )
  punch received
+ $32 punch? ( -- )
  Request punch addresses
  Seed and gen all IVS
+ $33 >time-offset ( n -- )
  set time offset
+ $34 context ( -- )
  make context active
+ $35 gen-reply ( -- )
  generate a key request reply reply
+ $36 gen-punch-reply ( -- )
  generate a key request reply reply

### connection commands ###

+ $20 disconnect ( -- )
  close connection
+ $21 set-ip ( $:string -- )
  set address information
+ $22 get-ip ( -- )
  request address information
+ $23 set-blocksize ( n -- )
  set blocksize to 2^n
+ $24 set-blockalign ( n -- )
  set block alignment to 2^n
+ $25 close-all ( -- )
  close all files
+ $26 set-top ( utop flag -- )
  set top, flag is true when all data is sent
+ $27 slurp ( -- )
  slurp in tracked files
+ $30 file-id ( uid -- o:file )
  choose a file object

### file commands ###

+ $20 open-file ( $:string mode -- )
  open file with mode
+ $21 file-type ( n -- )
  choose file type
+ $22 close-file ( -- )
  close file
+ $23 set-size ( size -- )
  set size attribute of current file
+ $24 set-seek ( useek -- )
  set seek attribute of current file
+ $25 set-limit ( ulimit -- )
  set limit attribute of current file
+ $26 set-stat ( umtime umod -- )
  set time and mode of current file
+ $27 get-size ( -- )
  requuest file size
+ $28 get-stat ( -- )
  request stat of current file
+ $29 set-form ( w h -- )
  if file is a terminal, set size
+ $2A get-form ( -- )
  if file is a terminal, request size
+ $2B poll-request ( ulimit -- )
  poll a file to check for size changes
+ $31 ack ( -- o:acko )
  ack object

### ack commands ###

+ $20 ack-addrtime ( utime addr -- )
  packet at addr received at time
+ $21 ack-resend ( flag -- )
  set resend toggle flag
+ $22 set-rate ( urate udelta-t -- )
  set rate 
+ $23 resend-mask ( addr umask -- )
  resend mask blocks starting at addr
+ $24 track-timing ( -- )
  track timing
+ $25 rec-timing ( $:string -- )
  recorded timing
+ $26 send-timing ( -- )
  request recorded timing
+ $27 ack-b2btime ( utime addr -- )
  burst-to-burst time at packet addr
+ $28 ack-resend# ( addr $:string -- )
  resend numbers
+ $29 ack-flush ( addr -- )
  flushed to addr
+ $2A set-head ( addr -- )
  set head
+ $2B timeout ( uticks -- )
  timeout request
+ $2C set-rtdelay ( ticks -- )
  set round trip delay only

### log commands ###

+ $1A log-token ( $:token n -- )
+ $20 emit ( utf8 -- )
  emit character on server log
+ $21 type ( $:string -- )
  type string on server log
+ $22 cr ( -- )
  newline on server log
+ $23 . ( n -- )
  print number on server log
+ $24 f. ( r -- )
  print fp number on server log
+ $25 .time ( -- )
  print timer to server log
+ $26 !time ( -- )
  start timer
+ $32 log ( -- o:log )

### key storage commands ###

+ $11 privkey ( $:string -- )
  private key
+ $12 keytype ( n -- )
key type (0: anon, 1: user, 2: group)
+ $13 keynick ( $:string -- )
key nick
+ $14 keyprofile ( $:string -- )
key profile (hash of a resource)
+ $15 keymask ( x -- )
key mask
+ $16 keypsk ( $:string -- )
preshared key (unclear if that's going to stay
+ $17 +keysig ( $:string -- )
add a key signature

### dht commands ###

+ $33 dht-id ( $:string -- o:o )
set dht id for further operations on it
+ $20 dht-host+ ( $:string -- )
+ $21 dht-host- ( $:string -- )
+ $22 dht-tags+ ( $:string -- )
+ $23 dht-tags- ( $:string -- )
+ $24 dht-owner+ ( $:string -- )
+ $25 dht-owner- ( $:string -- )
+ $26 dht-host? ( -- )
+ $27 dht-tags? ( -- )
+ $28 dht-owner? ( -- )

### address commands ###

+ $10 addr-pri# ( n -- )
  priority
+ $11 addr-id ( $:id -- )
  unique host id string
+ $12 addr-anchor ( $:pubkey -- )
  anchor for routing further
+ $13 addr-ipv4 ( n -- )
  ip address
+ $14 addr-ipv6 ( $:ipv6 -- )
  ipv6 address
+ $15 addr-portv4 ( n -- )
  ipv4 port
+ $16 addr-portv6 ( n -- )
  ipv6 port
+ $17 addr-port ( n -- )
  ip port
+ $18 addr-route ( $:net2o -- )
  net2o routing part
+ $19 addr-key ( $:addr -- )
  key for connection setup
  forward message to all next nodes of that message group

### message commands ###

+ $34 msg ( -- o:msg )
  push a message object
+ $20 msg-start ( $:pksig -- )
  start message
+ $21 msg-group ( $:group -- )
  specify a chat group
  already a message there
+ $22 msg-join ( $:group -- )
  join a chat group
+ $23 msg-leave ( $:group -- )
  leave a chat group
+ $24 msg-signal ( $:pubkey -- )
  signal message to one person
+ $25 msg-re ( $:hash )
  relate to some object
+ $26 msg-text ( $:msg -- )
  specify message string
+ $27 msg-object ( $:object -- )
  specify an object, e.g. an image
+ $28 msg-action ( $:msg -- )
  specify message string
+ $29 msg-reconnect ( $:pubkey -- )
  rewire distribution tree

### vault commands ###

+ $20 dhe ( $:pubkey -- )
  start diffie hellman exchange
+ $21 vault-keys ( $:keys -- )
+ $22 vault-file ( $:content -- )
+ $23 vault-sig ( $:sig -- )
+ $24 vault-crypt ( n -- )
  set encryption mode and key wrap size
