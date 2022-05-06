#! /usr/bin/env gforth-fast
\ Presentation on CloudCalypse

\ Copyright © 2021 Bernd Paysan

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

[IFDEF] android
    hidekb also android >changed hidestatus >changed previous
[THEN]

also minos

ctx 0= [IF]  window-init  [THEN]

require minos2/font-style.fs

44e update-size#

require minos2/text-style.fs
require minos2/presentation-support.fs

0e m2c:animtime% f!

tex: net2o-logo
tex: minos2-logo
' net2o-logo  "net2o-200.png"    0.666e }}image-file 2Constant net2o-img
' minos2-logo "net2o-minos2.png" 0.666e }}image-file 2Constant minos2-img

0 Value n2-img
0 Value m2-img

: ft2021-slides-updated ( -- )
    n2-img m2-img
    slide# @ 7 8 within IF swap THEN
    /flip drop /flop drop ;
' ft2021-slides-updated is slides-updated

: logo-img ( o1 -- o o-img ) { rightimg }
    baseline# 0e to baseline#
    {{  {{ glue*ll }}glue rightimg }}h
    glue*l }}glue
    }}v >o font-size# f2/ to border o o>
    to baseline# ;

: logo-img2 ( o1 o2 -- o o-img ) { leftimg rightimg }
    baseline# 0e to baseline#
    {{  {{ leftimg glue*ll }}glue rightimg }}h
    glue*l }}glue
    }}v >o font-size# f2/ to border o o>
    to baseline# ;

' }}i18n-text is }}text'

light-gui
$005555FF text-color: author#
dark-gui
$44FFFFFF re-color author#

