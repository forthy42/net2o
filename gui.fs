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
	dup 1- swap !slides +sync +resize  EXIT
    THEN
    1e fswap f- 1- sin-t anim!slides +sync +resize ;

: next-anim ( n r0..1 -- )
    dup slides[] $[]# 1- u>= IF  drop fdrop  EXIT  THEN
    fdup 1e f>= IF  fdrop
	dup 1+ swap !slides +sync +resize  EXIT
    THEN
    1+ sin-t anim!slides +sync +resize ;

0.4e FValue slide-time%

: prev-slide ( -- )
    slide-time% anims[] $@len IF  anim-end 50% f*  THEN
    slide# @ ['] prev-anim >animate +textures +lang ;
: next-slide ( -- )
    slide-time% anims[] $@len IF  anim-end 50% f*  THEN
    slide# @ ['] next-anim >animate +textures +lang ;

\ frames

0 Value pw-frame
0 Value id-frame
0 Value chat-frame

\ password screen

0 Value pw-err
0 Value pw-num
0 Value phrase-unlock
0 Value create-new-id
0 Value phrase-again
0 Value plus-login
0 Value minus-login
0 Value nick-edit

: err-fade ( r addr -- )
    1e fover [ pi f2* ] Fliteral f* fcos 1e f+ f2/ f-
    2 tries# @ lshift s>f f* fdup 1e f> IF fdrop 1e ELSE +sync +resize THEN
    .fade fdrop ;

: shake-lr ( r addr -- )
    [ pi 16e f* ] FLiteral f* fsin f2/ 0.5e f+ \ 8 times shake
    font-size# f2/ f* font-size# f2/ fover f-
    glue-sleft  >o 0g fdup hglue-c glue! o>
    glue-sright >o 0g fdup hglue-c glue! o> +sync +resize drop ;

0e 0 shake-lr

: pres-frame ( color -- o1 o2 ) \ drop $FFFFFFFF
    glue*wh slide-frame dup .button1 ;

