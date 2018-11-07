\ net2o GUI

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

require minos2/widgets.fs

also minos

ctx 0= [IF]  window-init  [THEN]

require minos2/font-style.fs

: slide-frame ( glue color -- o )
    font-size# 70% f* }}frame ;
: bar-frame ( glue color -- o )
    font-size# 20% f* }}frame dup .button3 ;
: update-size# ( -- )
    screen-pwh max s>f
    default-diag screen-diag f/ fsqrt default-scale f* 1/f #48 fm*
    f/ fround to font-size#
    font-size# 133% f* fround to baseline#
    font-size# 32e f/ to pixelsize# ;

update-size#

require minos2/text-style.fs

glue new Constant glue-sleft
glue new Constant glue-sright
glue ' new static-a with-allocater Constant glue-left
glue ' new static-a with-allocater Constant glue-right

: glue0 ( -- ) 0e fdup
    [ glue-left  .hglue-c ]L df!
    [ glue-right .hglue-c ]L df! ;
glue0

Variable slides[]
Variable slide#

: >slides ( o -- ) slides[] >stack ;

: !slides ( nprev n -- )
    over slide# !
    slides[] $[] @ /flip drop
    slides[] $[] @ /flop drop glue0 ;
: anim!slides ( r0..1 n -- )
    slides[] $[] @ /flop drop
    fdup fnegate dpy-w @ fm* glue-left  .hglue-c df!
    -1e f+       dpy-w @ fm* glue-right .hglue-c df! ;

: prev-anim ( n r0..1 -- )
    dup 0<= IF  drop fdrop  EXIT  THEN
    fdup 1e f>= IF  fdrop
	dup 1- swap !slides +sync +config  EXIT
    THEN
    1e fswap f- 1- sin-t anim!slides +sync +config ;

: next-anim ( n r0..1 -- )
    dup slides[] $[]# 1- u>= IF  drop fdrop  EXIT  THEN
    fdup 1e f>= IF  fdrop
	dup 1+ swap !slides +sync +config  EXIT
    THEN
    1+ sin-t anim!slides +sync +config ;

1e FValue slide-time%

: prev-slide ( -- )
    slide-time% anims[] $@len IF  anim-end .2e f*  THEN
    slide# @ ['] prev-anim >animate ;
: next-slide ( -- )
    slide-time% anims[] $@len IF  anim-end .2e f*  THEN
    slide# @ ['] next-anim >animate ;

\ frames

0 Value pw-frame
0 Value id-frame
0 Value chat-frame

\ password screen

0 Value pw-err
0 Value pw-num

: err-fade ( r addr -- )
    1e fover [ pi f2* ] Fliteral f* fcos 1e f+ f2/ f-
    2 tries# @ lshift s>f f* fdup 1e f> IF fdrop 1e ELSE +sync +config THEN
    $FF swap .fade fdrop ;

: shake-lr ( r addr -- )
    [ pi 16e f* ] FLiteral f* fsin f2/ 0.5e f+ \ 8 times shake
    font-size# f2/ f* font-size# f2/ fover f-
    glue-sleft  >o 0g fdup hglue-c glue! o>
    glue-sright >o 0g fdup hglue-c glue! o> +sync +config drop ;

0e 0 shake-lr

: pres-frame ( color -- o1 o2 ) \ drop $FFFFFFFF
    glue*wh swap slide-frame dup .button1 ;

