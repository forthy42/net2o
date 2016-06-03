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
60.000000000 d>64 64Value delta-notify \ one notification per minute is enough
3 Value notify-mode
$FFFF00 Value notify-rgb
500 Value notify-on
4500 Value notify-off
true Value notify-text

: tick-notify? ( -- flag )
    ticks last-notify 64- delta-notify 64< ;

Variable notify? -2 notify? ! \ default: no notification when active
Variable notify$
Variable pending-notifications

: notify- ( -- )
    pending-notifications off
    64#0 to last-notify ;

Sema notify-sema
: notify; nip (;]) ]] notify-sema c-section ; [[ ;
: notify> comp-[: ['] notify; colon-sys-xt-offset stick ; immediate

: notify-title ( -- )
    ." net2o: " pending-notifications @ dup .
    ." Message" 1 > IF ." s"  THEN ;

[IFDEF] android
    require android/notify.fs
[ELSE]
    [IFDEF] linux
	require linux/notify.fs
    [THEN]
	
    : show-notification ( -- )
	1 pending-notifications +!
	[IFDEF] linux-notification
	    linux-notification
	[THEN] ;
    : msg-builder ;
    [IFUNDEF] rendering
	Variable rendering  -1 rendering !
    [THEN]
    also also
[THEN]

: msg-notify ( -- ) notify>
    ticks to latest-notify
    rendering @ notify? @ <= dup IF
	notify-
    THEN
    tick-notify? or 0= IF
	build-notification show-notification
	ticks to last-notify
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