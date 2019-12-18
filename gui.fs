\ net2o GUI

\ Copyright © 2018-2019   Bernd Paysan

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
#64 Value lines/diag
: update-gsize# ( -- )
    screen-pwh dup * swap dup * + s>f fsqrt
    screen-diag default-diag f/ .5e f** lines/diag fm* f/
    m2c:scale% f@ f* fround to font-size#
    font-size# 133% f* fround to baseline#
    font-size# 32e f/ to pixelsize# ;

require minos2/md-viewer.fs

update-gsize#

0.4e to slide-time%

\ frames

0 Value pw-frame
0 Value id-frame
0 Value chat-frame
0 Value post-frame
0 Value n2o-frame

\ password screen

0 Value pw-err
0 Value pw-num
0 Value phrase-unlock
0 Value create-new-id
0 Value phrase-first
0 Value phrase-again
0 Value plus-login
0 Value minus-login
0 Value nick-edit

: err-fade ( r addr -- )
    1e fover [ pi f2* ] Fliteral f* fcos 1e f+ f2/ f-
    2 tries# @ lshift s>f f* fdup 1e f> IF fdrop 1e ELSE +sync +resize THEN
    .fade fdrop ;

glue new Constant glue-sleft
glue new Constant glue-sright

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
0 Value pw-field
0 Value nick-field
0 Value nick-pw
0 Value pw-back

Variable nick$

: nick-done ( max span addr pos -- max span addr pos flag )
    over 3 pick nick$ $!
    0e pw-field [: data .engage fdrop ;] >animate \ engage delayed
    create-new-id /hflip
    phrase-first /flop +lang
    1 to nick-pw  true ;

: clear-edit ( max span addr pos -- max 0 addr 0 true )
    drop nip 0 tuck true ;

: do-shake ( max span addr pos -- max span addr pos flag )
    keys sec[]free
    clear-edit invert
    1e o ['] shake-lr >animate
    1 tries# @ lshift s>f f2/ pw-err ['] err-fade >animate ;

: right-phrase ( max span addr pos -- max span addr pos flag )
    \ ." Right passphrase" cr
    0 >o 0 secret-key init-client >raw-key
    read-chatgroups announce-me
    o>
    show-nicks clear-edit ;

: pw-done ( max span addr pos -- max span addr pos flag )
    case nick-pw
	1 of
	    1 +to nick-pw
	    over 3 pick >passphrase +key
	    create-new-id /hflip
	    phrase-first /hflip
	    phrase-again /flop
	    clear-edit invert
	endof
	2 of
	    over 3 pick >passphrase lastkey@ str= IF
		\ ." Create nick " nick$ $. ."  with passphrase (hashed) " lastkey@ 85type cr
		gen-keys-dir nick$ $@ 0 .new-key,
		right-phrase
	    ELSE
		1 to nick-pw
		phrase-first /flop
		phrase-again /hflip
		1 tries# ! do-shake
	    THEN
	endof
	err-fade? IF  false  EXIT  THEN
	drop over 3 pick >passphrase +key
	read-keys secret-keys# 0= IF
	    \ ." Wrong passphrase" cr
	    1 tries# +! tries# @ 0 <# #s #> pw-num >o to text$ o>
	    do-shake
	ELSE
	    right-phrase
	THEN  0
    endcase  +lang +resize ;

: 20%bt ( o -- o ) >o font-size# 20% f* to bordert o o> ;
: 25%b ( o -- o ) >o font-size# 25% f* to border o o> ;
: 25%bv ( o -- o ) >o font-size# 25% f* fdup to border fnegate to borderv o o> ;
: 40%b ( o -- o ) >o font-size# 40% f* to border o o> ;

\ password frame

tex: net2o-logo

$FF0040FF text-color, FValue pw-num-col#
$666666FF text-color, FValue pw-text-col#
$000000FF text-color, FValue show-sign-color#
$FFCCCCFF $44FF44FF fade-color, FValue pw-bg-col#
$0000BFFF new-color, FValue dark-blue#
$0000FF08 new-color, FValue chbs-col#
$FFFFFFFF new-color, FValue login-bg-col#
$FF000000 $FF0000FF fade-color, FValue pw-err-col#
$000000FF dup text-emoji-color: black-emoji
$000000FF new-color, FValue otr-col#
$FFFFFFFF new-color, FValue chat-col#
$80FFFFFF new-color, FValue chat-bg-col#
$FFFFFFFF new-color, FValue posting-bg-col#

: entropy-colorize ( -- )
    prev-text$ erase  addr prev-text$ $free
    edit-w .text$ passphrase-entropy 1e fmin pw-bg-col# f+
    pw-back >o to w-color o> ;
: size-limit ( -- )
    edit-w .text$ nip #800 u> IF
	prev-text$ edit-w >o to text$ o>
    THEN ;

glue new Constant glue*lll±
glue*lll± >o 1Mglue fnip 1000e fswap hglue-c glue! 0glue fnip 1filll fswap dglue-c glue! 1glue vglue-c glue! o>

glue new Constant glue*shrink
glue*shrink >o 0e 1filll 0e hglue-c glue! 1glue dglue-c glue! 1glue vglue-c glue! o>

' dark-blue >body f@

{{  login-bg-col# pres-frame
    dark-blue# ' dark-blue >body f!
    {{
	{{ glue*lll± }}glue }}v
	' net2o-logo "doc/net2o.png" 0.666e }}image-file Constant net2o-glue /center
	!i18n l" net2o GUI" /title
	!lit
	\footnote cbl dark-blue net2o-version }}text /center
	!i18n l" Copyright © 2010–2019 Bernd Paysan" }}text' /center !lit
	{{
	    {{
		glue*ll }}glue
		blackish \large "👤" }}text \normal
		{{
		    glue*l pw-bg-col# font-size# f2/ f2/ }}frame dup .button3
		    {{
			nt
			blackish \bold
			"nick" }}edit 25%b dup to nick-field
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
		dup to pw-back
		\mono \normal
		{{ chbs-col# to x-color "Correct Horse Battery Staple" }}text 25%b
		glue*l }}h
		{{
		    glue-sright }}glue
		    glue*l }}glue \bold
		    l" wrong passphrase!" pw-err-col#
		    to x-color }}i18n-text \regular
		    25%b dup to pw-err
		    glue*l }}glue
		    glue-sleft }}glue
		}}h
		blackish
		{{
		    {{
			pw-text-col# to x-color
			"" }}pw dup to pw-field
			25%b >o config:passmode# @ to pw-mode o o>
			glue*lll }}glue
		    }}h
		    pw-field ' pw-done edit[] ' entropy-colorize filter[]
		    \normal \sans white# to x-color
		    "" }}text blackish
		    dup value show-pw-sign
		    \regular
		    : pw-show/hide ( flag -- )
			dup IF  ""  ELSE  ""  THEN  show-pw-sign >o to text$ o>
			2 config:passmode# @ 1 min rot select pw-field >o to pw-mode o>
			pw-field engage +sync ;
		    ' pw-show/hide config:passmode# @ 1 > toggle[]
		    \normal
		}}h box[]
	    }}z box[] bx-tab
	    {{
		\large
		"🔴" }}text \normal  >o font-size# 10% f* to raise o o>
		"➕" }}text /center dup to plus-login
		"➖" }}text /center dup to minus-login /vflip
		\large
		: id-show-hide ( flag -- )
		    IF
			phrase-unlock /hflip
			create-new-id /flop
			phrase-first /hflip
			phrase-again /hflip
			plus-login /flip
			minus-login /flop
			nick-edit /flop
			[ x-baseline ] FLiteral nick-edit >o
			fdup gap% f* to gap to baseline o>
			"nick" nick-field engage-edit
			1 to nick-pw
		    ELSE
			phrase-unlock /flop
			create-new-id /hflip
			phrase-first /hflip
			phrase-again /hflip
			plus-login /flop
			minus-login /flip
			nick-edit /vflip
			0e nick-edit >o to baseline o>
			pw-field engage
			0 to nick-pw
		    THEN +resize +lang ;
		\normal
	    }}z ' id-show-hide false toggle[] dup Value id-toggler
	    glue-sright }}glue
	    glue*lll }}glue
	}}h box[] \skip >bl
	\ Advices, context sensitive
	{{  \small dark-blue !i18n
	    l" Enter passphrase to unlock" }}text' /center dup to phrase-unlock
	    l" Create new ID" }}text' /center dup to create-new-id /hflip
	    l" Enter new passphrase" }}text' /center dup to phrase-first /hflip
	    l" Enter new passphrase again" }}text' /center dup to phrase-again /hflip
	    !lit
	}}z box[] /center >bl
	{{ glue*lll }}glue }}v
    }}v box[]
}}z box[] to pw-frame

