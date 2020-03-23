\ Presentation on CloudCalypse

\ Copyright © 2018 Bernd Paysan

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
tex: 35c3-logo
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

$FFFFBBFF text-color: redish
$00CCCCFF dup text-emoji-color: blue-emoji#
m2c:shadow-color# @ color, FValue shadow-col#
dark-mode
m2c:shadow-color# @ re-color shadow-col#
$FFFFBBFF re-text-color redish
$00CCCCFF dup re-emoji-color blue-emoji#
light-mode

$10 stack: vp-tops

' }}i18n-text is }}text'

{{
    {{ glue-left }}glue
	
	\ page 0
	' blackish >body f@  ' dark-blue >body f@
	{{
	    $FFFFFF00 dup pres-frame
	    ' redish >body f@ ' dark-blue >body f!
	    ' blue-emoji# >body f@ ' blackish >body f!

	    tex: cloudcalypse
	    \ 1 ms
	    ' cloudcalypse "cloudcalypse-16-9.jpg" 2e 3e f/ }}image-file drop /center
	    {{
		glue*l }}glue
		tex: worms-wappen
		' worms-wappen "worms-wappen-swap.png" 0.5e }}image-file
		Constant coa-glue /center
\		glue*l }}glue
	    }}v box[]
	    {{
		{{
		    glue*l }}glue \ ) $CCDDDD3F color, 4e }}frame dup .button1
		    l" CloudCalypse, was nun?" /title
		    l" It looks like you’ve reached the end." /subtitle
		    l" Wie man Daten nach net2o importiert" /subtitle
		    {{
			{{ \tiny
			    glue*l }}glue
			    {{  nt
				{{ glue*lll }}glue l" ἀποκάλυψις" }}text' }}h bx-tab
				l"  ➡ " }}text'
				{{ l" Offenbarung" }}text' glue*lll }}glue }}h bx-tab
			    }}h /center
			    {{
				{{ glue*lll }}glue l" cloud[o]calypse" }}text' }}h bx-tab
				l"  ➡ " }}text'
				{{ l" σύννεφο καταστροφή" }}text' glue*lll }}glue }}h bx-tab
			    }}h /center
			    glue*l }}glue
			}}v box[]
			glue*2 }}glue
		    }}z box[]
		    l" Bernd Paysan" /author
		    l" Forth–Tagung 2019, Worms" /location
		    {{
			glue*l }}glue \ ) $CCDDDD3F color, 4e }}frame dup .button1
			{{
			    glue*l }}glue \ ) $CCDDDD3F color, 4e }}frame dup .button1
			    \tiny l" Photo: Ralph W. Lambrecht" }}text' /right \normal
			}}v box[]
		    }}z box[]
		tex: vp-title glue*l ' vp-title }}vp vp[] dup value title-vp
		>o 3 vp-shadow>># lshift to box-flags o o>
	    }}v box[] >o font-size# to border o Value title-page o o>
	}}z box[] dup >slides
	' dark-blue >body f!  ' blackish >body f!

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
    $3F0000FF $FF8888FF pres-frame
    {{
	l" 5 Jahre nach Snowden" /title
	l" Was hat sich getan?" \\
	\skip
	l" Politik" /subsection
	{{
	    l"   EU–Parlament möchte Upload-Filter" "🤦" e\\
	    l"   EU–Parlament führt Link–Steuer ein (stattdessen: “<right>”)" "🤦🤦" e\\
	    l"   EU–Parlament filtert „terroristische Inhalte“" "🤦🤦🤦" e\\
	    l"   Deutschland kopiert Chinas Cyberadministration CAC (Medienstaatsvertrag)" "🤦🤦🤦🤦" e\\
	    l"   Tor– und VPN–Verbot geplant" "🤦🤦🤦🤦🤦" e\\
	    l"   Seehofers neues Polizeistaatgesetz: Passwörter her!" "🤦🤦🤦🤦🤦🤦" e\\
	    l"   Deanonymisierung im österreichischen Internet" "🤦🤦🤦🤦🤦🤦🤦" e\\
	}}v box[]
	\skip
	\skip
	l" Fortschritt" /subsection
	l"   EGMR hat geurteilt: Massenüberwachung des GCHQs verletzt unsere Rechte" \\
	l"   net2o wird immer benutzbarer" \\
	glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
    }}v box[] >o o Value snowden-page font-size# to border o o>
}}z box[] /flip dup >slides