: err-fade? ( -- flag ) 0 { flag }
    anims@ 0 ?DO
	>o action-of animate ['] err-fade = flag or to flag
	o anims[] >stack o>
    LOOP  flag ;

forward show-nicks
forward gui-msgs
0 Value title-vp

Variable nick$

: nick-done ( max span addr pos -- max span addr pos flag )
    over 3 pick nick$ $! true ;

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
    $FF0040FF text-color, FValue pw-num-col#
    $AAAAAAFF text-color, FValue pw-text-col#
    $000000FF text-color, FValue show-sign-color#
    $FFFFFFFF color, FValue pw-bg-col#
    $0000BFFF color, FValue dark-blue#
    $0000FF08 color, FValue chbs-col#
    $FFFFFFFF color, FValue login-bg-col#
[ELSE]
    $FF0040FF text-color, FValue pw-num-col#
    $cc6600FF text-color, FValue pw-text-col#
    $FFFFFFFF text-color, FValue show-sign-color#
    $550000FF color, FValue pw-bg-col#
    $88FF00FF color, FValue dark-blue#
    $00FF0020 color, FValue chbs-col#
    $000020FF color, FValue login-bg-col#
[THEN]

glue new Constant glue*lll±
glue*lll± >o 1Mglue fnip 1000e fswap hglue-c glue! 0glue fnip 1filll fswap dglue-c glue! 1glue vglue-c glue! o>

glue new Constant glue*shrink
glue*shrink >o 0e 1filll 0e hglue-c glue! 1glue dglue-c glue! 1glue vglue-c glue! o>

{{ login-bg-col# pres-frame dark-blue# ' dark-blue >body f!
    {{
	{{ glue*lll± }}glue }}v
	' net2o-logo "doc/net2o.png" 0.666e }}image-file Constant net2o-glue /center
	!i18n l" net2o GUI" /title
	!lit
	\footnote cbl dark-blue net2o-version }}text /center
	{{
	    {{
		glue*ll }}glue
		\large "👤" }}text \normal
		{{
		    glue*l pw-bg-col# font-size# f2/ f2/ }}frame dup .button3
		    transp# to x-color
		    "f(g" }}text /left 25%b
		    {{
			nt
			white# to x-color \bold
			"nick" }}edit 25%b dup Value nick-field
			glue*lll }}glue \regular
		    }}h bx-tab nick-field ' nick-done edit[]
		}}z box[] blackish
		{{ \large "👤" }}text \normal }}h /phantom
		glue*ll }}glue
	    }}h box[]
	}}v box[] /vflip dup to nick-edit
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
		    glue*l }}glue \bold
		    l" wrong passphrase!" $FF000000 $FF0000FF fade-color,
		    to x-color }}i18n-text \regular
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
			glue*lll }}glue
		    }}h
		    pw-field ' pw-done edit[]
		    {{
			\large \sans $FFFFFFFF text-color, to x-color "👁" }}text
			\normal \bold show-sign-color# to x-color "＼" }}text dup value show-pw-sign /center blackish
		    }}z \regular
		    : pw-show/hide ( flag -- )
			dup IF  ['] transparent >body f@
			ELSE  show-sign-color#  THEN
			show-pw-sign >o to text-color o>
			2 config:passmode# @ 1 min rot select pw-field >o to pw-mode o>
			pw-field engage +sync ;
		    ' pw-show/hide config:passmode# @ 1 > toggle[]
		    \normal
		}}h box[]
	    }}z box[] bx-tab
	    {{
		\large
		"🔴" }}text \normal  >o font-size# 10% f* to raise o o>
		$000000FF dup text-emoji-color, to x-color
		"➕" }}text /center dup to plus-login
		"➖" }}text /center dup to minus-login /vflip
		\large
		: id-show-hide ( flag -- )
		    IF
			phrase-unlock /hflip
			create-new-id /flop
			plus-login /flip
			minus-login /flop
			nick-edit /flop
			[ x-baseline ] FLiteral nick-edit >o
			fdup gap% f* to gap to baseline o>
			"nick" nick-field engage-edit
		    ELSE
			phrase-unlock /flop
			create-new-id /hflip
			plus-login /flop
			minus-login /flip
			nick-edit /vflip
			0e nick-edit >o to baseline o>
		    THEN +resize +lang ;
		\normal
	    }}z ' id-show-hide false toggle[]
	    glue-sright }}glue
	    glue*lll }}glue
	}}h box[] \skip >bl
	{{  \small dark-blue !i18n
	    l" Enter passphrase to unlock" }}text' /center dup to phrase-unlock
	    l" Create new ID" }}text' /center dup to create-new-id /hflip
	    l" Enter passphrase again" }}text' /center dup to phrase-again /hflip
	    !lit
	}}z box[] /center >bl
	{{ glue*lll }}glue }}v
    }}v box[]
}}z box[] to pw-frame

$0000BFFF text-color, ' dark-blue >body f!

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

new-htab tab-glue: name-tab
new-htab tab-glue: pk-tab
new-htab tab-glue: group-tab
new-htab tab-glue: chatname-tab

[IFUNDEF] child+
    : child+ ( o -- ) o over >o to parent-w o> childs[] >stack ;
[THEN]

Create ke-imports#rgb

Create imports#rgb-bg
$33EE33FF color, sf, \ myself is pretty green
$BBDD66FF color, sf, \ manually imported is green, too
$55DD55FF color, sf, \ scanned is more green
$CCEE55FF color, sf, \ seen in chat is more yellow
$EECC55FF color, sf, \ imported from DHT is pretty yellow
$FF8844FF color, sf, \ invited is very yellow
$FF6600FF color, sf, \ provisional is very orange
$FF0000FF color, sf, \ untrusted is last
Create imports#rgb-fg
$003300FF color, sf,
$000000FF color, sf,
$000000FF color, sf,
$000000FF color, sf,
$0000FFFF color, sf,
$0000FFFF color, sf,
$0000FFFF color, sf,
$00FFFFFF color, sf,

