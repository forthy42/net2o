\ net2o distributed version control system

\ Copyright (C) 2016   Bernd Paysan

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

Variable dvcs-objects \ hash of objects

Variable dvcs-table

vocabulary project

msg-class class
    scope: dvcs

    field: commits \ msg class for commits
    field: branch$
    field: message$
    field: files#    \ snapshot config
    field: oldfiles# \ old state to compare to
    field: in-files$
    field: patch$
    field: out-files$
    field: out-fileoff
    field: fileentry$
    field: oldhash$
    field: oldtype
    field: hash$
    field: type
    field: equiv$
    field: equivtype
    field: rmdirs[]   \ sorted array of dirs to be delete
    field: outfiles[] \ sorted array of files to write out

    }scope
    
    scope{ project \ per-project configuration values
    
    field: revision$
    field: branch$
    field: project$

    }scope

    scope{ dvcs

end-class dvcs-class

begin-structure filehash
    64field: timestamp
    wfield: perm
    0 +field name
end-structure

}scope

msg-class class
    field: equiv#
    field: re#
    field: re$
    field: object$
end-class commit-class

: /name ( addr u -- addr' u' )
    [ hash#128 dvcs:name ]L /string ;
: /name' ( addr u -- addr' u' )
    [ hash#128 2 + ]L /string ;
: fn-split ( hash+ts+perm+fn u -- hash+ts+perm u1 fname u2 )
    [ hash#128 dvcs:name ]L >r 2dup r@ umin 2swap r> /string ;

: .mode ( u -- )
    dup S_IFMT and 12 rshift "0pc3d5b7f9lBsDEF" drop + c@ emit space
    S_IFMT invert and ['] . 8 base-execute ;

: .file+hash ( addr u -- )
    over hash#128 85type space  hash#128 /string
    over dvcs:timestamp le-64@ .ticks space
    over dvcs:perm le-uw@ .mode
    0 dvcs:name /string type cr ;

: +fileentry ( addr u o:dvcs -- )
    \G add a file entry and replace same file if it already exists
    dvcs( ." +f: " 2dup .file+hash ) fn-split dvcs:files# #! ;
: -fileentry ( addr u o:dvcs -- )
    dvcs( ." -f: " 2dup .file+hash ) /name dvcs:files# #off ;

: dvcs-outfile-name ( hash+perm-addr u1 fname u2 -- )
    2>r 2dup key| dvcs-objects #@ 2swap hash#128 /string
    drop dvcs:perm le-uw@ { perm } 2r>
    perm S_IFMT and  case
	S_IFLNK of
	    symlink ?ior  endof
	S_IFREG of
	    r/w create-file throw >r
	    r@ write-file throw
	    r@ fileno perm fchmod ?ior
	    r> close-file throw  endof
	S_IFDIR of
	    2dup perm mkdir-parents throw
	    perm chmod ?ior
	    2drop  endof  \ no content in directory
	2drop 2drop \ unhandled types
    endcase ;

: dvcs-outfile-hash ( baddr u1 fname u2 -- )
    hash#128 umin dvcs-objects #! ;

: dvcs-in-hash ( addr u -- )
    dvcs-objects #@ dvcs:in-files$ $+! ;

: filelist-print ( filelist -- )
    [: >r r@ cell+ $@ 85type space r> $@ type cr ;] #map ;
: filelist-out ( o:dvcs -- )
    ".n2o/files" [: >r dvcs:files# ['] filelist-print r> outfile-execute ;]
    new-file ;

: filelist-loop ( -- )
    BEGIN  refill  WHILE
	    source bl $split 2>r base85>$ 2r> dvcs:files# #!
    REPEAT ;
: filelist-in ( addr u o:dvcs -- )
    r/o open-file throw ['] filelist-loop execute-parsing-file ;

scope{ net2o-base

\g 
\g ### DVCS patch commands ###
\g 
\g DVCS metadata is stored in messages, containing message text, refs
\g and patchset objects. Patchset objects are constructed in a way
\g that makes identical transactions have the same hash.
\g 

reply-table $@ inherit-table dvcs-table

net2o' emit net2o: dvcs-read ( $:hash -- ) \g read in an object
    1 !!>=order? $> dvcs-in-hash ;
+net2o: dvcs-rm ( $:hash+name -- ) \g delete file
    2 !!>=order? $> 2dup hash#128 /string
    dvcs( ." -f: " 2dup forth:type forth:cr ) dvcs:files# #off
    hash#128 umin dvcs-in-hash ;
+net2o: dvcs-rmdir ( $:name -- ) \g delete directory
    4 !!>=order? $> dvcs( ." -f: " 2dup forth:type forth:cr ) dvcs:files# #off ;
+net2o: dvcs-patch ( $:diff len -- ) \g apply patch, len is the size of the result
    8 !!>order? $> dvcs:patch$ $! dvcs:out-fileoff off
    64dup config:patchlimit& 2@ d>64 64u> !!patch-limit!!
    dvcs:patch$ bpatch$len 64<> !!patch-size!! \ sanity check!
    dvcs:in-files$ dvcs:patch$ ['] bpatch$2 dvcs:out-files$ $exec ;
+net2o: dvcs-write ( $:perm+name size -- ) \g write out file
    $10 !!>=order? 64>n { fsize }
    dvcs:out-files$ $@ dvcs:out-fileoff @ safe/string fsize umin
    2dup >file-hash $> 2swap  dvcs:fileentry$ $off
    [: forth:type ticks { 64^ ts } ts 1 64s forth:type forth:type ;]
    dvcs:fileentry$ $exec dvcs:fileentry$ $@
    2dup +fileentry  dvcs-outfile-hash
    fsize dvcs:out-fileoff +! ;

}scope

: n2o:new-dvcs ( -- o )
    dvcs:dvcs-class new >o  dvcs-table @ token-table !
    commit-class new >o  msg-table @ token-table !  o o>  dvcs:commits !
    o o> ;
: clean-delta ( o:dvcs -- )
    dvcs:in-files$ $off dvcs:out-files$ $off  dvcs:patch$ $off ;
: n2o:dispose-commit ( o:commit -- )
    re$ $off  object$ $off  re# #offs  equiv# #offs  dispose ;
: n2o:dispose-dvcs ( o:dvcs -- )
    dvcs:branch$ $off  dvcs:message$ $off
    dvcs:files# #offs  dvcs:oldfiles# #offs
    dvcs:rmdirs[] $[]off  dvcs:outfiles[] $[]off
    clean-delta  dvcs:fileentry$ $off
    dvcs:hash$ $off  dvcs:oldhash$ $off  dvcs:equiv$ $off
    project:revision$ $off  project:branch$ $off  project:project$ $off
    dvcs:commits @ .n2o:dispose-commit dispose ;

Variable new-files[]
Variable del-files[]
Variable old-files[]
Variable new-file$
Variable branches[]

: clean-up ( -- )
    new-files[] $[]off  del-files[] $[]off  old-files[] $[]off
    branches[] $[]off  new-file$ $off ;

User tmp1$
: $tmp1 ( xt -- ) tmp1$ $off  tmp1$ $exec  tmp1$ $@ ;

: hashstat-rest ( addr u -- addr' u' )
    [: statbuf st_mode w@ 0 { w^ perm } perm le-w!
	statbuf st_mtime ntime@ d>64 64#0 { 64^ timestamp } timestamp le-64!
	perm le-uw@ S_IFMT and  case
	    S_IFLNK of  $200 new-file$ $!len
		2dup new-file$ $@ readlink
		dup ?ior new-file$ $!len  endof
	    S_IFREG of  2dup new-file$ $slurp-file  endof
	    S_IFDIR of  0 new-file$ $!len  endof
	endcase
	new-file$ $@ >file-hash
	new-file$ $@ 2swap dvcs-objects #!
	keyed-hash-out hash#128 type  timestamp 1 64s type  perm 2 type  type
    ;] $tmp1 ;
: file-hashstat ( addr u -- addr' u' )
    2dup statbuf lstat ?ior  hashstat-rest ;

: $ins[]f ( addr u array -- ) [ hash#128 dvcs:name ]L $ins[]/ drop ;

: new-files-loop ( -- )
    BEGIN  refill  WHILE  source file-hashstat new-files[] $ins[]f  REPEAT ;
: new-files-in ( addr u -- )
    r/o open-file dup no-file# = IF  2drop  EXIT  THEN  throw
    ['] new-files-loop execute-parsing-file ;

: config>dvcs ( o:dvcs -- )
    "~+/.n2o/config" ['] project >body read-config
    project:revision$ $@ base85>$ dvcs:oldhash$ $! ;
: files>dvcs ( o:dvcs -- )
    "~+/.n2o/files" filelist-in ;
: new>dvcs ( o:dvcs -- )
    "~+/.n2o/newfiles" new-files-in ;
: dvcs?modified ( o:dvcs -- )
    dvcs:files# [: >r
	r@ $@ statbuf lstat
	0< IF  errno ENOENT = IF
		r> [: dup cell+ $. $. ;] $tmp1 del-files[] $ins[]f
		EXIT  THEN  -1 ?ior  THEN
	r@ cell+ $@ drop hash#128 + dvcs:timestamp le-64@
	statbuf st_mtime ntime@ d>64 64<>
	r@ cell+ $@ drop hash#128 + dvcs:perm le-uw@
	statbuf st_mode w@ <> or  IF
	    r@ $@ hashstat-rest 2dup fn-split dvcs:files# #@
	    hash#128 umin 2swap hash#128 umin
	    str=
	    IF  2drop
	    ELSE  new-files[] $ins[]f
		r@ [: dup cell+ $. $. ;] $tmp1 old-files[] $ins[]f
	    THEN
	THEN  rdrop
    ;] #map dvcs(
    ." --- old files:" cr old-files[] ['] .file+hash $[]map
    ." +++ new files:" cr new-files[] ['] .file+hash $[]map
    ) ;

: dvcs+in ( hash u -- )
    hash#128 umin dvcs-objects #@ dvcs:in-files$ $+! ;
: dvcs+out ( hash u -- )
    hash#128 umin dvcs-objects #@ dvcs:out-files$ $+! ;

also net2o-base

: read-old-fs ( -- )
    old-files[] [: hash#128 umin 2dup $, dvcs-read dvcs+in ;] $[]map ;
: read-del-fs ( -- )
    del-files[] [: over hash#128 dvcs:perm + le-uw@
	S_IFMT and S_IFDIR =  IF  /name $, dvcs-rmdir
	ELSE 2dup [: over hash#128 forth:type /name forth:type ;] $tmp1 $,
	    dvcs-rm hash#128 umin dvcs+in  THEN ;] $[]map ;
: read-new-fs ( -- )
    new-files[] ['] dvcs+out $[]map ;
: write-new-fs ( -- )
    new-files[] [: 2dup hash#128 dvcs:perm /string $,
	/name statbuf lstat ?ior statbuf st_size @
	statbuf st_mode w@ S_IFMT and S_IFDIR <> and ulit,
	dvcs-write ;] $[]map ;
: compute-patch ( -- )
    dvcs:in-files$ dvcs:out-files$ ['] bdelta$2 dvcs:patch$ $exec
    dvcs:patch$ $@ $, dvcs:out-files$ $@len ulit, dvcs-patch ;

: compute-diff ( -- )
    read-old-fs  read-del-fs  read-new-fs
    compute-patch  write-new-fs ;

previous

: save-project ( -- )
    "~+/.n2o/config" ['] project >body write-config ;

: append-line ( addr u file u -- )
    2dup w/o open-file dup no-file# = IF
	2drop w/o create-file throw  ELSE  throw nip nip  THEN
    >r r@ file-size throw r@ reposition-file throw
    r@ write-line throw r> close-file throw ;

\ unencrypted hash stuff

Variable patch-in$

: write-hashed ( addr1 u1 -- addrhash u2 )
    keyed-hash-out hash#128 ['] 85type $tmp1 2>r
    2r@ .objects/ ?.net2o/objects spit-file 2r> ;

: read-hashed ( addr1 u1 -- addrhash u2 )
    2dup ['] 85type $tmp 2dup 2>r .objects/ patch-in$ $slurp-file
    patch-in$ $@ >file-hash str= 0= !!wrong-hash!! 2r> ;

\ encrypted hash stuff, using signature secret as PSK

\ probably needs padding...

: write-enc-hashed ( addr1 u1 -- addrhash85 u2 )
    keyed-hash-out hash#128 ['] 85type $tmp 2>r
    enchash  2>r
    save-mem 2dup c:encrypt  over swap
    ?.net2o/objects  2r> hash>filename  spit-file
    free throw  2r> ;

: enchash>filename ( hash1 u1 -- filename u2 )
    keyed-hash-out hash#128 smove
    enchash hash>filename ;

: read-enc-hashed ( hash1 u1 -- )
    2dup enchash>filename patch-in$ $slurp-file
    patch-in$ $@ c:decrypt
    patch-in$ $@ >file-hash str= 0= !!wrong-hash!! ;

\ patch stuff

' n2o:new-dvcs static-a with-allocater Constant sample-patch

\ read in branches, new version

: hash+type ( addr u type addr1 -- ) >r r@ $off
    [: { w^ x } type x cell type ;] r> $exec ;
: hash+type$ ( addr u type -- )
    [: { w^ x } type x cell type ;] $tmp1 ;

' 2drop commit-class to msg:tag
' 2drop commit-class to msg:start
' 2drop commit-class to msg:coord
' 2drop commit-class to msg:signal
' 2drop commit-class to msg:text
' 2drop commit-class to msg:action
' noop  commit-class to msg:end

:noname ( addr u type -- )
    drop re$ $+! ; commit-class to msg:re
:noname ( addr u type -- )
    object$ hash+type  re$ $@len 0= ?EXIT
    re$ $@ object$ $@ key| re# #! ; commit-class to msg:object
:noname ( addr u type -- ) >r
    object$ $@len 0= IF  rdrop 2drop  EXIT  THEN
    object$ $@ 2over equiv# #!
    r> hash+type$ object$ $@ key| equiv# #! ; commit-class to msg:equiv

: chat>dvcs ( o:dvcs -- )
    project:project$ $@ load-msg ;
: .hash ( addr -- )
    [: dup $@ 85type ."  -> " cell+ $@ 85type cr ;] #map ;
: chat>branches-loop ( o:commit -- )
    last# msg-log@ over { log } bounds ?DO
	re$ $off  object$ $off
	I $@ ['] msg-display catch IF  ." invalid entry" cr 2drop THEN
    cell +LOOP  log free throw
    dvcs( ." === re ===" cr re# .hash
    ." === equiv ===" cr equiv# .hash ) ;
: chat>branches ( o:dvcs -- )
    project:project$ $@ ?msg-log  dvcs:commits @ .chat>branches-loop ;
: re>branches-loop ( addr u -- )  0 { w^ x }
    BEGIN
	2dup d0<> IF  x cell branches[] 0 $ins  2dup 0 branches[] $[]!  THEN
	re# #@ 2dup d0<> WHILE
	    bounds ?DO  I I' over - dup hash#128 <= ?LEAVE
		hash#128 umin recurse
	    hash#128 +LOOP
    REPEAT  2drop ;
: re>branches ( addr u -- )
    branches[] $[]off  dvcs:commits @ .re>branches-loop
    dvcs( ." re:" cr branches[] [: 85type cr ;] $[]map ) ;
: branches>dvcs ( -- )
    branches[] [: dup IF
	    dvcs( ." read enc hash: " 2dup 85type cr )
	    read-enc-hashed
	    clean-delta  c-state off patch-in$ $@ do-cmd-loop
	    clean-delta
	ELSE  2drop  THEN
    ;] $[]map ;

\ push out a revision

: >revision ( addr u -- )
    2dup >file-hash dvcs:hash$ $!
    write-enc-hashed  project:revision$ $! ;

: dvcs-readin ( -- )
    config>dvcs  chat>dvcs  chat>branches  dvcs:oldhash$ $@  re>branches
    branches>dvcs  files>dvcs  new>dvcs  dvcs?modified ;
: dvcs-readin-rev ( addr u -- )
    config>dvcs  chat>dvcs  chat>branches  re>branches ;

: dvcs-log ( -- )
    n2o:new-dvcs >o  config>dvcs
    project:project$ $@ [ -1 1 rshift cell/ ]l load-msgn
    n2o:dispose-dvcs o> ;

also net2o-base
: (dvcs-newsentry) ( oldtype equivtype type -- )
    dvcs:type ! dvcs:equivtype ! dvcs:oldtype !
    msg-group$ @ >r
    project:project$ @ msg-group$ !
    o [: with dvcs
      message$   $@
      equivtype  @
      equiv$     $@
      type       @
      hash$      $@
      oldtype    @
      oldhash$   $@
      project:branch$ $@
      endwith
      $, msg-tag
      dup IF  $, ulit, msg-re      ELSE  2drop drop  THEN
      dup IF  $, ulit, msg-object  ELSE  2drop drop  THEN
      dup IF  $, ulit, msg-equiv   ELSE  2drop drop  THEN
      "Checkin" $, msg-action
      $, msg-text ;] (send-avalanche) IF  .chat  ELSE   2drop  THEN
    r> msg-group$ ! ;
previous

: dvcs-snapentry ( -- )
    0 dvcs:oldhash$ !@ dvcs:equiv$ !
    msg:patch# msg:patch# msg:snapshot# (dvcs-newsentry) ;
: dvcs-newsentry ( -- )
    msg:patch# msg:patch# msg:patch# (dvcs-newsentry) ;

: (dvcs-ci) ( addr u o:dvcs -- ) dvcs:message$ $!
    dvcs-readin
    new-files[] $[]# del-files[] $[]# d0= IF
	." Nothing to do" cr
    ELSE
	['] compute-diff gen-cmd$ >revision
	del-files[] ['] -fileentry $[]map
	new-files[] ['] +fileentry $[]map
	save-project  dvcs-newsentry  filelist-out
	"~+/.n2o/newfiles" delete-file dup no-file# <> and throw
    THEN  clean-up ;

: dvcs-ci ( addr u -- ) \ checkin command
    n2o:new-dvcs >o (dvcs-ci)  n2o:dispose-dvcs o> ;

: dvcs-diff ( -- )
    n2o:new-dvcs >o dvcs-readin
    ['] compute-diff gen-cmd$ 2drop
    dvcs:in-files$ dvcs:patch$ color-bpatch$2
    clean-up  n2o:dispose-dvcs o> ;

: ci-args ( -- message u )
    ?nextarg 0= IF "untitled checkin" THEN
    2dup "-m" str= IF  ?nextarg IF  2nip  THEN  THEN ;

: dvcs-add ( addr u -- )
    2dup '/' -scan '/' -skip dup IF  recurse  ELSE  2drop  THEN
    2dup dvcs:files# #@ drop IF  2drop  EXIT
    ELSE  "dummy" 2over dvcs:files# #!
	"~+/.n2o/newfiles" append-line  THEN ;

: dvcs-snap ( addr u -- )
    n2o:new-dvcs >o  dvcs:message$ $!
    config>dvcs  project:revision$ $off  files>dvcs
    dvcs:files# [: $@ file-hashstat new-files[] $ins[]f ;] #map
    ['] compute-diff gen-cmd$ >revision
    save-project  dvcs-snapentry  clean-up n2o:dispose-dvcs o> ;

: del-oldfile ( hash-entry -- )
    dup cell+ $@ drop hash#128 dvcs:perm + le-uw@
    S_IFMT and S_IFDIR = IF
	$@ dvcs:rmdirs[] $ins[] drop
    ELSE  dup $@ delete-file dup 0< IF
	    <err> >r ." can't delete file " $. ." : "
	    r> error$ type <default> cr
	ELSE  2drop  THEN
    THEN ;

: new->old ( -- ) dvcs( ." === remove old files ===" cr )
    dvcs:rmdirs[] $[]off
    dvcs:oldfiles# [: dup $@ dvcs:files# #@ drop 0= IF
	    del-oldfile
	ELSE  dup cell+ $@ last# cell+ $@ str= 0= IF
		del-oldfile  THEN
	THEN ;] #map
    dvcs:rmdirs[] $@ bounds cell- swap cell- U-DO
	I $@ rmdir 0< IF
	    errno strerror <err>
	    ." can't delete directory " I $. ." : " type <default> cr
	THEN
    cell -LOOP
    dvcs:rmdirs[] $[]off ;
: old->new ( -- ) dvcs( ." === write out new files ===" cr )
    dvcs:outfiles[] $[]off
    dvcs:files# [: $@ dvcs:outfiles[] $ins[] drop ;] #map \ sort filenames
    dvcs:outfiles[] [: dvcs:files# #@ d0<> IF
	    last# dup >r $@ dvcs:oldfiles# #@ over IF
		r@ cell+ $@ str=
	    ELSE  drop
	    THEN  0= IF
		dvcs( ." out " r@ $. space r@ cell+ $@ 85type cr )
		r@ cell+ $@ r@ $@ dvcs-outfile-name
	    THEN  rdrop  THEN ;] $[]map
    dvcs:outfiles[] $[]off ;

: dvcs-co ( addr u -- ) \ checkout revision
    2dup base85>$  n2o:new-dvcs >o 2swap 2>r
    config>dvcs  files>dvcs  0 dvcs:files# !@ dvcs:oldfiles# !
    dvcs-readin-rev  branches>dvcs  new->old  old->new
    2r> project:revision$ $!  save-project  filelist-out
    n2o:dispose-dvcs o> ;

: hash-add ( addr u -- )
    slurp-file 2dup >file-hash 2drop write-enc-hashed 2drop ;
: hash-out ( addr u -- )
    base85>$ 2dup 2>r read-enc-hashed patch-in$ $@ 2r> sane-85 spit-file ;

\ pull and sync a database

$A $E 2Value dvcs-bufs#

Variable dvcs-request#
Variable sync-file-list[]
$18 Constant /sync-files
$20 /sync-files * Constant /sync-reqs

event: ->dvcs-sync-done ( o -- ) >o
    file-reg# off  file-count off
    msg-group$ $@ ?msg-log ?save-msg   0 dvcs-request# !
    msg( ." === metadata sync done ===" forth:cr )
    msg-group$ $@ rows display-lastn o> ;

: dvcs-sync-done ( -- )
    msg( ." dvcs-sync-done" forth:cr )
    n2o:close-all net2o-code expect-reply close-all end-code
    msg( ." dvcs-sync-done closed" forth:cr )
    <event o elit, ->dvcs-sync-done wait-task @ event> ;

: +dvcs-sync-done ( -- )
    ['] dvcs-sync-done sync-done-xt ! ;

: dvcs-connect ( addr u -- )
    1 dvcs-request# !  dvcs-bufs# chat#-connect ;

: dvcs-connects ( -- )
    chat-keys [: key>group ?load-msgn
      dup 0= IF  msg-group$ $@ msg-groups #!  EXIT  THEN
      2dup search-connect ?dup-IF  .+group 2drop EXIT  THEN
      2dup pk-peek?  IF  dvcs-connect  ELSE  2drop  THEN ;] $[]map ;

: wait-dvcs-request ( -- )
    BEGIN  stop dvcs-request# @ 0= UNTIL ;

: +needed ( addr u -- )
    2dup enchash>filename file-status nip no-file# = IF
	sync-file-list[] $ins[] drop
    ELSE  2drop  THEN ;

: #needed ( hash -- )
    dup $@ +needed cell+ $@ key| +needed ;
: dvcs-needed-files ( -- )
    re#    ['] #needed #map
    equiv# ['] #needed #map ;

: get-needed-files ( -- ) +resend
    sync-file-list[] $[]# 0 ?DO
	I /sync-reqs + I' umin I U+DO
	    net2o-code expect-reply
	    $10 blocksize! 0 blockalign!
	    I /sync-files + I' umin I U+DO
		I sync-file-list[] $[]@ n2o:copy#
	    LOOP
	    I /sync-files + I' u>=
	    IF  n2o:done end-code| n2o:close-all  ELSE  end-code  THEN
	/sync-files +LOOP
    /sync-reqs +LOOP ;

: dvcs-data-sync ( -- )
    sync-file-list[] $[]off  branches[] $[]off
    msg-group$ $@ ?msg-log
    dvcs:commits @ .chat>branches-loop
    dvcs:commits @ .dvcs-needed-files
    connection .get-needed-files ;

: pull-readin ( -- )
    config>dvcs  chat>dvcs ;

: handle-pull ( -- )  ?.net2o/objects
    n2o:new-dvcs >o  pull-readin
    msg( ." === syncing metadata ===" forth:cr )
    0 >o dvcs-connects +dvcs-sync-done
    net2o-code expect-reply ['] last?, [msg,] end-code
    wait-dvcs-request o>
    msg( ." === syncing data ===" forth:cr )
    dvcs-data-sync
    msg( ." === data sync done ===" forth:cr )
    leave-chats
    n2o:dispose-dvcs o> ;

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]