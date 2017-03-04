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
synonym \O \G \ comment for help

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
debug: wait(
debug: ack(
debug: crypt(
debug: noens(
debug: key( \ key stuff
debug: vkey( \ vault key stuff
debug: genkey( \ See generated keys - never let this go to a log file!
debug: cookie( 
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
debug: ipv6( \ use ipv6
debug: ipv4( \ use ipv4
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
debug: no0key( \ generate 0key, default off for now
debug: dvcs( \ print debugging for dvcs
debug: rate( \ print debugging for rate settings
debug: health( \ print that a health check passed
debug: verbose( \ print more verbose messages
debug: quicksig( \ quick check for sigs

-db profile( \ )
+db ipv6( \ ipv6 should be on by default )
+db ipv4( \ ipv4 should be on by default )

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

up@ Value main-up@

:noname defers 'cold up@ to main-up@ ; is 'cold

event: ->type { w^ x } x $@ type x $off ;
event: ->hide ctrl Z inskey <event ->wake event> ;
: btype  b$ $+! ;
: bemit  b$ c$+! ;
: <hide> ( task -- )
    <event up@ elit, ->hide event>  stop ;
: bflush ( -- )
    [IFUNDEF] android    b$ $@ defers type b$ $off
    [ELSE]
	up@ main-up@ = IF  b$ $@ defers type b$ $off  EXIT  THEN
	0 b$ !@ <event elit, ->type main-up@ event>
    [THEN] ;
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
[IFDEF] traceall  is name traceall [THEN]

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

debugging-method [defined] record-locs and [IF] record-locs [THEN]
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