' dark-blue >body f!

\ id frame

0 Value mykey-box
0 Value groups-box
0 Value nicks-box
0 value peers-box
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
$33EE33FF new-color, sf, \ myself is pretty green
$BBDD66FF new-color, sf, \ manually imported is green, too
$55DD55FF new-color, sf, \ scanned is more green
$CCEE55FF new-color, sf, \ seen in chat is more yellow
$EECC55FF new-color, sf, \ imported from DHT is pretty yellow
$FF8844FF new-color, sf, \ invited is very yellow
$FF6600FF new-color, sf, \ provisional is very orange
$FF0000FF new-color, sf, \ untrusted is last
Create imports#rgb-fg
$003300FF text-color, sf,
$000000FF text-color, sf,
$000000FF text-color, sf,
$000000FF text-color, sf,
$0000FFFF text-color, sf,
$0000FFFF text-color, sf,
$0000FFFF text-color, sf,
$00FFFFFF text-color, sf,

\ more colors

$88FF88FF new-color: my-signal
$CCFFCCFF new-color: other-signal
$CC00CCFF new-color: my-signal-otr
$880088FF new-color: other-signal-otr
$4444CCFF text-color: link-blue
$44CC44FF text-color: re-green
$CC4444FF text-color: obj-red
$00BFFFFF text-color: light-blue
$44FF44FF text-color: greenish
$33883366 new-color: day-color
$88333366 new-color: hour-color
$FFFFFFFF text-color: realwhite
$FFFFFFFF new-color: edit-bg
$808080C0 new-color: log-bg
$80FF80FF new-color: send-color
$00FF0020 new-color: pet-color
$FFFF80FF new-color, fvalue users-color#
$FFCCCCFF new-color, fvalue gps-color#
$000077FF new-color, fvalue chain-color#
$FF000000 $FF0000FF fade-color: show-error-color
$338833FF text-color: lock-color
$883333FF text-color: lockout-color
$FFAA44FF text-color, fvalue perm-color#

: nick[] ( box o:nick -- box )
    [: data >o ." clicked on " ke-nick $. cr o> ;] o click[] ;

Hash: avatar#

glue new Constant glue*avatar
glue*avatar >o pixelsize# 64 fm* 0e 0g glue-dup hglue-c glue! vglue-c glue! 0glue dglue-c glue! o>
: wh-glue! ( w h -- )
    pixelsize# fm* 0e 0g vglue-c glue!
    pixelsize# fm* 0e 0g hglue-c glue! ;
: glue*thumb ( w h -- o )
    glue new >o wh-glue! 0glue dglue-c glue! o o> ;

: read-avatar ( addr u -- addr' u' )
    ?read-enc-hashed mem>thumb atlas-region ;
Variable user-avatar#
Variable dummy-thumb#
Variable user.png$
Variable thumb.png$
: ]path ( addr u -- )
    file>fpath ]] SLiteral [[ ] ;
: read-user.png ( -- )
    [ "doc/user.png" ]path user.png$ $slurp-file ;
: read-thumb.png ( -- )
    [ "minos2/thumb.png" ]path thumb.png$ $slurp-file ;
: user-avatar ( -- addr )
    user-avatar# @ 0= IF
	read-user.png user.png$ $@ mem>thumb atlas-region
	user-avatar# $!
    THEN   user-avatar# $@ drop ;
: read-dummy ( -- addr u )
    read-thumb.png thumb.png$ $@ mem>thumb atlas-region ;
: dummy-thumb ( -- addr )
    dummy-thumb# @ 0= IF
	read-dummy dummy-thumb# $!
    THEN   dummy-thumb# $@ drop ;
: avatar-thumb ( avatar -- )
    glue*avatar swap }}thumb >r {{ r> }}v 40%b ;