\ page 3
{{
    $201010FF $FFDDDDFF pres-frame
    {{
	glue*ll }}glue
	{{
	    glue*ll }}glue
	    tex: biggest-breaches
	    ' biggest-breaches "biggest-breaches.png" 0.666e }}image-file drop
	}}h box[]
    }}v box[] >bdr
    {{
	l" Cloud[o]Calypse" /title
	l" Irgendwas ist total kaputt in der Cloud [2]" /subtitle
	\skip
	l" Cloud kaputt?" /subsection
	l"   😱 Microsoft hat github gekauft (installiert gitlab–ce!)" \\
	l"   😱 Dropbox dropt Linux–Client (außer ext4 unverschlüsselt)" \\
	l"   😱 Facebook: Cambridge–Analytica–Scandal+viele API–Löcher [3]" \\
	l"   😱 Google+ hat zugemacht wegen API–Löchlein (beide User betroffen)" \\
	l"   😱 tumblr hat allen Pr0n gelöscht (übrig bleibt 1% contents)" \\
	l"   😱 Man kann nicht mehr auf Tinder daten, verletzt Facebooks policy" \\
	\skip
	l" Wurzel des Übels" /subsection
	l"   toxisches Werbe–Geschäftsmodell" \\
	l"   user+password authentication" \\
	l"   Deine Daten sind auf Computern anderer Leute" \\
	
	glue*l }}glue
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 4
{{
    $222222FF $DDDDDDFF pres-frame
    {{
	l" Werbe–Geschäftsmodell = toxisch?" /title
	vt{{
	    l" • " l" Anreiz, dich auf einer Seite zu halten" b\\
	    l" • " l" Beste Methode, dich zu halten: Kontroverse Diskussionen" b\\
	    l" 👎 " l" (fehlendes Dislike zwingt dich, bei Kontroversen zu kommentieren)" b\\
	    l" • " l" Anreiz, dich möglichst gut manipulieren zu können" b\\
	    l" • " l" Die schlimmsten “fake news” sind die Werbung selbst" b\\
	    l" • " l" Anreiz, viel über dich zu erfahren, um zielgerichtet zu werben" b\\
	    l" • " l" Lässt dich intimes Zeugs posten" b\\
	    l" • " l" Anreiz für Teilnehmer, Influencer—Marketing zu machen" b\\
	    \skip
	    l" Vorsicht: Gilt auch für TV und Print" \\
	}}vt
	glue*ll }}glue
    }}v box[] >bdr
    {{
	glue*ll }}glue \tiny \mono dark-blue
	{{ glue*ll }}glue l" 🔗" }}text' l" xkcd.com/386" }}text' _underline_ }}h
	[: s" xdg-open https://xkcd.com/386" system ;] 0 click[]
	tex: duty-calls \normal \sans
	' duty-calls "duty_calls.png" 0.95e }}image-file drop /right
    }}v box[] >bdr blackish
}}z box[] /flip dup >slides

