\ file states

\ Copyright (C) 2010-2014   Bernd Paysan

\ This program is free software: you can redistribute it and/or modify
\ it under the terms of the GNU Affero General Public License as published by
\ the Free Software Foundation, either version 3 of the License, or
\ (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU Affero General Public License for more details.

\ You should have received a copy of the GNU Affero General Public License
\ along with this program.  If not, see <http://www.gnu.org/licenses/>.

Sema file-sema

Variable net2o-path
pad $400 get-dir net2o-path $!

cmd-class class
    64field: fs-size
    64field: fs-seek
    64field: fs-seekto
    64field: fs-limit
    64field: fs-time
    field: fs-fid
    field: fs-path
    field: fs-id
    field: term-w
    field: term-h
    field: fs-inbuf
    field: fs-outbuf
    field: fs-termtask
    method fs-read
    method fs-write
    method fs-open
    method fs-close
    method fs-poll
end-class fs-class

Variable fs-table

: >seek ( size 64to 64seek -- size' )
    64dup 64>d fs-fid @ reposition-file throw 64- 64>n umin ;

: fs-timestamp! ( mtime fileno -- ) >r
    [IFDEF] android  rdrop 64drop
    [ELSE]  \ ." Set time: " r@ . 64dup 64>d d. cr
	64>d 2dup statbuf ntime!
	statbuf 2 cells + ntime!
	r> statbuf futimens ?ior [THEN] ;
: fs-size! ( 64size -- )
    64dup fs-size 64! fs-limit 64!
    64#0 fs-seek 64! 64#0 fs-seekto 64! 64#0 fs-time 64! ;

:noname ( addr u -- n )
    fs-limit 64@ fs-seekto 64@ >seek
    fs-fid @ read-file throw
    dup n>64 fs-seekto 64+!
; fs-class to fs-read
:noname ( addr u -- n )
    fs-limit 64@ fs-size 64@ 64umin
    fs-seek 64@ >seek
    tuck fs-fid @ write-file throw
    dup n>64 fs-seek 64+!
; fs-class to fs-write
:noname ( -- )
    fs-fid @ 0= ?EXIT
    fs-time 64@ 64dup 64-0= IF  64drop
    ELSE
	fs-fid @ flush-file throw
	fs-fid @ fileno fs-timestamp!
    THEN
    fs-fid @ close-file throw  fs-fid off
; fs-class to fs-close
:noname ( -- size )
    fs-fid @ file-size throw d>64
; fs-class to fs-poll
:noname ( addr u mode -- ) fs-close 64>n
    msg( dup 2over ." open file: " type ."  with mode " . cr )
    >r 2dup absolut-path?  !!abs-path!!
    net2o-path open-path-file throw fs-path $! fs-fid !
    r@ r/o <> IF  0 fs-fid !@ close-file throw
	fs-path $@ r@ open-file throw fs-fid  !  THEN  rdrop
    fs-poll fs-size!
; fs-class to fs-open

\ subclassing for other sorts of files

fs-class class
end-class socket-class

:noname ( addr u port -- ) fs-close 64>n
    msg( dup 2over ." open socket: " type ."  with port " . cr )
    open-socket fs-fid ! 64#0 fs-size! ; socket-class to fs-open
:noname ( -- size )
    fs-fid @ fileno check_read dup 0< IF  -512 + throw  THEN
    n>64 fs-size 64@ 64+ ; socket-class to fs-poll

fs-class class
end-class termclient-class

:noname ( addr u -- u ) tuck type ; termclient-class to fs-write
:noname ( addr u -- u ) 0 -rot bounds ?DO
	key? 0= ?LEAVE  key I c! 1+  LOOP ; termclient-class to fs-read
:noname ( addr u 64n -- ) 64drop 2drop ; termclient-class to fs-open
:noname ( -- ) ; termclient-class to fs-close

termclient-class class
end-class termserver-class

Variable termserver-tasks
User termfile

: ts-type ( addr u -- ) termfile @ .fs-outbuf $+! ;
: ts-emit ( c -- ) termfile @ .fs-outbuf c$+! ;
: ts-form ( -- w h ) termfile @ >o term-w @ term-h @ o> ;
: ts-key? ( -- flag ) termfile @ .fs-inbuf $@len 0<> ;
: ts-key ( -- key )
    BEGIN  ts-key? 0=  WHILE  stop  REPEAT
    termfile @ >o fs-inbuf $@ drop c@ fs-inbuf 0 1 $del o> ;

' ts-type ' ts-emit what's cr ' ts-form output: termserver-out
op-vector @
what's at-xy what's at-deltaxy what's page what's attr!
termserver-out
IS attr! IS page IS at-deltaxy IS at-xy
op-vector !
' ts-key  ' ts-key? input: termserver-in

1 Constant file-permit#
2 Constant socket-permit#
4 Constant ts-permit#
8 Constant tc-permit#
file-permit# Value fs-class-permit \ by default permit only files

: >termserver-io ( -- )
    [: up@ { w^ t } t cell termserver-tasks $+! ;] file-sema c-section
    ts-permit# fs-class-permit or to fs-class-permit ;

event: ->termfile ( o -- ) dup termfile ! >o form term-w ! term-h ! o>
    termserver-in termserver-out ;
event: ->termclose ( -- ) termfile off  default-in default-out ;

:noname ( addr u -- u ) tuck fs-inbuf $+! ; termserver-class to fs-write
:noname ( addr u -- u ) fs-outbuf $@len umin >r
    fs-outbuf $@ r@ umin rot swap move
    fs-outbuf 0 r@ $del r> ; termserver-class to fs-read
:noname ( addr u 64n -- )  64drop 2drop
    [: termserver-tasks $@ 0= !!no-termserver!!
	@ termserver-tasks 0 cell $del dup fs-termtask !
	<event o elit, ->termfile event>
    ;] file-sema c-section
; termserver-class to fs-open
:noname ( -- )
    [: fs-termtask @ ?dup-IF
	    <event ->termclose event>
	    fs-termtask cell termserver-tasks $+! fs-termtask off
	THEN ;] file-sema c-section
; termserver-class to fs-close

Create file-classes
' fs-class ,
' socket-class ,
' termclient-class ,
' termserver-class ,

here file-classes - cell/ Constant file-classes#

: fs-class! ( n -- )
    dup file-classes# u>= !!fileclass!!
    1 over lshift fs-class-permit and 0= !!fileclass!!
    cells file-classes + @ o cell- ! ;

\ id handling

: id>addr ( id -- addr remainder )
    >r file-state $@ r> cells /string >r dup IF  @  THEN r> ;
: id>addr? ( id -- addr )
    id>addr cell < !!fileid!! ;
: new>file ( id -- )
    [: fs-class new { w^ fsp } fsp cell file-state $+!
      fsp @ >o fs-id !
      fs-table @ token-table ! 64#-1 fs-limit 64! o> ;]
    filestate-lock c-section ;

: lastfile@ ( -- fs-state ) file-state $@ + cell- @ ;
: state-addr ( id -- addr )
    dup >r id>addr dup 0< !!gap!!
    0= IF  drop r@ new>file lastfile@  THEN  rdrop ;

\ state handling

: dest-top! ( addr -- )
    \ dest-tail @ dest-size @ + umin
    dup dup dest-top @ U+DO
	data-ackbits @ I I' fix-size dup { len }
	chunk-p2 rshift swap chunk-p2 rshift swap bit-erase
    len +LOOP  dest-top ! ;

: dest-back! ( addr -- )
    dup dup dest-back @ U+DO
	data-ackbits @ I I' fix-size dup { len }
	chunk-p2 rshift swap chunk-p2 rshift swap bit-fill
    len +LOOP  dest-back ! ;

: size! ( 64 -- )
    64dup fs-size 64!  fs-limit 64umin! ;
: seek-off ( -- )
    64#0 fs-seekto 64! 64#0 fs-seek 64! ;
: seekto! ( 64 -- )
    fs-size 64@ 64umin fs-seekto 64umax! ;
: limit-min! ( 64 id -- )
    fs-size 64@ 64umin fs-limit 64! ;
: init-limit! ( 64 id -- )  state-addr .fs-limit 64! ;
: poll! ( 64 -- 64 )
    fs-limit 64! fs-poll 64dup size! ;

: file+ ( addr -- ) >r 1 r@ +!
    r@ @ id>addr nip 0<= IF  r@ off  THEN  rdrop ;

: fstates ( -- n )  file-state $@len cell/ ;

: fstate-off ( -- )  file-state @ 0= ?EXIT
    file-state $@ bounds ?DO  I @ .dispose  cell +LOOP
    file-state $off ;
: n2o:save-block ( id -- delta )
    rdata-back@ file( over data-rmap @ .dest-raddr @ -
    { os } ." file write: " 2 pick . os hex.
\    os addr>ts data-rmap @ .dest-cookies @ + over addr>ts xtype space
\    data-rmap @ .data-ackbits @ os addr>bits 2 pick addr>bits bittype space
    )
    rot id>addr? .fs-write dup /back file( dup hex. residualwrite @ hex. cr ) ;

\ careful: must follow exactpy the same logic as slurp (see below)
: n2o:spit ( -- ) fstates 0= ?EXIT
    [: +calc fstates 0 { states fails }
	BEGIN  rdata-back?  WHILE
		write-file# @ n2o:save-block
		IF 0 ELSE fails 1+ residualwrite off THEN to fails
		residualwrite @ 0= IF
		    write-file# file+ blocksize @ residualwrite !  THEN
	    fails states u>= UNTIL  THEN msg( ." Write end" cr ) +file ;]
    file-sema c-section ;

: save-to ( addr u n -- )  state-addr >o
    r/w create-file throw fs-fid ! o> ;

\ file status stuff

: n2o:get-stat ( -- mtime mod )
    fs-fid @ fileno statbuf fstat ?ior
    statbuf st_mtime ntime@ d>64
    statbuf st_mode l@ $FFF and ;

: n2o:track-mod ( mod fileno -- )
    [IFDEF] android 2drop
    [ELSE] swap fchmod ?ior [THEN] ;

: n2o:set-stat ( mtime mod -- )
    fs-fid @ fileno n2o:track-mod fs-time 64! ;

\ open/close a file - this needs *way more checking*! !!FIXME!!

User file-reg#

: n2o:close-file ( id -- )
    id>addr? .fs-close ;

: blocksizes! ( n -- )
    dup blocksize !
    file( ." file read: ======= " cr ." file write: ======= " cr )
    dup residualread !  residualwrite ! ;

: n2o:close-all ( -- )
    [: fstates 0 ?DO
	    I n2o:close-file
	LOOP  file-reg# off  fstate-off
	blocksize @ blocksizes!
	read-file# off  write-file# off ;] file-sema c-section ;

: n2o:open-file ( addr u mode id -- )
    state-addr .fs-open ;

\ read in from files

: n2o:slurp-block ( id -- delta )
    data-head@ file( over data-map @ .dest-raddr @ -
    >r ." file read: " rot dup . -rot r> hex. )
    rot id>addr? .fs-read dup /head file( dup hex. residualread @ hex. cr ) ;

\ careful: must follow exactpy the same loic as n2o:spit (see above)
: n2o:slurp ( -- head end-flag )
    data-head? 0= fstates 0= or IF  head@ 0  EXIT  THEN
    [: +calc fstates 0 { states fails }
	0 BEGIN  data-head?  WHILE
		read-file# @ n2o:slurp-block
		IF 0 ELSE fails 1+ residualread off THEN to fails
		residualread @ 0= IF
		    read-file# file+  blocksize @ residualread !  THEN
	    fails states u>= UNTIL  THEN msg( ." Read end" cr ) +file
	head@ fails states u>= ;]
    file-sema c-section file( dup IF  ." data end" cr  THEN ) ;
    
: n2o:track-seeks ( idbits xt -- ) { xt } ( i seeklen -- )
    8 cells 0 DO
	dup 1 and IF
	    I dup id>addr? >o fs-seek 64@ fs-seekto 64@ 64<> IF
		fs-seekto 64@ 64dup fs-seek 64! o>
		xt execute  ELSE  drop o>  THEN
	THEN  2/
    LOOP  drop ;

: n2o:track-all-seeks ( xt -- ) { xt } ( i seeklen -- )
    fstates 0 ?DO
	I dup id>addr? >o fs-seek 64@ fs-seekto 64@ 64<> IF
	    fs-seekto 64@ 64dup fs-seek 64! o>
	    xt execute  ELSE  drop o>  THEN
    LOOP ;

0 [IF]
Local Variables:
forth-local-words:
    (
     (("event:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     (("debug:" "field:" "2field:" "sffield:" "dffield:" "64field:") non-immediate (font-lock-type-face . 2)
      "[ \t\n]" t name (font-lock-variable-name-face . 3))
     ("[a-z\-0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("event:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]
