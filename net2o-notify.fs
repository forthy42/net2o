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

: tick-notify? ( -- flag )
    ticks last-notify 64- delta-notify 64< ;
Variable notify? -2 notify? ! \ default: no notification when active
Variable notify$
Sema notify-sema
: notify; nip (;]) ]] notify-sema c-section ; [[ ;
: notify> comp-[: ['] notify; colon-sys-xt-offset stick ; immediate

[IFDEF] android
    also android also jni
    Variable pending-notifications
    jvalue nb
    jvalue ni
    jvalue nf
    jvalue notification-manager
    : notify+ ( addr u -- )  notify> notify$ $+! ;
    : notify! ( addr u -- )  notify> notify$ $! ;
    : ?nm ( -- )
	notification-manager 0= IF
	    NOTIFICATION_SERVICE clazz .getSystemService
	    to notification-manager
	THEN ;
    : ?ni ( -- )
	ni 0= IF  clazz .gforthintent to ni  THEN ;
    : msg-builder ( -- ) ?nm ?ni
	clazz newNotification.Builder to nb
	notify-rgb notify-on notify-off nb .setLights to nb
	notify-mode nb .setDefaults to nb
	0x01080077 nb .setSmallIcon to nb
	ni nb .setContentIntent to nb
	1 nb .setAutoCancel to nb ;
    msg-builder
    : build-notification ( -- )
	1 pending-notifications +!
	[: ." net2o: " pending-notifications @ dup .
	  ." Message" 1 > IF ." s"  THEN ;] $tmp
	make-jstring nb .setContentTitle to nb
	notify$ $@ make-jstring nb .setContentText to nb
	notify$ $@ make-jstring nb .setTicker to nb
	nb .build to nf ;
    : msg-notify ( -- ) notify>
	ticks to latest-notify
	rendering @ notify? @ <= dup IF
	    pending-notifications off
	    64#0 to last-notify
	THEN
	tick-notify? or 0= IF
	    build-notification
	    1 nf notification-manager .notify
	    ticks to last-notify
	THEN  notify$ $off ;
    previous previous
[ELSE]
    : notify+ notify> notify$ $+! ;
    : notify! notify> notify$ $! ;
    : msg-notify ( ." notificaton: " notify$ $. cr )
	notify> notify$ $off  ticks to last-notify ;
    : msg-builder ;
[THEN]

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