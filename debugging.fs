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
debug: dht( \ debuggin for dht functions
debug: hash( \ dht hasing function debug
debug: file( \ file read/write debugging
debug: save( \ separate save task
debug: bg( \ started in background mode
debug: nat( \ NAT traversal stuff

-db profile(

\ key debugging task

: toggle ( addr -- )  dup @ 0= swap ! ;

: debug-task ( -- )  stacksize4 NewTask4 activate
    BEGIN  case key
	    'c' of  ['] cmd( >body toggle  endof
	    'r' of  ['] resend( >body toggle  endof
	    'f' of  ['] file( >body toggle  endof
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

\ misc

: etype ( addr u -- ) >stderr type ;
: $err ( xt -- )  $tmp stderr write-file throw ;

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
