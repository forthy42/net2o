\ base85 output (derived from RFC 1924, suitable as file name)

85 buffer: 85>chars
s" 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$%&()*+-;<=>?@^_`{|}~"
85>chars 85 smove
$80 buffer: chars>85
chars>85 $80 $FF fill
85 0 [DO] [I] dup 85>chars + c@ chars>85 + c! [LOOP]

: .b85 ( n -- n' ) 0 85 um/mod swap 85>chars + c@ emit ;
: .1base85 ( addr -- ) c@ .b85 .b85 drop ;
: .2base85 ( addr -- ) le-uw@ .b85 .b85 .b85 drop ;
: .3base85 ( addr -- ) le-ul@ $FFFFFF and .b85 .b85 .b85 .b85 drop ;
: .4base85 ( addr -- ) le-ul@ .b85 .b85 .b85 .b85 .b85 drop ;
Create .base85s ' drop , ' .1base85 , ' .2base85 , ' .3base85 , ' .4base85 ,
: 85type ( addr u -- )
    bounds ?DO  I I' over - 4 umin cells .base85s + perform  4 +LOOP ;

: b85digit ( char -- n ) $7F umin chars>85 + c@
    dup $FF = !!no-85-digit!! ;

: base85>n ( addr u -- n )  0 1 2swap bounds +DO
	I c@ b85digit over * rot + swap 85 *
    LOOP  drop ;
: (base85>$) ( addr u -- addr' u' )  bounds ?DO
	I I' over - 5 umin dup >r base85>n 0 { w^ x } x le-l! x r> 4 5 */ type
    5 +LOOP ;
: base85>$ ['] (base85>$) $tmp ;

: 85" ( "base85string" -- addr u )
    '"' parse base85>$ ;
comp: execute postpone SLiteral ;

: .85info ( addr u -- )
    <info> 85type <default> ;
: .85warn ( addr u -- )
    <warn> 85type <default> ;

: hash-85 ( addr u -- addr' u' )
    ['] 85type $tmp hash-sanitize ;
: chat-85 ( addr u -- addr' u' )
    ['] 85type $tmp chat-sanitize ;
: hash>filename ( addr u -- filename u' )
    hash-85 [: config:objects$ $. '/' emit type ;] $tmp ;
: .chats/ ( addr u -- addr' u' )
    chat-85 [: config:chats$  $. '/' emit type ;] $tmp ;