: avatar-frame ( addr u -- frame# )
    key| 2dup avatar# #@ nip 0= IF
	2dup read-avatar 2swap avatar# #!
    ELSE  2drop  THEN  last# cell+ $@ drop ;
: show-avatar ( addr u -- o / 0 )
    [: avatar-frame avatar-thumb ;] catch IF  2drop 0  THEN ;

: re-avatar ( last# -- )
    >r r@ $@ read-avatar r> cell+ $@ smove ;
: re-dummy ( -- )
    dummy-thumb# @ 0= ?EXIT \ nobody has a dummy thumb
    read-dummy dummy-thumb# $@ smove ;

:noname defers free-thumbs
    re-dummy avatar# ['] re-avatar #map ; is free-thumbs

event: :>update-avatar ( thumb hash u1 -- )
    avatar-frame swap .childs[] $@ drop @ >o to frame# o>
    ['] +sync peers-box .vp-needed +sync ;
event: :>fetch-avatar { thumb task hash u1 pk u2 -- }
    pk u2 $8 $A pk-connect? IF  +resend +flow-control
	net2o-code expect+slurp $10 blocksize! $A blockalign!
	hash u1 net2o:copy# end-code| net2o:close-all disconnect-me
	<event thumb elit, hash u1 e$, :>update-avatar task event>
    ELSE  2drop  THEN ;

: ?+avatars ( o:key o/0 -- o )
    ?dup-0=-IF
	user-avatar avatar-thumb
	<event dup elit, up@ elit,
	ke-avatar $@ e$, ke-pk $@ e$, :>fetch-avatar
	?query-task event>
    THEN ;

: ?avatar ( addr u -- o / )
    key# #@ IF
	cell+ .ke-avatar $@ dup IF
	    show-avatar ?dup-0=-IF  THEN
	ELSE  2drop  THEN
    ELSE  drop  THEN ;

: show-nick ( o:key -- )
    ke-imports @ [ 1 import#provisional lshift ]L and ?EXIT
    ke-imports @ >im-color# sfloats { ki }
    {{ glue*l imports#rgb-bg ki + sf@ slide-frame dup .button1
	{{
	    {{ \large imports#rgb-fg ki + sf@ to x-color
		ke-avatar $@ dup IF  show-avatar ?+avatars
		ELSE  2drop user-avatar avatar-thumb   THEN
		ke-sk sec@ nip IF  \bold  ELSE  \regular  THEN  \sans
		['] .nick-base $tmp }}text 25%b
		ke-pets[] $[]# IF
		    {{
			x-color glue*l pet-color x-color slide-frame dup .button3 to x-color
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

false Value in-group?

: group[] ( box group -- box )
    [:  in-group? ?EXIT  true to in-group?
	data $@ group-name >o to text$ o>
	data cell+ $@ drop cell+ >o groups:id$ groups:member[] o>
	[: chat-keys $+[]! ;] $[]map
	gui-msgs  <event :>chat-connects ?query-task event>
	next-slide +lang +resize
    ;] swap click[] ;

: show-group ( group-o -- )
    dup { g -- } cell+ $@ drop cell+ >o
    {{ glue*l chat-bg-col# slide-frame dup .button1
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

also [ifdef] android android [then]

tex: vp-title

$F110 Constant 'spinner'
$F012 Constant 'signal'
$F234 Constant 'user-plus'
$F503 Constant 'user-minus'
$F235 Constant 'user-times'

0 Value online-flag

: online-symbol ( -- addr u )
    'signal' 'spinner' online? select ['] xemit $tmp ;
: !online-symbol ( -- )
    online-symbol online-flag >o to text$ o> +sync ;
:noname  true to online? ['] announce-me catch 0= to online?
    !online-symbol ; is addr-changed

: nicks-title ( -- )
    {{ glue*l black# slide-frame dup .button1
	{{
	    {{
		{{
		    {{ \large \bold \sans realwhite
		    l" Nick+Pet" }}i18n-text 25%b glue*l }}glue }}h name-tab
		    {{
			{{ \script \mono \bold l" Pubkey"   }}i18n-text 20%bt glue*l }}glue }}h
			{{ \script \sans \bold l" Key date" }}i18n-text glue*l }}glue }}h
		    }}v pk-tab
		    glue*lll± }}glue
		}}h box[]
	    vp-title glue*lll ['] vp-title }}vp vp[] dup to title-vp
	}}h box[]
    }}z box[] ;

previous

{{ users-color# pres-frame
    {{
	{{
	    nicks-title
	    glue*shrink }}glue
	    \Large
	    s" ❌" $444444FF new-color, }}button-lit /hfix [: -1 data +! ;]
	    [IFDEF] android android:level# [ELSE] level# [THEN] click[]
	}}h box[] /vfix
	{{
	    {{
		{{ glue*l $303000FF new-color, bar-frame
		{{ \script l" My key" }}i18n-text 25%b glue*l }}glue }}h }}z
		{{ }}v box[] dup to mykey-box
		{{ glue*l $300030FF new-color, bar-frame
		{{ \script l" My groups" }}i18n-text 25%b glue*l }}glue }}h }}z
		{{ }}v box[] dup to groups-box /vflip
		{{ glue*l $003030FF new-color, bar-frame
		{{ \script l" My peers" }}i18n-text 25%b glue*l }}glue }}h }}z
		{{ }}v box[] dup to nicks-box /vflip
		glue*lll }}glue
	    tex: vp-nicks vp-nicks glue*lll ' vp-nicks }}vp vp[] dup to peers-box
	    $444444FF new-color, to slider-color
	    $CCCCCCFF new-color, to slider-fgcolor
	    font-size# 33% f* to slider-border
	dup font-size# 66% f* fdup vslider }}h box[]
    }}v box[]
}}z box[] to id-frame

