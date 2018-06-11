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

glue new Constant glue-left
glue new Constant glue-right
glue new Constant glue-sleft
glue new Constant glue-sright

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
	read-chatgroups announce-me o>
	\ ." Right passphrase" cr
	show-nicks
	true
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

{{ login-bg-col# pres-frame dark-blue# ' dark-blue >body !
    {{
	glue*lll }}glue
	' net2o-logo "doc/net2o.png" 0.666e }}image-file Constant net2o-glue /center
	!i18n l" net2o GUI" /title
	l" Enter passphrase to unlock" /subtitle
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
$66CC66FF , \ manually imported is green, too
$55DD55FF , \ scanned is more green
$CCEE55FF , \ seen in chat is more yellow
$EECC55FF , \ imported from DHT is pretty yellow
$FF8844FF , \ invited is very yellow
$FF0000FF , \ untrusted is last
Create imports#rgb-fg
$003300FF ,
$000000FF ,
$000000FF ,
$000000FF ,
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
    top-widget >o htop-resize  <draw-init draw-init draw-init> htop-resize o> ;

: show-connected ( -- ) ;

: gui-chat-connects ( -- )
    [: chat-keys [: key>group
	    2dup search-connect ?dup-IF  >o +group greet o> 2drop  EXIT  THEN
	    2dup pk-peek? IF  chat-connect true !!connected!!
	    ELSE  2drop  THEN ;] $[]map ;] catch
    [ ' !!connected!! >body @ ]L = IF  show-connected  THEN ;

: group[] ( box group -- box )
    [:  top-widget >r
	data $@ group-name >o to text$ o>
	data cell+ $@ drop cell+ >o groups:id$ groups:member[] o>
	[: [: 2over type '@' emit type ;] $tmp chat-keys $+[]! ;] $[]map
	gui-msgs chat-frame to top-widget refresh-top
	gui-chat-connects
	widgets-loop \ connection .send-leave
	r> to top-widget +sync +config
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

: nicks-title ( -- )
    {{ glue*l $000000FF slide-frame dup .button1
	{{
	    {{ \large \bold \sans whitish
	    l" Nick+Pet" }}i18n-text 25%b glue*l }}glue }}h name-tab
	    {{
		{{ \script \mono \bold l" Pubkey"   }}i18n-text 20%bt glue*l }}glue }}h
		{{ \script \sans \bold l" Key date" }}i18n-text glue*l }}glue }}h
	    }}v pk-tab
	glue*lll }}glue }}h
    }}z ;

{{ $FFFF80FF pres-frame
    {{
	nicks-title
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
    fill-nicks fill-groups
    id-frame to top-widget
    refresh-top
    peers-box .vp-top +sync +config ;

\ messages

msg-class class
end-class wmsg-class

$88FF88FF Value my-signal#
$CCFFCCFF Value other-signal#
$4444CCFF color: link-blue
$44CC44FF color: re-green
$CC4444FF color: obj-red
$BBDDDDFF color: msg-bg
$33883366 Value day-color#
$88333366 Value hour-color#

Variable last-bubble-pk
0 Value last-bubble

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
:noname { d: pk -- o }
    pk key| pkc over str= { me? }
    pk key| last-bubble-pk $@ str= IF
	{{ }}h dup to msg-box >r
	{{ r> glue*l }}glue }}h >bl
	msg-vbox .child+
    ELSE
	pk startdate@ add-dtms
	pk key| last-bubble-pk $!
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
		glue*l $FFFFFFFF slide-frame dup me? IF .rbubble ELSE .lbubble THEN
		{{
		    {{ }}h dup to msg-box
		    >r {{ r> glue*l }}glue }}h >bl
		}}v >o font-size# 25% f*
		me? IF
		    fdup to border fnegate fdup to borderl to borderv
		ELSE  to borderl  THEN o o> dup to msg-vbox
	    }}z
	    glue*ll }}glue
	    me? IF  swap rot  THEN
	}}h msgs-box .child+
    THEN
