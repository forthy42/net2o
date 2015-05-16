\ hex helpers

: hex[ ]] [: [[ ; immediate
: ]hex ]] ;] $10 base-execute [[ ; immediate
: x~~ ]] hex[ ~~ ]hex [[ ; immediate

: xtype ( addr u -- )  hex[
    bounds ?DO  I c@ 0 <# # # #> type  LOOP  ]hex ;

: (digits>$) ( addr u -- addr' u' ) save-mem
    >r dup dup r> bounds ?DO
	I 2 s>number drop over c! char+ 
    2 +LOOP  over - ;

: hex>$ ( addr u -- addr' u' )
    ['] (digits>$) $10 base-execute ;

: x" ( "hexstring" -- addr u )
    '"' parse hex>$ ;
comp: execute postpone SLiteral ;