: nick[] ( box o:nick -- box )
    [: data >o ." clicked on " ke-nick $. cr o> ;] o click[] ;

: show-nick ( o:key -- )
    ke-imports @ >im-color# sfloats { ki }
    {{ glue*l imports#rgb-bg ki + sf@ slide-frame dup .button1
	{{
	    {{ \large imports#rgb-fg ki + sf@ to x-color
		ke-sk sec@ nip IF  \bold  ELSE  \regular  THEN  \sans
		['] .nick-base $tmp }}text 25%b
		ke-pets[] $[]# IF
		    {{ glue*l $00FF0020 color, slide-frame dup .button3
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
    {{ glue*l $CCAA44FF color, slide-frame dup .button1
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
        glue*l color color, font-size# 40% f* }}frame dup .button2
        text }}text 25%b /center
    }}z box[] ;

also [ifdef] android android [then]

tex: vp-title

: nicks-title ( -- )
    {{ glue*l $000000FF color, slide-frame dup .button1
	{{
	    {{
		{{
		    {{ \large \bold \sans whitish
		    l" Nick+Pet" }}i18n-text 25%b glue*l }}glue }}h name-tab
		    {{
			{{ \script \mono \bold l" Pubkey"   }}i18n-text 20%bt glue*l }}glue }}h
			{{ \script \sans \bold l" Key date" }}i18n-text glue*l }}glue }}h
		    }}v pk-tab
		    glue*lll± }}glue
		}}h box[]
	    vp-title glue*lll ['] vp-title }}vp vp[] dup to title-vp
	    \large s" ❌" $444444FF }}button-lit [: -1 data +! ;] level# click[]
	}}h box[]
    }}z box[] ;

previous

{{ $FFFF80FF color, pres-frame
    {{
	{{
	    nicks-title
	    glue*shrink }}glue
	}}h box[]
	{{
	    {{
		{{ glue*l $303000FF color, bar-frame
		{{ \script l" My key" }}i18n-text 25%b glue*l }}glue }}h }}z
		{{ }}v box[] dup to mykey-box
		{{ glue*l $300030FF color, bar-frame
		{{ \script l" My groups" }}i18n-text 25%b glue*l }}glue }}h }}z
		{{ }}v box[] dup to groups-box /vflip
		{{ glue*l $003030FF color, bar-frame
		{{ \script l" My peers" }}i18n-text 25%b glue*l }}glue }}h }}z
		{{ }}v box[] dup to nicks-box /vflip
		glue*lll }}glue
	    tex: vp-nicks vp-nicks glue*lll ' vp-nicks }}vp vp[] dup value peers-box
	    $444444FF color, to slider-color
	    $CCCCCCFF color, to slider-fgcolor
	    font-size# 33% f* to slider-border
	dup font-size# 66% f* fdup vslider }}h box[]
    }}v box[]
}}z box[] to id-frame

: show-nicks ( -- )
    fill-nicks fill-groups next-slide
    0.01e peers-box [: .vp-top fdrop title-vp .vp-top +sync +resize ;] >animate ;

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
    \small blackish
    1n fm* >day { day } day last-day <> IF
	{{
	    glue*l day-color# color, slide-frame dup .button1
	    \bold day ['] .day $tmp }}text 25%b \regular
	}}z /center msgs-box .child+
    THEN  day to last-day
    24 fm* fsplit { hour } hour last-hour <>
    60 fm* fsplit { minute } minute 10 / last-minute 10 / <> or
    IF
	{{
	    glue*l hour-color# color, slide-frame dup .button1
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
			>o imports#rgb-fg last-ki >im-color# sfloats + sf@
			to text-color
			o o> me? IF  swap  THEN
			\regular
		    }}h
		    glue*l imports#rgb-bg last-ki >im-color# sfloats + sf@
		    slide-frame dup .button2
		    swap
		}}z me? 0= IF  chatname-tab  THEN
	    }}v
	    {{
		glue*l $000000FF $FFFFFFFF last-otr? select
		color, slide-frame dup me? IF .rbubble ELSE .lbubble THEN
		"bubble" name!
		{{
		    new-msg-par
		}}v box[] dup to msg-vbox "msg-vbox" name!
		me? >bubble-border
	    }}z box[] "msg-zbox" name!
	    glue*ll }}glue
	    me? IF  swap rot  THEN
	}}h box[] "msgs-box" name! msgs-box .child+
	blackish
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
	glue*l $FFCCCCFF color, slide-frame dup .button1
	string [: ."  GPS: " .coords ;] $tmp }}text 25%b
    }}z "gps" name! msg-box .child+
