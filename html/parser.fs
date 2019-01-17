\ HTML parser for simple HTML

\ Copyright (C) 2016   Bernd Paysan

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

s" Unknown HTML character" exception Constant html-char-throw
s" Unknown HTML tag" exception Constant html-throw

scope: html-chars
'&' constant amp
'<' constant lt
'>' constant gt
'"' constant quot
''' constant apos \ for XML
$A0 constant nbsp
}scope

$10 stack: o-stack

: scan-vals ( wid -- ) to config-wl
    BEGIN  '=' parse bl skip dup  WHILE  2>r
	    parse-name config-recognizer recognize 2r> eval-config
    REPEAT  2drop ;

Variable list-class$
Variable br$
$10 stack: list-stack

scope: html-tags
: b  ." **" 2drop ;
: /b 2drop
    2dup s" <b>" string-prefix? IF  3 /string  ELSE  ." **"  THEN ;
: i  '_' emit 2drop ;
: /i 2drop
    2dup s" <i>" string-prefix? IF  3 /string  ELSE  '_' emit  THEN ;
: del ." ~~" 2drop ;
: /del ." ~~" 2drop ;
: u ." __" 2drop ; \ not the common markdown, though
: /u ." __" 2drop ;

: ol 2drop
    list-class$ @ list-stack >stack
    list-class$ $@ 2dup bl skip nip -
    list-class$ off list-class$ $!
    s"   1. " list-class$ $+! ;
: /ol 2drop cr
    list-class$ $free list-stack stack> list-class$ !
    list-class$ $@len 0= IF  cr  THEN ;
: ul 2drop
    list-class$ @ list-stack >stack
    list-class$ $@ 2dup bl skip nip -
    list-class$ off list-class$ $!
    s"   * " list-class$ $+! ;
: /ul 2drop cr
    list-class$ $free list-stack stack> list-class$ !
    list-class$ $@len 0= IF  cr  THEN ;
: li 2drop
    cr list-class$ $. ;
: /li 2drop ;
: h1 2drop ." # " ;
: /h1 2drop ."  #" cr cr ;
: h2 2drop ." ## " ;
: /h2 2drop ."  ##" cr cr ;
: h3 2drop ." ### " ;
: /h3 2drop ."  ###" cr cr ;

: blockquote 2drop
    br$ $@len 0= IF  "\\\n"  br$ $!  THEN
    "> " br$ $+! br$ $@ 1 /string type ;
: /blockquote 2drop
    br$ $@len 4 u> IF  br$ 2 2 $del  ELSE  br$ $free  THEN  cr cr ;

object class{ a-params
    field: href$
    field: rel$
    field: class$
    field: style$
    field: imageanchor$
    field: target$
    field: jslog$
    field: dir$
    field: oid$
    field: type$
    : dispose ( -- )
    href$ $free
    rel$ $free
    class$ $free
    style$ $free
    imageanchor$ $free
    target$ $free
    jslog$ $free
    dir$ $free
    oid$ $free
    type$ $free
    dispose ;
}class

: a ( -- )
    a-params-class new >o r> o-stack >stack
    [: ['] a-params >body scan-vals ;] execute-parsing
    a-params:class$ $@ s" ot-hashtag" string-prefix? 0= IF
	'[' emit
    THEN ;
: /a 2drop
    a-params:class$ $@ s" ot-hashtag" string-prefix? 0= IF
	." ](" a-params:href$ $@ type ')' emit
    THEN a-params:dispose o-stack stack> >r o> ;

object class{ img-params
    field: src$
    field: alt$
    field: border$
    field: height$
    field: width$
    field: class$
    field: style$
    : dispose ( -- )
    src$ $free
    alt$ $free
    border$ $free
    height$ $free
    width$ $free
    class$ $free
    style$ $free
    dispose ;
}class

: img ( -- )
    a-params-class new >o r> o-stack >stack
    '/' -skip [: ['] img-params >body scan-vals ;] execute-parsing
    ." ![" img-params:alt$ $@ type
    ." ](" img-params:src$ $@ basename type ')' emit
    img-params:src$ $@ basename file-status nip no-file# = IF
	[: ." curl '" img-params:src$ $. ." ' -s -S --output "
	    img-params:src$ $@ basename type ;] $tmp system
    THEN
    img-params:dispose o-stack stack> >r o> ;
: span ( -- )
    a-params-class new >o r> o-stack >stack
    [: ['] a-params >body scan-vals ;] execute-parsing ;
: /span 2drop
    a-params:dispose o-stack stack> >r o> ;
synonym div span
: /div /span cr ;
synonym style span
synonym /style /span

object class{ table-params
    field: align$
    field: cellpadding$
    field: cellspacing$
    field: class$
    field: style$
    : dispose ( -- )
    align$ $free
    cellpadding$ $free
    cellspacing$ $free
    class$ $free
    style$ $free
    dispose ;
}class

: tr ( -- )
    table-params-class new >o r> o-stack >stack
    [: ['] table-params >body scan-vals ;] execute-parsing
;
: table ( -- ) cr tr ;
: /table 2drop cr
    table-params:dispose o-stack stack> >r o> ;
synonym tbody table
in forth : <extra-space ( -- )
    space table-params:style$ $@
    2dup s" center" search nip nip  IF  2drop space  ELSE
	s" right" search nip nip  IF space  THEN  THEN ;
in forth : extra-space> ( -- )
    space table-params:style$ $@
    2dup s" center" search nip nip  IF  2drop space  ELSE
	s" left" search nip nip  IF space  THEN  THEN ;
: th table '|' emit <extra-space ;
: td table '|' emit <extra-space ;
synonym /tbody /table
: /th 2drop extra-space> '|' emit
    table-params:dispose o-stack stack> >r o> ;
: /tr 2drop '|' emit
    table-params:dispose o-stack stack> >r o> ;
: /td 2drop extra-space>
    table-params:dispose o-stack stack> >r o> ;

: br 2drop br$ @ IF  br$ $.  ELSE  cr  THEN ;
}scope

: un-html ( addr u -- )
    dup IF
	over c@ '#' = IF
	    over 1+ c@ 'x' = IF  2 /string $10  ELSE  #10  THEN  >r
	    2dup ['] s>number? r> base-execute
	    IF  drop emit 2drop  EXIT  THEN
	THEN
	2dup ['] html-chars >body find-name-in ?dup-IF
	    name>int execute xemit  2drop  EXIT
	THEN  source type cr html-char-throw throw
    ELSE  2drop  THEN ;

: type-nolf ( addr u -- )
    BEGIN  #lf $split  dup WHILE  2swap dup IF type space ELSE 2drop THEN
    REPEAT
    2drop type ;

: html-unescape ( addr u -- )
    BEGIN  '&' $split dup  WHILE  2swap type-nolf
	    ';' $split 2swap un-html
    REPEAT  2drop type-nolf ;

: html-tag ( addr u -- )
    bl $split 2swap ['] html-tags >body find-name-in ?dup-IF
	name>int execute
    ELSE  html-throw throw  THEN ;

: html-untag ( addr u -- ) config-wl >r
    BEGIN  '<' $split dup  WHILE  2swap html-unescape
	    '>' $split 2swap html-tag
    REPEAT  2drop html-unescape
    r> to config-wl ;

[IFDEF] entries[]
    : .un-htmls
	entries[] $[]# 0 ?DO i entries[] $[] @ .g+:comments:content$
	    2dup type cr
	    ." ================================================================" cr
	    html-untag cr
	    ." ----------------------------------------------------------------" cr
	LOOP ;
[THEN]

\\\
Local Variables:
forth-local-words:
    (
     (("class{") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     (("}class") definition-ender (font-lock-keyword-face . 1))
    )
forth-local-indent-words:
    (
     (("class{") (0 . 2) (0 . 2))
     (("}class") (-2 . 0) (0 . -2))
    )
End:
[THEN]
