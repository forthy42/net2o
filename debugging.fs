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
debug: crypt(
debug: ens(
debug: key(
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

+db ens(

\ timing measurements

64Variable timer-tick
Variable last-tick

\ timing ticks

[IFDEF] 64bit
    : ticks ( -- u )  ntime drop ;
[ELSE]
    ' ntime Alias ticks
[THEN]

: ticks-u ( -- u )  ntime drop ;
: !@ ( value addr -- old-value )   dup @ >r ! r> ;

: +t ( addr -- )
    ticks-u dup last-tick !@ - swap +! ;

true [IF]
    Variable timer-list
    : timer: Create 0 , here timer-list !@ ,
      DOES> profile( +t )else( drop ) ;
    : map-timer { xt -- }
	timer-list BEGIN  @ dup  WHILE dup >r
		cell - xt execute r> REPEAT drop ;
    
    : init-timer ( -- )
	ticks-u last-tick ! [: off ;] map-timer ;
    
    : .times ( -- ) profile(
	[: dup body> >name name>string 1 /string
	   tuck type 8 swap - 0 max spaces ." : "
	   @ s>f 1n f* f. cr ;] map-timer ) ;

    : !time ( -- ) ticks timer-tick 64! ;
    : @time ( -- f ) ticks timer-tick 64@ 64- 64>f 1e-9 f* ;
    : .time ( -- ) @time f. ." s" ;
[ELSE]
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
