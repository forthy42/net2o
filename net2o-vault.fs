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
    field: v-state
end-class vault-class

: >vault ( -- o:vault ) \ push a vault object
    vault-class new n:>o vault-table @ token-table ! v-state off ;

Defer do-decrypted ( addr u -- ) \ what to do with a decrypted file

vault-table >table

get-current also net2o-base definitions

cmd-table $@ inherit-table vault-table

net2o' emit net2o: dhe ( $:pubkey -- ) v-state @ !!inv-order!!
    \ start diffie hellman exchange
    $> keysize <> !!keysize!! skc swap v-dhe ed-dh 2drop
    v-key keysize erase 1 v-state or! ;
+net2o: vault-keys ( $:keys -- ) v-state @ 1 <> !!no-tmpkey!!
    $> bounds ?DO
	I' I - $40 u>= IF
	    I vaultkey $40 move
	    vaultkey $40 v-dhe keysize decrypt$ IF
		dup keysize <> !!keysize!! v-key swap move
	    ELSE  2drop  THEN
	THEN
    $40 +LOOP 2 v-state or! ;
+net2o: vault-file ( $:content -- ) v-state @ 3 <> !!no-tmpkey!!
    v-key keysize >crypt-key $> 2dup c:decrypt v-data 2!
    @keccak v-kstate keccak# move 4 v-state or! ; \ keep for signature
+net2o: vault-sig ( $:sig -- ) v-state @ 7 <> !!no-data!!
    $> v-key keysize decrypt$ 0= !!no-decrypt!!
    v-kstate @keccak keccak# move
    verify-tag 0= !!wrong-sig!! 2drop 8 v-state or! ;

gen-table $freeze
' context-table is gen-table

set-current

cmd-buf-c class
    cell uvar cmd$
    1 pthread-mutexes uvar cmd$lock
end-class cmd-buf$

cmd-buf$ new cmdbuf: code-buf$

code-buf$
cmd$lock 0 pthread_mutex_init drop

' cmd$lock to cmdlock
:noname  cmd$ $@ ; to cmdbuf$
:noname  cmd$ $off ; to cmdreset
' true to maxstring \ really maxuint = -1 = true
:noname ( addr u -- ) cmd$ $+! ; to +cmdbuf
:noname ( -- 64dest ) 64#0 ; to cmddest

code0-buf \ reset default

Variable enc-filename
Variable enc-file

keysize 4 64s + buffer: keygenbuf
keysize buffer: keygendh
keysize buffer: vkey
keysize buffer: vpk
keysize buffer: vsk

: vdhe, ( -- )   vsk vpk ed-keypair vpk keysize $, dhe ;
: vkeys, ( key-list -- )
    keysize rng$ vkey swap move
    [: [: drop vsk swap keygendh ed-dh 2>r
	vkey keygenbuf $10 + keysize move
	keygenbuf $40 2r> encrypt$
	keygenbuf $40 F type ;] $[]map ;] $tmp
    $, vault-keys ;
: vfile, ( -- )
    enc-filename $@ enc-file $slurp-file
    vkey keysize >crypt-key enc-file $@ c:encrypt
    enc-file $@ $, vault-file ;
: vsig, ( -- )
    [: $10 spaces now>never .pk .sig $10 spaces ;] $tmp
    2dup vkey keysize encrypt$ $, vault-sig ;

: encrypt-file ( filename u key-list -- )  code-buf$
    >r enc-filename $!  pkc keysize r@ $+[]! \ encrypt for ourself
    vdhe, r> vkeys, vfile, vsig,
    s" .v2o" enc-filename $+!
    enc-filename $@ w/o create-file throw >r
    cmd$ $@ r@ write-file throw r> F close-file throw
    code0-buf ;

Defer write-decrypt
: write-1file ( -- ) enc-filename $@ dup 4 - 0 max safe/string s" .v2o" str=
    IF  enc-filename dup $@len 4 - 4 $del  THEN
    enc-filename $@ w/o create-file throw >r
    v-data 2@ r@ write-file throw r> F close-file throw ;
' write-1file is write-decrypt

: decrypt-file ( filename u -- )
    enc-filename $!
    enc-filename $@ enc-file $slurp-file
    enc-file $@ >vault do-cmd-loop
    v-state @ $F = IF write-decrypt THEN n:o> ;

\ define key lists

Variable vkey-list

: vpks-off ( -- ) vkey-list $[]off ;
: +pk ( "name" -- )  pk' keysize umin vkey-list $+[]! ;

: get-me ( -- )
    ." Enter your net2o passphrase: " +passphrase
    next-arg >key ;
: enc-vault ( -- ) \ filename myname user1 .. usern
    next-arg get-me
    BEGIN argc @ 1 >  WHILE
	    next-arg nick-key >o ke-pk $@ o> keysize umin vkey-list $+[]!
    REPEAT  vkey-list encrypt-file ;
: dec-vault ( -- )
    next-arg get-me decrypt-file ;

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