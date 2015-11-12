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

2 Value pw-level# \ pw-level# 0 is lowest

[IFDEF] androidxxx '*' [ELSE] '⬤' [THEN] Constant pw*

xc-vector up@ - class-o !

0 cell uvar esc-state drop

Defer old-emit  what's emit is old-emit

here
xc-vector @ cell- dup @ tuck - here swap dup allot move
, here 0 , Constant utf-8*

: *-width ( addr u -- n )
    0 -rot bounds ?DO  I c@ $C0 $80 within -  LOOP ;

xc-vector @  utf-8* xc-vector ! ' *-width is x-width  xc-vector !

: emit-pw* ( n -- )
    dup #esc = IF  esc-state on  THEN
    dup bl < IF  old-emit  EXIT  THEN
    esc-state @ IF  dup old-emit
    ELSE  dup $C0 $80 within IF
	    [ pw* ' xemit $tmp
	    bounds [?DO] [I] c@ ]L old-emit [ [LOOP] ]
	THEN
    THEN
    toupper 'A' '[' within IF  esc-state off  THEN ;

: type-pw* ( addr u -- )  2dup bl skip nip 0=
    IF    bounds U+DO  bl old-emit    LOOP
    ELSE  bounds U+DO  I c@ emit-pw*  LOOP  THEN ;

