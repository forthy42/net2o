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

msg-class class
    field: branch$
    field: files[] \ snapshot config
    field: objects# \ object hash
    field: in-files$
    field: patch$
    field: out-files$
    field: out-fileoff
    field: fileentry
end-class dvcs-class

begin-structure
    $40 +field dvcs-hash
    64field: dvcs-timestamp
    field: dvcs-perm
    0 +field: dvcs-name
end-structure

: hash>filename ( addr u -- addr' u' )
    0. 2swap files[] [: 2over $40 umin 2over str= IF
	  0 dvcs-name /string 2>r 2nip 2r> 2swap
	THEN ;] $[]map 2nip ;

: search-files[] ( -- n )
    -1 0 files[] [: 0 dvcs-name /string fileentry $@ 0 dvcs-name /string str=
      IF  nip dup  THEN  1+ ;] $[]map  drop ;

: +fileentry ( o:dvcs -- )
    \G add a file entry and replace same file if it already exists
    fileentry $@ search-files[]
    dup 0>= IF files[] $[]!
    ELSE  drop files[] $+[]!  THEN ;

: hash>entry ( size o:dvcs -- )
    >r out-files$ $@ out-fileoff @ safe/string r> umin c:0key c:hash
    fileentry $40 $!len fileentry $@ c:hash@ ;

scope{ net2o-base

\g 
\g ### DVCS commands ###
\g 

dvcs-table >table

msg-table $@ inherit-table dvcs-table

+net2o: dvcs-commit ( $:branch -- ) \g start a commit to branch
    $> branch$ $! ;
+net2o: dvcs-read ( $:hash -- ) \g read in an object
    $> hash>filename in-files$ $+slurp-file ;
+net2o: dvcs-patch ( $:diff -- ) \g apply patch
    $> patch$ $! out-fileoff off
    in-files$ patch$ ['] bdelta$2 out-files$ $exec ;
+net2o: dvcs-write ( perm timestamp size $:name -- ) \g write out file
    64>n { 64^ timestamp fsize } 64>n { w^ perm }
    fsize >hash
    timeestamp 1 64s fileentry $+!
    perm cell fileentry $+!
    $> 2dup fileentry $+!
    r/w create-file throw { fd }
    fd perm fchmod ?ior
    out-files$ $@ out-fileoff @ safe/string fsize umin fd write-file throw
    timestamp 64>d 1000000000 um/mod { d^ ts-ns }
    fd ts-ns futimens ?ior
    fd close-file throw
    +fileentry fsize out-fileoff +! ;
+net2o: dvcs-del ( $:name -- ) \g delete file
    $> delete-file throw ;

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