: show-nicks ( -- )
    dummy-thumb drop
    fill-nicks fill-groups !online-symbol
    next-slide +lang +resize  peers-box engage
    peers-box 0.01e [: .vp-top fdrop title-vp .vp-top +sync +resize ;] >animate ;

\ messages

msg-class class
end-class wmsg-class

Variable last-bubble-pk
0 Value last-otr?
0 Value last-bubble
64#0 64Value last-tick
64#-1 64Value end-tick
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
	    x-color { f: xc }
	    glue*l day-color x-color slide-frame dup .button1
	    xc to x-color
	    \bold day ['] .day $tmp }}text 25%b \regular
	}}z /center msgs-box .child+
    THEN  day to last-day
    24 fm* fsplit { hour } hour last-hour <>
    60 fm* fsplit { minute } minute 10 / last-minute 10 / <> or
    IF
	{{
	    x-color { f: xc }
	    glue*l hour-color x-color slide-frame dup .button1
	    xc to x-color
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

: .posting ( addr u -- )
    2dup keysize /string
    2dup printable? IF  '[' emit type '@' emit
    ELSE  ." #["  85type ." /@"  THEN
    key| .key-id? ;

hash: chain-tags#

scope{ dvcs
dvcs-log-class class
end-class posting-log-class

Variable like-char

:noname ( addr u -- )
    + sigpksize# - [ keysize $10 + ]L dvcs-log:id$ $!
    like-char off
; posting-log-class is msg:start
:noname ( xchar -- )  like-char ! ; posting-log-class is msg:like
' 2drop posting-log-class is msg:tag
' 2drop posting-log-class is msg:id
' 2drop posting-log-class is msg:text
' 2drop posting-log-class is msg:action
:noname ( addr u -- )
    like-char @ 0= IF  2drop  EXIT  THEN
    8 umin { | w^ id+like }
    like-char @ dvcs-log:id$ $@ [: forth:type forth:xemit ;] id+like $exec
    id+like cell
    2over chain-tags# #@ d0= IF
	2swap chain-tags# #!
    ELSE
	2nip last# cell+ $+!
    THEN
; posting-log-class is msg:chain
:noname ( addr u -- )
    [: dvcs-log:id$ $. forth:type ;] dvcs-log:urls[] dup $[]# swap $[] $exec
; posting-log-class is msg:url

: new-posting-log ( -- o )
    posting-log-class new >o msg-table @ token-table ! o o> ;
}scope

0 Value posting-vp

{{
    posting-bg-col# pres-frame
    {{
	{{
	    glue*l $000000FF new-color, slide-frame dup .button1
	    {{
		\large realwhite
		"⬅" }}text 40%b [: prev-slide ;] over click[]
		!i18n l" Post" }}text' !lit 40%b
		glue*l }}glue
	    }}h box[]
	}}z box[]
	{{
	    {{
		glue*ll }}glue
		tex: vp-md
	    glue*l ' vp-md }}vp dup to posting-vp
	    >o "posting" to name$ font-size# dpy-w @ dpy-h @ > [IF]  dpy-w @ 25% fm* fover f- [ELSE] 0e [THEN] fdup fnegate to borderv f+ to border o o>
	dup font-size# 66% f* fdup vslider }}h box[]
	>o "posting-slider" to name$ o o>
    }}v box[]
    >o "posting-vbox" to name$ o o>
}}z box[]
>o "posting-zbox" to name$ o o>
to post-frame

hash: buckets#

: #!+ ( addr u hash -- ) >r
    2dup r@ #@ IF
	1 swap +!  rdrop 2drop
    ELSE
	drop 1 { w^ one }
	one cell 2swap r> #!
    THEN ;

Variable emojis$ "👍👎🤣😍😘😛🤔😭😡😱🔃" emojis$ $! \ list need to be bigger

: chain-string ( addr u -- addr' u' )
    buckets# #frees
    bounds U+DO
	I $@ [ keysize 2 64s + ]L /string buckets# #!+
    cell +LOOP
    emojis$ $@ bounds DO
	I dup I' over - x-size 2dup buckets# #@
	IF    @ >r tuck type r> .
	ELSE  drop nip  THEN
    +LOOP ;
: display-title { d: prj | ki -- }
    prj key>o ?dup-IF  .ke-imports @ >im-color# sfloats to ki  THEN
    {{
	glue*l imports#rgb-bg ki + sf@ slide-frame dup .button1
	{{
	    prj key| ?avatar
	    \large imports#rgb-fg ki + sf@ to x-color
	    prj key| ['] .key-id? $tmp }}text 25%b
	    glue*ll }}glue
	    \small prj drop keysize + le-64@ [: .ticks space ;] $tmp }}text 25%b
	    \normal
	    prj drop keysize + 8 chain-tags# #@
	    ['] chain-string $tmp }}text 25%b blackish
	}}h box[]
    }}z box[] posting-vp .child+ ;

: display-file { d: prj -- }
    prj display-title
    prj [ keysize $10 + ]L safe/string
    2dup "file:" string-prefix? IF
	0 to md-box
	5 /string [: ." ~+/" type ;] $tmp markdown-parse
	md-box posting-vp .child+
	dpy-w @ dpy-h @ > IF  dpy-w @ 50% fm*
	ELSE  dpy-w @ s>f font-size# f2* f-  THEN
	p-format
    ELSE  2drop  THEN ;
