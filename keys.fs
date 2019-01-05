\ net2o key storage

\ Copyright (C) 2013-2015   Bernd Paysan

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

scope{ config
Variable pw-level# 2 pw-level# ! \ pw-level# 0 is lowest
Variable pw-maxlevel# 4 pw-maxlevel# ! \ pw-maxlevel# is the maximum checked
}scope

\ Keys are passwords and private keys (self-keyed, i.e. private*public key)

cmd-buf0 uclass cmdbuf-o
    maxdata -
    key-salt# uvar keypack
    keypack#  uvar keypack-buf
    key-cksum# uvar keypack-chksum
end-class cmd-keybuf-c

cmd-keybuf-c ' new static-a with-allocater code-key^ !
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

Variable groups[] \ names of groups, sorted by order in groups file

User >storekey
Variable defaultkey

: free-key ( o:key -- o:key )
    \g free all parts of the subkey
    ke-sk sec-free
    ke-sksig sec-free
    ke-pk $free
    ke-nick $free
    ke-selfsig $free
    ke-chat $free
    ke-sigs[] $[]free
    ke-pets[] $[]free
    ke-pets# $free ;

\ key class

0
enum key#anon
enum key#user
enum key#group
drop

\ key import type

0
enum import#self        \ private key
enum import#manual      \ manual import
enum import#scan        \ scan import
enum import#chat        \ seen in chat
enum import#dht         \ dht import
enum import#invited     \ invitation import
enum import#provisional \ provisional key
enum import#untrusted   \ must be last
drop
$1F enum import#new   \ new format
drop

Create imports$ $20 allot imports$ $20 bl fill
"Imscdipu" imports$ swap move

Variable import-type  import#new import-type !

: >im-color# ( mask -- color# )
    8 cells 0 DO  dup 1 and IF  drop I LEAVE  THEN  2/  LOOP ;

Create >im-color  $B600 , $D600 , $9600 , $C600 , $A600 , $8B01 , $8C01 , $E600 ,
DOES> swap >im-color# 7 umin cells + @ attr! ;

: .imports ( mask -- )
    imports$ import#new bounds DO
	1 I imports$ - lshift >im-color
	dup 1 and IF  I c@ emit  THEN  2/ LOOP
    drop <default> ;

Create import-name$
"I myself" s, "manual" s, "scan" s, "chat" s, "dht" s, "invited" s, "provisional" s, "untrusted" s,

: .import-colors ( -- )
    import-name$
    import#untrusted 1+ 0 ?DO
	1 I lshift >im-color count 2dup type <default> space + aligned
    LOOP drop ;

\ sample key

key-entry ' new static-a with-allocater Constant sample-key

Variable key# \ key hash table
Variable nick# \ nick hash table

64Variable key-read-offset

: current-key ( addr u -- o )
    2dup key| key# #@ drop
    dup 0= IF  drop ." unknown key: " 85type cr  0 EXIT  THEN
    cell+ >o ke-pk $! o o> ;

Variable sim-nick!

: nick! ( -- ) sim-nick! @ ?EXIT  o { w^ optr }
    ke-nick $@ nick# #@ d0= IF
	optr cell ke-nick $@ nick# #! 0
    ELSE
	last# cell+ $@len cell/
	optr cell last# cell+ $+!
    THEN  ke-nick# ! ;

: #.nick ( hash -- )
    dup $@ type '#' emit cell+ $@len cell/ . ;

: last-pet@ ( -- addr u )
    ke-pets[] $[]# ?dup-IF  1- ke-pets[] $[]@  ELSE  #0.  THEN ;

: pet! ( -- ) sim-nick! @ ?EXIT  o { w^ optr }
    last-pet@ nick# #@ d0= IF
	optr cell last-pet@ nick# #! 0
    ELSE
	last# cell+ $@len cell/
	optr cell last# cell+ $+!
    THEN  ke-pets[] $[]# 1- ke-pets# $[] ! ;

: key:new ( addr u -- o )
    \G create new key, addr u is the public key
    sample-key >o  ke-sk ke-end over - erase
    key-entry-table @ token-table !
    >storekey @ ke-storekey !
    key-read-offset 64@ ke-offset 64!
    1 import-type @ lshift [ 1 import#new lshift ]L or ke-imports !
    keypack-all# n>64 key-read-offset 64+! o cell- ke-end over -
    2over key| key# #! o>
    current-key ;

0 Value last-key

: key?new ( addr u -- o )
    \G Create or lookup new key
    2dup key| key# #@ drop
    dup 0= IF  drop key:new
    ELSE  nip nip cell+  1 import-type @ lshift over .ke-imports or!  THEN
    dup to last-key ;

\ search for keys - not optimized

: #split ( addr u -- addr u n )
    [: 2dup '#' -scan nip >r
      r@ 0= IF  rdrop 0  EXIT  THEN
      #0. 2over r@ 1+ /string >number
      0= IF  nip drop nip r> swap  ELSE
	  rdrop drop 2drop 0   THEN ;] #10 base-execute ;

: nick-key ( addr u -- o / 0 ) \ search for key nickname
    #split >r nick# #@ 2dup d0= IF  rdrop drop  EXIT  THEN
    r> cells safe/string 0= IF  drop 0  EXIT  THEN  @ ;

: secret-keys# ( -- n )
    0 key# [: cell+ $@ drop cell+ >o ke-sk @ 0<> - o> ;] #map ;
: secret-key ( n -- o/0 )
    0 tuck key# [: cell+ $@ drop cell+ >o ke-sk @ IF
	  2dup = IF  rot drop o -rot  THEN  1+
      THEN  o> ;] #map 2drop ;
: .# ( n -- ) ?dup-IF  '#' emit 0 .r  THEN ;
: .nick-base ( o:key -- )
    ke-nick $.  ke-nick# @ .# ;
