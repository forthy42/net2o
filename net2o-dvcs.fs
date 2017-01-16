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

cmd-class class
    scope: dvcs

    method read
    method rm
    method rmdir
    method patch
    method write
    method unzip
    method add
    
    }scope
end-class dvcs-abstract

dvcs-abstract class
    scope{ dvcs
    field: adds[]
    }scope
end-class dvcs-adds

dvcs-abstract class
    scope{ dvcs
    field: commits \ msg class for commits
    field: searchs \ msg class for searchs
    field: id$     \ commit id
    field: branch$
    field: message$
    field: files#    \ snapshot config
    field: oldfiles# \ old state to compare to
    field: in-files$
    field: patch$
    field: out-files$
    field: out-fileoff
    field: fileentry$
    field: oldid$
    field: hash$
    field: type
    field: rmdirs[]   \ sorted array of dirs to be delete
    field: outfiles[] \ sorted array of files to write out

    }scope
    
    scope{ project \ per-project configuration values

    field: chain$
    field: revision$
    field: branch$
    field: project$

    }scope

end-class dvcs-class

scope{ dvcs

begin-structure filehash
    64field: timestamp
    wfield: perm
    0 +field name
end-structure

}scope

msg-class class
    field: id>patch#  \ convert an ID to a patch+reference list
    field: id>snap#   \ convert an ID to a snapshot (starting point)
    field: id$
    field: re$
    field: object$
end-class commit-class

msg-class class
    field: match-tag$
    field: match-flag
    field: match-id$
end-class search-class

msg-class class
    field: log-sig$
    field: log-tag$
    field: log-id$
    field: log-action$
    field: log-text$
    field: log-chain$
end-class dvcs-log-class

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

: create-symlink-f ( addrdest udest addrlink ulink -- )
    \G create symlink and overwrite existing file
    2over 2over symlink dup -1 = IF
	errno EEXIST = IF  drop
	    2dup delete-file throw 2over 2over symlink
	THEN
    THEN  ?ior 2drop 2drop ;

: dvcs-outfile-name ( hash+perm-addr u1 fname u2 -- )
    2>r 2dup key| dvcs-objects #@ 2swap hash#128 /string
    drop dvcs:perm le-uw@ { perm } 2r>
    perm S_IFMT and  case
	S_IFLNK of
	    create-symlink-f  endof
	S_IFREG of
	    r/w create-file throw >r
	    r@ write-file throw
	    r@ fileno perm fchmod ?ior
	    r> close-file throw  endof
	S_IFDIR of
	    2dup delete-file drop \ try deleting it as file
	    2dup perm mkdir-parents
	    dup file-exist# = IF  drop  ELSE  throw  THEN
	    perm chmod ?ior
	    2drop  endof  \ no content in directory
	2drop 2drop \ unhandled types
    endcase ;

: dvcs-outfile-hash ( baddr u1 fhash u2 -- )
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
    1 !!>=order? $> dvcs:read ;
+net2o: dvcs-rm ( $:hash+name -- ) \g delete file
    2 !!>=order? $> dvcs:rm ;
+net2o: dvcs-rmdir ( $:name -- ) \g delete directory
    4 !!>=order? $> dvcs:rmdir ;
+net2o: dvcs-patch ( $:diff len -- ) \g apply patch, len is the size of the result
    8 !!>order? $> dvcs:patch ;
+net2o: dvcs-write ( $:perm+name size -- ) \g write out file
    $10 !!>=order? $> dvcs:write ;
+net2o: dvcs-unzip ( $:diffgz size algo -- $:diff ) \g unzip an object
    1 !!>=order? 64>n $> dvcs:unzip ; \ this is a stub
+net2o: dvcs-add ( $:hash -- ) \g add (and read) external hash reference
    1 !!>=order? $> dvcs:add ; \ this is a stub, too

}scope

