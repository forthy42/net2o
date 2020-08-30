#! /usr/bin/env gforth-fast
\ Presentation on CloudCalypse

\ Copyright © 2020 Bernd Paysan

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

tex: net2o-logo
tex: 36c3-logo
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
		{{
		    glue*l }}glue
		    l" Social Distancing with net2o" /title
		    l" Plans and progress on video conferencing" /subtitle
		    glue*2 }}glue	
		    l" Bernd Paysan" /author
		    l" EuroForth 2020. Video Conference" /location
		    {{
			glue*l }}glue
			{{
			    glue*l }}glue author#
			    {{
				glue*l }}glue
				\tiny l" Photo: Ralph W. Lambrecht" }}text'
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
		l" 9 Months since COVID–19" /title
		l" Surveillance for epidemics control" \\
		\skip
		l" Politics" /subsection
		{{
		    l"   Smartphone location tracking for contact tracing" \\
		    l"   China: Contact state (red/green)" \\
		    l"   China: QR code for entrance" \\
		    l"   Singapore/EU: bluetooth based tracing" \\
		    l"   China: Virus on cash ➡ pay with WeChat/AliPay" \\
		    l"   Everywhere: Home Office, Video Conferences" \\
		}}v box[]
		\skip
		l" Progress" /subsection
		l"   Nothing of this is ready with net2o" \\
		\skip
		l" Permanent Record" /subsection
		l"   BTW, Snowden wrote a book" \\
		glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
	    }}v box[] >o o Value snowden-page font-size# to border o o>
	}}z box[] /flip dup >slides
	
	\ page 3
	{{
	    $3F0000FF $FFAAAAFF pres-frame
	    {{
		glue*l }}glue
		tex: snowden
		' snowden "snowden.png" 0.666e }}image-file drop /right
		glue*l }}glue
	    }}v >o font-size# to border o o>
	    {{
		l" Permanent Record" /title
		l" About social networks" /subsection
		\italic
		l" … Few of us understood it at that time, but none of the things that we’d go on to share would belong to us anymore. The successors to the e–commerce companies that had failed because they couldn’t find anything we were interested in buying now had a new product to sell." p\\
		\skip
		l"   That new product was Us." p\\
		l"   Our attention, our activities, our locations, our desires—everything about us that we revealed, knowingly or not, was being surveilled and sold in secret, so as to delay the inevitable feeling of violation that is, for most of us, coming only now. And this surveillance would go on to be actively encouraged, and even funded by an army of governments greedy for the vast volume of intelligence they would gain." p\\
		\regular \skip
		l" Edward Snowden" }}text' /right
		\skip
		l" Note: libertarian framing? corporate vs. government evilness" p\\
		glue*l }}glue
	    }}v box[] >o o Value snowden-page2 font-size# to border o o>
	}}z box[] /flip dup >slides

	\ page 3
	{{
	    $3F0000FF $FFAAAAFF pres-frame
	    {{
		l" How to Destroy Surveillance Capitalism" /title
		l" by Cory Doctorow" /subsection
		l" Cory Doctorow published a lengthy book on OneZero, describing in great details how surveillance capitalism creates many of the adverse effects of current Internet, as side effect of selling ads to people as product" p\\
		\skip
		l"   Snowden made clear that the libertarian framing is not his thing." p\\
		\italic
		l"    “We have legalised the abuse of the person, and entrenched a system that makes populations vulnerable for the benefit of private companies.”" p\\
		l"    “The problem is not data protection. It is data collection. GDPR assumes the data was all collected properly in the first place. It is as if it is okay to spy on everyone, as long as the data never leaks. When it does, it is not data being exploited. It is people.”" p\\
		\skip
		l" Edward Snowden" }}text' /right
		\skip \regular
		glue*l }}glue
	    }}v box[] >o o Value snowden-page3 font-size# to border o o>
	}}z box[] /flip dup >slides

	\ page 4
	{{
	    $5F0000FF $FF7777FF pres-frame
	    {{
		l" Antisocial Hateworks" /title
		l" Problems with People since Eternal September" /subsection
		vt{{
		    l" Opinions " l" are not facts, but values people believe in" b\\
		    l" Beliefs "  l" are not up to discussion, but part of identity" b\\
		    l" Identity " l" is vigurously defended and used to segregate people" b\\
		    l" Walls "    l" are in the head, and tearing them down causes aggression" b\\
		    \skip
		    l" Free Speech " l" I’m more and more convinced, that “speech” is too" b\\
		    l" " l" generic." b\\
		    l" " l" Lies and deception need no protection." b\\
		    l" " l" Claims need proof and evidence." b\\
		    l" " l" Truth needs protection." b\\
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

	\ page 7
	{{
	    $202000FF $FFFFCCFF pres-frame
	    {{
		l" ToDo–Liste vom letzten Jahr" /title
		vt{{
		    l" + " l" Den bulk importer für Google+ fertig machen" b\\
		    l" – " l" Einen bulk importers für Facebook/Twitter/Blogger/etc." b\\
		    l" + " l" Avatare für die User–IDs" b\\
		    l" + " l" Markdown renderer" b\\
		    l" + " l" Album–Viewer" b\\
		    l" – " l" Film–Abspieler" b\\
		    l" – " l" Key handover für Kontakte in der net2o–Welt (temporare Schlüsselpaare)" b\\
		    l" + " l" Temporäre Keys als nicht vertrauenswürdig kennzeichnen" b\\
		}}vt
		glue*l }}glue
		l" Zur Demo" /subsection
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 8
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Neue Herausforderungen" /title
		l" Das hier ist Forschung, was wirklich gebraucht wird" /subsection
		vt{{
		    l" • " l" Wir brauchen einen Dark Mode ✅" b\\
		    l" • " l" Manche JPEGS haben keinen Thumbnail (epeg?)" b\\
		    l" • " l" Nicht–öffentliches Protokoll für „Wer hat was“ (✅⅞)" b\\
		    l" • " l" Komfortables ID–Cloning (siehe IETF MEDUP task group)" b\\
		    l" • " l" Berechtigungen für DVCS updates/posting&comment submission" b\\
		    l" • " l" Likes/+1s/etc.: nur der letzte zählt (pro user)" b\\
		    l" • " l" Geschlossene Group–Chats ✅" b\\
		    l" • " l" Berechtigung für Moderation" b\\
		    l" • " l" Teilbare Listen für Sammlungen/Gruppen" b\\
		    l" • " l" Was ist mit 53/80/443–only Netzwerken?" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 9
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" “Who has What”" /title
		l" Query object origin by hash" /subsection
		vt{{
		    l" ❓ " l" Ursprünglicher Plan: Hashes im DHT (wie BitTorrent)" b\\
		    l" ➡ " l" Anfragen verraten, wer was will" b\\
		    l" ❓ " l" Ursprünglicher Lösungsansatz: Hashes verschlüsseln" b\\
		    l" ➡ " l" Anfragen verraten, wer das gleiche Ding will" b\\
		    l" ❓ " l" Onion routing innerhalb des DHTs?" b\\
		    l" ➡ " l" Komplex, langsam" b\\
		    \skip
		    l" ➡ " l" Besser „wer hat was“ im Chat–Log mit abspeichern" b\\
		    l" ➡ " l" „Wer“ ist device.pubkey" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides	

	\ page 10
	{{
	    $200030FF $EECCFF pres-frame
	    {{
		l" Komfortables ID–cloning" /title
		l" Löst das Mehrgeräte–Problem" /subsection
		vt{{
		    l" ❓ " l" Kopiere geheimen und öffentlichen Schlüssel" b\\
		    l" ➡ " l" Autorisieren von Remote–File—Access" b\\
		    l" ❓ " l" Kann man das nicht in net2o selbst machen?" b\\
		    l" ➡ " l" Scan einen color–QR–code" b\\
		    l" ➡ " l" Schicke eine Einladung zurück" b\\
		    l" ➡ " l" Bekomme eine Bestätigung via color–QR–code" b\\
		    l" ➡ " l" Liefere einen Zero–Knowledge–Proof “Ich kenne das Password”" b\\
		    l" ➡ " l" Schicke die Schlüssel über die net2o–Verbindung" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides	

	\ page 11
	{{
	    $200030FF $EECCFF pres-frame
	    {{
		l" Web–only Netzwerke" /title
		l" Strenge Portfilter–Regeln, nur DNS+HTTP[S]" /subsection
		vt{{
		    l" ❓ " l" Man kann kein UDP als Overlay verwenden (auch DNS geht nur zum internen Resolver)" b\\
		    l" ➡ " l" braucth einen Transport–Layer über HTTPS" b\\
		    l" ❓ " l" Web Socket API?" b\\
		    l" ➡ " l" Angreifer könnte Verbindungen testen und net2o–Tunnel gezielt unterbinden" b\\
		    l" ➡ " l" Autorisierung im ersten Paket unterbringen" b\\
		    l" ➡ " l" Ändert das net2o connection setup" b\\
		    l" ➡ " l" Bonus: Ein RTD weniger für NAT traversal" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides	

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Datensparsames Tracking" /title
		l" Problem" /subsection
		vt{{
		    l" • " l" Für das Tracking braucht man eine global sichtbare Datenbank" b\\
		    l" • " l" Die Teilnehmer müssen informiert werden können" b\\
		    l" • " l" Aus der Datenbank darf aber so wenig wie möglich extrahierbar sein" b\\
		}}vt
		l" Lösungsansatz" /subsection
		vt{{
		    l" • " l" Pseudonymer Eintrag mit orts&zeitabhängigem Pseudonym" b\\
		    l" • " l" Eintrag mit Onion–Routing für die Antwort" b\\
		    l" • " l" Eintrag: Mixer-Key | Ephemeral Key | Verschlüsselte Message im gleichen Format" b\\
		    l" • " l" Exit–Node ist der Hausarzt (der kann deanonymisieren)" b\\
		}}vt
		glue*l }}glue
	    }}v box[] >bdr
	}}z box[] /flip dup >slides
	
	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Videostreaming/Videokonferenzen" /title
		vt{{
		    l" • " l" Gestreamte Videos sind keine fertigen (=kompletten) Dateien" b\\
		    l" • " l" Andere Herangehensweise beim Multiplexen nötig" b\\
		    l" • " l" Qualität ist abhängig von der erzielbaren Datenrate" b\\
		    l" • " l" Thumbnail/Fullscreen–Streams bei Konferenzen" b\\
		    l" • " l" Audio synchron für Filme/asynchron für Konferenzen" b\\
		}}vt
		glue*l }}glue
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Wunschzettel 1/2" /title
		vt{{
		    l" • " l" Stream von der Kamera, vertikaler Crop (Portrait)" b\\
		    l" • " l" Stream vom Desktop/Fenster" b\\
		    l" • " l" Herunterskaliert für alle (je mehr Teilnehmer, desto kleiner)" b\\
		    l" • " l" Hochskaliert für redende Teilnehmer" b\\
		    l" • " l" Zwei oder drei redende Teilnehmer nebeneinander" b\\
		    l" • " l" Audio mixer&autolevel" b\\
		    l" • " l" FFT vom Audio anzeigen für Visualisierung" b\\
		    l" • " l" Audio biquad filters, z.B. notch, um schlechte Audioqualität zu verbessern" b\\
		    l" • " l" Push to talk (hotkeys für alles)" b\\
		    l" • " l" Hotkey für cut mark + keyframe" b\\
		    l" • " l" Lokale Aufzeichnung mit besserer Auflösung/Audio–Qualität für den Präsentator" b\\
		    l" • " l" Mehrere Geräte (Kameras, Mics) für einen Teilnehmr (Akira Kurosawa–Setup)," b\\
		    l"  " l" kein eigenes Audio in der Ausgabe" b\\
		}}vt
		glue*l }}glue
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Wunschzettel 2/2" /title
		vt{{
		    l" • " l" Templates für schöne Präsentations–Aufzeichnungen" b\\
		    l" • " l" Logo für einen Talk setzen" b\\
		    l" • " l" Räume für kleine Seitengruppen–Gespräche" b\\
		    l" • " l" Verschiedene Verbindungen ausprobieren, und die besten behalten" b\\
		    l" • " l" Audio/Video getrennt an/ausschalten" b\\
		    l" • " l" Zusätzliche Audio-Quelle zumixen (Hintergrundmusik, Desktop, Klatschen/Lacher…)" b\\
		    l" • " l" Räumliches Audio (Gesprächspartner kommen von links oder von rechts)." b\\
		}}vt
		glue*l }}glue
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Nicht–technische Probleme" /title
		vt{{
		    l" • " l" Kontakte von net2o überzeugen" b\\
		    l" • " l" Wie macht man ein soziales Netzwerk wohnlich?" b\\
		    l" • " l" Finanzierung von net2o?" b\\
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
		}}vt
		glue*l }}glue
		tex: qr-code
		tex: qr-code-inv
		{{
		    glue*l }}glue
		    ' qr-code "qr-code.png" 12e }}image-file drop
		    qr-code nearest
		    glue*l }}glue
		    ' qr-code-inv "qr-code-inv.png" 12e }}image-file drop
		    qr-code-inv nearest
		    glue*l }}glue
		}}h
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
