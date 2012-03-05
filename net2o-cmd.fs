\ generic net2o command interpreter

\ net2o commands are protobuf coded, not byte coded.

\ command helper

2Variable buf-state

: u>n ( u -- n )
    dup 2/ swap 1 and IF negate THEN ;
: n>u ( n -- u )
    dup 0< 1 and swap abs 2* or ;

: p@+ ( addr -- u addr' )  >r 0
    BEGIN  7 lshift r@ c@ $7F and or r@ c@ $80 and  WHILE
	    r> 1+ >r  REPEAT  r> 1+ ;
: p!+ ( u addr -- addr' )  >r
    <<#  dup $7F and hold  7 rshift
    BEGIN  dup  WHILE  dup $7F and $80 or hold 7 rshift  REPEAT
    0 #> tuck r@ swap move r> + #>> ;
: ps!+ ( n addr -- addr' )
    >r n>u r> p!+ ;
: ps@+ ( addr -- n addr' )
    p@+ >r u>n r> ;

: p@ ( -- u ) buf-state 2@ over + >r p@+ r> over - buf-state 2! ;
: ps@ ( -- n ) p@ u>n ;

: byte@ ( addr u -- addr' u' b )
    >r count r> 1- swap ;

\ Command streams contain both commands and data
\ the dispatcher is a byte-wise dispatcher, though
\ commands come in junks of 8 bytes
\ Commands are zero-terminated

: net2o-crash  base @ >r hex
    buf-state 2@ swap 8 u.r 8 u.r ." :" buf-state 2@ drop 1- c@ 2 u.r cr
    r> base !  buf-state 2@ dump
    true abort" Unimplemented net2o function" ;

Create cmd-base-table 256 0 [DO] ' net2o-crash , [LOOP]

: cmd-dispatch ( addr u -- addr' u' )
    byte@ >r buf-state 2! r> cells cmd-base-table + perform buf-state 2@ ;

: cmd@ ( -- u ) buf-state 2@ byte@ >r buf-state 2! r> ;

: extend-cmds ( -- xt ) noname Create lastxt $100 0 DO ['] net2o-crash , LOOP
  DOES>  >r cmd@ cells r> + perform ;

8 buffer: 'cmd-buf

: >cmd ( xt u -- ) 'cmd-buf p!+  'cmd-buf tuck -
    cmd-base-table >r
    BEGIN  dup 1 >  WHILE  over c@ >r 1 /string r>
	    cells r> + dup @ ['] net2o-crash = IF
		extend-cmds over !
	    THEN
	    @ >body >r
    REPEAT
    drop c@ cells r> + ! ;

Defer >throw

: cmd-loop ( addr u -- )
\    ticks u. ." do-cmd" cr
    sp@ >r
    TRY  BEGIN  cmd-dispatch  dup 0=  UNTIL
	IFERROR  dup DoError nothrow >throw  THEN  ENDTRY  drop  r> sp! 2drop ;

' cmd-loop is queue-command

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

: F also forth parse-name parser1 execute previous ; immediate

Vocabulary net2o-base

forth also net2o-base definitions previous

\ Command numbers preliminary and subject to change

0 net2o: end-cmd ( -- ) 0. buf-state 2! ;
1 net2o: ulit ( -- x ) p@ ;
2 net2o: slit ( -- x ) ps@ ;
3 net2o: string ( -- addr u )  buf-state 2@ over + >r
    p@+ swap 2dup + r> over - buf-state 2! ;

\ these functions are only there to test the server

4 net2o: emit ( xc -- ) xemit ;
5 net2o: type ( addr u -- )  type ;
6 net2o: . ( -- ) . ;
7 net2o: cr ( -- ) cr ;

definitions

\ net2o assembler

: cmdbuf     j^ cmd-out $@ drop cmd-buf# ;
: endcmdbuf  j^ cmd-out $@ + ;

: cmdreset  cmdbuf off ;

: @+ ( addr -- n addr' )  dup @ swap cell+ ;

: cmd, ( n -- )  cmdbuf @+ + dup >r p!+ r> - cmdbuf +! ;

: net2o, @ cmd, ;

: net2o-code  ['] net2o, IS net2o-do also net2o-base ;
net2o-code previous

: send-cmd ( addr -- )  code-packet on
    cmdbuf cell+ swap j^ return-address @
    max-size^2 1+ 0 DO
	cmdbuf @ min-size I lshift u<= IF  I sendX  cmdreset  UNLOOP  EXIT  THEN
    LOOP  true abort" too many commands" ;

: 0cmd ( -- )  0 send-cmd ;
: scmd ( -- )  code-dest send-cmd ;

\ net2o assembler stuff

also net2o-base definitions

: $, ( addr u -- )  string  >r r@ cmd,
    r@ endcmdbuf cmdbuf @+ + - u>= abort" didn't fit"
    cmdbuf @+ + r@ move   r> cmdbuf +! ;
: lit, ( u -- )  ulit cmd, ;
: slit, ( n -- )  slit n>u cmd, ;
: end-code ( -- ) end-cmd previous ;

previous definitions

\ commands to read and write files

also net2o-base definitions forth

11 net2o: new-context ( -- ) return-addr @ n2o:new-context ;
12 net2o: new-data ( addr u -- ) n2o:new-data ;
13 net2o: new-code ( addr u -- ) n2o:new-code ;

net2o-base

: data-map, ( addr u -- )  2dup n2o:new-data swap lit, lit, new-data ;
: code-map, ( addr u -- )  2dup n2o:new-code swap lit, lit, new-code ;

14 net2o: open-file ( addr u mode id -- )  n2o:open-file ;
15 net2o: close-file ( id -- )  n2o:close-file ;
16 net2o: file-size ( id -- size )  id>file F file-size throw drop ;
17 net2o: slurp-chunk ( id -- ) id>file data$@ rot read-file throw /data ;
18 net2o: send-chunk ( -- ) net2o:send-chunk ;
19 net2o: send-chunks ( -- ) net2o:send-chunks ;

20 net2o: ack-addrtime ( addr time -- )  net2o:ack-addrtime ;
21 net2o: ack-resend ( flag -- )  net2o:ack-resend ;
22 net2o: set-rate ( ticks1 ticks2 -- )  net2o:set-rate ;
23 net2o: ack-range ( addr u -- )  net2o:ack-range ;
24 net2o: resend ( addr u -- )  net2o:resend ;
25 net2o: resend-mask ( addr mask -- ) net2o:resend-mask ;

\ crypto functions

26 net2o: receive-key ( addr u -- )  net2o:receive-key  keypad set-key ;
27 net2o: gen-data-ivs ( addr u -- ) net2o:gen-data-ivs ;
28 net2o: gen-code-ivs ( addr u -- ) net2o:gen-code-ivs ;
29 net2o: gen-rdata-ivs ( addr u -- ) net2o:gen-rdata-ivs ;
30 net2o: gen-rcode-ivs ( addr u -- ) net2o:gen-rcode-ivs ;

\ create commands to send back

: send-key ( pk -- )  net2o:send-key $, receive-key ;
: data-ivs ( -- ) \ two IV seeds for send and receive data
    rng$ 2dup $, gen-data-ivs net2o:gen-rdata-ivs
    rng$ 2dup $, gen-rdata-ivs net2o:gen-data-ivs ;
: code-ivs ( -- ) \ two IV seeds for send and receive code
    rng$ 2dup $, gen-code-ivs net2o:gen-rcode-ivs
    rng$ 2dup $, gen-rcode-ivs net2o:gen-code-ivs ;

31 net2o: push-$    $, ;
32 net2o: push-slit slit, ;
33 net2o: push-char lit, ;
' push-char alias push-lit

34 net2o: push'     p@ cmd, ;
35 net2o: cmd:      cmdreset ;
36 net2o: cmd;      end-cmd  scmd ;

\ better slurping

37 net2o: slurp-block ( seek maxlen id -- nextseek )
          n2o:slurp-block ;
38 net2o: track-size ( size id -- )
          track( 2dup ." file <" 0 .r ." > size: " F . F cr ) size! ;
39 net2o: track-seek ( seek id -- )
          track( 2dup ." file <" 0 .r ." > seek: " F . F cr ) seek! ;
40 net2o: open-tracked-file ( addr u mode id -- )
          dup >r n2o:open-file
          r@ id>file F file-size throw drop lit, r> lit, track-size ;
41 net2o: slurp-tracked-block ( seek maxlen id -- )
          dup >r n2o:slurp-block lit, r> lit, track-seek ;
42 net2o: rewind-sender ( -- )  net2o:rewind-sender ;

: rewind ( -- )  net2o:rewind-receiver rewind-sender ;

\ This must be defined last, otherwise dangerous name-clash!

43 net2o: throw ( error -- )  throw ;

net2o-base

: lit<   lit, push-lit ;
: slit<  slit, push-slit ;
:noname  server? IF
	dup  IF  dup lit, throw end-cmd scmd  THEN
    THEN  throw ; IS >throw

previous definitions


\ client side timing

: ack-size ( -- )  1 j^ acks +!  ticks j^ lastb-ticks ! ;
: ack-first ( -- )
    j^ lastb-ticks @ ?dup-IF  j^ firstb-ticks @ - j^ delta-ticks +!  THEN
    ticks j^ firstb-ticks !  j^ lastb-ticks off ;

: ack-timing ( n -- )  ratex( dup 3 and s" .[+(" drop + c@ emit )
    b2b-toggle# and  IF  ack-first  ELSE  ack-size  THEN ;

: .rate ( n -- n ) dup . ." rate" cr ;
: .eff ( n -- n ) dup . ." eff" cr ;
also net2o-base
: >rate ( -- )  j^ delta-ticks 2@ 0= swap 0= or ?EXIT
    ticks dup j^ burst-ticks !@ dup IF
	- rate( .eff ) >r
	j^ delta-ticks @ tick-init 1+ j^ acks @ */
	rate( .rate ) lit, r> lit, set-rate
    ELSE
	2drop
    THEN
    j^ delta-ticks off  j^ acks off ;

: net2o:acktime ( -- )
    ticks dest-addr @
    timing( 2dup F . F . ." acktime" F cr )
    lit, lit, ack-addrtime ;

\ client side acknowledge

: net2o:ackrange ( -- )
    j^ data-ack $@ dup IF
	over 2@ drop >r + 2 cells - 2@ + r> tuck - swap lit, lit, ack-range
    ELSE  2drop  THEN ;
: net2o:gen-resend ( -- )
    inbuf 1+ c@ invert resend-toggle# and lit, ack-resend ;
: net2o:genack ( -- )
    net2o:gen-resend  net2o:acktime  >rate  net2o:ackrange ;
: net2o:sendack ( -- )
    end-cmd  scmd ;

: receive-flag ( -- flag )  inbuf 1+ c@ resend-toggle# and 0<> ;
: data-ackbit ( flag -- addr )
    IF  data-ackbits1  ELSE  data-ackbits0  THEN ;
: data-firstack# ( flag -- addr )
    IF  data-firstack0#  ELSE  data-firstack1#  THEN ;
: net2o:do-resend ( -- )
    j^ data-rmap $@ drop { dmap }
    dest-addr @ dmap dest-vaddr @ - addr>bits
    dmap receive-flag data-ackbit @ over 1- 2/ 2/ 2/ 1+ resend( 2dup dump )
    dmap receive-flag data-firstack# @ +DO  dup I + c@ $FF <> IF
	    dup I + c@ $FF xor
	    I chunk-p2 3 + lshift dmap dest-vaddr @ +
	    lit, lit, resend-mask  LEAVE
	ELSE
	    I dmap receive-flag data-firstack# !
	THEN
    LOOP  2drop ;

: restart-transfer ( -- )
    cmd:
    0 j^ file-state $@ bounds +DO
	I fs-size @ I fs-seek @ u> IF
	    ." restart <" dup 0 .r ." >: " I fs-seek ? F cr
	    I fs-seek @ I fs-size @ over - swap lit, lit, dup lit,
	    slurp-tracked-block
	THEN  1+
    file-state-struct +LOOP  drop
    send-chunks cmd; ;

: rewind-transfer ( -- )
    j^ expected @ negate j^ total +!
    j^ expected off  j^ received off
    rewind  restart-transfer
    j^ total @ 0<= IF  ." Chunk transfer done!" F cr  EXIT  THEN
;

: expected? ( -- )  maxdata j^ received +!
    j^ received @ j^ expected @ tuck u>= and IF
	." Block transfer done!" F cr
	save-blocks  rewind-transfer
    THEN ;

: received! ( -- )
    dest-addr @ inbuf body-size j^ data-ack del-range
\   ^^^ legacy code!!!
    j^ data-rmap $@ drop >r
    dest-addr @ r@ dest-vaddr @ - addr>bits
    \ set bucket as received in current polarity bitmap
    r@ receive-flag data-ackbit @ over +bit@
    r> swap >r >r
    dup r@ data-lastack# @ > IF
	\ if we are at head, fill other polarity with 1s
	dup r@ data-lastack# !@
	r@ receive-flag 0= data-ackbit @ -rot
	+DO  dup I 1+ +bit  LOOP
    ELSE
	\ otherwise, set only this specific bucket
	r@ receive-flag 0= data-ackbit @ over +bit
    THEN
    drop rdrop r> 0= IF  expected?  THEN ;
    
: net2o:do-ack ( -- )
    cmdbuf @ >r
    received!
    inbuf 1+ c@ acks# and
    dup j^ ack-receive !@ xor >r
    r@ resend-toggle# and IF  net2o:do-resend  THEN
    r@ ack-toggle# and IF  net2o:genack  THEN
    r> ack-timing
    r> cmdbuf @ <> IF  net2o:sendack  THEN ;
' net2o:do-ack IS do-ack

previous