' dvcs-in-hash ( addr u -- ) dvcs-class to dvcs:read
:noname ( addr u -- ) 2dup hash#128 /string
    dvcs( ." -f: " 2dup forth:type forth:cr ) dvcs:files# #off
    hash#128 umin dvcs-in-hash ; dvcs-class to dvcs:rm
:noname ( addr u -- )
    dvcs( ." -f: " 2dup forth:type forth:cr ) dvcs:files# #off
; dvcs-class to dvcs:rmdir
:noname ( 64len addr u -- )
    dvcs:patch$ $! dvcs:out-fileoff off
    64dup config:patchlimit& 2@ d>64 64u> !!patch-limit!!
    dvcs:patch$ bpatch$len 64<> !!patch-size!! \ sanity check!
    dvcs:in-files$ dvcs:patch$ ['] bpatch$2 dvcs:out-files$ $exec
; dvcs-class to dvcs:patch
:noname ( 64size addr u -- )
    2>r 64>n { fsize }
    dvcs:out-files$ $@ dvcs:out-fileoff @ safe/string fsize umin
    2dup >file-hash 2r> 2swap  dvcs:fileentry$ $free
    [: forth:type ticks { 64^ ts } ts 1 64s forth:type forth:type ;]
    dvcs:fileentry$ $exec dvcs:fileentry$ $@
    2dup +fileentry  dvcs-outfile-hash
    fsize dvcs:out-fileoff +! ; dvcs-class to dvcs:write
' !!FIXME!! ( 64size algo addr u --- ) dvcs-class to dvcs:unzip
:noname ( addr u -- ) \ hash+perm+name
    dvcs:fileentry$ $free
    [: over hash#128 forth:type ticks { 64^ ts } ts 1 64s forth:type
	hash#128 /string forth:type ;] dvcs:fileentry$ $exec
    dvcs:fileentry$ $@ +fileentry
; dvcs-class to dvcs:add

' 2drop dvcs-adds to dvcs:read
' 2drop dvcs-adds to dvcs:rm
' 2drop dvcs-adds to dvcs:rmdir
:noname 2drop 64drop ; dup dvcs-adds to dvcs:patch
dvcs-adds to dvcs:write
:noname 2drop drop 64drop ; dvcs-adds to dvcs:unzip
:noname ( addr u -- ) dvcs:adds[] $+[]! ; dvcs-adds to dvcs:add

: n2o:new-dvcs ( -- o )
    dvcs-class new >o  dvcs-table @ token-table !
    commit-class new >o  msg-table @ token-table !  o o>  dvcs:commits !
    search-class new >o  msg-table @ token-table !  o o>  dvcs:searchs !
    o o> ;
: n2o:new-dvcs-adds ( -- o )
    dvcs-adds new >o  dvcs-table @ token-table !  o o> ;
: clean-delta ( o:dvcs -- )
    dvcs:in-files$ $free dvcs:out-files$ $free  dvcs:patch$ $free ;
: n2o:dispose-commit ( o:commit -- )
    id$ $free  re$ $free  object$ $free  dispose ;
: n2o:dispose-search ( o:commit -- )
    match-id$ $free  match-tag$ $free  dispose ;
: n2o:dispose-dvcs-adds ( o:dvcs -- )
    dvcs:adds[] $[]free dispose ;
: n2o:dispose-dvcs ( o:dvcs -- )
    dvcs:branch$ $free  dvcs:message$ $free
    dvcs:files# #offs  dvcs:oldfiles# #offs
    dvcs:rmdirs[] $[]off  dvcs:outfiles[] $[]off
    clean-delta  dvcs:fileentry$ $free
    dvcs:hash$ $free
    dvcs:id$ $free  dvcs:oldid$ $free
    project:revision$ $free   project:chain$ $free
    project:branch$ $free  project:project$ $free
    dvcs:commits @ .n2o:dispose-commit
    dvcs:searchs @ .n2o:dispose-search
    dispose ;

Variable new-files[]
Variable ref-files[]
Variable del-files[]
Variable old-files[]
Variable new-file$
Variable branches[]

: clean-up ( -- )
    new-files[] $[]off  ref-files[] $[]off
    del-files[] $[]off  old-files[] $[]off
    branches[]  $[]off  new-file$ $free ;

User tmp1$
: $tmp1 ( xt -- ) tmp1$ $free  tmp1$ $exec  tmp1$ $@ ;

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
    project:revision$ $@ base85>$ dvcs:oldid$ $! ;
: files>dvcs ( o:dvcs -- )
    "~+/.n2o/files" filelist-in ;
: new>dvcs ( o:dvcs -- )
    "~+/.n2o/reffiles" new-files-in 0 new-files[] !@ ref-files[] !
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
    ." +++ ref files:" cr ref-files[] ['] .file+hash $[]map
    ) ;

: dvcs+in ( hash u -- )
    hash#128 umin dvcs-objects #@ dvcs:in-files$ $+! ;
: dvcs+out ( hash u -- )
    hash#128 umin dvcs-objects #@ dvcs:out-files$ $+! ;

: file-size@ ( addr u -- 64size )
    statbuf lstat ?ior statbuf st_size 64@
    statbuf st_mode w@ S_IFMT and S_IFDIR <> n>64 64and ;

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
: read-ref-fs ( -- )
    ref-files[] ['] 2drop $[]map ; \ !!FIXME!! stub!
: write-new-fs ( -- )
    new-files[] [: 2dup hash#128 dvcs:perm /string $,
	/name file-size@ lit, dvcs-write ;] $[]map ;
: write-ref-fs ( -- )
    ref-files[] [: over hash#128 $, dvcs-add hash#128 /string 2dup $,
	2 /string file-size@ lit, dvcs-write ;] $[]map ;
: compute-patch ( -- )
    dvcs:in-files$ dvcs:out-files$ ['] bdelta$2 dvcs:patch$ $exec
    dvcs:patch$ $@ $, dvcs:out-files$ $@len ulit, dvcs-patch ;

: compute-diff ( -- )
    read-old-fs  read-del-fs  read-new-fs  read-ref-fs
    compute-patch  write-new-fs write-ref-fs ;

Variable id-files[]

: dvcs-gen-id ( -- addr u )
    id-files[] $[]off
    dvcs:files# [: dup cell+ $@ 2>r $@ 2r> [: forth:type forth:type ;] $tmp
	id-files[] $ins[] drop ;] #map \ sort filenames
    [: id-files[] [:
	    over hash#128 $, dvcs-read hash#128 /string
	    0 dvcs:perm /string 2dup $,
	    2 /string file-size@ lit, dvcs-write
	;] $[]map ;] gen-cmd$
    dup IF  >file-hash  THEN ;

previous

: save-project ( -- )
    dvcs:id$ $@ ['] 85type project:revision$ dup $free $exec
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

: hash+type ( addr u type addr1 -- ) >r r@ $free
    [: { w^ x } type x cell type ;] r> $exec ;
: hash+type$ ( addr u type -- )
    [: { w^ x } type x cell type ;] $tmp1 ;

' 2drop commit-class to msg:tag
' 2drop commit-class to msg:start
' 2drop commit-class to msg:coord
' 2drop commit-class to msg:signal
' 2drop commit-class to msg:text
' 2drop commit-class to msg:action
' 2drop commit-class to msg:chain
' noop  commit-class to msg:end

:noname ( addr u -- )
    re$ $+! ; commit-class to msg:re
:noname ( addr u -- )
    id$ $! re$ $free ; commit-class to msg:id
:noname ( addr u type -- )
    object$ hash+type
    object$ $@ key| id$ $@
    id>patch# id>snap# re$ $@len select #!
    re$ $@len IF
	re$ $@ last# cell+ $+!
    THEN ; commit-class to msg:object

\ search for a specific id

' 2drop search-class to msg:start
' 2drop search-class to msg:coord
' 2drop search-class to msg:signal
' 2drop search-class to msg:text
' 2drop search-class to msg:action
' 2drop search-class to msg:chain
' 2drop search-class to msg:re
' noop  search-class to msg:end

: 3drop  2drop drop ;

:noname match-tag$ $@ str= match-flag ! ; search-class to msg:tag
:noname match-flag @ IF  match-id$ $!  ELSE  2drop  THEN ; search-class to msg:id
' 3drop search-class to msg:object

' 2drop dvcs-log-class to msg:re
' 2drop dvcs-log-class to msg:coord
' 3drop dvcs-log-class to msg:object
' noop  dvcs-log-class to msg:end
:noname log-sig$    $! ; dvcs-log-class to msg:start
:noname log-tag$    $! ; dvcs-log-class to msg:tag
:noname log-id$     $! ; dvcs-log-class to msg:id
:noname log-text$   $! ; dvcs-log-class to msg:text
:noname log-action$ $! ; dvcs-log-class to msg:action
:noname log-chain$  $! ; dvcs-log-class to msg:chain

: chat>dvcs ( o:dvcs -- )
    project:project$ $@ load-msg ;
: .hash ( addr -- )
    [: dup $@ 85type ."  -> " cell+ $@ 85type cr ;] #map ;
: chat>branches-loop ( o:commit -- )
    last# msg-log@ over { log } bounds ?DO
	re$ $free  object$ $free
	I $@ ['] msg-display catch IF  ." invalid entry" cr 2drop THEN
    cell +LOOP  log free throw
    dvcs( ." === id>patch ===" cr id>patch# .hash
    ." === id>snap ===" cr id>snap# .hash ) ;
: chat>branches ( o:dvcs -- )
    project:project$ $@ ?msg-log  dvcs:commits @ .chat>branches-loop ;

: >branches ( addr u -- flag )
    $make branches[] >back ;
User id-check# \ check hash
: id>branches-loop ( addr u -- )
    BEGIN  2dup id-check# #@ d0<> ?EXIT
	s" !" 2over id-check# #!
	2dup id>snap# #@ 2dup d0<> IF  >branches 2drop  EXIT  THEN
	2drop id>patch# #@ 2dup d0<> WHILE
	    2dup hash#128 umin >branches
	    hash#128 safe/string  hash#128 - 2dup + >r
	    bounds U+DO  I hash#128 recurse  hash#128 +LOOP
	    r> hash#128 \ tail recursion optimization
    REPEAT ;
: id>branches ( addr u -- )
    id-check# #offs
    branches[] $[]off  dvcs:commits @ .id>branches-loop
    id-check# #offs
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
    write-enc-hashed 2drop ;

: pull-readin ( -- )
    config>dvcs  chat>dvcs  chat>branches ;
: dvcs-readin-rev ( addr u -- )
    pull-readin  id>branches ;
: dvcs-readin ( -- )
    dvcs:oldid$ $@ dvcs-readin-rev
    branches>dvcs  files>dvcs  new>dvcs  dvcs?modified ;

: n2o:new-dvcs-log ( -- o )
    dvcs-log-class new >o msg-table @ token-table ! o o> ;
: n2o:dispose-dvcs-log ( o:log -- )
    log-sig$    $free  log-tag$  $free  log-id$    $free
    log-action$ $free  log-text$ $free  log-chain$ $free
    dispose ;

: display-logn ( addr u n -- )
    project:branch$ $@ { d: branch }
    n2o:new-dvcs-log >o
    cells >r ?msg-log  last# msg-log@ 2dup { log u }
    dup r> - 0 max dup >r /string r> cell/ -rot bounds ?DO
	I $@ ['] msg-display catch
	IF  ." invalid entry" cr 2drop
	ELSE
	    branch log-tag$ $@ str= IF
		dup 0 .r ." : [" log-id$ $@ 85type ." ] "
		log-sig$ $@ 2dup startdate@ .ticks
		log-chain$ $@ dup IF
		    2dup sighash? IF  <info>  ELSE  <err>  THEN
		    ." <-" drop le-64@ .ticks  <default>
		ELSE  2drop  THEN  space
		log-action$ $. ." : " log-text$ $. space
		last# >r  .key-id  r> to last#
		cr
	    THEN
	THEN  1+
    cell +LOOP  drop
    log free n2o:dispose-dvcs-log o> throw ;

: dvcs-log ( -- )
    n2o:new-dvcs >o  config>dvcs
    project:project$ $@ 2dup load-msg
    config:logsize# @ display-logn
    n2o:dispose-dvcs o> ;

also net2o-base
: (dvcs-newsentry) ( type -- )
    dvcs:type !
    msg-group$ @ >r
    project:project$ @ msg-group$ !
    o [: with dvcs
	project:chain$ $@ base85>$
	message$   $@
	type       @
	hash$      $@
	oldid$     $@
	id$        $@
	project:branch$ $@
	endwith
	$, msg-tag
	$, msg-id
	dup >r
	dup IF  $, msg-re     ELSE  2drop  THEN
	dup >r dup IF  $, ulit, msg-object  ELSE  2drop drop  THEN
	r> r> IF   IF  "Patchset"  ELSE  "Revert"  THEN
	ELSE  drop "Snapshot"  THEN  $, msg-action
	$, msg-text
	dup IF  $, msg-chain  ELSE  2drop  THEN
    ;] (send-avalanche) IF  .chat  ELSE   2drop  THEN
    r> msg-group$ ! ;
previous

: dvcs-snapentry ( -- )
    dvcs:oldid$ $free
    msg:snapshot# (dvcs-newsentry) ;
: dvcs-newsentry ( -- )
    msg:patch# msg:snapshot# dvcs:oldid$ $@len select (dvcs-newsentry) ;

: >id-revision ( addr u -- )
    dvcs-gen-id 2dup dvcs:id$ $!
    dvcs:commits @ >o
    2dup id>patch# #@ d0= >r id>snap# #@ d0= r> and o>
    IF  >revision  ELSE  2drop  THEN ;

: ?delete-file ( addr u -- )
    delete-file dup no-file# <> and throw ;

: dvcs-sig$ ( -- )
    last-signed 2@ sigdate@ over 1 64s 85type 2 64s /string
    c:0key >hash hashtmp hash#128 85type ;
 
: (dvcs-ci) ( addr u o:dvcs -- ) dvcs:message$ $!
    dvcs-readin
    ref-files[] $[]# new-files[] $[]# del-files[] $[]# or or 0= IF
	." Nothing to do" cr
    ELSE
	['] compute-diff gen-cmd$
	del-files[] ['] -fileentry $[]map
	new-files[] ['] +fileentry $[]map
	ref-files[] ['] +fileentry $[]map
	>id-revision  dvcs-newsentry
	['] dvcs-sig$ project:chain$ dup $free $exec
	save-project  filelist-out
	"~+/.n2o/newfiles" ?delete-file
	"~+/.n2o/reffiles" ?delete-file
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

: dvcs-ref ( addr u -- )
    2dup '/' -scan '/' -skip dup IF  dvcs-add  ELSE  2drop  THEN
    2dup dvcs:files# #@ drop IF  2drop  EXIT
    ELSE  "dummy" 2over dvcs:files# #!
	"~+/.n2o/reffiles" append-line  THEN ;

: dvcs-snap ( addr u -- )
    n2o:new-dvcs >o  dvcs:message$ $!
    config>dvcs  files>dvcs
    dvcs:files# [: $@ file-hashstat new-files[] $ins[]f ;] #map
    ['] compute-diff gen-cmd$ >id-revision
    dvcs-snapentry  save-project  clean-up n2o:dispose-dvcs o> ;

: del-oldfile ( hash-entry -- )
    dup cell+ $@ drop hash#128 dvcs:perm + le-uw@
    S_IFMT and S_IFDIR = IF
	$@ dvcs( ." rd " 2dup type cr ) dvcs:rmdirs[] $ins[] drop
    ELSE  dup $@ dvcs( ." rm " 2dup type cr )
	delete-file dup 0< IF
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

: co-rest ( -- )
    0 dvcs:files# !@ dvcs:oldfiles# !
    branches>dvcs  new->old  old->new
    save-project  filelist-out ;

: dvcs-co ( addr u -- ) \ checkout revision
    base85>$  n2o:new-dvcs >o
    config>dvcs   2dup dvcs:id$ $!  dvcs-readin-rev
    branches>dvcs  files>dvcs  new>dvcs  dvcs?modified  co-rest
    n2o:dispose-dvcs o> ;

: chat>searchs-loop ( o:commit -- )
    last# msg-log@ over { log } bounds ?DO
	I $@ ['] msg-display catch IF  ." invalid entry" cr 2drop THEN
    cell +LOOP  log free throw ;
: search-last-rev ( -- addr u )
    project:project$ $@ ?msg-log
    project:branch$ $@
    dvcs:searchs @ >o match-tag$ $!
    chat>searchs-loop match-id$ $@ o> ;

: dvcs-up ( -- ) \ checkout latest revision
    n2o:new-dvcs >o
    pull-readin  files>dvcs  new>dvcs  dvcs?modified
    new-files[] $[]# del-files[] $[]# d0= IF
	search-last-rev  2dup dvcs:id$ $!
	2dup dvcs:oldid$ $@ str= IF
	    2drop ." already up to date" cr
	ELSE  id>branches  co-rest  THEN
    ELSE
	." Local changes, don't update" cr
    THEN
    n2o:dispose-dvcs o> ;

: dvcs-revert ( -- ) \ restore to last revision
    n2o:new-dvcs >o
    pull-readin  dvcs:oldid$ $@  2dup dvcs:id$ $!
    id>branches  co-rest
    n2o:dispose-dvcs o> ;

: hash-add ( addr u -- )
    slurp-file 2dup >file-hash 2drop write-enc-hashed 2drop ;
: hash-out ( addr u -- )
    base85>$ 2dup 2>r read-enc-hashed patch-in$ $@ 2r> hash-85 spit-file ;

\ pull and sync a database

$A $E 2Value dvcs-bufs#

Variable dvcs-request#
Variable sync-file-list[]
$18 Constant /sync-files
$20 /sync-files * Constant /sync-reqs

event: ->dvcs-sync-done ( o -- ) >o
    file-reg# off  file-count off
    msg-group$ $@ ?msg-log ?save-msg   0 dvcs-request# !
    msg( ." === metadata sync done ===" forth:cr ) o> ;

: dvcs-sync-done ( -- )
    msg( ." dvcs-sync-done" forth:cr )
    n2o:close-all net2o-code expect-reply close-all net2o:gen-reset end-code
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
	dvcs( ." need: " 2dup 85type cr )
	sync-file-list[] $ins[] drop
    ELSE  dvcs( ." don't need: " 2dup 85type cr ) 2drop  THEN ;

: #needed ( hash -- )
    cell+ $@ key| +needed ;
: dvcs-needed-files ( -- )
    id>patch# ['] #needed #map
    id>snap#  ['] #needed #map ;

: get-needed-files ( -- ) +resend
    sync-file-list[] $[]# 0 ?DO
	I /sync-reqs + I' umin I U+DO
	    net2o-code expect+slurp
	    $10 blocksize! 0 blockalign!
	    I /sync-files + I' umin I U+DO
		I sync-file-list[] $[]@ n2o:copy#
	    LOOP
	    I /sync-files + I' u>=
	    IF  end-code| n2o:close-all  ELSE  end-code  THEN
	/sync-files +LOOP
    /sync-reqs +LOOP ;

: dvcs-data-sync ( -- )
    sync-file-list[] $[]off  branches[] $[]off
    msg-group$ $@ ?msg-log
    dvcs:commits @ .chat>branches-loop
    dvcs:commits @ .dvcs-needed-files
    connection .get-needed-files ;

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