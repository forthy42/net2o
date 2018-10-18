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

also regexps
Charclass [blT] bl +char 'T' +char
: iso-?date ( addr u -- flag )
    (( \( \d \d \d \d \) ` - \( \d \d \) ` - \( \d \d \) [blT] c?
    \( \d \d \) ` : \( \d \d \) ` : \( \d \d \)
    {{ ` . \( {++ \d \d \d ++} \) || \( \) }}
    {{ ` Z \( \) \( \) ||
       {{ ` + \( || \( ` - }} \d \d `? : \d \d \)
    }} )) ;
: iso-date>ticks ( -- ticks )
    \1 s>number drop \2 s>number drop \3 s>number drop ymd2day unix-day0 -
    #24 *
    \4 s>number drop + #60 * \5 s>number drop +
    \8 2 umin s>number drop   #60 *
    \8 dup 2 - /string s>unumber? 2drop over 0< IF - ELSE + THEN -
    #60 * \6 s>number drop +
    #1000000000 um*
    \7 s>unumber? 2drop
    case \7 nip
	3 of  #1000000 um*  endof
	6 of  #1000    um*  endof
	0 swap
    endcase  d+
    d>64 ;
previous

Defer ?date
Defer date>ticks

: iso-date
    ['] iso-?date is ?date
    ['] iso-date>ticks is date>ticks ;

$Variable key$ \ key string
256 cells buffer: json-tokens
5 stack: jsons-recognizer
1 stack: json-recognizer

' noop ' lit, dup rectype: rectype-bool
' noop ' lit, dup rectype: rectype-nil

s" JSON error" exception Value json-throw
s" JSON key not found" exception Value json-key-throw
s" JSON class not found" exception Value json-class-throw

: json-err  cr order json-throw throw ;

0 Value schema-scope
0 Value outer-class
0 Value schema-wid

require g+-schema.fs
require fb-schema.fs
require twitter-schema.fs

$10 stack: element-stack
$10 stack: key-stack
$10 stack: array-stack
0 Value array-item
0 Value last-type
0 Value previous-type

: set-val ( value -- )
    key$ $@ find-name ?dup-IF  (int-to)  EXIT  THEN
    json-err ;

: set-int ( value -- )
    key$ $@ find-name ?dup-IF  (int-to)  EXIT  THEN
    '%' key$ $@ + 1- c!  key$ $@ find-name ?dup-IF
	>r s>f r> (int-to) EXIT  THEN
    '!' key$ $@ + 1- c!  key$ $@ find-name ?dup-IF
	>r #1000000000 um* d>64 r> (int-to) EXIT  THEN
    json-err ;

Defer next-element

: next-element# ( element -- )
    array-item ?dup-IF  >r
	case previous-type
	    rectype-name   of                   endof
	    rectype-num    of        r@ >stack  endof
	    rectype-dnum   of  drop  r@ >stack  endof
	    rectype-string of  s>number? 0= IF json-err THEN
		drop r@ >stack  endof
	    rectype-float  of  f>s   r@ >stack  endof
	    rectype-bool   of        r@ >stack  endof
	    rectype-nil    of        r@ >stack  endof
	endcase  rdrop
    THEN ;

: f>stack ( r stack -- )
    { f^ r } r 1 floats rot $+! ;

: next-element% ( element -- )
    array-item ?dup-IF  >r
	case previous-type
	    rectype-name   of                    endof
	    rectype-float  of        r@ f>stack  endof
	    rectype-string of  over >r >float r> free throw
		0= IF json-err THEN  r@ f>stack  endof
	    rectype-num    of  s>f   r@ f>stack  endof
	    rectype-dnum   of  d>f   r@ f>stack  endof
	    rectype-bool   of  s>f   r@ f>stack  endof
	    rectype-nil    of  s>f   r@ f>stack  endof
	endcase  rdrop
    THEN ;

: next-element$ ( element -- )
    array-item ?dup-IF  >r
	case previous-type
	    rectype-name   of                    endof
	    rectype-string of  over >r $make r> free throw  r@ >stack  endof
	    rectype-num    of  [: 0 .r ;] $tmp $make r@ >stack  endof
	    rectype-dnum   of  [: 0 d.r ;] $tmp $make r@ >stack  endof
	    rectype-float  of  ['] f. $tmp -trailing $make  r@ >stack  endof
	    rectype-bool   of  IF "true" ELSE "false" THEN $make r@ >stack  endof
	    rectype-nil    of  r@ >stack  endof
	endcase  rdrop
    THEN ;

' next-element$ is next-element

: begin-element ( -- )
    \ '"' emit key$ $. .\" \": {" cr
    key$ $@ schema-scope find-name-in
    ?dup-IF  name>int >body >r
	[: key$ $. ." -class" ;] $tmp schema-scope find-name-in
	?dup-IF
	    name>int execute new
	    dup array-item ?dup-IF
		>stack
	    ELSE
		s" {}" key$ $+! set-val
	    THEN
	    >o r> element-stack >stack
	    key$ @ key-stack >stack key$ off
	    get-order r> swap 1+ set-order
	    array-item array-stack >stack 0 to array-item
	ELSE
	    cr key$ $. json-class-throw throw
	THEN
    ELSE
	key$ $@len IF
	    cr key$ $. json-key-throw throw
	THEN
    THEN ;

: end-array ( -- )
    next-element
    array-stack stack> to array-item ;
: end-element ( -- )
    key$ $free  key-stack stack> key$ !
    previous element-stack stack> >o rdrop  end-array ;
: begin-array ( -- )
    array-item array-stack >stack
    [: key$ $. ." []" ;] $tmp find-name ?dup-IF
	name>int execute to array-item
	['] next-element$ is next-element  EXIT  THEN
    [: key$ $. ." []#" ;] $tmp find-name ?dup-IF
	name>int execute to array-item
	['] next-element# is next-element  EXIT  THEN
    [: key$ $. ." []%" ;] $tmp find-name ?dup-IF
	name>int execute to array-item
	['] next-element% is next-element  EXIT  THEN
    json-err ;

: key-find? ( char -- nt )
    key$ $@ + 1- c! key$ $@ find-name ;

: json-string! ( addr u -- )
    over >r
    '$' key$ c$+! key$ $@ find-name ?dup-IF  (int-to) r> free throw  EXIT  THEN
    \ workaround if you mean number but wrote string
    '&' key-find? ?dup-IF
	>r s>number?  IF  r> (int-to) r> free throw  EXIT  THEN  json-err  THEN
    '#' key-find? ?dup-IF
	>r s>number?  IF  drop r> (int-to) r> free throw  EXIT  THEN  json-err  THEN
    '!' key-find? ?dup-IF  drop
	?date IF  date>ticks set-val r> free throw  EXIT  THEN  json-err  THEN
    '%' key-find? ?dup-IF  drop
	>float IF  set-val r> free throw  EXIT  THEN  json-err  THEN
    r> free throw json-err ;

: eval-json ( .. tag -- )
    case
	rectype-name   of  name?int execute       endof
	rectype-string of  json-string!           endof
	rectype-num    of  '#' key$ c$+! set-int  endof
	rectype-dnum   of  '&' key$ c$+! set-val  endof
	rectype-float  of  '%' key$ c$+! set-val  endof
	rectype-bool   of  '?' key$ c$+! set-val  endof
	rectype-nil    of  drop                   endof \ default is null, anyhow
	json-err
    endcase ;

: key-value ( addr u -- ) over >r key$ $! r> free throw
    parse-name jsons-recognizer recognize eval-json ;

' begin-element '{' cells json-tokens + !
' end-element   '}' cells json-tokens + !
' next-element  ',' cells json-tokens + !
' begin-array   '[' cells json-tokens + !
' end-array     ']' cells json-tokens + !
' key-value     ':' cells json-tokens + !

: rec-json ( addr u -- )
    1 = IF
	c@ cells json-tokens + @
	dup IF  rectype-name  EXIT  THEN
    THEN
    drop rectype-null ;

256 buffer: stop-chars
bl 1+ 0 [do] 1 stop-chars [i] + c! [loop]
"{}[],:\"" bounds [do] 1 stop-chars [i] c@ + c! [loop]

: parse-json ( -- addr u )
    source >in @ safe/string
    dup 0 U+DO  over c@ bl u> ?LEAVE  1 safe/string  LOOP
    dup 1 U+DO  over I + c@ stop-chars + c@  IF  drop I  LEAVE  THEN  LOOP
    2dup + source drop - >in ! 2dup input-lexeme! ;

cs-scope: bools
false rectype-bool 2constant false
true  rectype-bool 2constant true
0     rectype-nil  2constant null
}scope

: rec-bool ( addr u -- ... )
    ['] bools >body find-name-in ?dup-IF
	name>int execute
    ELSE  rectype-null  THEN ;

' rec-bool ' rec-num ' rec-float ' rec-string ' rec-json
5 jsons-recognizer set-stack

: rec-jsons ( addr u -- ... json-type )
    last-type to previous-type
    jsons-recognizer recognize dup to last-type ;

' rec-jsons 1 json-recognizer set-stack

: json-load ( addr u -- o )
    outer-class new >o
    o element-stack >stack  0 key-stack >stack  0 array-stack >stack
    get-order n>r schema-wid 1 set-order
    forth-recognizer >r  json-recognizer to forth-recognizer
    action-of parse-name >r ['] parse-json is parse-name
    ['] included catch
    r> is parse-name  r> to forth-recognizer  nr> set-order
    throw o o> ;

$Variable entries[]

: json-load-dir ( addr u -- )
    2dup open-dir throw { dd } fpath dup $@len >r also-path dd
    [: { dd } !time
	BEGIN
	    pad $100 dd read-dir throw  WHILE  pad swap
		2dup "*.json" filename-match IF
		    json-load entries[] >stack
		ELSE  2drop  THEN
	REPEAT  drop
	[: ." read " entries[] $[]# . ." postings in " .time ;]
	success-color color-execute cr ;] catch
    r> fpath $!len  dd close-dir throw  throw ;
