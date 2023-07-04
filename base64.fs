\ base64 output (not the usual base64, suitable as filenames)

: .b64 ( n -- n' ) dup >r 6 rshift r> $3F and
    dup #10 u< IF  '0' + emit  EXIT  THEN  #10 -
    dup #26 u< IF  'A' + emit  EXIT  THEN  #26 -
    dup #26 u< IF  'a' + emit  EXIT  THEN  #26 -
    IF  '_'  ELSE  '-'  THEN  emit ;
: .1base64 ( addr -- )
    c@ .b64 .b64 drop ;
: .2base64 ( addr -- )
    w@ wle .b64 .b64 .b64 drop ;
: .3base64 ( addr -- )
    l@ lle $FFFFFF and .b64 .b64 .b64 .b64 drop ;
Create .base64s ' drop , ' .1base64 , ' .2base64 , ' .3base64 ,
: 64type ( addr u -- )
    bounds ?DO  I I' over - 3 umin cells .base64s + perform  3 +LOOP ;

: b64digit ( char -- n )
    '0' - dup #09 u<= ?EXIT
    [ 'A' '9' - 1- ]L - dup #36 u<= ?EXIT
    dup #40 = IF  drop #63  EXIT  THEN
    [ 'a' 'Z' - 1- ]L - dup #62 u<= ?EXIT
    drop #62 ;
    
: base64>n ( addr u -- n )  #0. 2swap bounds +DO
	I c@ b64digit over lshift rot or swap 6 +
    LOOP  drop ;
: base64>$ ( addr u -- addr' u' ) save-mem >r dup dup r@ bounds ?DO
	I I' over - 4 umin base64>n lle over l! 3 +
    4 +LOOP  drop r> 3 4 */ ;

: 64" ( "base64string" -- addr u )
    '"' parse base64>$ ;
compsem: [compile] 64" postpone SLiteral ;

