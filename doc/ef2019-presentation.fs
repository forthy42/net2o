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
' net2o-logo "net2o-200.png" 0.666e }}image-file 2Constant net2o-img

: logo-img ( o1 -- o o-img ) { rightimg }
    baseline# 0e to baseline#
    {{  {{ glue*ll }}glue rightimg }}h
    glue*l }}glue
    }}v >o font-size# f2/ to border o o>
    to baseline# ;

$FFFFBBFF text-color: redish
$00CCCCFF dup text-emoji-color: blue-emoji#
m2c:shadow-color# @ color, FValue shadow-col#
dark-gui
m2c:shadow-color# @ re-color shadow-col#
$FFFFBBFF re-text-color redish
$00CCCCFF dup re-emoji-color blue-emoji#
light-gui

glue new Constant glue*20l
glue*20l >o 1glue hglue-c glue! 0glue dglue-c glue! 1glue 20e f* vglue-c glue! o>

' }}i18n-text is }}text'

{{
    {{ glue-left @ }}glue
	
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
		glue*20l }}glue
		tex: wappen
		' wappen "hamburg-coa.png" 0.5e }}image-file
		Constant coa-glue /center
		glue*l }}glue
	    }}v box[]
	    {{
		{{
		    glue*l }}glue \ ) $CCDDDD3F color, 4e }}frame dup .button1
		    l" CloudCalypse" /title
		    l" building a social network on top of net2o," /subtitle
		    l" and importing your existing data" /subtitle
		    {{
			{{ \tiny
			    glue*l }}glue
			    {{  nt
				{{ glue*lll }}glue l" ἀποκάλυψις" }}text' }}h bx-tab
				l"  ➡ " }}text'
				{{ l" uncovering" }}text' glue*lll }}glue }}h bx-tab
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
		    l" EuroForth 2019, Hamburg" /location
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
	l" 6 Years after Snowden" /title
	l" What changed?" \\
	\skip
	l" Politics" /subsection
	{{
	    l"   EU parliament wants upload filters" "🤦" e\\
	    l"   EU parliament taxes the link (instead: “right”)" "🤦🤦" e\\
	    l"   EU parliament wants filtering “terrorist contents”" "🤦🤦🤦" e\\
	    l"   Germany wants a Cyberadministration like CAC (Medienstaatsvertrag)" "🤦🤦🤦🤦" e\\
	    l"   Tor– and VPN–ban planned" "🤦🤦🤦🤦🤦" e\\
	    l"   Seehofer plans reveal–your–password law" "🤦🤦🤦🤦🤦🤦" e\\
	    l"   And he wants Alexa’s “tapes” & disable E2E encryption [2]" "🤦🤦🤦🤦🤦🤦🤦" e\\
	}}v box[]
	\skip
	\skip
	l" Progress" /subsection
	l"   The ECHR ruled that GCHQ’s dragnet surveillances violates your rights" \\
	l"   net2o becomes more and more usable" \\
	glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
    }}v box[] >o o Value snowden-page font-size# to border o o>
}}z box[] /flip dup >slides

