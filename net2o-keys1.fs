\ net2o key storage

require mkdir.fs

Vocabulary new-keys

also new-keys definitions

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

\ get passphrase

3 Value passphrase-retry#
$100 Value passphrase-diffuse#
$100 Constant max-passphrase# \ 256 characters should be enough...
max-passphrase# buffer: passphrase

: get-passphrase ( -- addr u )
    passphrase max-passphrase# 2dup accept* safe/string erase
    wurst-key >c:key
    passphrase max-passphrase# c:hash
    passphrase-diffuse# 0 ?DO  c:diffuse  LOOP \ just to waste time ;-)
    c:key@ $40 save-mem ;

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

2Variable key+len \ current key + len

also net2o-base definitions
100 net2o: newkey ( -- ) ; \ stub
101 net2o: privkey ( addr u -- ) 2drop ; \ stub
102 net2o: pubkey ( addr u -- ) 2drop ; \ stub
103 net2o: keytype ( n -- ) 64drop ;
104 net2o: keynick ( addr u -- )  2drop ;
105 net2o: keyprofile ( addr u -- ) 2drop ;
106 net2o: newkeysig ( addr u -- ) 2drop ;
107 net2o: keymask ( x -- ) 64drop ;
108 net2o: keyfirst ( date-ns -- ) 64drop ;
109 net2o: keylast ( date-ns -- ) 64drop ;

previous definitions

: key:code ( -- )
    net2o-code0 keypack keypack-all# erase
    keypack mykey-salt# + cmd0source ! ;

also net2o-base definitions

: end:key ( -- )
    end-cmd previous
    keypack keypack-all#
    key+len 2@ encrypt$
    cmdlock unlock ;

previous definitions

Variable keys "" keys $!

: +key ( addr u -- ) key+len 2! key+len 2 cells keys $+! ;
: +passphrase ( -- )  get-passphrase +key ;

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

: +keypair ( nick u -- )
    +passphrase gen-keys ticks 64>r
    key:code [ also net2o-base ]
    newkey skc keysize $, privkey 2dup $, keynick 64r@ lit, keyfirst
    end:key [ previous ]
    keypack keypack-all# ?key-fd append-file
    keypad skc pkc crypto_scalarmult keypad keysize +key
    key:code [ also net2o-base ]
    newkey pkc keysize $, pubkey $, keynick 64r> lit, keyfirst
    end:key [ previous ]
    keypack keypack-all# ?key-fd append-file ;

\ read key file

: try-decrypt ( -- addr u / 0 0 )
    keys $@ bounds ?DO
	keypack keypack-d keypack-all# move
	keypack-d keypack-all# I 2@
	decrypt$ IF  unloop  EXIT  THEN
	2drop
    2 cells +LOOP  0 0 ;

: do-key ( addr u / 0 0  -- )
    dup 0= IF  2drop  EXIT  THEN
    n2o:see ;

: read-keys ( -- addr u / 0 0 )
    0. ?key-fd reposition-file throw
    BEGIN
	keypack keypack-all# ?key-fd read-file throw
	keypack-all# = WHILE  try-decrypt do-key
    REPEAT ;

\ revert

previous definitions