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

\ Keys are passwords and private keys (self-keyed, i.e. private*public key)

$1E0 Constant keypack#

2 Value pw-level# \ pw-level# 0 is lowest
\ !!TODO!! we need a way to tell how much we can trust keys
\ passwords need a pw-level (because they are guessable)
\ secrets don't, they aren't. We can quickly decrypt all
\ secret-based stuff, without bothering with slowdowns.
\ So secrets should use normal string decrypt

keypack# mykey-salt# + $10 + Constant keypack-all#

cmd-buf0 class
    maxdata -
    mykey-salt# uvar keypack
    keypack# uvar keypack-buf
    $10 uvar keypack-chksum
end-class cmd-keybuf-c

cmd-keybuf-c new cmdbuf: code-key

code-key
cmd0lock 0 pthread_mutex_init drop

:noname ( -- addr u ) keypack-buf cmdbuf# @ ; to cmdbuf$
:noname ( -- n )  keypack# cmdbuf# @ - ; to maxstring

code0-buf

keypack-all# buffer: keypack-d

\ hashed key data base

cmd-class class
    field: ke-sk \ secret key
    field: ke-pk \ public key
    field: ke-psk \ preshared key for stateless communication
    field: ke-nick
    field: ke-prof
    field: ke-sigs
    field: ke-type
    field: ke-key
    64field: ke-first
    64field: ke-last
    64field: ke-offset \ offset in key file
    0 +field ke-end
end-class key-entry

Variable key-entry-table

0 Constant key#anon
1 Constant key#user
2 Constant key#group

0 Value sample-key

Variable key-table

64Variable key-read-offset

: current-key ( addr u -- o )
    2dup keysize umin key-table #@ drop
    dup 0= IF  drop ." unknown key: " 85type cr  0 EXIT  THEN
    cell+ >o ke-pk $! o o> ;

: key:new ( addr u -- )
    \ addr u is the public key
    sample-key >o
    key-entry-table @ token-table !
    ke-sk ke-end over - erase
    64#-1 ke-last 64!
    key-read-offset 64@ ke-offset 64!
    keypack-all# n>64 key-read-offset 64+! o cell- ke-end over -
    2over keysize umin key-table #! o>
    current-key ;

\ search for keys - not optimized

: nick-key ( addr u -- o ) \ search for key nickname
    0 -rot key-table 
    [: cell+ $@ drop cell+ >o ke-nick $@ 2over str= IF
	rot drop o -rot
    THEN  o> ;] #map 2drop ;

: key-exist? ( addr u -- flag )
    key-table #@ d0<> ; 

Variable strict-keys  strict-keys on

require ansi.fs

: .black ( addr u -- )
    [ black >bg black >fg or ]L attr!   85type
	[ default-color >bg default-color >fg or ]L attr! ;
: .rsk ." \ revoke: " skrev $20 .black cr ;
: .key ( addr u -- ) drop cell+ >o
    ." nick: " ke-nick $@ type cr
    ." ke-pk: " ke-pk $@ 85type cr
    ke-sk @ IF  ." ke-sk: " ke-sk @ keysize
	.black cr  THEN
    ." first: " ke-first 64@ .sigdate cr
    ." last: " ke-last 64@ .sigdate cr
    o> ;

: dumpkey ( addr u -- ) drop cell+ >o
    .\" x\" " ke-pk $@ 85type .\" \" key:new" cr
    ke-sk @ IF  .\" x\" " ke-sk @ keysize 85type .\" \" ke-sk sec! +seckey" cr  THEN
    '"' emit ke-nick $@ type .\" \" ke-nick $! "
    ke-first 64@ 64>d [: '$' emit 0 ud.r ;] $10 base-execute
    ." . d>64 ke-first 64! " ke-type @ . ." ke-type !"  cr o> ;

: .keys ( -- ) key-table [: cell+ $@ .key ;] #map ;
: dumpkeys ( -- ) key-table [: cell+ $@ dumpkey ;] #map ;

: .key# ( addr u -- ) keysize umin
    ." Key '" key-table #@ 0= IF drop EXIT THEN
    cell+ .ke-nick $@ type ." ' ok" cr ;

:noname ( addr u -- )
    o IF  dest-pubkey @ IF
	    2dup dest-pubkey $@ keysize umin str= 0= IF
		[: ." want: " dest-pubkey $@ keysize umin 85type cr
		  ." got : " 2dup 85type cr ;] $err
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

:noname ( pkc -- skc )
    keysize key-table #@ 0= !!unknown-key!!
    cell+ .ke-sk sec@ 0= !!unknown-key!! ; is search-key

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

: key>default ( -- ) keys $[]# 1- keys sec[]@ key+len 2! ;
: +key ( addr u -- ) keys sec+[]! ;
: +passphrase ( -- )  get-passphrase +key ;
: ">passphrase ( addr u -- ) >passphrase +key ;
: +seckey ( -- )
    ke-sk @ ke-pk $@ drop keypad ed-dh +key ;

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

get-current also net2o-base definitions

cmd-table $@ inherit-table key-entry-table

$10 net2o: newkey ( $:string -- o:key ) $> key:new n:>o ;
key-entry-table >table
+net2o: privkey ( $:string -- ) $> ke-sk sec! +seckey ;
+net2o: keytype ( n -- )  64>n ke-type ! ; \ default: anonymous
+net2o: keynick ( $:string -- )    $> ke-nick $! ;
+net2o: keyprofile ( $:string -- ) $> ke-prof $! ;
+net2o: newkeysig ( $:string -- )  $> ke-sigs $+[]! ;
+net2o: keymask ( x -- )  64drop ;
+net2o: keyfirst ( date-ns -- )  ke-first 64! ;
+net2o: keylast  ( date-ns -- )  ke-last 64! ;
dup set-current previous

