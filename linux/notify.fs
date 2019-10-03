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
	    '<' of  ." &lt;"  endof
	    '>' of  ." &gt;"  endof
	    '&' of  ." &amp;" endof
	    emit  0 endcase  LOOP ;

: build-notification ( -- ) ;
: notify@ ( -- addr u )
    config:notify-text# @ IF
	notify-otr? @ config:notify-text# @ 0> and IF
	    "<i>[otr] message</i>"
	ELSE  notify$ $@ ['] escape-<&> $tmp  THEN
    ELSE  "<i>encrypted message</i>"  THEN ;

Variable notify-send
Variable upath

: !upath ( -- )
    "PATH" getenv upath $!
    upath $@ bounds ?DO I c@ ':' = IF 0 I c! THEN LOOP
    "notify-send" upath open-path-file 0= IF
	rot close-file throw
	over c@ '/' <> IF
	    pad $1000 get-dir notify-send $! '/' notify-send c$+!
	THEN
	notify-send $+!
    THEN
    upath $off ;

Variable net2o-logo

: !net2o-logo ( -- )
    s" ../doc/net2o-logo.png" open-fpath-file 0= IF
	rot close-file throw
	over c@ '/' <> IF
	    pad $1000 get-dir net2o-logo $! '/' net2o-logo c$+!
	THEN
	net2o-logo $+!
    THEN ;

!upath !net2o-logo

: 0string ( addr u -- cstr )
    over 0= IF  2drop s" "  THEN
    1+ save-mem over + 1- 0 swap c! ;

0 Value content-string
0 Value title-string

10 cells buffer: notify-args

: !notify-args ( -- )
    here >r notify-args dp !
    "notify-send\0" drop ,
    "-a\0" drop ,
    "net2o\0" drop ,
    "-c\0" drop ,
    "im.received\0" drop ,
    net2o-logo $@len IF
	"-i\0" drop ,
	net2o-logo $@ 0string ,
    THEN
    here to title-string 0 ,
    here to content-string 0 ,
    0 , \ must be terminated by null pointer
    r> dp ! ;

!notify-args

:noname defers 'cold
    notify-send off upath off net2o-logo off
    !upath !net2o-logo !notify-args ; is 'cold

: linux-notification ( -- )  notify-send $@len 0= ?EXIT
    title-string 0 ?free  content-string 0 ?free
    ['] notify-title $tmp dup 0= IF  2drop  EXIT  THEN
    notify@ dup 0= IF  2drop 2drop  EXIT  THEN
    0string content-string !
    0string title-string !
    notify-send $@ notify-args fork+exec ;
