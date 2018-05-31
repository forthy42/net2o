\ net2o template for new files

\ Copyright (C) 2015   Bernd Paysan

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

\ notifications (on android only now)

64#0 64Value last-notify
64#0 64Value latest-notify

scope{ config
2Variable delta-notify& #60.000.000.000 delta-notify& 2! \ one notification per minute is enough
Variable notify-mode# 3 notify-mode# !
Variable notify-rgb# $FFFF00 notify-rgb# !
Variable notify-on# 500 notify-on# !
Variable notify-off# 4500 notify-off# !
Variable notify-text# 1 notify-text# !
Variable notify?# -2 notify?# ! \ default: no notification when active
}scope

: tick-notify? ( -- flag )
    ticks last-notify 64- config:delta-notify& 2@ d>64 64< ;

Variable notify$
Variable notify-nick$
Variable notify-otr?
Variable pending-notifications

: notify- ( -- )
    pending-notifications off
    latest-notify to last-notify ;

Sema notify-sema
: notify; nip (;]) ]] notify-sema c-section ; [[ ;
: notify> comp-[: ['] notify; colon-sys-xt-offset stick ; immediate
: notify+ ( addr u -- )  notify> notify$ $+! ;
: notify-nick+ ( addr u -- )  notify> s"  @" notify-nick$ $+! notify-nick$ $+! ;
: notify-nick! ( addr u -- )  notify> s"  @" notify-nick$ $!
    notify-nick$ $+! notify$ $free ;
: notify! ( addr u -- )  notify> notify$ $! ;

: notify-title ( -- )
    ." net2o: " pending-notifications @ dup .
    ." Message" 1 > IF ." s"  THEN
    ."  from" notify-nick$ $. ;

[IFDEF] android
    require android/notify.fs
[ELSE]
    [IFDEF] linux
	require linux/notify.fs
	[IFUNDEF] rendering
	    [IFUNDEF] mslinux
		require minos2/gl-helper.fs
	    [ELSE]
		: ftime ( -- r ) ntime d>f 1e-9 f* ;
	    [THEN]
	[THEN]
    [THEN]
    
    : show-notification ( -- )
	1 pending-notifications +!
	[IFDEF] linux-notification
	    linux-notification
	[THEN] ;
    : msg-builder ;
    [IFUNDEF] rendering
	Variable rendering
    [THEN]  -2 rendering !
    also also
[THEN]

: msg-notify ( -- ) notify>
    ticks to latest-notify
    rendering @ config:notify?# @ <= dup IF
	notify-
    THEN
    tick-notify? or 0= IF
	[IFDEF] build-notification
	    build-notification show-notification
	[THEN]
	latest-notify to last-notify
    THEN  notify$ $off ;

previous previous

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]
