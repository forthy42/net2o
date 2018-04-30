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
    dpy-h @ s>f
    default-diag screen-diag f/ fsqrt default-scale f* 1/f 32 fm*
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
    2 tries# @ lshift s>f f* fdup 1e f> IF fdrop 1e -sync ELSE +sync THEN
    $FF swap .fade fdrop ;

: shake-lr ( r addr -- )
    [ pi 16e f* ] FLiteral f* fsin f2/ 0.5e f+ \ 8 times shake
    font-size# f2/ f* font-size# f2/ fover f-
    glue-sleft  >o 0g fdup hglue-c glue! o>
    glue-sright >o 0g fdup hglue-c glue! o> +sync drop ;

0e 0 shake-lr

: pres-frame ( color -- o1 o2 ) \ drop $FFFFFFFF
    glue*wh swap slide-frame dup .button1 ;

: err-fade? ( -- flag ) 0 { flag }
    anims@ 0 ?DO
	>o action-of animate ['] err-fade = flag or to flag
	o anims[] >stack o>
    LOOP  flag ;

forward show-nicks

: pw-done ( max span addr pos -- max span addr pos flag )
    err-fade? IF  false  EXIT  THEN
    over 3 pick >passphrase +key
    read-keys secret-keys# 0= IF
	\ ." Wrong passphrase" cr
	1 tries# +! tries# @ 0 <# #s #> pw-num >o to text$ +glyphs o>
	keys sec[]free
	drop nip 0 tuck false
	1e o ['] shake-lr >animate
	1 tries# @ lshift s>f f2/ pw-err ['] err-fade >animate
    ELSE
	0 >o 0 secret-key init-client >raw-key
	read-chatgroups o>
	\ ." Right passphrase" cr
	show-nicks
	true
    THEN ;

: 20%bt ( o -- o ) >o font-size# 20% f* to bordert o o> ;
: 25%b ( o -- o ) >o font-size# 25% f* to border o o> ;
: 40%b ( o -- o ) >o font-size# 40% f* to border o o> ;

\ password frame

tex: net2o-logo

{{ $FFFFFFFF pres-frame
    {{
	glue*lll }}glue
	' net2o-logo "doc/net2o.png" 0.666e }}image-file Constant net2o-glue /center
	\latin \sans \regular
	!i18n l" net2o GUI" /title
	l" Enter passphrase to unlock" /subtitle
	!lit
	{{
	    glue*lll }}glue
	    glue-sleft }}glue
	    {{
		\large \sans "🔐" }}text
		$FF0040FF to x-color s" " }}text
		25%b dup to pw-num /center
	    }}z
	    {{
		glue*l $FFFFFFFF 4e }}frame dup .button3
		\mono \normal
		{{ $0000FF08 to x-color "Correct Horse Battery Staple" }}text 25%b
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
			[IFDEF] android
			    glue*l }}glue
			[THEN]
			"" }}pw dup Value pw-field
			25%b >o config:passmode# @ to pw-mode o o>
			glue*l }}glue
		    }}h
		    pw-field ' pw-done edit[]
		    \large \sans $60606060 to x-color "👁" }}text blackish
		    : pw-show/hide ( flag -- )
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

\ id frame

0 Value mykey-box
0 Value groups-box
0 Value nicks-box
0 Value msg-box

htab-glue new tab-glue: name-tab
htab-glue new tab-glue: pk-tab
htab-glue new tab-glue: group-tab

[IFUNDEF] child+
    : child+ ( o -- ) o over >o to parent-w o> childs[] >stack ;
[THEN]

Create ke-imports#rgb

Create imports#rgb-bg
$FFFFCCFF ,
$4400CCFF ,
$FFFFFFFF ,
$44CCFFFF ,
$4488FFFF ,
$FFFFFFFF ,
$FFFFFFFF ,
Create imports#rgb-fg
$0000FFFF ,
$00FF00FF ,
$00FFFFFF ,
$88CC00FF ,
$FFFFFFFF ,
$8800FFFF ,
$FF0000FF ,