: err-fade? ( -- flag ) 0 { flag }
    anims@ 0 ?DO
	>o action-of animate ['] err-fade = flag or to flag
	o anims[] >stack o>
    LOOP  flag ;

forward show-nicks
forward gui-msgs

: pw-done ( max span addr pos -- max span addr pos flag )
    err-fade? IF  false  EXIT  THEN
    over 3 pick >passphrase +key
    read-keys secret-keys# 0= IF
	\ ." Wrong passphrase" cr
	1 tries# +! tries# @ 0 <# #s #> pw-num >o to text$ o>
	keys sec[]free
	drop nip 0 tuck false
	1e o ['] shake-lr >animate
	1 tries# @ lshift s>f f2/ pw-err ['] err-fade >animate
    ELSE
	0 >o 0 secret-key init-client >raw-key
	read-chatgroups announce-me
	o>
	\ ." Right passphrase" cr
	show-nicks
	drop nip 0 tuck true
    THEN ;

: 20%bt ( o -- o ) >o font-size# 20% f* to bordert o o> ;
: 25%b ( o -- o ) >o font-size# 25% f* to border o o> ;
: 25%bv ( o -- o ) >o font-size# 25% f* fdup to border fnegate to borderv o o> ;
: 40%b ( o -- o ) >o font-size# 40% f* to border o o> ;

\ password frame

tex: net2o-logo

[IFDEF] light-login \ light color sceme
    $0000BFFF Value dark-blue#
    $FF0040FF Value pw-num-col#
    $FFFFFFFF Value pw-bg-col#
    $AAAAAAFF Value pw-text-col#
    $0000FF08 Value chbs-col#
    $FFFFFFFF Value login-bg-col#
    $000000FF Value show-sign-color#
[ELSE]
    $88FF00FF Value dark-blue#
    $FF0040FF Value pw-num-col#
    $550000FF Value pw-bg-col#
    $cc6600FF Value pw-text-col#
    $00FF0020 Value chbs-col#
    $000020FF Value login-bg-col#
    $FFFFFFFF Value show-sign-color#
[THEN]

glue new Constant glue*lll±
glue*lll± >o 1Mglue hglue-c glue! 0glue fnip 1filll fswap dglue-c glue! 1glue vglue-c glue! o>

glue new Constant glue*shrink
glue*shrink >o 0e 1filll 0e hglue-c glue! 1glue dglue-c glue! 1glue vglue-c glue! o>

{{ login-bg-col# pres-frame dark-blue# ' dark-blue >body !
    {{
	glue*lll± }}glue
	' net2o-logo "doc/net2o.png" 0.666e }}image-file Constant net2o-glue /center
	!i18n l" net2o GUI" /title
	!lit
	{{
	    glue*lll }}glue
	    glue-sleft }}glue
	    {{
		\large \sans "🔐" }}text
		\large pw-num-col# to x-color s" " }}text
		25%b dup to pw-num /center
	    }}z
	    {{
		glue*l pw-bg-col# font-size# f2/ f2/ }}frame dup .button3
		\mono \normal
		{{ chbs-col# to x-color "Correct Horse Battery Staple" }}text 25%b
		glue*l }}h
		{{
		    glue-sright }}glue
		    glue*l }}glue
		    l" wrong passphrase!" $FF000000 to x-color }}i18n-text
		    25%b dup to pw-err
		    glue*l }}glue
		    glue-sleft }}glue
		}}h
		blackish
		{{
		    {{
			pw-text-col# to x-color
			"" }}pw dup Value pw-field
			25%b >o config:passmode# @ to pw-mode o o>
			glue*l }}glue
		    }}h
		    pw-field ' pw-done edit[]
		    {{
			\large \sans $FFFFFFFF to x-color "👁" }}text
			\normal \bold show-sign-color# to x-color "＼" }}text dup value show-pw-sign /center blackish
		    }}z \regular
		    : pw-show/hide ( flag -- )
			dup 0 show-sign-color# rot select show-pw-sign >o to text-color o>
			2 config:passmode# @ 1 min rot select pw-field >o to pw-mode o>
			pw-field engage +sync ;
		    ' pw-show/hide config:passmode# @ 1 > toggle[]
		    \normal
		}}h box[]
	    }}z box[]
	    glue-sright }}glue
	    glue*lll }}glue
	}}h box[] \skip >bl
	!i18n l" Enter passphrase to unlock" /subtitle !lit
	glue*lll }}glue
    }}v box[]
}}z box[] to pw-frame

$0000BFFF ' dark-blue >body !

\ id frame

0 Value mykey-box
0 Value groups-box
0 Value nicks-box
0 Value msgs-box
0 Value msg-box
0 Value msg-par
0 Value msg-vbox

0 Value group-name
0 Value group-members

htab-glue new tab-glue: name-tab
htab-glue new tab-glue: pk-tab
htab-glue new tab-glue: group-tab
htab-glue new tab-glue: chatname-tab

[IFUNDEF] child+
    : child+ ( o -- ) o over >o to parent-w o> childs[] >stack ;
[THEN]

Create ke-imports#rgb