: display-posting ( addr u -- )
    posting-vp >o dispose-childs ( free-thumbs ) 0 to active-w o>
    project:branch$ $@ { d: branch }
    dvcs:new-posting-log >o
    >group msg-log@ 2dup { log u }
    bounds ?DO
	I $@ msg:display \ this will only set the URLs
    cell +LOOP
    glue*lll }}glue posting-vp dup .act 0= IF  vp[]  THEN  .child+
    log free
    dvcs-log:urls[] ['] display-file $[]map
    dvcs:dispose-dvcs-log o> ;
: .posting-log ( -- )
    album-imgs[] $[]free
    dvcs:new-dvcs >o  config>dvcs
    project:project$ $@ @/ 2drop 2dup load-msg
    display-posting
    dvcs:dispose-dvcs o> ;
: album>path ( -- )
    >dir
    album-imgs[] $@ bounds U+DO
	"/" I 0 $ins  dir@ I 0 $ins
    cell +LOOP
    dir> ;
: open-posting { d: prj -- }
    >dir "posts" ~net2o-cache/  chat-keys $[]free
    ." open " prj .posting cr
    prj 2dup keysize /string [: type '@' emit key| .key-id? ;] $tmp nick>chat
    handle-clone
    prj keysize /string set-dir throw
    .posting-log next-slide +lang +resize album>path
    posting-vp 0.01e [: >o vp-top box-flags box-touched# invert and to box-flags o>
	fdrop +sync +resize ;] >animate
    dir> ;

:noname ( -- )
    glue*ll }}glue msg-box .child+
    dpy-w @ 80% fm* msg-par .par-split
    {{ msg-par unbox }} cbl
    dup >r 0 ?DO  I pick box[] >bl "unboxed" name! drop  LOOP  r>
    msg-vbox .+childs
; wmsg-class is msg:end
0 Value nobody-online-text \ nobody is online warning
:noname 2e nobody-online-text [: f2* sin-t .fade +sync ;] >animate
; wmsg-class is msg:.nobody

log-mask off \ in GUI mode, default is no marking

: +log#-date-token ( log-mask -- o ) >r
    {{
	[: '#' emit log# 0 u.r       ;] $tmp }}text /left
	>r {{ r> }}v 25%bv "log#"  name! r@ log#num  and 0= IF  /flip  THEN
	[: '<' emit last-tick .ticks ;] $tmp }}text /left
	>r {{ r> }}v 25%bv
	r@ log#perm and IF  "perm#"  ELSE  "date#"  THEN name!
	r@ log#date and 0= IF  /flip  THEN
	[: '>' emit end-tick  .ticks ;] $tmp }}text /left
	>r {{ r> }}v 25%bv "end#"  name! r> log#end  and 0= IF  /flip  THEN
    }}v box[] ;

: ?flip ( flag -- ) IF  o /flop  ELSE  o /flip  THEN  drop ;
Variable re-indent#
: re-box-run ( -- ) recursive
    gui( re-indent# @ spaces name$ type cr )
    log-mask @ >r
    name$ "log#"  str= IF  r> log#num  and ?flip  EXIT  THEN
    name$ "date#" str= IF  r> log#date and ?flip  EXIT  THEN
    name$ "end#"  str= IF  r> log#end  and ?flip  EXIT  THEN
    rdrop
    hbox vbox zbox o cell- @ tuck = >r tuck = >r = r> r> or or  IF
	1 re-indent# +! ['] re-box-run do-childs
	-1 re-indent# +!
    THEN ;
: re-log#-token ( -- )
    ['] re-box-run msgs-box .do-childs
    [: +resize +sync ;] msgs-box .vp-needed ;
' re-log#-token is update-log

: new-msg-par ( -- )
    {{ }}p "msg-par" name!
    dup .subbox box[] drop box[] cbl >bl
    dup .subbox "msg-box" name!
    to msg-box to msg-par
    \script cbl re-green log-mask @ +log#-date-token msg-box .child+
    \normal cbl ;
:noname { d: pk -- o }
    pk key| to msg:id$  pk startdate@ to msg:timestamp
    pk [: .simple-id ." : " ;] $tmp notify-nick!
    pk key| pkc over str= { me? }
    pk enddate@ otr? { otr }
    pk key| last-bubble-pk $@ str= otr last-otr? = and
    pk startdate@ last-tick 64over to last-tick
    pk enddate@ to end-tick
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
			0 pk key| ?avatar dup IF  nip
			    pk ['] .key-id $tmp 2drop
			ELSE  drop
			    \bold pk ['] .key-id $tmp }}text 25%b
			    >o imports#rgb-fg last-ki >im-color# sfloats + sf@
			    to text-color  o o>
			THEN
			me? IF  swap  THEN
			\regular
		    }}h
		    glue*l imports#rgb-bg last-ki >im-color# sfloats + sf@
		    slide-frame dup .button2
		    swap
		}}z me? 0= IF  chatname-tab  THEN
	    }}v
	    {{
		glue*l last-otr? IF otr-col# ELSE chat-col# THEN
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
	blackish
    THEN
; wmsg-class is msg:start
:noname { d: string -- o }
    link-blue \mono string [: '#' emit type ;] $tmp
    ['] utf8-sanitize $tmp }}text text-color! \sans
    msg-box .child+
; wmsg-class is msg:tag
:noname { d: string -- o }
    text-color!
    string ['] utf8-sanitize $tmp }}text 25%bv
    "text" name! msg-box .child+
; wmsg-class is msg:text
:noname { d: string -- o }
    \italic last-otr? IF light-blue ELSE dark-blue THEN
    string ['] utf8-sanitize $tmp }}text 25%bv \regular
    text-color!
    "action" name! msg-box .child+
; wmsg-class is msg:action
:noname { d: string -- o }
    last-otr? IF light-blue ELSE dark-blue THEN
    string ['] utf8-sanitize $tmp }}text _underline_ 25%bv
    text-color!
    [: data >o text$ o> open-url ;]
    over click[]
    click( ." url: " dup ..parents cr )
    "url" name! msg-box .child+
; wmsg-class is msg:url
:noname ( d: string -- )
    0 .v-dec$ dup IF
	msg-key!  msg-group-o .msg:+lock
	{{
	    glue*l lock-color x-color slide-frame dup .button1
	    greenish l" chat is locked" }}text' 25%bv
	}}z
    ELSE  2drop
	{{
	    glue*l lockout-color x-color slide-frame dup .button1
	    show-error-color 1e +to x-color l" locked out of chat" }}text' 25%bv
	}}z
    THEN "lock" name! msg-box .child+ ; wmsg-class is msg:lock
:noname ( -- o )
	{{
	    glue*l lock-color x-color slide-frame dup .button1
	    blackish l" chat is unlocked" }}text' 25%bv
	}}z msg-box .child+ ; wmsg-class is msg:unlock
