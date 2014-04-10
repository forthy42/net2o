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

object class
    field: ke-sk
    field: ke-pk
    field: ke-nick
    field: ke-prof
    field: ke-sigs
    field: ke-type
    64field: ke-first
    64field: ke-last
end-class key-entry

0 Constant key#anon
1 Constant key#user
2 Constant key#group

key-entry >osize @ buffer: sample-key

Variable key-table
Variable this-key
Variable this-keyid
2Variable addsig
sample-key this-key ! \ dummy

: current-key ( addr u -- )
    2dup key-table #@ drop dup this-key ! >o rdrop ke-pk $! ;
: make-thiskey ( addr -- )
    dup $@ drop this-keyid !  cell+ $@ drop dup this-key ! >o rdrop ;

: key:new ( addr u -- )
    \ addr u is the public key
    sample-key key-entry >osize @ 2dup erase
    2over key-table #! current-key ;

\ search for keys - not optimized

: nick-key ( addr u -- ) \ search for key nickname and make current
    key-table 
    [: dup >r cell+ $@ drop >o ke-nick $@ o> 2over str= IF
	r@ make-thiskey
    THEN  rdrop ;] #map 2drop ;

: key-exist? ( addr u -- flag )
    key-table #@ d0<> ; 

Variable strict-keys  strict-keys on

: .key ( addr u -- )
    ." Key '" key-table #@ drop >o ke-nick $@ o> type ." ' ok" cr ;

:noname ( addr u -- )
    o IF  dest-pubkey @ IF
	    2dup dest-pubkey $@ str= 0= IF
		." want: " dest-pubkey $@ xtype cr
		." got : " 2dup xtype cr
		true !!wrong-key!!
	    THEN
	    .key  EXIT
	THEN  THEN
    2dup key-exist? 0= IF
	strict-keys @ !!unknown-key!!
	." Unknown key "  .nnb cr
    ELSE
	.key
    THEN ; IS check-key

\ get passphrase

3 Value passphrase-retry#
$100 Value passphrase-diffuse#
$100 Constant max-passphrase# \ 256 characters should be enough...
max-passphrase# buffer: passphrase

: passphrase-in ( -- addr u )
    passphrase dup max-passphrase# accept* ;

: >passphrase ( addr u -- addr u )
    >r passphrase r@ max-passphrase# umin move
    passphrase max-passphrase# r> safe/string erase
    no-key >c:key
    passphrase max-passphrase# c:hash
    passphrase-diffuse# 0 ?DO  c:diffuse  LOOP \ just to waste time ;-)
    pad c:key> pad $40 save-mem ;

: get-passphrase ( -- addr u )
    passphrase-in >passphrase ;

Variable keys "" keys $!
2Variable key+len \ current key + len

: +key ( addr u -- ) key+len 2! key+len 2 cells keys $+! ;
: +passphrase ( -- )  get-passphrase +key ;
: ">passphrase ( addr u -- ) >passphrase +key ;
: +seckey ( -- )
    ke-sk $@ drop ke-pk $@ drop ed-dh +key ;

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

keypack# mykey-salt# + $10 + Constant keypack-all#

keypack-all# buffer: keypack
keypack-all# buffer: keypack-d

get-current also net2o-base definitions

100 net2o: newkey ( $:string -- ) $> keys? IF  key:new  ELSE  2drop  THEN ;
101 net2o: privkey ( $:string -- ) $> keys? IF  ke-sk $! +seckey   ELSE  2drop  THEN ;
102 net2o: keytype ( n -- ) keys? IF  64>n ke-type !  ELSE  64drop  THEN ; \ default: anonymous
103 net2o: keynick ( $:string -- ) $> keys? IF  ke-nick $!  ELSE  2drop  THEN ;
104 net2o: keyprofile ( $:string -- ) $> keys? IF ke-prof $!  ELSE  2drop  THEN ;
105 net2o: newkeysig ( $:string -- ) $> keys? IF save-mem addsig 2!
    addsig 2 cells ke-sigs $+!  ELSE  2drop  THEN ;
106 net2o: keymask ( x -- ) 64drop ;
107 net2o: keyfirst ( date-ns -- ) keys? IF ke-first 64!  ELSE  64drop  THEN ;
108 net2o: keylast ( date-ns -- ) keys? IF ke-last 64!  ELSE  64drop  THEN ;

dup set-current previous

: key:code ( -- )
    net2o-code0 keypack keypack-all# erase
    keypack mykey-salt# + cmd0source ! ;
comp: :, also net2o-base ;

also net2o-base definitions

: end:key ( -- )
    end-cmd previous
    keypack keypack-all#
    key+len 2@ encrypt$
    cmdlock unlock ;
comp: :, previous ;

set-current previous previous

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
    skc pkc ed-dh +key ;

: +gen-keys ( type nick u -- )
    gen-keys
    key:code
        pkc keysize $, newkey skc keysize $, privkey
        $, keynick lit, keytype ticks lit, keyfirst
    end:key key>file >keys ;

: +keypair ( type nick u -- ) +passphrase +gen-keys ;

\ read key file

: try-decrypt ( -- addr u / 0 0 )
    keys $@ bounds ?DO
	keypack keypack-d keypack-all# move
	keypack-d keypack-all# I 2@
	decrypt$ IF  unloop  EXIT  THEN
	2drop
    2 cells +LOOP  0 0 ;

: do-key ( addr u / 0 0  -- )
    dup 0= IF  2drop  EXIT  THEN  validated @ >r  keys-val validated !
    ( 2dup n2o:see ) do-cmd-loop  r> validated ! ;

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
    this-keyid @ pkc keysize move
    ke-sk $@ skc swap move ;

: dest-key ( addr u -- )
    0 >o nick-key o>  this-keyid @ 0= !!unknown-key!!
    this-keyid @ keysize dest-pubkey $! ;

0 [IF] \ generate keypairs
    keys $@ drop 2@ key+len 2! key#anon "test" +gen-keys
    keys $@ drop 2@ key+len 2! key#anon "anonymous" +gen-keys
[THEN]

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:") (0 . 2) (0 . 2) non-immediate)
     (("[:") (0 . 1) (0 . 1) immediate)
     ((";]") (-1 . 0) (0 . -1) immediate)
    )
End:
[THEN]