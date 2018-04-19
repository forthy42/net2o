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

: pw-done ( -- flag )
    2dup >passphrase +key
    read-keys secret-keys# 0= IF
	drop nip 0 tuck false
    ELSE
	true
    THEN ;

tex: net2o-logo

{{ $FFFFFFFF pres-frame
{{
glue*lll }}glue
' net2o-logo "doc/net2o-200.png" 1e }}image-file Constant net2o-glue /center
\latin \sans \regular
"net2o GUI" /title
"Enter Password to unlock" /subtitle
{{
glue*l }}glue
{{
glue*l $EEFFEEFF 4e }}frame dup .button3
\mono 0 to x-color "|Password long enough|" }}text
>o font-size# 25% f* to border o o>
blackish \skip
{{
"" }}pw dup Value pw-field pw-field ' pw-done edit[] >bl
>o font-size# 25% f* to border o o>
glue*l
}}h box[]
}}z box[]
glue*l }}glue
}}h box[]
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
