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
require minos2/md-viewer.fs

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
0 Value post-frame

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
	    phrase-first /hflip
	    phrase-again /flop
	    clear-edit invert +lang
	endof
	2 of
	    over 3 pick >passphrase lastkey@ str= IF
		\ ." Create nick " nick$ $. ."  with passphrase (hashed) " lastkey@ 85type cr
		gen-keys-dir nick$ $@ 0 .new-key,
		right-phrase
	    ELSE
		1 to nick-pw
		phrase-first /flop
		phrase-again /hflip +lang
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
    endcase ;

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

{{ login-bg-col# pres-frame
    dark-blue# ' dark-blue >body f!
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
		    {{
			nt
			whitish \bold
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
		    ELSE
			phrase-unlock /flop
			create-new-id /hflip
			phrase-first /hflip
			phrase-again /hflip
			plus-login /flop
			minus-login /flip
			nick-edit /vflip
			0e nick-edit >o to baseline o>
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

\ more colors

$88FF88FF color: my-signal
$CCFFCCFF color: other-signal
$CC00CCFF color: my-signal-otr
$880088FF color: other-signal-otr
$4444CCFF text-color: link-blue
$44CC44FF text-color: re-green
$CC4444FF text-color: obj-red
$00BFFFFF text-color: light-blue
$44FF44FF text-color: greenish
$33883366 color: day-color
$88333366 color: hour-color
$FFFFFFFF text-color: realwhite

: nick[] ( box o:nick -- box )
    [: data >o ." clicked on " ke-nick $. cr o> ;] o click[] ;

Hash: avatar#

glue new Constant glue*avatar
glue*avatar >o pixelsize# 64 fm* 0e 0g glue-dup hglue-c glue! vglue-c glue! 0glue dglue-c glue! o>

: read-avatar ( addr u -- addr' u' )
    ?read-enc-hashed mem>thumb atlas-region ;
: show-avatar ( addr u -- o )
    2dup avatar# #@ nip 0= IF
	2dup read-avatar 2swap avatar# #!
    ELSE  2drop  THEN
    glue*avatar last# cell+ $@ drop }}thumb
    >r {{ r> }}v 40%b ;

: re-avatar ( last# -- )
    >r r@ $@ read-avatar r> cell+ $@ smove ;

:noname defers free-thumbs
    avatar# ['] re-avatar #map ; is free-thumbs

: ?avatar ( addr u -- o / )
    key# #@ IF
	cell+ .ke-avatar $@ dup IF
	    show-avatar
	ELSE  2drop  THEN
    ELSE  drop  THEN ;

: show-nick ( o:key -- )
    ke-imports @ [ 1 import#provisional lshift ]L and ?EXIT
    ke-imports @ >im-color# sfloats { ki }
    {{ glue*l imports#rgb-bg ki + sf@ slide-frame dup .button1
	{{
	    {{ \large imports#rgb-fg ki + sf@ to x-color
		ke-avatar $@ dup IF  show-avatar  ELSE  2drop  THEN
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

false Value in-group?

: group[] ( box group -- box )
    [:  in-group? ?EXIT  true to in-group?
	data $@ group-name >o to text$ o>
	data cell+ $@ drop cell+ >o groups:id$ groups:member[] o>
	[: chat-keys $+[]! ;] $[]map
	gui-msgs  <event :>chat-connects ?query-task event>
	next-slide
    ;] swap click[] ;

: show-group ( last# -- )
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
	    \large s" ❌" $444444FF color, }}button-lit [: -1 data +! ;] level# click[]
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
    peers-box 0.01e [: .vp-top fdrop title-vp .vp-top +sync +resize ;] >animate ;

\ messages

msg-class class
end-class wmsg-class

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
	    glue*l $000000FF color, slide-frame dup .button1
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
	0 to v-box
	5 /string [: ." ~+/" type ;] $tmp markdown-parse
	v-box posting-vp .child+
	dpy-w @ dpy-h @ > IF  dpy-w @ 50% fm*
	ELSE  dpy-w @ s>f font-size# f2* f-  THEN
	p-format
    ELSE  2drop  THEN ;
: display-posting ( addr u -- )
    posting-vp >o dispose-childs  free-thumbs  0 to active-w o>
    project:branch$ $@ { d: branch }
    dvcs:new-posting-log >o
    ?msg-log  last# msg-log@ 2dup { log u }
    bounds ?DO
	I $@ msg:display \ this will only set the URLs
    cell +LOOP
    glue*lll }}glue posting-vp dup .act 0= IF  vp[]  THEN  .child+
    log free
    dvcs-log:urls[] ['] display-file $[]map
    dvcs:dispose-dvcs-log o> ;
: .posting-log ( -- )
    dvcs:new-dvcs >o  config>dvcs
    project:project$ $@ @/ 2drop 2dup load-msg
    display-posting
    dvcs:dispose-dvcs o> ;
: open-posting { d: prj -- }
    >dir "posts" ~net2o-cache/
    ." open " prj .posting cr
    prj 2dup keysize /string [: type '@' emit key| .key-id? ;] $tmp nick>chat
    handle-clone
    prj keysize /string set-dir throw
    .posting-log next-slide
    posting-vp 0.01e [: >o vp-top box-flags box-touched# invert and to box-flags o>
	fdrop +sync +resize ;] >animate
    dir> ;

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
; wmsg-class to msg:signal
:noname ( addr u -- )
    re-green [: ." [" 85type ." ]→" ;] $tmp }}text msg-box .child+
    text-color!
; wmsg-class to msg:re
:noname ( addr u -- )
    obj-red [: ." [" 85type ." ]:" ;] $tmp }}text msg-box .child+
    text-color!
; wmsg-class to msg:id
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
    THEN ; wmsg-class is msg:otrify
:noname ( addr u type -- )
    obj-red
    [: case 0 >r
	    msg:image#     of  ." img["      85type  endof
	    msg:thumbnail# of  ." thumb["    85type  endof
	    msg:patch#     of  ." patch["    85type  endof
	    msg:snapshot#  of  ." snapshot[" 85type  endof
	    msg:message#   of  ." message["  85type  endof
	    msg:posting#   of  ." posting"
		rdrop 2dup [{: d: prj :}H prj open-posting ;] >r
		.posting
	    endof
	endcase ." ]" r> ;] $tmp }}text
    swap ?dup-IF  0 click[]  THEN  msg-box .child+
    text-color!
; wmsg-class is msg:object

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
    64#0 to last-tick  last-bubble-pk $free
    0 to msg-par  0 to msg-box
    msgs-box .dispose-childs
    glue*lll }}glue msgs-box .child+
    2dup load-msg ?msg-log
    last# msg-log@ 2dup { log u }
    dup gui-msgs# cells - 0 max /string bounds ?DO
	I $@ { d: msgt }
	msgt ['] wmsg-display wmsg-o .catch IF
	    <err> ." invalid entry" <default> 2drop
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
		!i18n l" " }}text' !lit 40%b
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
	    {{ glue*lll white# font-size# 40% f* }}frame dup .button3
		{{ \normal \regular blackish "" }}edit 40%b dup to chat-edit glue*l }}glue
		    glue*lll }}glue
		}}h box[]
	    }}z chat-edit [: edit-w .chat-edit-enter drop nip 0 tuck false ;] edit[] ' size-limit filter[]
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
	[ box-actor :: ekeyed ]  EXIT
    endcase ; net2o-actor to ekeyed

: net2o[] ( o -- o )
    >o net2o-actor new !act o o> ;

{{
    glue-left }}glue
    pw-frame          dup >slides
    id-frame   /flip  dup >slides
    chat-frame /flip  dup >slides
    post-frame /flip  dup >slides
    glue-right }}glue
}}h net2o[]
Value n2o-frame

\ top widgets

: !widgets ( -- )
    top-widget .htop-resize
    pw-field engage
    1e ambient% sf! set-uniforms ;

: net2o-gui ( -- )
    [IFDEF] x11
	dpy win l" net2o GUI" locale@ x11:XStoreName drop
    [THEN]
    n2o-frame to top-widget
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
