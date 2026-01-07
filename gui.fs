\ net2o GUI

\ Copyright © 2018-2023   Bernd Paysan

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

\ utf-8 set-encoding

require minos2/widgets.fs

[IFDEF] window-title$
    "net2o GUI" window-title$ $!
[THEN]
[IFDEF] window-app-id$
    "net2o" window-app-id$ $!
[THEN]

Forth definitions also minos

require minos2/font-style.fs
require unix/open-url.fs

gl-init

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
require i18n-date.fs

' update-gsize# is rescaler
rescaler

\ check if dispose damages things

[IFDEF] dispose-check-xxx
    : pollfds-check ( -- )
	pollfds ['] @ catch nip IF
	    ." Dispose check failed! " up@ h. cr
	THEN ;
    : net2o:dispose-check ( -- )
	up@ >r
	net2o-tasks $@ bounds U+DO
	    I @ up! pollfds-check
	cell +LOOP
	r> up!  pollfds-check ;
    ' net2o:dispose-check is dispose-check
[THEN]

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
0 Value too-short-id
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

: pres-frame# ( color# -- o1 o2 ) \ drop $FFFFFFFF
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
0 Value pw-gen
0 Value pw-back
2 Value min-id-len#

Variable nick$

0.001e FConstant engage-delay#

: >engage ( field -- )
    engage-delay# [: 1e f= IF  engage  ELSE  drop  THEN ;] >animate ;
: nick-check? ( -- flag )
    nick$ $@ x-width min-id-len# u< ;
: nick-too-short ( -- )
    create-new-id /hflip drop
    too-short-id /flop drop +lang
    nick-field >engage ;
: phrase-first-show ( -- )
    create-new-id /hflip drop
    too-short-id /hflip drop
    phrase-first /flop drop +lang
    phrase-again /hflip drop ;
: nick-done ( max span addr pos -- max span addr pos flag )
    over 3 pick nick$ $!
    nick-check? IF  nick-too-short  false  EXIT  THEN
    pw-field >engage  phrase-first-show
    1 to nick-pw  true ;
: nick-engaged ( -- )
    nick-pw IF
	create-new-id too-short-id
	nick-check? IF  swap  THEN
	/hflip drop
	/flop drop
    THEN
    phrase-first /hflip drop
    phrase-again /hflip drop
    0 to nick-pw
    +lang ;
: pw-engaged ( -- )
    pw-gen to nick-pw
    nick-field .text$ nick$ $!
    nick-check? IF  nick-too-short  EXIT  THEN
    pw-gen IF  phrase-first-show  THEN
    +lang ;

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

tex: net2o-logo-tex

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
$70eFeFFF new-color, FValue chat-hist-col#
$FFFFFFFF new-color, FValue posting-bg-col#
$444444FF new-color, FValue close-color#
$FF6633FF new-color, FValue recording-color#

: entropy-colorize ( -- )
    prev-text$ erase  addr prev-text$ $free
    edit-w .text$ passphrase-entropy 1e fmin pw-bg-col# f+
    pw-back >o to w-color o> ;
: size-limit ( -- )
    edit-w .text$ nip #800 u> IF
	prev-text$ edit-w >o to text$ o>
    THEN ;
: nick-filter ( -- ) edit-w >o
    0 >r BEGIN  text$ r@ safe/string  WHILE
	    c@ dup bl = over '@' = or swap '#' = or
	    IF  addr text$ r@ 1 $del
		r@ curpos u< +to curpos
	    ELSE  r> 1+ >r  THEN  REPEAT  drop rdrop
    o> ;

glue new Constant glue*lll±
glue*lll± >o 1Mglue fnip 1000e fswap hglue-c glue! 0glue fnip 1filll fswap dglue-c glue! 1glue vglue-c glue! o>

glue new Constant glue*shrink
glue*shrink >o 0e 1filll 0e hglue-c glue! 1glue dglue-c glue! 1glue vglue-c glue! o>

' dark-blue >body f@

{{  login-bg-col# pres-frame#
    dark-blue# ' dark-blue >body f!
    {{
	{{ glue*lll± }}glue }}v
	' net2o-logo-tex "doc/net2o.png" 0.666e }}image-file Constant net2o-glue /center
	!i18n l" net2o GUI" /title
	!lit
	\footnote cbl dark-blue net2o-version }}text /center
	!i18n l" Copyright © 2010–2025 Bernd Paysan" }}text' /center !lit
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
		    ' nick-filter filter[]
		    ' nick-engaged engaged[]
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
		    ' pw-engaged engaged[]
		    \normal \sans white# to x-color
		    "" }}text blackish
		    dup value show-pw-sign
		    \regular
		    : pw-show/hide ( flag -- )
			dup IF  ""  ELSE  ""  THEN  show-pw-sign >o to text$ o>
			2 config:passmode# @ 1 min rot select pw-field >o to pw-mode o>
			pw-field >engage +sync ;
		    ' pw-show/hide config:passmode# @ 1 > toggle[]
		    \normal
		}}h box[]
	    }}z box[] bx-tab
	    {{
		\large
		"🔴" }}text \normal  >o font-size# 10% f* to raise o o>
		"➕️" }}text /center dup to plus-login
		"➖️" }}text /center dup to minus-login /vflip
		\large
		: id-show-hide ( flag -- )
		    IF
			phrase-unlock /hflip drop
			create-new-id /flop drop
			too-short-id /hflip drop
			phrase-first /hflip drop
			phrase-again /hflip drop
			plus-login /flip drop
			minus-login /flop drop
			nick-edit /flop drop
			[ x-baseline ] FLiteral nick-edit >o
			fdup gap% f* to gap to baseline o>
			"nick" nick-field engage-edit
			1 to pw-gen
			1 to nick-pw
		    ELSE
			phrase-unlock /flop drop
			create-new-id /hflip drop
			too-short-id /hflip drop
			phrase-first /hflip drop
			phrase-again /hflip drop
			plus-login /flop drop
			minus-login /flip drop
			nick-edit /vflip drop
			0e nick-edit >o to baseline o>
			pw-field >engage
			0 to pw-gen
			0 to nick-pw
		    THEN +resize +lang ;
		\normal
	    }}z ' id-show-hide false toggle[] dup Value id-toggler
	    glue-sright }}glue
	    glue*lll }}glue
	}}h box[] \skip >bl
	\ Advices, context sensitive
	{{  \small dark-blue !i18n
	    glue*l login-bg-col# font-size# f2/ f2/ }}frame dup .button1
	    {{ l" Enter passphrase to unlock" }}text' }}h dup to phrase-unlock
	    {{ l" Create new ID" }}text' }}h dup to create-new-id /hflip
	    {{ l" ID too short!" }}text' }}h dup to too-short-id /hflip
	    {{ l" Enter new passphrase" }}text' }}h dup to phrase-first /hflip
	    {{ l" Enter new passphrase again" }}text' }}h dup to phrase-again /hflip
	    !lit
	}}z /center 25%b >bl
	[: k-enter pw-frame .act .ekeyed ;] 0 click[]
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
0 Value msgs-title-box

0 Value group-name
0 Value group-members

new-htab tab-glue: name-tab
new-htab tab-glue: pk-tab
new-htab tab-glue: group-tab
new-htab tab-glue: chatname-tab

?: child+ ( o -- ) o over >o to parent-w o> childs[] >stack ;

Create ke-imports#rgb

