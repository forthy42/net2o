\ key handling

require mkdir.fs

: ?.net2o ( -- )
    s" ~/.net2o" r/o open-file nip IF
	s" ~/.net2o" $1C0 mkdir-parents throw
    THEN ;

: key-in ( dest addr u -- )
    r/o open-file throw { fd }
    keysize fd read-file throw keysize <> !!nokey!!
    fd close-file throw ;

: key-out ( source addr u -- )
    r/w create-file throw { fd }
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
    message state# 8 * 2dup accept dup >r safe/string erase
    r> IF
	source-init wurst-key hash-init
	message roundsh# rounds
	passphrase-diffuse# 0 ?DO  start-diffuse  LOOP \ just to waste time ;-)
	wurst-state passskc keysize xors
	wurst-state keysize + passskc keysize xors
    THEN  passskc ;

: read-keys ( -- )  ?.net2o
    pkc s" ~/.net2o/pubkey.ecc" key-in
    testskc s" ~/.net2o/seckey.ecc" key-in
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

: write-keys ( -- )  ?.net2o
    new-passphrase
    pkc s" ~/.net2o/pubkey.ecc" key-out
    testskc s" ~/.net2o/seckey.ecc" key-out ;

: ?keypair ( -- )
    ['] read-keys catch IF  nothrow gen-keys write-keys  THEN ;