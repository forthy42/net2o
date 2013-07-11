\ net2o key storage

require unix/pthread.fs
require wurstkessel.fs
require wurstkessel-init.fs
require mkdir.fs

wurstkessel-o crypto-o !

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

: get-passphrase ( -- addr u )
    wurst-source rounds-setkey
    message state# 8 * 2dup accept* safe/string erase
    source-init wurst-key hash-init
    message roundsh# rounds-encrypt
    passphrase-diffuse# 0 ?DO  c:diffuse  LOOP \ just to waste time ;-)
    wurst-state state# ;
