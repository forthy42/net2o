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
    16e fround to font-size#
    font-size# 133% f* fround to baseline#
    1e to pixelsize# ;

update-size#

require minos2/text-style.fs

: pres-frame ( color -- o1 o2 ) \ drop $FFFFFFFF
    glue*wh swap slide-frame dup .button1 simple[] ;

: pw-done ( max span addr pos -- max span addr pos flag )
    over 3 pick >passphrase +key
    read-keys secret-keys# 0= IF
	." Wrong passphrase" cr
	drop nip 0 tuck false
    ELSE
	." Right passphrase" cr
	true
    THEN ;

tex: net2o-logo

{{ $FFFFFFFF pres-frame
{{
glue*lll }}glue
' net2o-logo "doc/net2o-200.png" 1e }}image-file Constant net2o-glue /center
\latin \sans \regular
!i18n l" net2o GUI" /title
l" Enter passphrase to unlock" /subtitle
!lit
{{
glue*l }}glue
{{
glue*l $FFFFFFFF 4e }}frame dup .button3
{{ \mono $FF000018 to x-color "12345678" }}text
>o font-size# 25% f* to border o o>
glue*l }}h box[]
\mono $0000FF18 to x-color "Horse Battery Staple" }}text
>o font-size# 25% f* to border o o>
blackish
{{
"" }}pw dup Value pw-field pw-field ' pw-done edit[]
>o font-size# 25% f* to border o o>
glue*l
}}h box[]
}}z box[]
glue*l }}glue
}}h box[] cbl >bl
glue*lll }}glue
}}v box[]
}}z box[] Value pw-frame

: !widgets ( -- ) top-widget .htop-resize
    pw-field engage
    1e ambient% sf! set-uniforms ;

: net2o-gui ( -- )  pw-frame to top-widget
    1config  !widgets  widgets-loop ;

' net2o-gui is run-gui

previous

cs-scope: lang

locale en \ may differ from native
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