; wmsg-class to msg:start
:noname ( -- )
; wmsg-class to msg:end
:noname { d: string -- o }
    link-blue \mono string [: '#' emit type ;] $tmp ['] utf8-sanitize $tmp }}text 25%bv blackish \sans
    msg-box .child+
; wmsg-class to msg:tag
:noname { d: string -- o }
    blackish string ['] utf8-sanitize $tmp }}text 25%bv
    msg-box .child+
; wmsg-class to msg:text
:noname { d: string -- o }
    \italic dark-blue string ['] utf8-sanitize $tmp }}text 25%bv \regular blackish msg-box .child+
; wmsg-class to msg:action
:noname { d: string -- o }
    {{
	glue*l $FFCCCCFF slide-frame dup .button1
	string [: ."  GPS: " .coords ;] $tmp }}text 25%b
    }}z msg-box .child+
; wmsg-class to msg:coord
:noname { d: pk -- o }
    {{
	pk key| 2dup 0 .pk@ key| str= my-signal# other-signal# rot select
	glue*l swap slide-frame dup .button1 40%b >r
	[: '@' emit .key-id ;] $tmp ['] utf8-sanitize $tmp }}text 25%b r> swap
    }}z msg-box .child+
; wmsg-class to msg:signal
:noname ( addr u -- )
    re-green [: ." [" 85type ." ]→" ;] $tmp }}text msg-box .child+
    blackish
; msg-class to msg:re
:noname ( addr u -- )
    obj-red [: ." [" 85type ." ]:" ;] $tmp }}text msg-box .child+
    blackish
; msg-class to msg:id

wmsg-class ' new static-a with-allocater Constant wmsg-o
wmsg-o >o msg-table @ token-table ! o>

: wmsg-display ( addr u -- )
    !date wmsg-o .tmsg-display ;
:noname ( addr u -- ) wmsg-display
    msgs-box >o [: +sync +config ;] vp-needed vp-bottom
    +sync +config o> ; is msg-display

#128 Value gui-msgs# \ display last 128 messages

: gui-msgs ( gaddr u -- )
    -1 to last-day
    -1 to last-hour
    -1 to last-minute
    msgs-box .dispose-childs
    glue*lll }}glue msgs-box .child+
    2dup load-msg ?msg-log
    last# msg-log@ 2dup { log u }
    dup gui-msgs# cells - 0 max /string bounds ?DO
	I $@ ['] wmsg-display catch IF
	    <err> ." invalid entry" <default> cr 2drop
	THEN
    cell +LOOP
    log free throw  msgs-box >o resized vp-bottom o> ;

[IFDEF] android also android [THEN]

: chat-edit-enter ( o:edit-w -- )
    text$ do-chat-cmd? 0= IF  avalanche-text  THEN ;

{{ $80FFFFFF pres-frame
    {{
	{{
	    glue*l $000000FF slide-frame dup .button1
	    {{
		\large whitish
		"⬅" }}text 40%b [: -1 level# +! ;] over click[]
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
	    over >r }}h box[] r> font-size# 66% f* fdup hslider
	}}v box[]
	{{
	    {{ glue*lll $FFFFFFFF font-size# 40% f* }}frame dup .button3
		{{ \normal \regular blackish "" }}edit 40%b dup value chat-edit glue*l }}glue
		    glue*lll }}glue
		}}h box[]
	    }}z chat-edit [: edit-w .chat-edit-enter drop nip 0 tuck false ;] edit[]
	    {{
		glue*l $80FF80FF font-size# 40% f* }}frame dup .button2
		!i18n l" Send" }}text' !lit 40%b
		[: data >o chat-edit-enter "" to text$ o> ;] chat-edit click[]
	    }}z box[]
	}}h box[]
    }}v box[]
}}z box[] to chat-frame

[IFDEF] android previous [THEN]

\ top widgets

: !widgets ( -- )
    top-widget .htop-resize
    pw-field engage
    1e ambient% sf! set-uniforms ;

: net2o-gui ( -- )
    pw-frame to top-widget
    "PASSPHRASE" getenv 2dup d0= IF  2drop
    ELSE
	>passphrase +key  read-keys
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

s" LANG" getenv '_' $split 2swap
' lang >body find-name-in ?dup [IF] execute [THEN]
'.' $split 2drop
' lang >body find-name-in ?dup [IF] execute [THEN]

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
     (("}}h" "}}v" "}}z" "}}vp") (-2 . 0) (-2 . 0) immediate)
    )
End:
[THEN]