Create imports#rgb-bg
$33EE33FF new-color, sf, \ myself is pretty green
$BBDD66FF new-color, sf, \ manually imported is green, too
$55DD55FF new-color, sf, \ scanned is more green
$CCEE55FF new-color, sf, \ seen in chat is more yellow
$EECC55FF new-color, sf, \ imported from DHT is pretty yellow
$FF8844FF new-color, sf, \ invited is very yellow
$FFFFFFFF new-color, sf, \ provisional is just white
$FF0000FF new-color, sf, \ untrusted is last
Create imports#rgb-fg
$003300FF text-color, sf,
$000000FF text-color, sf,
$000000FF text-color, sf,
$000000FF text-color, sf,
$0000FFFF text-color, sf,
$0000FFFF text-color, sf,
$000000FF text-color, sf, \ provisional is just white
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
$FF6666FF new-color, fvalue gps-color#
$000077FF new-color, fvalue chain-color#
$FF000000 $FF0000FF fade-color: show-error-color
$00990000 $009900FF fade-color: show-info-color
$338833FF text-color: lock-color
$883333FF text-color: lockout-color
$FFAA44FF text-color, fvalue perm-color#
\ $553300FF text-color: mono-col
$FFFFDDFF text-color: mono-otr-col

: nick[] ( box o:nick -- box )
    [: data >o ." clicked on " ke-nick $. cr o> ;] o click[] ;

Hash: avatar#

glue new Constant glue*avatar
glue*avatar >o pixelsize# 64 fm* 0e 0g glue-dup hglue-c glue! vglue-c glue! 0glue dglue-c glue! o>
: wh-glue! ( w h -- )
    default-imgwh% 15% f* fswap 20% f* fswap wh>glue ;
: glue*thumb ( w h -- o )
    glue new >o wh-glue! 0glue dglue-c glue! o o> ;

: read-avatar ( addr u -- addr' u' )
    ?read-enc-hashed mem>thumb atlas-region +textures ;
Variable user-avatar#
Variable dummy-thumb#
Variable user.png$
Variable thumb.png$
: read-user ( -- region )
    user.png$ $@len 0= IF
	[ "doc/user.png" ]path user.png$ $slurp-file  THEN
    user.png$ $@ mem>thumb atlas-region +textures ;
: read-thumb ( -- )
    thumb.png$ $@len 0= IF
	[ "minos2/thumb.png" ]path thumb.png$ $slurp-file  THEN
    thumb.png$ $@ mem>thumb atlas-region +textures ;
: user-avatar ( -- addr )
    user-avatar# @ 0= IF
	read-user user-avatar# $!
    THEN   user-avatar# $@ drop ;
: dummy-thumb ( -- addr )
    dummy-thumb# @ 0= IF
	read-thumb dummy-thumb# $!
    THEN   dummy-thumb# $@ drop ;
: avatar-thumb ( avatar -- )
    glue*avatar swap }}thumb >r {{ r> }}v 40%b ;