Create imports#rgb-bg
$33EE33FF , \ myself is pretty green
$BBDD66FF , \ manually imported is green, too
$55DD55FF , \ scanned is more green
$CCEE55FF , \ seen in chat is more yellow
$EECC55FF , \ imported from DHT is pretty yellow
$FF8844FF , \ invited is very yellow
$FF6600FF , \ provisional is very orange
$FF0000FF , \ untrusted is last
Create imports#rgb-fg
$003300FF ,
$000000FF ,
$000000FF ,
$000000FF ,
$0000FFFF ,
$0000FFFF ,
$0000FFFF ,
$00FFFFFF ,

: nick[] ( box o:nick -- box )
    [: data >o ." clicked on " ke-nick $. cr o> ;] o click[] ;

: show-nick ( o:key -- )
    ke-imports @ >im-color# cells { ki }
    {{ glue*l imports#rgb-bg ki + @ slide-frame dup .button1
	{{
	    {{ \large imports#rgb-fg ki + @ to x-color
		ke-sk sec@ nip IF  \bold  ELSE  \regular  THEN  \sans
		['] .nick-base $tmp }}text 25%b
		ke-pets[] $[]# IF
		    {{ glue*l $00FF0020 slide-frame dup .button3
			['] .pet-base $tmp }}text 25%b
		    }}z
		THEN
	    glue*l }}glue }}h name-tab
	    {{
		{{ \sans \script ke-selfsig $@ ['] .sigdates $tmp }}text glue*l }}glue }}h
		{{ \mono \script ke-pk $@ key| ['] 85type $tmp }}text 20%bt glue*l }}glue }}h swap
	    }}v pk-tab
	glue*lll }}glue }}h
    }}z nick[]  \regular
    mykey-box nicks-box ke-sk sec@ nip select /flop .child+ ;

: fill-nicks ( -- )
    keys>sort[]
    key-list[] $@ bounds ?DO
	I @ .show-nick
    cell +LOOP ;

: refresh-top ( -- )
    +sync +lang
    top-widget >o htop-resize  <draw-init draw-init draw-init> htop-resize
    false to grab-move? o> ;

: show-connected ( -- ) main-up@ connection .wait-task ! ;

: gui-chat-connects ( -- )
    [: up@ wait-task ! ;] IS do-connect
    [: chat-keys [:
	    2dup search-connect ?dup-IF  >o +group greet o> 2drop  EXIT  THEN
	    2dup pk-peek? IF  chat-connect true !!connected!!
	    ELSE  2drop  THEN ;] $[]map ;] catch
    [ ' !!connected!! >body @ ]L = IF  show-connected  THEN ;

event: :>!connection    to connection ;
event: :>chat-connects  gui-chat-connects
    <event connection dup elit, :>!connection .wait-task @ event> ;

: group[] ( box group -- box )
    [:	data $@ group-name >o to text$ o>
	data cell+ $@ drop cell+ >o groups:id$ groups:member[] o>
	[: chat-keys $+[]! ;] $[]map
	gui-msgs  <event :>chat-connects ?query-task event>
	next-slide
    ;] swap click[] ;

: show-group ( last# -- )
    dup { g -- } cell+ $@ drop cell+ >o
    {{ glue*l $CCAA44FF slide-frame dup .button1
	{{
	    {{ \large blackish
		\regular \sans g $@ }}text 25%b
	    glue*l }}glue }}h name-tab
	    {{
		{{
		    \mono \bold \script groups:id$
		    2dup g $@ str= 0= IF  key| ['] 85type $tmp  THEN
		}}text 20%bt glue*l }}glue }}h
		glue*l }}glue
	    }}v pk-tab
	glue*lll }}glue }}h
    }}z g group[] o>
    groups-box /flop .child+ ;

: fill-groups ( -- )
    groups>sort[]
    group-list[] $@ bounds ?DO
	I @ show-group
    cell +LOOP ;

: }}button-lit { d: text color -- o }
    {{
        glue*l color font-size# 40% f* }}frame dup .button2
        text }}text 25%b /center
    }}z box[] ;

: nicks-title ( -- )
    {{ glue*l $000000FF slide-frame dup .button1
	{{
	    {{ \large \bold \sans whitish
	    l" Nick+Pet" }}i18n-text 25%b glue*l }}glue }}h name-tab
	    {{
		{{ \script \mono \bold l" Pubkey"   }}i18n-text 20%bt glue*l }}glue }}h
		{{ \script \sans \bold l" Key date" }}i18n-text glue*l }}glue }}h
	    }}v pk-tab
	    glue*lll }}glue
	    \large s" ❌" $444444FF }}button-lit [: -1 data +! ;] level# click[]
	}}h box[]
    }}z box[] ;

