# Commands #

Version 0.5.6-20180208.

net2o separates data and commands.  Data is pass through to higher
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
+ $5 end-with ( o:object -- )
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
+ $B secstring ( #string -- $:string )
  secret string literal
+ $C nop ( -- )
  do nothing
+ $D 4cc ( #3letter -- )
  At the beginning of a file, this can be used as FourCC code
+ $E padding ( #len -- )
  add padding to align fields
+ $F version ( $:version -- )
  version check

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
+ $19 token ( $:token n -- )
  generic inspection token
+ $1A error-id ( $:errorid -- )
  error-id string
+ $1B version? ( $:version -- )
  version cross-check

### connection generic commands ###

+ $20 request-done ( ureq -- )
  signal request is completed
+ $21 set-cookie ( utimestamp -- )
  cookies and round trip delays
+ $22 punch-load, ( $:string -- )
  use for punch payload: nest it
+ $23 punch ( $:string -- )
  punch NAT traversal hole
+ $24 punch-done ( -- )
  punch received

### connection setup commands ###

+ $30 tmpnest ( $:string -- )
  nested (temporary encrypted) command
+ $31 encnest ( $:string -- )
  nested (completely encrypted) command
+ $32 close-tmpnest ( -- )
  cose a opened tmpnest, and add the necessary stuff
+ $33 close-encnest ( -- )
  cose a opened tmpnest, and add the necessary stuff
+ $34 new-data ( addr addr u -- )
  create new data mapping
+ $35 new-code ( addr addr u -- )
  crate new code mapping
+ $36 store-key ( $:string -- )
  store key
+ $37 map-request ( addrs ucode udata -- )
  request mapping
+ $38 set-tick ( uticks -- )
  adjust time
+ $39 get-tick ( -- )
  request time adjust
+ $3A receive-tmpkey ( $:key -- )
  receive emphemeral key
+ $3B tmpkey-request ( -- )
  request ephemeral key
+ $3C keypair ( $:yourkey $:mykey -- )
  select a pubkey
+ $3D update-key ( -- )
  update secrets
+ $3E gen-ivs ( $:string -- )
  generate IVs
+ $3F set-cmd0key ( $:string -- )
  set key for reply
+ $40 punch? ( -- )
  Request punch addresses
+ $41 >time-offset ( n -- )
  set time offset
+ $42 context ( -- )
  make context active
+ $43 gen-reply ( -- )
  generate a key request reply
+ $44 gen-punch-reply ( -- )
+ $45 invite ( $:nick+sig $:pk -- )
  invite someone
+ $46 request-invitation ( -- )
  ask for an invitation as second stage of invitation handshake
+ $47 sign-invite ( $:signature -- )
  send you a signature
+ $48 request-qr-invitation ( -- )
  ask for an invitation as second stage of invitation handshake
+ $49 tmp-secret, ( -- )
+ $4A qr-challenge ( $:challenge $:respose -- )

### connection commands ###

+ $25 disconnect ( -- )
  close connection
+ $26 set-ip ( $:string -- )
  set address information
+ $27 get-ip ( -- )
  request address information
+ $28 set-blocksize ( n -- )
  set blocksize to 2^n
+ $29 set-blockalign ( n -- )
  set block alignment to 2^n
+ $2A close-all ( -- )
  close all files
+ $2B set-top ( utop flag -- )
  set top, flag is true when all data is sent
+ $2C slurp ( -- )
  slurp in tracked files
+ $2D ack-reset ( -- )
  reset ack state

### file commands ###

+ $30 file-id ( uid -- o:file )
  choose a file object
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
  request file size
+ $28 get-stat ( -- )
  request stat of current file
+ $29 set-form ( w h -- )
  if file is a terminal, set size
+ $2A get-form ( -- )
  if file is a terminal, request size
+ $2B poll-request ( ulimit -- )
  poll a file to check for size changes

### ack commands ###

+ $31 ack ( -- o:acko )
  ack object
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
+ $2D seq# ( n -- )
  set the ack number and check for smaller

### log commands ###

+ $19 log-token ( $:token n -- )
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
  free all parts of the subkey

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
  key access right mask
+ $16 keygroups ( $:groups -- )
  access groups
+ $17 +keysig ( $:string -- )
  add a key signature
+ $18 keyimport ( n -- )
+ $19 rskkey ( $:string --- )
  revoke key, temporarily stored
+ $1A keypet ( $:string -- )
+ $1B walletkey ( $:seed -- )
  read a nested key into sample-key