: avatar-frame ( addr u -- frame# )
    key| 2dup avatar# #@ nip 0= IF
	2dup read-avatar 2swap avatar# #!
    ELSE  2drop  THEN  last# cell+ $@ drop ;
: show-avatar ( addr u -- o / 0 )
    [: avatar-frame avatar-thumb ;] catch IF  2drop 0 nothrow  THEN ;

: re-avatar ( last# -- )
    dup >r $@ read-avatar r> cell+ $@ smove ;
: re-dummy ( -- )
    dummy-thumb# @ 0= ?EXIT \ nobody has a dummy thumb
    read-thumb dummy-thumb# $@ smove ;
: re-user ( -- )
    user-avatar# @ 0= ?EXIT \ nobody has a dummy thumb
    read-user user-avatar# $@ smove ;

:is free-thumbs defers free-thumbs
    re-user re-dummy avatar# ['] re-avatar #map
    fetch-finish# #frees ;

: ev-update-avatar ( thumb hash u1 -- )
    avatar-frame swap .childs[] $@ drop @ >o to frame# o>
    ['] +sync peers-box .vp-needed +sync ;
: ev-fetch-avatar [{: thumb task hash u1 pk u2 :}h1
    pk u2 $8 $A pk-connect? IF  +resend +flow-control
	net2o-code expect+slurp $10 blocksize! $A blockalign!
	hash u1 net2o:copy# end-code| net2o:close-all disconnect-me
	thumb hash u1
	[{: thumb hash u1 :}h1 thumb hash u1 ev-update-avatar ;]
	task send-event
    ELSE  2drop  THEN ;] ;

: ?+avatars ( o:key o/0 -- o )
    ?dup-0=-IF
	user-avatar avatar-thumb
	dup up@ ke-avatar $@ ke-pk $@ ev-fetch-avatar
	?query-task send-event
    THEN ;

: ?avatar ( addr u -- o / )
    key# #@ IF
	cell+ .ke-avatar $@ dup IF
	    show-avatar ?dup-0=-IF  THEN
	ELSE  2drop  THEN
    ELSE  drop  THEN ;

: >re-color-edit ( bg-color backframe -- )
    >o to frame-color o> +resize +sync ;
: un-act ( edit-x engage-o -- )
    >o act .dispose  0 to act o> 0 to inside-move? ;

: edit-pen[] ( backframe edit-x update-xt -- xt )
    \G create a closure for an edit pen
    [{: backframe edit-x xt: upd :}d
	data >o edit-x .act IF  \ push back to default
	    edit-x un-act  true upd  transp#
	ELSE                    \ engage+select
	    edit-x o
	    action-of upd backframe edit-x r@ .caller-w
	    [{: xt: upd backframe edit-x click-o :}d
		edit-x click-o [{: edit-x click-o :}h1
		    edit-x click-o >engage un-act +sync ;] up@ send-event
		true upd
		transp# backframe >re-color-edit
		-1 to outselw
		true ;] edit[]
	    ['] nick-filter filter[] drop
	    edit-x >engage  false upd
	    imports#rgb-bg sf@
	THEN  backframe >re-color-edit o> ;] ;

: nick-upd[] ( edit-x o:key -- xt )
    o [{: edit-x o:key :}d
	IF  edit-x .text$ o:key >o
	    ke-nick $@ nick# #@ nip IF
		o:key last# cell+ del$cell
	    ELSE  2drop  THEN
	    ke-nick $! nick! key-sign
	    o>  save-seckeys
	    ['] add-me-id ?query-task send-event
	THEN ;] ;

: show-nick ( o:key -- )
    ke-imports @ [ 1 import#provisional lshift ]L and ?EXIT
    ke-imports @ >im-color# sfloats { ki }
    {{ glue*l imports#rgb-bg ki + sf@ slide-frame dup .button1
	{{
	    {{ \large imports#rgb-fg ki + sf@ to x-color
		ke-avatar $@ dup IF  show-avatar ?+avatars
		ELSE  2drop user-avatar avatar-thumb   THEN
		\sans  ke-sk sec@ nip IF
		    \bold
		    {{
			glue*l }}glue
			{{
			    glue*l transp# font-size# 40% f* }}frame
			    dup .button3 dup { backframe }
			    {{
				['] .nick-base $tmp }}edit 25%b	dup { edit-x }
				l" " black# }}button /hfix
				backframe edit-x dup nick-upd[]
				edit-pen[] edit-x click[]
			    }}h box[]
			}}z box[]
		    }}v box[] >o 0e to baseline 0e to gap o o>
		ELSE
		    \regular
		    ['] .nick-base $tmp }}text 25%b
		THEN
		ke-pets[] $[]# IF
		    {{
			x-color glue*l pet-color x-color slide-frame dup .button3 to x-color
			['] .pet-base $tmp }}text 25%b
		    }}z
		THEN
	    glue*l }}glue }}h name-tab box[]
	    {{
		{{ \sans \script ke-selfsig $@ ['] .sigdates $tmp }}text glue*l }}glue }}h
		{{ \mono \script ke-pk $@ key| ['] 85type $tmp }}text 20%bt glue*l }}glue }}h swap
	    }}v pk-tab
	glue*lll }}glue }}h box[]
    }}z box[]  \regular
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
	    ELSE  2drop  THEN ;] $[]map ;] catch nothrow
    [ ' !!connected!! >body @ ]L = IF  show-connected  THEN ;

: ev-chat-connects  gui-chat-connects
    connection dup [{: con :}h1 con to connection ;]
    swap .wait-task-event ;

false Value in-group?

: group[] ( box group -- box )
    [:  in-group? ?EXIT  true to in-group?
	data $@ group-name >o to text$ o>
	data cell+ $@ drop cell+ >o groups:id$ groups:member[] o>
	[: chat-keys $+[]! ;] $[]map
	gui-msgs  ['] ev-chat-connects ?query-task send-event
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

'' Constant 'spinner'
'' Constant 'signal'
'' Constant 'user-plus'
$F503 Constant 'user-minus'
'' Constant 'user-times'

0 Value online-flag
0 Value select-mode-button
0 Value select-range-button
0 Value deselect-all-button
0 Value clipboard-button

: online-symbol ( -- addr u )
    'signal' 'spinner' online? select ['] xemit $tmp ;
: !online-symbol ( -- )
    online-symbol online-flag >o to text$ o> +sync ;
:is addr-changed  true to online? ['] announce-me catch nothrow 0= to online?
    !online-symbol ;

\ selection range

Variable $selected

: .sel ( -- ) \ net2o-debug
    $selected $@ cell mem+do i ? Loop ;

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

{{ users-color# pres-frame#
    {{
	{{
	    nicks-title
	    glue*shrink }}glue
	    \Large
	    s" ❌️" close-color# }}button-lit /hfix [: -1 data +! ;]
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
    user-avatar drop dummy-thumb drop
    fill-nicks fill-groups !online-symbol
    next-slide +lang +resize  peers-box >engage
    peers-box 0.01e [: .vp-top fdrop title-vp .vp-top +sync +resize ;] >animate ;

\ messages

msg:class class
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

: msgs-box+resize ( -- )
    [ ' +resize >body @ ' +resizeall >body @ or ]L msgs-box .vp-need or! ;
: >msgs-box ( child -- )
    msgs-box .child+ msgs-box+resize ;

: add-dtms ( ticks -- )
    \sans \small blackish
    >fticks fticks>day { day } day last-day <> IF
	{{
	    x-color { f: xc }
	    glue*l day-color x-color slide-frame dup .button1
	    xc to x-color
	    \bold day ['] localized.day $tmp -trailing }}text 25%b \regular
	}}z /center >msgs-box
    THEN  day to last-day
    24 fm* fsplit { hour } hour last-hour <>
    60 fm* fsplit { minute } minute 10 / last-minute 10 / <> or
    IF
	{{
	    x-color { f: xc }
	    glue*l hour-color x-color slide-frame dup .button1
	    xc to x-color
	    60 fm* fsplit minute hour
	    [: #60 * + #60 * + localized.hms .tz ;] $tmp }}text 25%b
	}}z /center >msgs-box
    THEN  hour to last-hour  minute to last-minute
    fdrop \normal ;

: otr? ( tick -- flag )
    64dup 64#-1 64<> ;
: text-color! ( -- ) last-otr? IF  greenish  ELSE  blackish  THEN ;

hash: chain-tags#

scope{ dvcs
dvcs-log-class class
end-class posting-log-class

Variable like-char

posting-log-class :method msg:start ( addr u -- )
    + sigpksize# - [ keysize $10 + ]L dvcs-log:id$ $!
    like-char off  false to msg:silent? ;
posting-log-class :method msg:silent-start ( addr u -- )
    key| to msg:id$ true to msg:silent? ;
posting-log-class :method msg:like ( xchar -- )  like-char ! ;
' 2drop posting-log-class is msg:tag
' 2drop posting-log-class is msg:id
' 2drop posting-log-class is msg:text
posting-log-class :method msg:text+format 2drop drop ;
' 2drop posting-log-class is msg:action
' drop  posting-log-class is msg:vote \ !!FIXME!! what do we need here?
posting-log-class :method msg:chain ( addr u -- )
    like-char @ 0= IF  2drop  EXIT  THEN
    8 umin { | w^ id+like }
    like-char @ dvcs-log:id$ $@ [: forth:type forth:xemit ;] id+like $exec
    id+like cell
    2over chain-tags# #@ d0= IF
	2swap chain-tags# #!
    ELSE
	2nip last# cell+ $+!
    THEN ;
posting-log-class :method msg:url ( addr u -- )
    [: dvcs-log:id$ $. forth:type ;] dvcs-log:urls[] dup $[]# swap $[] $exec ;

: new-posting-log ( -- o )
    posting-log-class new >o msg-table @ token-table ! o o> ;
}scope

0 Value posting-vp
: posting-box+resize ( -- )
    [ ' +resize >body @ ' +resizeall >body @ or ]L posting-vp .vp-need or! ;

{{
    posting-bg-col# pres-frame#
    {{
	{{
	    glue*l $000000FF new-color, slide-frame dup .button1
	    {{
		\large realwhite
		"⬅️" }}text 40%b [: prev-slide ;] over click[]
		!i18n l" Post" }}text' !lit 40%b
		glue*l }}glue
	    }}h box[]
	}}z box[]
	{{
	    {{
		glue*ll }}glue
		tex: vp-md
	    glue*l ' vp-md }}vp dup to posting-vp
	    >o "posting" to name$
	    font-size# dpy-w @ s>f fover maxcols# fm* f- f2/ 0e fmax fover f-
	    fdup fnegate to borderv f+ to border o o> dup Value posting-box
	dup font-size# 66% f* fdup vslider }}h box[]
	>o "posting-slider" to name$ o o>
    }}v box[]
    >o "posting-vbox" to name$ o o>
}}z box[]
>o "posting-zbox" to name$ o o>
to post-frame

:is re-config defers re-config
    posting-box >o
    font-size# dpy-w @ s>f fover maxcols# fm* f- f2/ 0e fmax fover f-
    fdup fnegate to borderv f+ to border o> ;

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
	dpy-w @ dup s>f dpy-h @ >
	IF    font-size# maxcols# fm* fmin
	ELSE  font-size# f2* f-  THEN
	p-format
    ELSE  2drop  THEN ;
: display-posting ( addr u -- )
    posting-vp >o  dispose-childs  free-thumbs  0 to active-w o>
    project:branch$ $@ { d: branch }
    dvcs:new-posting-log >o
    >group msg-log@ 2dup { log u }
    bounds ?DO
	I $@ msg:display \ this will only set the URLs
    cell +LOOP
    glue*lll }}glue posting-vp dup .act 0= IF  vp[]  THEN  .child+
    log free
    dvcs-log:urls[] ['] display-file $[]map
    dvcs:dispose-dvcs-log o>
    posting-box+resize ;
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
    .posting-log next-slide +lang +resize +resizeall album>path
    posting-vp 0.01e [: >o vp-top box-flags box-touched# invert and to box-flags o>
	fdrop +sync +resize +resizeall ;] >animate
    dir> ;

wmsg-class :method msg:end ( -- )
    msg:silent? IF  false to msg:silent?  EXIT  THEN
    glue*ll }}glue msg-box .child+
    msg-par .subbox .name$ { d: subbox-name }
    dpy-w @ 80% fm* msg-par .par-split
    {{ msg-par unbox }} cbl
    dup >r 0 ?DO  I pick box[] >bl subbox-name name! drop  LOOP  r>
    msg-vbox .+childs  enqueue ;
0 Value edit-infobar-text \ nobody is online warning
l" Deactivate Verbatim"
l" Activate Verbatim"
l" On The Record"
l" Off The Record"
l" Nobody is online"
Create edit-infos
' show-error-color , , \ nobody
' show-info-color  , , \ otr on
' show-info-color  , , \ otr off
' show-info-color  , , \ mono
' show-info-color  , , \ normal
: change-edit-info ( n -- )
    2* cells edit-infos + 2@ execute
    edit-infobar-text >o x-color to text-color
    dup to l-text  locale@ to text$ 
    o> +resize
    2e edit-infobar-text [: f2* sin-t .fade +sync ;] >animate ;
wmsg-class :method msg:.nobody ( -- )
    0 change-edit-info ;

'🔲' Constant deselected-text-char
'🔳' Constant selected-text-char

: .$selected ( -- )
    $selected $@ cell mem+DO
	I @ #msg-log@ textmsg-o .msg:display forth:cr
    LOOP ;

: select-click[] ( o text log# -- o )
    swap [{: widget :}h data $selected ~sorted$
	selected-text-char deselected-text-char rot select
	['] xemit $tmp widget >o to text$ o> +sync
	['] .$selected $tmp primary!
    ;] swap click[] ;

Create boxes? hbox , vbox , zbox , here >r
DOES>  [ r> ]L swap U+DO  dup I @ = IF  drop true unloop  EXIT  THEN
  cell +LOOP  drop false ;

: re-run-selection ( -- )
    $selected $@
    [:  name$ "num:" string-prefix? IF
	    dup IF  over @ name$ 4 /string s>number drop =  ELSE  dup  THEN  IF
		selected-text-char ['] xemit $tmp to text$ +sync
		cell /string
	    ELSE
		deselected-text-char ['] xemit $tmp to text$ +sync
	    THEN
	THEN
	o cell- @ boxes?  IF  ['] recurse do-childs  THEN
    ;] ['] do-childs msgs-box .vp-needed 2drop +sync 
    ['] .$selected $tmp primary! ;

: select-range ( -- ) { | w^ added }
    $selected $@len 2 cells u< ?EXIT
    true $selected $@ cell- cell mem+DO
	IF
	    I 2@ - 1 u> IF
		I 2@ 1+ U+DO  I added >stack  LOOP
	    THEN
	    false
	ELSE
	    I 2@ - 1 u>
	THEN
    LOOP  drop
    added $@ cell mem+DO
	I @ $selected +sorted$
    LOOP
    added $free
    re-run-selection ;

: deselect-all ( -- )
    $selected $free  re-run-selection ;

: <log#>! ( o addr u -- o )
    log# 0 <# #s 2swap holds #> name! ;

: +log#-date-token ( log-mask -- o ) >r
    {{
	\sans
	{{ "🔲" }}text dup { textwidget } "num:" <log#>!
	}}v 25%bv "log:select" name! r@ log:select and 0= IF  /flip  THEN
	\script
	{{
	    [: '#' emit log# 0 u.r       ;] $tmp }}text /left
	    >r {{ r> }}v 25%bv "log:num"  name! r@ log:num  and 0= IF  /flip  THEN
	    [: '<' emit last-tick .ticks ;] $tmp }}text /left
	    >r {{ r> }}v 25%bv
	    r@ log:perm and IF  "log:perm"  ELSE  "log:date"  THEN name!
	    r@ log:date and 0= IF  /flip  THEN
	    [: '>' emit end-tick  .ticks ;] $tmp }}text /left
	    >r {{ r> }}v 25%bv "log:end"  name! r> log:end  and 0= IF  /flip  THEN
	}}v
    }}h textwidget log# select-click[]
    \normal ;

: ?flip ( flag -- ) IF  o /flop  ELSE  o /flip  THEN  drop ;
Variable re-indent#
User search-results
Variable chain-pars
0 Value vote-box
UValue search-result

: (search-log#) ( string-post string-pre -- string-pre string-post )
    name$ 2over string-prefix? IF
	2over 2over nip name$ rot /string str= IF  o search-results >stack  THEN
	EXIT \ don't recurse if the prefix matches
    THEN
    o cell- @ boxes?  IF  ['] recurse do-childs  THEN
\    o cell- @ parbox =  IF  ['] recurse subbox .do-childs  THEN
;
: search-par# ( n -- )
    s>d tuck <# #s rot sign #> "par:"
    ['] (search-log#) msgs-box .do-childs 2drop 2drop
    0 search-results !@ chain-pars !@ sp@ $free drop ;
: search-vote# ( xc -- )
    search-results $free
    <# xhold #0. #> "vote:"
    chain-pars $@ bounds U+DO  I @ .(search-log#)  cell +LOOP  2drop 2drop
    search-results get-stack 0 ?DO  to vote-box  LOOP ;

: re-box-run ( -- )
    gui( re-indent# @ spaces name$ type cr )
    logmask# @ >r
    name$ "log:" string-prefix? IF
	name$ 4 /string over c@ '~' = IF  1 /string r> invert >r  THEN
	['] log >wordlist find-name-in
	?dup-IF  name>interpret execute r> and ?flip  EXIT  THEN  THEN
    rdrop
    o cell- @ boxes?  IF
	1 re-indent# +! ['] recurse do-childs
	-1 re-indent# +!
    THEN ;
: re-log#-token ( -- )
    ['] re-box-run msgs-box .do-childs
    ['] re-box-run msgs-title-box .do-childs
    msgs-box+resize  [: +resize +sync ;] msgs-box .vp-needed ;
' re-log#-token is update-log

: msg-start-par ( -- )
    {{ }}p
    dup .subbox box[] "par:" <log#>! to msg-box
    box[] cbl >bl to msg-par ;
: new-msg-par ( -- )
    msg-start-par
    cbl re-green logmask# @ +log#-date-token msg-box .child+
    cbl ;
wmsg-class :method msg:start { d: pk -- o }
    false to msg:silent?
    pk key| to msg:id$  pk startdate@ to msg:timestamp
    pk [: .simple-id ." : " ;] $tmp notify-nick!
    pk key| pkc over str= { me? }
    pk enddate@ 64dup to end-tick 64dup >notify-otr? otr? { otr }
    pk key| last-bubble-pk $@ str= otr last-otr? = and
    pk startdate@ last-tick 64over to last-tick
    64- delta-bubble 64u< and
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
	}}h box[] "msgs-box" name! >msgs-box
	blackish
    THEN ;
wmsg-class :method msg:silent-start ( addr u -- )
    key| to msg:id$ true to msg:silent? ;
wmsg-class :method msg:tag dup 0= IF  2drop EXIT  THEN { d: string -- o }
    link-blue \mono string [: '#' emit type ;] $tmp
    ['] utf8-sanitize $tmp }}text text-color! \sans
    msg-box .child+ ;
wmsg-class :method msg:text dup 0= IF  2drop EXIT  THEN { d: string -- o }
    text-color!
    string ['] utf8-sanitize $tmp }}text 25%bv
    "text" name! msg-box .child+ ;
: mono-col? ( -- )
    msg-group-o .msg:?otr  IF  mono-otr-col  ELSE  mono-col  THEN ;
wmsg-class :method msg:text+format over 0= IF  drop 2drop EXIT  THEN { d: string format -- o }
    text-color!
    case  format msg:#bold msg:#italic or and
	msg:#bold  of  \bold  endof
	msg:#italic  of  \italic  endof
	msg:#bold msg:#italic or  of \bold-italic  endof
	\regular
    endcase
    format msg:#mono and  IF  \mono mono-col?  THEN
    string ['] utf8-sanitize $tmp }}text 25%bv
    format msg:#underline and IF  _underline_  THEN
    format msg:#strikethrough and IF  -strikethrough-  THEN
    "text" name! msg-box .child+
    \regular \normal \sans ;
: vote { xc -- }
    chain-pars stack# IF  xc search-vote#
	vote-box ?dup-IF >o
	    text$ s>unumber? IF
		#1. d+ <# #s #> to text$
	    ELSE  2drop  THEN
	    o>
	ELSE
	    ." Didn't find vote box " xc xemit cr
	THEN
    ELSE
	." Didn't find vote chain " xc xemit cr
    THEN ;

wmsg-class :method msg:like { xc -- }
    xc vote
    msg:silent? 0= IF
	text-color!
	xc ['] .like $tmp }}text 25%bv
	"like" name! msg-box .child+
    THEN ;
wmsg-class :method msg:vote { xc -- }
    msg:end msg-start-par
    {{
	glue*l send-color x-color font-size# 40% f* }}frame dup .button2
	text-color!
	{{ xc [: ." vote: " .like ;] $tmp }}text 25%b
	    "0" }}text 25%b xc [: "vote:" type xemit ;] $tmp name! 
	}}h
    }}z xc [{: xc :}h xc addr data $@ send-silent-like ;] log$ click[]
    msg-box .child+ ;
wmsg-class :method msg:action dup 0= IF  2drop EXIT  THEN { d: string -- o }
    \italic last-otr? IF light-blue ELSE dark-blue THEN
    string ['] utf8-sanitize $tmp }}text 25%bv \regular
    text-color!
    "action" name! msg-box .child+ ;
wmsg-class :method msg:url dup 0= IF  2drop EXIT  THEN { d: string -- o }
    last-otr? IF light-blue ELSE dark-blue THEN
    string ['] utf8-sanitize $tmp }}text _underline_ 25%bv
    text-color!
    [: data >o text$ encode-% o> open-url ;]
    over click[]
    click( ." url: " dup ..parents cr )
    "url" name! msg-box .child+ ;
wmsg-class :method msg:lock dup 0= IF  2drop EXIT  THEN ( d: string -- )
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
    THEN "lock" name! msg-box .child+ ;
wmsg-class :method msg:unlock ( -- o )
	{{
	    glue*l lock-color x-color slide-frame dup .button1
	    blackish l" chat is unlocked" }}text' 25%bv
	}}z msg-box .child+ ;
wmsg-class :method msg:coord dup 0= IF  2drop EXIT  THEN { d: string -- o }
    {{
	glue*l gps-color# slide-frame dup .button1
	blackish string [: ."  GPS: " .coords ;] $tmp }}text 25%b
    }}z "gps" name! msg-box .child+ ;
wmsg-class :method msg:perms { 64^ perm d: pk -- }
    perm [ 1 64s ]L pk msg-group-o .msg:perms# #!
    {{
	glue*l perm-color# slide-frame dup .button1
	{{
	    pk [: '@' emit .key-id ;] $tmp ['] utf8-sanitize $tmp }}text 25%b
	    perm 64@ 64>n ['] .perms $tmp }}text 25%b
	}}h
    }}z msg-box .child+ ;
wmsg-class :method msg:chain ( addr u -- )
    dup 0= IF  2drop EXIT  THEN  sighash? { flag -- }
    flag IF  log# dup search-par#  ELSE  -1  THEN  to chain-log#
    msg:silent? 0= IF
	{{
	    glue*l chain-color# slide-frame dup .button1
	    flag IF  re-green  ELSE  obj-red  THEN
	    log:date log:perm or +log#-date-token
	}}z "chain" name! msg-box .child+
    THEN ;
wmsg-class :method msg:signal { d: pk -- o }
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
    }}z msg-box .child+ ;
wmsg-class :method msg:re ( addr u -- )
    re-green [: ." [" 85type ." ]→" ;] $tmp }}text msg-box .child+
    text-color! ;
wmsg-class :method msg:id ( addr u -- )
    obj-red [: ." [" 85type ." ]:" ;] $tmp }}text msg-box .child+
    text-color! ;
: +otr-box ( addr u -- )
     light-blue \italic }}text 25%bv \regular blackish
     "otrify" name! msg-box .child+ ;
wmsg-class :method msg:otrify { sig u' addr u -- }
    u' 64'+ u =  u sigsize# = and IF
	addr u startdate@ 64dup date>i >r 64#1 64+ date>i' r>
	\ 2dup = IF  ."  [otrified] "  addr u startdate@ .ticks  THEN
	U+DO
	    I msg-log-dec@
	    2dup dup sigpksize# - /string key| msg:id$ str= IF
		dup u - /string addr u str= IF
		    otrify( I [: ."  [OTRifying] #" u. forth:cr ;] do-debug )
		    I [: ."  [OTRify] #" u. ;] $tmp +otr-box
		    sig u' I msg-group-o .msg:log[] $[]@ replace-sig
		    save-msgs&
		ELSE
		    I [: ."  [OTRified] #" u. ;] $tmp +otr-box
		THEN
	    ELSE
		otrify( I [: ."  [OTRifignore] #" u. forth:cr ;] do-debug )
		2drop
	    THEN
	LOOP
    THEN ;

: >rotate ( addr u -- )
    rotate@ to rotate# ;
: >swap ( w h addr u -- w h / h w )
    rotate@ 4 and IF  swap  THEN ;

: update-thumb { d: hash object rotate -- }
    last# >r
    hash avatar-frame object >o to frame#  rotate to rotate#
    frame# i.w frame# i.h rotate 4 and IF  swap  THEN  tile-glue .wh-glue! o>
    [: +sync +resize +textures ;] msgs-box .vp-needed +sync +resize +textures
    r> to last# ;

: 40%bv ( o -- o ) >o current-font-size% 40% f* fdup to border
    fnegate f2/ to borderv o o> ;

: ?thumb { d: hash -- o }
    hash ['] avatar-frame catch nothrow 0= IF
	dup >r i.w r@ i.h hash >swap
	glue*thumb r> }}thumb >r hash r@ .>rotate
    ELSE
	128 128 glue*thumb dummy-thumb }}thumb dup
	>r hash rotate@
	[{: object rotate :}h1 object rotate update-thumb ;]
	hash >hash-finish
	hash key| ?fetch
    THEN  {{ glue*ll }}glue r> }}v 40%bv box[] ;

Hash: audio#
0 Value audio-playing

[IFDEF] linux
    also
    require minos2/pulse-audio.fs
    require minos2/opus-codec.fs
    \ +db pulse( \ )
    \ +db opus( \ )
    pulse-init
    previous
[ELSE]
    [IFDEF] android
	also require minos2/sles-audio.fs sles-init
	require minos2/opus-codec.fs
	previous
    [ELSE]
	begin-structure idx-head
	    4 +field idx-magic
	    cfield: idx-channels
	    cfield: idx-frames
	    wfield: idx-samples
	    8 +field idx-pos
	end-structure
	Variable idx-block
	Variable play-block
	2 Value channels
	
	: start-play ;
	: pause-play ;
	: resume-play ;
	: open-play 2drop 2drop ;
	: open-rec+ 2drop ;
	: close-rec-mono ;
	: close-rec-stereo ;
	scope: pulse
	#48000 Value sample-rate
	: record-mono ;
	}scope
    [THEN]
[THEN]

: be-l, ( l -- ) lbe 4 small-allot l! ;

Create audio-colors
$0000FF00 be-l, $0008F700 be-l, $0010EF00 be-l, $0018E700 be-l,
$0020DF00 be-l, $0029D600 be-l, $0031CE00 be-l, $0039C600 be-l,
$0041BE00 be-l, $004AB500 be-l, $0052AD00 be-l, $005AA500 be-l,
$00629D00 be-l, $006A9500 be-l, $00738C00 be-l, $007B8400 be-l,
$00837C00 be-l, $008B7400 be-l, $00946B00 be-l, $009C6300 be-l,
$00A45B00 be-l, $00AC5300 be-l, $00B44B00 be-l, $00BD4200 be-l,
$00C53A00 be-l, $00CD3200 be-l, $00D52A00 be-l, $00DE2100 be-l,
$00E61900 be-l, $00EE1100 be-l, $00F60900 be-l, $00FF0000 be-l,
$00FF0000 be-l, $08F70000 be-l, $10EF0000 be-l, $18E70000 be-l,
$20DF0000 be-l, $29D60000 be-l, $31CE0000 be-l, $39C60000 be-l,
$41BE0000 be-l, $4AB50000 be-l, $52AD0000 be-l, $5AA50000 be-l,
$629D0000 be-l, $6A950000 be-l, $738C0000 be-l, $7B840000 be-l,
$837C0000 be-l, $8B740000 be-l, $946B0000 be-l, $9C630000 be-l,
$A45B0000 be-l, $AC530000 be-l, $B44B0000 be-l, $BD420000 be-l,
$C53A0000 be-l, $CD320000 be-l, $D52A0000 be-l, $DE210000 be-l,
$E6190000 be-l, $EE110000 be-l, $F6090000 be-l, $FF000000 be-l,

also opengl

: audio-idx>img ( addr u -- addr w h )
    2dup 1- swap idx-frames c@
    $FF over / { inc } 2* dup { /second } idx-head + / 1+ { w }
    w 8 lshift alloz dup { mem ptr }
    bounds ?DO
	audio-colors ptr $100 move
	I idx-head + dup /second + I' umin over - bounds ?DO
	    ptr 3 + I w@ wle #10 rshift $3F umin sfloats bounds ?DO
		inc I c+!
	    1 sfloats +LOOP
	2 +LOOP
	$100 +to ptr
    /second idx-head + +LOOP
    mem 64 w ;
\ debugging tool
require unix/stb-image-write.fs
: img>png { addr w h -- }
\    "audio.png" soil:SOIL_SAVE_TYPE_PNG w h 4 addr soil:SOIL_save_image ;
    "audio.png" w h 4 addr 0 stbi_write_png ;
\ example use:
\ "audio.idx" slurp-file audio-idx>img img>png

: img>thumb ( mem w h -- ivec4-addr len )
    GL_TEXTURE1 glActiveTexture
    thumb-tex-rgba thumb-rgba rgba>style
    atlas-region ;

previous

: read-audio ( addr u -- addr' u' )
    ?read-enc-hashed audio-idx>img img>thumb ;
: audio-frame ( addr u -- frame# )
    key| 2dup audio# #@ nip 0= IF
	2dup read-audio 2swap audio# #!
    ELSE  2drop  THEN  last# cell+ $@ drop ;
: glue*audio ( w h -- o )
    glue new >o
    pixelsize# fm* vglue-c df!
    pixelsize# fm* 4e f* hglue-c df!
    0glue dglue-c glue! o o> ;
Variable idx-hash
Variable play$ "" play$ $!
Variable pause$ "" pause$ $!
: ?audio-idx { d: hash -- o }
    hash ['] audio-frame catch nothrow 0= IF
	dup >r i.w r@ i.h swap
	glue*audio r> }}thumb >o 7 to rotate# o o>
    ELSE
	drop  128 128 glue*thumb dummy-thumb }}thumb
    THEN  >r {{ blackish \large
	play$ $@ }}text \normal
    r> }}h 25%b box[]
    hash idx-hash $! ;

hash: imgs# \ hash of images

: .img ( addr u -- )
    over be-64@ .ticks space 1 64s /string 85type ;
: group.imgs ( addr u -- )
    bounds U+DO  I $@ .img cr  cell +LOOP ;
: .imgs ( -- )
    imgs# [: dup $. ." :" cr cell+ $@ group.imgs ;] #map ;

: +things ( addr u thing -- $string )  >r
    [: { | ts[ 1 64s ] }
	msg:timestamp ts[ be-64!
	ts[ 1 64s type  type ;] $tmp
    2dup msg-group$ $@ r> #!ins[] $make ;
: +imgs ( addr u -- $string )  imgs# +things ;

: img>group# ( img u -- n )
    msg-group$ $@ imgs# #@ bounds ?DO
	2dup I $@ str= IF
	    2drop I last# cell+ $@ drop - cell/  unloop  EXIT
	THEN
    cell +LOOP  2drop -1 ;

: >msg-album-viewer ( img u -- )
\    2dup .img cr
    img>group# dup 0< IF  drop  EXIT  THEN
    last# cell+ $@ album-imgs[] $!
    album-prepare
    [:  1 64s /string ['] ?read-enc-hashed catch nothrow
	IF  2drop thumb.png$ $@  THEN  save-mem ;] is load-img
    4 album-reload
    md-frame album-viewer >o to parent-w o>
    album-viewer md-frame .childs[] >stack
    +sync +resize +resizeall ;

: album-view[] ( addr u o -- o )
    [: addr data $@ >msg-album-viewer ;]
    2swap +imgs 64#1 +to msg:timestamp click[] ;

Variable current-play$
Variable current-player

[IFDEF] opensles also opensles [THEN]

: >msg-audio-player ( addr u -- )
    2dup current-play$ $@ str= IF
	resume-play
    ELSE
	2dup current-play$ $!  caller-w current-player !
	2dup key| ?read-enc-hashed idx-block $!
	keysize safe/string key| ?read-enc-hashed play-block $!
	start-play
    THEN ;

: make-current-player ( -- )
    current-player @ ?dup-IF
	caller-w <> IF
	    play$ $@ current-player @ >o to text$ o> +sync
	THEN
    THEN ;

: audio-play[] ( addr u o -- o )
    dup cell- @ boxes? IF  .childs[] $@ drop @  THEN
    [:  make-current-player
	caller-w .text$ play$ $@ str=
	dup to audio-playing
	IF
	    addr data $@ ['] >msg-audio-player catch 0= IF
		pause$ $@
	    ELSE  2drop EXIT  THEN
	ELSE
	    pause-play play$ $@
	THEN
	caller-w >o to text$ o> +sync ;]
    2swap $make 64#1 +to msg:timestamp click[] ;

[IFDEF] opensles previous [THEN]

: msg-stack= ( addr u -- o true | false )
    2>r msg-box .childs[] $[]# dup IF
	1- msg-box .childs[] $[] @ dup .name$ 2r@ str=
	dup 0= IF  nip  THEN
    THEN  2rdrop ;

wmsg-class :method msg:object ( addr u type -- )
    obj-red
    case
	msg:image#     of
	    2dup key| ?fetch
	    "thumbnail" msg-stack= IF  album-view[] drop  EXIT  THEN
	    "filename"  msg-stack= IF  album-view[] drop  EXIT  THEN
	    [: ." img[" 2dup 85type ']' emit ;] $tmp }}text  "image" name!
	    ( 2rdrop ) album-view[]
	endof
	msg:thumbnail# of
	    ?thumb
	    "filename" msg-stack= IF
		swap 2>r
		{{
		    0e to x-baseline  r> /center
		    0e to x-baseline  r> /center
		}}v box[]
		msg-box .childs[] stack> drop
	    THEN  "thumbnail" name!
	endof
	msg:audio#     of  key| 2dup ?fetch  idx-hash $+! idx-hash $@
	    "audio-idx" msg-stack= IF  audio-play[] drop  EXIT  THEN
	    [: ." audio[" 2dup 85type ']' emit ;] $tmp }}text  "audio" name!
	    0 over .childs[] $[] @ audio-play[] drop
	endof
	msg:audio-idx# of  2dup key| ?fetch
	    ?audio-idx "audio-idx" name!  endof
	msg:patch#     of  [: ." patch["    85type ']' emit
	    ;] $tmp }}text  "patch" name!  endof
	msg:snapshot#  of  [: ." snapshot[" 85type ']' emit
	    ;] $tmp }}text  "snapshot" name!  endof
	msg:message#   of  [: ." message["  85type ']' emit
	    ;] $tmp }}text  "message" name!  endof
	msg:posting#   of
	    ( rdrop ) 2dup $make [: addr data $@ open-posting ;] swap 2>r
	    [: ." posting" .posting ;] $tmp }}text 2r> click[]  "posting" name!
	endof
	msg:filename#  of  \script rework-% }}text \normal  "filename" name!  endof
	nip nip [: ." ???(" h. ." )" ;] $tmp }}text 0
    endcase
    msg-box .child+
    text-color! ;