: nick[] ( box o:nick -- box )
    [: data >o ." clicked on " ke-nick $. cr o> ;] o click[] ;

: show-nick ( o:key -- )
    ke-imports @ >im-color# cells { ki }
    {{ glue*l imports#rgb-bg ki + @ slide-frame
	{{
	    {{ \large imports#rgb-fg ki + @ to x-color
		ke-sk sec@ nip IF  \bold  ELSE  \regular  THEN  \sans
		['] .nick-base $tmp }}text 25%b
		ke-pets[] $[]# IF
		    {{ glue*l $00FF0020 slide-frame
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

: group[] ( box group -- box )
    [: data { g } ." clicked on " g $. cr ;] swap click[] ;

: show-group ( last# -- )
    dup cell+ $@ drop cell+ >o { g -- }
    {{ glue*l $CCAA44FF slide-frame
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
    {{ glue*l $000000FF slide-frame
	{{
	    {{ \large \bold \sans $FFFFFFFF to x-color
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
	    tex: vp-nicks glue*lll ' vp-nicks }}vp vp[] dup value peers-box
	    $444444FF to slider-color
	    $CCCCCCFF to slider-fgcolor
	    font-size# 33% f* to slider-border
	dup font-size# 66% f* fdup vslider }}h box[]
    }}v box[]
}}z box[] to id-frame

: show-nicks ( -- )
    fill-nicks fill-groups
    id-frame to top-widget +glyphs +sync
    top-widget >o
    htop-resize
    <draw-init     draw-init      draw-init>
    2 0 ?DO  htop-resize  LOOP
    o>
    peers-box .vp-top ;

\ messages

msg-class class
end-class wmsg-class

$FF4444FF Value my-signal#
$CCCCCCFF Value other-signal#
$4444CCFF color: link-blue
$44CC44FF color: re-green
$CC4444FF color: obj-red

:noname ( addr u -- o )
    {{
	{{
	    glue*l imports#rgb-bg ( ki + ) @ slide-frame
	    \sans \large \bold ['] .key-id $tmp }}text 25%b
	    \regular \normal
	}}z ; wmsg-class to msg:start
:noname ( addr u -- o )
    link-blue \mono [: '#' emit type ;] $tmp }}text 25%b blackish \sans
; wmsg-class to msg:tag
:noname ( addr u -- o )
    }}text 25%b
; wmsg-class to msg:text
:noname ( addr u -- o )
    \italic }}text 25%b \regular
; wmsg-class to msg:action
:noname ( addr u -- )
    [: ."  GPS: " .coords ;] $tmp }}text 25%b ; wmsg-class to msg:coord
:noname ( addr u -- o )
    {{
	key| 2dup pk@ key| str= my-signal# other-signal# rot select
	glue*l swap slide-frame
	[: '@' emit .key-id ;] $tmp }}text 25%b
    }}z
; wmsg-class to msg:signal
:noname ( addr u -- )
    re-green [: ." [" 85type ." ]→" ;] $tmp }}text blackish ; msg-class to msg:re
:noname ( addr u -- )
    obj-red [: ." [" 85type ." ]:" ;] $tmp }}text blackish ; msg-class to msg:id
:noname ( -- )
    }}h box[] msg-box .+child ; wmsg-class to msg:end

wmsg-class ' new static-a with-allocater Constant wmsg-o
wmsg-o >o msg-table @ token-table ! o>

: wmsg-display ( addr u -- )
    !date wmsg-o .msg-display ;

{{ $80FFFFFF pres-frame
    {{
    }}v box[] dup to msg-box
}}z box[] to chat-frame

\ top widgets

: !widgets ( -- )
    top-widget .htop-resize
    pw-field engage
    1e ambient% sf! set-uniforms ;

: net2o-gui ( -- )
    pw-frame to top-widget
    "PASSPHRASE" getenv 2dup d0= IF  2drop
    ELSE
	>passphrase +key  read-keys  secret-keys# IF
	    show-nicks
	THEN
    THEN
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
