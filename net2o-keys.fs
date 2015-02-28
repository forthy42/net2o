\ net2o key storage

\ Copyright (C) 2010-2015   Bernd Paysan

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
    \g accept-like input, but types * instead of the character
    \g don't save into history
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


2 Value pw-level# \ pw-level# 0 is lowest
\ !!TODO!! we need a way to tell how much we can trust keys
\ passwords need a pw-level (because they are guessable)
\ secrets don't, they aren't. We can quickly decrypt all
\ secret-based stuff, without bothering with slowdowns.
\ So secrets should use normal string decrypt

cmd-buf0 class
    maxdata -
    key-salt# uvar keypack
    keypack#  uvar keypack-buf
    key-cksum# uvar keypack-chksum
end-class cmd-keybuf-c

cmd-keybuf-c new cmdbuf: code-key

code-key
cmd0lock 0 pthread_mutex_init drop

:noname ( -- addr u ) keypack-buf cmdbuf# @ ; to cmdbuf$
:noname ( -- n )  keypack# cmdbuf# @ - ; to maxstring

code0-buf

\ hashed key data base

User >storekey

cmd-class class
    field: ke-sk   \ secret key
    field: ke-pk   \ public key
    field: ke-type \ key type
    field: ke-nick \ key nick
    field: ke-psk  \ preshared key for stateless communication
    field: ke-prof \ profile object
    field: ke-selfsig
    field: ke-sigs
    field: ke-storekey \ used to encrypt on storage
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
    ke-sk ke-end over - erase  >storekey @ ke-storekey !
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
: .red ( addr u -- )
    [ red >bg white >fg or bold or ]L attr!   85type
    [ default-color >bg default-color >fg or ]L attr! ;
: .rsk ( nick u )
    ." \ revoke: " skrev $20 .red space type ."  (keep offline copy!)" cr ;
: .key ( addr u -- ) drop cell+ >o
    ." nick: " ke-nick $@ type cr
    ." pubkey: " ke-pk $@ 85type cr
    ke-sk @ IF  ." seckey: " ke-sk @ keysize
	.black ."  (keep secret!)" cr  THEN
    ." first: " ke-selfsig $@ drop 64@ .sigdate cr
    ." last: " ke-selfsig $@ drop 64'+ 64@ .sigdate cr
    o> ;

: dumpkey ( addr u -- ) drop cell+ >o
    .\" x\" " ke-pk $@ 85type .\" \" key:new" cr
    ke-sk @ IF  .\" x\" " ke-sk @ keysize 85type .\" \" ke-sk sec! +seckey" cr  THEN
    '"' emit ke-nick $@ type .\" \" ke-nick $! "
    ke-selfsig $@ drop 64@ 64>d [: '$' emit 0 ud.r ;] $10 base-execute
    ." . d>64 ke-first! " ke-type @ . ." ke-type !"  cr o> ;

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

: key>default ( -- ) keys $[]# 1- keys $[] @ >storekey ! ;
: +key ( addr u -- ) keys sec+[]! ;
: +passphrase ( -- )  get-passphrase +key ;
: +checkphrase ( -- flag ) get-passphrase keys $[]# 1- keys sec[]@ str= ;
: +newphrase ( -- )
    BEGIN
	." Passphrase: " +passphrase cr
	." Retype pls: " +checkphrase 0= WHILE
	    ."  didn't match, try again please" cr
    REPEAT cr ;

: ">passphrase ( addr u -- ) >passphrase +key ;
: +seckey ( -- )
    ke-sk @ ke-pk $@ drop keypad ed-dh +key ;

"" ">passphrase \ following the encrypt-everything paradigm,
\ no password is the empty string!  It's still encrypted ;-)!

\ a secret key just needs a nick and a type.
\ Secret keys can be persons and groups.

\ a public key needs more: nick, type, profile.
\ The profile is a structured document, i.e. pointed to by a hash.

\ a signature contains a pubkey, a checkbox bitmask,
\ a date, an expiration date, the signer's pubkey and the signature itself
\ (r+s).  There is an optional signing protocol document (hash).

\ we store each item in a 256 bytes encrypted string, i.e. with a 16
\ byte salt and a 16 byte checksum.

: ke-last! ( 64date -- )
    ke-selfsig $@len $10 umax ke-selfsig $!len
    ke-selfsig $@ drop 64'+ 64! ;
: ke-first! ( 64date -- ) 64#-1 ke-last!
    ke-selfsig $@ drop 64! ;

get-current also net2o-base definitions

cmd-table $@ inherit-table key-entry-table

\ $10 net2o: newkey ( $:string -- o:key ) !!signed?
\     $> key:new n:>o  1 !!>order? ;
\ key-entry-table >table
$11 net2o: privkey ( $:string -- )
    \ does not need to be signed, the secret key verifies itself
    $> over keypad sk>pk \ generate pubkey
    keypad ke-pk $@ drop keysize tuck str= 0= !!wrong-key!!
    ke-sk sec! +seckey ;
