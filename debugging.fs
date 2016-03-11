\ debugging aids

false [IF]
    : debug: ( -- )  Create immediate false ,
      DOES>
	@ IF  ['] noop assert-canary
	ELSE  postpone (  THEN ;
    : )else(  ]] ) ( [[ ; immediate \ )
[THEN]

: nodebug: ['] ( Alias immediate ;

require xtype.fs
require base64.fs
require base85.fs

: .nnb ( addr n -- )  85type ;
: .64b ( addr -- ) 64 .nnb ;

synonym \U \G \ comment for help

\ debugging switches

debug: timing(
debug: bursts(
debug: resend(
debug: track(
debug: data(
debug: cmd(
debug: cmd0(
debug: send(
debug: firstack(
debug: msg(
debug: stat(
debug: timeout(
debug: ack(
debug: crypt(
debug: noens(
debug: key(
debug: genkey( \ See generated keys - never let this go to a log file!
debug: cookie( 
debug: cookies( \ dump all cookies on rewinding
debug: delay( \ used to add delays at performance critical places
debug: tag(
debug: flush(
debug: flush1(
debug: flush2(
debug: flush3(
debug: waitkey(
debug: address(
debug: dump(
debug: trace(
debug: header(
debug: sender( \ extra sender task
debug: dht( \ debugging for dht functions
debug: hash( \ dht hasing function debug
debug: file( \ file read/write debugging
debug: bg( \ started in background mode
debug: nat( \ NAT traversal stuff
debug: route( \ do routing
debug: noipv6( \ use only ipv4 for routing
debug: noipv4( \ use only ipv6 for routing
debug: request( \ track requests
debug: beacon( \ debug sending beacons
debug: replace-beacon( \ reply to every beacon
debug: kalloc( \ secure allocate
debug: invalid( \ print invalid packets
debug: regen( \ regenerate keys
debug: sema( \ semaphores
debug: recvfrom(
debug: sendto(
debug: avalanche( \ distribution tree
debug: adjust-timer( \ adjust timer
debug: reply( \ test replies
debug: connect( \ connect debugging messages
debug: reveal( \ reveal secrets
debug: reconnect( \ reconnect
debug: tweak( \ tweaked key
debug: ivs( \ IVS regen
debug: rtd( \ round trip delay related stuff

-db profile( \ )

0 [IF]
false warnings !@

: c-section ( xt addr -- ) 
    \G implement a critical section that will unlock the semaphore
    \G even in case there's an exception within.
    { sema }
    sema lock
    sema( ." sema: " sema dup hex. body> >name .name ." lock" cr )
    catch
    sema( ." sema: " sema dup hex. body> >name .name ." unlock" cr )
    sema unlock
    throw ;

warnings !
[THEN]

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
timer: +enc
timer: +rec
timer: +send
timer: +wait
timer: +cmd
timer: +dest
timer: +ack

\ buffered typing

Ustack b$

[IFUNDEF] inskey
    : inskey ( key -- )  key-buffer c$+! ;
[THEN]

event: ->b$off b$ $off ;
event: ->type defers type <event ->b$off event> ctrl L inskey ;
event: ->hide ctrl Z inskey <event ->wake event> ;
: btype  b$ $+! ;
: bemit  b$ c$+! ;
: <hide> ( task -- )
    <event up@ elit, ->hide event>  stop ;
: bflush ( -- )
    [ up@ ]l <hide>
    b$ $@ <event up@ elit, e$, ->type [ up@ ]l event>
    BEGIN  b$ @  WHILE  stop  REPEAT ;
: bcr    #lf bemit bflush ;
: bat-deltaxy ( dx dy -- ) drop
    dup 0> IF  0 ?DO  bl bemit  LOOP
    ELSE  >r  b$ dup $@len r@ + r> negate $del  THEN ;

' btype ' bemit ' bcr ' form output: b-out
op-vector @
b-out
[IFUNDEF] android ' (attr!) is attr! [THEN] \ no color on android
' bat-deltaxy is at-deltaxy
op-vector !
\ ' noop alias b-out

:noname defers DoError bflush ; is DoError
:noname defers .debugline bflush ; is .debugline

\ misc

[IFUNDEF] do-debug
    : do-debug ( xt -- )
	op-vector @ { oldout }
	debug-vector @ op-vector !
	catch oldout op-vector ! throw ;
[THEN]

: etype ( addr u -- ) >stderr type ;
: $err ( xt -- )  $tmp stderr write-file throw ;
\ : $err ( xt -- ) execute ;
[IFDEF] traceall  what's name notrace [THEN]
op-vector @
$-out ' (attr!) is attr!
op-vector !
[IFDEF] traceall  is name [THEN]

\ extra hints for last word executed

: m: : ;
false [IF]
    User last-exe-xt
    : .exe ( -- ) last-exe-xt @ .name ;
    : : ( "name" -- colon-sys )
	: lastxt ]]L last-exe-xt ! [[ ;
[ELSE]
    : .exe ;
[THEN]

\ more phony throw stuff, only for debugging engine

debugging-method drop 0 [IF]
:noname  ." Store backtrace..." cr defers store-backtrace
    dobacktrace ; is store-backtrace

:noname  ?dup-IF  ." Throw directly " dup . cr dobacktrace
	defers throw  THEN ; is throw
[THEN]

\ Emacs fontlock mode: Highlight more stuff

0 [IF]
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
