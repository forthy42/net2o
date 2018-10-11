\ JSON parser to import Google+

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

require ../tools.fs
scope: regexps
require regexp.fs
}scope

$Variable key$ \ key string
256 cells buffer: json-tokens
4 stack: json-recognizer

s" JSON error" exception Value json-throw
s" JSON date error" exception Value json-date-throw

: .json-err
    ." can't parse json line " sourceline# 0 .r ." : '" source type ." '" cr
    json-throw throw ;

require g+-schema.fs

$10 stack: element-stack
$10 stack: key-stack
$10 stack: array-stack
Variable array-item
' g+ >body Value schema-scope

: set-val ( addr u -- )
    key$ $@ find-name dup 0=
    IF  order cr json-throw throw  THEN
    (int-to) ;
: begin-element ( -- )
    \ '"' emit key$ $. .\" \": {" cr
    key$ $@ schema-scope find-name-in
    ?dup-IF  name>int >body >r
	[: key$ $. ." -class" ;] $tmp schema-scope find-name-in
	?dup-IF
	    name>int execute new
	    dup array-item @ ?dup-IF
		>stack
	    ELSE
		s" {}" key$ $+! set-val
	    THEN
	    >o r> element-stack >stack
	    key$ @ key-stack >stack key$ off
	    get-order r> swap 1+ set-order
	    array-item @ array-stack >stack array-item off
	ELSE
	    cr key$ $. ."  class not found" cr ~~ rdrop json-throw throw
	THEN
    ELSE
	key$ $@len IF
	    cr key$ $. ."  key not found" cr ~~ json-throw throw
	THEN
    THEN ;

: end-array ( -- )
    array-stack stack> array-item ! ;
: end-element ( -- )
    key$ $free  key-stack stack> key$ !
    previous element-stack stack> >o rdrop  end-array ;
: begin-array ( -- )
    [: key$ $. ." []" ;] $tmp find-name dup IF
	array-item @ array-stack >stack
	name>int execute array-item !
    ELSE
	cr order
	json-throw throw
    THEN ;

synonym next-element noop ( -- )

' noop ' lit, dup rectype: rectype-bool

: eval-json ( .. tag -- )
    case
	rectype-name   of  name?int execute       endof
	rectype-string of
	    '!' key$ c$+! key$ $@ find-name IF
		?date IF  date>ticks set-val
		ELSE  json-date-throw throw  THEN
	    ELSE
		'$' key$ $@ + 1- c! set-val
	    THEN  endof
	rectype-num    of  '#' key$ c$+! set-val  endof
	rectype-dnum   of  '&' key$ c$+! set-val  endof
	rectype-float  of  '%' key$ c$+! set-val  endof
	rectype-bool   of  '?' key$ c$+! set-val  endof
	.json-err
    endcase ;

: key-value ( addr u -- ) key$ $!
    parse-name json-recognizer recognize eval-json ;

' begin-element '{' cells json-tokens + !
' end-element   '}' cells json-tokens + !
' next-element  ',' cells json-tokens + !
' begin-array   '[' cells json-tokens + !
' end-array     ']' cells json-tokens + !
' key-value     ':' cells json-tokens + !

: rec-json ( addr u -- )
    1 u>= IF
	dup 1+ >r c@ cells json-tokens + @
	dup IF
	    r> source drop - >in @ drop >in !
	    rectype-name  EXIT  THEN
	rdrop
    THEN
    drop rectype-null ;

: ?json-token ( addr u -- addr u' )
    bounds 2dup U+DO
	I c@ cells json-tokens + @ IF
	    nip I swap
	    I source drop - >in !
	THEN
    LOOP  tuck - ;
: rec-num' ( addr u -- ... )
    ?json-token rec-num ;
: rec-float' ( addr u -- ... )
    ?json-token rec-float ;
: rec-bool ( addr u -- ... )
    2dup s" true" str= IF  2drop true rectype-bool  EXIT  THEN
    s" false" str= IF  false rectype-bool  ELSE  rectype-null  THEN ;

' rec-string ' rec-bool ' rec-num' ' rec-float' ' rec-json 5 json-recognizer set-stack

: json-load ( addr u -- o )
    g+:comments-class new >o
    o element-stack >stack  0 key-stack >stack  0 array-stack >stack
    get-order n>r ['] g+:comments >body 1 set-order
    forth-recognizer >r  json-recognizer to forth-recognizer
    ['] included catch
    r> to forth-recognizer nr> set-order
    throw o o> ;

$Variable entries[]

: json-read-loop ( -- )
    BEGIN  refill  WHILE
	    source json-load entries[] >stack
    REPEAT ;

: json-loads ( addr u -- )
    r/o open-file throw ['] json-read-loop execute-parsing-file
    ." read " entries[] $[]# . ." in " .time cr ;
