\ debugging aids

: debug)  ]] THEN [[ ;

true [IF]
    : debug: ( -- ) Create immediate false ,
      DOES>
	state @ IF  ]] Literal @ IF [[
		['] debug) assert-canary
	    ELSE  @ IF ['] noop assert-canary
		ELSE postpone (
		THEN
	THEN  ;
[ELSE]
    : debug: ( -- )  Create immediate false ,
      DOES>
	@ IF  ['] noop assert-canary
	ELSE  postpone (  THEN ;
[THEN]

: hex[ ]] base @ >r hex [[ ; immediate
: ]hex ]] r> base ! [[ ; immediate
: x~~ ]] hex[ ~~ ]hex [[ ; immediate

\ debugging switches

debug: timing(
debug: rate(
debug: ratex(
debug: deltat(
debug: slack(
debug: slk(
debug: bursts(
debug: resend(
debug: track(
debug: data(
debug: cmd(
debug: send(
debug: firstack(
debug: msg(
debug: profile(
debug: stat(
debug: timeout(
debug: ack(

: +db ( "word" -- ) ' >body on ;

Variable debug-eval

: +debug ( -- )
    BEGIN  argc @ 1 > WHILE
	    1 arg s" +" string-prefix?  WHILE
		1 arg debug-eval $!
		s" db " debug-eval 1 $ins
		s" (" debug-eval $+!
		debug-eval $@ evaluate
		shift-args
	REPEAT  THEN ;

\ timing measurements

Variable last-tick

: ticks-u ( -- u )  ntime drop ;
: !@ ( value addr -- old-value )   dup @ >r ! r> ;

: delta-t ( -- n )
    ticks-u dup last-tick !@ - ;

: timing ;
[IFDEF] timing
    Variable calc-time
    Variable calc1-time
    Variable send-time
    Variable rec-time
    Variable enc-time
    Variable wait-time
    
    : init-timer ( -- )
	ticks-u last-tick !
	calc-time  off  send-time off
	rec-time   off  wait-time off
	calc1-time off  enc-time  off ;
    
    : +calc  profile( delta-t calc-time +! ) ;
    : +calc1 profile( delta-t calc1-time +! ) ;
    : +send  profile( delta-t send-time +! ) ;
    : +enc   profile( delta-t enc-time +! ) ;
    : +rec   profile( delta-t rec-time +! ) ;
    : +wait  profile( delta-t wait-time +! ) ;
    
    : .times ( -- ) profile(
	." wait: " wait-time @ s>f 1n f* f. cr
	." send: " send-time @ s>f 1n f* f. cr
	." rec : " rec-time  @ s>f 1n f* f. cr
	." enc : " enc-time  @ s>f 1n f* f. cr
	." calc: " calc-time @ s>f 1n f* f. cr
	." calc1: " calc1-time @ s>f 1n f* f. cr ) ;
[ELSE]
    ' noop alias +calc immediate
    ' noop alias +calc1 immediate
    ' noop alias +send immediate
    ' noop alias +rec immediate
    ' noop alias +wait immediate
    ' noop alias +enc immediate
    ' noop alias init-timer
    ' noop alias .times
[THEN]

0 [IF]
Local Variables:
forth-local-words:
    (
     (("debug:" "field:" "sffield:" "dffield:" "64field:")
      non-immediate (font-lock-type-face . 2)
      "[ \t\n]" t name (font-lock-variable-name-face . 3))
     ("[a-z]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
End:
[THEN]
