\ file states

\ Copyright © 2010-2014   Bernd Paysan

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
    parent >o -1 file-count +!@ 1 = IF
	wait-task @ ?dup-IF
	    wake# over 's @ 1+ (restart)
	THEN
    THEN o>
    [: .time ." download done: " fs-id ? fs-path $@ type cr ;] do-debug ;
: parent-file-done ( -- )
    o [{: xo :}h1
	xo >o action-of file-xt IF  file-xt  ELSE  file:err  THEN
	0 is file-xt o> ;]
    parent .wait-task-event ;
\ id handling

: id>addr ( id -- addr remainder )
    [: >r file-state $@ r> cells /string >r dup IF  @  THEN r> ;]
    filestate-sema c-section ;
: id>addr? ( id -- addr )
    id>addr cell < !!fileid!! ;
: new>file ( id -- )
    [: fs-table fs-class new-tok { w^ fsp } fsp cell file-state $+!
      o fsp @ >o parent! fs-id ! ['] file:done is file-xt
      64#-1 to fs-limit o> ;]
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

Vocabulary fs

in fs : fs-read ( addr u -- u )
    fs-limit fs-seekto >seek
    fs-fid @ read-file throw
    dup n>64 +to fs-seekto
; ' fs:fs-read fs-class is fs-read
in fs : fs-write ( addr u -- u )
    dup 0= IF  nip  EXIT  THEN
    fs-limit fs-size 64umin
    64dup fs-seek 64u<= IF  64drop 2drop 0  EXIT  THEN
    fs-seek >seek
    tuck fs-fid @ write-file throw
    dup n>64 +to fs-seek  fs-seek to fs-seekto
    fs-size fs-seek 64= IF
	fs-flush parent-file-done
    THEN
; ' fs:fs-write fs-class is fs-write
in fs : fs-clear ( -- )
    64#0
    64dup to fs-limit  64dup to fs-seekto  64dup to fs-seek
    64dup to fs-size  to fs-time  fs-path $free  fs-rename+ $free
    ['] noop is file-xt ;
in fs : fs-flush ( -- )
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
; ' fs:fs-flush fs-class is fs-flush
in fs : fs-close ( -- )
    fs-fid @ 0= ?EXIT
    fs-flush
    fs-fid @ close-file throw
    fs-fid off
    fs:fs-clear
; ' fs:fs-close fs-class is fs-close
fs-class :method fs-poll ( -- 64size )
    fs-fid @ file-size throw d>64
;
fs-class :method fs-open ( addr u mode -- ) fs-close
    msg( dup 2over ." open file: " forth:type ."  with mode " . forth:cr )
    >r ?sane-file
    config:rootdirs$ open-path-file throw fs-path $! fs-fid !
    r@ r/o <> IF  0 fs-fid !@ close-file throw
	fs-path $@ r@ open-file throw fs-fid  !  THEN  rdrop
    fs-poll fs-size!
;
fs-class :method fs-create ( addr u -- )  fs-close
    msg( 2dup ." create file: " forth:type forth:cr )
    ?sane-file
    2dup fs-path $! >rename+ r/w create-file throw fs-fid !
;
fs-class :method fs-perm? ( perm -- )
    perm%filename and 0= !!filename-perm!!
;

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

hashfs-class :method fs-open ( addr u mode -- )  fs-close
    >r hashfs>file r> open-file throw fs-fid ! fs-poll fs-size!
;
hashfs-class :method fs-create ( addr u -- )  fs-close
    hashfs>file >rename+ r/w create-file throw fs-fid !
;
hashfs-class :method fs-perm? ( perm -- )
    perm%filehash and 0= !!filehash-perm!!
;
hashfs-class :method fs-read ( addr u -- n )
    c:key@ >r
    over >r fs:fs-read
    fs-cryptkey $@ drop c:key!
    r> over c:decrypt
    r> c:key! ;
hashfs-class :method fs-write ( addr u -- n )
    dup 0= IF  nip  EXIT  THEN
    $make { w^ file-pad$ } file-pad$ $@
    c:key@ >r  fs-cryptkey $@ drop c:key!
    2dup c:encrypt fs:fs-write file-pad$ $free
    r> c:key! ;
hashfs-class :method fs-close ( -- )
    fs:fs-close
    fs-cryptkey $free ;

\ subclassing for other sorts of files

fs-class class
end-class socket-class

socket-class :method fs-create ( addr u port -- ) fs-close 64>n
    msg( dup 2over ." open socket: " type ."  with port " . cr )
    open-socket fs-fid ! 64#0 fs-size! ;
latestxt socket-class is fs-open
socket-class :method fs-poll ( -- size )
    fs-fid @ fileno check_read dup 0< IF  -512 + throw  THEN
    n>64 fs-size 64+ ;
socket-class :method fs-perm? ( perm -- )
    perm%socket and 0= !!socket-perm!!
;

fs-class class
end-class termclient-class

termclient-class :method fs-write ( addr u -- u ) tuck type ;
termclient-class :method fs-read ( addr u -- u ) 0 -rot bounds ?DO
	key? 0= ?LEAVE  key I c! 1+  LOOP ;
termclient-class :method fs-create ( addr u 64n -- ) 64drop 2drop ;
latestxt termclient-class is fs-open
termclient-class :method fs-close ( -- ) ;
termclient-class :method fs-perm? ( perm -- )
    perm%terminal and 0= !!terminal-perm!!
;

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

' ts-type ' ts-emit action-of cr ' ts-form output: termserver-out
action-of parse-name
op-vector @
action-of at-xy action-of at-deltaxy action-of page action-of attr!
[IFDEF] notrace get-recognizers n>r notrace [THEN]
termserver-out
IS attr! IS page IS at-deltaxy IS at-xy
op-vector !
is parse-name
' ts-key  ' ts-key? input: termserver-in
[IFDEF] notrace nr> set-recognizers [THEN]

: >termserver-io ( -- )
    [: up@ { w^ t } t cell termserver-tasks $+! ;] file-sema c-section ;

: ev-termfile ( o -- ) dup termfile ! >o form term-w ! term-h ! o>
    termserver-in termserver-out ;
: ev-termclose ( -- ) termfile off  default-in default-out ;

termserver-class :method fs-write ( addr u -- u )
    dup 0= IF  nip  EXIT  THEN
    fs-limit 64>n fs-inbuf $@len - min  tuck fs-inbuf $+!
    fs-size fs-inbuf $@len u>64 64= fs-inbuf $@len 0<> and IF
	parent-file-done
    THEN ;
termserver-class :method fs-read ( addr u -- u ) fs-outbuf $@len umin >r
    fs-outbuf $@ r@ umin rot swap move
    fs-outbuf 0 r@ $del r> ;
termserver-class :method fs-create ( addr u 64n -- )  64drop 2drop
    [: termserver-tasks $@ 0= !!no-termserver!!
	@ termserver-tasks 0 cell $del dup fs-termtask !
	o [{: xo :}h1 xo ev-termfile ;] swap send-event
    ;] file-sema c-section
;
latestxt termserver-class is fs-open
termserver-class :method fs-close ( -- )
    [: fs-termtask @ ?dup-IF
	    ['] ev-termclose swap send-event
	    fs-termtask cell termserver-tasks $+! fs-termtask off
	THEN ;] file-sema c-section
;
termserver-class :method fs-perm? ( perm -- )
    perm%termserver and 0= !!termserver-perm!!
;

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
	data-ackbits @ I delta-I fix-range dup { len }
	chunk-p2 rshift swap chunk-p2 rshift swap bit-erase
    len +LOOP  to dest-top ;

: ackbits-erase ( oldback newback -- )
    swap U+DO
	data-ackbits @ I delta-I fix-range dup { len }
	chunk-p2 rshift swap chunk-p2 rshift swap bit-fill
    len +LOOP ;

}scope

: size! ( 64 -- )
    64dup to fs-size  fs-limit 64umin to fs-limit ;
: seek-off ( -- )
    64#0 to fs-seekto 64#0 to fs-seek ;
: seekto! ( 64 -- )
    fs-size 64umin  fs-seekto 64umax to fs-seekto ;
: limit-min! ( 64 id -- )
    fs-size 64umin to fs-limit ;
: init-limit! ( 64 id -- )  state-addr >o to fs-limit o> ;
: poll! ( 64 -- 64 )
    to fs-limit fs-poll 64dup size! ;

: file+ ( addr -- ) >r 1 r@ +!
    r@ @ id>addr nip 0<= IF  r@ off  THEN  rdrop ;

: fstates ( -- n )  file-state $@len cell/ ;

Variable f-rid -1 f-rid !
Variable f-ramount
Variable f-wid -1 f-wid !
Variable f-wamount

[IFDEF] old-spit
    in net2o : save-block ( back tail id -- delta ) { id -- delta }
	data-rmap with mapc fix-size raddr+ endwith residualwrite @ umin
	id id>addr? .fs-write
	file1( id f-wid @ = IF  dup f-wamount +!
	ELSE  f-wid @ 0>= f-wamount @ 0> and IF
	    ." spit: " f-wid @ . f-wamount @ h. cr  THEN
	id f-wid ! dup f-wamount !  THEN )
	>blockalign dup negate residualwrite +! ;
[THEN]

in net2o : save-block ( back tail id len -- delta ) { id len -- delta }
    slurp( ." spit: " over h. dup h. id h. len h. )
    id $FF = IF  swap - len umin \ only alignment
    ELSE
	data-rmap with mapc fix-size raddr+ endwith
	len umin
	id id>addr? .fs-write
    THEN
    len over - residualwrite ! ;

: .spit ( -- )
    spit#$ $@ bounds ?DO  I c@ h. I 1+ p2@+ >r x64. cr r> I - +LOOP ;

in net2o : spit [: { back tail | spitbuf# -- newback } +calc slurp( .spit )
	spit#$ $@ bounds ?DO
	    back tail I count swap p2@+ I - { +I }
	    64>n residualwrite @ umin
	    net2o:save-block slurp( ." => " dup h. forth:cr )
	    dup +to back
	    0<> residualwrite @ and IF  0 to +I  ELSE
		blocksize @ residualwrite !  THEN
	    +I +to spitbuf#
	    back tail u>= ?LEAVE
	+I +LOOP
	spit#$ 0 spitbuf# $del
	back ;] file-sema c-section +file ;

\ careful: must follow exactly the same logic as slurp (see below)

[IFDEF] old-spit
    in net2o : spit { back tail -- newback }
	back tail back u<= ?EXIT fstates 0= ?EXIT drop
	slurp( ." spit: " tail rdata-back@ drop data-rmap with mapc dest-raddr - endwith h.
	write-file# ? residualwrite @ h. forth:cr ) back tail
	[: +calc fstates 0 { back tail states fails }
	    BEGIN  tail back u>  WHILE
		    back tail write-file# @ net2o:save-block dup +to back
		    IF 0 ELSE fails 1+ residualwrite off THEN to fails
		    residualwrite @ 0= IF
			write-file# file+ blocksize @ residualwrite !  THEN
		fails states u>= UNTIL
	    THEN
	msg( ." Write end" cr ) +file
	    back  fails states u>= IF  >maxalign  THEN  \ if all files are done, align
	;] file-sema c-section
	slurp( .spit ) spit#$ $free
	slurp( ."  left: " tail rdata-back@ drop data-rmap with mapc dest-raddr - endwith h.
	write-file# ? residualwrite @ h. forth:cr ) ;
[THEN]

: save-to ( addr u n -- )  state-addr .fs-create ;
: save-to# ( addr u n -- )  state-addr >o  1 fs-class!  fs-create o> ;

\ file status stuff

: fstates-free ( -- )
     file-state $@ bounds ?DO  I @ .dispose  cell +LOOP ;
: fstate-free ( -- )  file-state @ 0= ?EXIT
    [: fstates-free file-state $free ;] file-sema c-section ;

scope{ net2o

: get-stat ( -- mtime mod )
    fs-fid @ fileno statbuf fstat ?ior
    statbuf st_mtime ntime@ d>64
    statbuf st_mode [ sizeof st_mode 2 = ] [IF] w@ [ELSE] l@ [THEN] $FFF and ;
' net2o:get-stat fs-class is fs-get-stat
' net2o:get-stat hashfs-class is fs-get-stat

8 base !@
: track-mod ( mod fileno -- )
    [IFDEF] android 2drop
    [ELSE] swap dup 0= 644 and or 400 or fchmod ?ior [THEN] ;
base !

: set-stat ( mtime mod -- )
    fs-fid @ fileno net2o:track-mod to fs-time ;
' net2o:set-stat fs-class is fs-set-stat
' net2o:set-stat hashfs-class is fs-set-stat

\ open/close a file - this needs *way more checking*! !!FIXME!!

: close-file ( id -- )
    id>addr? .fs-close ;

: blocksizes! ( n -- )
    dup blocksize !
    file( ." file read: ======= " dup . forth:cr
    ." file write: ======= " dup . forth:cr )
    dup residualwrite !
    residualread ! ;

: close-all ( -- )
    msg( ." Closing all files" forth:cr )
    fstates 0 ?DO  I net2o:close-file  LOOP
    file-reg# off  fstate-free
    blocksize @ blocksizes!
    read-file# off  write-file# off ;

: open-file ( addr u mode id -- )
    state-addr .fs-open ;

\ read in from files

: >slurp ( n id -- )
    slurp#$ c$+! u>64 slurp#$ p2$+! ;
: >salign ( n -- )
    ?dup-IF  $FF >slurp  THEN ;
: slurp-block { id -- delta }
    data-head@ id id>addr? .fs-read
    dup IF  dup id >slurp
	dup >blockalign over - >salign
    THEN
    dup /head
    file1( id f-rid @ = IF  dup f-ramount +!
    ELSE  f-rid @ 0>=  f-ramount @ 0> and IF
	    ." slurp: " f-rid @ . f-ramount @ h. cr  THEN
        id f-rid ! dup f-ramount !  THEN ) ;

\ careful: must follow exactpy the same logic as net2o:spit (see above)
: slurp ( -- head end-flag )
    data-head? 0= fstates 0= or  IF  head@ 0  EXIT  THEN
    slurp( ." slurp: " data-head@ drop data-map with mapc dest-raddr - endwith h.
    read-file# ? residualread @ h. forth:cr )
    [: +calc fstates 0 { states fails }
	0 BEGIN  data-head?  WHILE
		read-file# @ net2o:slurp-block
		IF 0 ELSE fails 1+ residualread off THEN to fails
		residualread @ 0= IF
		    read-file# file+  blocksize @ residualread !  THEN
	    fails states u>= UNTIL
	THEN  +file
	\ if all files are done, align
	fails states u>= dup IF  max/head@ >salign  THEN
	head@ swap
	msg( ." Read end: " over h. forth:cr ) ;]
    file-sema c-section
    slurp( ."  left: " data-head@ drop data-map with mapc dest-raddr - endwith h.
    read-file# ? residualread @ h. forth:cr )

    file( dup IF  ." data end: " over h. dup forth:. forth:cr  THEN ) ;
    
: track-seeks ( idbits xt -- ) \ xt: ( i seeklen -- )
    [: { xt } 8 cells 0 DO
	    dup 1 and IF
		I dup id>addr? >o fs-seek fs-seekto 64<> IF
		    fs-seekto 64dup to fs-seek o>
		    xt execute  ELSE  drop o>  THEN
	    THEN  2/
	LOOP  drop ;] file-sema c-section ;

: track-all-seeks ( xt -- ) \ xt: ( i seeklen -- )
    [: { xt } fstates 0 ?DO
	    I dup id>addr? >o fs-seek fs-seekto 64<> IF
		fs-seekto 64dup to fs-seek o>
		xt execute  ELSE  drop o>  THEN
	LOOP ;] file-sema c-section ;

}scope

\ permission checks

Create >file-perm
perm%filerd w, perm%filerd perm%filewr or w, perm%filewr w,
DOES>  + w@ ;
: ?rd-perm ( n -- ) perm%filerd and 0<> !!filerd-perm!! ;
: ?wr-perm ( n -- ) perm%filewr and 0<> !!filewr-perm!! ;
: ?rw-perm ( n perm -- )
    >r >file-perm r> invert and dup ?rd-perm ?wr-perm ;

\\\
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