in net2o : new-wmsg ( o:connection -- o )
    o wmsg-class new >o  parent!  msg-table @ token-table ! o o> ;
' net2o:new-wmsg is net2o:new-msg

wmsg-class ' new static-a with-allocater Constant wmsg-o
wmsg-o >o msg-table @ token-table ! o>

: vp-softbottom ( o:viewport -- )
    vp-y fnegate act >o o anim-del  set-startxy
    0e to vmotion-dx  to vmotion-dy
    m2c:animtime% f@ f2/ o ['] vp-scroll >animate o> ;
: re-msg-box ( -- )
    msgs-box+resize msgs-box >o vp-h { f: old-h } resized
    vp-h old-h f- vp-y f+ 0e fmax to vp-y
    grab-move? 0= IF  vp-softbottom  THEN +sync +resize +resizeall o> ;

: wmsg-display ( addr u -- )
    msg-tdisplay re-msg-box ;
' wmsg-display wmsg-class is msg:display

#200 Value gui-msgs# \ display last x messages by default
0 Value chat-edit-box
0 Value chat-record-button
0 Value chat-recording-button
0 Value chat-mic-icon
0 Value chat-keyboard-icon
0 Value chat-edit    \ chat edit field
0 Value chat-edit-bg \ chat edit background

: >ymd ( ticks -- year month day )
    >fticks fticks>day fdrop unix-day0 + day2ymd ;
: second>i ( dsecond -- index )
    >r #1000000000 um* r> #1000000000 um* drop + d>64 date>i ;
: localtime-fixup ( dsecond -- dsecond' )
    date? #localtime and IF
	2dup >time&date&tz 2drop >r drop 2drop 2drop 2drop
	2dup r> s>d d- >time&date&tz 2drop >r drop 2drop 2drop 2drop
	r> s>d d-
    THEN ;
: day'>i ( day -- index ) #86400 m* localtime-fixup second>i ;
: day>second ( year month day -- dseconds )
    ymd2day unix-day0 - #86400 m* localtime-fixup ;
: day>i ( year month day -- index )
    day>second second>i ;
: month>i ( year month -- index )
    1 day>i ;
: quartal>i ( quartal -- index )
    4 /mod swap 3 * 1+ month>i ;
: year>i ( year -- index )
    1 month>i ;

0 Value chat-history

: closer ( -- o closer )
    {{
	\Large
	{{ s" ❌️" close-color# }}text }}h 25%b
	dup { closer } /right
	glue*ll }}glue
    }}v box[] closer ;
: closerz ( o -- o )
    >r {{ r>
	closer { closer }
    }}z box[] >r
    closer [: data chat-history .childs[] del$cell
      data .dispose-widget +resize +sync +resizeall ;] r@ click[] drop
    r> ;
: }}closerh ( o1 .. on -- o )
	s" ❌️" close-color# }}text dup { closer }
    }}h box[] >r
    closer [: data chat-history .childs[] del$cell
      data .dispose-widget +resize +sync +resizeall ;] r@ click[] drop
    r> ;

