\ Linux specific notification stuff

\ Copyright © 2016   Bernd Paysan

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

: escape-<&> ( addr u -- )
    bounds ?DO  case i c@
	    '<' of  ." &lt;"   endof
	    '>' of  ." &gt;"   endof
	    '&' of  ." &amp;"  endof
	    '"' of  ." &quot;" endof
	    emit  0 endcase  LOOP ;
: escape-<&>-shell ( addr u -- )
    bounds ?DO  case i c@
	    '<' of  ." &lt;"   endof
	    '>' of  ." &gt;"   endof
	    '&' of  ." &amp;"  endof
	    '"' of  ." &quot;" endof
	    '\' of  ." \\"     endof
	    '$' of  ." \$"     endof
	    '!' of  ." \!"     endof
	    emit  0 endcase  LOOP ;
: escape-shell ( addr u -- )
    bounds ?DO  case i c@
	    '\' of  ." \\"     endof
	    '$' of  ." \$"     endof
	    '!' of  ." \!"     endof
	    emit  0 endcase  LOOP ;

: build-notification ( -- ) ;
: notify@ ( -- addr u )
    config:notify-text# @ IF
	notify-otr? @ config:notify-text# @ 0> and IF
	    "<i>[otr] message</i>"
	ELSE  notify$ $@ ['] escape-<&> $tmp  THEN
    ELSE  "<i>encrypted message</i>"  THEN ;

: 0$! ( addr u cstr-addr -- )
    >r 1+ over 0= IF  2drop "\0"  THEN
    save-mem over + 1- 0 swap c! r> ! ;

0 Value content-string
0 Value title-string

10 cells buffer: notify-args

$Variable notify-send
$Variable net2o-logo

: file>abspath ( file u path -- addr u )
    ['] file>path catch IF
	drop 2drop #0.
    ELSE
	over c@ '/' <> IF
	    [: pad $1000 get-dir type '/' emit type ;] $tmp
	    compact-filename
	THEN
    THEN ;

: !upath ( -- ) { | w^ upath }
    "PATH" getenv upath $!
    upath $@ bounds ?DO I c@ ':' = IF 0 I c! THEN LOOP
    "notify-send" upath file>abspath notify-send $!
    upath $free ;

: !net2o-logo ( -- )
    s" ../doc/net2o-logo.png" fpath file>abspath net2o-logo $! ;

: ?free0 ( addr -- )
    dup 0= IF  drop  EXIT  THEN  @ free throw ;
: !notify-args ( -- )
    title-string   ?free0
    content-string ?free0
    here >r notify-args dp !
    "notify-send\0" drop ,
    "-a\0" drop ,
    "net2o\0" drop ,
    "-c\0" drop ,
    "im.received\0" drop ,
    net2o-logo $@len IF
	"-i\0" drop ,
	net2o-logo $@ drop ,
    THEN
    here to title-string 0 ,
    here to content-string 0 ,
    0 , \ must be terminated by null pointer
    r> dp ! ;

!upath !net2o-logo !notify-args

:noname defers 'cold
    !upath !net2o-logo !notify-args ; is 'cold

: linux-notification ( -- )  notify-send $@len 0= ?EXIT
    [IFDEF] use-execve
	notify@ content-string 0$!
	['] notify-title $tmp dup 0= IF  2drop  EXIT  THEN  title-string 0$!
	notify-send $@ notify-args fork+exec
    [ELSE]
	[: notify-send $. space
	    ." -a net2o -c im.received "
	    net2o-logo $@len IF
		." -i " net2o-logo $. space  THEN
	    ['] notify-title $tmp dup 0= IF  2drop  EXIT  THEN
	    '"' emit escape-<&>-shell '"' emit space
	    '"' emit notify@ escape-shell '"' emit ;] $tmp system
    [THEN] ;