\ page 5
{{
    $221100FF $FFEEDDFF pres-frame
    {{
	l" Zentralisiert/Föderiert/P2P?" /title
	vt{{
	    l" Zentralisiert" /subsection
	    l" + " l" Gute Finanzierung, robuste Hardware und Angriffsabwehr" b\\
	    l" – " l" Fehlende Privatsphäre, Honeypot, cative, EOL wenn der CEO will" b\\
	    l" – " l" Allerlei globale Zensur, toxisches Geschäftsmodell" b\\
	    l" Federated" /subsection
	    l" + " l" nicht captive, schwaches Geschäftsmodell" b\\
	    l" ± " l" Regionale Zensur (Knoten blacklisted, z.B. Lolicon Mastodon Knoten)" b\\
	    l" – " l" Schwache Finanzierung, schwache Hardware und Angriffsabwehr" b\\
	    l" – " l" Fehlende Privatsphäre, EOL der Knoten, wenn der Admin will" b\\
	    l" Peer2Peer" /subsection
	    l" + " l" Volle Kontrolle über deinen Knoten, gute Privatsphäre" b\\
	    l" + " l" Entwicklung muss finanziert werden, Rest billig" b\\
	    l" ± " l" Nicht—existierende Zensur (zieht Zensurflüchtlinge an)" b\\
	    l" – " l" Volle Verantwortung über deinen Knoten" b\\
	}}vt
	glue*ll }}glue
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 6
{{
    $000000FF $FFFFFFFF pres-frame
    {{
	l" Recht auf Datenübertragbarkeit" /title
	l" Art. 20 DSGVO" /subtitle
	\skip \footnote nt
	l" 1. " l" Die betroffene Person hat das Recht, die sie betreffenden personenbezogenen Daten, die sie einem Verantwortlichen bereitgestellt hat, in einem strukturierten, gängigen und maschinenlesbaren Format zu erhalten, und sie hat das Recht, diese Daten einem anderen Verantwortlichen ohne Behinderung durch den Verantwortlichen, dem die personenbezogenen Daten bereitgestellt wurden, zu übermitteln, sofern" p2\\ \skip
	l"   (a) " l" die Verarbeitung auf einer Einwilligung gemäß Artikel 6 Absatz 1 Buchstabe a oder Artikel 9 Absatz 2 Buchstabe a oder auf einem Vertrag gemäß Artikel 6 Absatz 1 Buchstabe b beruht und" p2\\
	l"   (b) " l" die Verarbeitung mithilfe automatisierter Verfahren erfolgt." p2\\ \skip 
	l" 2. " l" Bei der Ausübung ihres Rechts auf Datenübertragbarkeit gemäß Absatz 1 hat die betroffene Person das Recht, zu erwirken, dass die personenbezogenen Daten direkt von einem Verantwortlichen einem anderen Verantwortlichen übermittelt werden, soweit dies technisch machbar ist." p2\\ \skip
	l" 3. " l" ¹Die Ausübung des Rechts nach Absatz 1 des vorliegenden Artikels lässt Artikel 17 unberührt. ²Dieses Recht gilt nicht für eine Verarbeitung, die für die Wahrnehmung einer Aufgabe erforderlich ist, die im öffentlichen Interesse liegt oder in Ausübung öffentlicher Gewalt erfolgt, die dem Verantwortlichen übertragen wurde." p2\\ \skip
	l" 4. " l" Das Recht gemäß Absatz 1 darf die Rechte und Freiheiten anderer Personen nicht beeinträchtigen." p2\\
	glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 5
{{
    $202020FF $DDDDDDFF pres-frame
    {{
	l" Google+ JSON Takeout" /title
	\skip
	l" 🔗" l" https://takeout.google.com/settings/takeout" bm\\
	"https://takeout.google.com/settings/takeout" link[]
	glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
	tex: g+takeout
	' g+takeout "google-takeout.png" 1e }}image-file drop /center
	glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
    }}v box[] >bdr
}}z box[] /flip dup >slides
    
\ page 5
{{
    $202020FF $DDDDDDFF pres-frame
    {{
	l" Google+ JSON Takeout" /title
	\skip \mono \footnote !lit
	"~/Downloads/Takeout/Stream in Google+/Beiträge> cat '20181101 - +++ #net2o Import von Google+_ Avatare_.json'" p\\
	{{
	    {{
	"{" \\
	"  \"url\": \"https://plus.google.com/+BerndPaysan/posts/P6CiHfAJgpy\"," \\
	"  \"creationTime\": \"2018-11-01 17:51:40+0100\"," \\
	"  \"updateTime\": \"2018-11-01 17:51:40+0100\"," \\
	"  \"author\": {" \\
	"    \"displayName\": \"Bernd Paysan\"," \\
	"    \"profilePageUrl\": \"https://plus.google.com/+BerndPaysan\"," \\
	"    \"avatarImageUrl\": \"https://lh3.googleusercontent.com/a-/AN66SAyasgoOyZqe-kQqoDpoFmrBKAll3N1-jLFUel43iAg\\u003ds64-c\"," p\\
	"    \"resourceName\": \"users/114020517704693241828\"" \\
	"  }," \\
        "  \"content\": \"\\u003cb\\u003e+++ \\u003c/b\\u003e\\u003cb\\u003e\\u003ca rel\\u003d\\\"nofollow\\\" class\\u003d\\\"ot-hashtag bidi_isolate\\\" href\\u003d\\\"https://plus.google.com/s/%23net2o/posts\\\" \\u003e#net2o\\u003c/a\\u003e\\u003c/b\\u003e\\u003cb\\u003e Import von Google+: Avatare importieren +++\\u003c/b\\u003e\\u003cbr\\u003e\\u003cbr\\u003eDer Takeout von Google+ enthält nur die URLs der Avatare. Für einen vernünftig aussehenden Import ist der Avatar aber unverzichtbar. Und es sind nicht nur ein paar Avatare, mein Takeout hat über 4000 Avatar-URLs drin. Die Datenmenge hält sich in Grenzen, das sind 23MB. Wie importiert man die jetzt flott? Da das alles kleine Dateien sind, bestimmt die Latenz die Ladezeit — also müssen mehrere Verbindungen parallel geöffnet werden. Bei um die 32 Verbindungen habe ich derzeit das Maximum gesehen (etwas über eine Sekunde Download-Zeit für die 4000 Avatare), das ist sicher auch noch abhängig davon, was für eine Bandbreite man zur Verfügung hat — das ist jetzt am mit einem Gigabit angebundenen Server gemessen, auf einem Client am WLAN sieht man auch mit 16 Verbindungen keinen schnelleren Download. Ich splitte die Liste der herunterzuladenden Avatare also auf 128 Stück auf, und starte pro Liste einen parallelen curl-Prozess.\\u003cbr\\u003e\\u003cbr\\u003eZusätzlich haben die Dateien noch die großartige Eigenschaft, dass jeder Avatar als “photo.jpg” in der URL ist, was schon mal blöd ist, weil man keine Zuordnung von User-Profile zum Dateinamen hat, und zum zweiten, weil das in Wahrheit ein PNG ist, und kein JPEG (ja, wirklich!). Ich hätte gern die Dateien als \\u0026lt;user-id\\u0026gt;.png, danke. Gut, muss man also für jede URL noch ein -o konfigurieren.\\u003cbr\\u003e\\u003cbr\\u003eDann hat Google natürlich noch mehrere Server-Namen, um die Avatare aufzuteilen (4, um genau zu sein), und um die Verbindung wiederzuverwenden (curl kann sogar Pipelining!), muss man also Requests nach Server aufteilen. Der Einfachheit halber sortiere ich die Liste also vorher.\\u003cbr\\u003e\\u003cbr\\u003eUnd dann muss man natürlich noch warten, bis alle Prozesse wieder beendet sind, denn erst dann sind die Dateien ja da. Das geht aber zum Glück, dafür gibt es den Bash-Befehl wait. Damit man sieht, wie lange das dauert, mit time:\\u003cbr\\u003e\\u003cbr\\u003etime eval \\u0026#39;(for i in avatars.sh.*; do curl -s $(cat $i) \\u0026amp; done; wait)\\u0026#39;\\u003cbr\\u003e\\u003cbr\\u003eWie schon vorher angekündigt: Jeder fremde User bekommt ein vorläufiges Keypair (also eine ID), mit dem seine Messages signiert werden können. Und natürlich wird der Avatar Teil dieser ID. In net2o sind Objekte alle über Hashes indiziert, also auch diese Datei.\\u003cbr\\u003e\\u003cbr\\u003eIch musste dann noch das SAVE-KEYS anpassen, weil das alles “secret keys” sind, die aber nicht so behandelt werden dürfen. Der secret key ist da ja nur vorläufig drin.\\u003cbr\\u003e\\u003cbr\\u003eNatürlich werden schon heruntergeladene Avatare nur einmal heruntergeladen, d.h. wenn man den nächsten Import startet, nur für die neuen Kontakte. Die vorläufigen IDs werden deterministisch gebaut, d.h. die ändern sich auch nicht, wenn man mehrmals importiert.\\u003cbr\\u003e\\u003cbr\\u003eDamit ist der erste Schritt erledigt: IDs sind importiert. Mit diesen IDs kann ich dann den nächsten Schritt angreifen: Tatsächliche Postings importieren. Die müssen ja der jeweiligen ID zugeordnet werden.\"," p\\
        "  \"link\": {" \\
        "    \"title\": \"json/g+-schema.fs · master · Bernd Paysan / net2o\"," p\\
        "    \"url\": \"https://git.net2o.de/bernd/net2o/blob/master/json/g+-schema.fs\"," p\\
        "    \"imageUrl\": \"http://git.net2o.de/assets/gitlab_logo-7ae504fe4f68fdebb3c2034e36621930cd36ea87924c11ff65dbcb8ed50dca58.png\"" p\\
        "  }," \\
        "  \"resourceName\": \"users/114020517704693241828/posts/UgiEEMxaTyXK0ngCoAEC\"," p\\
        "  \"plusOnes\": [{" \\
        "    \"plusOner\": {" \\
        "      \"displayName\": \"Alexander Nolting\"," \\
        "      \"profilePageUrl\": \"https://plus.google.com/+AlexanderNolting\"," p\\
        "      \"avatarImageUrl\": \"https://lh3.googleusercontent.com/a-/AN66SAznEomPiCcn4UwcKFyxeN_PF8MZ4OfR_eBAk_71OQ\\u003ds64-c\"," p\\
        "      \"resourceName\": \"users/109141459210065659338\"" \\
        "    }" \\
        "  }, {" \\
        "    \"plusOner\": {" \\
        "      \"displayName\": \"Michael Stuhr\"," \\
        "      \"profilePageUrl\": \"https://plus.google.com/100221681241123059187\"," p\\
        "      \"avatarImageUrl\": \"https://lh3.googleusercontent.com/a-/AN66SAypGjmduWzTrkGMuqsOM2WFbSCLCL5LpeMTriUNYQ\\u003ds64-c\"," p\\
        "      \"resourceName\": \"users/100221681241123059187\"" \\
        "    }" \\
        "  }, {" \\
        "    \"plusOner\": {" \\
        "      \"displayName\": \"Thomas Bindewald\"," \\
        "      \"profilePageUrl\": \"https://plus.google.com/111230804128406013164\"," p\\
        "      \"avatarImageUrl\": \"https://lh3.googleusercontent.com/a-/AN66SAxVa3SNIL9rWdnxffPfWBpKhYZDZzSwfX8HtMjIyXs\\u003ds64-c\"," p\\
        "      \"resourceName\": \"users/111230804128406013164\"" \\
        "    }" \\
        "  }, {" \\
        "    \"plusOner\": {" \\
        "      \"displayName\": \"Christoph S\"," \\
        "      \"profilePageUrl\": \"https://plus.google.com/+ChristophS\"," \\
        "      \"avatarImageUrl\": \"https://lh3.googleusercontent.com/a-/AN66SAyVPtuSrWHDhSNA6dy0TkdVcVJYiXYQZWZfdRAh7Q8\\u003ds64-c\"," p\\
        "      \"resourceName\": \"users/109481623926683998721\"" \\
        "    }" \\
        "  }]," \\
        "  \"postAcl\": {" \\
        "    \"collectionAcl\": {" \\
        "      \"collection\": {" \\
        "        \"resourceName\": \"collections/UWXXX\"," \\
        "        \"displayName\": \"Softwarethemen\"" \\
        "      }" \\
        "    }," \\
        "    \"isPublic\": true" \\
        "  }" \\
        "}" \\
	tex: vp-google+ glue*lll ' vp-google+ }}vp vp[] dup vp-tops >stack
	!i18n \sans \normal
	light-mode $DDDDDDFF color, dark-mode
	$202020FF color, fdup to slider-color to slider-fgcolor
	light-mode
	dup font-size# f2/ f2/ fdup vslider
	}}h box[]
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 5b
{{
    $202020FF $DDDDDDFF pres-frame
    {{
	l" Google+ JSON Takeout" /title
	\skip \mono \footnote !lit
	"~/Downloads/Takeout/Stream in Google+/Beiträge> cat '20181101 - +++ #net2o Import von Google+_ Avatare_.json'" p\\
	{{
	    {{
	"{" \\
	"  \"url\": \"https://plus.google.com/+BerndPaysan/posts/P6CiHfAJgpy\"," \\
	"  \"creationTime\": \"2018-11-01 17:51:40+0100\"," \\
	"  \"updateTime\": \"2018-11-01 17:51:40+0100\"," \\
	"  \"author\": {" \\
	"    \"displayName\": \"Bernd Paysan\"," \\
	"    \"profilePageUrl\": \"https://plus.google.com/+BerndPaysan\"," \\
	"    \"avatarImageUrl\": \"https://lh3.googleusercontent.com/a-/AN66SAyasgoOyZqe-kQqoDpoFmrBKAll3N1-jLFUel43iAg\u003ds64-c\"," p\\
	"    \"resourceName\": \"users/114020517704693241828\"" \\
	"  }," \\
        "  \"content\": \"\u003cb\u003e+++ \u003c/b\u003e\u003cb\u003e\u003ca rel\u003d\\\"nofollow\\\" class\u003d\\\"ot-hashtag bidi_isolate\\\" href\u003d\\\"https://plus.google.com/s/%23net2o/posts\\\" \u003e#net2o\u003c/a\u003e\u003c/b\u003e\u003cb\u003e Import von Google+: Avatare importieren +++\u003c/b\u003e\u003cbr\u003e\u003cbr\u003eDer Takeout von Google+ enthält nur die URLs der Avatare. Für einen vernünftig aussehenden Import ist der Avatar aber unverzichtbar. Und es sind nicht nur ein paar Avatare, mein Takeout hat über 4000 Avatar-URLs drin. Die Datenmenge hält sich in Grenzen, das sind 23MB. Wie importiert man die jetzt flott? Da das alles kleine Dateien sind, bestimmt die Latenz die Ladezeit — also müssen mehrere Verbindungen parallel geöffnet werden. Bei um die 32 Verbindungen habe ich derzeit das Maximum gesehen (etwas über eine Sekunde Download-Zeit für die 4000 Avatare), das ist sicher auch noch abhängig davon, was für eine Bandbreite man zur Verfügung hat — das ist jetzt am mit einem Gigabit angebundenen Server gemessen, auf einem Client am WLAN sieht man auch mit 16 Verbindungen keinen schnelleren Download. Ich splitte die Liste der herunterzuladenden Avatare also auf 128 Stück auf, und starte pro Liste einen parallelen curl-Prozess.\u003cbr\u003e\u003cbr\u003eZusätzlich haben die Dateien noch die großartige Eigenschaft, dass jeder Avatar als “photo.jpg” in der URL ist, was schon mal blöd ist, weil man keine Zuordnung von User-Profile zum Dateinamen hat, und zum zweiten, weil das in Wahrheit ein PNG ist, und kein JPEG (ja, wirklich!). Ich hätte gern die Dateien als \u0026lt;user-id\u0026gt;.png, danke. Gut, muss man also für jede URL noch ein -o konfigurieren.\u003cbr\u003e\u003cbr\u003eDann hat Google natürlich noch mehrere Server-Namen, um die Avatare aufzuteilen (4, um genau zu sein), und um die Verbindung wiederzuverwenden (curl kann sogar Pipelining!), muss man also Requests nach Server aufteilen. Der Einfachheit halber sortiere ich die Liste also vorher.\u003cbr\u003e\u003cbr\u003eUnd dann muss man natürlich noch warten, bis alle Prozesse wieder beendet sind, denn erst dann sind die Dateien ja da. Das geht aber zum Glück, dafür gibt es den Bash-Befehl wait. Damit man sieht, wie lange das dauert, mit time:\u003cbr\u003e\u003cbr\u003etime eval \u0026#39;(for i in avatars.sh.*; do curl -s $(cat $i) \u0026amp; done; wait)\u0026#39;\u003cbr\u003e\u003cbr\u003eWie schon vorher angekündigt: Jeder fremde User bekommt ein vorläufiges Keypair (also eine ID), mit dem seine Messages signiert werden können. Und natürlich wird der Avatar Teil dieser ID. In net2o sind Objekte alle über Hashes indiziert, also auch diese Datei.\u003cbr\u003e\u003cbr\u003eIch musste dann noch das SAVE-KEYS anpassen, weil das alles “secret keys” sind, die aber nicht so behandelt werden dürfen. Der secret key ist da ja nur vorläufig drin.\u003cbr\u003e\u003cbr\u003eNatürlich werden schon heruntergeladene Avatare nur einmal heruntergeladen, d.h. wenn man den nächsten Import startet, nur für die neuen Kontakte. Die vorläufigen IDs werden deterministisch gebaut, d.h. die ändern sich auch nicht, wenn man mehrmals importiert.\u003cbr\u003e\u003cbr\u003eDamit ist der erste Schritt erledigt: IDs sind importiert. Mit diesen IDs kann ich dann den nächsten Schritt angreifen: Tatsächliche Postings importieren. Die müssen ja der jeweiligen ID zugeordnet werden.\"," p\\
        "  \"link\": {" \\
        "    \"title\": \"json/g+-schema.fs · master · Bernd Paysan / net2o\"," p\\
        "    \"url\": \"https://git.net2o.de/bernd/net2o/blob/master/json/g+-schema.fs\"," p\\
        "    \"imageUrl\": \"http://git.net2o.de/assets/gitlab_logo-7ae504fe4f68fdebb3c2034e36621930cd36ea87924c11ff65dbcb8ed50dca58.png\"" p\\
        "  }," \\
        "  \"resourceName\": \"users/114020517704693241828/posts/UgiEEMxaTyXK0ngCoAEC\"," p\\
        "  \"plusOnes\": [{" \\
        "    \"plusOner\": {" \\
        "      \"displayName\": \"Alexander Nolting\"," \\
        "      \"profilePageUrl\": \"https://plus.google.com/+AlexanderNolting\"," p\\
        "      \"avatarImageUrl\": \"https://lh3.googleusercontent.com/a-/AN66SAznEomPiCcn4UwcKFyxeN_PF8MZ4OfR_eBAk_71OQ\u003ds64-c\"," p\\
        "      \"resourceName\": \"users/109141459210065659338\"" \\
        "    }" \\
        "  }, {" \\
        "    \"plusOner\": {" \\
        "      \"displayName\": \"Michael Stuhr\"," \\
        "      \"profilePageUrl\": \"https://plus.google.com/100221681241123059187\"," p\\
        "      \"avatarImageUrl\": \"https://lh3.googleusercontent.com/a-/AN66SAypGjmduWzTrkGMuqsOM2WFbSCLCL5LpeMTriUNYQ\u003ds64-c\"," p\\
        "      \"resourceName\": \"users/100221681241123059187\"" \\
        "    }" \\
        "  }, {" \\
        "    \"plusOner\": {" \\
        "      \"displayName\": \"Thomas Bindewald\"," \\
        "      \"profilePageUrl\": \"https://plus.google.com/111230804128406013164\"," p\\
        "      \"avatarImageUrl\": \"https://lh3.googleusercontent.com/a-/AN66SAxVa3SNIL9rWdnxffPfWBpKhYZDZzSwfX8HtMjIyXs\u003ds64-c\"," p\\
        "      \"resourceName\": \"users/111230804128406013164\"" \\
        "    }" \\
        "  }, {" \\
        "    \"plusOner\": {" \\
        "      \"displayName\": \"Christoph S\"," \\
        "      \"profilePageUrl\": \"https://plus.google.com/+ChristophS\"," \\
        "      \"avatarImageUrl\": \"https://lh3.googleusercontent.com/a-/AN66SAyVPtuSrWHDhSNA6dy0TkdVcVJYiXYQZWZfdRAh7Q8\u003ds64-c\"," p\\
        "      \"resourceName\": \"users/109481623926683998721\"" \\
        "    }" \\
        "  }]," \\
        "  \"postAcl\": {" \\
        "    \"collectionAcl\": {" \\
        "      \"collection\": {" \\
        "        \"resourceName\": \"collections/UWXXX\"," \\
        "        \"displayName\": \"Softwarethemen\"" \\
        "      }" \\
        "    }," \\
        "    \"isPublic\": true" \\
        "  }" \\
        "}" \\
	tex: vp-google2+ glue*lll ' vp-google2+ }}vp vp[] dup vp-tops >stack
	    !i18n \sans \normal
	    dup font-size# f2/ f2/ fdup vslider
	}}h box[]
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 6
{{
    $000040FF $CCCCFFFF pres-frame
    {{
	l" Facebook JSON takeout" /title
	\skip \mono \footnote !lit
	"~/Downloads/Facebook/posts> cat your_posts.json" \\
	{{
	    {{
		"{" \\
		"  \"status_updates\": [" \\
		"    {" \\
		"      \"timestamp\": 1539297571," \\
		"      \"attachments\": [" \\
		"        {" \\
		"          \"data\": [" \\
		"            {" \\
		"              \"media\": {" \\
		"                \"uri\": \"photos_and_videos/videos/10000000_1829816733782306_2429950629012045824_n_10215835485911416.mp4\"," p\\
		"                \"creation_timestamp\": 1539297649," \\
        "                \"media_metadata\": {" \\
        "                  \"video_metadata\": {" \\
        "                    \"upload_timestamp\": 0," \\
        "                    \"upload_ip\": \"2001:16b8:26b2:9400:3280:72f1:d48d:358b\"" \\
        "                  }" \\
        "                }," \\
        "                \"thumbnail\": {" \\
        "                  \"uri\": \"photos_and_videos/thumbnails/43232013_10215835494711636_2757553898079125504_n.jpg\"" p\\
        "                }," \\
        "               \"comments\": [" \\
        "                  {" \\
        "                    \"timestamp\": 1539298102," \\
        "                    \"comment\": \"Der DCMA-Content-Filter hat die zwei Lieder angemeckert, die das Foto-Institut da draufgelegt hat, und das Video in Teilen der Welt stummgeschaltet.\"," p\\
        "                    \"author\": \"Bernd Paysan\"" \\
        "                  }" \\
        "                ]," \\
        "                \"title\": \"Wedding Photos\"," \\
        "                \"description\": \"Ich hab' noch gar kein Video in meiner Zeitleiste. Deshalb hier die Wedding-Fotos.\"" p\\
        "              }" \\
        "            }" \\
        "          ]" \\
        "        }" \\
        "      ]," \\
        "      \"data\": [" \\
        "        {" \\
        "          \"post\": \"Ich hab' noch gar kein Video in meiner Zeitleiste. Deshalb hier die Wedding-Fotos.\"" p\\
        "        }" \\
        "      ]" \\
        "    }," \\
	    tex: vp-facebook glue*lll ' vp-facebook }}vp vp[] dup vp-tops >stack
	    !i18n \sans \normal
	    light-mode
	    $CCCCFFFF color, fdrop
	    dark-mode
	    $000040FF color, fdup to slider-color to slider-fgcolor
	    light-mode
	    dup font-size# f2/ f2/ fdup vslider
	}}h box[]
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 7
{{
    $202020FF $DDDDDDFF pres-frame
    {{
	l" Twitter JSON takeout" /title
	\skip \mono \footnote !lit
	"~/Downloads/Twitter> cat tweet.js " \\
	{{
	    {{
		"window.YTD.tweet.part0 = [ {" \\
		"  \"retweeted\" : false," \\
		"  \"source\" : \"<a href=\\\"https://mobile.twitter.com\\\" rel=\\\"nofollow\\\">Twitter Lite</a>\"," p\\
		"  \"entities\" : {" \\
		"    \"hashtags\" : [ ]," \\
		"    \"symbols\" : [ ]," \\
		"    \"user_mentions\" : [ {" \\
        "      \"name\" : \"daimbag101\"," \\
        "      \"screen_name\" : \"marco_keule\"," \\
        "      \"indices\" : [ \"0\", \"12\" ]," \\
        "      \"id_str\" : \"3353806857\"," \\
        "      \"id\" : \"3353806857\"" \\
        "    }, {" \\
	"      \"name\" : \"Karl Lauterbach\"," \\
	"      \"screen_name\" : \"Karl_Lauterbach\"," \\
	"      \"indices\" : [ \"13\", \"29\" ]," \\
	"      \"id_str\" : \"3292982985\"," \\
	"      \"id\" : \"3292982985\"" \\
	"    } ]," \\
        "    \"urls\" : [ ]" \\
        "  }," \\
        "  \"display_text_range\" : [ \"0\", \"104\" ]," \\
        "  \"favorite_count\" : \"0\"," \\
        "  \"in_reply_to_status_id_str\" : \"1049587076797214720\"," \\
        "  \"id_str\" : \"1049599508122865664\"," \\
        "  \"in_reply_to_user_id\" : \"3353806857\"," \\
        "  \"truncated\" : false," \\
        "  \"retweet_count\" : \"0\"," \\
        "  \"id\" : \"1049599508122865664\"," \\
        "  \"in_reply_to_status_id\" : \"1049587076797214720\"," \\
        "  \"created_at\" : \"Tue Oct 09 09:56:38 +0000 2018\"," \\
        "  \"favorited\" : false," \\
        "  \"full_text\" : \"@marco_keule @Karl_Lauterbach Die AfD stößt selbst sehr viel Methan aus, wenn sie ihre Furze verbreiten.\"," p\\
        "  \"lang\" : \"de\"," \\
        "  \"in_reply_to_screen_name\" : \"marco_keule\"," \\
        "  \"in_reply_to_user_id_str\" : \"3353806857\"" \\
        "}, {" \\
	tex: vp-twitter glue*lll ' vp-twitter }}vp vp[] dup vp-tops >stack
	!i18n \sans \normal
	light-mode $DDDDDDFF color, dark-mode
	$202020FF color, fdup to slider-color to slider-fgcolor
	dup font-size# f2/ f2/ fdup vslider
	}}h box[]
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 8
{{
    $202020FF $DDDDDDFF pres-frame
    {{
	l" Blogger Atom feed takeout" /title
	\skip \mono \footnote !lit
	"~/Downloads/Takeout/Blogger/Blogs/Bernds Blog> cat feed.atom " \\
	{{
	    {{
        "<?xml version='1.0' encoding='utf-8'?>" \\
        "<feed xmlns='http://www.w3.org/2005/Atom' xmlns:blogger='http://schemas.google.com/blogger/2018'>" p\\
        "  <id>tag:blogger.com,1999:blog-408168245790957392</id>" \\
        "  <title>Bernds Blog</title>" \\
        "  <entry>" \\
        "    <id>tag:blogger.com,1999:blog-408168245790957392.post-94038096732765326</id>" p\\
        "    <blogger:type>POST</blogger:type>" \\
        "    <blogger:status>LIVE</blogger:status>" \\
        "    <author>" \\
        "      <name>Bernd</name>" \\
        "      <uri>https://plus.google.com/114020517704693241828</uri>" \\
        "      <blogger:type>BLOGGER</blogger:type>" \\
        "    </author>" \\
        "    <title>Nach Suzhou</title>" \\
        "    <content type='html'>&lt;div dir=\"ltr\"&gt;[...]" \\
        "    </content>" \\
        "    <blogger:created>2011-10-08T19:40:53.278Z</blogger:created>" \\
        "    <published>2011-05-11T19:46:00Z</published>" \\
        "    <updated>2011-11-11T19:26:59.997Z</updated>" \\
        "    <blogger:location>" \\
        "      <blogger:name>Suzhou, Jiangsu, Volksrepublik China</blogger:name>" p\\
        "      <blogger:latitude>31.298886</blogger:latitude>" \\
        "      <blogger:longitude>120.585316</blogger:longitude>" \\
        "      <blogger:span>0.434161,0.631714</blogger:span>" \\
        "    </blogger:location>" \\
        "    <blogger:filename>/2011/10/nach-suzhou.html</blogger:filename>" p\\
        "  </entry>" \\
		tex: vp-blogger glue*lll ' vp-blogger }}vp vp[] dup vp-tops >stack
	    !i18n \sans \normal
	    dup font-size# f2/ f2/ fdup vslider
	}}h box[]
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 9
{{
    $101010FF $EEEEEEFF pres-frame
    {{
	l" Was braucht man für den Importer?" /title
	vt{{
	    l" • " l" JSON parser, XML parser, HTML parser" b\\
	    l" • " l" JSON/XML schemas für alle Exports" b\\
	    l" • " l" HTML nach Markdown Konverter" b\\
	    l" • " l" Downloader für die fehlenden Teile (z.B. Avatare)" b\\
	    l" • " l" Temporäre Secrets für alle anderen Autoren/Kommentatoren" b\\
	}}vt
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 10
{{
    $200020FF $FFDDFFFF pres-frame
    {{
	l" Aufbau eines Soziales Netzwerk in net2o" /title
	vt{{
	    l" Texte " l" als markdown" b\\
	    l" Bilder " l" JPEG, PNG" b\\
	    l" Videos " l" mkv/webm" b\\
	    l" Timeline " l" Chat log mit Links auf DVCS–Projekt" b\\
	    l" Posting " l" DVCS–Projekt, hält Daten+Kommentare zusammen" b\\
	    l" DVCS–Projekt " l" Chat log mit Links auf patchset/snapshot" b\\
	    l" Teilen " l" Fork+posting+log message in eigener Timeline" b\\
	    l" Kommentar " l" Checkin von weiterem Posting" b\\
	    l" Likes " l" Chat messages mit Emoji direkt ins DVCS–Projekt" b\\
	}}vt
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 11
{{
    $202000FF $FFFFDDFF pres-frame
    {{
	l" Status" /title
	vt{{
	    l" + " l" Bulk importer für Google+" b\\
	    l" – " l" Bulk importer für Facebook/Twitter/Blogger/etc." b\\
	    l" + " l" Avatare, um die User–ID anzuzeigen" b\\
	    l" + " l" Markdown renderer" b\\
	    l" – " l" Album–Betrachter" b\\
	    l" – " l" Video–Player" b\\
	    l" – " l" Temporäres Schlüsselpaar einem Kontakt zuweisen" b\\
	    l" + " l" Temporäre Keys nicht vertrauenswürdig markieren" b\\
	}}vt
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

\ page 13
{{
    $000000FF $FFFFFFFF pres-frame
    {{
	l" Literatur & Links" /title \small
	vt{{
	    l" Bernd Paysan  " l" net2o fossil repository" bi\\
	    l" 🔗" l" https://net2o.de/" bm\\
	    "https://net2o.de/" link[]
	    l" Information  " l" World's Biggest Data Breaches & Hacks" bi\\
	    l" is beautiful 🔗" l" https://informationisbeautiful.net/visualizations/" bm\\
	    "https://informationisbeautiful.net/visualizations/worlds-biggest-data-breaches-hacks/" link[]
	    l"  " l" worlds-biggest-data-breaches-hacks/" bm\\
	    "https://informationisbeautiful.net/visualizations/worlds-biggest-data-breaches-hacks/" link[]
	    l" Marvin Strathmann  " l" Hallo Mark, viel Spaß mit Deinen Erinnerungen auf Facebook 2018" bi\\
	    l" 🔗" l" https://heise.de/-4254681" bm\\
	    "https://heise.de/-4254681" link[]
	}}vt
	glue*l }}glue
	tex: qr-code
	' qr-code "qr-code-inv.png" 12e }}image-file drop /center
	qr-code nearest
	glue*l }}glue
    }}v box[] >bdr
}}z box[] /flip dup >slides

' }}text is }}text'

\ end
glue-right }}glue
}}h box[]
net2o-img drop  logo-img
}}z slide[]
to top-widget

also opengl

: !widgets ( -- )
    set-fullscreen-hint 1 set-compose-hint
    top-widget .htop-resize
    vp-tops get-stack 0 ?DO  .vp-top  LOOP
    1e ambient% sf! set-uniforms ;

[IFDEF] writeout-en
    lsids ' .lsids s" ef2018/en" r/w create-file throw
    dup >r outfile-execute r> close-file throw
[THEN]

previous

also [IFDEF] android android [THEN]

: presentation ( -- )
    1config
    [IFDEF] hidestatus hidekb hidestatus [THEN]
    !widgets widgets-loop ;

previous

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
