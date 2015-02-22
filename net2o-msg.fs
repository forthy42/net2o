\ messages                                           06aug2014py

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

get-current also net2o-base definitions

$34 net2o: msg ( -- o:msg ) \ push a message object
    msg-context @ dup 0= IF
	drop  n2o:new-msg dup msg-context !
    THEN
    n:>o buf-state 2@ drop c-buf ! ;

msg-table >table

reply-table $@ inherit-table msg-table

net2o' emit net2o: msg-text ( $:msg -- ) \ specify message string
    $> F type ;
+net2o: msg-object ( $:hash -- ) \ specify an object, e.g. an image
    $> ." wrapped object: " 85type F cr ;
+net2o: msg-sig ( $:signature -- ) \ detached signature
    $>- c:0key c:hash
    keysize 2* <> !!keysize!!
    parent @ .pubkey $@ keysize <> !!keysize!!
    date-sig? .check 2drop F cr ;

gen-table $freeze
' context-table is gen-table

set-current

: <msg ( -- )
    \G start a msg block
    msg cmdbuf$ + c-buf ! ;
: msg> ( -- )
    \G end a msg block by adding a signature
    c-buf @ cmdbuf$ + over - now>never
    hash-sig $, msg-sig endwith ;

previous

: send-text ( addr u -- )
    net2o-code  expect-reply
    <msg $, msg-text msg>
    cookie+request end-code| ;

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