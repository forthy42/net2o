\ Linux specific notification stuff

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

: escape-<&> ( addr u -- )
    bounds ?DO  case i c@
	    '<' of  ." &lt;"  endof
	    '>' of  ." &gt;"  endof
	    '&' of  ." &amp;" endof
	    emit  0 endcase  LOOP ;

: notify+ notify> notify$ $+! ;
: notify! notify> notify$ $! ;
: build-notification ( -- ) ;
: notify@ ( -- addr u )
    notify-text IF  notify$ $@ ['] escape-<&> $tmp
    ELSE  "<i>hidden cryptic text</i>"  THEN ;

Variable notify-send
Variable upath
"PATH" getenv upath $!
upath $@ bounds [?DO] [I] c@ ':' = [IF] 0 [I] c! [THEN] [LOOP]
"notify-send" upath open-path-file 0= [IF]
    rot close-file throw
    over c@ '/' <> [IF]
	pad $1000 get-dir notify-send $! '/' notify-send c$+!
    [THEN]
    notify-send $+!
[THEN]
upath $off

Variable net2o-logo
s" doc/net2o-logo.png" open-fpath-file 0= [IF]
    rot close-file throw
    over c@ '/' <> [IF]
	pad $1000 get-dir net2o-logo $! '/' net2o-logo c$+!
    [THEN]
    net2o-logo $+!
[THEN]

: 0string ( addr u -- cstr )
    1+ save-mem over + 1- 0 swap c! ;

Create notify-args
"notify-send\0" drop ,
"-a\0" drop ,
"net2o\0" drop ,
"-c\0" drop ,
"im.received\0" drop ,
net2o-logo $@len [IF]
    "-i\0" drop ,
    net2o-logo $@ 0string ,
[THEN]
here 0 ,
here 0 ,
0 , \ must be terminated by null pointer
Constant content-string
Constant title-string

: linux-notification ( -- )  notify-send $@len 0= ?EXIT
    title-string 0 ?free  content-string 0 ?free
    ['] notify-title $tmp 0string title-string !
    notify@ 0string content-string !
    notify-send $@ notify-args fork+exec ;