{{
    {{
	glue-left @ }}glue
	
	\ page 0
	{{
	    $FFFFFF00 dup pres-frame

	    tex: cloudcalypse
	    ' cloudcalypse "cloudcalypse-16-9-corona.jpg" 2e 3e f/ }}image-file drop /center
	    {{
		{{
		    glue*l }}glue
		    l" net2o Fortschrittsbericht" /title
		    l" ToDo–Liste für Gforth 1.x" /subtitle
		    glue*2 }}glue	
		    l" Bernd Paysan" /author
		    l" Forth–Tagung 2022. Video–Konferenz" /location
		    {{
			glue*l }}glue
			{{
			    glue*l }}glue author#
			    {{
				glue*l }}glue
				\tiny l" Foto: Ralph W. Lambrecht" }}text'
			    }}h box[] \normal blackish
			}}v box[]
		    }}z box[]
		tex: vp-title glue*l ' vp-title }}vp vp[] dup value title-vp
		>o 3 vp-shadow>># lshift to box-flags o o>
	    }}v box[] >o font-size# to border o Value title-page o o>
	}}z box[] dup >slides

	\ page 1
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Motivation" /title
		glue*l }}glue \ ) $CCDDDD3F color, 4e }}frame dup .button1
		tex: bad-gateway
		' bad-gateway "bad-gateway.png" 0.666e }}image-file
		Constant bgw-glue /center
		glue*l }}glue \ ) $CCDDDD3F color, 4e }}frame dup .button1
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 2
	{{
	    $3F0000FF $FFAAAAFF pres-frame
	    {{
		glue*l }}glue
		tex: apocalypse-riders
		' apocalypse-riders "horsemen-apocalypse.jpg" 1.3e }}image-file drop /center
		glue*l }}glue
	    }}v
	    {{
		{{
		    l" Demotivation" /title
		    l" Sechs Reiter der Apokalypse" /subtitle
		    \skip
		    {{ \ 1️⃣2️⃣3️⃣4️⃣5️⃣6️⃣
			vt{{
			    l" 1️⃣  " l" Pest: Covid–21L/22A+C läuft noch" b\\
			    l" 2️⃣  " l" Krieg: Ukraine als Schlachtfeld" b\\
			    l" 3️⃣  " l" Hunger: Kommt als Folge der Sanktionen" b\\
			    l" 4️⃣  " l" Tod: Ist die Folge der anderen Reiter" b\\
			    l" 5️⃣  " l" Desinformation: Reitet auch mit" b\\
			    l" 6️⃣  " l" Umweltverschmutzung: Trotz Rückkehr der Pest noch da" b\\
			}}vt
		    }}v box[]
		    \skip
		    l" Fortschritt (wenig)" /subsection
		    l"   Nur kurzer Abstand zum letzten Report" \\
		    l"   Gforth 1.0 derzeit etwas wichtiger" \\
		    glue*l }}glue
			glue*l }}glue
			{{
			    glue*l }}glue author#
			    {{
				glue*l }}glue
				\tiny l" Bild: Ви́ктор Миха́йлович Васнецо́в" }}text'
			    }}h box[] \normal blackish
			}}v box[]
		    \ ) $CCDDDD3F 4e }}frame dup .button1
		tex: vp-apo glue*l ' vp-apo }}vp vp[]
		>o 3 vp-shadow>># lshift to box-flags o o>
	    }}v box[] >o font-size# to border o o>
	}}z box[] /flip dup >slides
	
	{{
	    $3F0000FF $FFAAAAFF pres-frame
	    {{
		l" Motivation" /title
		\skip
		{{
		    l"   Reicht für Kriegs–Hurra–Patriotismus nicht ein Balkon?" \\
		    l"   Musk kauft  Twitter" \\
		    l"   Die EU möchte Föderation bei Chat–Protokollen" \\
		    l"   Föderation bei asozialen Hetzwerken wär' auch nett" \\
		}}v box[]
	    }}v box[] >o font-size# to border o o>
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Bug Fixes und Polishing" /title
		vt{{
		    l" • " l" Anzeige des Datums auch in Localtime" b\\
		    l" • " l" Anpassungen an Gforth 1.0–Änderungen" b\\
		    l" • " l" Kein ADDR auf Locals und Values mehr" b\\
		    l" • " l" Container: Snapcraft, Docker und Flatpak laufen" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Föderiertes Chat–Protokoll" /title
		vt{{
		    l" • " l" Kleinster gemeinsamer Nenner?" b\\
		    l" • " l" Nachrichten müssen über Server ausgetauscht werden" b\\
		    l" • " l" Server sind nicht vertrauenswürdig" b\\
		    l" • " l" Verschlüsselte Chat–Messages: Keys für Key Exchange" b\\
		    l" • " l" Zusätzliches Nachrichtenformat mit IV+Entschlüsselung" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Föderierte soziale Netzwerke" /title
		vt{{
		    l" • " l" Nachrichten können als „öffentlich“ betrachtet werden" b\\
		    l" • " l" Braucht ein Protokoll zum Posten und Abholen" b\\
		    l" • " l" Beispiel: Mastodon/Twitter–Crossposts" b\\
		    l" • " l" Ist Server, der Twitter+Mastodon–Credentials braucht😱" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Gforth auf 1.0 bringen" /title
		vt{{
		    l" • " l" Neue Plattform: RISC–V (mit Assembler&Disassembler)" b\\
		    l" • " l" ToDo–Liste weitgehend abgearbeitet" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" ToDo–Liste für ΜΙΝΩΣ2" /title
		vt{{
		    l" • " l" Absätze neu umbrechen, wenn sich die Breite der äußeren Box geändert hat" b\\
		    l" • " l" Treiber für Video4Linux2 fertig stellen" b\\
		    l" • " l" Treiber für Video Acceleration API" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides
	
	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Literatur & Links" /title \small
		vt{{
		    l" Bernd Paysan  " l" net2o fossil repository" bi\\
		    l" 🔗" l" https://net2o.de/" bm\\
		}}vt
		glue*l }}glue
	    }}v box[] >bdr
	}}z box[] /flip dup >slides
	
	' }}text is }}text'
	
	\ end
	glue-right @ }}glue
    }}h box[]
    baseline# 0e to baseline#
    {{
	{{ glue*ll }}glue net2o-img drop }}h dup to n2-img
	{{ glue*ll }}glue minos2-img drop }}h dup to m2-img /flip
	glue*l }}glue
    }}v >o font-size# f2/ to border o o>
    to baseline#
}}z slide[]
to top-widget

also opengl

[IFDEF] writeout-en
    lsids ' .lsids s" ef2018/en" r/w create-file throw
    dup >r outfile-execute r> close-file throw
[THEN]

previous

dark-gui

script? [IF]
    next-arg s" time" str= [IF]  +db time( \ ) [THEN]
    presentation bye
[ELSE]
    presentation
[THEN]

\\\
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
     (("x\"" "l\"") immediate (font-lock-string-face . 1)
      "[\"\n]" nil string (font-lock-string-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
     (("{{" "vt{{") (0 . 2) (0 . 2) immediate)
     (("}}h" "}}v" "}}z" "}}vp" "}}p" "}}vt") (-2 . 0) (-2 . 0) immediate)
    )
End:
[THEN]
