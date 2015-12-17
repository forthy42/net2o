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

Sema notify-sema
: notify; nip (;]) ]] notify-sema c-section ; [[ ;
: notify> comp-[: ['] notify; colon-sys-xt-offset stick ; immediate

: notify-title ( -- )
    ." net2o: " pending-notifications @ dup .
    ." Message" 1 > IF ." s"  THEN ;

[IFDEF] android
    also android also jni
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
	['] notify-title $tmp make-jstring nb .setContentTitle to nb
	notify@ make-jstring nb .setContentText to nb
	notify@ make-jstring nb .setTicker to nb
	nb .build to nf ;
    : show-notification ( -- )
	1 nf notification-manager .notify ;
[ELSE]
    : escape-<&> ( addr u -- )
	bounds ?DO  case i c@
		'<' of  ." &lt;"  endof
		'>' of  ." &gt;"  endof
		'&' of  ." &amp;" endof
		default: emit  endcase  LOOP ;
    
    : notify+ notify> notify$ $+! ;
    : notify! notify> notify$ $! ;
    : build-notification ( -- ) ;
    : notify@ ( -- addr u )
	notify-text IF  notify$ $@ ['] escape-<&> $tmp
	ELSE  "<i>hidden cryptic text</i>"  THEN ;

    [IFDEF] linux
	[IFDEF] fork+exec
	    Variable notify-send  $1000 notify-send $!len
	    s" which notify-send" r/o open-pipe throw
	    >r notify-send $@ r@ read-file throw r> close-file throw
	    notify-send $@ rot umin #lf -skip notify-send $!len  drop
	    
	    Variable net2o-logo
	    s" doc/net2o-logo.png" open-fpath-file 0= [IF]
		rot close-file throw
		over c@ '/' <> [IF]
		    pad $200 get-dir net2o-logo $! '/' net2o-logo c$+!
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
	[ELSE]
	    : linux-notification ( -- ) \ shell script based
		[ s" which notify-send >/dev/null 2>/dev/null" system
		$? 0= ] [IF]
		    [: .\" notify-send -a net2o -c im.received "
			[ s" doc/net2o-logo.png" open-fpath-file 0= ] [IF]
			    ." -i " [ over c@ '/' <> [IF]
				pad $200 get-dir ] sliteral type ." /" [
			    [THEN]
			    ] sliteral 'type' [ close-file throw ]
			[THEN]
			."  '" notify-title ." ' " 
			notify@ 'type' ;] $tmp system
		[THEN] ;
	[THEN]
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

: notify- ( -- )
    pending-notifications off
    64#0 to last-notify ;

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