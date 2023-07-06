\ net2o distributed version control system

\ Copyright © 2016-2019   Bernd Paysan

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
    field: in-files$
    field: patch$
    field: out-files$

    method read
    method rm
    method rmdir
    method patch
    method write
    method unzip
    method ref
    
    }scope
end-class dvcs-abstract

dvcs-abstract class
    scope{ dvcs
    field: refs[]
    }scope
end-class dvcs-refs

dvcs-abstract class
    scope{ dvcs
    field: commits    \ msg class for commits
    field: searchs    \ msg class for searchs
    field: id$        \ commit id
    field: branch$
    field: message$   \ commit message
    field: fileref[]  \ file refs
    field: files#     \ snapshot config
    field: oldfiles#  \ old state to compare to
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

msg:class class
    field: id>patch#  \ convert an ID to a patch+reference list
    field: id>snap#   \ convert an ID to a snapshot (starting point)
    field: id$
    field: re$
    field: object$
end-class commit-class

msg:class class
    scope: match
    field: tag$
    field: flag
    field: id$
    }scope
end-class search-class

msg:class class
    scope: dvcs-log
    field: sig$
    field: tag$
    field: id$
    field: action$
    field: text$
    field: chain$
    field: urls[]
    }scope
end-class dvcs-log-class

: /name ( addr u -- addr' u' )
    [ hash#128 dvcs:name ]L /string ;
: fn-split ( hash+ts+perm+fn u -- hash+ts+perm u1 fname u2 )
    [ hash#128 dvcs:name ]L >r 2dup r@ umin 2swap r> /string ;

: .mode ( u -- )
    dup S_IFMT and 12 rshift "0pc3d5b7frlBsDEF" drop + c@ emit space
    S_IFMT invert and ['] . 8 base-execute ;

: .file+hash ( addr u -- )
    over hash#128 85type space  hash#128 /string
    over dvcs:timestamp le-64@ .ticks space
    over dvcs:perm w@ wle .mode
    0 dvcs:name /string type cr ;

: +fileentry ( addr u o:dvcs -- )
    \G add a file entry and replace same file if it already exists
    dvcs( ." +f: " 2dup .file+hash ) fn-split dvcs:files# #! ;
: -fileentry ( addr u o:dvcs -- )
    dvcs( ." -f: " 2dup .file+hash ) /name dvcs:files# #free ;

: create-symlink-f ( addrdest udest addrlink ulink perm -- ) { perm }
    \G create symlink and overwrite existing file
    2over 2over symlink dup -1 = IF
	errno EEXIST = IF  drop
	    2dup delete-file throw 2over 2over symlink
	THEN
    THEN  ?ior 2drop 2drop ;

: create-file-f ( addr u addrfile ufile perm -- ) { perm }
    r/w create-file throw >r
    dvcs( ." write " dup . ." bytes" cr )
    r@ write-file throw
    r@ fileno perm fchmod ?ior
    r> close-file throw ;

: create-dir-f ( addr 0 addrdir udir perm -- ) { perm }
    2dup delete-file drop \ try deleting it as file
    2dup perm mkdir-parents
    dup file-exist# = IF  drop  ELSE  throw  THEN
    perm chmod ?ior
    2drop ;

S_IFMT $1000 invert and Constant S_IFMT?

: dvcs-outfile-name ( hash+perm-addr u1 fname u2 -- )
    2>r 2dup key| dvcs-objects #@ 2swap hash#128 /string
    drop dvcs:perm w@ wle { perm } 2r>
    perm S_IFMT? and  case
	S_IFLNK              of  perm create-symlink-f  endof
	S_IFREG              of  perm create-file-f     endof
	S_IFDIR              of  perm create-dir-f      endof  \ no content in directory
	dvcs( ." unhandled type " hex. type space hex. drop cr 0 )else(
	2drop 2drop ) \ unhandled types
    endcase ;

\ encrypted hash stuff, using signature secret as PSK

\ probably needs padding...

: write-enc-hashed ( addr1 u1 -- addrhash85 u2 )
    $make { w^ enc-pad$ } enc-pad$ $@
    keyed-hash-out hash#128 ['] 85type $tmp 2>r
    enchash  2>r
    2dup c:encrypt
    ?.net2o/objects  2r> hash>filename  spit-file
    enc-pad$ $free 2r> ;

: enchash>filename ( hash1 u1 -- filename u2 )
    keyed-hash-out hash#128 smove
    enchash hash>filename ;

Variable patch-in$

: read-enc-hashed ( hash1 u1 -- )
    \ 2dup 85type space
    2dup enchash>filename patch-in$ $slurp-file
    patch-in$ $@ c:decrypt
    patch-in$ $@ >file-hash \ 2dup 85type cr
    str= 0= !!wrong-hash!! ;

: ?read-enc-hashed ( hash1 u1 -- addr u )
    2dup dvcs-objects #@ 2dup d0= IF
	2drop 2dup read-enc-hashed
	patch-in$ $@ 2swap dvcs-objects #!
    ELSE
	2drop 2drop
    THEN  last# cell+ $@ ;

\ in-memory file hash+contents database

: dvcs-outfile-hash ( baddr u1 fhash u2 -- )
    hash#128 umin dvcs-objects #! ;

: ?fileentry-hash ( -- )
    dvcs:fileentry$ $@ hash#128 umin
    2dup dvcs-objects #@ d0<> IF  2drop  EXIT  THEN
    read-enc-hashed
    patch-in$ $@ dvcs:fileentry$ $@ dvcs-outfile-hash ;

: dvcs-in-hash ( addr u -- )
    dvcs( ." +read: " 2dup 85type cr )
    dvcs-objects #@ over 0= !!dvcs-hash!!
    dvcs:in-files$ $+! ;

: filelist-print ( filelist -- )
    [: dup >r cell+ $@ 85type space r> $@ type cr ;] #map ;
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
    1 !!>=order? 64>n $> dvcs:unzip ;
+net2o: dvcs-ref ( $:hash+perm+name -- ) \g external hash reference
    $10 !!>=order? $> dvcs:ref ;

}scope

dvcs-table $save

' dvcs-in-hash ( addr u -- ) dvcs-class is dvcs:read
:noname ( addr u -- )
    2dup hash#128 /string ?sane-file
    dvcs( ." -f: " 2dup forth:type forth:cr ) dvcs:files# #free
    hash#128 umin dvcs-in-hash ; dvcs-class is dvcs:rm
:noname ( addr u -- )
    2dup 2 /string ?sane-file 2drop
    dvcs( ." -f: " 2dup forth:type forth:cr ) dvcs:files# #free
; dvcs-class is dvcs:rmdir
:noname ( 64len addr u -- )
    dvcs:patch$ $! dvcs( ." -patch: " 64dup u64. )
    dvcs:out-fileoff off
    64dup config:patchlimit& 2@ d>64 64u> !!patch-limit!!
    dvcs:patch$ bpatch$len 64<> !!patch-size!! \ sanity check!
    dvcsfiles( ." ===== in files =====" cr dvcs:in-files$ $. cr )
    dvcs( ." ===== diff =====" cr
    dvcs:in-files$ dvcs:patch$ color-bpatch$2 )
    dvcs:out-files$ $free
    dvcs:in-files$ dvcs:patch$ ['] bpatch$2 dvcs:out-files$ $exec
    dvcsfiles( ." ===== " dvcs:out-files$ $@len u. ."  =====" cr
    dvcs:out-files$ $. ." ========================" cr )
; dvcs-class is dvcs:patch
:noname ( 64size addr u -- )
    2dup 2 /string ?sane-file 2drop
    2>r dvcs( ." -write: " 64dup u64. cr )
    64>n { fsize }
    dvcs:out-files$ $@ dvcs:out-fileoff @ safe/string fsize umin
    2dup >file-hash 2r> 2swap  dvcs:fileentry$ $free
    [: forth:type ticks { 64^ ts } ts 1 64s forth:type forth:type ;]
    dvcs:fileentry$ $exec dvcs:fileentry$ $@
    2dup +fileentry  dvcs-outfile-hash
    fsize dvcs:out-fileoff +! ; dvcs-class is dvcs:write
' !!FIXME!! ( 64size algo addr u --- ) dvcs-class is dvcs:unzip
:noname ( addr u -- ) \ hash+perm+name
    0 patch-in$ !@ >r
    dvcs:fileentry$ $free
    [: over hash#128 forth:type ticks { 64^ ts } ts 1 64s forth:type
	hash#128 /string forth:type ;] dvcs:fileentry$ $exec
    dvcs:fileentry$ $@ +fileentry
    ?fileentry-hash
    patch-in$ $free  r> patch-in$ !
; dvcs-class is dvcs:ref

\ DVCS refs are scanned for in patchsets, and then fetched

' 2drop dvcs-refs is dvcs:read
' 2drop dvcs-refs is dvcs:rm
' 2drop dvcs-refs is dvcs:rmdir
:noname 2drop 64drop ; dup dvcs-refs is dvcs:patch
dvcs-refs is dvcs:write
:noname 2drop drop 64drop ; dvcs-refs is dvcs:unzip
:noname ( addr u -- )
    hash#128 umin 2dup dvcs-objects #@ d0<> IF  2drop  EXIT  THEN
    2dup enchash>filename file-status nip no-file# <> IF  2drop  EXIT  THEN
    dvcs:refs[] $+[]! ; dvcs-refs is dvcs:ref

scope{ dvcs
: new-dvcs ( -- o )
    dvcs-table dvcs-class new-tok >o
    msg-table commit-class new-tok  dvcs:commits !
    msg-table search-class new-tok  dvcs:searchs !
    o o> ;
: new-dvcs-refs ( -- o )
    dvcs-table dvcs-refs new-tok ;
: clean-delta ( o:dvcs -- )
    in-files$ $free out-files$ $free patch$ $free ;
: dispose-commit ( o:commit -- )
    id$ $free  re$ $free  object$ $free  dispose ;
: dispose-search ( o:commit -- )
    match:id$ $free  match:tag$ $free  dispose ;
: dispose-dvcs-refs ( o:dvcs -- )
    dvcs:refs[] $[]free dispose ;
: dispose-dvcs ( o:dvcs -- )
    dvcs:branch$ $free  dvcs:message$ $free  dvcs:fileref[] $[]free
    dvcs:files# #frees  dvcs:oldfiles# #frees
    dvcs:rmdirs[] $[]free  dvcs:outfiles[] $[]free
    clean-delta  dvcs:fileentry$ $free
    dvcs:hash$ $free
    dvcs:id$ $free  dvcs:oldid$ $free
    project:revision$ $free   project:chain$ $free
    project:branch$ $free  project:project$ $free
    dvcs:commits @ .dvcs:dispose-commit
    dvcs:searchs @ .dvcs:dispose-search
    dispose ;
}scope

Variable new-files[]
Variable ref-files[]
Variable del-files[]
Variable old-files[]
Variable new-file$
Variable branches[]

: clean-up ( -- )
    new-files[] $[]free  ref-files[] $[]free
    del-files[] $[]free  old-files[] $[]free
    branches[]  $[]free  new-file$ $free ;

User tmp1$
: $tmp1 ( xt -- ) tmp1$ $free  tmp1$ $exec  tmp1$ $@ ;

: mode@ ( -- mode )
    statbuf st_mode [ sizeof st_mode 2 = ] [IF] w@ [ELSE] l@ [THEN] ;
: mode! ( -- mode )
    statbuf st_mode [ sizeof st_mode 2 = ] [IF] w! [ELSE] l! [THEN] ;

$1000 Constant path-max#

Defer xstat ' lstat is xstat
Defer xfiles[] ' new-files[] is xfiles[]
Defer hash-import ' noop is hash-import

: ref-hash-import ( hash u -- hash u )
    2>r new-file$ $@ write-enc-hashed 2drop 2r> ;

: hashstat-rest ( addr u -- addr' u' )
    [: mode@ { | w^ perm } wle perm w!
	statbuf st_mtime ntime@ d>64 64#0 { 64^ timestamp } timestamp le-64!
	perm w@ wle S_IFMT? and  case
	    S_IFLNK of  path-max# new-file$ $!len \ pathmax: 4k
		2dup new-file$ $@ readlink
		dup ?ior new-file$ $!len  endof
	    S_IFREG of  2dup new-file$ $slurp-file  endof
	    0 new-file$ $!len
	endcase
	new-file$ $@ >file-hash 2dup type hash-import
	new-file$ $@ 2swap dvcs-objects #!
	timestamp 1 64s type  perm 2 type type
    ;] $tmp1 ;

: file-hashstat ( addr u -- addr' u' )
    2dup statbuf xstat ?ior  hashstat-rest ;

: $ins[]f ( addr u array -- ) [ hash#128 dvcs:name ]L $ins[]/ drop ;

: new-files-loop ( -- )
    BEGIN  refill  WHILE  \ source type cr
	    source file-hashstat xfiles[] $ins[]f  REPEAT ;
: new-files-in ( addr u -- )
    r/o open-file dup no-file# = IF  2drop  EXIT  THEN  throw
    ['] new-files-loop execute-parsing-file ;
: do-refs ( xt -- )
    [: stat mode@ $1000 or mode! ;] is xstat
    ['] ref-files[] is xfiles[]
    ['] ref-hash-import is hash-import
    catch
    ['] lstat is xstat
    ['] new-files[] is xfiles[]
    ['] noop is hash-import
    throw ;
: ref-hashstat ( addr u -- addr' u' )
    ['] file-hashstat do-refs ;
: ref-files-in ( addr u -- )
    ['] new-files-in do-refs ;

: config>dvcs ( o:dvcs -- )
    config-throw >r 0 to config-throw \ don't throw on config errors
    [: "~+/.n2o/config" ['] project >wordlist read-config ;] catch
    r> to config-throw throw \ throw on any other problem
    project:revision$ $@ base85>$ dvcs:oldid$ $! ;
: files>dvcs ( o:dvcs -- )
    "~+/.n2o/files" filelist-in ;
: new>dvcs ( o:dvcs -- )
    "~+/.n2o/reffiles" ref-files-in
    "~+/.n2o/newfiles" new-files-in ;
: mode<> ( mode1 mode2 -- flag )
    over S_IFMT invert and over S_IFMT invert and <> >r
    S_IFMT and swap S_IFMT and dup $1000 and IF
	$-2000 and swap dup S_IFLNK = IF  drop S_IFREG  THEN
    THEN  <> r> or ;
: dvcs?modified ( o:dvcs -- )
    dvcs:files# [: dup
	>r cell+ $@ drop hash#128 + dvcs:perm w@ wle { perm }
	r@ $@ statbuf perm $1000 and IF  stat  ELSE  lstat  THEN
	0< IF  errno ENOENT = IF
		r> [: dup cell+ $. $. ;] $tmp1 del-files[] $ins[]f
		EXIT  THEN  -1 ?ior  THEN
	r@ cell+ $@ drop hash#128 + dvcs:timestamp le-64@
	statbuf st_mtime ntime@ d>64 64<>
	perm mode@ mode<> or  IF
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
    ." ===" cr
    ) ;

: dvcs+in ( hash u -- )
    dvcs( ." read in: " 2dup 85type forth:cr )
    hash#128 umin dvcs-objects #@ over 0= !!wrong-hash!!
    dvcs:in-files$ $+! ;
: dvcs+out ( hash u -- )
    hash#128 umin dvcs-objects #@ over 0= !!wrong-hash!!
    dvcs:out-files$ $+! ;

: file-lsize@ ( addr u -- 64size )
    statbuf lstat ?ior statbuf st_size 64@
    mode@ S_IFMT? and S_IFDIR <> n>64 64and ;
: file-size@ ( addr u -- 64size )
    statbuf stat ?ior statbuf st_size 64@
    mode@ S_IFMT? and S_IFDIR <> n>64 64and ;

also net2o-base

: read-old-fs ( -- ) dvcs:in-files$ $free
    old-files[] [: hash#128 umin 2dup $, dvcs-read dvcs+in ;] $[]map ;
: read-del-fs ( -- )
    del-files[] [: over hash#128 dvcs:perm + w@ wle
	S_IFMT? and S_IFDIR =  IF  /name $, dvcs-rmdir
	ELSE 2dup [: over hash#128 forth:type /name forth:type ;] $tmp1 $,
	    dvcs-rm hash#128 umin dvcs+in  THEN ;] $[]map ;
: read-new-fs ( -- )
    new-files[] ['] dvcs+out $[]map ;
: dvcs+hash ( addr u -- ) 2drop ; \ !!STUB!!
: read-ref-fs ( -- )
    ref-files[] ['] dvcs+hash $[]map ;
: write-new-fs ( -- )
    new-files[] [: 2dup hash#128 dvcs:perm /string $,
	/name file-lsize@ lit, dvcs-write ;] $[]map ;
: write-ref-fs ( -- )
    ref-files[] [:
	[: over hash#128 forth:type
	    hash#128 dvcs:perm /string forth:type ;] $tmp $,
	dvcs-ref ;] $[]map ;
: compute-patch ( -- )
    dvcsfiles( ." ===== in-files$ ====" forth:cr dvcs:in-files$ $.
    ." ===== out-files$ =====" forth:cr dvcs:out-files$ $. )
    dvcs:in-files$ dvcs:out-files$ ['] bdelta$2 dvcs:patch$ $exec
    dvcs:patch$ $@ $, dvcs:out-files$ $@len ulit, dvcs-patch ;

: compute-diff ( -- )
    read-old-fs  read-del-fs  read-new-fs  read-ref-fs
    compute-patch  write-new-fs write-ref-fs ;

Variable id-files[]

: dvcs-gen-id ( -- addr u )
    id-files[] $[]free
    dvcs:files# [: dup cell+ $@ 2>r $@ 2r> [: forth:type forth:type ;] $tmp
	id-files[] $ins[] drop ;] #map \ sort filenames
    [:  id-files[] [: drop hash#128 $, dvcs-read ;] $[]map
	id-files[] [: hash#128 /string
	    0 dvcs:perm /string 2dup $,
	    2 /string file-lsize@ lit, dvcs-write
	;] $[]map ;] gen-cmd$
    dup IF  >file-hash  THEN ;

previous

: 85$! ( addr u $addr -- )
    ['] 85type swap $set ;

: save-project ( -- )
    dvcs( ." saving '" dvcs:id$ $@ 85type cr )
    dvcs:id$ $@ project:revision$ 85$!
    ".n2o/config+" ['] project >wordlist write-config
    ".n2o/config+" ".n2o/config" rename-file throw ;

\ init project

: dvcs-init ( project u -- )
    ".n2o" $1FF init-dir drop
    ".n2o/files" touch
    dvcs:new-dvcs >o
    '#' $split  dup 0= IF  2drop "master"  ELSE  2swap  THEN
    project:branch$ $!  project:project$ $!
    save-project  dvcs:dispose-dvcs o> ;

\ append a line

: append-line ( addr u file u -- )
    2dup w/o open-file dup no-file# = IF
	2drop w/o create-file throw  ELSE  throw nip nip  THEN
    dup >r file-size throw r@ reposition-file throw
    r@ write-line throw r> close-file throw ;

\ patch stuff

\ read in branches, new version

: hash+type ( addr u type addr1 -- ) dup >r $free
    [: { w^ x } type x cell type ;] r> $exec ;

' 2drop commit-class is msg:tag
' 2drop commit-class is msg:start
' 2drop commit-class is msg:coord
' 2drop commit-class is msg:signal
' 2drop commit-class is msg:text
' 2drop commit-class is msg:url
' 2drop commit-class is msg:action
' 2drop commit-class is msg:chain
' drop  commit-class is msg:like
' noop  commit-class is msg:end
' drop  commit-class is msg:redisplay

:noname ( addr u -- )
    re$ $+! ; commit-class is msg:re
:noname ( addr u -- )
    id$ $! re$ $free ; commit-class is msg:id
:noname ( addr u type -- )
    object$ hash+type
    object$ $@ key| id$ $@
    id>patch# id>snap# re$ $@len select #!
    re$ $@len IF
	re$ $@ last# cell+ $+!
    THEN ; commit-class is msg:object

\ search for a specific id

' 2drop search-class is msg:start
' 2drop search-class is msg:coord
' 2drop search-class is msg:signal
' 2drop search-class is msg:text
' 2drop search-class is msg:action
' 2drop search-class is msg:chain
' 2drop search-class is msg:re
' 2drop search-class is msg:url
' noop  search-class is msg:end
' drop  search-class is msg:like
' drop  search-class is msg:redisplay

: 3drop  2drop drop ;

:noname match:tag$ $@ str= match:flag ! ; search-class is msg:tag
:noname match:flag @ IF  match:id$ $!  ELSE  2drop  THEN ; search-class is msg:id
' 3drop search-class is msg:object

' 2drop dvcs-log-class is msg:re
' 2drop dvcs-log-class is msg:coord
' 3drop dvcs-log-class is msg:object
' noop  dvcs-log-class is msg:end
:noname dvcs-log:sig$    $! ; dvcs-log-class is msg:start
:noname dvcs-log:tag$    $! ; dvcs-log-class is msg:tag
:noname dvcs-log:id$     $! ; dvcs-log-class is msg:id
:noname dvcs-log:text$   $! ; dvcs-log-class is msg:text
:noname dvcs-log:action$ $! ; dvcs-log-class is msg:action
:noname dvcs-log:chain$  $! ; dvcs-log-class is msg:chain
:noname dvcs-log:urls[] $+[]! ; dvcs-log-class is msg:url
' drop dvcs-log-class is msg:like
' drop dvcs-log-class is msg:redisplay

: chat>dvcs ( o:dvcs -- )
    project:project$ $@ @/ 2drop load-msg ;
: .hash ( addr -- )
    [: dup $@ 85type ."  -> " cell+ $@ 85type cr ;] #map ;
: chat>branches-loop ( o:commit -- )
    msg-log@ over { log } bounds ?DO
	re$ $free  object$ $free
	I $@ ['] msg:display catch IF  ." invalid entry" cr 2drop THEN
    cell +LOOP  log free throw
    dvcs( ." === id>patch ===" cr id>patch# .hash
    ." === id>snap ===" cr id>snap# .hash ) ;
: chat>branches ( o:dvcs -- )
    project:project$ $@ @/ 2drop >group  dvcs:commits @ .chat>branches-loop ;

: >branches ( addr u -- )
    $make branches[] >back ;
User id-check# \ check hash
: id>branches-loop ( addr u -- )
    BEGIN  2dup id-check# #@ d0<> IF  2drop  EXIT  THEN
	s" !" 2over id-check# #!
	2dup id>snap# #@ 2dup d0<> IF  >branches 2drop  EXIT  THEN
	2drop id>patch# #@ 2dup d0<> WHILE
	    2dup hash#128 umin >branches
	    hash#128 safe/string  hash#128 - 2dup + >r
	    bounds U+DO  I hash#128 recurse  hash#128 +LOOP
	    r> hash#128 \ tail recursion optimization
    REPEAT  2drop ;
: id>branches ( addr u -- )
    id-check# #frees
    branches[] $[]free  dvcs:commits @ .id>branches-loop
    id-check# #frees
    dvcs( ." re:" cr branches[] [: 85type cr ;] $[]map ) ;
: branches>dvcs ( -- )
    branches[] [: dup IF
	    dvcs( ." read enc hash: " 2dup 85type cr )
	    ?read-enc-hashed  c-state off  do-cmd-loop
	    dvcs:clean-delta
	ELSE  2drop  THEN
    ;] $[]map ;

\ push out a revision

: >revision ( addr u -- )
    2dup >file-hash dvcs:hash$ $!
    dvcs( ." ===== ci '" dvcs:hash$ $@ 85type ." ' =====" cr )
    write-enc-hashed 2drop ;

: pull-readin ( -- )
    config>dvcs  chat>dvcs  chat>branches ;
: dvcs-readin ( $addr -- )
    pull-readin  $@ id>branches  branches>dvcs
    files>dvcs  new>dvcs  dvcs?modified ;

scope{ dvcs
: new-dvcs-log ( -- o )
    msg-table dvcs-log-class new-tok ;
: clear-log ( -- )
    dvcs-log:sig$    $free  dvcs-log:tag$  $free  dvcs-log:id$    $free
    dvcs-log:action$ $free  dvcs-log:text$ $free  dvcs-log:chain$ $free
    dvcs-log:urls[] $[]free ;
: dispose-dvcs-log ( o:log -- )
    clear-log dispose ;
}scope

: display-logn ( addr u n -- )
    project:branch$ $@ { d: branch }
    dvcs:new-dvcs-log >o
    cells >r >group  msg-log@ 2dup { log u }
    dup r> - 0 max dup >r /string r> cell/ -rot bounds ?DO
	dvcs:clear-log  I $@ ['] msg:display catch
	IF  ." invalid entry" cr 2drop
	ELSE
	    branch dvcs-log:tag$ $@ str= IF
		dup 0 .r ." : [" dvcs-log:id$ $@ 85type ." ] "
		dvcs-log:sig$ $@ 2dup startdate@ .ticks
		dvcs-log:chain$ $@ dup IF
		    2dup sighash? IF  <info>  ELSE  <err>  THEN
		    ." <-" drop le-64@ .ticks  <default>
		ELSE  2drop  THEN  space
		dvcs-log:action$ $. ." : " dvcs-log:text$ $. space
		dvcs-log:urls[] [: <warn> type <default> space ;] $[]map
		.key-id?
		cr
	    THEN
	THEN  1+
    cell +LOOP  drop
    log free dvcs:dispose-dvcs-log o> throw ;

: .dvcs-log ( -- )
    dvcs:new-dvcs >o  config>dvcs
    project:project$ $@ @/ 2drop 2dup load-msg
    config:logsize# @ display-logn
    dvcs:dispose-dvcs o> ;

also net2o-base
true Value add-object?

: (dvcs-newsentry) ( type -- )
    dvcs:type !
    dvcs:hash$ $@len 0= IF  #0. last-signed 2!  EXIT  THEN
    msg-group$ @ >r
    project:project$ @ msg-group$ ! msg-group$ $@ >group
    o [: with dvcs
	project:chain$ $@ base85>$
	fileref[]
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
	dup >r dup IF  $, ulit, msg-object
	ELSE  2drop drop  THEN
	r> r> IF   IF  "Patchset"  ELSE  "Revert"  THEN
	ELSE  drop "Snapshot"  THEN  $, msg-action
	$, msg-text
	dup [: [: ." file:" forth:type ;] $tmp $, msg-url ;] $[]map $[]free
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
    IF  >revision  ELSE  >file-hash dvcs:hash$ $!  THEN ;

: ?delete-file ( addr u -- )
    delete-file dup no-file# <> and throw ;

: dvcs-sig$ ( -- )
    last-signed 2@ sigdate@ over 1 64s 85type 2 64s /string
    c:0key >hash hashtmp hash#128 85type ;
 
: (dvcs-ci) ( addr u o:dvcs -- ) dvcs:message$ $!
    dvcs:oldid$ dvcs-readin
    ref-files[] $[]# new-files[] $[]# del-files[] $[]# or or 0= IF
	." Nothing to do" cr
    ELSE
	['] compute-diff gen-cmd$
	$make { w^ diff$ } diff$ $@ \ 2dup net2o:see
	dvcs( ." ===== patch len: " dvcs:patch$ $@len . ." =====" cr )
	del-files[] ['] -fileentry $[]map
	new-files[] ['] +fileentry $[]map
	ref-files[] ['] +fileentry $[]map
	>id-revision  my-key >r
	my-key-default to my-key  dvcs-newsentry
	r> to my-key
	['] dvcs-sig$ project:chain$ $set
	save-project  filelist-out
	"~+/.n2o/newfiles" ?delete-file
	"~+/.n2o/reffiles" ?delete-file
	diff$ $free
    THEN  clean-up ;

: dvcs-ci ( addr u -- ) \ checkin command
    dvcs:new-dvcs >o now>never (dvcs-ci)  dvcs:dispose-dvcs o> ;

: dvcs-diff ( -- )
    dvcs:new-dvcs >o dvcs:oldid$ dvcs-readin
    ['] compute-diff gen-cmd$ 2drop
    dvcs( ." ===== diff len: " dvcs:patch$ $@len . ." =====" cr )
    dvcs:in-files$ dvcs:patch$ color-bpatch$2
    clean-up  dvcs:dispose-dvcs o> ;

: ci-args ( -- message u )
    ?nextarg 0= IF "untitled checkin" THEN
    2dup "-m" str= IF  ?nextarg IF  2nip  THEN  THEN ;

: dvcs-add ( addr u -- )
    2dup dirname dup 0<> + dup IF  recurse  ELSE  2drop  THEN
    2dup dvcs:files# #@ drop IF  2drop  EXIT
    ELSE
	\ "dummy128dummy128dummy128dummy128" 2over dvcs:files# #!
	"~+/.n2o/newfiles" append-line  THEN ;

: dvcs-ref ( addr u -- )
    2dup dirname dup 0<> + dup IF  dvcs-add  ELSE  2drop  THEN
    2dup dvcs:files# #@ drop IF  2drop  EXIT
    ELSE  "~+/.n2o/reffiles" append-line  THEN ;

: dvcs-snap ( addr u -- )
    dvcs:new-dvcs >o  dvcs:message$ $!
    config>dvcs  files>dvcs
    dvcs:files# [:
	dup cell+ $@ drop hash#128 + dvcs:perm w@ wle $1000 and
	IF    $@  ref-hashstat ref-files[]
	ELSE  $@ file-hashstat new-files[]  THEN
	$ins[]f ;] #map
    ['] compute-diff gen-cmd$ >id-revision
    now>never  dvcs-snapentry
    save-project  clean-up dvcs:dispose-dvcs o> ;

: del-oldfile ( hash-entry -- )
    dup cell+ $@ drop hash#128 dvcs:perm + w@ wle
    S_IFMT? and S_IFDIR = IF
	$@ dvcs( ." rd " 2dup type cr ) dvcs:rmdirs[] $ins[] drop
    ELSE  dup $@ dvcs( ." rm " 2dup type cr )
	delete-file dup 0< IF
	    <warn> >r ." can't delete file " $. ." : "
	    r> error$ type <default> cr
	ELSE  2drop  THEN
    THEN ;

: new->old ( -- ) dvcs( ." === remove old files ===" cr )
    dvcs:rmdirs[] $[]free
    dvcs:oldfiles# [: dup $@ dvcs:files# #@ drop 0= IF
	    del-oldfile
	ELSE  dup cell+ $@ last# cell+ $@ str= 0= IF
		del-oldfile  THEN
	THEN ;] #map
    dvcs:rmdirs[] $@ bounds cell- swap cell- U-DO
	I $@ rmdir 0< IF
	    errno strerror <warn>
	    ." can't delete directory " I $. ." : " type <default> cr
	THEN
    cell -LOOP
    dvcs:rmdirs[] $[]free ;
: old->new ( -- ) dvcs( ." === write out new files ===" cr )
    dvcs:outfiles[] $[]free
    dvcs:files# [: $@ dvcs:outfiles[] $ins[] drop ;] #map \ sort filenames
    dvcs:outfiles[] [: dvcs:files# #@ d0<> IF
	    last# dup >r $@ dvcs:oldfiles# #@ over IF
		r@ cell+ $@ str=
	    ELSE  drop
	    THEN  0= IF
		dvcs( ." out " r@ $. space r@ cell+ $@ 85type cr )
		r@ cell+ $@ r@ $@ dvcs-outfile-name
	    THEN  rdrop  THEN ;] $[]map
    dvcs:outfiles[] $[]free ;

: co-rest ( -- )
    0 dvcs:files# !@ dvcs:oldfiles# !
    branches>dvcs  new->old  old->new
    save-project  filelist-out ;

: dvcs-co ( addr u -- ) \ checkout revision
    base85>$  dvcs:new-dvcs >o
    config>dvcs   dvcs:id$ $! dvcs:id$  dvcs-readin  co-rest
    dvcs:dispose-dvcs o> ;

: chat>searchs-loop ( o:commit -- )
    msg-log@ over { log } bounds ?DO
	I $@ ['] msg:display catch IF  ." invalid entry" cr 2drop THEN
    cell +LOOP  log free throw ;
: search-last-rev ( -- addr u )
    project:project$ $@ @/ 2drop >group
    project:branch$ $@
    dvcs:searchs @ >o match:tag$ $!
    chat>searchs-loop match:id$ $@ o> ;

: dvcs-up ( -- ) \ checkout latest revision
    dvcs:new-dvcs >o
    pull-readin  files>dvcs  new>dvcs  dvcs?modified
    new-files[] $[]# del-files[] $[]# d0= IF
	search-last-rev  2dup dvcs:id$ $!
	2dup dvcs:oldid$ $@ str= IF
	    2drop ." already up to date" cr
	ELSE  id>branches  co-rest  THEN
    ELSE
	." Local changes, don't update" cr
    THEN
    dvcs:dispose-dvcs o> ;

: dvcs-revert ( -- ) \ restore to last revision
    dvcs:new-dvcs >o
    pull-readin  dvcs:oldid$ $@  2dup dvcs:id$ $!
    id>branches  co-rest
    dvcs:dispose-dvcs o> ;

hash#128 buffer: hash-save

: hash-in ( addr u -- hash u )
    2dup >file-hash hash-save hash#128 smove
    write-enc-hashed 2drop
    hash-save hash#128 ;
: hash-add ( addr u -- )
    slurp-file over >r hash-in 2drop r> free throw ;
: hash-out ( addr u -- )
    base85>$ 2dup 2>r read-enc-hashed patch-in$ $@ 2r> hash-85 spit-file ;
: hash-rm ( addr u -- )
    base85>$ enchash>filename delete-file drop ;

\ pull and sync a database

$B $E 2Value dvcs-bufs#

Variable dvcs-request#
Variable sync-file-list[]
$10 Constant /sync-files
$20 /sync-files * Constant /sync-reqs

: dvcs-sync-none ( -- )
    -1 dvcs-request# +!@ 0<= IF  dvcs-request# off  THEN ;

event: :>dvcs-sync-done ( o -- ) >o
    file-reg# off  file-count off
    msg-group$ $@ >group ?save-msg  0 dvcs-request# !
    msg( ." === metadata sync done ===" forth:cr ) o> ;

: dvcs-sync-done ( -- )
    msg( ." dvcs-sync-done" forth:cr )
    net2o:close-all
    msg( ." dvcs-sync-done closed" forth:cr )
    <event o elit, :>dvcs-sync-done wait-task @ event> ;

: +dvcs-sync-done ( -- )
    ['] dvcs-sync-done is sync-done-xt
    ['] dvcs-sync-none is sync-none-xt ;

also net2o-base
: dvcs-join, ( -- )
    [: msg-join last?, ;] [msg,] ;
previous

: dvcs-greet ( -- )
    net2o-code expect-msg
    log !time end-with dvcs-join, get-ip end-code ;

: dvcs-connect ( addr u -- )
    dvcs-bufs# chat#-connect? IF  2 dvcs-request# !  dvcs-greet  THEN ;

: dvcs-connect-key ( addr u -- )
    key>group ?load-msgn
    2dup search-connect ?dup-IF  >o +group rdrop 2drop  EXIT  THEN
    \ check for disconnected here or in pk-peek?
    2dup pk-peek?  IF  dvcs-connect  ELSE  2drop  THEN ;

: dvcs-connects? ( -- flag )
    chat-keys ['] dvcs-connect-key $[]map dvcs-request# @ 0> ;

: wait-dvcs-request ( -- )
    BEGIN  dvcs-request# @  WHILE  stop  REPEAT ;

: need-hashed? ( addr u -- flag )
    enchash>filename file-status nip no-file# = ;

: +needed ( addr u -- )
    2dup need-hashed? IF
	dvcs( ." need: " 2dup 85type cr )
	sync-file-list[] $ins[] drop
    ELSE  dvcs( ." don't need: " 2dup 85type cr ) 2drop  THEN ;

: #needed ( hash -- )
    cell+ $@ key| +needed ;
: dvcs-needed-files ( -- )
    id>patch# ['] #needed #map
    id>snap#  ['] #needed #map ;

: get-needed-files { list -- } +resend
    list $[]# 0 ?DO
	I /sync-reqs + I' umin I U+DO
	    net2o-code expect+slurp
	    $10 blocksize! 0 blockalign!
	    I /sync-files + I' umin I U+DO
		I list $[]@ net2o:copy#
	    LOOP
\	    I /sync-files + I' u>= IF
	    end-code| net2o:close-all -map-resend
	    I /sync-files + I' u>= IF
		+resend +flow-control
		net2o-code expect+slurp  close-all  ack rewind end-with
		[ previous ]
	    THEN
\	    ELSE  end-code  THEN
	/sync-files +LOOP
    /sync-reqs +LOOP ;

: dvcs-data-sync ( -- )
    sync-file-list[] $[]free  branches[] $[]free
    msg-group$ $@ >group
    dvcs:commits @ .chat>branches-loop
    dvcs:commits @ .dvcs-needed-files
    sync-file-list[] $[]# 0> connection and
    IF    sync-file-list[] connection .get-needed-files  THEN ;

: dvcs-ref-sync ( -- )
    search-last-rev id>branches
    dvcs:new-dvcs-refs >o
    branches>dvcs
    dvcs:refs[] $[]# 0 ?DO
	." ref: " I dvcs:refs[] $[]@ 85type cr  LOOP
    dvcs:refs[] $[]# 0> connection and
    IF  dvcs:refs[] connection .get-needed-files  THEN
    dvcs:dispose-dvcs-refs o> ;

: handle-fetch ( -- )  ?.net2o/objects
    dvcs:new-dvcs >o  pull-readin
    msg( ." === syncing metadata ===" forth:cr )
    0 >o dvcs-connects? IF  +dvcs-sync-done  wait-dvcs-request  THEN o>
    msg( ." === syncing data ===" forth:cr )
    dvcs-data-sync
    msg( ." === data sync done ===" forth:cr )
    dvcs-ref-sync
    msg( ." === ref sync done ===" forth:cr )
    connection ?dup-IF
	.data-rmap IF  msg-group$ $@ >group last# silent-leave-chat  THEN
    THEN
    dvcs:dispose-dvcs o> ;

: handle-clone ( -- )
    0 chat-keys !@ { w^ clone-keys }
    clone-keys [: >dir  2dup chat-keys $+[]!
	[: @/ 2swap
	    '#' $split dup 0= IF  2drop  ELSE  2nip  THEN
	    2dup $1FF init-dir drop 2dup set-dir throw
	    [: type '@' emit .key-id? ;] $tmp dvcs-init
	    handle-fetch dvcs-up ;] catch
	chat-keys $[]free  dir>  throw
    ;] $[]map
    clone-keys @ chat-keys ! ;

\\\
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