:noname { d: string -- o }
    {{
	glue*l gps-color# slide-frame dup .button1
	string [: ."  GPS: " .coords ;] $tmp }}text 25%b
    }}z "gps" name! msg-box .child+
; wmsg-class is msg:coord
:noname { 64^ perm d: pk -- }
    perm [ 1 64s ]L pk msg-group-o .msg:perms# #!
    {{
	glue*l perm-color# slide-frame dup .button1
	{{
	    pk [: '@' emit .key-id ;] $tmp ['] utf8-sanitize $tmp }}text 25%b
	    perm 64@ 64>n ['] .perms $tmp }}text 25%b
	}}h
    }}z msg-box .child+
; wmsg-class is msg:perms
:noname { d: string -- o }
    {{
	glue*l chain-color# slide-frame dup .button1
	string sighash? IF  re-green  ELSE  obj-red  THEN
	log#date log#perm or +log#-date-token
    }}z "chain" name! msg-box .child+
; wmsg-class is msg:chain
:noname { d: pk -- o }
    {{
	x-color { f: xc }
	pk key|
	2dup 0 .pk@ key| str=
	last-otr? IF  IF  my-signal-otr  ELSE  other-signal-otr  THEN
	ELSE  IF  my-signal  ELSE  other-signal  THEN  THEN
	x-color glue*l slide-frame dup .button1 40%b >r
	black# to x-color
	[: '@' emit .key-id ;] $tmp ['] utf8-sanitize $tmp }}text 25%b r> swap
	xc to x-color
    }}z msg-box .child+
; wmsg-class is msg:signal
:noname ( addr u -- )
    re-green [: ." [" 85type ." ]→" ;] $tmp }}text msg-box .child+
    text-color!
; wmsg-class is msg:re
:noname ( addr u -- )
    obj-red [: ." [" 85type ." ]:" ;] $tmp }}text msg-box .child+
    text-color!
; wmsg-class is msg:id
:noname { sig u' addr u -- }
    u' 64'+ u =  u sigsize# = and IF
	addr u startdate@ 64dup date>i >r 64#1 64+ date>i' r>
	\ 2dup = IF  ."  [otrified] "  addr u startdate@ .ticks  THEN
	U+DO
	    I msg-group-o .msg:log[] $[]@
	    2dup dup sigpksize# - /string key| msg:id$ str= IF
		dup u - /string addr u str= IF
		    I [: ."  [OTRifying] #" u. forth:cr ;] do-debug
		    I [: ."  OTRify #" u. ;] $tmp
		    \italic }}text 25%bv \regular light-blue text-color!
		    "otrify" name! msg-box .child+
		    sig u' I msg-group-o .msg:log[] $[]@ replace-sig
		    \ !!Schedule message saving!!
		ELSE
		    I [: ."  [OTRified] #" u. forth:cr ;] do-debug
		THEN
	    ELSE
		I [: ."  [OTRifignore] #" u. forth:cr ;] do-debug
		2drop
	    THEN
	LOOP
    THEN ; wmsg-class is msg:otrify

: >rotate ( addr u -- )
    keysize safe/string IF  c@ to rotate#  ELSE  drop  THEN ;
: >swap ( w h addr u -- w h / h w )
    keysize safe/string IF  c@ 4 and IF  swap  THEN  ELSE  drop  THEN ;

: update-thumb { d: hash object -- }
    hash avatar-frame object >o to frame# hash >rotate
    frame# i.w 2* frame# i.h 2* tile-glue hash >swap .wh-glue!  o>
    [: +sync +resize ;] msgs-box .vp-needed +sync +resize ;

: 40%bv ( o -- o ) >o current-font-size% 40% f* fdup to border
    fnegate f2/ to borderv o o> ;

: ?thumb { d: hash -- o }
    hash ['] avatar-frame catch 0= IF
	>r r@ i.w 2* r@ i.h 2* hash >swap
	glue*thumb r> }}thumb >r hash r@ .>rotate
    ELSE
	128 128 glue*thumb dummy-thumb }}thumb >r
	r@ [n:h update-thumb ;] { w^ xt } xt cell hash key| fetch-finish# #!
	hash key| ?fetch
    THEN  {{ glue*ll }}glue r> }}v 40%bv box[] ;

hash: imgs# \ hash of images

: group.imgs ( addr u -- )
     bounds ?DO
	    I $@ over be-64@ .ticks space
	    1 64s /string 85type cr
	cell +LOOP ;
: .imgs ( -- )
    imgs# [: dup $. ." :" cr cell+ $@ group.imgs ;] #map ;
