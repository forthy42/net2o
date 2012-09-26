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

: +t ( addr -- )
    ticks-u dup last-tick !@ - swap +! ;

true [IF]
    Variable timer-list
    : timer: Create 0 , here timer-list !@ ,
      DOES> profile( +t EXIT ) drop ;
    : map-timer { xt -- }
	timer-list BEGIN  @ dup  WHILE dup >r
		cell - xt execute r> REPEAT drop ;
    
    : init-timer ( -- )
	ticks-u last-tick ! [: off ;] map-timer ;
    
    : .times ( -- ) profile(
	[: dup body> >name name>string 1 /string
	   tuck type 8 swap - 0 max spaces ." : "
	   @ s>f 1n f* f. cr ;] map-timer ) ;
[ELSE]
    ' noop alias init-timer
    ' noop alias .times
    : timer: ['] noop alias immediate ;
[THEN]

timer: +calc1
timer: +calc
timer: +enc
timer: +rec
timer: +send
timer: +wait

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
    )
End:
[THEN]
