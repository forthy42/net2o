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
' 36c3-logo "36c3-logo.png" 0.444e }}image-file 2Constant 36c3-img

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
		    l" CloudCalypse — Coronavirus Edition" /title
		    l" Social Distancing mit net2o" /subtitle
		    glue*2 }}glue	
		    l" Bernd Paysan" /author
		    l" Virtuelle Forth–Tagung 2020, Internet" /location
		    {{
			glue*l }}glue
			{{
			    glue*l }}glue author#
			    \tiny l" Photo: Ralph W. Lambrecht" }}text' /right \normal blackish
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
		l" 3 Monate seit COVID–19" /title
		l" Überwachung zur Seuchenbekämpfung" \\
		\skip
		l" Politik" /subsection
		{{
		    l"   China/Südkorea/Singapur: Handyortung per Smartphone zum Tracking" \\
		    l"   China: Kontaktstatus (grün/rot)" \\
		    l"   China: QR–Code beim Eintritt" \\
		    l"   Singapur: Bluetooth zum Tracing" \\
		    l"   China: Viren auf Bargeld ➡ alles bargeldlos" \\
		    l"   Überall: Home Office, Videokonferenzen" \\
		}}v box[]
		\skip
		l" Fortschritt" /subsection
		l"   Nichts davon gibt’s schon fertig bei net2o" \\
		\skip
		l" Permanent Record" /subsection
		l"   BTW, Snowden hat ein Buch geschrieben" \\
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
		l" Über soziale Netzwerke" /subsection
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
		    l" ❓ " l" Original plan: keep hashes in DHT" b\\
		    l" ➡ " l" Query reveals who wants what" b\\
		    l" ❓ " l" Original solution: Encrypt hashes" b\\
		    l" ➡ " l" Query reveals who wants/has the same thing" b\\
		    l" ❓ " l" Onion routing within DHT?" b\\
		    l" ➡ " l" Complex, slow" b\\
		    \skip
		    l" ➡ " l" Better keep “who has what” within the chat log structure" b\\
		    l" ➡ " l" “who” is device.pubkey" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides	

	\ page 10
	{{
	    $200030FF $EECCFF pres-frame
	    {{
		l" Comfortable ID cloning" /title
		l" solve the multi–device problem" /subsection
		vt{{
		    l" ❓ " l" Copy your secret+public keys" b\\
		    l" ➡ " l" You need authorized remote file access" b\\
		    l" ❓ " l" Establish authorization with net2o itself" b\\
		    l" ➡ " l" Scan a color–QR–code" b\\
		    l" ➡ " l" Send an invitation packet back" b\\
		    l" ➡ " l" Get a confirmation color–QR–Signature" b\\
		    l" ➡ " l" Do a zero–knowledge proof of “has the password”" b\\
		    l" ➡ " l" Send the keys over via that net2o connection" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides	

	\ page 11
	{{
	    $200030FF $EECCFF pres-frame
	    {{
		l" Web–only networks" /title
		l" Strict port filter policy, DNS+HTTP[S] only" /subsection
		vt{{
		    l" ❓ " l" Can not use UDP as overlay (DNS only to intern resolver)" b\\
		    l" ➡ " l" need a transport layer over HTTPS" b\\
		    l" ❓ " l" Web Socket API?" b\\
		    l" ➡ " l" Adversary may test connection and drop connections if net2o tunnel is detected" b\\
		    l" ➡ " l" Add authorization" b\\
		    l" ➡ " l" Requires single–package auth" b\\
		    l" ➡ " l" Change of net2o connection setup" b\\
		    l" ➡ " l" Bonus: one RTD less for NAT traversal, too" b\\
		}}vt
	    }}v box[] >bdr
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
		    l" • " l" Gestreamte Videos sind keine fertigen Dateien" b\\
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
    ( 36c3-img drop ) net2o-img drop  logo-img
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