: msg-tdisplay-loop ( addr u uskip log -- )
    { log }
    safe/string bounds U+DO
	I log - cell/ to log# \ index
	I @ to log$           \ actual message address
	I $@ { d: msgt }
	msgt ['] msg-tdisplay wmsg-o .catch nothrow  IF
	    <err> ." invalid entry" cr <default> 2drop
	THEN
    cell +LOOP ;
: log-data { endi starti -- } 64#0 to last-tick
    msg-log@ { log u } msgs-box { box }
    {{ }}v box[] dup to msgs-box
    closerz chat-history .child+
    log u endi cells umin starti cells
    log msg-tdisplay-loop  +resize +sync
    box to msgs-box
    log free throw ;
: +days ( end start -- o )
    >r >r
    {{
	\Large \sans \bold
	glue*ll }}glue
	i' unix-day0 + day2ymd drop <# '-' hold 0 # # 2drop '-' hold 0 #s #> }}text
	r> r> DO
	    I 1+ day'>i I day'>i - IF
		I unix-day0 + day2ymd nip nip
		<# 0 # # #> day-color x-color blackish }}button-lit
		[: data 1+ day'>i data day'>i log-data ;] I click[]
	    THEN
	LOOP
	glue*ll }}glue
    }}closerh ;
: +months ( year end start -- o )
    >r 1+ >r { year }
    {{
	\Large \sans \bold
	glue*ll }}glue
	year 0 <# '-' hold #s #> }}text
	r> r> DO
	    year I 1+ month>i year I month>i - IF
		I 0 <# # # #> day-color x-color blackish }}button-lit
		[: data #12 /mod { m y }
		  y m 2 + month>i  y m 1 + month>i 2dup - gui-msgs# u>
		  IF  2drop
		      y m 2 + 1 ymd2day unix-day0 -
		      y m 1 + 1 ymd2day unix-day0 - +days
		      chat-history .child+ +resize +sync
		  ELSE
		      log-data
		  THEN
		;] I 1- year 12 * + click[]
	    THEN
	LOOP
	glue*ll }}glue
    }}closerh ;
