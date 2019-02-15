\ net2o GUI, markdown viewer

\ Copyright (C) 2019   Bernd Paysan

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

\ Inspiration: wf.fs, a markdown-like parser, which converts to HTML

Defer .char

Variable md-text$
Variable last-cchar
Variable last-emph-flags
Variable emph-flags \ emphasis flags
Variable up-emph
Variable count-emph
Variable us-state

0 Value p-box \ paragraph box
0 Value v-box \ vertical box

1 7 bits: italic underline 2underline sitalic bold mono strikethrough

: +emphs ( flags -- )
    \regular \sans
    dup [ underline 2underline or ]L and 2/  us-state !
    dup strikethrough and 4 rshift us-state +!
    dup mono and IF  \mono  THEN
    [ italic sitalic bold or or ]L and
    dup 1 and swap 3 rshift xor
    case
	1 of  \italic       endof
	2 of  \bold         endof
	3 of  \bold-italic  endof
    endcase ;

: .md-text ( -- )
    us-state @ md-text$ $@ }}text-us p-box .+childs md-text$ $free ;

: default-char ( char -- )
    emph-flags @ last-emph-flags @ over last-emph-flags ! <> IF
	.md-text emph-flags @ +emphs
    THEN
    md-text$ c$+!  last-cchar off ;

' default-char is .char

Create do-char $100 0 [DO] ' .char , [LOOP]

: md-char ( xt "char" -- )
    char cells do-char + ! ;
: md-char: ( "char" -- )
    depth >r :noname depth r> - 1- roll md-char ;

: ?count-emph ( flag char -- )
    last-cchar @ over last-cchar ! <> IF  count-emph off
	emph-flags @ and 0= up-emph !
    ELSE  1 count-emph +!  drop  THEN ;

md-char: * ( char -- )
    [ sitalic bold or ]L swap ?count-emph
    sitalic up-emph @ 0= IF  negate  THEN  emph-flags +! ;
md-char: _ ( char -- )
    [ italic underline 2underline or or ]L swap ?count-emph
    italic up-emph @ 0= IF  negate  THEN  emph-flags +! ;
md-char: ` ( char -- )
    mono swap ?count-emph
    count-emph @ 2 = IF
	mono up-emph @ 0= IF  negate  THEN  emph-flags +!
    THEN ;
md-char: ~ ( char -- )
    strikethrough swap ?count-emph
    count-emph @ 1 = IF
	strikethrough up-emph @ 0= IF  negate  THEN  emph-flags +!
    THEN ;
md-char: \ ( char -- )
    drop source >in @ /string IF  c@ .char  1 >in +!
    ELSE  drop ( add line break )  THEN ;

: render-line ( addr u -- )
    \G render a line
    0 +emphs
    bounds U+DO
	I c@ dup cells do-char + perform
    LOOP  .md-text ;

: >indent ( n -- )
    >in @ + 2/ 4 * spaces ;

scope: markdown
\ headlines limited to h1..h3
: # ( -- )
    source 2dup + 2 - 2 " #" str= -2 and +
    \huge \bold \sans render-line \normal \regular ;
: ## ( -- )
    source 2dup + 3 - 3 " ##" str= -3 and +
    \large \bold \sans render-line \normal \regular ;
: ### ( -- )
    source 2dup + 4 - 4 " ###" str= -4 and +
    \normal \bold \sans render-line \normal \regular ;
: 1. ( -- )
    \ render counted line
    -3 >indent
;
synonym 2. 1.
synonym 3. 1.
synonym 4. 1.
synonym 5. 1.
synonym 6. 1.
synonym 7. 1.
synonym 8. 1.
synonym 9. 1.
: * ( -- )
    -2 >indent \ "•‒⋆‧"
;
synonym + *
synonym - *
synonym ± *

}scope

: +p-box ( -- )
    {{ }}p dup v-box .+childs .subbox to p-box ;

: markdown-loop ( -- )
    BEGIN  refill  WHILE
	    source nip 0= IF
		p-box ?dup-IF  .subbox .par-init  THEN
		+p-box
	    ELSE
		parse-name ['] markdown >body find-name-in ?dup-IF
		    name?int execute
		ELSE  >in off  source render-line  THEN
	    THEN
    REPEAT ;

: markdown-parse ( addr u -- )
    {{ }}v to v-box +p-box
    ['] markdown-loop execute-parsing-named-file ;

\\\
Local Variables:
forth-local-words:
    (
     (("md-char:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
    )
forth-local-indent-words:
    (
     (("md-char:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]