{{ $FFFF80FF pres-frame
    {{
	{{
	    nicks-title
	    glue*shrink }}glue
	}}h box[]
	{{
	    {{
		{{ glue*l $303000FF bar-frame
		{{ \script l" My key" }}i18n-text 25%b glue*l }}glue }}h }}z
		{{ }}v box[] dup to mykey-box
		{{ glue*l $300030FF bar-frame
		{{ \script l" My groups" }}i18n-text 25%b glue*l }}glue }}h }}z
		{{ }}v box[] dup to groups-box /vflip
		{{ glue*l $003030FF bar-frame
		{{ \script l" My peers" }}i18n-text 25%b glue*l }}glue }}h }}z
		{{ }}v box[] dup to nicks-box /vflip
		glue*lll }}glue
	    tex: vp-nicks vp-nicks glue*lll ' vp-nicks }}vp vp[] dup value peers-box
	    $444444FF to slider-color
	    $CCCCCCFF to slider-fgcolor
	    font-size# 33% f* to slider-border
	dup font-size# 66% f* fdup vslider }}h box[]
    }}v box[]
}}z box[] to id-frame

: show-nicks ( -- )
    fill-nicks fill-groups next-slide
    0.01e peers-box [: .vp-top fdrop ;] >animate ;

\ messages

msg-class class
end-class wmsg-class

$88FF88FF Value my-signal#
$CCFFCCFF Value other-signal#
$CC00CCFF Value my-signal-otr#
$880088FF Value other-signal-otr#
$4444CCFF color: link-blue
$44CC44FF color: re-green
$CC4444FF color: obj-red
$BBDDDDFF color: msg-bg
$00BFFFFF color: light-blue
$44FF44FF color: greenish
$33883366 Value day-color#
$88333366 Value hour-color#

Variable last-bubble-pk
0 Value last-otr?
0 Value last-bubble
64#0 64Value last-tick
#300 #1000000000 um* d>64 64Constant delta-bubble

: >bubble-border ( o me? -- )
    swap >o font-size# 25% f*
    IF
	fdup f2* to border
	fnegate fdup to borderl fdup to borderv to bordert
    ELSE
	fdup f2* to border
	0e to borderl fnegate f2* to bordert 0e to borderv
    THEN o o> ;
: add-dtms ( ticks -- )
    \small
    1n fm* >day { day } day last-day <> IF
	{{
	    glue*l day-color# slide-frame dup .button1
	    \bold day ['] .day $tmp }}text 25%b \regular
	}}z /center msgs-box .child+
    THEN  day to last-day
    24 fm* fsplit { hour } hour last-hour <>
    60 fm* fsplit { minute } minute 10 / last-minute 10 / <> or
    IF
	{{
	    glue*l hour-color# slide-frame dup .button1
	    60 fm* fsplit minute hour
	    [: .## ':' emit .## ':' emit .## 'Z' emit ;] $tmp }}text 25%b
	}}z /center msgs-box .child+
    THEN  hour to last-hour  minute to last-minute
    fdrop \normal ;

: otr? ( tick -- flag )
    64dup 64#-1 64<> ;
: text-color! ( -- ) last-otr? IF  greenish  ELSE  blackish  THEN ;

[IFDEF] android
    also jni
    : open-url ( addr u -- )
	clazz >o make-jstring to args0 o>
	['] startbrowser post-it ;
    previous
[ELSE]
    [IFDEF] linux
	: open-url ( addr u -- )
	    [: ." xdg-open " type ;] $tmp system ;
    [THEN]
[THEN]

:noname ( -- )
    glue*ll }}glue msg-box .child+
    dpy-w @ 90% fm* msg-par .par-split
    {{ msg-par unbox }}
    dup >r 0 ?DO  I pick box[] "unboxed" name! drop  LOOP  r>
    msg-vbox .+childs
; wmsg-class to msg:end
: new-msg-par ( -- )
    {{ }}p "msg-par" name!
    dup .subbox box[] drop box[] cbl >bl
    dup .subbox "msg-box" name!
    to msg-box to msg-par ;
:noname { d: pk -- o }
    pk [: .simple-id ." : " ;] $tmp notify-nick!
    pk key| pkc over str= { me? }
    pk enddate@ otr? { otr }
    pk key| last-bubble-pk $@ str= otr last-otr? = and
    pk startdate@ last-tick 64over to last-tick
    64- delta-bubble 64< and
    IF
	new-msg-par
    ELSE
	pk startdate@ add-dtms
	pk key| last-bubble-pk $!  otr to last-otr?  text-color!
	{{
	    {{ glue*l }}glue
		{{ \sans \normal
		    {{
			glue*l }}glue
			\bold pk ['] .key-id $tmp }}text 25%b
			>o imports#rgb-fg last-ki >im-color# cells + @ to text-color
			o o> me? IF  swap  THEN
			\regular
		    }}h
		    glue*l imports#rgb-bg last-ki >im-color# cells + @
		    slide-frame dup .button2
		    swap
		}}z me? 0= IF  chatname-tab  THEN
	    }}v
	    {{
		glue*l $000000FF $FFFFFFFF last-otr? select
		slide-frame dup me? IF .rbubble ELSE .lbubble THEN
		"bubble" name!
		{{
		    new-msg-par
		}}v box[] dup to msg-vbox "msg-vbox" name!
		me? >bubble-border
	    }}z box[] "msg-zbox" name!
	    glue*ll }}glue
	    me? IF  swap rot  THEN
	}}h box[] "msgs-box" name! msgs-box .child+
    THEN