: +quartals ( year end start -- o )
    >r 1+ >r { year }
    {{
	\Large \sans \bold blackish
	glue*ll }}glue
	year 0 <# '/' hold #s #> }}text
	r> r> DO
	    I year 4 * + quartal>i
	    I 1- year 4 * + quartal>i - IF
		I 0 <# #s 'Q' hold #> day-color x-color blackish }}button-lit
		[: data 1+ quartal>i data quartal>i 2dup - gui-msgs# u>
		  IF  2drop
		      data 4 /mod swap dup 2* + dup 3 + swap 1+ +months
		      chat-history .child+ +resize +sync
		  ELSE
		      log-data
		  THEN
		;] I 1- year 2* 2* + click[]
	    THEN
	LOOP
	glue*ll }}glue
    }}closerh ;
: +years ( end start -- o ) { | lastyear }
    >r 1+ >r
    {{
	\Large \sans \bold
	glue*ll }}glue
	r> r> DO
	    I 1+ year>i I year>i -
	    IF  I lastyear - dup #100 u>= IF  lastyear +  THEN
		0 <# #s #> day-color x-color blackish }}button-lit
		[: data 1+ year>i data year>i 2dup - gui-msgs# u> IF  2drop
		      data 4 1 +quartals chat-history .child+ +resize +sync
		  ELSE
		      log-data
		  THEN
		;] I click[]
	    THEN
	    I #100 / #100 * to lastyear
	LOOP
	glue*ll }}glue
    }}h box[] ;

