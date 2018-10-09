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

require tools.fs
scope: regexps
require regexp.fs
}scope

also regexps
charclass [+-] '+' +char '-' +char
: ?date ( addr u -- flag )
    (( \( \d \d \d \d \) ` - \( \d \d \) ` - \( \d \d \) \s
    \( \d \d \) ` : \( \d \d \) ` : \( \d \d \)
    {{ ` + \( || \( ` - }} \d \d \) `? : \( \d \d \) )) ;
: date>ticks ( -- ticks )
    \1 s>number drop \2 s>number drop \3 s>number drop ymd2day unix-day0 -
    #24 *
    \4 s>number drop + #60 * \5 s>number drop +
    \7 s>number drop   #60 * \8 s>number drop over 0< IF - ELSE + THEN -
    #60 * \6 s>number drop +
    #1000000000 um* d>64 ;
previous

$Variable key$ \ key string
256 cells buffer: json-tokens
4 stack: json-recognizer

s" JSON error" exception Value json-throw
s" JSON date error" exception Value json-date-throw

: .json-err
    ." can't parse json line " sourceline# 0 .r ." : '" source type ." '" cr
    json-throw throw ;

object class
    scope: g+
    cs-scope: comment
    $value: url$
    synonym postUrl$ url$ \ comment has postUrl$ instead of url$
    64value: creationTime!
    64value: updateTime!
    value: author{}
    value: media{}
    value: link{}
    $value: content$
    $value: resourceName$
    field: comments[]
    field: plusOnes[]
    value: postAcl{} \ only for message, not for comment
    }scope
    }scope
end-class g+-comment

object class
    scope{ g+
    cs-scope: author
    $value: displayName$
    $value: profilePageUrl$
    $value: avatarImageUrl$
    $value: resourceName$
    }scope
    }scope
end-class g+-author

object class
    scope{ g+
    cs-scope: link
    $value: title$
    $value: url$
    $value: imageUrl$
    }scope
    }scope
end-class g+-link

$10 stack: element-stack

: set-val ( addr u -- )
    \ ... key$ $@ 2dup type
    find-name dup 0=
    IF  order cr json-throw throw  THEN
    (int-to) ;
: begin-element ( -- )
    \ '"' emit key$ $. .\" \": {" cr
    key$ $@ ['] g+ >body find-name-in
    ?dup-IF  >body >r
	[: ." g+-" key$ $. ;] $tmp ['] forth >body find-name-in
	?dup-IF
	    execute new
	    dup s" {}" key$ $+! set-val
	    >o r> element-stack >stack
	    get-order r> swap 1+ set-order
	ELSE
	    rdrop
	THEN
    THEN
;
: end-element ( -- )
    previous element-stack stack> >o rdrop ;
: begin-array ( -- )
    key$ $. ." []:" ." ["
    s" item" key$ $!
;
: end-array ( -- )
    ." ]" cr
;
: next-element ( -- )
    \ ." ," cr
;
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
    2dup 1- + c@ cells json-tokens + @ 0<> + ;
: rec-num' ( addr u -- ... )
    ?json-token rec-num ;
: rec-float' ( addr u -- ... )
    ?json-token rec-float ;

' rec-string ' rec-num' ' rec-float' ' rec-json 4 json-recognizer set-stack

: json-load ( addr u -- o )
    g+-comment new >o
    get-order n>r ['] g+:comment >body 1 set-order
    forth-recognizer >r  json-recognizer to forth-recognizer
    ['] included catch
    r> to forth-recognizer nr> set-order
    throw o o> ;
