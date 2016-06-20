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
    field: files[] \ snapshot config
    field: in-files$
    field: patch$
    field: out-files$
    field: out-fileoff
    field: fileentry
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

: >file-hash ( addr u -- )
    c:0key c:hash
    hash#256 dvcs:fileentry $!len
    dvcs:fileentry $@ c:hash@ ;

: /name ( addr u -- addr' u' )
    hash#256 dvcs:name /string ;

: hash>filename ( addr u -- addr' u' )
    0. 2swap hash#256 umin dvcs:files[] [: >r
	r@ cell+ $@ hash#256 umin 2over str= IF
	    2drop r@ $@  THEN  rdrop ;] #map 2nip ;

: dvcs-filename@ ( -- addr u -- )
    dvcs:fileentry $@ /name ;

: +fileentry ( o:dvcs -- )
    \G add a file entry and replace same file if it already exists
    dvcs:fileentry $@ drop hash#256 dvcs:name dvcs-filename@ dvcs:files[] #! ;

: -fileentry ( o:dvcs -- )
    dvcs-filename@ dvcs:files[] #off ;

Defer dvcs-outfile

: dvcs-outfile-name ( baddr u1 fname u2 -- )
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
    hash#256 umin dvcs-objects #! ;

: dvcs-in-hash ( addr u -- )
    2dup dvcs-objects $@ ?dup-IF  2nip dvcs:in-files$ $+!
    ELSE  hash>filename dvcs:in-files$ $+slurp-file  THEN ;

' dvcs-outfile-name is dvcs-outfile

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
+net2o: dvcs-patch ( $:diff -- ) \g apply patch
    $10 !!>order? $> dvcs:patch$ $! dvcs:out-fileoff off
    dvcs:in-files$ dvcs:patch$ ['] bpatch$2 dvcs:out-files$ $exec ;
+net2o: dvcs-del ( $:name -- ) \g delete file
    $20 !!>=order? $> delete-file throw ;
+net2o: dvcs-write ( size $:ts+perm+name -- ) \g write out file
    $40 !!>=order? 64>n { fsize }
    dvcs:out-files$ $@ dvcs:out-fileoff @ safe/string fsize umin
    2dup >file-hash
    $> dvcs:fileentry $+!  dvcs:fileentry $@ dvcs-outfile
    +fileentry fsize dvcs:out-fileoff +! ;

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

hash#256 buffer: newhash

: hashstat-rest ( addr -- ) >r
    statbuf st_mode w@ 0 { w^ perm } perm le-w!
    perm 2 r@ 0 $ins
    statbuf st_mtime ntime@ d>64 64#0 { 64^ timestamp } timestamp le-64!
    timestamp 1 64s r@ 0 $ins
    perm le-uw@ S_IFMT and  case
	S_IFLNK of  $200 new-file$ $!len
	    r@ $@ 0 dvcs:name /string new-file$ $@ readlink
	    dup ?ior r@ $!len  endof
	S_IFREG of  r@ $@ 0 dvcs:name /string new-file$ $slurp-file  endof
	S_IFDIR of  "" new-file$ $!  endof
    endcase
    c:0key new-file$ $@ c:hash newhash hash#256 c:hash@
    newhash hash#256 r> 0 $ins
    new-file$ $@ newhash hash#256 dvcs-objects #! ;
: file-hashstat ( addr -- ) >r
    r@ $@ statbuf lstat ?ior r> hashstat-rest ;

: new-files-in ( addr u -- )
    new-files[] $[]slurp-file
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
	r@ cell+ $@ drop hash#256 + dvcs:timestamp le-64@
	statbuf st_mtime ntime@ d>64 64<>
	r@ cell+ $@ drop hash#256 + dvcs:perm le-uw@
	statbuf st_mode w@ <> or  IF
	    r@ cell+ $@ old-files[] $+[]!
	    r@ $@ old-files[] dup $[]# 1- swap $[]+!
	    r@ $@ new-files[] $+[]!
	    new-files[] $@ + cell- hashstat-rest
	THEN
    ;] #map ;

also net2o-base

: compute-diff ( addr u -- )
    project:branch$ $@ $, dvcs-commit  $, dvcs-message
    project:revision$ $@ dup IF  $, dvcs-ref  ELSE  2drop  THEN
    old-files[] [: hash#256 umin 2dup $, dvcs-read
	dvcs-objects #@ dvcs:in-files$ $+! ;] $[]map
    new-files[] [: hash#256 umin
	dvcs-objects #@ dvcs:out-files$ $+! ;] $[]map
    dvcs:in-files$ dvcs:out-files$ ['] bdelta$2 dvcs:patch$ $exec
    dvcs:patch$ $@ $, dvcs-patch
    del-files[] [: /name $, dvcs-del ;] $[]map
    new-files[] [: hash#256 /string $, dvcs-write ;] $[]map ;

previous

: save-project ( -- )
    "~+/.n2o/config" ['] project >body write-config ;

: (dvcs-ci) ( addr u o:dvcs -- )
    config>dvcs  files>dvcs  new>dvcs  dvcs?modified
    ['] compute-diff gen-cmd$
    2dup c:0key c:hash newhash hash#256 c:hash@
    newhash hash#256 sane-85 2dup project:revision$ $!
    .objects/ ?.net2o/objects spit-file
    del-files[] [: dvcs:fileentry $! -fileentry ;] $[]map
    new-files[] [: dvcs:fileentry $! +fileentry ;] $[]map
    save-project filelist-out "~+/.n2o/newfiles" delete-file throw ;

: dvcs-ci ( addr u -- ) \ checkin command
    n2o:new-dvcs >o (dvcs-ci)  n2o:dispose-dvcs o> ;

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