: gen-calendar { log u -- o/0 }
    u gui-msgs# cells u<= IF  0  EXIT  THEN
    log u cell- 0 max + $@ startdate@ { 64: endd }
    log $@ startdate@ { 64: std }
    {{
	glue*l chat-hist-col# font-size# 40% f* }}frame dup .button3
	{{
	    endd >ymd 2drop
	    std >ymd 2drop 2dup <> IF
		+years
	    ELSE  2drop
		endd >ymd drop 1- 3 / 1+
		std >ymd drop nip 1- 3 / 1+ +quartals
	    THEN
	}}v box[] dup to chat-history
    }}z box[] ;

: (gui-msgs) ( gaddr u -- )
    reset-time
    64#0 to last-tick  last-bubble-pk $free
    0 to msg-par  0 to msg-box
    msgs-box .childs[] $free \ ) .dispose-childs
    load-msg msg-log@ { log u }
    log u gen-calendar ?dup-IF  msgs-box .child+  THEN
    glue*lll }}glue >msgs-box
    u gui-msgs# cells - 0 max { u' }
    log u ?scan-pks  ?fetch-pks \ activate ?fetch-pks
    log u' wmsg-o .?search-lock
    log u u' log msg-tdisplay-loop
    log free throw  re-msg-box  chat-edit >engage ;

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
    IF   otr-col#  greenish  ELSE chat-col# blackish  THEN
    chat-edit    >o x-color to w-color o>
    chat-edit-bg >o to w-color o> ;
: ?chat-```-status ( o:edit-w -- )
    ```-state
    IF  \mono mono-col?  ELSE  \sans
	msg-group-o .msg:?otr IF  greenish  ELSE  blackish  THEN
    THEN  \regular \normal
    chat-edit >o font@ to text-font x-color to text-color  o> ;

: chat-edit-enter ( o:edit-w -- )
    text$ dup ```-state or IF  do-chat-cmd? 0= IF  avalanche-text
	ELSE  ?chat-otr-status ?chat-```-status  THEN
    ELSE  2drop  THEN
    64#-1 line-date 64!  $lastline $free ;

\ +db click( \ )
\ +db click-o( \ )
\ +db gui( \ )

