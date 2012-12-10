\ key handling

require mkdir.fs

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

: ?.net2o ( -- )
    s" ~/.net2o" r/o open-file nip IF
	s" ~/.net2o" $1C0 mkdir-parents throw
    THEN ;

: keys-in ( pkc skc addr u -- )
    r/o open-file throw { fd } swap
    keysize fd read-file throw keysize <> !!nokey!!
    keysize fd read-file throw keysize <> !!nokey!!
    fd close-file throw ;

: keys-out ( pkc skc addr u -- )
    r/w create-file throw { fd } swap
    keysize fd write-file throw
    keysize fd write-file throw
    fd close-file throw ;

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
    passskc keysize move   wurst-source !key
    message state# 8 * 2dup accept* dup >r safe/string erase
    r> IF
	source-init wurst-key hash-init
	message roundsh# rounds
	passphrase-diffuse# 0 ?DO  start-diffuse  LOOP \ just to waste time ;-)
	wurst-state passskc keysize xors
	wurst-state keysize + passskc keysize xors
    THEN  passskc ;

Variable keyfile

: >key-name ( addr u -- )
    s" ~/.net2o/" keyfile $! 
    keyfile $+! s" .ecc" keyfile $+! ;

: key-name ( -- )  keyfile @ ?EXIT
    ." ID name: " pad 100 accept pad swap >key-name ;

: read-keys ( -- )  ?.net2o key-name
    pkc testskc keyfile $@ keys-in
    testskc check-key? ?EXIT
    passphrase-retry# 0 ?DO
	cr ." Passphrase: "
	testskc get-passphrase check-key? IF  unloop  EXIT  THEN
    LOOP  !!nokey!! ;

: new-passphrase ( -- )
    passphrase-retry# 0 ?DO
	cr ." Enter Passphrase: "       skc get-passphrase
	testskc keysize move
	cr ." Reenter Passphrase: "     skc get-passphrase
	testskc keysize tuck str= IF  unloop  EXIT  THEN
    LOOP  !!nokey!! ;

: write-keys ( -- )  ?.net2o key-name
    new-passphrase
    pkc testskc keyfile $@ keys-out ;

: ?keypair ( -- )
    ['] read-keys catch IF  nothrow gen-keys write-keys  THEN ;