: .pet-base ( o:key -- )
    0 ke-pets[] [: space type dup ke-pets# $[] @ .#  1+ ;] $[]map drop ;
: .pet0-base ( o:key -- )
    ke-pets[] $[]# IF  0 ke-pets[] $[]@ type 0 ke-pets# $[] @ .#
    ELSE  .nick-base  THEN ;
: .real-nick ( o:key -- )   ke-imports @ >im-color .nick-base <default> ;

0 Value last-ki

: .nick ( o:key -- )   ke-imports @ dup to last-ki >im-color .pet0-base <default> ;
: .nick+pet ( o:key -- )
    ke-imports @ >im-color .nick-base .pet-base <default> ;

: nick>pk ( nick u -- pk u )
    nick-key ?dup-IF .ke-pk $@ ELSE 0 0 THEN ;
: host.nick>pk ( addr u -- pk u' )
    '.' $split dup 0= IF  2swap  THEN [: nick>pk type type ;] $tmp ;

: key-exist? ( addr u -- o/0 )
    key# #@ IF  cell+  THEN ; 

\ permission modification

26 buffer: perm-chars
0 perm$ count bounds [DO] dup [I] c@ 'a' - perm-chars + c! 1+ [LOOP] drop

: .perm ( permission -- )  1 perm$ count bounds DO
	2dup and 0<> I c@ '-' rot select emit 2*
    LOOP  2drop ;
: permand ( permand permor new -- permand' permor )
    invert tuck and >r and r> ;
: >perm-mod ( permand permor -- permand' permor )
    swap dup 0= IF  drop dup invert  THEN swap ;
: >perm ( addr u -- permand permor )
    \G parse permissions: + adds, - removes permissions,
    \G no modifier sets permissons.
    0 0 ['] or { xt }
    2swap bounds ?DO
	I c@ case
	    '+' of  >perm-mod ['] or to xt endof
	    '-' of  >perm-mod ['] permand to xt  endof
	    '=' of  2drop perm%default dup ['] or to xt  endof
	    'a' - dup 'z' u<=  IF
		perm-chars + c@ 1 swap lshift xt execute
		0 ( dummy for endcase )
	    THEN  endcase
    LOOP ;
: .permandor ( permand permor -- )
    0 { +- }
    1 perm$ count bounds DO  >r
	over r@ and 0= IF  '-' dup +- <> IF  dup to +- emit
	    ELSE  drop  THEN r>  I c@ emit  >r THEN
	dup  r@ and    IF  '+' dup +- <> IF  dup to +- emit
	    ELSE  drop  THEN r>  I c@ emit  >r THEN
	r> 2*
    LOOP  drop 2drop ;

\ read in permission groups, groups is in the .net2o directory

: >group-id ( addr u -- id/-1 )
    -1 0 groups[] [: 2swap 2>r 2 cells /string
      2over string-prefix? IF  2r> nip dup
      ELSE  2r>  THEN  1+ ;] $[]map
    2nip drop ;

: >groups ( addr u pand por -- )
    s" " groups[] $+[]!
    [: { d^ pandor } pandor 2 cells type  type ;]
    groups[] dup $[]# 1- swap $[] $exec ;

: init-groups ( -- )
    "myself"  perm%myself  dup >groups
    "peer"    perm%default dup >groups
    "dht"     perm%dhtroot dup >groups
    "unknown" perm%unknown dup >groups
    "blocked" perm%blocked perm%indirect or dup >groups ;

: .groups ( -- )
    groups[] [: 2dup 2 cells /string type space
      drop 2@ .permandor cr ;] $[]map ;

: .in-groups ( addr u -- )
    bounds ?DO
	I p@+ I - >r 64>n groups[] $[]@ 2 cells /string space type
    r> +LOOP ;

: write-groups ( -- )
    [: ." groups+" getpid 0 .r ;] $tmp .net2o/ 2dup w/o create-file throw >r
    ['] .groups r@ outfile-execute
    r> close-file throw '+' -scan 1- >backup ;

: group-line ( -- )
    parse-name parse-name >perm >groups ;

: read-groups-loop ( -- )
    BEGIN  refill  WHILE  group-line  REPEAT ;

: read-groups ( -- )
    "groups" .net2o-config/ 2dup file-status nip no-file# = IF
	init-groups write-groups 2drop  EXIT
    THEN  >included throw
    ['] read-groups-loop execute-parsing-named-file ;

: groups>mask ( addr u -- mask )
    0 -rot bounds ?DO
	I p@+ I - >r
	64>n dup groups[] $[]# u>= !!no-group!!
	groups[] $[]@ drop 2@ >r and r> or
    r> +LOOP ;

: ?>groups ( mask -- mask' )
    ke-groups $@len 0= IF
	groups[] $[]# 0 DO
	    dup I groups[] $[]@ drop @
	    or over = IF
		I ke-groups c$+!
		I groups[] $[]@ drop cell+ @ invert and
	    THEN
	LOOP
    THEN  drop ;

:noname defers 'cold  groups[] off read-groups ; is 'cold

\ key display

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

: .stripe85 ( addr u -- )  0 -rot bounds ?DO
	dup cells 85colors + @ attr! 1+
	I 4 85type  dup cells 85colors + @ attr! 1+
    I 4 + 4 85type <default> cr 8 +LOOP  drop ;
: .import85 ( addr u -- )
    ke-imports @ >im-color 85type <default> ;
: .rsk ( nick u -- )
    skrev $20 .stripe85 space type ."  (keep offline copy!)" cr ;
: .key ( addr u -- )
    ." nick:   " .nick cr
    ." pubkey: " ke-pk $@ 85type cr
    ke-sk @ IF
	." seckey: " ke-sk sec@ .black85 ."  (keep secret!)" cr  THEN
    ke-wallet @ IF
	." wallet: " ke-wallet sec@ .black85 ."  (keep secret!)" cr  THEN
    ." valid:  " ke-selfsig $@ .sigdates cr
    ." groups: " ke-groups $@ .in-groups cr
    ." perm:   " ke-mask @ .perm cr ;
: .key-rest ( o:key -- o:key )
    ke-pk $@ key| .import85
    ke-wallet sec@ nip IF
	wallet( space ke-wallet sec@ .black85 )else( ."  W" )
    ELSE  wallet( $15 )else( 2 ) spaces THEN
    ke-selfsig $@ space .sigdates
    ke-groups $@ 2dup .in-groups groups>mask invert
    space ke-mask @ and -1 swap .permandor
    #tab emit ke-imports @ .imports
    space .nick+pet ;
: .key-list ( o:key -- o:key )
    ke-offset 64@ 64>d keypack-all# fm/mod nip 3 .r space
    .key-rest cr ;

\ print invitations

: .key-invite ( o:key -- o:key )
    ke-pk $@ keysize umin
    ke-imports @ >im-color 85type <default>
    space .nick space ;
: .key-short ( o:key -- o:key )
    ke-nick $. ke-prof $@len IF ."  profile: " ke-prof $@ 85type THEN ;

\ print sorted list of keys by nick

Variable key-list[]
: $ins[]key ( o:key $array -- pos )
    \G insert O(log(n)) into pre-sorted array
    \G @var{pos} is the insertion offset or -1 if not inserted
    { a[] } 0 a[] $[]#
    BEGIN  2dup u<  WHILE  2dup + 2/ { left right $# }
	    ke-nick $@ $# a[] $[] @ .ke-nick $@ compare dup 0= IF
		drop ke-nick# @ $# a[] $[] @ .ke-nick# @ -  THEN
	    0< IF  left $#  ELSE  $# 1+ right  THEN
    REPEAT  drop >r
    o { w^ ins$0 } ins$0 cell a[] r@ cells $ins r> ;
: keys>sort[] ( -- )  key-list[] $free
    key# [: cell+ $@ drop cell+ >o key-list[] $ins[]key drop o> ;] #map ;
: list-keys ( -- )
    keys>sort[]
    ." colors: " .import-colors cr
    ." num pubkey                                   "
    wallet( ." wallet             " )
    ."   date                     grp+prm h nick" cr
    key-list[] $@ bounds ?DO  I @ ..key-list  cell +LOOP ;
: list-nicks ( -- )
    nick# [: dup $. ." :" cr cell+ $@ bounds ?DO
      I @ ..key-list  cell +LOOP ;] #map ;

\ list of secret keys to select from

Variable secret-nicks[]
Variable secret-nicks#
: .secret-nicks-insert ( -- )
    secret-nicks[] $free  secret-nicks# $free
    0 key# [: cell+ $@ drop cell+ >o ke-sk @ IF
	  secret-nicks[] $ins[]key >r
	  dup { c^ x } x 1 secret-nicks# r> $ins  1+
      THEN o> ;] #map drop ;
: nick#>key# ( n1 -- n2 )
    secret-nicks# $@ rot safe/string IF  c@  ELSE  drop -1  THEN ;
: .secret-nicks ( -- )
    .secret-nicks-insert
    secret-nicks[] $[]# 0 ?DO
	I 1 ['] .r #36 base-execute space
	I secret-nicks[] $[] @ ..key-rest cr
    LOOP ;

\ dump keys

: dumpkey ( addr u -- ) drop cell+ >o
    .\" x\" " ke-pk $@ 85type .\" \" key?new" cr
    ke-sk @ IF  .\" x\" " ke-sk @ keysize 85type .\" \" ke-sk sec! +seckey" cr  THEN
    '"' emit .nick .\" \" ke-nick $! "
    ke-selfsig $@ drop 64@ 64>d [: '$' emit 0 ud.r ;] $10 base-execute
    ." . d>64 ke-first! " ke-type @ . ." ke-type !"  cr o> ;

: .keys ( -- ) key# [: ." index: " dup $@ 85type cr cell+ $@
	 drop cell+ ..key ;] #map ;
: dumpkeys ( -- ) key# [: cell+ $@ dumpkey ;] #map ;

: key>o ( addrkey u1 -- o / 0 )
    key| key# #@ 0= IF  drop 0  EXIT  THEN  cell+ ;
: key>nick ( addrkey u1 -- nick u2 )
    \G convert key to nick
    key>o dup IF  .ke-nick $@  ELSE  0  THEN ;
: key>key ( addrkey u1 -- key u2 )
    \G expand key to full size and check if we know it
    key>o dup IF  .ke-pk $@  ELSE  0  THEN ;

: .key# ( addr u -- ) key|
    ." Key '" key# #@ 0= IF drop EXIT THEN
    cell+ ..nick ." ' ok" cr ;

Forward dht-nick?
Variable keysearchs#
event: :>search-key ( $addr -- )
    { w^ key } key $@ dht-nick? key $free
    1 keysearchs# +!@ drop ;

: .unkey-id ( addr u -- ) <err> 8 umin 85type ." (unknown)" <default>
    [ 1 import#untrusted lshift ]L to last-ki ;

: .key-id ( addr u -- )  last# >r  key| 2dup key# #@ 0=
    IF  drop keysearchs# @ 1+ >r
	<event 2dup $make elit, :>search-key ?query-task event|
	BEGIN  keysearchs# @ r@ - 0<  WHILE  <event  query-task event|  REPEAT
	rdrop  2dup key# #@ 0= IF  drop .unkey-id  r> to last# EXIT  THEN
    THEN
    cell+ ..nick 2drop r> to last# ;

: .con-id ( o:connection -- ) pubkey $@ .key-id ;

: .simple-id ( addr u -- ) last# >r
    key>o dup IF  ..nick-base  ELSE  drop ." unknown"  THEN
    r> to last# ;

: check-key ( addr u -- )
    o IF  pubkey @ IF
	    2dup pubkey $@ key| str= 0= IF
		[: ." want: " pubkey $@ key| 85type cr
		  ." got : " 2dup 85type cr ;] $err
		true !!wrong-key!!
	    THEN
	    connect( .key# )else( 2drop )  EXIT
	THEN  THEN
    2dup key-exist?
    ?dup-0=-IF  perm%unknown  ELSE  .ke-mask @  THEN  tmp-perm !
    connect( 2dup .key# )
    tmp-perm @ perm%blocked and IF
	[: ." Unknown key, connection refused: " 85type cr ;] $err
	true !!connect-perm!!
    ELSE  2drop  THEN ;

: search-key ( pkc -- o skc )
    keysize key# #@ 0= !!unknown-key!!
    cell+ dup .ke-sk sec@ 0= !!unknown-key!! ;
: search-key? ( pkc -- false / o skc )
    keysize key# #@ 0= IF  drop 0  EXIT  THEN
    cell+ dup .ke-sk sec@ 0= IF  2drop 0  EXIT  THEN ;

\ apply permissions&groups

: apply-permission ( permand permor o:key -- permand permor o:key )
    over ke-mask @ and over or ke-mask ! .key-list ;

: -group-perm ( o:key -- )
    ke-groups $@ groups>mask invert ke-mask and! ;
: +group-perm ( o:key -- )
    ke-groups $@ groups>mask        ke-mask or! ;

: add-group ( id o:key -- )
    dup -1 = !!no-group!! -group-perm u>64 cmdtmp$ ke-groups $+! +group-perm ;
: set-group ( id o:key -- )
    dup -1 = !!no-group!! -group-perm u>64 cmdtmp$ ke-groups $! +group-perm ;
: sub-group ( id o:key -- )
    dup -1 = !!no-group!! -group-perm u>64 cmdtmp$ ke-groups $@ 2over search
    IF   nip >r nip ke-groups dup $@len r> - rot $del
    ELSE  2drop 2drop  THEN +group-perm ;

: apply-group ( addr u o:key -- )
    over c@ '+' = IF  1 /string >group-id add-group .key-list  EXIT  THEN
    over c@ '-' = IF  1 /string >group-id sub-group .key-list  EXIT  THEN
    >group-id set-group .key-list ;

\ get passphrase

3 Value passphrase-retry#
$100 Constant max-passphrase# \ 256 characters should be enough...
max-passphrase# buffer: passphrase

: passphrase-in ( addr u -- addr u )
    "PASSPHRASE" getenv 2dup d0= IF  2drop type
	passphrase dup max-passphrase# accept* cr
    ELSE  2nip  THEN ;

: >passphrase ( addr u -- addr u )
    \G create a 512 bit hash of the passphrase
    no-key >c:key c:hash
    keccak-padded c:key> keccak-padded keccak#max 2/ ;

: get-passphrase ( addr u -- addr u )
    passphrase-in >passphrase ;

Variable keys

: lastkey@ ( -- addr u ) keys $[]# 1- keys sec[]@ ;
: key>default ( -- ) lastkey@ drop >storekey ! ;
: +key ( addr u -- ) keys sec+[]! ;
: +passphrase ( addr u -- )  get-passphrase +key ;
: +checkphrase ( addr u -- flag ) get-passphrase lastkey@ str= ;
: +newphrase ( -- )
    BEGIN
	s" Passphrase: " +passphrase
	s" Retype pls: " +checkphrase 0= WHILE
	    cr ."  didn't match, try again please" cr
    REPEAT cr ;

: ">passphrase ( addr u -- ) >passphrase +key ;
: >seckey ( -- addr u )
    ke-sk @ ke-pk $@ drop keypad ed-dh ;
: +seckey ( -- ) >seckey +key ;

\ "" ">passphrase \ following the encrypt-everything paradigm,
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

Variable save-keys-again
Variable key-version
: key-version$ "1" ;
key-version$ evaluate Constant key-version#

: new-pet? ( addr u -- addr u flag )
    0 ke-pets[] [: rot >r 2over str= r> or ;] $[]map 0= ;

scope{ net2o-base

cmd-table $@ inherit-table key-entry-table
\g 
\g ### key storage commands ###
\g
$2 net2o: slit ( #lit -- ) \g deprecated slit version
    p@ key-version @ 0= IF  zz>n save-keys-again on  ELSE  64invert  THEN ;
$F net2o: kversion ( $:string -- ) \g key version
    $> s>unumber? IF  drop  ELSE  2drop 0  THEN  dup key-version !
    key-version# u< save-keys-again or! ;
$11 net2o: privkey ( $:string -- )
    \g private key
    \ does not need to be signed, the secret key verifies itself
    !!unsigned? $40 !!>=order?
    keypack c@ $F and ke-pwlevel !
    $> over keypad sk>pk \ generate pubkey
    keypad ke-pk $@ drop keysize tuck str= 0= !!wrong-key!!
    ke-sk sec! +seckey "\0" ke-groups $! 0 groups[] $[]@ drop @ ke-mask ! ;
+net2o: keytype ( n -- )           !!signed?   1 !!>order? 64>n ke-type ! ;
    \g key type (0: anon, 1: user, 2: group)
+net2o: keynick ( $:string -- )    !!signed?   2 !!>order? $> ke-nick $!
    \g key nick
    nick! ;
+net2o: keyprofile ( $:string -- ) !!signed?   4 !!>order? $> ke-prof $! ;
    \g key profile (hash of a resource)
+net2o: keymask ( x -- )         !!unsigned? $40 !!>=order? 64>n
    \g key access right mask
    1 import-type @ lshift
    [ 1 import#self lshift 1 import#new lshift or ]L
    and 0= IF  drop perm%default  THEN  dup ke-mask or! ?>groups ;
+net2o: keygroups ( $:groups -- ) !!unsigned? $20 !!>order? $>
    \g access groups
    1 import-type @ lshift
    [ 1 import#self lshift 1 import#new lshift or ]L
    and 0= IF  2drop "\x01"  THEN
    2dup ke-groups $! groups>mask ke-mask ! ;
+net2o: +keysig ( $:string -- )  !!unsigned? $10 !!>=order? $> ke-sigs[] $+[]! ;
    \g add a key signature
+net2o: keyimport ( n -- )       !!unsigned? $10 !!>=order?
    config:pw-level# @ 0< IF  64>n
	dup [ 1 import#new lshift ]L and 0= IF
	    import#untrusted umin 1 swap lshift [ 1 import#new lshift ]L or
	ELSE
	    [ 2 import#untrusted lshift 1- 1 import#new lshift or ]L and
	THEN
	ke-imports or!
    ELSE  64drop  THEN ;
+net2o: rskkey ( $:string --- )
    \g revoke key, temporarily stored
    \ does not need to be signed, the revoke key verifies itself
    !!unsigned? $80 !!>=order?
    $> 2dup skrev swap key| move ke-pk $@ drop check-rev? 0= !!not-my-revsk!!
    pkrev keysize2 erase  ke-rsk sec! ;
+net2o: keypet ( $:string -- )  !!unsigned?  $>
    new-pet? IF
	ke-pets[] $+[]! pet!  EXIT
    THEN  2drop ;
+net2o: walletkey ( $:seed -- ) !!unsigned?  $>
    ke-wallet sec! ;
+net2o: avatar ( $:string -- ) !!signed?   8 !!>order? $> ke-avatar $! ;
    \g key profile (hash of a resource)
}scope

key-entry-table $save

' context-table is gen-table

: key:nest-sig ( addr u -- addr u' flag )
    pk2-sig? dup ?EXIT drop
    2dup + sigsize# - sigsize# >$
    sigpk2size# - 2dup + keysize2 key?new n:>o $> ke-selfsig $!
    sim-nick! off c-state off sig-ok ;
' key:nest-sig key-entry to nest-sig

key-entry-table @ sample-key .token-table !

: key:code ( -- )
    code-key  cmdlock lock
    keypack keypack-all# erase
    cmdreset init-reply also net2o-base ;
comp: :, also net2o-base ;

scope{ net2o-base

: end:key ( -- )
    end-with previous cmdlock unlock ;
comp: :, previous ;

}scope

: key-crypt ( -- )
    keypack keypack-all#
    >storekey sec@ dup $20 u<= \ is a secret, no need to be slow
    IF  encrypt$  ELSE  config:pw-level# @ encrypt-pw$  THEN ;

0 Value key-sfd \ secret keys
0 Value key-pfd \ pubkeys

\ legacy for early versions of net2o prior 20160606

: net2o>keys { addr u -- }
    addr u .net2o/  addr u .keys/ rename-file drop ;
: ?legacy-keys ( flag -- )
    \ !!FIXME!! needs to be removed when all current users
    \ have migrated
    IF
	"pubkeys.k2o" net2o>keys
	"seckeys.k2o" net2o>keys
    THEN ;

: gen-keys-dir ( -- )
    init-dirs ?.net2o/keys ?legacy-keys
    groups[] $[]# 0= IF  read-groups  THEN ;

: ?fd-keys ( fd addr u -- fd' ) { addr u } dup ?EXIT drop
    gen-keys-dir
    addr u r/w open-file dup no-file# = IF
	2drop addr u r/w create-file
    THEN  throw ;

: ?key-sfd ( -- fd )
    key-sfd "seckeys.k2o" .keys/ ?fd-keys dup to key-sfd ;
: ?key-pfd ( -- fd )
    key-pfd "pubkeys.k2o" .keys/ ?fd-keys dup to key-pfd ;

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

\ key generation
\ for reproducibility of the selfsig, always use the same order:
\ "pubkey" newkey <n> keytype "nick" keynick "sig" keyselfsig

User pk+sig$

keysize2 Constant pkrk#

: ]pk+sign ( addr u -- ) +cmdbuf ]sign ;

also net2o-base
: pack-core ( o:key -- ) \ core without key
    ke-type @ ulit, keytype
    ke-nick $@ $, keynick
    ke-prof $@ dup IF  $, keyprofile  ELSE  2drop  THEN
    ke-avatar $@ dup IF  $, avatar  ELSE  2drop  THEN ;

: pack-signkey ( o:key -- )
    sign[
    pack-core
    ke-pk $@ +cmdbuf
    ke-selfsig $@ +cmdbuf cmd-resolve> 2drop nestsig ;

: pack-corekey ( o:key -- )
    pack-signkey
    ke-imports @ ulit, keyimport
    ke-mask @  ke-groups $@len IF
	ke-groups $@ 2dup $, keygroups
	groups>mask invert and  THEN
    ?dup-IF  ulit, keymask  THEN
    ke-pets[] [: $, keypet ;] $[]map
    ke-storekey @ >storekey ! ;
previous

: pack-pubkey ( o:key -- )
    key:code
      key-version$ $, version
      pack-corekey
    end:key ;
: pack-outkey ( o:key -- )
    key:code
      "n2o" net2o-base:4cc,
      key-version$ $, version
      pack-signkey
    end:key ;
: pack-seckey ( o:key -- )
    key:code
      key-version$ $, version
      pack-corekey
      ke-sk sec@ sec$, privkey
      ke-rsk sec@ dup IF  sec$, rskkey  ELSE  2drop  THEN
      ke-wallet sec@ dup IF  sec$, walletkey  ELSE  2drop  THEN
    end:key ;
: keynick$ ( o:key -- addr u )
    \G get the annotations with signature
    ['] pack-core gen-cmd$ 2drop
    ke-selfsig $@ tmp$ $+! tmp$ $@ ;
: keypk2nick$ ( o:key -- addr u )
    \G get the annotations with signature
    ['] pack-core gen-cmd$ 2drop
    ke-pk $@ tmp$ $+! ke-selfsig $@ tmp$ $+! tmp$ $@ ;
: mynick-key ( -- o )
    pk@ key| key# #@ drop cell+ ;
: mynick$ ( -- addr u )
    \G get my nick with signature
    mynick-key .keynick$ ;
: mypk2nick$ ( o:key -- addr u )
    \G get my nick with signature
    mynick-key .keypk2nick$ ;
: key-sign ( o:key -- o:key )
    ['] pack-core gen-cmd$
    [: type ke-pk $@ type ;] $tmp
    now>never c:0key c:hash ['] .sig $tmp ke-selfsig $! ;

Variable cp-tmp

: sec-key? ( o:key -- flag )
    ke-sk sec@ d0<>
    ke-groups $@ $01 scan nip 0= and ;

: save-pubkeys ( -- )
    key-pfd ?dup-IF  close-file throw  THEN
    "pubkeys.k2o" .keys/ [: to key-pfd
      key# [: cell+ $@ drop cell+ >o
	sec-key? 0= IF  pack-pubkey
	    flush( ." saving " .nick forth:cr )
	    key-crypt ke-offset 64@ key>pfile@pos
	THEN o> ;] #map
    0 to key-pfd ;] save-file  ?key-pfd drop ;

: save-seckeys ( -- )
    key-sfd ?dup-IF  close-file throw  THEN
    "seckeys.k2o" .keys/ [: to key-sfd
      key# [: cell+ $@ drop cell+ >o
	sec-key? IF  pack-seckey
	    config:pw-level# @ >r  ke-pwlevel @ config:pw-level# !
	    key-crypt ke-offset 64@ key>sfile@pos
	    r> config:pw-level# !
	THEN o> ;] #map
    0 to key-sfd ;] save-file  ?key-sfd drop ;

: save-keys ( -- )  ?.net2o/keys drop
    save-pubkeys save-seckeys ;

\ respond to scanning keys

in net2o forward pklookup

true Value scan-once?

: scanned-key-in ( addr u -- )
    ." scanned "  2dup .key-id cr
    key| key# #@ IF
	cell+ >o [ 1 import#scan lshift ]L ke-imports or!
	.key-list cr o>
	save-keys
    ELSE  drop  THEN ;
: ?scan-level ( -- )
    [IFDEF] android [ also android ]
	level# @ 0> scan-once? and level# +!  [ previous ]
    [THEN] ;

: scanned-key ( addr u -- )
    scanned-key-in ?scan-level ;
: scanned-hash ( addr u -- )
    ." hash: " 85type cr ;
: scanned-keysig ( addr u -- )
    ." sig: " 85type cr
    ?scan-level ;
: scanned-secret ( addr u -- )
    ." secret: " 85type cr
    ?scan-level ;
: scanned-payment ( addr u -- )
    ." payment: " 85type cr
    ?scan-level ;

Create scanned-x
' noop , \ stub for ownkey
' scanned-key ,
' scanned-keysig ,
' scanned-hash ,
' scanned-secret ,
' scanned-payment ,

here scanned-x - cell/ constant scanned-max#

Variable lastscan$

: lastscan? ( addr u tag -- flag )
    >r $make { w^ just$ } r> just$ c$+!
    just$ $@ lastscan$ $@ str=
    just$ @ lastscan$ $!buf ;
: scan-result ( addr u tag -- )
    dup 2over rot lastscan? IF drop 2drop EXIT THEN
    dup scanned-max# u< IF  cells scanned-x + perform
    ELSE  ." unknown tag " hex. ." scanned " 85type cr ?scan-level  THEN ;

\ generate keys

: sksig! ( -- )
    ke-pk $@ ke-sk sec@ c:0key >keyed-hash keypad keysize keccak>
    keypad keysize ke-sksig sec! ;

: +gen-keys ( nick u type -- )
    gen-keys  64#-1 key-read-offset 64!
    pkc keysize2 key:new >o o to my-key-default  o to my-key
    [ 1 import#self lshift 1 import#new lshift or ]L ke-imports !
    ke-type !  ke-nick $!  nick!
    config:pw-level# @ ke-pwlevel !  perm%myself ke-mask !
    skc keysize ke-sk sec!  +seckey
    skrev keysize ke-rsk sec!
    sksig!
\    $10 rng$ ke-wallet sec! \ wallet key is just $10
    key-sign o> ;

: this-key-sign ( -- )
    my-key >r o to my-key  key-sign  r> to my-key ;

: dummy-key ( raddr u nick u -- o )
    \G Generate a deterministic key based on the address and our sksig
    2>r
    2dup sksig@ keyed-hash#128 sk1 swap move sk1 pk1 sk>pk
    sksig@ 2over keyed-hash#128 skrev swap move skrev pkrev sk>pk
    sk1 pkrev skc pkc ed-keypairx 2r>
    import#provisional import-type !
    pkc keysize2 key:new >o ke-pets[] $+[]! ke-nick $! nick!
    skc keysize ke-sk sec!  skrev keysize ke-rsk sec!  sksig!
    perm%default ke-mask ! "\x01" ke-groups $!
    this-key-sign o o> ;

$40 buffer: nick-buf

: get-nick ( -- addr u )
    ." nick: " nick-buf $40 accept nick-buf swap -trailing cr ;

false value ?yes
: yes? ( addr u -- flag )
    ?yes IF  2drop true  ELSE  type ."  (y/N)" key cr 'y' =  THEN ;

: ?rsk ( -- )
    pk@ key| key-exist? dup 0= IF  drop  EXIT  THEN
    >o ke-rsk sec@ dup 0= IF  2drop o>  EXIT  THEN
    ." You still haven't stored your revoke key securely off-line." cr
    s" Paper and pencil ready?" yes? IF
	.stripe85
	s" Written down?" yes? IF
	    s" You won't see this again! Delete?" yes?
	    IF ke-rsk sec-free  save-keys
		." revoke key deleted." cr o>  EXIT  THEN  THEN
    ELSE  2drop  THEN
    ." I'm keeping your revoke key.  This will show up again." cr o> ;

\ read key file

: try-decrypt-key ( key u1 -- addr u2 flag )
    keypack keypack-d keypack-all# move
    keypack-d keypack-all# 2swap
    dup $20 = IF  decrypt$  ELSE
	keypack c@ $F and config:pw-maxlevel# @ <=
	IF  decrypt-pw$  ELSE  2drop false  THEN
    THEN ;

: try-decrypt ( flag -- addr u / 0 0 ) { flag }
    keys $[]# 0 ?DO
	I keys sec[]@ dup keysize = flag xor IF
	    try-decrypt-key IF
		I keys $[] @ dup >storekey ! defaultkey !
		unloop  EXIT  THEN  THEN
	2drop
    LOOP  0 0 ;

: ?perm ( o:key -- )
    ke-sk sec@ nip dup IF  perm%myself  ELSE  perm%default  THEN  ke-mask !
    IF  "\x00"  ELSE  "\x01"  THEN  ke-groups $! ;

: ?wallet ( o:key -- )
    ke-sk sec@ nip IF
	ke-wallet sec@ nip 0= IF
	    $10 rng$ ke-wallet sec!  save-keys-again on
	THEN
    THEN ;

: do-key ( addr u / 0 0  -- )  key-version off
    dup 0= IF  2drop  EXIT  THEN
    sample-key >o ke-sk ke-end over - erase  do-cmd-loop
    key-version @ key-version# u< save-keys-again or!
    ( last-key .?wallet ) o> ;

: .key$ ( addr u -- )
    sample-key >o  ke-sk ke-end over - erase
    signed-val validated or!  c-state off  nest-cmd-loop
    signed-val invert validated and!
    .key-short free-key o> ;

: read-keys-loop ( fd -- )  save-keys-again off
    code-key
    >r #0. r@ reposition-file throw
    BEGIN
	r@ file-position throw d>64 key-read-offset 64!
	keypack keypack-all# r@ read-file throw
	keypack-all# = WHILE
	    import-type @ import#self = try-decrypt do-key
	REPEAT  rdrop  code0-buf ;
: migrate-key-loop ( -- )  secret-keys# >r
    old-pw-diffuse  ?key-sfd read-keys-loop  new-pw-diffuse
    secret-keys# r> u> IF
	[: ." Migrating password hash to ECC+keccak" cr ;]
	info-color ['] color-execute do-debug
	save-keys-again on
    THEN ;
: read-key-loop ( -- )
    import#self import-type !  secret-keys# >r
    ?key-sfd read-keys-loop
    secret-keys# r> = IF  migrate-key-loop  THEN
    save-keys-again @ IF  save-seckeys      THEN ;
: read-pkey-loop ( -- )
    lastkey@ drop defaultkey ! \ at least one default key available
    -1 config:pw-level#
    [: import#new import-type !
      ?key-pfd read-keys-loop
      save-keys-again @ IF  save-keys  THEN ;] !wrapper ;

: read-keys ( -- )
    read-key-loop read-pkey-loop import#new import-type ! ;

: read-pk2key$ ( addr u -- )
    \g read a nested key into sample-key
    sample-key >o c-state off  sim-nick! on
    pk2-sig? !!sig!! sigpk2size# - 2dup + >r do-nestsig
    r@ keysize2 ke-pk $!
    r> keysize2 + sigsize# ke-selfsig $!
    o>  sim-nick! off ;

: .pk2key$ ( addr u -- )
    read-pk2key$ sample-key >o
    [ 1 import#invited lshift 1 import#new lshift or ]L ke-imports !
    .key-invite free-key o> ;

\ select key by nick

Forward !my-addr$

: raw-key! ( o:key -- )
    ke-sksig @ 0= IF  sksig!  THEN
    ke-pk $@ pkc pkrk# smove
    ke-sk sec@ skc swap key| move
    ke-sksig sec@ sksig keysize smove ;

: >raw-key ( o -- )
    dup 0= !!no-nick!! dup to my-key-default .raw-key!  !my-addr$ ;

: >key ( addr u -- )
    key# @ 0= IF  read-keys  THEN
    nick-key >raw-key ;

: i'm ( "name" -- ) parse-name >key ;
: pk' ( "name" -- addr u )
    parse-name nick>pk ;

: dest-key ( addr u -- ) dup 0= IF  2drop  EXIT  THEN
    nick-key >o o 0= !!unknown-key!!
    ke-pk $@ o>
    pubkey $! ;

: dest-pk ( addr u -- ) key2| 2dup key| key# #@ 0= IF
	drop  perm%unknown
    ELSE  cell+ >o 2drop
	ke-pk $@ ke-mask @ o>  THEN
    perm-mask ! pubkey $! ;

: replace-key 1 /string { rev-addr u -- o } \ revocation ticket
    key( ." Replace:" cr .key )
    import#self import-type !
    s" #revoked" dup >r ke-nick $+!
    ke-nick $@ r> - ke-prof $@ ke-sigs[] ke-type @
    rev-addr pkrk# key?new >o
    ke-type ! [: ke-sigs[] $+[]! ;] $[]map ke-prof $! ke-nick $!
    rev-addr pkrk# ke-pk $!
    rev-addr u + 1- dup c@ 2* - $10 - $10 ke-selfsig $!
    key( ." with:" cr .key ) o o>
    import#new import-type ! ;

: renew-key ( revaddr u1 keyaddr u2 -- o )
    current-key >o replace-key o>
    >o skc keysize ke-sk sec! o o> ;

\ generate new key

: out-key ( o -- )
    >o pack-outkey ['] .nick-base $tmp fn-sanitize o>
    [: ." ~/" type ." .n2o" ;] $tmp w/o create-file throw
    >r cmdbuf$ r@ write-file throw r> close-file throw ;
: out-me ( -- )
    pk@ key| key# #@ 0= !!unknown-key!! \ well, you should know yourself
    cell+ out-key ;

$Variable dhtroot.n2o

: +dhtroot ( -- )
    defaultkey @ >storekey !
    import#manual import-type !  64#-1 key-read-offset 64!
    dhtroot.n2o $@ do-key
    last-key >o "\x02" ke-groups $! perm%dhtroot ke-mask ! o>
    import#new import-type ! ;

: new-key, ( nickaddr u -- )
    ?check-rng \ before generating a key, check the rng for health
    key>default
    key#user +gen-keys
    secret-keys# 1- secret-key >raw-key  lastkey@ drop defaultkey !
    out-me +dhtroot save-keys ;

: new-key ( nickaddr u -- )
    +newphrase new-key, ;

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

: >revoke ( skrev -- )  skrev keymove  pkc check-rev? 0= !!not-my-revsk!! ;

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

event: :>invite ( addr u -- )
    ." invite me: " over >r .pk2key$ cr r> free throw ctrl L inskey ;

: pk2key$-add ( addr u perm -- ) { perm }
    sample-key >o import#invited import-type ! cmd:nestsig
    perm ke-mask !
    import#new import-type !  save-pubkeys o> ;

: x-erase ( len -- )  edit-curpos !
    xedit-startpos  edit-curpos @ spaces  xedit-startpos ;

: invite-key ( addr u -- key )
    2dup x-width { addr u len }
    BEGIN  addr u type key  len x-erase
	dup ctrl Z =
    WHILE  drop  BEGIN  key ctrl L =  UNTIL  REPEAT ;

: process-invitation ( addr u -- )
    s" invite (y/n/b)?" invite-key
    case
	'y' of  perm%default pk2key$-add  ." added" cr   endof
	'b' of  perm%blocked pk2key$-add  ." blocked" cr endof
	2drop ." ignored" cr
    endcase ;

: add-invitation ( addr u -- ) \ add invitation without asking
    perm%default pk2key$-add ;

: filter-invitation? ( addr u -- flag )
    sigpk2size# - + keysize key# #@ d0<> ; \ already there

: .invitations ( -- )
    invitations [: 2dup .pk2key$ cr process-invitation ;] $[]map
    invitations $[]free ;

: queue-invitation ( addr u -- )
    invitations $[]# >r
    2dup invitations $ins[]sig drop
    invitations $[]# r> <> IF
	save-mem main-up@ <hide>
	<event e$, :>invite main-up@ event|
    ELSE  2drop  THEN ;

forward .sigqr
event: :>show-keysig ( $addr -- )
    { w^ pk } msg( ." Sign invitation QR" forth:cr )
    pk $@ 2dup filter-invitation? 0= IF
	msg( ." Add invitation " 2dup 85type forth:cr )
	2dup add-invitation  THEN
    .sigqr pk $free ;

: >invitations ( addr u -- )
    qr-crypt? IF
	msg( ." QR invitation with signature" forth:cr )
	<event $make elit, :>show-keysig main-up@ event>
    ELSE
	2dup filter-invitation? IF  2drop EXIT  THEN
	msg( ." queue invitation" forth:cr )
	queue-invitation
    THEN ;

also net2o-base

: invite-me ( -- )
    [: 0key, nest[ mypk2nick$ $, pubkey $@ key| $, invite cookie+request
      ]tmpnest end-cmd ;] is expect-reply? ;
: qr-challenge, ( -- )
    $10 rng$ 2dup $, qr-key $8
    msg( ." challenge: " 2over 85type space 2dup xtype forth:cr )
    c:0key >keyed-hash
    qr-hash $40 c:hash@ qr-hash $10 $, qr-challenge ;
: qr-invite-me ( -- )
    [: 0key, nest[ qr-challenge,
      mypk2nick$ $, pubkey $@ key| $, invite cookie+request
      ]tmpnest end-cmd ;] is expect-reply? ;
: send-invitation ( -- ) 
    setup!  +resend-cmd  gen-tmpkeys
    ['] connect-rest rqd?
    cmd( ind-addr @ IF  ." in" THEN ." direct connect" forth:cr )
    ivs( ." gen request" forth:cr )
    net2o-code0
    net2o-version $, version?  0key,
    nest[ cookie, ]nest
    tpkc keysize $, receive-tmpkey
    tmpkey-request tmp-secret,
    nest[ request-invitation request, ]nest
    close-tmpnest
    ['] push-cmd IS expect-reply?
    end-code|
    net2o:dispose-context ;
: send-qr-invitation ( -- success-bit )
    setup!  +resend-cmd  gen-tmpkeys
    ['] connect-rest rqd?
    cmd( ind-addr @ IF  ." in" THEN ." direct connect" forth:cr )
    ivs( ." gen request" forth:cr )
    net2o-code0
    net2o-version $, version?  0key,
    nest[ cookie, request, ]nest
    tpkc keysize $, receive-tmpkey
    tmpkey-request tmp-secret,
    nest[ request-qr-invitation request, ]nest
    close-tmpnest
    ['] push-cmd IS expect-reply?
    end-code| invite-result#
    net2o:dispose-context ;
previous

forward >qr-key
event: :>?scan-level ( -- ) ?scan-level ;
event: :>qr-invitation { task w^ pk -- }
    pk $@ keysize2 /string >qr-key
    pk $@ keysize2 umin [: net2o:pklookup send-qr-invitation ;] catch
    IF    2drop ." send qr invitation, aborted" 0
    ELSE  ." sent qr invitation, got " dup hex. THEN
    forth:cr
    0= IF  <event :>?scan-level task event>  THEN  pk $free ;

: scanned-ownkey { d: pk -- }
    pk scanned-key-in
    <event up@ elit, pk $10 + $make elit, :>qr-invitation ?query-task event> ;
\ the idea of scan an own key is to send a invitation,
\ and receive a signature that proofs the scanned device
\ has access to the secret key
' scanned-ownkey scanned-x qr:ownkey# cells + !

\ key api helpers

: del-last-key ( -- )
    keys $[]# 1- keys $[] sec-free
    keys $@len cell- keys $!len ;

: storekey! ( -- )
    >seckey keys $[]# 0 ?DO  2dup I keys sec[]@ str= IF
	    I keys sec[]@ drop >storekey !  LEAVE  THEN  LOOP  2drop ;

: choose-key ( -- o )
    0 BEGIN  drop
	." Choose key by number:" cr .secret-nicks
	BEGIN  key dup bl < WHILE  drop  REPEAT \ swallow control keys
	['] digit? #36 base-execute 0= IF  drop 0
	ELSE  nick#>key# secret-key  THEN
	dup 0= WHILE
	    ." Please enter a base-36 number between 0 and "
	    secret-keys# 1- ['] . #36 base-execute cr  rdrop
    REPEAT
    dup .storekey!  >storekey @ defaultkey !
    ." ==== key " dup ..nick ."  chosen ====" cr ;

\ will ask for your password and if possible auto-select your id

Variable tries#
#10 Value maxtries#

forward read-chatgroups

: get-skc ( -- )
    secret-keys# IF  read-chatgroups  EXIT  THEN  tries# off
    debug-vector @ op-vector !@ >r <default>
    secret-keys#
    BEGIN  dup 0= tries# @ maxtries# u< and  WHILE drop
	    s" Passphrase: " +passphrase   !time
	    read-keys secret-keys# dup 0= IF
		\ fail right after the first try if PASSPHRASE is used
		\ and give the maximum waiting penalty in that case
		1 maxtries# s" PASSPHRASE" getenv d0= select tries# +!
		<err> ." Try# " tries# @ 0 .r '/' emit maxtries# .
		." failed, no key found, waiting "
		#1000 tries# @ lshift dup . ." ms..." ms  <default> cr
		del-last-key
	    THEN
    REPEAT
    dup 0= IF  #-56 throw  THEN
    1 = IF  0 secret-key
	." ==== opened: " dup ..nick ."  in " .time ." ====" cr
    ELSE  ." ==== opened in " .time ." ====" cr choose-key  THEN
    >raw-key ?rsk read-chatgroups  r> op-vector ! ;

scope: n2o
Forward help
}scope

: get-my-key ( -- xt )
    gen-keys-dir  "seckeys.k2o" .keys/ 2dup file-status nip
    0= IF  r/o open-file throw >r r@ file-size throw d0=
	r> close-file throw  ELSE  true  THEN
    IF  [: ." Generate a new keypair:" cr
	  get-nick dup 0= #-56 and throw \ empty nick: pretend to quit
	  new-key .keys ?rsk read-chatgroups ;]
    ELSE  ['] get-skc  THEN ;

: .keyinfo ( -- )
    <warn> ." ==== No key opened ====" cr
    <info> ." generate a new one with 'keygen'" cr <default> ;

: get-me ( -- )
    get-my-key catch dup #-56 = IF drop .keyinfo ELSE throw THEN ;

: ?get-me ( -- )
    \G this version of get-me fails hard if no key is opened
    get-my-key catch
    case
	#-56 of .keyinfo true !!no-key-open!! endof
	#-28 of .keyinfo true !!no-key-open!! endof
	throw  0
    endcase ;

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
    )
End:
[THEN]
