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

require net2o.fs

Variable vault-table

cmd-class class
    KEYBYTES +field v-dhe \ diffie hellman exchange tmpkey
    KEYBYTES +field v-key \ file vault key
    keccak# +field v-kstate
    2field: v-data
end-class vault-class

: >vault ( -- o:vault ) \ push a vault object
    vault-class new n:>o vault-table @ token-table ! ;

Defer do-decrypted ( addr u -- ) \ what to do with a decrypted file

vault-table >table

get-current also net2o-base definitions

cmd-table $@ inherit-table vault-table

net2o' emit net2o: dhe ( $:pubkey -- ) \ start diffie hellman exchange
    $> keysize <> !!keysize!! skc v-dhe ed-dhv 2drop
    v-key keysize erase ;
+net2o: vault-keys ( $:keys -- ) $> bounds ?DO
	I' I - state# u>= IF
	    I state# vaultkey move
	    vaultkey state# v-dhe keysize decrypt$ IF
		dup keysize <> !!keysize!! v-key move
	    ELSE  2drop  THEN
	THEN
    state# +LOOP ;
+net2o: vault-file ( $:content -- )
    $> v-key keysize decrypt$ 0= !!no-decrypt!!
    @keccak v-kstate keccak# move \ keep for signature
    v-data 2! ;
+net2o: vault-sig ( $:sig -- )
    $> v-key keysize decrypt$ 0= !!no-decrypt!!
    v-kstate @keccak keccak# move
    over >r $20 /string dup sigsize# <> !!wrong-sig!!
    >date r> verify-sig !!wrong-sig!! 2drop ;

gen-table $freeze
' context-table is gen-table

set-current

cmd-buf-c class
    cell uvar cmd$
    1 pthread-mutexes uvar cmd$lock
end-class cmd-buf$

cmd-buf$ new cmdbuf: code-buf$

code-buf$

' cmd$lock to cmdlock
:noname  cmd$ $@ cmdbuf# @ umin ; to cmdbuf$
' true to maxstring \ really maxuint = -1 = true
:noname ( u -- ) cmdbuf# @ + cmd$ $!len ; to ?cmdbuf
:noname ( -- 64dest ) 64#0 ; to cmddest

code0-buf \ reset default

Variable enc-filename
Variable enc-file

KEYBYTES 4 64s + buffer: keygenbuf
KEYBYTES buffer: keygendh
KEYBYTES buffer: vkey

: vdhe, ( -- )  gen-tmpkeys $, dhe ;
: vkeys, ( key-list -- )
    KEYBYTES rng$ vkey swap move
    [: [: drop tskc swap keygendh ed-dh
	vkey keygenbuf $10 + KEYBYTES move
	keygenbuf $40 keygendh KEYBYTES encrypt$
	keygenbuf $40 type ;] $[]map ;] $tmp
    $, vault-keys ;
: vfile, ( -- )
    enc-filename $@ enc-file $slurp-file
    "                " enc-file 0 $ins \ add space for iv
    "                " enc-file $+!    \ add space for checksum
    enc-file $@ vkey KEYBYTES encrypt$
    enc-file $@ $, vault-file ;
: vsig, ( -- ) [: pkc KEYBYTES type now>never .sig
    ;] $tmp $, vault-sig ;

: encryt-file ( filename u key-list -- )  code-buf$
    >r enc-filename $!  pkc KEYBYTES r@ $+[]! \ encrypt for ourself
    vdhe, r> vkeys, vfile, vsig,
    s" .v2o" enc-filename $+!  code0-buf ;

Defer write-decrypt
: write-1file ( -- ) enc-file dup $@len 4 - 0 max 4 $del
    enc-file $@ w/o create-file throw >r
    v-data 2@ r@ write-file throw r> close-file throw ;
' write-1file is write-decrypt

: decrypt-file ( filename u -- )  enc-filename $!
    enc-filename $@ enc-file $slurp-file
    enc-file $@ do-cmd-loop
    write-decrypt ;

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