; wmsg-class to msg:coord
:noname { d: pk -- o }
    {{
	pk key|
	2dup 0 .pk@ key| str=
	last-otr? IF  my-signal-otr# other-signal-otr#
	ELSE  my-signal# other-signal#  THEN  rot select color,
	glue*l slide-frame dup .button1 40%b >r
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
:noname { sig u' addr u -- }
    u' 64'+ u =  u sigsize# = and IF
	last# >r last# $@ ?msg-log
	addr u startdate@ 64dup date>i >r 64#1 64+ date>i' r>
	\ 2dup = IF  ."  [otrified] "  addr u startdate@ .ticks  THEN
	U+DO
	    I last# cell+ $[]@
	    2dup dup sigpksize# - /string key| msg:id$ str= IF
		dup u - /string addr u str= IF
		    I [: ."  [OTRifying] #" u. forth:cr ;] do-debug
		    I [: ."  OTRify #" u. ;] $tmp
		    \italic }}text 25%bv \regular light-blue text-color!
		    "otrify" name! msg-box .child+
		    sig u' I last# cell+ $[]@ replace-sig
		    \ !!Schedule message saving!!
		ELSE
		    I [: ."  [OTRified] #" u. forth:cr ;] do-debug
		THEN
	    ELSE
		I [: ."  [OTRifignore] #" u. forth:cr ;] do-debug
		2drop
	    THEN
	LOOP
	r> to last#
    THEN ; msg-class is msg:otrify

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
    msgs-box >o [: +sync +resize ;] vp-needed vp-bottom
    +sync +resize o> ;
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
    msgs-box >o [: +sync +resize ;] vp-needed vp-bottom
    +sync +resize o>  ;
' msg-wredisplay wmsg-class is msg:redisplay

[IFDEF] android also android [THEN]

: chat-edit-enter ( o:edit-w -- )
    text$ dup IF  do-chat-cmd? 0= IF  avalanche-text  THEN
    ELSE  2drop  THEN
    64#-1 line-date 64!  $lastline $free ;

\ +db click( \ )
\ +db gui( \ )

{{ $80FFFFFF color, pres-frame
    {{
	{{
	    glue*l $000000FF color, slide-frame dup .button1
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
	    {{ glue*lll $FFFFFFFF color, font-size# 40% f* }}frame dup .button3
		{{ \normal \regular blackish "" }}edit 40%b dup to chat-edit glue*l }}glue
		    glue*lll }}glue
		}}h box[]
	    }}z chat-edit [: edit-w .chat-edit-enter drop nip 0 tuck false ;] edit[]
	    >o act >o [: connection .chat-next-line ;] is edit-next-line o> o o>
	    >o act >o [: connection .chat-prev-line ;] is edit-prev-line o> o o>
	    {{
		glue*l $80FF80FF color, font-size# 40% f* }}frame dup .button2
		!i18n l" Send" }}text' !lit 40%b
		[: data >o chat-edit-enter "" to text$ o>
		    chat-edit engage ;] chat-edit click[]
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
    1config  !widgets
    get-order n>r ['] /chat >body 1 set-order
    ['] widgets-loop catch
    nr> set-order throw ;

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

: ??lang ( addr u -- )
    ['] lang >body find-name-in ?dup-IF  execute  THEN ;

s" LANG" getenv '_' $split 2swap ??lang '.' $split ??lang ??lang

\ lsids .lsids

[IFDEF] load-cov  load-cov [THEN]

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
