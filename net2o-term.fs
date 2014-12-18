\ terminal                                           06aug2014py

\ Copyright (C) 2014   Bernd Paysan

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

: collect-keys ( -- )
    BEGIN  key?  WHILE  key key-buf$ c$+!  REPEAT ;

get-current also net2o-base definitions

term-table >table

fs-table $@ inherit-table term-table

+net2o: at-xy ( x y -- ) F at-xy ;
+net2o: set-form ( w h -- ) term-h ! term-w ! ;
+net2o: get-form ( -- ) form swap lit, lit, set-form ;

gen-table $freeze
' context-table is gen-table

$35 net2o: terminal ( -- o:terminal ) \ push a terminal object
    term-context @ dup 0= IF
	drop  n2o:new-term dup term-context !
    THEN  n:>o ;
term-table >table

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