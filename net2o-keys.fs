\ key handling

require mkdir.fs

\ hashed key data base

begin-structure key-entry
field: ke-sk
field: ke-nick
field: ke-name
field: ke-sigs
64field: ke-created
64field: ke-expires
end-structure

key-entry buffer: sample-key

Variable key-table
Variable this-key
Variable this-keyid
sample-key this-key ! \ dummy

: current-key ( addr u -- )
    key-table #@ drop this-key ! ;
: make-thiskey ( addr -- )
    dup $@ drop this-keyid !  cell+ $@ drop this-key ! ;

: new-key ( addr u -- )
    \ addr u is the public key
    sample-key key-entry 2dup erase
    2over key-table #! current-key ;

: (digits>$) ( addr u -- addr' u' ) save-mem
    >r dup dup r> bounds ?DO
	I 2 s>number drop over c! char+ 
    2 +LOOP  over - ;

: hex>$ ( addr u -- addr' u' )
    ['] (digits>$) $10 base-execute ;

: x" ( "hexstring" -- addr u )
    '"' parse hex>$ ;
compile> execute postpone SLiteral ;

Vocabulary key-parser

: ^key ( -- fstart )  this-key @ ;

also key-parser definitions

: id: ( "id" -- ) 0 parse hex>$ new-key ;
: sk: ( "sk" -- ) 0 parse hex>$ ^key ke-sk $! ;
: nick: ( "sk" -- ) 0 parse ^key ke-nick $! ;
: name: ( "sk" -- ) 0 parse ^key ke-name $! ;
: created: ( "number" -- )  parse-name s>number d>64 ^key ke-created 64! ;
: expires: ( "number" -- )  parse-name s>number d>64 ^key ke-expires 64! ;

previous definitions

: .key ( addr -- )  dup @ 0= IF  drop  EXIT  THEN
    ." id: "   dup $@ xtype cr cell+ $@ drop >r
    r@ ke-sk   @ IF  ." sk: "   r@ ke-sk $@ xtype cr  THEN
    r@ ke-nick @ IF  ." nick: " r@ ke-nick $@ type cr  THEN
    r@ ke-name @ IF  ." name: " r@ ke-name $@ type cr  THEN
    r@ ke-created 64@ 64dup 64-0= IF  64drop
    ELSE  ." created: " 64>d d. cr  THEN
    r@ ke-expires 64@ 64dup 64-0= IF  64drop
    ELSE  ." expires: " 64>d d. cr  THEN
    rdrop cr ;

: .skey ( addr -- )  dup ke-sk @    IF  .key  ELSE  drop  THEN ;
: .pkey ( addr -- )  dup ke-sk @ 0= IF  .key  ELSE  drop  THEN ;

: dump-skeys ( fd -- )
    [: key-table ['] .skey #map ;] swap outfile-execute ;
: dump-pkeys ( fd -- )
    [: key-table ['] .pkey #map ;] swap outfile-execute ;

: ?.net2o ( -- )
    s" ~/.net2o" r/o open-file nip IF
	s" ~/.net2o" $1C0 mkdir-parents throw
    THEN ;

: dump-keys ( -- )  ?.net2o
    s" ~/.net2o/seckeys.n2o" r/w open-file throw
    dup >r dump-skeys r> close-file throw 
    s" ~/.net2o/pubkeys.n2o" r/w open-file throw
    dup >r dump-pkeys r> close-file throw ;

: n>r ( x1 .. xn n -- r:xn..x1 r:n )
    r> { n ret }
    0  BEGIN  dup n <  WHILE  swap >r 1+  REPEAT  >r
    ret >r ;
: nr> ( r:xn..x1 r:n -- x1 .. xn n )
    r> r> { ret n }
    0  BEGIN  dup n <  WHILE  r> swap 1+  REPEAT
    ret >r ;

: scan-keys ( fd -- )  get-order n>r
    only previous  key-parser  include-file  nr> set-order ;

: ?scan-keys ( addr u -- )
    r/w open-file 0= IF scan-keys ELSE drop THEN ;

: read-keys ( -- )
    s" default.n2o" ?scan-keys
    s" ~/.net2o/seckeys.n2o" ?scan-keys
    s" ~/.net2o/pubkeys.n2o" ?scan-keys ;

\ search for keys by name and nick
\ !!FIXME!! not optimized

: nick-key ( addr u -- ) \ search for key nickname and make current
    key-table 
    [: dup >r cell+ $@ drop ke-nick $@ 2over str= IF
	r@ make-thiskey
    THEN  rdrop ;] #map ;

: name-key ( addr u -- ) \ search for key name and make current
    key-table 
    [: dup >r cell+ $@ drop ke-name $@ 2over str= IF
	r@ make-thiskey
    THEN  rdrop ;] #map ;

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
		-rot xc!+? 0= IF  bell  ELSE  '*' emit  THEN
	    THEN
    REPEAT  drop  nip r> swap - ;

keysize buffer: testkey
keysize buffer: testskc
keysize buffer: passskc

: check-key? ( addr -- flag )  >r
    testkey r@ base9 crypto_scalarmult
    testkey keysize pkc over str= IF  r@ skc keysize move  true
    ELSE  false  THEN  rdrop ;

3 Value passphrase-retry#
$100 Value passphrase-diffuse#

: get-passphrase ( addrin -- addrout )
    passskc keysize move   wurst-source rounds-setkey
    message state# 8 * 2dup accept* dup >r safe/string erase
    r> IF
	source-init wurst-key hash-init
	message roundsh# rounds-encrypt
	passphrase-diffuse# 0 ?DO  start-diffuse  LOOP \ just to waste time ;-)
	wurst-state passskc keysize xors
	wurst-state keysize + passskc keysize xors
    THEN  passskc ;

: new-passphrase ( -- )
    passphrase-retry# 0 ?DO
	cr ." Enter Passphrase: "       skc get-passphrase
	testskc keysize move
	cr ." Reenter Passphrase: "     skc get-passphrase
	testskc keysize tuck str= IF  unloop  EXIT  THEN
    LOOP  !!nokey!! ;

: decrypt-skc  ( -- )
    testskc check-key? ?EXIT
    passphrase-retry# 0 ?DO
       cr ." Passphrase: "
       testskc get-passphrase check-key? IF  unloop  EXIT  THEN
    LOOP  !!nokey!! ;

: >key ( addr u -- )
    key-table @ 0= IF  read-keys  THEN
    nick-key
    this-keyid @ pkc keysize move
    ^key ke-sk $@ testskc swap move  decrypt-skc ;
