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
}scope

$10 stack: o-stack

: scan-vals ( wid -- ) to config-wl
    BEGIN  '=' parse bl skip dup  WHILE  2>r
	    parse-name config-recognizer recognize 2r> eval-config
    REPEAT  2drop ;

scope: html-tags
: b  ." **" 2drop ;
: /b 2drop
    2dup s" <b>" string-prefix? IF  3 /string  ELSE  ." **"  THEN ;
: i  '*' emit 2drop ;
: /i 2drop
    2dup s" <i>" string-prefix? IF  3 /string  ELSE  ." *"  THEN ;
: del ." ~~" 2drop ;
: /del ." ~~" 2drop ;
: u ." __" 2drop ; \ not the common markdown, though
: /u ." __" 2drop ;

object class{ a-params
    field: href$
    field: rel$
    field: class$
    field: target$
    field: jslog$
    field: dir$
    field: oid$
    : dispose ( -- )
    href$ $free
    rel$ $free
    class$ $free
    target$ $free
    jslog$ $free
    dir$ $free
    oid$ $free
    forth:dispose ;
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
: span ( -- )
    a-params-class new >o r> o-stack >stack
    [: ['] a-params >body scan-vals ;] execute-parsing
;
: /span 2drop
    a-params:dispose o-stack stack> >r o> ;
: br 2drop cr ;
}scope

: un-html ( addr u -- )
    dup IF
	over c@ '#' = IF
	    over 1+ c@ 'x' = IF  2 /string $10  ELSE  #10  THEN  >r
	    2dup ['] s>number? r> base-execute
	    IF  drop emit 2drop  EXIT  THEN
	THEN
	2dup ['] html-chars >body find-name-in ?dup-IF
	    name>int execute emit  2drop  EXIT
	THEN  source type cr html-char-throw throw
    ELSE  2drop  THEN ;

: html-unescape ( addr u -- )
    BEGIN  '&' $split dup  WHILE  2swap type
	    ';' $split 2swap un-html
    REPEAT  2drop type ;

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

0 [IF]
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
