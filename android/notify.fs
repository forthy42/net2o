\ Andoid specific notification stuff

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

also android also jni

0x01080077 Value net2o-icon# \ default
: get-net2o-icon# "gnu.gforth:drawable/net2o_notify" R.id ;
get-net2o-icon# to net2o-icon#
jvalue nb
jvalue ni
jvalue nf
jvalue notification-manager
: notify+ ( addr u -- )  notify> notify$ $+! ;
: notify! ( addr u -- )  notify> notify$ $! ;
: notify@ ( -- addr u )
    notify-text IF  notify$ $@
    ELSE  "hidden cryptic text"  THEN ;

: ?nm ( -- )
    notification-manager 0= IF
	NOTIFICATION_SERVICE clazz .getSystemService
	to notification-manager
    THEN ;
: ?ni ( -- )
    ni 0= IF  clazz .gforthintent to ni  THEN ;
SDK_INT 11 >= [IF]
    : msg-builder ( -- ) ?nm ?ni
	clazz newNotification.Builder to nb
	notify-rgb notify-on notify-off nb .setLights to nb
	notify-mode nb .setDefaults to nb
	ni nb .setContentIntent to nb
	net2o-icon# nb .setSmallIcon to nb
	1 nb .setAutoCancel to nb ;
    msg-builder
    : build-notification ( -- )
	1000 clazz .screen_on
	1 pending-notifications +!
	['] notify-title $tmp make-jstring nb .setContentTitle to nb
	notify@ make-jstring nb .setContentText to nb
	notify@ make-jstring nb .setTicker to nb
	nb .build to nf ;
    : show-notification ( -- )
	1 nf notification-manager .notify ;
[ELSE]
    \ no notification for Android 2.3 for now...
    : msg-builder ( -- ) ;
    : build-notification ( -- ) ;
    : show-notification ( -- ) ;
[THEN]

:noname defers android-active rendering @ IF  notify-  THEN ;
is android-active