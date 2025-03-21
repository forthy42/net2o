\ debugging aids

false [IF]
    : debug: ( -- )  Create immediate false ,
      DOES>
	@ IF  ['] noop assert-canary
	ELSE  postpone (  THEN ;
    : )else(  ]] ) ( [[ ; immediate \ )
[THEN]

:is printdebugdata .time defers printdebugdata !time ;

: nodebug: ['] ( Alias immediate ;

require xtype.fs
require base64.fs
require base85.fs

: .nnb ( addr n -- )  85type ;
: .64b ( addr -- ) 64 .nnb ;

synonym \U \G \ comment for help
synonym \O \G \ comment for help

\ debugging switches

debug: timing(
debug: bursts(
debug: resend(
debug: track(
debug: cmd( \ disassemble command packets
debug: cmd0( \ disassemble command to zero (stateless) packets
debug: msg( \ messaging
debug: timeout( \ timeouts
debug: wait( \ waiting for things
debug: ack( \ acknowledge
debug: acks( \ cummulative acknowledges
debug: crypt( \ cryptography
debug: noens( \ disable extra nanosecond delay
debug: key( \ key stuff
debug: vkey( \ vault key stuff
debug: vault( \ vault stuff
debug: genkey( \ See generated keys - never let this go to a log file!
debug: mykey( \ debug mykey things
debug: cookie( \ debug cookies
debug: tag( \ debug tagging
debug: flush( \ show saving keys
debug: address( \ debug addresses
debug: trace( \ trace command
debug: header( \ print header
debug: sender( \ extra sender task
debug: dht( \ debugging for dht functions
debug: file( \ file read/write debugging
debug: file1( \ simple file read/write debugging
debug: nat( \ NAT traversal stuff
debug: netlink( \ Netlink changes
debug: route( \ do routing
debug: ipv6( \ use ipv6
debug: ipv4( \ use ipv4
debug: ipv64( \ prefer ipv4 over 6
debug: xlat464( \ use xlat 464 prefix
debug: request( \ track requests
debug: beacon( \ debug sending beacons
debug: invalid( \ print invalid packets
debug: regen( \ regenerate keys
debug: recvfrom( \ print received packets
debug: sendto( \ print send packets
debug: avalanche( \ distribution tree
debug: adjust-timer( \ adjust timer
debug: reply( \ test replies
debug: connect( \ connect debugging messages
debug: reveal( \ reveal secrets
debug: fullreveal( \ fully reveal secrets
debug: reconnect( \ reconnect
debug: tweak( \ tweaked key
debug: ivs( \ IVS regen
debug: rtd( \ round trip delay related stuff
debug: no0key( \ generate 0key, default off for now
debug: dvcs( \ print debugging for dvcs
debug: dvcsfiles( \ print debugging for dvcs
debug: rate( \ print debugging for rate settings
debug: health( \ print that a health check passed
debug: verbose( \ print more verbose messages
debug: quicksig( \ quick check for sigs
debug: slurp( \ debug slurp&spit
debug: wallet( \ debug wallet stuff
debug: qr( \ qr code stuff
debug: deprecated( \ deprecated stuff
debug: unhandled( \ unhandled commands
debug: syncfile( \ synchronous file operations
debug: newvault( \ new style vault keys
debug: pks( \ fetch pks
debug: fetch( \ fetch hashed objects
debug: silent( \ silent messages
debug: otrify( \ otrify debug messages
debug: restart( \ restart task on clean-request
debug: msgparse( \ parse messages
debug: args( \ parse arguments

: search-debug ( addr u xt -- ) { xt }
    [: ." debug: " type ;] $tmp
    [ sourcefilename ] SLiteral open-fpath-file throw
    xt [{: xt: xt :}l BEGIN  refill  WHILE
	      source 2over string-prefix? IF  xt  THEN
      REPEAT  2drop ;] execute-parsing-named-file ;
: .debug ( -- ) parse-name 2drop
    ." ±" parse-name 1- dup >r type $F r> - spaces
    '\' parse 2drop source >in @ /string type cr ;

-db profile( \ don't profile by default )
+db ipv6( \ ipv6 should be on by default )
+db ipv4( \ ipv4 should be on by default )
-db ipv64( \ ipv6 over 4
-db xlat464( \ no xlat 464 by default
-db newvault( \ new vault disabled for now )
+db syncfile( \ disable async file operations for now )

\ key debugging task

: toggle ( addr -- )  dup @ 0= swap ! ;

0 Value debug-task
: new-debug-task ( -- ) debug-task ?EXIT
    stacksize4 NewTask4 dup to debug-task activate
    BEGIN  case key
	    'c' of  ['] cmd( >body toggle  endof
	    'm' of  ['] msg( >body toggle  endof
	    'r' of  ['] resend( >body toggle  endof
	    'f' of  ['] file( >body toggle  endof
	    't' of  ['] timeout( >body toggle  endof
	endcase
    AGAIN ;

\ timing ticks

false [IF]
    ' noop alias init-timer
    ' noop alias .times
    : timer: ['] noop alias immediate ;
[THEN]

timer: +file
timer: +send-cmd
timer: +sendX2
timer: +sendX
timer: +chunk
timer: +desta
timer: +inmove
timer: +next
timer: +reset
timer: +event
timer: +calc
timer: +cryptsu
timer: +sig
timer: +sigquick
timer: +enc
timer: +rec
timer: +send
timer: +wait
timer: +cmd
timer: +dest
timer: +ack

\ buffered typing

Ustack b$

?: inskey ( key -- )  key-buffer c$+! ;

up@ Value main-up@

:is 'cold defers 'cold up@ to main-up@ ;
:is 'image defers 'image  0 to main-up@ ;

Variable edit-restart
:is edit-update ( span addr u -- )
    2 pick 0= IF  0 edit-restart !@ ?dup-IF  wake  THEN  THEN
    defers edit-update ;

: <hide> ( task -- ) up@ edit-restart !
    [: ctrl Z unkey ;] swap send-event
    #30000000 stop-ns  edit-restart off ;
: btype  b$ $+! ;
: bemit  b$ c$+! ;
: bflush ( -- )
    [IFUNDEF] gl-emit      b$ $@ defers type b$ $free
    [ELSE]
	up@ main-up@ = IF  b$ $@ defers type b$ $free  EXIT  THEN
	0 b$ !@ [{: w^ x :}h1 x $@ type x $free ;] main-up@ send-event
    [THEN] ;
: bcr    #lf bemit bflush ;
: bat-deltaxy ( dx dy -- ) drop
    dup 0> IF  0 ?DO  bl bemit  LOOP
    ELSE  >r  b$ dup $@len r@ + r> negate $del  THEN ;

' btype ' bemit ' bcr ' form output: b-out
op-vector @
b-out
[IFUNDEF] gl-emit ' (attr!) is attr! [THEN] \ no color on android
' bat-deltaxy is at-deltaxy
op-vector !
\ ' noop alias b-out

:is DoError defers DoError bflush ;
:is .debugline defers .debugline bflush ;

\ misc

: etype ( addr u -- ) >stderr type ;
: $err ( xt -- )  $tmp stderr write-file throw ;
: .black85 ( addr u -- )
    [:  fullreveal( <dim> )else( <black> )
	reveal( 85type )else( nip 5 4 */ spaces ) ;] execute-theme-color ;

\ extra hints for last word executed

false [IF]
    User last-exe-xt
    : .exe ( -- ) last-exe-xt @ .name ;
    : : ( "name" -- colon-sys )
	: lastxt lit, ]] last-exe-xt ! [[ ;
[ELSE]
    : .exe ;
[THEN]

\ more phony throw stuff, only for debugging engine

debugging-method drop false [IF]
:is store-backtrace  ." Store backtrace..." cr defers store-backtrace
    dobacktrace ;

:is throw  ?dup-IF  ." Throw directly " dup . cr dobacktrace
	defers throw  THEN ;
[THEN]

\ Emacs fontlock mode: Highlight more stuff

\\\
Local Variables:
forth-local-words:
    (
     (("debug:" "timer:")
      non-immediate (font-lock-type-face . 2)
      "[ \t\n]" t name (font-lock-variable-name-face . 3))
     ("[a-z]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
     (("[:") definition-starter (font-lock-keyword-face . 1))
     ((";]") definition-ender (font-lock-keyword-face . 1))
    )
End:
[THEN]
