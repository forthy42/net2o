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
    slide# @ 8 14 within IF swap THEN
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
		glue*l }}glue
		tex: rome-logo
		' rome-logo "rome-logo.png" 0.5e }}image-file drop /center
		glue*l }}glue
	    }}v
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
			    l" 1️⃣  " l" Pest: Covid–22 läuft noch" b\\
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
		    glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
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
		    l" • " l" Container: Snapcraft, Docker und Flatpak läuft" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Gforth auf 1.0 bringen" /title
		vt{{
		    l" • " l" Nutzt Pulseaudio auf Linux, OpenSLES auf Android" b\\
		    l" • " l" Codiert mit Opus" b\\
		    l" • " l" Aufnahme mit OpenSLES funktioniert noch nicht" b\\
		    l" • " l" Android verweigert Aufruf von dynamisch erzeugten Code in Callbacks" b\\
		    l" • " l" Der Rest wird so behandelt wie Bilder" b\\
		    l" • " l" Der Thumbnail ist eine Waveform mit maximalem Pegel/Sekunde" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" ToDo–Liste in ΜΙΝΩΣ2" /title
		vt{{
		    l" • " l" Absätze neu umbrechen, wenn sich die Breite der äußeren Box geändert hat" b\\
		    l" • " l" Treiber für Video4Linux2 fertig stellen" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Harfbuzz–Beispiele 1️" /title
		l" Indien: Ein Staat, 22 Skripten, alphasyllabisch, nichtlinear (Ligaturen & Diakritika)" /subsection
		vt{{
		    l" Devanagari " l" देवनागरी" b\\
		    l" Gurmukhi " l" ਗੁਰਮੁਖੀ" b\\
		    l" Meetei Mayek " l" ꯃꯤꯇꯩ ꯃꯌꯦꯛ" b\\
		    l" Bengali " l" অসমীয়া লিপি" b\\
		    l" Oriya " l" ଓଡ଼ିଆ ଅକ୍ଷର" b\\
		    l" Gujarati " l" ગુજરાતી લિપિ" b\\
		    l" Telugu " l" తెలుగు లిపి" b\\
		    l" Kannada " l" ಕನ್ನಡ ಅಕ್ಷರಮಾಲೆ" b\\
		    l" Tamil " l" தமிழ் அரிச்சுவடி" b\\
		    l" Malayalam " l" മലയാളലിപി" b\\
		    l" Tibetan " l" ཨོཾ་མ་ཎི་པདྨེ་ཧཱུྃ" b\\
		}}vt
	    }}v box[] >bdr
	    {{
		glue*ll }}glue
		tex: indian-scripts \normal \sans
		' indian-scripts "indian-script-map.jpg" 0.24e }}image-file drop /right
	    }}v box[] >bdr blackish
	}}z box[] /flip dup >slides
	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Harfbuzz–Beispiele 2️" /title
		vt{{
		    l" Arabic " l" اَلْعَرَبِيَّةُ" b\\
\italic		    l" Urdu " l" شکسته نستعلیق اُردُو" b\\
\regular	    l" Hieroglyphen " l" 𓆓𓂧𓇋𓈖𓌞𓅱𓀀𓇋𓈎𓂋𓏛𓅱𓍒𓄿𓏜𓄣𓏤𓎡𓄂𓂝𓀀𓅓𓂝" b\\
		    l" " l" 𓎡𓄖𓂻𓈖𓈖𓏥𓄚𓈖𓏌𓅱𓉐𓊏𓊪𓂡𓐍𓂋𓊪𓏲𓆱" b\\
		    l" Zalgo " l" Ẑ̸͓̦͙̼̉͆͑̇͌̎̀ͅa̷̧̡̘͔͉̟͇͈̠̦̱͉͍̓͒́̆̕̚l̵͙̬̰̘͈͉͕̲̙͖̹̻̪͐́̕g̴̢̛͔̘͉̪̮̒̒̄͋́̿̅͂̕͜͠ő̴͓̻͈̪̣̤̱̜͂̋͜͜ ̴̮̳̯̖̪̬̪́T̸̮͖̭͈́ę̵̛̛̛̮̟͖͖̱̖̋̒͑̊̾̌̇̓͐x̵̯̝̝̭͎̮̥͔̞́̒͗͒̌͜t̷̢̠̗̲͇̯̜̹͇̅͋̓̓́́̀́̋͠" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	dark-gui
	$0000EECC new-color, fvalue button-color#
	light-gui
	$FFFFAAFF re-color button-color#
	' noop  is translator
	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Simplified/Traditional/Shinjitai" /title
		vt{{
		    l" • " l" Unicode–Fuckup: Sollte in den Font, nicht in den Zeichensatz" b\\
		    l" • " l" Gleiche Zeichen unterscheiden sich je nach Land" b\\
		    l" • " l" Die Leute wollen sehen, was sie jeweils gewohnt sind" b\\
		    l" • " l" Praktisch ungeordnet als Sack CJ–Glyphen im Unicode" b\\
		    l" • " l" Hangul (nicht verwandt) wenigstens als ein Block" b\\
		    \skip
		    l" • " l" Eingebaut: Konversion SC↔TC (Unihan–Datenbank)" b\\
		    l" • " l" Japanische Varianten? Muss die noch suchen…" b\\
		    \skip
		    l" Beispiel" /subsection
		    \skip
		    {{
			l" • " b0 \large
			l" 双头龙" }}text' glue*l }}glue
			l" 雙頭龍" }}text' glue*l }}glue \normal 
			l" 🇨🇳 scify 🇲🇾" button-color# }}button
			[: ['] translators:scify is translator ;] over click[] glue*l }}glue
			l" 🇹🇼 tcify 🇭🇰" button-color# }}button
			[: ['] translators:tcify is translator ;] over click[] glue*l }}glue
			l" as is" button-color# }}button
			[: ['] noop  is translator ;] over click[] glue*l }}glue  glue*l }}glue 
		    }}h box[]
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Rant gegen Unicode" /title
		vt{{
		    l" • " l" Zeichen kommen selten ohne Kontext vor" b\\
		    l" • " l" Jedes Schrift hat nach wie vor einen eigenen Font" b\\
		    l" • " l" Viele Schriften sind nichtlinear (Zeichen+Diakritika)" b\\
		    l" • " l" Für Kurznachrichten starke Benachteiligung von nicht–lateinischen Schriften" b\\
		    l"  " l" (Kompression bei Kurznachrichten nur eingeschränkt nützlich)" b\\
		    l" • " l" Je nach Schrift andere Schreibrichtung" b\\
		    l" • " l" Bidi Algorithmus unnötig komplex" b\\
		    l" Vergessen" /subsection
		    l" • " l" Trennungsregeln nach Sprache" b\\
		    l" • " l" Vertikale Schriften" b\\
		    l" • " l" Schriften, die horizontal und vertikal geschrieben werden können" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Verbesserungsvorschlag" /title
		vt{{
		    l" • " l" Kontext–Marker selektiert Codepage, Sprache & Schreibrichtung" b\\
		    l" • " l" Stack, Kontext–Pop stellt vorherigen Zustand wieder her" b\\
		    l" • " l" Jede Codepage kann einfach (zusammenhängend) erweitert werden" b\\
		    l" • " l" Kompakter zu speichern, höhere Performance, weniger komplexer Code" b\\
		    l" • " l" Also: Win, win, win, win!" b\\
		}}vt
		l" Vorschlag:" /subsection
		vt{{
		    l" • " l" Freie Bytes (in UTF–8): $F8–$FF" b\\
		    l" $FF " l" Pop" b\\
		    l" $FE+xc " l" Script Selector" b\\
		    l" $FD+xc " l" Language Selector (bei Schriften mit mehreren Sprachen)" b\\
		    l" $FC+xc " l" Variant Selector (Schreibrichtung & Stil)" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Dezentralisierte Zensur" /title
		l" net2o zu einem besseren Ort machen" /subsection
		vt{{
		    l" • " l" Interne, nicht externe Zensur" b\\
		    l" • " l" Desinfodemie in einem peer2peer–Network ähnelt Pandemie–Modellen" b\\
		    l" • " l" Filtert eingehende Inhalte, nicht die eigenen" b\\
		    l" • " l" Der Sender weiß nicht, dass seine Message nicht angekommen ist" b\\
		    l" • " l" Verschiedene Szenarien möglich:" b\\
		}}vt
		vt{{
		    l"   1. " l" Filter versteckt Messages" b\\
		    l"   2. " l" Filter verbreitet Messages nicht" b\\
		    l"   3. " l" Beides („Sterile Immunität“)" b\\
		}}vt
		vt{{
		    l" • " l" Typischer Verteilfaktor = R₀" b\\
		    l" • " l" Wenn mehr als 1–1/R₀ filtern, kommt Informationsmüll nicht weit" b\\
		    l" • " l" Das Teilen von Filter–Regeln muss einfach sein" b\\
		}}vt
		glue*lll }}glue
	    }}v box[] >bdr
	}}z box[] /flip dup >slides
	
	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Desinfodemie" /title
		l" Beispiele aus der Covid–Pandemie" /subsection
		vt{{
		    l" 李文亮 " l" Bekam Maulkorb von der Wuhan–Polizei, als die Hauptnachrichten (新闻联播)" b\\
		    l"  " l" schon einen Bericht hatten.  „Würde hier nicht passieren“" b\\
		    \skip
		    l" Hier? " l" Stattdessen wurden Unmengen Desinformations–Müll über uns gekippt" b\\
		    \skip
		    l" Böse Reg. " l" Ja, die Regierung ist böse. Aber auch inkompetent…" b\\
		    l"  " l" Und sie hat einen pro–Kommerz–Bias. Die Boshaftigkeit hat einen Zweck." b\\
		    \skip
		    l" Science " l" hinterfragt alles.  Aber sie machen Experimente." b\\
		    l"  " l" Masken & Lockdowns funktionieren. Das Virus ist nicht aus dem Wuhan Lab." b\\
		    l"  " l" Impfstoffe sind sicher. Ivermectin/Chloroquin/Vitamine D keine Wundermittel." b\\
		    \skip
		    l" Massaker " l" Das Versagen in der Covid–Pandemie ist ein Massaker." b\\
		    l"  " l" Demokratien können solche Scheußlichkeiten nur mit Desinformation verüben." b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Von Hand moderieren?" /title
		l" Zu wenig, zu spät…" /subsection
		vt{{
		    l" • " l" Lösche die Falschinfo" b\\
		    l" • " l" Lasse die Korrekturen" b\\
		    l" • " l" Blockiere schlechte Teilnemer" b\\
		    l" • " l" In einem P2P–Netzwerk können Leute den Moderator blockieren…" b\\
		    l" • " l" Also ist ein grober Konsens nötig" b\\
		    \skip
		    l" • " l" Manuelle Intervention ist zu langsam" b\\
		    l" • " l" Niemand liest Berichtigungen" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Automatische Filter?" /title
		l" Tatsächlich das schwere Problem" /subsection
		vt{{
		    l" Texte: " l" Schlechte Texte (entspricht PCR–Test)" b\\
		    l" + " l" Leicht zu implementieren" b\\
		    l" – " l" Leicht zu umgehen, viele falsch–Positive" b\\
		    \skip
		    l" Bilder: " l" Fingerprints" b\\
		    l" + " l" Mittelschwer zu implementieren" b\\
		    l" – " l" Leicht zu umgehen, pre–image attacks auch einfach" b\\
		    \skip
		    l" Ton: " l" Sprache zu Text" b\\
		    l" + " l" Mittelschwer zu implementieren" b\\
		    l" – " l" Umgehen unklar? pre–image attacks auch einfach" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Was für schlechte Texte?" /title
		l" Es geht nicht um böse Wörter" /subsection
		vt{{
		    l" • " l" Di Rächtschraipunk ist grußlick" b\\
		    l" • " l" SIE SCHREIEN DIE GANZE ZEIT" b\\
		    l" • " l" und wenn ,dann plenken sie richtig übel !" b\\
		    l" • " l" Zahl (und Farbe) der Ausrufezeichen‼️‼️" b\\
		    \skip
		    l" • " l" Das klingt einfach zu umgehen — für kluge Leute" b\\
		    l" • " l" Kluge Leute sind nicht das Problem…" b\\
		    \skip
		    l" • " l" AI sagt, das ist Porn:" b\\
		}}vt
		tex: dunes \normal \sans
		' dunes "sand_dunes_police_ai_porn.jpg" 0.5e }}image-file drop /right
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		{{
		    l" Filter–Algorithmen aus dem RL" /title
		}}v box[] >bdr
		tex: perscheid-gesperrt
		tex: perscheid-flacherdler
		\normal \sans
		{{
		    ' perscheid-gesperrt "perscheid-gesperrt.jpg" 0.60e 896 640 fm*/ }}image-file drop 
		    {{
			l" • " l" Wort–basiert" b\\
			l" • " l" von billigen Click–Workern" b\\
			l" • " l" Konfuse Regeln" b\\
			l" • " l" Wollen die Idioten behalten" b\\
			l"  " l" weil die die Ads klicken" b\\
			l"  " l" die auch Betrug sind…" b\\
			glue*ll }}glue dark-blue \italic \serif
			l" Martin Perscheid" }}text' /center
			l" † 31. Juli 2021 — RIP" }}text' /center
			\normal blackish
		    }}v box[]
		    glue*ll }}glue
		    ' perscheid-flacherdler "perscheid-flacherdler-fb-sperre.jpg" 0.60e }}image-file drop 
		}}h box[] >bl
	    }}v box[]
	}}z box[] /flip dup >slides
	
	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" The non–technical problems" /title
		vt{{
		    l" • " l" Get your contacts over to net2o" b\\
		    l" • " l" How to make a social network a nice place?" b\\
		    l" • " l" Funding of net2o?" b\\
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
		    "https://net2o.de/" link[]
		    l" The New Yorker  " l" Why Facts don’t change our Mind" bi\\
		    l" 🔗" l" https://www.newyorker.com/magazine/2017/02/27/" bm\\
		    "https://www.newyorker.com/magazine/2017/02/27/why-facts-dont-change-our-minds" link[]
		    l" " l" why-facts-dont-change-our-minds" bm\\
		    "https://www.newyorker.com/magazine/2017/02/27/why-facts-dont-change-our-minds" link[]
		    l" Sascha Lobo  " l" QAnon — Verschwörungsideologie zum Mitmachen" bi\\
		    l" 🔗" l" https://www.spiegel.de/netzwelt/netzpolitik/qanon…" bm\\
		    "https://www.spiegel.de/netzwelt/netzpolitik/qanon-verschwoerungsideologie-zum-mitmachen-a-8656ef8e-b2dc-4b90-a09f-8cb6e4a4db19" link[]
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