+net2o: keytype ( n -- )           !!signed?   1 !!>order? 64>n ke-type ! ;
+net2o: keynick ( $:string -- )    !!signed?   2 !!>order? $> ke-nick $! ;
+net2o: keyprofile ( $:string -- ) !!signed?   4 !!>order? $> ke-prof $! ;
+net2o: keymask ( x -- )                       8 !!>order? 64drop ;
+net2o: keypsk ( $:string -- )     !!signed? $10 !!>order? $> ke-psk sec! ;
+net2o: +keysig ( $:string -- )  $20 !!>=order? $> ke-sigs $+[]! ;
dup set-current previous

gen-table $freeze
' context-table is gen-table

:noname ( addr u -- addr u' flag )
    2dup + 1- c@ 2* { pk# }
    c:0key 2dup sigsize# - c:hash
    2dup + pk# sigsize# + - date-sig? dup 0= ?EXIT  drop
    sigsize# - 2dup + sigsize# >$
    pk# - 2dup + pk# key:new n:>o $> ke-selfsig $!
    0 c-state ! true ; key-entry to nest-sig

key-entry ' new static-a with-allocater to sample-key
sample-key >o key-entry-table @ token-table ! o>

: key:code ( -- )
    code-key  cmdlock lock
    keypack keypack-all# erase
    cmdreset also net2o-base ;
comp: :, also net2o-base ;

also net2o-base definitions

: end:key ( -- )
    endwith previous cmdlock unlock ;
comp: :, previous ;

set-current previous previous

: key-crypt ( -- )
    keypack keypack-all#
    >storekey sec@ dup $20 u<= \ is a secret, no need to be slow
    IF  encrypt$  ELSE  pw-level# encrypt-pw$  THEN ;

0 Value key-sfd \ secret keys
0 Value key-pfd \ pubkeys

: ?.net2o ( -- )
    s" ~/.net2o" r/o open-file IF
	drop s" ~/.net2o" $1C0 mkdir-parents throw
    ELSE
	close-file throw
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
\ for reproducibility of the selfsig, always use the same order:
\ "pubkey" newkey <n> keytype "nick" keynick "sig" keyselfsig

User pk+sig$

keysize 2* Constant pkrk#

: ]pk+sign ( addr u -- ) +cmdbuf ]sign ;

: pack-key ( type nick u -- )
    now>never
    key:code
      sign[
      rot lit, keytype $, keynick
      pkc pkrk# ]pk+sign
      skc keysize $, privkey
    end:key ;

also net2o-base
: pack-corekey ( o:key -- )
    sign[
    ke-type @ ulit, keytype
    ke-nick $@ $, keynick
    ke-psk sec@ dup IF  $, keypsk  ELSE  2drop  THEN
    ke-prof $@ dup IF  $, keyprofile  ELSE  2drop  THEN
    ke-pk $@ +cmdbuf
    ke-selfsig $@ +cmdbuf cmd-resolve> 2drop nestsig
    ke-storekey @ >storekey ! ;
previous

: pack-pubkey ( o:key -- )
    key:code
      pack-corekey
    end:key ;
: pack-seckey ( o:key -- )
    key:code
      pack-corekey
      ke-sk sec@ $, privkey
    end:key ;

: >backup ( addr u -- )
    2dup 2dup [: type '~' emit ;] $tmp rename-file throw
    2dup [: type '+' emit ;] $tmp 2swap rename-file throw ;

: save-pubkeys ( -- )
    key-pfd ?dup-IF  close-file throw  THEN
    0 "~/.net2o/pubkeys.k2o+" ?fd to key-pfd
    key-table [: cell+ $@ drop cell+ >o
      ke-sk sec@ d0= IF  pack-pubkey  THEN
      key-crypt key>pfile o> ;] #map
    key-pfd close-file throw
    "~/.net2o/pubkeys.k2o" >backup
    0 to key-pfd ;

: save-seckeys ( -- )
    key-sfd ?dup-IF  close-file throw  THEN
    0 "~/.net2o/seckeys.k2o+" ?fd to key-sfd
    key-table [: cell+ $@ drop cell+ >o
      ke-sk sec@ d0<> IF  pack-seckey  THEN
      key-crypt key>sfile o> ;] #map
    "~/.net2o/seckeys.k2o" >backup
    key-sfd close-file throw 0 to key-sfd ;

: save-keys ( -- )
    save-pubkeys save-seckeys ;

: +gen-keys ( nick u type -- ) -rot
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
	I keys sec[]@ try-decrypt-key IF
	    I keys $[] @ >storekey ! unloop  EXIT  THEN
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
    ke-pk $@ pkc swap pkrk# umin move
    ke-psk sec@ my-0key sec!
    ke-sk sec@ skc swap keysize umin move
    >sksig o> ;

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
    ke-nick $@ r> - ke-prof $@ ke-psk sec@ ke-sigs ke-type @
    rev-addr pkrk# key:new >o
    ke-type ! [: ke-sigs $+[]! ;] $[]map ke-psk sec! ke-prof $! ke-nick $!
    rev-addr pkrk# ke-pk $!
    rev-addr u + 1- dup c@ 2* - $10 - $10 ke-selfsig $!
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
     (("debug:" "field:" "2field:" "sffield:" "dffield:" "64field:" "uvar" "uvalue") non-immediate (font-lock-type-face . 2)
      "[ \t\n]" t name (font-lock-variable-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
     (("[:" "key:code") (0 . 1) (0 . 1) immediate)
     ((";]" "end:key") (-1 . 0) (0 . -1) immediate)
    )
End:
[THEN]