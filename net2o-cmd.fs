\ generic net2o command interpreter

\ net2o commands are UTF-8 coded, not byte coded.
\ Command streams contain both commands and data
\ the dispatcher is a byte-wise dispatcher, though
\ commands come in junks of 8 bytes
\ Commands are zero-terminated

Variable cmd
Variable cmd' 0 cmd' !

: ?lit-space ( addr u -- addr u )
    dup 1 cells < abort" Command space exhausted" ;
: prefetch ( addr u -- addr' u' n ) ?lit-space
    over @ >r 1 cells /string r> ;
: byte-fetch ( addr u -- addr' u' n )
    cmd' @ 7 and 0= IF  prefetch cmd ! cmd' off  THEN
    cmd cmd' @ + c@  1 cmd' +! ;

2Variable buf-state

: net2o-crash  base @ >r hex
    cmd @ 8 .r cmd' ? buf-state 2@ swap 8 u.r 8 u.r cr
    r> base !
    true abort" Unimplemented net2o function" ;

Create cmd-base-table 256 0 [DO] ' net2o-crash , [LOOP]

: cmd-dispatch ( addr u -- addr' u' )
    byte-fetch >r buf-state 2! r> cells cmd-base-table + perform buf-state 2@ ;

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

: cmd-loop ( addr u -- )  cmd' off  sp@ >r
    BEGIN  cmd-dispatch  dup 0= cmd' @ 0= and  UNTIL  r> sp! 2drop ;

' cmd-loop is queue-command

\ command helper

: utf8-byte@ ( -- xc )
    byte-fetch  dup $80 u< ?EXIT  \ special case ASCII
    dup $C2 u< IF  UTF-8-err throw  THEN  \ malformed character
    $7F and  $40 >r
    BEGIN  dup r@ and  WHILE  r@ xor
	    6 lshift r> 5 lshift >r >r byte-fetch
	    dup $C0 and $80 <> IF   UTF-8-err throw  THEN
	    $3F and r> or
    REPEAT  rdrop ;

\ commands

Defer net2o-do
: net2o-exec  cell+ perform ;

: executer ['] net2o-exec IS net2o-do ;
executer

: net2o-does  DOES> net2o-do ;
: net2o: ( number "name" -- )
    ['] noop over >cmd \ allocate space in table
    Create dup >r , here >r 0 , net2o-does noname :
    lastxt dup r> ! r> >cmd ;

Vocabulary net2o-base

also net2o-base definitions previous

\ Command numbers preliminary and subject to change

0 net2o: end-cmd ( -- ) 0 cmd' !  0. buf-state 2! ;
1 net2o: lit ( -- x )  buf-state 2@ prefetch >r buf-state 2! r> ;
2 net2o: string ( -- addr u )  buf-state 2@
    2dup over xc@+ nip dup xc-size + aligned safe/string buf-state 2!
    >r xc@+ r> umin ;
3 net2o: char ( -- xc )
    utf8-byte@ ;

\ these functions are only there to test the server

4 net2o: emit ( xc -- ) xemit ;
5 net2o: type ( addr u -- )  type ;
6 net2o: . ( -- ) . ;
7 net2o: cr ( -- ) cr ;

definitions

\ net2o assembler

Variable cmdbuf $800 allot  here Constant endcmdbuf
Variable cmdaccu 0 ,
Variable cmdslot
Variable cmdextras

: cmdreset  cmdbuf off  cmdslot off  cmdextras off ;

cmdreset

: @+ ( addr -- n addr' )  dup @ swap cell+ ;

: cmdflush
    cmdaccu @ cmdbuf @+ + ! cmdextras @ 1 cells + cmdbuf +!
    cmdslot @ 8 - 0 max cmdslot !
    cmdaccu cell+ @ cmdaccu ! cmdextras off ;
: cmdflush?  cmdslot @ 8 u>= IF
	cmdflush
    THEN ;
: cmd, ( n -- )  cmdaccu 2 cells cmdslot @ /string xc!+? 2drop
    cmdaccu - cmdslot ! cmdflush? ;

: net2o, @ cmd, ;

: net2o-code  ['] net2o, IS net2o-do also net2o-base ;

: send-cmd ( dest addr -- ) 2>r  cmdbuf cell+ 2r> swap
    cmdbuf @ $20  <= IF  sendA  cmdbuf off  EXIT  THEN
    cmdbuf @ $80  <= IF  sendB  cmdbuf off  EXIT  THEN
    cmdbuf @ $200 <= IF  sendC  cmdbuf off  EXIT  THEN
    cmdbuf @ $800 <= IF  sendD  cmdbuf off  EXIT  THEN
    true abort" too many commands" ;

\ net2o assembler stuff

also net2o-base definitions

: $, ( addr u -- )  dup >r cmdbuf @+ + cmdextras @ + cell+
    endcmdbuf over - xc!+? 0= abort" didn't fit"
    r@ min move
    r> dup xc-size + aligned cmdextras +!  string ;
: lit, ( n -- )  cmdbuf @+ + cmdextras @ + cell+ !  1 cells cmdextras +!  lit ;
: char, ( xc -- )  char cmd, ;
: end-code  cmdflush previous ;

previous definitions

\ commands to read and write files

also net2o-base definitions forth

10 net2o: throw ( error -- )  throw ;
11 net2o: new-map ( addr u -- )  n2o:new-map ;
12 net2o: new-context ( -- ) n2o:new-context job-context ! ;
13 net2o: new-data ( addr u -- ) n2o:new-data ;
14 net2o: new-code ( addr u -- ) n2o:new-code ;
15 net2o: open-file ( addr u mode id -- )  n2o:open-file ;
16 net2o: file-size ( id -- size )  id>file file-size >throw drop ;

\ create commands to send back

also net2o-base

20 net2o: push-$    $, ;
21 net2o: push-lit  lit, ;
22 net2o: push-char char, ;

previous

23 net2o: push:     utf8-byte@ cmd, ;
24 net2o: start-cmd cmdreset ;
25 net2o: end-cmd   cmdflush ;

previous definitions