; wmsg-class to msg:start
:noname { d: string -- o }
    link-blue \mono string [: '#' emit type ;] $tmp
    ['] utf8-sanitize $tmp }}text text-color! \sans
    msg-box .child+
; wmsg-class to msg:tag
:noname { d: string -- o }
    text-color!
    string ['] utf8-sanitize $tmp }}text 25%bv
    "text" name! msg-box .child+
; wmsg-class to msg:text
:noname { d: string -- o }
    \italic last-otr? IF light-blue ELSE dark-blue THEN
    string ['] utf8-sanitize $tmp }}text 25%bv \regular
    text-color!
    "action" name! msg-box .child+
; wmsg-class to msg:action
:noname { d: string -- o }
    last-otr? IF light-blue ELSE dark-blue THEN
    string ['] utf8-sanitize $tmp }}text _underline_ 25%bv
    text-color!
    [: data >o text$ o> open-url ;]
    over click[]
    click( ." url: " dup ..parents cr )
    "url" name! msg-box .child+
; wmsg-class to msg:url
:noname { d: string -- o }
    {{
	glue*l $FFCCCCFF slide-frame dup .button1
	string [: ."  GPS: " .coords ;] $tmp }}text 25%b
    }}z "gps" name! msg-box .child+
; wmsg-class to msg:coord
:noname { d: pk -- o }
    {{
	pk key|
	2dup 0 .pk@ key| str=
	last-otr? IF  my-signal-otr# other-signal-otr#
	ELSE  my-signal# other-signal#  THEN  rot select
	glue*l swap slide-frame dup .button1 40%b >r
	[: '@' emit .key-id ;] $tmp ['] utf8-sanitize $tmp }}text 25%b r> swap
    }}z msg-box .child+
; wmsg-class to msg:signal
:noname ( addr u -- )
    re-green [: ." [" 85type ." ]→" ;] $tmp }}text msg-box .child+
    text-color!
; msg-class to msg:re
:noname ( addr u -- )
    obj-red [: ." [" 85type ." ]:" ;] $tmp }}text msg-box .child+
    text-color!
; msg-class to msg:id

in net2o : new-wmsg ( o:connection -- o )
    o wmsg-class new >o  parent!  msg-table @ token-table ! o o> ;
' net2o:new-wmsg is net2o:new-msg

wmsg-class ' new static-a with-allocater Constant wmsg-o
wmsg-o >o msg-table @ token-table ! o>

