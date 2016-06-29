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

scope: dvcs

msg-class class
    field: branch$
    field: message$
    field: files#    \ snapshot config
    field: in-files$
    field: patch$
    field: out-files$
    field: out-fileoff
    field: fileentry$
    field: outfiles   \ hash of files to write

    rot }scope
    
    scope{ project -rot \ per-project configuration values
    
    field: revision$
    field: branch$
    field: project$

    rot }scope

    scope{ dvcs -rot

end-class dvcs-class

begin-structure filehash
    64field: timestamp
    wfield: perm
    0 +field name
end-structure

}scope

hash#128 buffer: newhash

: >file-hash ( addr u -- )
    c:0key c:hash newhash hash#128 c:hash@ ;
: /name ( addr u -- addr' u' )
    [ hash#128 dvcs:name ]L /string ;
: /name' ( addr u -- addr' u' )
    [ hash#128 2 + ]L /string ;
: fn-split ( hash+ts+perm+fn u -- hash+ts+perm u1 fname u2 )
    [ hash#128 dvcs:name ]L >r 2dup r@ umin 2swap r> /string ;

: +fileentry ( addr u o:dvcs -- )
    \G add a file entry and replace same file if it already exists
    fn-split dvcs:files# #! ;
: -fileentry ( addr u o:dvcs -- )
    /name dvcs:files# #off ;

: dvcs-outfile-name ( baddr u1 fname u2 -- )  hash#128 /string
    over dvcs:perm le-uw@ { perm }
    0 dvcs:name /string
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
	    source bl $split 2>r base85>$ 2dup 2r> dvcs:files# #!
	    drop free throw  REPEAT ;
: filelist-in ( addr u o:dvcs -- )
    r/o open-file throw ['] filelist-loop execute-parsing-file ;

scope{ net2o-base

\g 
\g ### DVCS patch commands ###
\g
\g DVCS metadata is stored in messages, containing message text, refs
\g and patchset objects. Patchset objects are constructed in a way
\g that makes identical transactions have the same hash.

reply-table $@ inherit-table dvcs-table

net2o' emit net2o: dvcs-read ( $:hash -- ) \g read in an object
    1 !!>=order? $> dvcs-in-hash ;
+net2o: dvcs-rm ( $:hash+name -- ) \g delete file
    2 !!>=order? $> 2dup hash#128 /string dvcs:outfiles #off
    hash#128 umin dvcs-in-hash ;
+net2o: dvcs-rmdir ( $:name -- ) \g delete directory
    4 !!>=order? $> dvcs:outfiles #off ;
+net2o: dvcs-patch ( $:diff -- ) \g apply patch
    8 !!>order? $> dvcs:patch$ $! dvcs:out-fileoff off
    dvcs:in-files$ dvcs:patch$ ['] bpatch$2 dvcs:out-files$ $exec ;
+net2o: dvcs-write ( $:perm+name size -- ) \g write out file
    $10 !!>=order? 64>n { fsize }
    dvcs:out-files$ $@ dvcs:out-fileoff @ safe/string fsize umin
    2dup >file-hash $>
    [: newhash hash#128 forth:type
      ticks { 64^ ts } ts 1 64s forth:type forth:type ;]
    dvcs:fileentry$ $exec dvcs:fileentry$ $@
    2dup fn-split dvcs:outfiles #!
    2dup +fileentry  dvcs-outfile-hash
    fsize dvcs:out-fileoff +! ;

}scope

: n2o:new-dvcs ( -- o )
    dvcs:dvcs-class new >o  dvcs-table @ token-table ! o o> ;
: clean-delta ( o:dvcs -- )
    dvcs:in-files$ $off dvcs:out-files$ $off  dvcs:patch$ $off ;
: n2o:dispose-dvcs ( o:dvcs -- )
    dvcs:branch$ $off  dvcs:message$ $off  dvcs:files# #offs
    clean-delta dvcs:fileentry$ $off
    project:revision$ $off  project:branch$ $off  project:project$ $off
    dispose ;

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
	new-file$ $@ newhash hash#128 dvcs-objects #!
	newhash hash#128 type  timestamp 1 64s type  perm 2 type  type
    ;] $tmp1 ;
: file-hashstat ( addr u -- addr' u' )
    2dup statbuf lstat ?ior  hashstat-rest ;

: $ins[]f ( addr u array -- ) [ hash#128 dvcs:name ]L $ins[]/ ;

: new-files-loop ( -- )
    BEGIN  refill  WHILE  source file-hashstat new-files[] $ins[]f  REPEAT ;
: new-files-in ( addr u -- )
    r/o open-file dup no-file# = IF  2drop  EXIT  THEN  throw
    ['] new-files-loop execute-parsing-file ;

: config>dvcs ( o:dvcs -- )
    "~+/.n2o/config" ['] project >body read-config ;
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
	    r@ [: dup cell+ $. $. ;] $tmp1 old-files[] $ins[]f
	    r@ $@ hashstat-rest new-files[] $ins[]f
	THEN  rdrop
    ;] #map ;

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
    dvcs:patch$ $@ $, dvcs-patch ;

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
: branch$ ( -- addr u )
    [: ." .n2o/" project:branch$ $. ." .branch" ;] $tmp ;
: append-branch ( addr u -- )
    branch$ append-line ;

Variable patch-in$
' n2o:new-dvcs static-a with-allocater Value sample-patch

: branchlist-loop ( -- )
    BEGIN  refill  WHILE  source base85>$ over >r branches[] $+[]!
	    r> free throw  REPEAT ;
: apply-branch ( addr u -- flag )
    ['] 85type $tmp1 2dup project:revision$ $@ str= >r
    sample-patch >o clean-delta
    .objects/ patch-in$ $slurp-file patch-in$ $@
    c-state off do-cmd-loop o> r> ;
: branches>dvcs ( o:dvcs -- )  branches[] $[]off
    branch$ r/o open-file dup no-file# <> IF  throw
	['] branchlist-loop execute-parsing-file
	branches[] ['] apply-branch $[]map?
    ELSE  2drop  THEN ;

: >revision ( addr u -- )
    2dup >file-hash
    newhash hash#128 ['] 85type $tmp1 2dup project:revision$ $!
    2dup append-branch
    .objects/ ?.net2o/objects spit-file ;

: dvcs-readin ( -- )
    config>dvcs  branches>dvcs  files>dvcs  new>dvcs  dvcs?modified ;

: (dvcs-ci) ( addr u o:dvcs -- ) dvcs:message$ $!
    dvcs-readin
    new-files[] $[]# del-files[] $[]# d0= IF
	2drop ." Nothing to do" cr
    ELSE
	['] compute-diff gen-cmd$ >revision
	del-files[] ['] -fileentry $[]map
	new-files[] ['] +fileentry $[]map
	save-project filelist-out
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
    save-project  n2o:dispose-dvcs  clean-up o> ;

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