gen-table $freeze
' context-table is gen-table

key-entry ' new static-a with-allocater to sample-key
sample-key >o key-entry-table @ token-table ! o>

: key:code ( -- )
    code-key  cmdlock lock
    keypack keypack-all# erase
    cmdreset also net2o-base ;
comp: :, also net2o-base ;

also net2o-base definitions

: end:key ( -- )
    endwith end-cmd previous
    cmdlock unlock ;
comp: :, previous ;

set-current previous previous

: key-crypt ( -- )
    keypack keypack-all#
    key+len 2@ dup $20 u<= \ is a secret, no need to be slow
    IF  encrypt$  ELSE  pw-level# encrypt-pw$  THEN ;

0 Value key-sfd \ secret keys
0 Value key-pfd \ pubkeys

: ?.net2o ( -- )
    s" ~/.net2o" r/o open-file nip IF
	s" ~/.net2o" $1C0 mkdir-parents throw
    THEN ;

: ?fd ( fd addr u -- fd' ) { addr u } dup ?EXIT drop
    ?.net2o
    addr u r/w open-file dup -514 = IF
	2drop addr u r/w create-file
    THEN  throw ;
: ?key-sfd ( -- fd ) key-sfd "~/.net2o/seckeys.k2o" ?fd dup to key-sfd ;
: ?key-pfd ( -- fd ) key-pfd "~/.net2o/pubkeys.k2o" ?fd dup to key-pfd ;

: append-file ( addr u fd -- ) >r
    r@ file-size throw  r@ reposition-file throw
    r@ write-file throw  r> flush-file throw ;

: key>sfile ( -- )
    keypack keypack-all# ?key-sfd append-file ;
: key>pfile ( -- )
    keypack keypack-all# ?key-pfd append-file ;

: rnd>sfile ( -- )
    keypack keypack-all# >rng$ key>sfile ;
: rnd>pfile ( -- )
    keypack keypack-all# >rng$ key>pfile ;

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
    gen-keys >keys pack-key key-crypt key>sfile ;

: +keypair ( type nick u -- ) +passphrase +gen-keys ;

: .rvk ." Please write down revoke key: " cr
    skrev $20 bounds DO  ." \ " I 4 85type space I 4 + 4 85type cr 8 +LOOP ;

$40 buffer: nick-buf

: make-key ( -- )
    key#user ." nick: " nick-buf $40 accept nick-buf swap cr
    ." passphrase: " +passphrase key>default
    cr +gen-keys .rvk ;

\ read key file

: try-decrypt-key ( key u1 -- addr u2 flag )
    keypack keypack-d keypack-all# move
    keypack-d keypack-all# 2swap
    dup $20 = IF  decrypt$  ELSE
	keypack c@ $F and pw-level# <= IF  decrypt-pw$
	ELSE  2drop false  THEN
    THEN ;

: try-decrypt ( -- addr u / 0 0 )
    keys $[]# 0 ?DO
	I keys sec[]@ try-decrypt-key IF  unloop  EXIT  THEN
	2drop
    LOOP  0 0 ;

: do-key ( addr u / 0 0  -- )
    dup 0= IF  2drop  EXIT  THEN
    sample-key .do-cmd-loop ;

: read-keys-loop ( fd -- )  >r 0. r@ reposition-file throw
    BEGIN
	r@ file-position throw d>64 key-read-offset 64!
	keypack keypack-all# r@ read-file throw
	keypack-all# = WHILE  try-decrypt do-key
    REPEAT  rdrop ;
: read-key-loop ( -- ) ?key-sfd read-keys-loop ;
: read-pkey-loop ( -- ) pw-level# >r -1 to pw-level#
    ?key-pfd read-keys-loop r> to pw-level# ;

: read-keys ( -- )
    read-key-loop read-pkey-loop ;

\ select key by nick

: >key ( addr u -- )
    key-table @ 0= IF  read-keys  THEN
    nick-key >o o 0= IF  o> true !!no-nick!!  THEN
    ke-pk $@ pkc swap keysize 2* umin move
    ke-psk sec@ my-0key sec!
    ke-sk @ skc keysize move o> ;

: i'm ( "name" -- ) parse-name >key ;
: pk' ( "name" -- addr u )
    parse-name nick-key >o ke-pk $@ o> ;

: dest-key ( addr u -- ) dup 0= IF  2drop  EXIT  THEN
    nick-key >o o 0= !!unknown-key!!
    ke-psk sec@ state# umin
    ke-pk $@ keysize umin o>
    dest-pubkey $!  dest-0key sec! ;

: replace-key 1 /string { rev-addr u -- o } \ revocation ticket
    key( ." Replace:" cr o cell- 0 .key )
    s" #revoked" dup >r ke-nick $+!
    ke-nick $@ r> - ke-prof $@ ke-sigs ke-type @ ke-key @ 
    rev-addr keysize 2* key:new >o
    ke-key ! ke-type ! [: ke-sigs $+[]! ;] $[]map ke-prof $! ke-nick $!
    rev-addr keysize 2* ke-pk $!
    rev-addr u + 1- dup c@ 2* - $10 - dup 64@ ke-first 64! 64'+ 64@ ke-last 64!
    key( ." with:" cr o cell- 0 .key ) o o> ;

:noname ( revaddr u1 keyaddr u2 -- o )
    current-key >o replace-key o> >o skc keysize ke-sk sec!
    o o> ; is renew-key

also net2o-base
: fetch-id, ( id-addr u -- )
    $, dht-id dht-host? endwith ;
: fetch-host, ( nick u -- )
    nick-key .ke-pk $@ fetch-id, ;
previous

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