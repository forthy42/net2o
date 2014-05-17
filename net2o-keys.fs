\ net2o key storage

\ Copyright (C) 2010-2013   Bernd Paysan

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

require mkdir.fs

\ accept for password entry

: accept* ( addr u -- u' )
    \ accept-like input, but types * instead of the character
    dup >r
    BEGIN  xkey dup #cr <> WHILE
	    dup #bs = over #del = or IF
		drop dup r@ u< IF
		    over + >r xchar- r> over -
		    1 backspaces space 1 backspaces
		ELSE
		    bell
		THEN
	    ELSE
		-rot xc!+? 0= IF  bell  ELSE  '⬤' xemit  THEN
	    THEN
    REPEAT  drop  nip r> swap - ;

\ hashed key data base

cmd-class class
    field: ke-sk \ secret key
    field: ke-pk \ public key
    field: ke-nick
    field: ke-prof
    field: ke-sigs
    field: ke-type
    64field: ke-first
    64field: ke-last
end-class key-entry

key-entry >dynamic to key-entry

0 Constant key#anon
1 Constant key#user
2 Constant key#group

0 Value sample-key

' key-entry is cmd-table

Variable key-table
Variable this-key
Variable this-keyid
2Variable addsig

: current-key ( addr u -- )
    2dup keysize umin key-table #@ drop cell+ dup this-key ! >o rdrop ke-pk $! ;
: make-thiskey ( addr -- )
    dup $@ drop this-keyid !  cell+ $@ drop cell+ dup this-key ! >o rdrop ;

: key:new ( addr u -- )
    \ addr u is the public key
    sample-key dup cell- @ >osize @ 2dup erase
    over >o 64#-1 ke-last 64! o> -1 cells /string
    2over keysize umin key-table #! current-key ;

\ search for keys - not optimized

: nick-key ( addr u -- ) \ search for key nickname and make current
    key-table 
    [: dup >r cell+ $@ drop cell+ >o ke-nick $@ o> 2over str= IF
	r@ make-thiskey
    THEN  rdrop ;] #map 2drop ;

: key-exist? ( addr u -- flag )
    key-table #@ d0<> ; 

Variable strict-keys  strict-keys on

: .key ( addr u -- ) drop cell+ >o
    ." nick: " ke-nick $@ type cr
    ." ke-pk: " ke-pk $@ xtype cr
    ke-sk $@len IF  ." ke-sk: " ke-sk $@ xtype cr  THEN
    ." first: " ke-first 64@ .sigdate cr
    ." last: " ke-last 64@ .sigdate cr
    o> ;

: dumpkey ( addr u -- ) drop cell+ >o
    .\" x\" " ke-pk $@ xtype .\" \" key:new" cr
    ke-sk $@len IF  .\" x\" " ke-sk $@ xtype .\" \" ke-sk $! +seckey" cr  THEN
    '"' emit ke-nick $@ type .\" \" ke-nick $! "
    ke-first 64@ 64>d [: '$' emit 0 ud.r ;] $10 base-execute
    ." . d>64 ke-first 64! " ke-type @ . ." ke-type !"  cr o> ;

: .keys ( -- ) key-table [: cell+ $@ .key ;] #map ;
: dumpkeys ( -- ) key-table [: cell+ $@ dumpkey ;] #map ;

: .key# ( addr u -- ) keysize umin
    ." Key '" key-table #@ dup 0= IF 2drop EXIT THEN
    drop cell+ >o ke-nick $@ o> type ." ' ok" cr ;

:noname ( addr u -- )
    o IF  dest-pubkey @ IF
	    2dup dest-pubkey $@ keysize umin str= 0= IF
		[: ." want: " dest-pubkey $@ keysize umin xtype cr
		  ." got : " 2dup xtype cr ;] $err
		true !!wrong-key!!
	    THEN
	    .key#  EXIT
	THEN  THEN
    2dup key-exist? 0= IF
	strict-keys @ !!unknown-key!!
	." Unknown key "  .nnb cr
    ELSE
	.key#
    THEN ; IS check-key

\ get passphrase

3 Value passphrase-retry#
$100 Constant max-passphrase# \ 256 characters should be enough...
max-passphrase# buffer: passphrase

: passphrase-in ( -- addr u )
    passphrase dup max-passphrase# accept* ;

: >passphrase ( addr u -- addr u )
    \G create a 512 bit hash of the passphrase
    no-key >c:key c:hash
    keccak-padded c:key> keccak-padded keccak#max 2/ ;

: get-passphrase ( -- addr u )
    passphrase-in >passphrase ;

Variable keys
2Variable key+len \ current key + len

: +key ( addr u -- ) keys $+[]! ;
: +passphrase ( -- )  get-passphrase +key ;
: ">passphrase ( addr u -- ) >passphrase +key ;
: +seckey ( -- )
    ke-sk $@ drop ke-pk $@ drop keypad ed-dh +key ;

"" ">passphrase \ following the encrypt-everything paradigm,
\ no password is the empty string!  It's still encrypted!

\ a secret key just needs a nick and a type.
\ Secret keys can be persons and groups.

\ a public key needs more: nick, type, profile.
\ The profile is a structured document, i.e. pointed to by a hash.

\ a signature contains a pubkey, a checkbox bitmask,
\ a date, an expiration date, the signer's pubkey and the signature itself
\ (r+s).  There is an optional signing protocol document (hash).

\ we store each item in a 256 bytes encrypted string, i.e. with a 16
\ byte salt and a 16 byte checksum.

\ Keys are passwords and private keys (self-keyed, i.e. private*public key)

$100 Constant keypack#

0 Value pw-level# \ pw-level# 0 is lowest
\ !!TODO!! we need a way to tell how much we can trust keys
\ passwords need a pw-level (because they are guessable)
\ secrets don't, they aren't. We can quickly decrypt all
\ secret-based stuff, without bothering with slowdowns.
\ So secrets should use normal string decrypt

keypack# mykey-salt# + $10 + Constant keypack-all#

keypack-all# buffer: keypack
keypack-all# buffer: keypack-d

get-current also net2o-base definitions

8 net2o: newkey ( $:string -- ) $> key:new ;
+net2o: privkey ( $:string -- ) $> ke-sk $! +seckey ;
+net2o: keytype ( n -- )  64>n ke-type ! ; \ default: anonymous
+net2o: keynick ( $:string -- )    $> ke-nick $! ;
+net2o: keyprofile ( $:string -- ) $> ke-prof $! ;
+net2o: newkeysig ( $:string -- )  $> save-mem addsig 2!
    addsig 2 cells ke-sigs $+! ;
+net2o: keymask ( x -- )  64drop ;
+net2o: keyfirst ( date-ns -- )  ke-first 64! ;
+net2o: keylast  ( date-ns -- )  ke-last 64! ;
dup set-current previous

key-entry >static to key-entry \ back to static method table
' context-class is cmd-table

static-a to allocater
key-entry new to sample-key
dynamic-a to allocater
sample-key this-key ! \ dummy

: key:code ( -- )
    net2o-code0 keypack keypack-all# erase
    keypack mykey-salt# + cmd0source ! ;
comp: :, also net2o-base ;

also net2o-base definitions

: end:key ( -- )
    end-cmd previous
    cmdlock unlock ;
comp: :, previous ;

set-current previous previous

: key-crypt ( -- )
    keypack keypack-all#
    key+len 2@ dup $20 = \ is a secret, no need to be slow
    IF  encrypt$  ELSE  pw-level# encrypt-pw$  THEN ;

0 Value key-fd

: ?.net2o ( -- )
    s" ~/.net2o" r/o open-file nip IF
	s" ~/.net2o" $1C0 mkdir-parents throw
    THEN ;

: ?key-fd ( -- fd ) key-fd dup ?EXIT drop
    ?.net2o
    "~/.net2o/keyfile.n2o" r/w open-file dup -514 = IF
	2drop "~/.net2o/keyfile.n2o" r/w create-file
    THEN  throw
    dup to key-fd ;

: append-file ( addr u fd -- ) >r
    r@ file-size throw  r@ reposition-file throw
    r@ write-file throw  r> flush-file throw ;

: key>file ( -- )
    keypack keypack-all# ?key-fd append-file ;

: rnd>file ( -- )
    ( keypack keypack-all# >rng$ ) key>file ;

: >keys ( -- )
    \G add shared secret to list of possible keys
    skc pkc keypad ed-dh +key ;

\ key generation

: pack-key ( type nick u -- )
    key:code
        pkc keysize 2* $, newkey
	skc keysize $, privkey
        $, keynick lit, keytype ticks lit, keyfirst
    end:key ;

: +gen-keys ( type nick u -- )
    gen-keys >keys pack-key key-crypt key>file ;

: +keypair ( type nick u -- ) +passphrase +gen-keys ;

\ read key file

: try-decrypt-key ( key u1 -- addr u2 true / false )
    keypack c@ $F and pw-level# u<= IF
	keypack keypack-d keypack-all# move
	keypack-d keypack-all# 2swap
	dup $20 = IF  decrypt$  ELSE  decrypt-pw$  THEN
	?dup-if  EXIT  THEN
    THEN  2drop false ;

: try-decrypt ( -- addr u / 0 0 )
    keys $[]# 0 ?DO
	I keys $[]@ try-decrypt-key IF  unloop  EXIT  THEN
    LOOP  0 0 ;

: do-key ( addr u / 0 0  -- )
    dup 0= IF  2drop  EXIT  THEN
    sample-key >o do-cmd-loop o> ;

: read-key-loop ( -- )
    BEGIN
	keypack keypack-all# ?key-fd read-file throw
	keypack-all# = WHILE  try-decrypt do-key
    REPEAT ;

: read-keys ( -- )
    [: 0. ?key-fd reposition-file throw  read-key-loop ;] catch drop nothrow ;

\ select key by nick

: >key ( addr u -- )
    key-table @ 0= IF  read-keys  THEN
    nick-key  this-keyid @ 0= ?EXIT
    this-key @ >o ke-pk $@ o> pkc swap keysize 2* umin move
    ke-sk $@ skc swap move ;

: dest-key ( addr u -- )
    0 >o nick-key o>  this-keyid @ 0= !!unknown-key!!
    this-keyid @ keysize dest-pubkey $! ;

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
     (("[:") (0 . 1) (0 . 1) immediate)
     ((";]") (-1 . 0) (0 . -1) immediate)
    )
End:
[THEN]