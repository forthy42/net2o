\ generic net2o command interpreter

\ net2o commands are UTF-8 coded, not byte coded.
\ Command streams contain both commands and data
\ the dispatcher is a byte-wise dispatcher, though
\ commands come in junks of 8 bytes
\ Commands are zero-terminated

: net2o-crash true abort" Unimplemented net2o function" ;

Create cmd-base-table 256 0 [DO] ' net2o-crash , [LOOP]

Variable cmd
Variable cmd' 0 cmd' !

: prefetch ( addr u -- addr' u' n )
    dup 1 cells < abort" Command space exhausted"
    over @ >r 1 cells /string r> ;
: byte-fetch ( -- n )
    cmd' @ 0= IF  prefetch cmd !  THEN
    cmd cmd' @ + c@  1 cmd' +! ;

: cmd-dispatch ( addr u -- addr' u' )
    byte-fetch cells cmd-base-table + perform ;

: extend-cmds ( -- xt ) noname Create lastxt $40 0 DO ['] net2o-crash , LOOP
  DOES>  >r byte-fetch $80 - $3F umin cells r> + perform ;

Create 'cmd-buf 6 allot

: >cmd ( xt u -- ) 'cmd-buf 6 xc!+? 2drop  'cmd-buf tuck -
    cmd-base-table >r
    BEGIN  dup 1 >  WHILE  over c@ >r 1 /string r>
	    cells r> + dup @ ['] net2o-crash = IF
		extend-cmds over !
	    THEN
	    @ >body $80 cells - >r
    REPEAT
    drop c@ cells r> + ! ;
