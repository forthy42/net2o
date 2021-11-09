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
' net2o-logo "net2o-200.png" 0.666e }}image-file 2Constant net2o-img

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
		    l" net2o Progress Report" /title
		    l" Dezentralisierte Zensur" /subtitle
		    glue*2 }}glue	
		    l" Bernd Paysan" /author
		    l" Forth–Tagung 2021. Video–Konferenz" /location
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
		l" 1¾ Jahre COVID–19–Pandemie" /title
		\skip
		l" Überwachungskapitalismus" /subsection
		{{
		    l"    Apple möchte deine Bilder lokal auf KiPo scannen" \\
		    l"   ➡️ Musste schnell einen Rückzieher machen" \\
		    l"    Facebook &  Twitter “checken Fakten”" \\
		    l"   ➡️ und verteilen immer noch Unmengen Blödsinn" \\
		    l"    Telegram wurde das Tool der Wahl der Covidioten" \\
		    l"   ➡️ Meinungsfreiheit hat und ist ein Problem…" \\
		    l"    Twitter testet ein “safe space”–Feature…" \\
		    l"   ➡️ Der Algorithmus versteckt, was dir nicht gefällt" \\
		}}v box[]
		\skip
		l" Fortschritt" /subsection
		l"   Wenig bei net2o, mehr bei Bernd 2.0, etwas bei ΜΙΝΩΣ2" \\
		l"   TCP/IP wird 40" \\
		glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
	    }}v box[] >o o Value snowden-page font-size# to border o o>
	}}z box[] /flip dup >slides
	
	\ page 4
	{{
	    $5F0000FF $FF7777FF pres-frame
	    {{
		glue*ll }}glue
		tex: bill-gates
		' bill-gates "Bill_Gates.png" 1e }}image-file drop /right
	    }}v box[] >bdr blackish
	    {{
		l" Desinformation" /title
		l" Lehren aus der Pandemie" /subsection
		vt{{
		    l" Erster Eindruck " l" Fakten ändern keine Meinung [2]" b\\
		    l" Ikea–Effect "  l" Einfach zu Erlangendes hat “keinen Wert” [3]" b\\
		    l" Weltanschauung "  l" lassen uns Fakten verwerfen, die nicht passen" b\\
		    l" Wissenschaft "  l" muss umsichtig sein" b\\
		    l" Plausibilität "  l" Dieser Mann hat wiederholt Böses getan" b\\
		    l" " l" Allerdings braucht er dich nicht zu chippen" b\\
		    l" " l" Er hat schon alles, was er braucht" b\\
		    \skip
		    l" QAnon "  l" mutmaßlich ursprünglich Wu Ming (五名, 5 Namen)" b\\
		    l" " l" Wu Ming kann man auch 无明 aussprechen, Ignoranz" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 4
	{{
	    $5F0000FF $FF7777FF pres-frame
	    {{
		l" Assoziale Hetzwerke" /title
		l" Probleme mit Leuten seit dem Eternal September" /subsection
		vt{{
		    l" Meinungen " l" sind nicht Fakten, sondern Werte, an die die Leute glauben" b\\
		    l" Glauben "   l" ist nicht offen für Diskussion, sondern Teil der Identität" b\\
		    l" Identität " l" wird heftig verteidigt und benutzt, um Menschen zu segregieren" b\\
		    l" Mauern "    l" sind im Kopf und sie einzureißen erzeugt Aggressionen" b\\
		    \skip
		    l" Meinungsfreiheit " l" Ist ein Konzept aus eine Zeit, als Religion" b\\
		    l" " l" stark und Wissenschaft schwach war" b\\
		    l" " l" Ermöglichte Koexistenz zwischen" b\\
		    l" " l" verschiedenen Glauben," b\\
		    l" " l" zwischen Wissenschaft und Dogma" b\\
		}}vt
	    }}v box[] >bdr
	    {{
		glue*ll }}glue \tiny \mono dark-blue
		{{ glue*ll }}glue l" 🔗" }}text' l" xkcd.com/386" }}text' _underline_ }}h
		[: s" xdg-open https://xkcd.com/386" system ;] 0 click[]
		tex: duty-calls \normal \sans
		' duty-calls "duty_calls.png" 0.95e }}image-file drop /right
	    }}v box[] >bdr blackish
	}}z box[] /flip dup >slides

	\ page 9
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Erledigt: “Wer hat was”" /title
		l" Hash→Objekt–Ursprung" /subsection
		vt{{
		    l" ❓️ " l" Ursprünglicher Plan: Hashes in der DHT" b\\
		    l" ➡️ " l" Abfrage enthüllt: wer hat was?" b\\
		    l" ❓️ " l" Erste „Lösung“: Hashes verschlüsseln" b\\
		    l" ➡️ " l" Abfrage enthüllt wer das gleiche Ding will" b\\
		    l" ❓️ " l" Onion routing im DHT?" b\\
		    l" ➡️ " l" Komplex, langsam" b\\
		    \skip
		    l" ➡️ " l" Besser „wer hat was“ in der Chat–Log–Struktur halten" b\\
		    l" ➡️ " l" „wer“ ist device.pubkey" b\\
		    \skip
		    l" ❓️ " l" Fehlt noch: Reichweitenlimit für „wer hat was“" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides


	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Done: Formated chat messages" /title
		vt{{
		    l" • " l" Inspired by Mattermost" b\\
		    l" • " l" Format parsing different from Markdown (simpler)" b\\
		    l" • " l" Disabled by default" b\\
		    l" • " l" Sender parsed, so sender parser can change" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Mostly done: Voice messages" /title
		vt{{
		    l" • " l" Uses Pulseaudio on Linux, OpenSLES on Android" b\\
		    l" • " l" Encoding in Opus" b\\
		    l" • " l" OpenSLES recording doesn’t work yet" b\\
		    l" • " l" Android problems with callbacks into Gforth’s dynamic code" b\\
		    l" • " l" The rest is similar to pictures" b\\
		    l" • " l" Thumbnail is a wavefrom plot with max level/second" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Weitgehend erledigt: Harfbuzz (in ΜΙΝΩΣ2)" /title
		vt{{
		    l" • " l" Zweck: Den komplexeren Teil des Unicode–Renderings machen" b\\
		    l" • " l" Das Interface ist nicht sehr komplex" b\\
		    l" • " l" Aber man muss den Code restrukturieren" b\\
		    l" • " l" und den Bidi–Algorithmus selbst implementeiren" b\\
		    l" • " l" (und was ist mit vertikalen Skripts wie Mongolisch?)" b\\
		    \skip
		    l" • " l" Herausforderung: Text nach Fonts aufteilen" b\\
		    l" • " l" und Varianten–Selektoren beachten" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Harfbuzz–Beispiele" /title
		vt{{
		    l" Indien: " l" Ein Staat, 22 Skripten, alphasyllabisch, nichtlinear (Ligaturen & Diakritika)" b\\
		    l" Devanagari " l" देवनागरी" b\\
		    l" Gurmukhi " l" ਗੁਰਮੁਖੀ" b\\
		    \ l" Meetei Mayek " l" ꯃꯤꯇꯩ ꯃꯌꯦꯛ" b\\
		    l" Bengali " l" অসমীয়া লিপি" b\\
		    l" Oriya " l" ଓଡ଼ିଆ ଅକ୍ଷର" b\\
		    l" Gujarati " l" ગુજરાતી લિપિ" b\\
		    l" Telugu " l" తెలుగు లిపి" b\\
		    l" Kannada " l" ಕನ್ನಡ ಅಕ್ಷರಮಾಲೆ" b\\
		    l" Tamil " l" தமிழ் அரிச்சுவடி" b\\
		    l" Malayalam " l" മലയാളലിപി" b\\
		    l" • " l" Ẑ̸͓̦͙̼̉͆͑̇͌̎̀ͅa̷̧̡̘͔͉̟͇͈̠̦̱͉͍̓͒́̆̕̚l̵͙̬̰̘͈͉͕̲̙͖̹̻̪͐́̕g̴̢̛͔̘͉̪̮̒̒̄͋́̿̅͂̕͜͠ő̴͓̻͈̪̣̤̱̜͂̋͜͜ ̴̮̳̯̖̪̬̪́T̸̮͖̭͈́ę̵̛̛̛̮̟͖͖̱̖̋̒͑̊̾̌̇̓͐x̵̯̝̝̭͎̮̥͔̞́̒͗͒̌͜t̷̢̠̗̲͇̯̜̹͇̅͋̓̓́́̀́̋͠" b\\
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
		l" Simplified/Traditional/Shinjitai" /title
		vt{{
		    l" • " l" Unicode–Fuckup: Sollte in den Font, nicht in den Zeichensatz" b\\
		    l" • " l" Gleiche Zeichen unterscheiden sich je nach Land" b\\
		    l" • " l" Die Leute wollen sehen, was sie jeweils gewohnt sind" b\\
		    l" • " l" Praktisch ungeordnet als Sack CJ–Glyphen im Unicode" b\\
		    l" • " l" Hangul (nicht verwandt) wenigstens als ein Block" b\\
		    \skip
		    l" • " l" Eingebaut: Konversion SC↔TC" b\\
		    l" • " l" Japanische Varianten fehlen teilweise" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Decentralized Censorship" /title
		l" Make net2o a better place" /subsection
		vt{{
		    l" • " l" Internal, not external censorship" b\\
		    l" • " l" Disinfodemic in a peer2peer network similar to pandemic models" b\\
		    l" • " l" Filtering on incoming content, not your own content" b\\
		    l" • " l" Sender does not know that content is blocked" b\\
		    l" • " l" Different settings possible:" b\\
		}}vt
		vt{{
		    l"   1. " l" Filter hides messages" b\\
		    l"   2. " l" Filter doesn’t transmit messages" b\\
		    l"   3. " l" Both (“sterile immunity”)" b\\
		}}vt
		vt{{
		    l" • " l" Typical fanout of participants = R₀" b\\
		    l" • " l" If more than 1–1/R₀ filter, bad contents doesn’t get far" b\\
		    l" • " l" Requires easy filter sharing" b\\
		}}vt
		glue*lll }}glue
	    }}v box[] >bdr
	}}z box[] /flip dup >slides
	
	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Disinfodemic" /title
		l" Examples from the Covid pandemic" /subsection
		vt{{
		    l" 李文亮 " l" Was gag ordered by Wuhan police when the main news (新闻联播)" b\\
		    l"  " l" already had a report.  “Would not happen here”" b\\
		    \skip
		    l" Here? " l" Instead, a hell lot of disinformation spread out in the free west" b\\
		    \skip
		    l" Evil Govt " l" Yes, the government is evil.  But also incompetent." b\\
		    l"  " l" And its bias is pro corporations.  Evilness serves a purpose." b\\
		    \skip
		    l" Science? " l" Science questions everything.  But it conducts experiments to check." b\\
		    l"  " l" Masks work.  Lockdowns work.  Wuhan lab didn’t leak.  Vaccines are safe." b\\
		    l"  " l" Ivermectin/Chloroquin/Vitamine D are no miracle cure." b\\
		    \skip
		    l" Massacre " l" The failure (willful/incompetent) to contain Covid-19 is a massacre." b\\
		    l"  " l" Democraties can do such atrocities only with massive disinformation." b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Manual moderation?" /title
		l" Too late, too little" /subsection
		vt{{
		    l" • " l" Delete bad content" b\\
		    l" • " l" Leave the corrections" b\\
		    l" • " l" Block the bad actors" b\\
		    l" • " l" In a P2P network, people can block the moderators" b\\
		    l" • " l" So a rough consensus is needed" b\\
		    \skip
		    l" • " l" Manual interaction is too slow" b\\
		    l" • " l" People don’t read rectifications" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Automatic filter?" /title
		l" Actually the hard problem" /subsection
		vt{{
		    l" Texts: " l" Bad texts (equals PCR test)" b\\
		    l" + " l" Easy to implement" b\\
		    l" – " l" Easy to defeat, easy to be false positive" b\\
		    \skip
		    l" Images: " l" Fingerprints" b\\
		    l" + " l" Medium difficulty to implement" b\\
		    l" – " l" Easy to defeat, easy to generate pre–image attacks" b\\
		    \skip
		    l" Audio: " l" Speech to text" b\\
		    l" + " l" Medium difficulty to implement" b\\
		    l" – " l" Defeat is unclear, easy to generate pre–image attacks" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" What kind of bad text?" /title
		l" It’s not bad words" /subsection
		vt{{
		    l" • " l" Teh spellink is awefull" b\\
		    l" • " l" SHOUTING ALL THE TIME" b\\
		    l" • " l" Number (and color) of exclamation marks‼️‼️" b\\
		    \skip
		    l" • " l" This sounds like easy to defeat — for smart people" b\\
		    l" • " l" Smart people are rarely the problem…" b\\
		    \skip
		    l" • " l" This is porn according to AI:" b\\
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
		    l" Filter Algorithms in Real Life" /title
		}}v box[] >bdr
		tex: perscheid-gesperrt
		tex: perscheid-flacherdler
		\normal \sans
		{{
		    ' perscheid-gesperrt "perscheid-gesperrt.jpg" 0.60e 896 640 fm*/ }}image-file drop 
		    {{
			l" • " l" Algorithm based on words" b\\
			l" • " l" Click worker paid per case" b\\
			l" • " l" Weird rules" b\\
			l" • " l" Want to keep the idiots" b\\
			l"  " l" Because they click the ads" b\\
			l"  " l" which are frauds…" b\\
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
    net2o-img drop  logo-img
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