: accept* ( addr u -- u' )
    \G accept-like input, but types * instead of the character
    \G don't save into history
    history >r  what's type >r  what's emit is old-emit
    utf-8* xc-vector !@ >r  ['] type-pw* is type  ['] emit-pw* is emit
    0 to history
    ['] accept catch
    r> xc-vector !  what's old-emit is emit  r> is type  r> to history
    throw -1 0 at-deltaxy space ;

\ Keys are passwords and private keys (self-keyed, i.e. private*public key)

cmd-buf0 uclass cmdbuf-o
    maxdata -
    key-salt# uvar keypack
    keypack#  uvar keypack-buf
    key-cksum# uvar keypack-chksum
end-class cmd-keybuf-c

cmd-keybuf-c new code-key^ !
' code-key^ cmdbuf: code-key

code-key
cmd0lock 0 pthread_mutex_init drop

:noname ( -- addr u ) keypack-buf cmdbuf# @ ; to cmdbuf$
:noname ( -- n )  keypack# cmdbuf# @ - ; to maxstring

code0-buf

:noname defers alloc-code-bufs
    cmd-keybuf-c new code-key^ ! ; is alloc-code-bufs
:noname defers free-code-bufs
    code-key^ @ .dispose ; is free-code-bufs

\ hashed key data base

User >storekey

cmd-class class
    field: ke-sk       \ secret key
    field: ke-pk       \ public key
    field: ke-type     \ key type
    field: ke-nick     \ key nick
    field: ke-nick#    \ to avoid colissions, add a number here
    field: ke-psk      \ preshared key for stateless communication
    field: ke-prof     \ profile object
    field: ke-selfsig
    field: ke-sigs
    field: ke-import   \ type of key import
    field: ke-storekey \ used to encrypt on storage
    64field: ke-offset \ offset in key file
    0 +field ke-end
end-class key-entry

: free-key ( o:key -- o:key )
    \g free all parts of the subkey
    ke-sk sec-off
    ke-pk $off
    ke-nick $off
    ke-psk sec-off
    ke-selfsig $off
    ke-sigs $[]off ;

Variable key-entry-table

0
enum key#anon
enum key#user
enum key#group
drop

0
enum import#self      \ private key
enum import#manual    \ manual import
enum import#scan      \ scan import
enum import#chat      \ chat import
enum import#dht       \ dht import
enum import#invited   \ invitation import
enum import#untrusted \ must be last
drop

Variable import-type  import#untrusted import-type !

Create >im-color  $B60 , $D60 , $960 , $C60 , $A60 , $8B1 , $E60 ,
DOES> swap cells + @ attr! ;

0 Value sample-key

Variable key-table \ key hash table
Variable nick-table \ nick hash table

64Variable key-read-offset

: current-key ( addr u -- o )
    2dup key| key-table #@ drop
    dup 0= IF  drop ." unknown key: " 85type cr  0 EXIT  THEN
    cell+ >o ke-pk $! o o> ;

Variable sim-nick!

: nick! ( -- ) sim-nick! @ ?EXIT  o { w^ optr }
    ke-nick $@ nick-table #@ d0= IF
	optr cell ke-nick $@ nick-table #! 0
    ELSE
	last# cell+ $@len cell/
	optr cell last# cell+ $+!
    THEN  ke-nick# ! ;

: #.nick ( hash -- )
    dup $@ type '#' emit cell+ $@len cell/ . ;

: key:new ( addr u -- o )
    \G create new key, addr u is the public key
    sample-key >o
    key-entry-table @ token-table !
    ke-sk ke-end over - erase  >storekey @ ke-storekey !
    key-read-offset 64@ ke-offset 64!
    import-type @ ke-import !
    keypack-all# n>64 key-read-offset 64+! o cell- ke-end over -
    2over key| key-table #! o>
    current-key ;

: key?new ( addr u -- o )
    \G Create or lookup new key
    2dup key| key-table #@ drop
    dup 0= IF  drop key:new  ELSE  nip nip cell+  THEN ;

\ search for keys - not optimized

: #split ( addr u -- addr u n )
    [: 2dup '#' -scan nip >r
      r@ 0= IF  rdrop 0  EXIT  THEN
      0. 2over r@ /string >number
      0= IF  nip drop nip r> 1- swap  ELSE
	  rdrop drop 2drop 0   THEN ;] #10 base-execute ;

: nick-key ( addr u -- o / 0 ) \ search for key nickname
    #split >r nick-table #@ 2dup d0= IF  rdrop drop  EXIT  THEN
    r> cells safe/string 0= IF  drop 0  EXIT  THEN  @ ;

: secret-keys# ( -- n )
    0 key-table [: cell+ $@ drop cell+ >o ke-sk @ 0<> - o> ;] #map ;
: secret-key ( n -- o/0 )
    0 tuck key-table [: cell+ $@ drop cell+ >o ke-sk @ IF
	  2dup = IF  rot drop o -rot  THEN  1+
      THEN  o> ;] #map 2drop ;
: .nick-base ( o:key -- )
    ke-nick $.  ke-nick# @ ?dup-IF  '#' emit 0 .r  THEN ;
: .nick ( o:key -- )   ke-import @ >im-color .nick-base <default> ;

: nick>pk ( nick u -- pk u )
    nick-key ?dup-IF .ke-pk $@ ELSE 0 0 THEN ;
: host.nick>pk ( addr u -- pk u' )
    '.' $split dup 0= IF  2swap  THEN [: nick>pk type type ;] $tmp ;

: key-exist? ( addr u -- flag )
    key-table #@ d0<> ; 

Variable strict-keys  strict-keys on

[IFUNDEF] magenta  brown constant magenta [THEN]
[IFDEF] gl-type : bg| >bg or ; [ELSE] : bg| drop ; [THEN]

Create 85colors-bw
0 , invers ,
invers , 0 ,
0 , invers ,
invers , 0 ,
Create 85colors-cl
yellow >fg blue >bg or bold or , red >fg white bg| ,
black >fg cyan bg| , green >fg black >bg or bold or ,
white >fg black >bg or bold or , magenta >fg yellow bg| ,
blue >fg yellow bg| , cyan >fg red >bg or bold or ,

[IFDEF] gl-type 85colors-cl [ELSE] 85colors-bw [THEN] Value 85colors

: .black85 ( addr u -- )
    [ black >bg black >fg or ]L attr!   85type <default> ;
: .stripe85 ( addr u -- )  0 -rot bounds ?DO
	cr dup cells 85colors + @ attr! 1+
	I 4 85type  dup cells 85colors + @ attr! 1+
	I 4 + 4 85type <default> 8 +LOOP  drop ;
: .rsk ( nick u -- )
    skrev $20 .stripe85 space type ."  (keep offline copy!)" cr ;
: .key ( addr u -- ) drop cell+ >o
    ." nick: " .nick cr
    ." pubkey: " ke-pk $@ 85type cr
    ke-sk @ IF  ." seckey: " ke-sk @ keysize
	.black85 ."  (keep secret!)" cr  THEN
    ." created: " ke-selfsig $@ drop 64@ .sigdate cr
    ." expires: " ke-selfsig $@ drop 64'+ 64@ .sigdate cr
    o> ;
: .key-rest ( o:key -- o:key )
    ke-pk $@ keysize umin
    ke-import @ >im-color 85type <default>
    ke-selfsig $@ .sigdates
    space .nick ;
: .key-list ( o:key -- o:key )
    ke-offset 64@ 64>d keypack-all# fm/mod nip 2 .r space
    .key-rest cr ;
: .secret-nicks ( -- )
    0 key-table [: cell+ $@ drop cell+ >o ke-sk @ IF
	  dup 2 .r space .key-rest cr 1+
      THEN o> ;] #map drop ;
: .key-invite ( o:key -- o:key )
    ke-pk $@ keysize umin
    ke-import @ >im-color 85type <default>
    space .nick  cr ;
: .key-short ( o:key -- o:key )
    ke-nick $. ke-prof $@len IF ."  profile: " ke-prof $@ 85type THEN ;

: dumpkey ( addr u -- ) drop cell+ >o
    .\" x\" " ke-pk $@ 85type .\" \" key?new" cr
    ke-sk @ IF  .\" x\" " ke-sk @ keysize 85type .\" \" ke-sk sec! +seckey" cr  THEN
    '"' emit .nick .\" \" ke-nick $! "
    ke-selfsig $@ drop 64@ 64>d [: '$' emit 0 ud.r ;] $10 base-execute
    ." . d>64 ke-first! " ke-type @ . ." ke-type !"  cr o> ;

: .keys ( -- ) key-table [: cell+ $@ .key ;] #map ;
: dumpkeys ( -- ) key-table [: cell+ $@ dumpkey ;] #map ;

: key>nick ( addrkey u1 -- nick u2 )
    key-table #@ 0= IF  drop ""  EXIT  THEN
    cell+ .ke-nick $@ ;

: .key# ( addr u -- ) key|
    ." Key '" key-table #@ 0= IF drop EXIT THEN
    cell+ ..nick ." ' ok" cr ;

Defer dht-nick?
event: ->search-key  key| over >r dht-nick? r> free throw ;

: .unkey-id ( addr u -- ) <err> 8 umin 85type ." (unknown)" <default> ;

: .key-id ( addr u -- ) key| 2dup key-table #@ 0=
    IF  drop up@ receiver-task = IF
	    <event 2dup save-mem e$, ->search-key [ up@ ]l event>
	    .unkey-id EXIT  THEN
	2dup ['] dht-nick? cmd-nest
	2dup key-table #@ 0= IF  drop .unkey-id EXIT  THEN  THEN
    cell+ ..nick 2drop ;

: .simple-id ( addr u -- ) key| key>nick type ;

:noname ( addr u -- )
    o IF  pubkey @ IF
	    2dup pubkey $@ key| str= 0= IF
		[: ." want: " pubkey $@ key| 85type cr
		  ." got : " 2dup 85type cr ;] $err
		true !!wrong-key!!
	    THEN
	    connect( .key# )else( 2drop )  EXIT
	THEN  THEN
    2dup key-exist? 0= IF
	strict-keys @ !!unknown-key!!
	." Unknown key " 85type cr
    ELSE
	connect( .key# )else( 2drop )
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

: lastkey@ ( -- addr u ) keys $[]# 1- keys sec[]@ ;
: key>default ( -- ) lastkey@ drop >storekey ! ;
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
\g 
\g ### key storage commands ###
\g 
$11 net2o: privkey ( $:string -- )
    \g private key
    \ does not need to be signed, the secret key verifies itself
    $> over keypad sk>pk \ generate pubkey
    keypad ke-pk $@ drop keysize tuck str= 0= !!wrong-key!!
    ke-sk sec! +seckey ;
+net2o: keytype ( n -- )           !!signed?   1 !!>order? 64>n ke-type ! ;
\g key type (0: anon, 1: user, 2: group)
+net2o: keynick ( $:string -- )    !!signed?   2 !!>order? $> ke-nick $!
    nick! ;
\g key nick
+net2o: keyprofile ( $:string -- ) !!signed?   4 !!>order? $> ke-prof $! ;
\g key profile (hash of a resource)
+net2o: keymask ( x -- )                       8 !!>order? 64drop ;
\g key mask
+net2o: keypsk ( $:string -- )     !!signed? $10 !!>order? $> ke-psk sec! ;
\g preshared key, used for DHT encryption
+net2o: +keysig ( $:string -- )  $20 !!>=order? $> ke-sigs $+[]! ;
\g add a key signature
+net2o: keyimport ( n -- )
    pw-level# 0< IF  64>n import#untrusted umin ke-import !
    ELSE  64drop  THEN ;
dup set-current previous

gen-table $freeze
' context-table is gen-table

: key:nest-sig ( addr u -- addr u' flag )
    pk2-sig? dup ?EXIT drop
    2dup + sigsize# - sigsize# >$
    sigpk2size# - 2dup + keysize2 key?new n:>o $> ke-selfsig $!
    sim-nick! off c-state off sig-ok ;
' key:nest-sig key-entry to nest-sig

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

set-current previous

: key-crypt ( -- )
    keypack keypack-all#
    >storekey sec@ dup $20 u<= \ is a secret, no need to be slow
    IF  encrypt$  ELSE  pw-level# encrypt-pw$  THEN ;

0 Value key-sfd \ secret keys
0 Value key-pfd \ pubkeys

: ?key-sfd ( -- fd ) key-sfd "~/.net2o/seckeys.k2o" ?fd dup to key-sfd ;
: ?key-pfd ( -- fd ) key-pfd "~/.net2o/pubkeys.k2o" ?fd dup to key-pfd ;

: key>sfile ( -- )
    keypack keypack-all# ?key-sfd append-file ke-offset 64! ;
: key>pfile ( -- )
    keypack keypack-all# ?key-pfd append-file ke-offset 64! ;

: key>sfile@pos ( 64pos -- ) 64dup 64#-1 64= IF  64drop key>sfile
    ELSE  64>r keypack keypack-all# 64r> ?key-sfd write@pos-file  THEN ;
: key>pfile@pos ( 64pos -- ) 64dup 64#-1 64= IF  64drop key>pfile
    ELSE  64>r keypack keypack-all# 64r> ?key-pfd write@pos-file  THEN ;

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

keysize2 Constant pkrk#

: ]pk+sign ( addr u -- ) +cmdbuf ]sign ;

: pack-key ( type nick u -- )
    now>never
    key:code
      sign[
      rot ulit, keytype $, keynick
      pkc pkrk# ]pk+sign
      skc keysize $, privkey
    end:key ;

also net2o-base
: pack-core ( o:key -- ) \ core without key
    ke-type @ ulit, keytype
    ke-nick $@ $, keynick
    ke-psk sec@ dup IF  $, keypsk  ELSE  2drop  THEN
    ke-prof $@ dup IF  $, keyprofile  ELSE  2drop  THEN ;

: pack-corekey ( o:key -- )
    sign[
    pack-core
    ke-pk $@ +cmdbuf
    ke-selfsig $@ +cmdbuf cmd-resolve> 2drop nestsig
    ke-import @ ulit, keyimport
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
: keynick$ ( o:key -- addr u )
    \g get the annotations with signature
    ['] pack-core gen-cmd$ 2drop
    ke-selfsig $@ tmp$ $+! tmp$ $@ ;
: keypk2nick$ ( o:key -- addr u )
    \g get the annotations with signature
    ['] pack-core gen-cmd$ 2drop
    ke-pk $@ tmp$ $+! ke-selfsig $@ tmp$ $+! tmp$ $@ ;
: mynick-key ( -- o )
    pkc keysize key-table #@ drop cell+ ;
: mynick$ ( -- addr u )
    \g get my nick with signature
    mynick-key .keynick$ ;
: mypk2nick$ ( o:key -- addr u )
    \g get my nick with signature
    mynick-key .keypk2nick$ ;

Variable cp-tmp

: save-pubkeys ( -- )
    key-pfd ?dup-IF  close-file throw  THEN
    ?.net2o
    "~/.net2o/pubkeys.k2o" [: to key-pfd
      key-table [: cell+ $@ drop cell+ >o
	ke-sk sec@ d0= IF  pack-pubkey
	    flush( ." saving " .nick forth:cr )
	    key-crypt ke-offset 64@ key>pfile@pos
	THEN o> ;] #map
    0 to key-pfd ;] save-file  ?key-pfd drop ;

: save-seckeys ( -- )
    key-sfd ?dup-IF  close-file throw  THEN
    ?.net2o
    "~/.net2o/seckeys.k2o" [: to key-sfd
      key-table [: cell+ $@ drop cell+ >o
	ke-sk sec@ d0<> IF  pack-seckey
	    key-crypt ke-offset 64@ key>sfile@pos
	THEN o> ;] #map
    0 to key-sfd ;] save-file  ?key-sfd drop ;

: save-keys ( -- )
    save-pubkeys save-seckeys ;

: +gen-keys ( nick u type -- )
    gen-keys  64#-1 key-read-offset 64!  pkc keysize2 key:new >o
    import#self ke-import !  ke-type !  ke-nick $!  nick!
    skc keysize ke-sk sec!  +seckey
    [ also net2o-base ]
    [: ke-type @ ulit, keytype ke-nick $@ $, keynick ;] gen-cmd$
    [ previous ] [: type pkc keysize2 type ;] $tmp
    now>never c:0key c:hash ['] .sig $tmp ke-selfsig $!
    o> ;

: +keypair ( type nick u -- ) +passphrase +gen-keys ;

: .rvk ." Please write down revoke key: " cr
    skrev $20 bounds DO  ." \ " I 4 85type space I 4 + 4 85type cr 8 +LOOP ;

$40 buffer: nick-buf

: get-nick ( -- addr u )
    ." nick: " nick-buf $40 accept nick-buf swap cr ;
: make-key ( -- )
    key#user get-nick
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

: .key$ ( addr u -- )
    sample-key >o  ke-sk ke-end over - erase
    signed-val validated or!  c-state off  nest-cmd-loop
    signed-val invert validated and!
    .key-short free-key o> ;

: read-keys-loop ( fd -- )  code-key
    >r 0. r@ reposition-file throw
    BEGIN
	r@ file-position throw d>64 key-read-offset 64!
	keypack keypack-all# r@ read-file throw
	keypack-all# = WHILE  try-decrypt do-key
    REPEAT  rdrop  code0-buf ;
: read-key-loop ( -- )
    import#self import-type !
    ?key-sfd read-keys-loop ;
: read-pkey-loop ( -- )
    pw-level# >r -1 to pw-level#  import#manual import-type !
    ?key-pfd read-keys-loop
    r> to pw-level#  ;

: read-keys ( -- )
    read-key-loop read-pkey-loop import#untrusted import-type ! ;

: read-pk2key$ ( addr u -- )
    \g read a nested key into sample-key
    sample-key >o c-state off  sim-nick! on
    pk2-sig? !!sig!! sigpk2size# - 2dup + >r do-nestsig
    r@ keysize2 ke-pk $!
    r> keysize2 + sigsize# ke-selfsig $!
    o>  sim-nick! off ;

: .pk2key$ ( addr u -- )
    read-pk2key$ sample-key >o .key-invite free-key o> ;

\ select key by nick

: >raw-key ( o -- )
    dup 0= !!no-nick!! >o
    ke-pk $@ pkc swap pkrk# umin move
    ke-psk sec@ my-0key sec!
    ke-sk sec@ skc swap key| move
    >sksig o> ;

: >key ( addr u -- )
    key-table @ 0= IF  read-keys  THEN
    nick-key >raw-key ;

: i'm ( "name" -- ) parse-name >key ;
: pk' ( "name" -- addr u )
    parse-name nick>pk ;

: dest-key ( addr u -- ) dup 0= IF  2drop  EXIT  THEN
    nick-key >o o 0= !!unknown-key!!
    ke-psk sec@ state# umin
    ke-pk $@ key| o>
    pubkey $!  dest-0key sec! ;

: dest-pk ( addr u -- ) key2| 2dup key-table #@ 0= IF
	drop key| pubkey $!
    ELSE  cell+ >o
	ke-psk sec@ state# umin
	ke-pk $@ key| o>
	pubkey $!  dest-0key sec!  THEN ;

: replace-key 1 /string { rev-addr u -- o } \ revocation ticket
    key( ." Replace:" cr o cell- 0 .key )
    import#self import-type !
    s" #revoked" dup >r ke-nick $+!
    ke-nick $@ r> - ke-prof $@ ke-psk sec@ ke-sigs ke-type @
    rev-addr pkrk# key?new >o
    ke-type ! [: ke-sigs $+[]! ;] $[]map ke-psk sec! ke-prof $! ke-nick $!
    rev-addr pkrk# ke-pk $!
    rev-addr u + 1- dup c@ 2* - $10 - $10 ke-selfsig $!
    key( ." with:" cr o cell- 0 .key ) o o>
    import#untrusted import-type ! ;

: renew-key ( revaddr u1 keyaddr u2 -- o )
    current-key >o replace-key o>
    >o skc keysize ke-sk sec! o o> ;

\ revokation

4 datesize# + keysize 9 * + Constant revsize#

Variable revtoken

: 0oldkey ( -- ) \ pubkeys can stay
    oldskc keysize erase  oldskrev keysize erase ;

: keymove ( addr1 addr2 -- )  keysize move ;

: revoke-verify ( addr u1 pk string u2 -- addr u flag ) rot >r 2>r c:0key
    sigonlysize# - 2dup 2r> >keyed-hash
    sigdate +date
    2dup + r> ed-verify ;

: >revoke ( skrev -- )  skrev keymove  check-rev? 0= !!not-my-revsk!! ;

: +revsign ( sk pk -- )  sksig -rot ed-sign revtoken $+! bl revtoken c$+! ;

: sign-token, ( sk pk string u2 -- )
    c:0key revtoken $@ 2swap >keyed-hash
    sigdate +date +revsign ;

: revoke-key ( -- addr u )
    skc oldskc keymove  pkc oldpkc keymove  skrev oldskrev keymove
                                           \ backup keys
    oldskrev oldpkrev sk>pk                \ generate revokation pubkey
    gen-keys                               \ generate new keys
    pkc keysize2 revtoken $!               \ my new key
    oldpkrev keysize revtoken $+!          \ revoke token
    oldskrev oldpkrev "revoke" sign-token, \ revoke signature
    skc pkc "selfsign" sign-token,         \ self signed with new key
    "!" revtoken 0 $ins                    \ "!" + oldkeylen+newkeylen to flag revokation
    revtoken $@ gen>host 2drop             \ sign host information with old key
    sigdate +date sigdate datesize# revtoken $+!
    oldskc oldpkc +revsign
    0oldkey revtoken $@ ;

\ invitation

Variable invitations
Variable block-table

event: ->invite ( addr u -- )
    ." invite me: " over >r .pk2key$ r> free throw ctrl L inskey ;
event: ->wakeme ( o -- ) <event ->wake event> ;

: pk2key$-add ( addr u -- )
    sample-key >o import#invited import-type ! cmd:nestsig o>
    import#untrusted import-type !  save-pubkeys ;

: block-add ( addr u -- )
    sigpk2size# - + keysize 2dup block-table #!
    ( tbd: save-blocklist ) ;

: process-invitation ( addr u -- )
    key case
	'y' of  pk2key$-add ." added"    endof
	'n' of  2drop       ." ignored"  endof
	'b' of  block-add   ." blocked"  endof
	2drop
    endcase ;

: filter-invitation? ( addr u -- flag )
    sigpk2size# - +
    dup keysize block-table #@ nip keysize =
    IF drop true  EXIT  THEN
    keysize key-table #@ d0<> ; \ already there

: .invitations ( -- )
    invitations [: ." invite (y/n/b)? " 2dup .pk2key$ process-invitation
    ;] $[]map  invitations $[]off ;

:noname ( addr u -- )
    2dup filter-invitation? IF  2drop EXIT  THEN
    2dup invitations $ins[]sig save-mem [ up@ ]l <hide>
    <event e$, ->invite up@ elit, ->wakeme [ up@ ]l event> stop
; is >invitations
: send-invitation ( pk u -- )
    setup! mypk2nick$ 2>r
    gen-tmpkeys drop tskc swap keypad ed-dh do-keypad sec!
    net2o-code0
    tpkc keysize $, oneshot-tmpkey
    nest[ 2r> $, invite ]tmpnest
    cookie+request
    end-code| ;

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