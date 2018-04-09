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

cmd-class class
    64value: fs-size
    64value: fs-seek
    64value: fs-seekto
    64value: fs-limit
    64value: fs-time
    field: fs-fid
    field: fs-path
    field: fs-id
    field: term-w
    field: term-h
    field: fs-inbuf
    field: fs-outbuf
    field: fs-termtask
    defer: file-xt     \ callback for operation completed
    field: fs-cryptkey \ for en/decrypting a file on the fly
    field: fs-rename+  \ temporary path for downloads
    method fs-read
    method fs-write
    method fs-open
    method fs-create
    method fs-close
    method fs-flush
    method fs-poll
    method fs-perm?
    method fs-get-stat
    method fs-set-stat
end-class fs-class

Variable fs-table

\ file events
: file:err ( -- )
    <err> ." invalid file-done xt" <default> ~~bt ;
: file:done ( -- )
    -1 parent .file-count +!
    .time ." download done: " fs-id ? fs-path $@ type cr ;
event: :>file-done ( file-o -- ) \ .file-xt ;
    >o action-of file-xt IF  file-xt  ELSE  file:err  THEN o> ;

\ id handling

: id>addr ( id -- addr remainder )
    >r file-state $@ r> cells /string >r dup IF  @  THEN r> ;
: id>addr? ( id -- addr )
    id>addr cell < !!fileid!! ;
