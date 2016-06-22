\ net2o DVCS

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

scope: dvcs

msg-class class
    field: branch$
    field: message$
    field: files[]    \ snapshot config
    field: in-files$
    field: patch$
    field: out-files$
    field: out-fileoff
    field: fileentry
    field: delfiles[] \ list of files to delete
    field: deldirs[]  \ list of dirs to delete
    field: outfiles   \ hash of files to write
end-class dvcs-class

begin-structure filehash
    64field: timestamp
    wfield: perm
    0 +field name
end-structure

}scope

scope: project \ per-project configuration values

Variable revision$
Variable branch$
Variable project$

}scope

hash#128 buffer: newhash

: >file-hash ( addr u -- )
    c:0key c:hash newhash hash#128 c:hash@ ;
: /name ( addr u -- addr' u' )
    [ hash#128 dvcs:name ]L /string ;
: fn-split ( hash+ts+perm+fn u -- hash+ts+perm u1 fname u2 )
    [ hash#128 dvcs:name ]L >r 2dup r@ umin 2swap r> /string ;

: +fileentry ( addr u o:dvcs -- )
    \G add a file entry and replace same file if it already exists
    fn-split dvcs:files[] #! ;
: -fileentry ( addr u o:dvcs -- )
    /name dvcs:files[] #off ;

: dvcs-outfile-name ( baddr u1 fname u2 -- )  hash#128 /string
    over dvcs:timestamp le-64@ 64>d #1000000000 um/mod { d^ ts-ns }
    over dvcs:perm le-uw@ { perm }
    0 dvcs:name /string
    perm S_IFMT and  case
	S_IFLNK of
	    2dup 2>r symlink ?ior
	    2r> ts-ns lutimens ?ior  endof
	S_IFREG of
	    r/w create-file throw >r
	    r@ write-file throw
	    r@ fileno perm fchmod ?ior
	    r@ fileno ts-ns futimens ?ior
	    r> close-file throw  endof
	S_IFDIR of
	    2dup perm mkdir-parents throw
	    2dup ts-ns utimens ?ior
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
    ".n2o/files" [: >r dvcs:files[] ['] filelist-print r> outfile-execute ;]
    new-file ;

: filelist-loop ( -- )
    BEGIN  refill  WHILE
	    source bl $split 2>r base85>$ 2dup 2r> dvcs:files[] #!
	    drop free throw  REPEAT ;
: filelist-in ( addr u o:dvcs -- )
    r/o open-file throw ['] filelist-loop execute-parsing-file ;

scope{ net2o-base

\g 
\g ### DVCS commands ###
\g 

reply-table $@ inherit-table dvcs-table

net2o' emit net2o: dvcs-commit ( $:branch -- ) \g start a commit to branch
    1 !!>order? $> dvcs:branch$ $! ;
+net2o: dvcs-message ( $:message -- ) \g commit message
    2 !!>order? $> dvcs:message$ $! ;
+net2o: dvcs-ref ( $:hash -- ) \ g previous patch
    4 !!>order? $> ;
+net2o: dvcs-read ( $:hash -- ) \g read in an object
    8 !!>=order? $> dvcs-in-hash ;
+net2o: dvcs-rm ( $:name -- ) \g delete file
    $10 !!>=order? $> 2dup hash#128 umin dvcs-in-hash dvcs:delfiles[] $ins[] ;
+net2o: dvcs-rmdir ( $:name -- ) \g delete directory
    $10 !!>=order? $> dvcs:deldirs[] $ins[] ;
+net2o: dvcs-patch ( $:diff -- ) \g apply patch
    $20 !!>order? $> dvcs:patch$ $! dvcs:out-fileoff off
    dvcs:in-files$ dvcs:patch$ ['] bpatch$2 dvcs:out-files$ $exec ;
+net2o: dvcs-write ( size $:hash+ts+perm+name -- ) \g write out file
    $40 !!>=order? 64>n { fsize }
    dvcs:out-files$ $@ dvcs:out-fileoff @ safe/string fsize umin
    2dup >file-hash $> 2dup fn-split dvcs:outfiles #!
    2dup hash#128 umin newhash over str= 0= IF
	." hash mismatch: " 2dup hash#128 umin 85type space
	newhash hash#128 85type forth:cr
	2over forth:type
    THEN
    2dup +fileentry  dvcs-outfile-hash
    fsize dvcs:out-fileoff +! ;

}scope

: n2o:new-dvcs ( -- o )
    dvcs:dvcs-class new >o  dvcs-table @ token-table ! o o> ;
: n2o:dispose-dvcs ( o:dvcs -- )
    dvcs:branch$ $off  dvcs:message$ $off  dvcs:files[] #offs
    dvcs:in-files$ $off dvcs:out-files$ $off  dispose ;

Variable new-files[]
Variable del-files[]
Variable old-files[]
Variable new-file$

: clean-up ( -- )
    new-files[] $[]off  del-files[] $[]off  old-files[] $[]off
    new-file$ $off ;

: hashstat-rest ( addr -- ) >r
    statbuf st_mode w@ 0 { w^ perm } perm le-w!
    perm 2 r@ 0 $ins
    statbuf st_mtime ntime@ d>64 64#0 { 64^ timestamp } timestamp le-64!
    timestamp 1 64s r@ 0 $ins
    perm le-uw@ S_IFMT and  case
	S_IFLNK of  $200 new-file$ $!len
	    r@ $@ 0 dvcs:name /string new-file$ $@ readlink
	    dup ?ior new-file$ $!len  endof
	S_IFREG of  r@ $@ 0 dvcs:name /string new-file$ $slurp-file  endof
	S_IFDIR of  "" new-file$ $!  endof
    endcase
    new-file$ $@ >file-hash
    newhash hash#128 r> 0 $ins
    new-file$ $@ newhash hash#128 dvcs-objects #! ;
: file-hashstat ( addr -- ) >r
    r@ $@ statbuf lstat ?ior r> hashstat-rest ;

: new-files-in ( addr u -- )
    r/o open-file dup no-file# = IF  2drop  EXIT  THEN  throw
    dup >r new-files[] $[]slurp  r> close-file throw
    new-files[] $[]# 0 ?DO  I new-files[] $[] file-hashstat  LOOP ;

: config>dvcs ( o:dvcs -- )
    "~+/.n2o/config" ['] project >body read-config ;
: files>dvcs ( o:dvcs -- )
    "~+/.n2o/files" filelist-in ;
: new>dvcs ( o:dvcs -- )
    "~+/.n2o/newfiles" new-files-in ;
: dvcs?modified ( o:dvcs -- )
    dvcs:files[] [: >r
	r@ $@ statbuf lstat
	0< IF  errno ENOENT = IF
		r@ cell+ $@ del-files[] $+[]!
		r> $@ del-files[] dup $[]# 1- swap $[]+!
		EXIT  THEN  -1 ?ior  THEN
	r@ cell+ $@ drop hash#128 + dvcs:timestamp le-64@
	statbuf st_mtime ntime@ d>64 64<>
	r@ cell+ $@ drop hash#128 + dvcs:perm le-uw@
	statbuf st_mode w@ <> or  IF
	    r@ cell+ $@ old-files[] $+[]!
	    r@ $@ old-files[] dup $[]# 1- swap $[]+!
	    r@ $@ new-files[] $+[]!
	    new-files[] $@ + cell- hashstat-rest
	THEN  rdrop
    ;] #map ;

: dvcs+in ( hash u -- )
    hash#128 umin dvcs-objects #@ dvcs:in-files$ $+! ;
: dvcs+out ( hash u -- )
    hash#128 umin dvcs-objects #@ dvcs:out-files$ $+! ;

also net2o-base

: compute-diff ( addr u -- )
    project:branch$ $@ $, dvcs-commit  $, dvcs-message
    project:revision$ $@ dup IF  base85>$ over >r $, r> free throw dvcs-ref
    ELSE  2drop  THEN
    old-files[] [: hash#128 umin 2dup $, dvcs-read dvcs+in ;] $[]map
    del-files[] [: 2dup over hash#128 dvcs:perm + le-uw@ >r $,
	r> S_IFMT and S_IFDIR =  IF  dvcs-rmdir 2drop
	ELSE  dvcs-rm hash#128 umin dvcs+in  THEN ;] $[]map
    new-files[] ['] dvcs+out $[]map
    dvcs:in-files$ dvcs:out-files$ ['] bdelta$2 dvcs:patch$ $exec
    dvcs:patch$ $@ $, dvcs-patch
    new-files[] [:
	2dup /name statbuf lstat ?ior statbuf st_size @
	statbuf st_mode w@ S_IFMT and S_IFDIR <> and ulit,
	$, dvcs-write ;] $[]map ;

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
    BEGIN  refill  WHILE
	    source .objects/ patch-in$ $slurp-file
	    patch-in$ $@ sample-patch >o
	    dvcs:in-files$ $off dvcs:out-files$ $off
	    c-state off do-cmd-loop o>  REPEAT ;
: branches>dvcs ( o:dvcs -- )
    branch$ r/o open-file dup no-file# <> IF  throw
    ['] branchlist-loop execute-parsing-file  ELSE  2drop  THEN ;

: (dvcs-ci) ( addr u o:dvcs -- )
    config>dvcs  branches>dvcs  files>dvcs  new>dvcs  dvcs?modified
    new-files[] $[]# del-files[] $[]# d0= IF
	2drop ." Nothing to do" cr  EXIT  THEN
    ['] compute-diff gen-cmd$
    2dup c:0key c:hash newhash hash#128 c:hash@
    newhash hash#128 sane-85 2dup project:revision$ $!
    2dup append-branch
    .objects/ ?.net2o/objects spit-file
    del-files[] ['] -fileentry $[]map
    new-files[] ['] +fileentry $[]map
    save-project filelist-out clean-up
    "~+/.n2o/newfiles" delete-file dup no-file# <> and throw ;

: dvcs-ci ( addr u -- ) \ checkin command
    n2o:new-dvcs >o (dvcs-ci)  n2o:dispose-dvcs o> ;

: dvcs-add ( addr u -- )
    2dup '/' -scan '/' -skip dup IF  recurse  ELSE  2drop  THEN
    2dup dvcs:files[] #@ drop IF  2drop  EXIT
    ELSE  "dummy" 2over dvcs:files[] #!
	"~+/.n2o/newfiles" append-line  THEN ;

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