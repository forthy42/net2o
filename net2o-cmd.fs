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
    over be-ux@ >r 1 cells /string r> ;
: byte-fetch ( addr u -- addr' u' n )
    cmd' @ 7 and 0= IF  prefetch cmd be-x! cmd' off  THEN
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

: cmd-loop ( addr u -- )
\    ticks u. ." do-cmd" cr
    cmd' off  sp@ >r
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

forth also net2o-base definitions previous

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

: cmdbuf     job-context @ cmd-out $@ drop cmd-buf# ;
: endcmdbuf  job-context @ cmd-out $@ + ;
: cmdaccu    job-context @ cmd-out $@ drop cmd-accu# ;
: cmdslot    job-context @ cmd-out $@ drop cmd-slot ;
: cmdextras  job-context @ cmd-out $@ drop cmd-extras ;

: cmdreset  cmdbuf off  cmdslot off  cmdextras off ;

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
    max-size^2 1+ 0 DO
	cmdbuf @ min-size I lshift u<= IF  I sendX  cmdreset  UNLOOP  EXIT  THEN
    LOOP  true abort" too many commands" ;

\ net2o assembler stuff

also net2o-base definitions

: $, ( addr u -- )  dup >r cmdbuf @+ + cmdextras @ + cell+
    endcmdbuf over - xc!+? 0= abort" didn't fit"
    r@ min move
    r> dup xc-size + aligned cmdextras +!  string ;
: lit, ( n -- )  cmdbuf @+ + cmdextras @ + cell+ be-x!  1 cells cmdextras +!  lit ;
: char, ( xc -- )  char cmd, ;
: end-code ( -- ) cmdflush previous ;

previous definitions

\ commands to read and write files

also net2o-base definitions forth

10 net2o: throw ( error -- )  throw ;
11 net2o: new-map ( addr u -- )  n2o:new-map ;
12 net2o: new-code-map ( addr u -- )  n2o:new-code-map ;
13 net2o: new-context ( -- ) n2o:new-context job-context ! ;
14 net2o: new-data ( addr u -- ) n2o:new-data ;
15 net2o: new-code ( addr u -- ) n2o:new-code ;
16 net2o: open-file ( addr u mode id -- )  n2o:open-file ;
17 net2o: close-file ( id -- )  n2o:close-file ;
18 net2o: file-size ( id -- size )  id>file file-size >throw drop ;
19 net2o: slurp-chunk ( id -- )  id>file data$@ rot read-file >throw /data ;
20 net2o: send-chunk ( -- ) net2o:send-chunk ;
21 net2o: send-chunks ( -- ) net2o:send-chunks ;
22 net2o: ack-addrtime ( addr time1 time2 -- )  net2o:ack-addrtime ;
23 net2o: rate-adjust ( -- )  net2o:rate-adjust ;
24 net2o: set-rate ( ticks )  net2o:set-rate ;
25 net2o: ack-range ( addr u -- )  net2o:ack-range ;
26 net2o: resend ( addr u -- )  net2o:resend ;
27 net2o: receive-key ( addr u -- )  net2o:receive-key  keypad set-key ;

\ create commands to send back

also net2o-base
: send-key ( pk -- )  net2o:send-key $, receive-key ;

30 net2o: push-$    $, ;
31 net2o: push-lit  lit, ;
32 net2o: push-char char, ;

previous

33 net2o: push'     utf8-byte@ cmd, ;
34 net2o: cmd:      cmdreset ;
35 net2o: cmd;      cmdflush ;

previous definitions

\ client side timing

Variable firstb-ticks
Variable delta-ticks
Variable ack-sizes

: ack-size ( -- )  inbuf packet-size ack-sizes +! ;
: ack-firstb ( -- )  ticks firstb-ticks !  ack-size ;
: ack-lastb ( -- )  ticks firstb-ticks @ - delta-ticks +! ;

Create ack-timetable
' ack-size ,
' ack-firstb ,
' ack-lastb ,
' ack-lastb ,

: ack-timing ( n -- )
    2/ 3 and cells ack-timetable + perform ;

also net2o-base
: >rate ( -- )  ack-sizes @ 0= ?EXIT
    delta-ticks @ #1000 ack-sizes @ 1 max */ lit, set-rate
    delta-ticks off  ack-sizes off ;

: net2o:acktime ( -- )
    dest-addr @ -$20 and inbuf c@ $F and or lit, ticks lit, ack-addrtime ;
: net2o:ackrange ( -- )
    job-context @ data-ack $@ dup IF
	over 2@ drop >r + 2 cells - 2@ + r> tuck - swap lit, lit, ack-range
    ELSE  2drop  THEN ;
: net2o:sendack ( -- )
    net2o:acktime  >rate  ( rate-adjust )  net2o:ackrange
    cmdflush cmdbuf @+ swap
    code-dest job-context @ return-address @
    net2o:send-code-packet drop cmdreset ;
: net2o:do-resend ( -- )
    job-context @ data-ack $@ 2 cells - 0 max bounds ?DO
	I 2@ swap lit, lit, resend
    2 cells +LOOP
    job-context @ data-ack $@ nip 2 cells > IF
	send-chunks
    THEN
    net2o:sendack
    job-context @ pending-ack off ;

: net2o:do-ack ( -- )  job-context @ >r
    dest-addr @ inbuf body-size job-context @ data-ack del-range
\    net2o:acktime

    inbuf 1+ c@ acks# and
    dup r@ ack-receive !@ xor ack-toggle# and
    IF
	net2o:do-resend
\	net2o:sendack
\	send-ack# and IF
\	    r@ pending-ack @ 0= IF
\		['] net2o:do-resend #1000000 add-queue
\	    THEN
\	    r@ pending-ack on
\	THEN
    THEN  rdrop
    inbuf 1+ c@ ack-timing ;
' net2o:do-ack IS do-ack

previous