: new>file ( id -- )
    [: fs-class new { w^ fsp } fsp cell file-state $+!
      o fsp @ >o parent! fs-id ! ['] file:done is file-xt
      fs-table @ token-table ! 64#-1 to fs-limit o> ;]
    filestate-sema c-section ;

: lastfile@ ( -- fs-state ) file-state $@ + cell- @ ;
: state-addr ( id -- addr )
    dup >r id>addr dup 0< !!gap!!
    0= IF  drop r@ new>file lastfile@  THEN  rdrop ;

cell 8 = [IF]
    ' noop alias 64>usat
[ELSE]
    : 64>usat ( 64 -- u ) 0<> or ;
[THEN]

: >seek ( size 64to 64seek -- size' )
    64dup 64>d fs-fid @ reposition-file throw 64- 64>usat umin ;
: >rename+ ( addr u -- )
    fs-rename+ $!
    <<# getpid 0 #s '+' hold #> fs-rename+ $+! #>>
    fs-rename+ $@ ;
: fs-timestamp! ( mtime fid -- ) fileno >r
    [IFDEF] android  rdrop 64drop
    [ELSE]  \ ." Set time: " r@ . 64dup 64>d d. cr
	64>d 2dup statbuf ntime!
	statbuf 2 cells + ntime!
	r> statbuf futimens ?ior [THEN] ;
: fs-size! ( 64size -- )
    64dup to fs-size to fs-limit
    64#0 to fs-seek 64#0 to fs-seekto 64#0 to fs-time ;

: fs:fs-read ( addr u -- u )
    fs-limit fs-seekto >seek
    fs-fid @ read-file throw
    dup n>64 +to fs-seekto
; ' fs:fs-read fs-class to fs-read
: fs:fs-write ( addr u -- u )
    dup 0= IF  nip  EXIT  THEN
    fs-limit fs-size 64umin
    64dup fs-seek 64u<= IF  64drop 2drop 0  EXIT  THEN
    fs-seek >seek
    tuck fs-fid @ write-file throw
    dup n>64 +to fs-seek  fs-seek to fs-seekto
    fs-size fs-seek 64= IF
	fs-flush
	<event o elit, :>file-done parent .wait-task @ event>
    THEN
; ' fs:fs-write fs-class to fs-write
: fs:fs-clear ( -- )
    64#0
    64dup to fs-limit  64dup to fs-seekto  64dup to fs-seek
    64dup to fs-size  to fs-time  fs-path $free  fs-rename+ $free
    ['] noop to file-xt ;
: fs:fs-flush ( -- )
    fs-fid @ flush-file throw
    \ write away all buffered stuff, so that setting the
    \ timestamp works
    fs-time 64-0<> IF
	fs-time fs-fid @ fs-timestamp!
    THEN
    fs-rename+ $@ dup IF
	fs-path $@ rename-file throw
	fs-rename+ $free
    ELSE  2drop  THEN
; ' fs:fs-flush fs-class to fs-flush
: fs:fs-close ( -- )
    fs-fid @ 0= ?EXIT
    fs-flush
    fs-fid @ close-file throw
    fs-fid off
    fs:fs-clear
; ' fs:fs-close fs-class to fs-close
:noname ( -- 64size )
    fs-fid @ file-size throw d>64
; fs-class to fs-poll
:noname ( addr u mode -- ) fs-close
    msg( dup 2over ." open file: " forth:type ."  with mode " . forth:cr )
    >r ?sane-file
    config:rootdirs$ open-path-file throw fs-path $! fs-fid !
    r@ r/o <> IF  0 fs-fid !@ close-file throw
	fs-path $@ r@ open-file throw fs-fid  !  THEN  rdrop
    fs-poll fs-size!
; fs-class to fs-open
:noname ( addr u -- )  fs-close
    msg( dup 2over ." create file: " forth:type forth:cr )
    ?sane-file
    2dup fs-path $! >rename+ r/w create-file throw fs-fid !
; fs-class to fs-create
:noname ( perm -- )
    perm%filename and 0= !!filename-perm!!
; fs-class to fs-perm?

\ access to encrypted hash files

: >file-hash ( addr u -- addrhash u )
    c:0key c:hash keyed-hash-out hash#128 2dup c:hash@ ;
: enchash ( -- addr u )
    sksig keyed-hash-out hash#128 + keysize move
    keyed-hash-out hash#256 >file-hash ;

fs-class class
end-class hashfs-class

: hashfs>file ( addrhash u1 -- addrfile u2 )
    c:key@ >r  keccak# fs-cryptkey $!len
    fs-cryptkey $@ drop c:key!
    msg( ." open hash: " 2dup 85type cr )
    keyed-hash-out hash#128 smove
    enchash hash>filename
    msg( ." open file: " 2dup type cr )
    2dup fs-path $!
    r> c:key! ;

:noname ( addr u mode -- )  fs-close
    >r hashfs>file r> open-file throw fs-fid ! fs-poll fs-size!
; hashfs-class to fs-open
:noname ( addr u -- )  fs-close
    hashfs>file >rename+ r/w create-file throw fs-fid !
; hashfs-class to fs-create
:noname ( perm -- )
    perm%filehash and 0= !!filehash-perm!!
; hashfs-class to fs-perm?
:noname ( addr u -- n )
    c:key@ >r
    over >r fs:fs-read
    fs-cryptkey $@ drop c:key!
    r> over c:decrypt
    r> c:key! ; hashfs-class to fs-read
:noname ( addr u -- n )
    dup 0= IF  nip  EXIT  THEN
    c:key@ >r  fs-cryptkey $@ drop c:key!
    save-mem 2dup c:encrypt over >r fs:fs-write r> free throw
    r> c:key! ; hashfs-class to fs-write
:noname ( -- )
    fs:fs-close
    fs-cryptkey $free ; hashfs-class to fs-close

\ subclassing for other sorts of files

fs-class class
end-class socket-class

:noname ( addr u port -- ) fs-close 64>n
    msg( dup 2over ." open socket: " type ."  with port " . cr )
    open-socket fs-fid ! 64#0 fs-size! ;
dup socket-class to fs-open  socket-class to fs-create
:noname ( -- size )
    fs-fid @ fileno check_read dup 0< IF  -512 + throw  THEN
    n>64 fs-size 64+ ; socket-class to fs-poll
:noname ( perm -- )
    perm%socket and 0= !!socket-perm!!
; socket-class to fs-perm?

fs-class class
end-class termclient-class

:noname ( addr u -- u ) tuck type ; termclient-class to fs-write
:noname ( addr u -- u ) 0 -rot bounds ?DO
	key? 0= ?LEAVE  key I c! 1+  LOOP ; termclient-class to fs-read
:noname ( addr u 64n -- ) 64drop 2drop ;
dup termclient-class to fs-open  termclient-class to fs-create
:noname ( -- ) ; termclient-class to fs-close
:noname ( perm -- )
    perm%terminal and 0= !!terminal-perm!!
; termclient-class to fs-perm?

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
what's name
op-vector @
what's at-xy what's at-deltaxy what's page what's attr!
[IFDEF] notrace notrace [THEN]
termserver-out
IS attr! IS page IS at-deltaxy IS at-xy
op-vector !
is name
' ts-key  ' ts-key? input: termserver-in
[IFDEF] traceall traceall [THEN]

: >termserver-io ( -- )
    [: up@ { w^ t } t cell termserver-tasks $+! ;] file-sema c-section ;

event: :>termfile ( o -- ) dup termfile ! >o form term-w ! term-h ! o>
    termserver-in termserver-out ;
event: :>termclose ( -- ) termfile off  default-in default-out ;

:noname ( addr u -- u )
    dup 0= IF  nip  EXIT  THEN
    fs-limit 64>n fs-inbuf $@len - min  tuck fs-inbuf $+!
    fs-size fs-inbuf $@len u>64 64= fs-inbuf $@len 0<> and IF
	<event o elit, :>file-done parent .wait-task @ event>
    THEN ; termserver-class to fs-write
:noname ( addr u -- u ) fs-outbuf $@len umin >r
    fs-outbuf $@ r@ umin rot swap move
    fs-outbuf 0 r@ $del r> ; termserver-class to fs-read
:noname ( addr u 64n -- )  64drop 2drop
    [: termserver-tasks $@ 0= !!no-termserver!!
	@ termserver-tasks 0 cell $del dup fs-termtask !
	<event o elit, :>termfile event>
    ;] file-sema c-section
; dup termserver-class to fs-open  termserver-class to fs-create
:noname ( -- )
    [: fs-termtask @ ?dup-IF
	    <event :>termclose event>
	    fs-termtask cell termserver-tasks $+! fs-termtask off
	THEN ;] file-sema c-section
; termserver-class to fs-close
:noname ( perm -- )
    perm%termserver and 0= !!termserver-perm!!
; termserver-class to fs-perm?

Create file-classes
fs-class ,
hashfs-class ,
socket-class ,
termclient-class ,
termserver-class ,

here file-classes - cell/
$10 over - cells allot

Value file-classes#

: fs-class! ( n -- )
    dup file-classes# u>= !!fileclass!!
    cells file-classes + @ o cell- ! ;

: +file-classes ( addr -- )
    file-classes file-classes# dup 1+ to file-classes# cells + ! ;

\ state handling

scope{ mapc

: dest-top! ( addr -- )
    dup dup dest-top U+DO
	data-ackbits @ I I' fix-size dup { len }
	chunk-p2 rshift swap chunk-p2 rshift swap bit-erase
    len +LOOP  to dest-top ;

: ackbits-erase ( oldback newback -- )
    swap U+DO
	data-ackbits @ I I' fix-size dup { len }
	chunk-p2 rshift swap chunk-p2 rshift swap bit-fill
    len +LOOP ;

}scope

: size! ( 64 -- )
    64dup to fs-size  addr fs-limit 64umin! ;
: seek-off ( -- )
    64#0 to fs-seekto 64#0 to fs-seek ;
: seekto! ( 64 -- )
    fs-size 64umin addr fs-seekto 64umax! ;
: limit-min! ( 64 id -- )
    fs-size 64umin to fs-limit ;
: init-limit! ( 64 id -- )  state-addr >o to fs-limit o> ;
: poll! ( 64 -- 64 )
    to fs-limit fs-poll 64dup size! ;

: file+ ( addr -- ) >r 1 r@ +!
    r@ @ id>addr nip 0<= IF  r@ off  THEN  rdrop ;

: fstates ( -- n )  file-state $@len cell/ ;

: fstate-off ( -- )  file-state @ 0= ?EXIT
    file-state $@ bounds ?DO  I @ .dispose  cell +LOOP
    file-state $free ;
in net2o : save-block ( back tail id -- delta ) { id -- delta }
    data-rmap with mapc fix-size raddr+ endwith residualwrite @ umin
    file( over data-rmap .mapc:dest-raddr - >r
    id id>addr? .fs-seek #10 64rshift 64>n >r )
    id id>addr? .fs-write
    file( dup IF ." file write: "
    id . r> hex. r> #10 rshift hex. dup hex. residualwrite @ hex. forth:cr
    ELSE  rdrop rdrop  THEN ) ;

\ careful: must follow exactly the same logic as slurp (see below)

in net2o : spit { back tail -- newback }
    back tail back u<= ?EXIT fstates 0= ?EXIT drop
    slurp( ." spit: " tail rdata-back@ drop data-rmap with mapc dest-raddr - endwith hex.
    write-file# ? residualwrite @ hex. forth:cr ) back tail
    [: +calc fstates 0 { back tail states fails }
	BEGIN  tail back u>  WHILE
		back tail write-file# @ net2o:save-block
		>blockalign dup negate residualwrite +!  dup +to back
		IF 0 ELSE fails 1+ residualwrite off THEN to fails
		residualwrite @ 0= IF
		    write-file# file+ blocksize @ residualwrite !  THEN
	    fails states u>= UNTIL
	THEN
	msg( ." Write end" cr ) +file
	back  fails states u>= IF   >maxalign  THEN  \ if all files are done, align
    ;] file-sema c-section
    slurp( ."  left: " tail rdata-back@ drop data-rmap with mapc dest-raddr - endwith hex.
    write-file# ? residualwrite @ hex. forth:cr ) ;

: save-to ( addr u n -- )  state-addr >o  fs-create o> ;
: save-to# ( addr u n -- )  state-addr >o  1 fs-class!  fs-create o> ;

\ file status stuff

in net2o : get-stat ( -- mtime mod )
    fs-fid @ fileno statbuf fstat ?ior
    statbuf st_mtime ntime@ d>64
    statbuf st_mode [ sizeof st_mode 2 = ] [IF] w@ [ELSE] l@ [THEN] $FFF and ;
' net2o:get-stat fs-class to fs-get-stat
' net2o:get-stat hashfs-class to fs-get-stat

in net2o : track-mod ( mod fileno -- )
    [IFDEF] android 2drop
    [ELSE] swap fchmod ?ior [THEN] ;

in net2o : set-stat ( mtime mod -- )
    fs-fid @ fileno net2o:track-mod to fs-time ;
' net2o:set-stat fs-class to fs-set-stat
' net2o:set-stat hashfs-class to fs-set-stat

\ open/close a file - this needs *way more checking*! !!FIXME!!

in net2o : close-file ( id -- )
    id>addr? .fs-close ;

: blocksizes! ( n -- )
    dup blocksize !
    file( ." file read: ======= " dup . forth:cr
    ." file write: ======= " dup . forth:cr )
    dup residualread !  residualwrite ! ;

in net2o : close-all ( -- )
    msg( ." Closing all files" forth:cr )
    [:  fstates 0 ?DO  I net2o:close-file  LOOP
	file-reg# off  fstate-off  blocksize @ blocksizes!
	read-file# off  write-file# off ;] file-sema c-section ;

in net2o : open-file ( addr u mode id -- )
    state-addr .fs-open ;

\ read in from files

in net2o : slurp-block ( id -- delta )
    data-head@ file( over data-map .mapc:dest-raddr -
    >r ." file read: " 2 pick .
    2 pick id>addr? .fs-seek #10 64rshift $64. r> #10 rshift hex. )
    rot id>addr? .fs-read dup /head
    file( dup hex. residualread @ hex. forth:cr ) ;

\ careful: must follow exactpy the same logic as net2o:spit (see above)
in net2o : slurp ( -- head end-flag )
    data-head? 0= fstates 0= or  IF  head@ 0  EXIT  THEN
    slurp( ." slurp: " data-head@ drop data-map with mapc dest-raddr - endwith hex.
    read-file# ? residualread @ hex. forth:cr )
    [: +calc fstates 0 { states fails }
	0 BEGIN  data-head?  WHILE
		read-file# @ net2o:slurp-block
		IF 0 ELSE fails 1+ residualread off THEN to fails
		residualread @ 0= IF
		    read-file# file+  blocksize @ residualread !  THEN
	    fails states u>= UNTIL
	THEN  +file
	fails states u>= dup IF  max/head  THEN \ if all files are done, align
	head@ swap
	msg( ." Read end: " over hex. forth:cr ) ;]
    file-sema c-section
    slurp( ."  left: " data-head@ drop data-map with mapc dest-raddr - endwith hex.
    read-file# ? residualread @ hex. forth:cr )

    file( dup IF  ." data end: " over hex. dup forth:. forth:cr  THEN ) ;
    
in net2o : track-seeks ( idbits xt -- ) \ xt: ( i seeklen -- )
    [: { xt } 8 cells 0 DO
	    dup 1 and IF
		I dup id>addr? >o fs-seek fs-seekto 64<> IF
		    fs-seekto 64dup to fs-seek o>
		    xt execute  ELSE  drop o>  THEN
	    THEN  2/
	LOOP  drop ;] file-sema c-section ;

in net2o : track-all-seeks ( xt -- ) \ xt: ( i seeklen -- )
    [: { xt } fstates 0 ?DO
	    I dup id>addr? >o fs-seek fs-seekto 64<> IF
		fs-seekto 64dup to fs-seek o>
		xt execute  ELSE  drop o>  THEN
	LOOP ;] file-sema c-section ;

\ permission checks

Create >file-perm
perm%filerd w, perm%filerd perm%filewr or w, perm%filewr w,
DOES>  + w@ ;
: ?rd-perm ( n -- ) perm%filerd and 0<> !!filerd-perm!! ;
: ?wr-perm ( n -- ) perm%filewr and 0<> !!filewr-perm!! ;
: ?rw-perm ( n perm -- )
    >r >file-perm r> invert and dup ?rd-perm ?wr-perm ;

0 [IF]
Local Variables:
forth-local-words:
    (
     (("event:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     (("debug:" "field:" "2field:" "sffield:" "dffield:" "64field:" "64value:") non-immediate (font-lock-type-face . 2)
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
