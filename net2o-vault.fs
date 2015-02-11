\ encrypted files                                    10feb2015py

\ Copyright (C) 2015   Bernd Paysan

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

$35 net2o: vault ( -- o:vault ) \ push a vault object
    vault-context @ dup 0= IF
	drop  n2o:new-vault dup vault-context !
    THEN
    n:>o buf-state 2@ vault-buf 2! ;

Defer do-decrypted ( addr u -- ) \ what to do with a decrypted file

vault-table >table

reply-table $@ inherit-table vault-table

net2o' emit net2o: dhe ( $:pubkey -- ) \ start diffie hellman exchange
    $> keysize <> !!keysize!! skc v-dhe ed-dhv 2drop
    v-key keysize erase ;
+net2o: vault-keys ( $:keys -- ) $> bounds ?DO
	I' I - state# u>= IF
	    I state# vaultkey move
	    vaultkey state# v-dhe keysize decrypt$ IF
		@keccak v-kstate keccak# move \ keep for signature
		dup keysize <> !!keysize!! v-key move
	    ELSE  2drop  THEN
	THEN
    state# +LOOP ;
+net2o: vault-file ( $:content -- )
    $> v-key keysize decrypt$ 0= !!no-decrypt!!
    do-decrypted ;
+net2o: vault-sig ( $:sig -- )
    $> v-key keysize decrypt$ 0= !!no-decrypt!!
    v-kstate @keccak keccak# move
    over >r $20 /string dup sigsize# <> !!wrong-sig!!
    >date r> verify-sig !!wrong-sig!! 2drop ;

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