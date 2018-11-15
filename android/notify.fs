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

: notify@ ( -- addr u )
    notify-otr? @ 0> IF  "expired message"  EXIT  THEN
    config:notify-text# @ IF
	notify-otr? @ config:notify-text# @ 0> and IF
	    "<i>[otr] message</i>"
	ELSE  notify$ $@  THEN
    ELSE  "encrypted message"  THEN ;
: ?nm ( -- )
    notification-manager 0= IF
	clazz .notificationManager to notification-manager
    THEN ;
: ?ni ( -- )
    ni 0= IF  clazz .gforthintent to ni  THEN ;
SDK_INT 11 >= [IF]
    [IFDEF] newNotificationChannel
	Variable channel$ "gnu.gforth.notifications" channel$ $!
	Variable ch-name$ "net2o messages" ch-name$ $!
	3 Value channel-prio#
	JValue nc
	: ?nc ( -- )
	    nc 0= IF
		clazz .notificationChannel to nc
		ch-name$ $@ make-jstring nc .setName
	    THEN ;
    [THEN]
    : msg-builder ( -- ) ?nm ?ni [IFDEF] ?nc ?nc
	    clazz channel$ $@ make-jstring newNotification.Builder+Id >o
	[ELSE]
	    clazz newNotification.Builder >o
	[THEN]
	config:notify-rgb# @ config:notify-on# @ config:notify-off# @ setLights o> >o
	config:notify-mode# @ setDefaults o> >o
	ni setContentIntent o> >o
	net2o-icon# setSmallIcon o> >o
	1 setAutoCancel o> to nb ;
    msg-builder
    : build-notification ( -- )
\	1000 clazz .screen_on
	['] notify-title $tmp dup 0= IF  2drop  EXIT  THEN
	1 pending-notifications +!
	make-jstring nb .setContentTitle
	>o notify@ make-jstring setContentText o>
	>o notify@ make-jstring setTicker o>
	>o pending-notifications @ setNumber o>
	[IFDEF] setGroup
	    >o js" net2o notifications" setGroup o>
	[THEN]
	.build to nf ;
    : show-notification ( -- )
	clazz >o 1 0 to argj0
	nf to argnotify o>
	['] notifyer post-it ;
\       1 nf notification-manager .notify ;
[ELSE]
    \ no notification for Android 2.3 for now...
    : msg-builder ( -- ) ;
    : build-notification ( -- ) ;
    : show-notification ( -- ) ;
[THEN]

:noname defers android-active rendering @ IF  notify-  THEN ;
is android-active
