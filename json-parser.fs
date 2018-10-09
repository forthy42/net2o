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

Variable key$
: set-string ( addr u -- )
    2drop
;
: set-num ( n -- )
    drop
;
: set-dnum ( d -- )
    2drop
;
: set-float ( r -- )
    fdrop
;
: begin-element ( -- )
;
: end-element ( -- )
;
: begin-array ( -- )
;
: end-array ( -- )
;

4 stack: json-recognizer

' rec-string ' rec-num ' rec-float ' rec-word 4 json-recognizer set-stack

s" JSON error" exception Value json-throw

: .json-err
    ." can't parse json line " sourceline# 0 .r ." : '" source type ." '" cr
    json-throw throw ;

: eval-json ( .. tag -- )
    case
	rectype-name   of  name?int execute endof
	rectype-string of  '$' key$ c$+! set-string  endof
	rectype-num    of  '#' key$ c$+! set-num     endof
	rectype-dnum   of  '&' key$ c$+! set-dnum    endof
	rectype-float  of  '%' key$ c$+! set-float   endof
	.json-err
    endcase ;

scope: json
: } end-element ;
synonym }, }
: {  "{}" key$ $+! begin-element ;
: [{ "[]" key$ $+! begin-array ;
: }] end-array ;
synonym }], }]
: : key$ $! source + 1- c@ ',' = #tib +!
    parse-name json-recognizer recognize eval-json ;
}scope
