\ log dump class

\ Copyright (C) 2011-2014   Bernd Paysan

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

reply-table $@ inherit-table log-table

get-current also net2o-base definitions
\g 
\g ### log commands ###
\g 
net2o' token net2o: log-token ( $:token n -- )
    64>n 0 .r ." :" $> F type space ;

$20 net2o: emit ( utf8 -- ) \ emit character on server log
    64>n xemit ;
+net2o: type ( $:string -- ) \ type string on server log
    $> F type ;
+net2o: cr ( -- ) \ newline on server log
    F cr ;
+net2o: . ( n -- ) \ print number on server log
    64. ;
+net2o: f. ( r -- ) \ print fp number on server log
    F f. ;
+net2o: .time ( -- ) \ print timer to server log
    F .time .packets profile( .times ) ;
+net2o: !time ( -- ) \ start timer
    F !time init-timer ;

gen-table $freeze
' context-table is gen-table

$32 net2o: log ( -- o:log )
    log-context @ dup 0= IF
	drop  n2o:new-log dup log-context !
    THEN  n:>o ;
log-table >table

previous set-current

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z\-0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
     (("[:") (0 . 1) (0 . 1) immediate)
     ((";]") (-1 . 0) (0 . -1) immediate)
    )
End:
[THEN]