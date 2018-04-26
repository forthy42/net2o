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

\ frames

0 Value pw-frame
0 Value id-frame

\ password screen

0 Value pw-err
0 Value pw-num

: err-fade ( r addr -- )
    1e fover [ pi f2* ] Fliteral f* fcos 1e f+ f2/ f-
    2 tries# @ lshift s>f f* fdup 1e f> IF fdrop 1e -sync ELSE +sync THEN
    $FF swap .fade ;

: shake-lr ( r addr -- )
    [ pi 16e f* ] FLiteral f* fsin f2/ 0.5e f+ \ 8 times shake
    font-size# f2/ f* font-size# f2/ fover f-
    glue-left  >o 0g fdup hglue-c glue! o>
    glue-right >o 0g fdup hglue-c glue! o> +sync drop ;

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
	." Wrong passphrase" cr
	1 tries# +! tries# @ 0 <# #s #> pw-num >o to text$ +glyphs o>
	keys sec[]free
	drop nip 0 tuck false
	1e o ['] shake-lr >animate
	1 tries# @ lshift s>f f2/ pw-err ['] err-fade >animate
    ELSE
	0 secret-key >raw-key
	read-chatgroups
	." Right passphrase" cr
	true
	show-nicks
    THEN ;

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
	    glue-left }}glue
	    \large \sans "🔐" }}text \normal
	    {{
		glue*l $FFFFFFFF 4e }}frame dup .button3
		\mono
		{{ $0000FF08 to x-color "Correct Horse Battery Staple" }}text 25%b
		glue*l }}h
		{{
		    glue-right }}glue
		    l" wrong passphrase!" $FF000000 to x-color }}i18n-text
		    25%b dup to pw-err
		    glue*l
		    $FF0000FF to x-color s" " }}text
		    25%b dup to pw-num
		    glue-left }}glue
		}}h
		blackish
		{{
		    {{
			"" }}pw dup Value pw-field
			25%b >o config:passmode# @ to pw-mode o o>
		    glue*l }}h
		    pw-field ' pw-done edit[]
		    \large \sans $60606060 to x-color "👁" }}text blackish
		    : pw-show/hide ( flag -- )
			2 config:passmode# @ 1 min rot select pw-field >o to pw-mode o>
			pw-field engage +sync ;
		    ' pw-show/hide config:passmode# @ 1 > toggle[]
		    \normal
		}}h box[]
	    }}z box[]
	    glue-right }}glue
	    glue*lll }}glue
	}}h box[] \skip >bl
	glue*lll }}glue
    }}v box[]
}}z box[] to pw-frame

\ id frame

0 Value mykey-box
0 Value groups-box
0 Value nicks-box

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

: show-nick ( o:key -- )
    ke-imports @ >im-color# cells { ki }
    {{ glue*l imports#rgb-bg ki + @ slide-frame
    {{
    {{ \large imports#rgb-fg ki + @ to x-color
    ke-sk sec@ nip IF  \bold  ELSE  \regular  THEN  \sans
    ['] .nick-base $tmp }}text 40%b
    ke-pets[] $[]# IF
	{{ glue*l $00FF0020 slide-frame
	['] .pet-base $tmp }}text 40%b
	}}z box[]
    THEN
    glue*l }}glue }}h box[] name-tab
    {{
    {{ \sans \script ke-selfsig $@ ['] .sigdates $tmp }}text 25%b glue*l }}glue }}h box[]
    {{ \mono \bold \script ke-pk $@ key| ['] 85type $tmp }}text 25%b glue*l }}glue }}h box[] swap
    }}v box[] pk-tab
    glue*lll }}glue }}h box[]
    }}z box[]
    mykey-box nicks-box ke-sk sec@ nip select .child+ ;

: fill-nicks ( -- )
    keys>sort[]
    key-list[] $@ bounds ?DO
	I @ .show-nick
    cell +LOOP
    glue*lll }}glue nicks-box .child+ nicks-box /flop ;

: show-group ( last# -- )
    dup cell+ $@ drop cell+ >o { g -- }
    {{ glue*l $CCAA44FF slide-frame
    {{
    {{ \large blackish
    \regular \sans g $@ }}text 40%b
    glue*l }}glue }}h box[] name-tab
    {{
    {{
    \mono \bold \script groups:id$
    2dup g $@ str= 0= IF  key| ['] 85type $tmp  THEN
    }}text 25%b glue*l }}glue }}h box[]
    glue*l }}glue
    }}v box[] pk-tab
    glue*lll }}glue }}h box[]
    }}z box[] o>
    groups-box .child+
    groups-box /flop ;

: fill-groups ( -- )
    groups>sort[]
    group-list[] $@ bounds ?DO
	I @ show-group
    cell +LOOP ;

{{ $FFFF80FF pres-frame
    {{
	{{ glue*l $000000FF slide-frame
	    {{
		{{ \large \bold \sans $FFFFFFFF to x-color
		l" Nick+Pet" }}i18n-text 40%b glue*l }}glue }}h name-tab
		{{
		    {{ \script \mono l" Pubkey" }}i18n-text 25%b glue*l }}glue }}h
		    {{ \script \bold l" Key date" }}i18n-text 25%b glue*l }}glue }}h
		}}v pk-tab
	    glue*lll }}glue }}h
	}}z
	{{
	    {{
		{{ glue*l $303000FF bar-frame
		{{ \script l" My key" }}i18n-text 25%b glue*l }}glue }}h }}z
		{{ }}v box[] dup to mykey-box
		{{ glue*l $300030FF bar-frame
		{{ \script l" My groups" }}i18n-text 25%b glue*l }}glue }}h }}z
		{{ }}v box[] dup to groups-box /flip
		{{ glue*l $003030FF bar-frame
		{{ \script l" My peers" }}i18n-text 25%b glue*l }}glue }}h }}z
		{{ }}v box[] dup to nicks-box /flip
		glue*lll }}glue
	    tex: vp-nicks glue*lll ' vp-nicks }}vp vp[] dup value peers-box
	    $444444FF to slider-color
	    $CCCCCCFF to slider-fgcolor
	    font-size# 33% f* to slider-border
	dup font-size# 66% f* fdup vslider }}h box[]
    }}v box[]
}}z box[] to id-frame

: show-nicks ( -- )
    fill-nicks fill-groups id-frame to top-widget +glyphs +sync
    top-widget >o
    htop-resize
    <draw-init     draw-init      draw-init>
    2 0 ?DO  htop-resize  LOOP
    o>
    peers-box .vp-top ;

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
     (("{{") (0 . 2) (0 . 2) non-immediate)
     (("}}h" "}}v" "}}z" "}}vp") (-2 . 0) (-2 . 0) non-immediate)
    )
End:
[THEN]
