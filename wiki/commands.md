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
+ $21 new-data ( addr addr u -- )
+ $22 new-code ( addr addr u -- )
+ $23 set-cookie ( utimestamp -- )
+ $24 store-key ( $:string -- )
+ $25 map-request ( addrs ucode udata -- )
+ $26 set-tick ( uticks -- )
+ $27 get-tick ( -- )
+ $28 receive-key ( $:key -- )
+ $29 receive-tmpkey ( $:key -- )
+ $2A key-request ( -- )
+ $2B tmpkey-request ( -- )
+ $2C keypair ( $:yourkey $:mykey -- )
+ $2D update-key ( -- )
+ $2E gen-ivs ( $:string -- )
+ $2F punch ( $:string -- )
+ $30 punch-load, ( $:string -- )
+ $31 punch-done ( -- )
+ $32 punch? ( -- )
+ $33 >time-offset ( n -- )
+ $34 context ( -- )
+ $35 gen-reply ( -- )
+ $36 gen-punch-reply ( -- )

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
+ $24 dht-host? ( -- )
+ $25 dht-tags? ( -- )
  forward message to all next nodes of that message group

### message commands ###

+ $34 msg ( -- o:msg )
+ $20 msg-start ( $:pksig -- )
+ $21 msg-group ( $:group -- )
+ $22 msg-join ( $:group -- )
+ $23 msg-leave ( $:group -- )
+ $24 msg-signal ( $:pubkey -- )
+ $25 msg-re ( $:hash )
+ $26 msg-text ( $:msg -- )
+ $27 msg-object ( $:object -- )

### vault commands ###

+ $20 dhe ( $:pubkey -- )
  start diffie hellman exchange
+ $21 vault-keys ( $:keys -- )
+ $22 vault-file ( $:content -- )
+ $23 vault-sig ( $:sig -- )
+ $24 vault-crypt ( n -- )
  set encryption mode and key wrap size