: +imgs ( addr$ -- )
    [: { w^ string | ts[ 1 64s ] }
	msg:timestamp ts[ be-64!
	ts[ 1 64s type  string $. ;] $tmp
    msg-group$ $@ imgs# #!ins[] ;

: img>group# ( img u -- n )
    msg-group$ $@ imgs# #@ bounds ?DO
	2dup I $@ 1 64s /string str= IF
	    2drop I last# cell+ $@ drop - cell/  unloop  EXIT
	THEN
    cell +LOOP  2drop -1 ;

: >msg-album-viewer ( img u -- )
    img>group# dup 0< IF  drop  EXIT  THEN
    last# cell+ $@ album-imgs[] $!
    album-prepare
    [: 1 64s /string ['] ?read-enc-hashed catch
	IF    2drop thumb.png$ $@
	ELSE  save-mem  THEN ;] is load-img
    4 album-reload
    md-frame album-viewer >o to parent-w o>
    album-viewer md-frame .childs[] >stack
    +sync +resize ;

: album-view[] ( addr u o -- o )
    [: addr data $@ >msg-album-viewer ;]
    2swap $make dup +imgs 64#1 +to msg:timestamp click[] ;

:noname ( addr u type -- )
    obj-red
    case
	msg:image#     of
	    2dup key| ?fetch
	    msg-box .childs[] $[]# ?dup-IF
		rdrop  1- msg-box .childs[] $[] @
		dup .name$ "thumbnail" str= IF
		    album-view[] drop  EXIT  THEN  drop  THEN
	    [: ." img[" 2dup 85type ']' emit ;] $tmp }}text  "image" name!
	    2rdrop album-view[]
	endof
	msg:thumbnail# of  ?thumb  "thumbnail" name!  endof
	msg:patch#     of  [: ." patch["    85type ']' emit
	    ;] $tmp }}text  "patch" name!  endof
	msg:snapshot#  of  [: ." snapshot[" 85type ']' emit
	    ;] $tmp }}text  "snapshot" name!  endof
	msg:message#   of  [: ." message["  85type ']' emit
	    ;] $tmp }}text  "message" name!  endof
	msg:posting#   of  ." posting"
	    rdrop 2dup $make [: addr data $@ open-posting ;] swap 2>r
	    ['] .posting $tmp }}text 2r> click[]  "posting" name!
	endof
    endcase
    msg-box .child+
    text-color!
; wmsg-class is msg:object

in net2o : new-wmsg ( o:connection -- o )
    o wmsg-class new >o  parent!  msg-table @ token-table ! o o> ;
' net2o:new-wmsg is net2o:new-msg

wmsg-class ' new static-a with-allocater Constant wmsg-o
wmsg-o >o msg-table @ token-table ! o>

: vp-softbottom ( o:viewport -- )
    vp-y fnegate act >o o anim-del  set-startxy
    0e to vmotion-dx  to vmotion-dy
    0.25e o ['] vp-scroll >animate o> ;
: re-msg-box ( -- )
    msgs-box >o vp-h { f: old-h } resized
    vp-h old-h f- vp-y f+ 0e fmax to vp-y
    grab-move? 0= IF  vp-softbottom  THEN +sync +resize o> ;

: wmsg-display ( addr u -- )
    msg-tdisplay re-msg-box ;
' wmsg-display wmsg-class is msg:display

#200 Value gui-msgs# \ display last 200 messages
0 Value chat-edit    \ chat edit field
0 Value chat-edit-bg \ chat edit background

: (gui-msgs) ( gaddr u -- )
    reset-time
    64#0 to last-tick  last-bubble-pk $free
    0 to msg-par  0 to msg-box
    msgs-box .dispose-childs
    glue*lll }}glue msgs-box .child+
    load-msg msg-log@
    { log u } u gui-msgs# cells - 0 max { u' }  log u' wmsg-o .?search-lock
    log u u' /string bounds ?DO
	I log - cell/ to log#
	I $@ { d: msgt }
	msgt ['] msg-tdisplay wmsg-o .catch IF
	    <err> ." invalid entry" cr <default> 2drop
	THEN
    cell +LOOP
    log free throw  re-msg-box  chat-edit engage ;

: gui-msgs ( gaddr u -- )
    2dup msg-group$ $! (gui-msgs) ;

: msg-wredisplay ( n -- )
    drop 0 msg-group-o .msg:mode
    [: msg-group$ $@ (gui-msgs) ;] !wrapper
    re-msg-box ;
' msg-wredisplay wmsg-class is msg:redisplay

[IFDEF] android also android [THEN]

: ?chat-otr-status ( o:edit-w -- )
    msg-group-o .msg:?otr
    IF   otr-col#  [ greenish x-color ] Fliteral
    ELSE chat-col# [ blackish x-color ] Fliteral  THEN
    chat-edit    >o to w-color o>
    chat-edit-bg >o to w-color o> ;

: chat-edit-enter ( o:edit-w -- )
    text$ dup IF  do-chat-cmd? 0= IF  avalanche-text
	ELSE  ?chat-otr-status  THEN
    ELSE  2drop  THEN
    64#-1 line-date 64!  $lastline $free ;

\ +db click( \ )
\ +db click-o( \ )
\ +db gui( \ )

{{ chat-bg-col# pres-frame
    {{
	{{
	    glue*l black# slide-frame dup .button1
	    {{
		\large realwhite
		"⬅" }}text 40%b [: in-group? 0= ?EXIT  false to in-group?
		    leave-chats prev-slide ;] over click[]
		!i18n l" " }}text' !lit 40%b
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
	    {{ glue*lll edit-bg x-color font-size# 40% f* }}frame dup .button3
		dup to chat-edit-bg
		show-error-color \normal \regular
		!i18n l" Nobody is online" }}text' !lit
		dup to nobody-online-text /center
		{{ blackish "" }}edit 40%b dup to chat-edit glue*l }}glue
		    glue*lll }}glue
		}}h box[]
	    }}z chat-edit [: edit-w .chat-edit-enter drop nip 0 tuck false ;] edit[] ' size-limit filter[]
	    >o act >o [: connection .chat-next-line ;] is edit-next-line o> o o>
	    >o act >o [: connection .chat-prev-line ;] is edit-prev-line o> o o>
	    {{
		glue*l send-color x-color font-size# 40% f* }}frame dup .button2
		blackish !i18n l" Send" }}text' !lit 40%b
		[: data >o chat-edit-enter "" to text$ o>
		    chat-edit engage ;] chat-edit click[]
	    }}z box[]
	}}h box[]
    }}v box[]
}}z box[] to chat-frame

[IFDEF] android previous [THEN]

\ chat command redirection

User $[]exec-ptr
User $[]exec#

: $[]type ( addr u -- )  $[]exec# @ $[]exec-ptr @ $[]+! ;
: $[]emit ( char -- )    $[]exec# @ $[]exec-ptr @ $[] c$+! ;
: $[]cr   ( -- )         1 $[]exec# +! ;
-1 1 rshift dup 2Constant $[]form

' $[]type ' $[]emit ' $[]cr ' $[]form output: $[]-out

