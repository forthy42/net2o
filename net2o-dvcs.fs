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

Variable dvcs-table

scope: dvcs

msg-class class
    field: branch$
    field: message$
    field: files[] \ snapshot config
    field: objects# \ object hash
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

: hash>filename ( addr u -- addr' u' )
    0. 2swap dvcs:files[] [: 2over $40 umin 2over str= IF
	  $40 dvcs:name /string 2>r 2nip 2r> 2swap
	THEN ;] $[]map 2nip ;

: search-files[] ( -- n )
    -1 0 dvcs:files[] [: $40 dvcs:name /string
      dvcs:fileentry $@ $40 dvcs:name /string str=
      IF  nip dup  THEN  1+ ;] $[]map  drop ;

: +fileentry ( o:dvcs -- )
    \G add a file entry and replace same file if it already exists
    dvcs:fileentry $@ search-files[]
    dup 0>= IF dvcs:files[] $[]!
    ELSE  drop dvcs:files[] $+[]!  THEN ;

: hash>entry ( size o:dvcs -- )
    >r dvcs:out-files$ $@ dvcs:out-fileoff @ safe/string r> umin c:0key c:hash
    dvcs:fileentry $40 $!len dvcs:fileentry $@ c:hash@ ;

: dvcs-outfile ( baddr u1 fname u2 -- )
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
    8 !!>=order? $> hash>filename dvcs:in-files$ $+slurp-file ;
+net2o: dvcs-patch ( $:diff -- ) \g apply patch
    $10 !!>order? $> dvcs:patch$ $! dvcs:out-fileoff off
    dvcs:in-files$ dvcs:patch$ ['] bdelta$2 dvcs:out-files$ $exec ;
+net2o: dvcs-del ( $:name -- ) \g delete file
    $20 !!>=order? $> delete-file throw ;
+net2o: dvcs-write ( size $:ts+perm+name -- ) \g write out file
    $40 !!>=order? 64>n { fsize }
    fsize >hash
    dvcs:out-files$ $@ dvcs:out-fileoff @ safe/string fsize umin
    $> 2dup dvcs:fileentry $+!  dvcs-outfile
    +fileentry fsize dvcs:out-fileoff +! ;

}scope

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