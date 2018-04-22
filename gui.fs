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
: update-size# ( -- )
    dpy-h @ rows / s>f to font-size#
    font-size# 133% f* fround to baseline#
    font-size# 32e f/ to pixelsize# ;

update-size#

require minos2/text-style.fs

glue new Constant glue-left
glue new Constant glue-right

: 0glue ( -- )
    glue-left  >o 0g fdup hglue-c glue! o>
    glue-right >o 0g fdup hglue-c glue! o> ;

0glue

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

: pres-frame ( color -- o1 o2 ) \ drop $FFFFFFFF
    glue*wh swap slide-frame dup .button1 simple[] ;

: err-fade? ( -- flag ) 0 { flag }
    anims@ 0 ?DO
	>o action-of animate ['] err-fade = flag or to flag
	o anims[] >stack o>
    LOOP  flag ;

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
	." Right passphrase" cr
	true
    THEN ;

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
{{ $0000FF08 to x-color "Horse Battery Staple" }}text
>o font-size# 25% f* to border o o>
glue*l }}h
{{
glue-right }}glue
!i18n l" wrong passphrase!" $FF000000 to x-color }}text' !lit
>o font-size# 25% f* to border o o> dup to pw-err
glue*l
$FF0000FF to x-color s" " }}text
>o font-size# 25% f* to border o o> dup to pw-num
glue-left }}glue
}}h
blackish
{{
{{
"" }}pw dup Value pw-field
>o font-size# 25% f* to border config:passmode# @ to pw-mode o o>
glue*l }}h
pw-field ' pw-done edit[]
\large \sans $60606060 to x-color "👁" }}text blackish
: pw-show/hide ( flag -- )
    2 config:passmode# @ 1 min rot select pw-field >o to pw-mode o>
    pw-field engage +sync ;
' pw-show/hide false toggle[]
\normal
}}h box[]
}}z box[]
glue-right }}glue
glue*lll }}glue
}}h box[] \skip >bl
glue*lll }}glue
}}v box[]
}}z box[] Value pw-frame

: !widgets ( -- )
    top-widget .htop-resize
    pw-field engage
    1e ambient% sf! set-uniforms ;

: net2o-gui ( -- )  pw-frame to top-widget
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
    )
End:
[THEN]