\ page 3
{{
    $201010FF $FFDDDDFF pres-frame
    {{
	l" Cloud[o]Calypse" /title
	l" something went terminally wrong in a cloud [3]" /subtitle
	\skip
	l" Clouds failing" /subsection
	l"   Microsoft bought github (install gitlab-ee)" \\
	l"   Dropbox dropped Linux client (except ext4 unencrypted)" \\
	l"   Facebook: Cambridge Analytica scandal+many API holes [4]" \\
	l"   Google+ closed due to possible leak (both users affected)" \\
	l"   tumblr deleted all porn (remains: 1% contents)" \\
	l"   Can't date on Tinder, dating violates Facebook’s policy" \\
	\skip
	l" Root causes" /subsection
	l"   toxic ad–based revenue model" \\
	l"   user+password authentication" \\
	l"   your data is on someone else’s computer" \\
	
	glue*l }}glue
    }}v box[] >bdr
    {{
	glue*ll }}glue
	{{
	    glue*ll }}glue
	    tex: biggest-breaches
	    ' biggest-breaches "biggest-breaches.png" 0.666e }}image-file drop
	}}h box[]
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 4
{{
    $222222FF $DDDDDDFF pres-frame
    {{
	l" Ad–based business = toxic?" /title
	vt{{
	    l" • " l" Incentive to keep you on the site" b\\
	    l" • " l" Best way to keep you: controversial discussion" b\\
	    l" 👎 " l" (no dislikes to force you to comment if you don't agree)" b\\
	    l" • " l" Incentive to make you easily manipulated" b\\
	    l" • " l" Worst “fake news” are indeed the ads themselves" b\\
	    l" • " l" Incentive to gather all kinds of information to target ads to you" b\\
	    l" • " l" Make you post about your private things" b\\
	    l" • " l" Incentive for participants to do influencer marketing" b\\
	    \skip
	    l" Beware: applies to journals and TV, too" \\
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
	l" Centralized/Federated/P2P?" /title
	vt{{
	    l" Centralized" /subsection
	    l" + " l" good funding, robust hardware and attack protection" b\\
	    l" – " l" lacks privacy, honeypot, captive, EOL at whim of CEO" b\\
	    l" – " l" diverse global censorship, possible toxic business model" b\\
	    l" Federated" /subsection
	    l" + " l" not captive, small business models" b\\
	    l" ± " l" regional censorship (nodes blacklisted, e.g. Lolicon Mastodon nodes)" b\\
	    l" – " l" poor funding, underpowered hardware/attack protection" b\\
	    l" – " l" lacks privacy, EOL of nodes at whim of node admin" b\\
	    l" Peer2Peer" /subsection
	    l" + " l" Full control over your node, good privacy" b\\
	    l" + " l" Development funding? Otherwise cheap" b\\
	    l" ± " l" non–existend censorship (attracts censorship refugees)" b\\
	    l" – " l" Full responsibility for your node" b\\
	}}vt
	glue*ll }}glue
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 6
{{
    $000000FF $FFFFFFFF pres-frame
    {{
	l" Right to data portability" /title
	l" Art. 20 GDPR" /subtitle
	\skip \footnote nt
	l" 1. " l" The data subject shall have the right to receive the personal data concerning him or her, which he or she has provided to a controller, in a structured, commonly used and machine-readable format and have the right to transmit those data to another controller without hindrance from the controller to which the personal data have been provided, where:" p2\\ \skip
	l"   (a) " l" the processing is based on consent pursuant to point (a) of Article 6(1) or point (a) of Article 9(2) or on a contract pursuant to point (b) of Article 6(1); and" p2\\
	l"   (b) " l" the processing is carried out by automated means." p2\\ \skip 
	l" 2. " l" In exercising his or her right to data portability pursuant to paragraph 1, the data subject shall have the right to have the personal data transmitted directly from one controller to another, where technically feasible." p2\\ \skip
	l" 3. " l" The exercise of the right referred to in paragraph 1 of this Article shall be without prejudice to Article 17. That right shall not apply to processing necessary for the performance of a task carried out in the public interest or in the exercise of official authority vested in the controller." p2\\ \skip
	l" 4. " l" The right referred to in paragraph 1 shall not adversely affect the rights and freedoms of others." p2\\
	glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 6
{{
    $200020FF $DDDDDDFF pres-frame
    {{
	l" net2o in a nutshell" /title
	l" net2o consists of the following 6 layers (implemented bottom up):" \\
	\skip
	{{
	    vt{{
		l" 2. " b0 blackish l" Path switched packets with 2" }}text'
		\italic l" n" }}smalltext \regular >o font-size# -0.4e f* to raise o o>
	    l"  size writing into shared memory buffers" }}text'  glue*l }}glue }}h box[] >bl
	    l" 3. " l" Ephemeral key exchange and signatures with Ed25519," b\\
	    l"  " l" symmetric authenticated encryption+hash+prng with Keccak," b\\
	    l"  " l" symmetric block encryption with Threefish" b\\
	    l"  " l" onion routing camouflage with Threefish/Keccak" b\\
	    l" 4. " l" Timing driven delay minimizing flow control" b\\
	    l" 5. " l" Stack–oriented tokenized command language" b\\
	    l" 6. " l" Distributed data (files, messages) and distributed metadata (DHT, DVCS)" b\\
	    l" 7. " l" Apps in a sandboxed environment for displaying content"
	    b\\
	}}vt
	glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
    }}v box[] >bdr
}}z box[] /flip drop \ dup >slides

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
	' g+takeout "google-takeout.png" 1.000e }}image-file drop /center
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
	    dark-gui $202020FF color, fdrop
	    light-gui $DDDDDDFF color,
	    fdup to slider-color to slider-fgcolor
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
		dark-gui $000040FF color, fdrop light-gui $CCCCFFFF color,
		fdup to slider-color to slider-fgcolor
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
		dark-gui $202020FF color, fdrop
		light-gui $DDDDDDFF color,
		fdup to slider-color to slider-fgcolor
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
		dark-gui $202020FF color, fdrop
		light-gui $DDDDDDFF color,
		fdup to slider-color to slider-fgcolor
		dup font-size# f2/ f2/ fdup vslider
	}}h box[]
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 9
{{
    $101010FF $EEEEEEFF pres-frame
    {{
	l" Things needed for import" /title
	vt{{
	    l" • " l" JSON parser, XML parser, HTML parser" b\\
	    l" • " l" JSON/XML schemas for all those exports" b\\
	    l" • " l" HTML to Markdown converter" b\\
	    l" • " l" Downloader for missing parts (e.g. avatar photos)" b\\
	    l" • " l" Temporary secret keys for all those other authors" b\\
	}}vt
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 10
{{
    $200020FF $FFDDFFFF pres-frame
    {{
	l" Social Networks in net2o" /title
	vt{{
	    l" Texts " l" as markdown" b\\
	    l" Images " l" JPEG, PNG" b\\
	    l" Movies " l" mkv/webm" b\\
	    l" Timeline " l" Chat log with link to DVCS project" b\\
	    l" Posting " l" DVCS project, keeping data+comments together" b\\
	    l" DVCS project " l" Chat log with link to patchsets/snapshots" b\\
	    l" Reshare " l" Fork+added posting+log message in own timeline" b\\
	    l" Comment " l" Fork+added posting+pull request" b\\
	    l" Likes " l" Chat log messages directly in DVCS project" b\\
	}}vt
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 11
{{
    $202000FF $FFFFDDFF pres-frame
    {{
	l" Status" /title
	vt{{
	    l" + " l" Bulk importer for Google+" b\\
	    l" – " l" Bulk importers for Facebook/Twitter/Blogger/etc." b\\
	    l" + " l" Use avatars to display users's ID" b\\
	    l" + " l" Markdown renderer" b\\
	    l" – " l" Album viewer" b\\
	    l" – " l" Movie player" b\\
	    l" – " l" Key handover to contact in net2o world (temporary keypair)" b\\
	    l" + " l" Mark imported keys as not trustworthy" b\\
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

\ page 6
{{
    $200020FF $DDDDDDFF pres-frame
    {{
	l" net2o: things to do" /title
	l" The incomplete list of incomplete things:" \\
	\skip
	vt{{
	    l" 1. " l" Shorter connection setup with NAT traversal" b\\
	    l" 2. " l" Two–stage DHT to separate identity from queries" b\\
	    l" 3. " l" Complete the payment system (+ add support for ticket systems)" b\\
	    l" 4. " l" GUI–only operation must be possible" b\\
	    l" 5. " l" Markdown to presentation converter" b\\
	}}vt
	glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 13
{{
    $000000FF $FFFFFFFF pres-frame
    {{
	l" Literatur & Links" /title \small
	vt{{
	    l" Bernd Paysan  " l" net2o fossil repository" bi\\
	    l" 🔗" l" https://net2o.de/" bm\\ "https://net2o.de/" link[]
	    l" Stefan Krempl  " l" Innenminister Seehofer wünscht sich digitale Wanzen im Wohnzimmer" bi\\
	    l" 🔗" l" https://heise.de/-4449720" bm\\ "https://heise.de/-4449720" link[]
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
	' qr-code "qr-code.png" 8e }}image-file drop /center
	qr-code nearest
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

[IFDEF] writeout-en
    lsids ' .lsids s" ef2018/en" r/w create-file throw
    dup >r outfile-execute r> close-file throw
[THEN]

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
