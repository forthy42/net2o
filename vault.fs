\ encrypted files                                    10feb2015py

\ Copyright © 2015   Bernd Paysan

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

cmd-class class
    KEYBYTES +field v-dhe \ diffie hellman exchange tmpkey
    state# +field v-key \ file vault key, maximum 64 bytes
    keccak# +field v-kstate
    2field: v-data
    2field: v-sig
    value: v-mode \ crypto mode and key size
    value: v/blk2 \ block size power of two
end-class vault-class

Defer write-decrypt
Defer read-encrypt

: >vault ( -- o:vault ) \ push a vault object
    vault-table vault-class new-tok n:>o
    my-key-default to my-key ;
: v-mode>crypt2 ( -- )
    v-mode $10 rshift $FF and >crypt ;

vault-table >table

scope{ net2o-base

cmd-table $@ inherit-table vault-table
\g 
\g ### vault commands ###
\g 
net2o' emit net2o: dhe ( $:pubkey -- ) c-state @ !!inv-order!!
    \g start diffie hellman exchange
    $> ?keysize sk@ drop swap v-dhe ed-dh 2drop
    v-key state# erase 1 c-state or! ;
+net2o: vault-keys ( $:keys -- ) c-state @ 1 <> !!no-tmpkey!!
    \g vault keys can be opened with the dhe secret; each key is IV+session key+checksum
    v-mode dup $FF and state# umax { vk# } 8 rshift $FF and >crypt
    $> bounds ?DO
	delta-I vk# u>= IF
	    I vaultkey vk# move
	    vaultkey vk# v-dhe keysize decrypt$ IF
		dup state# 1+ keysize within !!keysize!!
		v-key state# move-rep
		vkey( ." vkey keyx: " v-key state# 85type forth:cr )
		2 c-state or!  LEAVE
	    ELSE  2drop  THEN
	THEN
    vk# +LOOP  0 >crypt ;
+net2o: vault-file ( $:content -- ) c-state @ 3 <> !!no-tmpkey!!
    \g this is the actual content of the vault
    \g if blockwise, there may be multiple parts
    v-mode>crypt2
    no-key state# >crypt-source  v-key state# >crypt-key
    vkey( ." vkey filex: " v-key state# 85type forth:cr )
    $> 2dup c:decrypt v-data 2!
    c:diffuse \ one extra diffusion round
    0 >crypt  4 c-state or! ; \ keep for signature
+net2o: vault-sig ( $:sig -- ) c-state @ 7 <> !!no-data!!
    \g the signature of the vault, using the keyed hash over the file
    vkey( ." vkey sigx: " v-key state# 85type forth:cr )
    c:key@ v-kstate c:key# move
    $> v-key state# decrypt$ 0= !!no-decrypt!!
    v-mode>crypt2
    v-kstate c:key@ c:key# move
    verify-tag !!sig!!
    2dup dup sigpksize# - /string v-sig 2!
    sigpksize# - IF  p@+ drop 64>n negate v-data +!  ELSE  drop  THEN
    0 >crypt 8 c-state or! ;
+net2o: vault-crypt ( n -- ) \g set encryption mode and key wrap size
    c-state @ 1 u> !!inv-order!!  64>n to v-mode ;
+net2o: vault-auth ( $:auth -- )
    \g block authentication, 64 byte block
    c-state @ 7 <> !!no-data!!
    \ otherwise would expose some data
    $> v-kstate c:key> v-kstate $40 str= 0= !!vault-auth!!
    write-decrypt \ write a chunk out
    4 c-state xor! ; \ step back to allow fault-file
+net2o: vault-dhe-keys ( $:dhe+keys -- )  c-state @ !!inv-order!!
    $> v-dec$ 2dup d0= !!unknown-key!!  v-key state# move-rep
    3 c-state or! ;

vault-table $save
' context-table is gen-table

also }scope

$80 Constant min-align#
$400 Constant pow-align#

: vault-aligned ( len -- len' ) \ net2o
    \G Align vault to minimum granularity plus relative alignment
    \G to hide the actual file-size
    1- 0 >r  BEGIN  dup pow-align# u>  WHILE  1 rshift r> 1+ >r  REPEAT
    1+ r> lshift  min-align# 1- + min-align# negate and ;

Variable enc-mode

: enc-keccak ( -- )        $60 enc-mode ! ; \ wrap with keccak
: enc-threefish ( -- ) $010160 enc-mode ! ; \ wrap with threefish
: enc-keyak ( -- )     $020260 enc-mode ! ; \ wrap with keyak
: enc>crypt2 ( -- )
    enc-mode @ $10 rshift $FF and >crypt ;

enc-keccak

: pk-off ( -- ) key-list $[]free ;

: vdhe, ( -- )   stskc stpkc ed-keypair stpkc keysize $, dhe ;
: vkeys, ( key-list -- )
    vaultkey $100 erase
    enc-mode @ $FF and $20 - rng$ vkey state# move-rep
    vkey( ." vkey key: " vkey state# 85type forth:cr )
    enc-mode @ dup ulit, vault-crypt 8 rshift $FF and >crypt
    [: [: drop stskc swap keygendh ed-dh 2>r
	    vkey vaultkey $10 + enc-mode @ $FF and $20 - move
	    vaultkey enc-mode @ $FF and 2r> encrypt$
	    vaultkey enc-mode @ $FF and forth:type ;] $[]map ;] $tmp
    $, vault-keys 0 >crypt ;
: vdhe-keys, ( key-list -- )
    v-enc$ $, vault-dhe-keys  0 >crypt ;
: vfile-in ( -- )
    enc-filename $@ enc-file $slurp-file ;
: vfile-pad ( -- )
    enc-file $@len dup >r vault-aligned enc-file $!len
    enc-file $@ r> /string dup enc-padding ! erase ;
: vfile-enc ( -- )
    vkey( ." vkey file: " vkey state# 85type forth:cr )
    enc>crypt2
    no-key state# >crypt-source
    vkey state# >crypt-key enc-file $@ c:encrypt c:diffuse
    enc-file $@ $, vault-file 0 >crypt enc-file $free ;
: vfile, ( -- )
    vfile-pad vfile-enc ;
: vsig, ( -- )
    enc>crypt2
    [: $10 spaces now>never enc-padding @ n>64 cmdtmp$ forth:type
      .pk .sig $10 spaces ;] $tmp
    0 >crypt
    vkey( ." vkey sig: " vkey state# 85type forth:cr )
    2dup vkey state# encrypt$ $, vault-sig ;

: encfile-rest ( key-list -- ) >vault >r
    code-buf$ cmdreset init-reply
    pk@ key| r@ $+[]! \ encrypt for ourself
    "v2o" 4cc,
    newvault( r> vdhe-keys, )else( vdhe, r> vkeys, )
    vfile, vsig,
    s" .v2o" enc-filename $+!
    enc-filename $@ [: >r cmd$ $@ r> write-file throw ;] new-file
    code0-buf dispose n:o> ;

: encrypt-file ( filename u key-list -- )
    >r enc-filename $! vfile-in r> encfile-rest ;

: ?enc-fd ( -- fd )
    enc-fd @ dup ?EXIT  drop
    enc-filename $@ 2dup dup 4 - 0 max safe/string s" .v2o" str=
    IF  4 -  THEN
    w/o create-file throw  dup enc-fd ! ;

: write-1file ( addr u -- ) ?enc-fd write-file throw ;
: vault>file ['] write-1file is write-decrypt ;
vault>file
: vault>out ['] forth:type is write-decrypt ;

: >decrypt ( -- addr u flag )
    enc-filename $!
    enc-filename $@ enc-file $slurp-file
    enc-file $@ >vault ['] do-cmd-loop catch 0 >crypt throw
    c-state @ $F = ;

: decrypt@ ( filename u -- addr u ) last# >r >decrypt
    IF
	msg( v-sig 2@ 2dup .sigdates space .key-id forth:cr )
	v-data 2@
    ELSE
	#0.
    THEN dispose n:o> r> to last# ;

: decrypt-file ( filename u -- )
    decrypt@ write-decrypt enc-file $free
    enc-fd @ ?dup-IF  forth:close-file throw  THEN ;
previous

\\\
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
    )
End:
