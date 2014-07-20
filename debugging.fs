\ debugging aids

false [IF]
    : debug: ( -- )  Create immediate false ,
      DOES>
	@ IF  ['] noop assert-canary
	ELSE  postpone (  THEN ;
    : )else(  ]] ) ( [[ ; immediate \ )
[THEN]

: nodebug: ['] ( Alias immediate ;
	
: hex[ ]] [: [[ ; immediate
: ]hex ]] ;] $10 base-execute [[ ; immediate
: x~~ ]] hex[ ~~ ]hex [[ ; immediate

: xtype ( addr u -- )  hex[
    bounds ?DO  I c@ 0 <# # # #> type  LOOP  ]hex ;
: .nnb ( addr n -- )  xtype ;
: .64b ( addr -- ) 64 .nnb ;

: (digits>$) ( addr u -- addr' u' ) save-mem
    >r dup dup r> bounds ?DO
	I 2 s>number drop over c! char+ 
    2 +LOOP  over - ;

: hex>$ ( addr u -- addr' u' )
    ['] (digits>$) $10 base-execute ;

: x" ( "hexstring" -- addr u )
    '"' parse hex>$ ;
comp: execute postpone SLiteral ;

\ base64 output (not the usual base64, suitable as filenames)

: .b64 ( n -- n' ) dup >r 6 rshift r> $3F and
    dup #10 u< IF  '0' + emit  EXIT  THEN  #10 -
    dup #26 u< IF  'A' + emit  EXIT  THEN  #26 -
    dup #26 u< IF  'a' + emit  EXIT  THEN  #26 -
    IF  '_'  ELSE  '-'  THEN  emit ;
: .1base64 ( addr -- )
    c@ .b64 .b64 drop ;
: .2base64 ( addr -- )
    le-uw@ .b64 .b64 .b64 drop ;
: .3base64 ( addr -- )
    le-ul@ $FFFFFF and .b64 .b64 .b64 .b64 drop ;
Create .base64s ' drop , ' .1base64 , ' .2base64 , ' .3base64 ,
: 64type ( addr u -- )
    bounds ?DO  I I' over - 3 umin cells .base64s + perform  3 +LOOP ;

: b64digit ( char -- n )
    '0' - dup #09 u<= ?EXIT
    [ 'A' '9' - 1- ]L - dup #36 u<= ?EXIT
    dup #40 = IF  drop #63  EXIT  THEN
    [ 'a' 'Z' - 1- ]L - dup #62 u<= ?EXIT
    drop #62 ;
    
: base64>n ( addr u -- n )  0. 2swap bounds +DO
	I c@ b64digit over lshift rot or swap 6 +
    LOOP  drop ;
: base64>$ ( addr u -- addr' u' ) save-mem >r dup dup r@ bounds ?DO
	I I' over - 4 umin base64>n over le-l! 3 +
    4 +LOOP  drop r> 3 4 */ ;

: 64" ( "base64string" -- addr u )
    '"' parse base64>$ ;
comp: execute postpone SLiteral ;

\ base85 output (derived from RFC 1924, suitable as file name)

85 buffer: 85>chars
s" 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$%&()*+-;<=>?@^_`{|}~"
85>chars swap move
$80 buffer: chars>85
85 0 [DO] [I] dup 85>chars + c@ chars>85 + c! [LOOP]

: .b85 ( n -- n' ) 85 /mod swap 85>chars + c@ emit ;
: .1base85 ( addr -- ) c@ .b85 .b85 drop ;
: .2base85 ( addr -- ) le-uw@ .b85 .b85 .b85 drop ;
: .3base85 ( addr -- ) le-ul@ $FFFFFF and .b85 .b85 .b85 .b85 drop ;
: .4base85 ( addr -- ) le-ul@ .b85 .b85 .b85 .b85 .b85 drop ;
Create .base85s ' drop , ' .1base85 , ' .2base85 , ' .3base85 , ' .4base85 ,
: 85type ( addr u -- )
    bounds ?DO  I I' over - 4 umin cells .base85s + perform  4 +LOOP ;

: b85digit ( char -- n ) $7F umin chars>85 + c@ ;
    
: base85>n ( addr u -- n )  0 1 2swap bounds +DO
	I c@ b85digit over * rot + swap 85 *
    LOOP  drop ;
: base85>$ ( addr u -- addr' u' ) save-mem >r dup dup r@ bounds ?DO
	I I' over - 5 umin base85>n over le-l! 4 +
    5 +LOOP  drop r> 4 5 */ ;

: 85" ( "base85string" -- addr u )
    '"' parse base85>$ ;
comp: execute postpone SLiteral ;

\ debugging switches

debug: timing(
debug: bursts(
debug: resend(
debug: track(
debug: data(
debug: cmd(
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
debug: save( \ save once per round
debug: bg( \ started in background mode
debug: nat( \ NAT traversal stuff
debug: route( \ do routing
debug: noipv6( \ use only ipv4 for routing
debug: noipv4( \ use only ipv6 for routing
debug: request( \ track requests
debug: beacon( \ debug sending beacons
debug: replace-beacon( \ reply to every beacon
debug: kalloc( \ secure allocate

-db profile( \ )

\ key debugging task

: toggle ( addr -- )  dup @ 0= swap ! ;

: debug-task ( -- )  stacksize4 NewTask4 activate
    BEGIN  case key
	    'c' of  ['] cmd( >body toggle  endof
	    'm' of  ['] msg( >body toggle  endof
	    'r' of  ['] resend( >body toggle  endof
	    'f' of  ['] file( >body toggle  endof
	    't' of  ['] timeout( >body toggle  endof
	endcase
    AGAIN ;

\ timing ticks

64Variable tick-adjust
: ticks ( -- u )  ntime d>64 tick-adjust 64@ 64+ ;

: ticks-u ( -- u )  ticks 64>n ;

false [IF]
    ' noop alias init-timer
    ' noop alias .times
    : timer: ['] noop alias immediate ;
[THEN]

require date.fs
1970 1 1 ymd2day Constant unix-day0

: fsplit ( r -- r n )  fdup floor fdup f>s f- ;

: .ticks ( ticks -- )
    64>f 1e-9 f* 86400e f/ fsplit unix-day0 + day2ymd
    rot 0 .r '-' emit swap 0 .r '-' emit 0 .r 'T' emit
    24e f* fsplit 0 .r ':' emit 60e f* fsplit 0 .r ':' emit
    60e f* fdup 10e f< IF '0' emit 5  ELSE  6  THEN  3 3 f.rdp 'Z' emit ;

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

User b$

: btype  b$ $+! ;
: bemit  b$ c$+! ;
: bcr    #lf bemit b$ $@ (type) b$ $off ;

' btype ' bemit ' bcr ' form output: b-out
\ ' noop alias b-out

\ misc

: etype ( addr u -- ) >stderr type ;
: $err ( xt -- )  $tmp stderr write-file throw ;
\ : $err ( xt -- ) execute ;

\ extra hints for last word executed

: ?int ( throw-code -- throw-code )  dup -28 = IF  bye  THEN ;

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

debugging-method [IF]
:noname  ." Store backtrace..." cr defers store-backtrace
    dobacktrace ; is store-backtrace

:noname  ?dup-IF  ." Throw directly" cr dobacktrace
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
