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
    state-init >c:key
    passphrase max-passphrase# c:hash
    passphrase-diffuse# 0 ?DO  c:diffuse  LOOP \ just to waste time ;-)
    c:key@ c:key# save-mem ;

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

also net2o-base definitions
100 net2o: newkey ( -- ) ; \ stub
101 net2o: privkey ( addr u -- ) 2drop ; \ stub
102 net2o: pubkey ( addr u -- ) 2drop ; \ stub
103 net2o: keytype ( n -- ) 64drop ;
104 net2o: keynick ( addr u -- )  2drop ;
105 net2o: keyprofile ( addr u -- ) 2drop ;
106 net2o: newkeysig ( addr u -- ) 2drop ;
107 net2o: keymask ( x -- ) 64drop ;
108 net2o: keyfirst ( date -- ) 64drop ;
109 net2o: keylast ( date -- ) 64drop ;

previous definitions

\ revert

previous definitions