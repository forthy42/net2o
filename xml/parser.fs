\ XML parser

\ Copyright (C) 2018   Bernd Paysan

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

require ../json/parser.fs
require ../html/parser.fs

s" Unknown XML tag" exception Constant xml-throw
s" Unknown XML attributes" exception Constant xml-attrs-throw
s" Unknown XML attribute" exception Constant xml-attr-throw
s" Value is not an XML string" exception Constant xml-string-throw

Variable xml-tag$
Variable xml-element$

object class{ xml
    value: attrs
}class

require blogger-atom.fs

: xml-parse ( -- addr u )
    source >in @ /string 1 >in +! drop c@
    dup ''' = over '"' = or IF  parse  ELSE  xml-string-throw throw  THEN
    ['] html-unescape $tmp save-mem ;

: xml-scan-vals ( -- )
    BEGIN  '=' parse bl skip dup  WHILE
	    key$ $! xml-parse json-string!
    REPEAT  2drop ;

: parse-attrs ( addr-tag u1 addr-attrs u -- )
    2swap 2dup [: type ." -attrs" ;] $tmp find-name ?dup-IF
	also name>int execute
	2dup [: type ." -attrs-class" ;] $tmp find-name ?dup-IF
	    name>int execute new >o r> o-stack >stack
	    2swap ['] xml-scan-vals execute-parsing
	ELSE
	    xml-attrs-throw throw
	THEN
	previous
    ELSE
	xml-attrs-throw throw
    THEN ;

Defer xml-end-tag
$10 stack: end-tags
$10 stack: tags-match

: find-class-tag ( addr u nt -- )
    >r 2dup [: type ." -class" ;] $tmp find-name ?dup-IF
	name>int execute new
	dup r> (int-to)
	>o r> to xml:attrs
    ELSE
	xml-throw throw
    THEN
    find-name ?dup-IF
	    also name>int execute
    ELSE  xml-throw throw  THEN
    [: context @ body> name>string str=
	IF  previous  ELSE  xml-throw throw  THEN ;] is xml-end-tag ;

: find-string-tag ( addr u nt -- ) tags-match >stack 2drop
    [: 2dup tags-match stack> name>string 1- str= IF
	    key$ $! xml-element$ $@ save-mem json-string!
	ELSE  xml-throw throw  THEN ;] is xml-end-tag ;

: find-tag ( addr u -- )
    2dup [: type ." {}" ;] $tmp find-name ?dup-IF
	find-class-tag  EXIT  THEN
    2dup [: type ." $" ;] $tmp find-name ?dup-IF
	find-string-tag  EXIT  THEN
    2dup [: type ." #" ;] $tmp find-name ?dup-IF
	find-string-tag  EXIT  THEN
    2dup [: type ." &" ;] $tmp find-name ?dup-IF
	find-string-tag  EXIT  THEN
    2dup [: type ." %" ;] $tmp find-name ?dup-IF
	find-string-tag  EXIT  THEN
    2dup [: type ." !" ;] $tmp find-name ?dup-IF
	find-string-tag  EXIT  THEN
    xml-throw throw ;

: xml-find-tag ( -- )
    bl $split dup IF
	parse-attrs find-tag
    ELSE
	2drop find-tag
    THEN ;

: xml-start-tag ( addr u -- )
    2dup + 1- c@ dup '?' = swap '/' = or IF
	1- xml-find-tag previous
    ELSE
	xml-find-tag
    THEN ;

: xml-tag ( addr u -- )
    over c@ '/' = IF
	1 /string xml-end-tag
	xml-element$ $free
	end-tags stack> is xml-end-tag
    ELSE
	action-of xml-end-tag end-tags >stack
	xml-start-tag
    THEN ;

false value in-tag?

: parse-end? ( char -- addr u flag )
    parse 2dup + source + = ;

: xml-<tag ( -- )
    '<' parse-end? >r
    ['] html-unescape xml-element$ $exec
    r> IF  #lf xml-element$ c$+!
    ELSE  true to in-tag?  THEN ;

: xml-tag> ( -- )
    '>' parse-end? >r xml-tag$ $+!
    r> 0= IF
	xml-tag$ $@ xml-tag
	xml-tag$ $free  false to in-tag?
    THEN ;

: xml-untags ( -- )
    BEGIN  in-tag? IF  xml-tag>  ELSE  xml-<tag  THEN
    source nip >in @ = UNTIL ;

: xml-untag ( addr u -- )
    ['] xml-untags execute-parsing ;

: atom-file ( addr u -- )
    false to in-tag?
    2dup r/o open-file throw -rot
    [: BEGIN  refill  WHILE  xml-untags  REPEAT ;]
    execute-parsing-named-file ;

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