: vp-softbottom ( o:viewport -- )
    act >o o anim-del  set-startxy
    0e           to vmotion-dx
    vp-y fnegate to vmotion-dy
    0.333e o ['] vp-scroll >animate o> ;

: wmsg-display ( addr u -- )
    msg-tdisplay
    msgs-box >o [: +sync +config +resize ;] vp-needed vp-bottom
    +sync +config o> ;
' wmsg-display wmsg-class to msg:display

#128 Value gui-msgs# \ display last 128 messages
0 Value chat-edit    \ chat edit field

: (gui-msgs) ( gaddr u -- )
    reset-time
    msgs-box .dispose-childs
    glue*lll }}glue msgs-box .child+
    2dup load-msg ?msg-log
    last# msg-log@ 2dup { log u }
    dup gui-msgs# cells - 0 max /string bounds ?DO
	I $@ ['] wmsg-display wmsg-o .catch IF
	    <err> ." invalid entry" <default> cr 2drop
	THEN
    cell +LOOP
    log free throw  msgs-box >o resized vp-bottom o>
    chat-edit engage ;

: gui-msgs ( gaddr u -- )
    2dup msg-group$ $! (gui-msgs) ;

: msg-wredisplay ( n -- )
    drop 0 otr-mode
    [: msg-group$ $@ (gui-msgs) ;] !wrapper
    msgs-box >o [: +sync +config +resize ;] vp-needed vp-bottom
    +sync +config o>  ;
' msg-wredisplay wmsg-class is msg:redisplay

[IFDEF] android also android [THEN]

: chat-edit-enter ( o:edit-w -- )
    text$ dup IF  do-chat-cmd? 0= IF  avalanche-text  THEN
    ELSE  2drop  THEN
    64#-1 line-date 64!  $lastline $free ;

\ +db click( \ )

{{ $80FFFFFF pres-frame
    {{
	{{
	    glue*l $000000FF slide-frame dup .button1
	    {{
		\large whitish
		"⬅" }}text 40%b [: leave-chats prev-slide ;] over click[]
		!i18n l" Chat Log" }}text' !lit 40%b
		"" }}text 40%b dup to group-name
		{{
		}}h box[] dup to group-members
		glue*l }}glue
	    }}h box[]
	}}z box[]
	{{
	    {{
		{{
		tex: vp-chats vp-chats glue*lll ' vp-chats }}vp vp[]
		dup to msgs-box
		dup font-size# 66% f* fdup vslider
	    over >r }}h box[] r>
	    font-size# 66% f* fdup hslider
	}}v box[]
	{{
	    {{ glue*lll $FFFFFFFF font-size# 40% f* }}frame dup .button3
		{{ \normal \regular blackish "" }}edit 40%b dup to chat-edit glue*l }}glue
		    glue*lll }}glue
		}}h box[]
	    }}z chat-edit [: edit-w .chat-edit-enter drop nip 0 tuck false ;] edit[]
	    >o act >o [: connection .chat-next-line ;] is edit-next-line o> o o>
	    >o act >o [: connection .chat-prev-line ;] is edit-prev-line o> o o>
	    {{
		glue*l $80FF80FF font-size# 40% f* }}frame dup .button2
		!i18n l" Send" }}text' !lit 40%b
		[: data >o chat-edit-enter "" to text$ o> ;] chat-edit click[]
	    }}z box[]
	}}h box[]
    }}v box[]
}}z box[] to chat-frame

[IFDEF] android previous [THEN]

\ top box

{{
    glue-left }}glue
    pw-frame          dup >slides
    id-frame   /flip  dup >slides
    chat-frame /flip  dup >slides
    glue-right }}glue
}}h box[]
Value n2o-frame

\ top widgets

: !widgets ( -- )
    top-widget .htop-resize
    pw-field engage
    1e ambient% sf! set-uniforms ;

: net2o-gui ( -- )
    n2o-frame to top-widget
    "PASSPHRASE" getenv 2dup d0= IF  2drop
    ELSE
	>passphrase +key  read-keys
	"PASSPHRASE" getenv erase \ erase passphrase after use!
    THEN
    secret-keys# IF  show-nicks  THEN
    1config  !widgets  widgets-loop ;

' net2o-gui is run-gui

previous

\ localization

cs-scope: lang

locale en \ may differ from development language
locale de \ German
locale zh \ Chinese

}scope

lang:de include-locale lang/de
lang:zh include-locale lang/zh
lang:en include-locale lang/en

: ?lang ( addr u -- )
    ['] lang >body find-name-in ?dup-IF  execute  THEN ;

s" LANG" getenv '_' $split 2swap ?lang '.' $split ?lang ?lang

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
     (("{{") (0 . 2) (0 . 2) immediate)
     (("}}h" "}}v" "}}z" "}}vp" "}}p") (-2 . 0) (-2 . 0) immediate)
    )
End:
[THEN]