: $[]exec ( ... xt str[] -- ... ) \ gforth
    \G execute @i{xt} with the output of @code{type} etc. redirected to
    \G @i{file-id}.
    dup $[]free
    op-vector @ { oldout } try
	$[]-out  $[]exec-ptr !  $[]exec# off  execute  0
    restore
	oldout op-vector !
    endtry
    throw ;

Variable gui-log[]

: chat-gui-exec ( xt -- )
    gui-log[] $[]exec
    gui-log[] $[]# IF
	{{
	    glue*lll log-bg x-color font-size# 40% f* }}frame dup .button3
	    \normal \mono blackish
	    {{
		gui-log[] [: }}text /left ;] $[]map
	    }}v box[] 25%b \regular
	    {{
		s" ❌" $444444FF new-color, }}text 25%b /right dup { closer }
		glue*ll }}glue
	    }}v box[]
	}}z box[] >r
	closer [: data msgs-box .childs[] del$cell
	    data .dispose-widget
	    re-msg-box ;] r@ click[] drop
	r> msgs-box .child+ re-msg-box
    THEN ;

' chat-gui-exec is chat-cmd-file-execute

\ special modified chat commands for GUI

scope{ /chat
chat-cmds uclass chat-cmd-o
end-class gui-chat-cmds

gui-chat-cmds new Constant gui-chat-cmd-o

gui-chat-cmd-o to chat-cmd-o
' drop is ./otr-info

text-chat-cmd-o to chat-cmd-o
}scope

\ top box

box-actor class
end-class net2o-actor

:noname ( ekey -- )
    case
	k-f5 of  color-theme 0=  IF  anim-end 0.25e o
		[:             fdup f>s to color-theme 0.5e f+ ColorMode! +sync +vpsync ;]
		>animate  THEN   endof
	k-f6 of  color-theme 0<> IF  anim-end 0.25e o
		[: 1e fswap f- fdup f>s to color-theme 0.5e f+ ColorMode! +sync +vpsync ;]
		>animate  THEN   endof
	k-f7 of  >normalscreen   endof
	k-f8 of  >fullscreen     endof
	[ box-actor :: ekeyed ]  EXIT
    endcase ; net2o-actor to ekeyed

: net2o[] ( o -- o )
    >o net2o-actor new !act o o> ;

0 Value invitations
0 Value invitations-list
0 Value invitations-notify
Variable invitation-stack

: invitations-s/h ( flag -- )
    invitations swap  IF  /flop  ELSE  /flip  THEN  drop +resize ;

: add-user ( key-o -- )
    data >o perm%default ke-mask !  "peer" >group-id set-group
    o cell- ke-end over - ke-pk $@ key| key# #! o> save-keys ;
: sub-user ( key-o -- )
    data >o perm%blocked ke-mask !  "blocked" >group-id set-group
    o cell- ke-end over - ke-pk $@ key| key# #! o> save-keys ;

: add-invitation ( addr u -- )
    over >r read-pk2key$ sample-key .clone >o
    o invitation-stack >stack
    {{
	ke-nick $@ }}text
	glue*ll }}glue
	'user-plus' ['] xemit $tmp }}text
	['] add-user o click[]
	'user-minus' ['] xemit $tmp }}text
	['] sub-user o click[]
    }}h box[] 25%b invitations-list .child+
    invitations-notify /flop drop +resize
    o> r> free throw ;

' add-invitation is do-invite

{{
    {{
	glue-left @ }}glue
	pw-frame          dup >slides
	id-frame   /flip  dup >slides
	chat-frame /flip  dup >slides
	post-frame /flip  dup >slides
	glue-right @ }}glue
    }}h box[] \ main slides
    {{
	{{
	    glue*lll }}glue
	    \large
	    {{
		'user-plus' ' xemit $tmp }}text
	    }}h ' invitations-s/h 0 toggle[] /flip dup to invitations-notify
	    {{
		glue*l $444444FF new-color, font-size# 40% f* }}frame dup .button2
		{{
		    realwhite online-symbol }}text 25%b dup to online-flag
		    s" ❌" }}text 25%b [: -1 data +! ;]
		    [IFDEF] android android:level# [ELSE] level# [THEN] click[]
		}}h box[]
	    }}z
	}}h box[] /vfix
	{{
	    glue*lll }}glue
	    {{
		chat-bg-col# pres-frame
		{{
		    \normal blackish
		    !i18n l" Invitations" }}text' /center 25%b
		}}v box[] dup to invitations-list
	    }}z box[]
	}}h box[]
	/flip dup to invitations
	glue*lll }}glue
    }}v box[] \ notifications
}}z net2o[] to n2o-frame

\ top widgets

: !widgets ( -- )
\    1 set-compose-hint
    top-widget .htop-resize
    pw-field engage
    1e ambient% sf! set-uniforms ;

[IFDEF] x11
    : set-net2o-hints ( -- )
	dpy win l" net2o GUI" locale@ x11:XStoreName drop
	{ | net2o-wm-class[ x11:XClassHint ] }
	"net2o-gui\0" drop dup net2o-wm-class[ 2!
	dpy win net2o-wm-class[ x11:XSetClassHint drop ;
[THEN]

: net2o-gui ( -- )
    [IFDEF] set-net2o-hints  set-net2o-hints  [THEN]
    /chat:gui-chat-cmd-o to chat-cmd-o
    n2o-frame to top-widget
    n2o-frame to md-frame
    "PASSPHRASE" getenv 2dup d0= IF  2drop
    ELSE
	>passphrase +key  read-keys
	"PASSPHRASE" getenv erase \ erase passphrase after use!
    THEN
    secret-keys# IF  show-nicks  ELSE
	lacks-key?  IF
	    0e 0 [: drop k-enter id-toggler .act .ekeyed ;] >animate
	THEN
    THEN
    1config  !widgets
    get-order n>r ['] /chat >body 1 set-order
    ['] widgets-loop catch
    /chat:text-chat-cmd-o to chat-cmd-o
    nr> set-order throw ;

' net2o-gui is run-gui

include gui-night.fs

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

\\\
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