: flip-mic/kb ( -- )
    chat-edit-box chat-record-button data IF  swap  THEN
    /flop drop /flip drop
    chat-mic-icon chat-keyboard-icon data IF  swap  THEN
    /flop drop /flip drop
    chat-recording-button /flip drop
    data IF  chat-edit >engage  THEN
    data 0= to data
    +resize +sync +lang ;

{{ chat-bg-col# pres-frame#
    {{
	{{
	    glue*l black# slide-frame dup .button1
	    {{
		\large realwhite
		"⬅️" }}text 40%b [: in-group? 0= ?EXIT  false to in-group?
		    leave-chats prev-slide ;] over click[]
		!i18n l" " }}text' !lit 40%b
		"" }}text 40%b dup to group-name
		{{
		}}h box[] dup to group-members
		{{
		    "✅️" }}text 40%b dup to select-mode-button
		    [: logstyles:+select +sync +resize ;] 0 click[]
		}}h box[] "log:~select" name!
		{{
		    "" }}text 40%b dup to deselect-all-button
		    [: deselect-all logstyles:-select +sync +resize ;] 0 click[]
		    "↔️" }}text 40%b dup to select-range-button
		    [: select-range +sync ;] 0 click[]
		    "" }}text 40%b dup to clipboard-button
		    [IFDEF] android also jni [THEN]
		    [: ['] .$selected $tmp clipboard! ;] 0 click[]
		    [IFDEF] android previous [THEN]
		}}h box[] "log:select" name! /flip
		glue*l }}glue
	    }}h box[] dup to msgs-title-box
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
	    {{
		{{ blackish \normal \regular !i18n l" Mic" }}text' !lit
		}}h box[] 40%b dup to chat-mic-icon
		{{ blackish \normal \regular !i18n l" Keyboard" }}text' !lit
		}}h box[] 40%b /flip dup to chat-keyboard-icon
	    }}z box[]
	    ' flip-mic/kb 0 click[]
	    {{
		glue*l send-color x-color font-size# 40% f* }}frame dup .button2
		!i18n blackish l" Record" }}text' !lit 25%b /center
	    }}z box[] /flip dup to chat-record-button
	    [:  chat-record-button /flip drop
		chat-recording-button /flop drop +resize +sync +lang
		[IFDEF] android 1 [ELSE] 2 [THEN] to channels
		"recording" .net2o-cache/ open-rec+
		[IFDEF] android
		    opensles:sample-rate ['] write-record opensles:record-mono
		[ELSE]
		    pulse:sample-rate pulse:record-stereo
		[THEN]
	    ;] 0 click[]
	    {{
		glue*l recording-color# font-size# 40% f* }}frame dup .button3
		!i18n blackish l" Recording…" }}text' !lit 25%b /center
	    }}z box[] /flip dup to chat-recording-button
	    [:  chat-recording-button /flip drop
		chat-record-button /flop drop +resize +sync +lang
		[IFDEF] android
		    close-rec-mono
		[ELSE]
		    close-rec-stereo
		[THEN]
		"recording.aidx" file://.net2o-cache/ avalanche-text
	    ;] 0 click[]
	    {{ glue*lll edit-bg x-color font-size# 40% f* }}frame dup .button3
		dup to chat-edit-bg
		show-error-color \normal \regular
		!i18n edit-infos cell+ @ }}text' !lit
		dup to edit-infobar-text /center
		{{ blackish "" }}edit 40%b dup to chat-edit glue*l }}glue
		    glue*lll }}glue
		}}h box[]
	    }}z dup to chat-edit-box
	    chat-edit [: edit-w .chat-edit-enter drop nip 0 tuck false ;] edit[]
	    ' size-limit filter[]
	    >o act >o [: connection .chat-next-line ;] is edit-next-line o> o o>
	    >o act >o [: connection .chat-prev-line ;] is edit-prev-line o> o o>
	    {{
		glue*l send-color x-color font-size# 40% f* }}frame dup .button2
		blackish !i18n l" Send" }}text' !lit 40%b
		[: data >o chat-edit-enter "" to text$ o>
		    chat-edit >engage ;] chat-edit click[]
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
    gui-log[] ['] $[]exec catch
    ?dup-IF  nip nip ['] DoError gui-log[] $[]exec  THEN
    gui-log[] $[]# IF
	{{
	    glue*lll log-bg x-color font-size# 40% f* }}frame dup .button3
	    \normal \mono blackish
	    {{
		gui-log[] [: }}text /left ;] $[]map
	    }}v box[] 25%b \regular
	    closer { closer }
	}}z box[] >r
	closer [: data .resize-parents data msgs-box .childs[] del$cell
	    data [{: data :}h1 data .dispose-widget ;] up@ send-event
	    re-msg-box ;] r@ click[] drop
	r> msgs-box .child+ re-msg-box
	msgs-box+resize
    THEN ;

' chat-gui-exec is chat-cmd-file-execute

\ special modified chat commands for GUI

chat-cmds uclass chat-cmd-o
end-class gui-chat-cmds

gui-chat-cmds new Constant gui-chat-cmd-o

gui-chat-cmd-o to chat-cmd-o
scope{ /chat
:is ./otr-info ( flag -- ) 2 + change-edit-info ;
:is ./mono-info ( flag -- ) 4 + change-edit-info ;
' .imgs is /imgs
}scope

text-chat-cmd-o to chat-cmd-o

\ top box

box-actor class
end-class net2o-actor

also [IFDEF] jni jni [THEN]

net2o-actor :method ekeyed ( ekey -- )
    case
	[IFDEF] jni
	    k-volup of  audio-playing IF  1 1 clazz .audioManager .adjustVolume
		ELSE  k-up [ box-actor ] defers ekeyed  THEN  endof
	    k-voldown of  audio-playing IF  -1 1 clazz .audioManager .adjustVolume
		ELSE  k-down [ box-actor ] defers ekeyed  THEN  endof
	[THEN]
	k-f5 of  color-theme 0=  IF  anim-end m2c:animtime% f@ f2/ o
		[: drop             fdup f>s to color-theme 1/2 f+ ColorMode! +sync +vpsync ;]
		>animate  THEN   endof
	k-f6 of  color-theme 0<> IF  anim-end m2c:animtime% f@ f2/ o
		[: drop 1e fswap f- fdup f>s to color-theme 1/2 f+ ColorMode! +sync +vpsync ;]
		>animate  THEN   endof
	k-f7 of  >normalscreen   endof
	k-f8 of  >fullscreen     endof
	[ box-actor ] defers ekeyed  EXIT
    endcase ;

previous

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
		glue*l close-color# font-size# 40% f* }}frame dup .button2
		{{
		    realwhite online-symbol }}text 25%b dup to online-flag
		    s" ❌️" }}text 25%b [: -1 data +! ;]
		    [IFDEF] android android:level# [ELSE] level# [THEN] click[]
		}}h box[]
	    }}z
	}}h box[] /vfix
	{{
	    glue*lll }}glue
	    {{
		chat-bg-col# pres-frame#
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
    pw-field >engage
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
    config:logmask-gui# to logmask#
    gui-chat-cmd-o to chat-cmd-o
    n2o-frame to top-widget
    n2o-frame to md-frame
    "PASSPHRASE" getenv 2dup d0= IF  2drop
    ELSE
	>passphrase +key  read-keys
	"PASSPHRASE" getenv erase \ erase passphrase after use!
    THEN
    secret-keys# IF  show-nicks  ELSE
	lacks-key?  IF
	    engage-delay# 0 [: fdrop drop k-enter id-toggler .act .ekeyed ;] >animate
	THEN
    THEN
    1config  !widgets
    get-order n>r ['] /chat >wordlist 1 set-order
    ['] widgets-loop catch  leave-chats
    text-chat-cmd-o to chat-cmd-o
    nr> set-order ?dup-IF  DoError  THEN ;

include gui-dark.fs

previous

\ localization

locale-csv net2o-lang.csv

${LANG} '.' $split 2drop set-locale

\ lsids .lsids

[IFDEF] load-cov  load-cov [THEN]

\\\
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]
