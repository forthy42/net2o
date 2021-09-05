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
		glue*l }}glue
		tex: rome-logo
		' rome-logo "rome-logo.png" 0.5e }}image-file drop /center
		glue*l }}glue
	    }}v
	    {{
		{{
		    glue*l }}glue
		    l" net2o Progress Report" /title
		    l" Decentralized Censorship" /subtitle
		    glue*2 }}glue	
		    l" Bernd Paysan" /author
		    l" EuroForth 2021. Video Conference (shoud have been Rome)" /location
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
		l" 1¾ years into COVID–19 pandemics" /title
		\skip
		l" Surveillance Capitalism" /subsection
		{{
		    l"    Apple wants to scan your pics locally for child porn" \\
		    l"   ➡ Had to back down quickly" \\
		    l"    Facebook &  Twitter “check for facts”" \\
		    l"   ➡ Actually still distribute a lot of disinformation" \\
		    l"    Telegram became tool of choice of Covidiots" \\
		    l"   ➡ Free speech seems to be a problem" \\
		    l"    Twitter tests “safe space” feature…" \\
		    l"   ➡ The algorithm hides what could hurt you" \\
		}}v box[]
		\skip
		l" Progress" /subsection
		l"   Little on net2o, more on Bernd 2.0" \\
		l"   TCP/IP turns 40" \\
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
		l" Disinformation" /title
		l" Lessons learned during the pandemics" /subsection
		vt{{
		    l" First Impression " l" Facts don’t change our minds [2]" b\\
		    l" Ikea Effect "  l" Easy to obtain things have “no value” [3]" b\\
		    l" Worldview "  l" lets us dismiss facts that don’t fit into it" b\\
		    l" Science "  l" needs to be prudent" b\\
		    l" Plausibility "  l" This man has done evil things many times" b\\
		    l" " l" It’s just that he doesn’t need to chip you" b\\
		    l" " l" He already has everything he wants to" b\\
		    \skip
		    l" QAnon "  l" suspected origin: Wu Ming (五名, 5 names)" b\\
		    l" " l" Wu Ming can be pronounced as 无明, Ignorance" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 5
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

	\ page 9
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" Things done: “Who has What”" /title
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
		    \skip
		    l" ❓ " l" Missing: limit reach of ”who has what”" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" In Progress: Harfbuzz (in ΜΙΝΩΣ2)" /title
		vt{{
		    l" • " l" Purpose: Do the more complex part of Unicode rendering" b\\
		    l" • " l" The interface is actually not that difficult" b\\
		    l" • " l" But requires restructuring code" b\\
		    l" • " l" And thinking about right to left scripts" b\\
		    l" • " l" (and top-to-bottom like Mongolian)" b\\
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
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Decentralized Censorship" /title
		l" Make net2o a better place" /subtitle
		\skip
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
		l" Examples from the Covid pandemic" /subtitle
		\skip
		vt{{
		    l" 李文亮 " l" Was gag ordered by Wuhan police when the main news (新闻联播)" b\\
		    l"  " l" already had a report.  “Would not happen here“" b\\
		    \skip
		    l" Here? " l" Instead, a hell lot of disinformation spread out in the free west" b\\
		    \skip
		    l" Evil Govt " l" Yes, the government is evil.  But also incompetent." b\\
		    l"  " l" And its bias is pro corporations.  Evilness serves a purpose." b\\
		    \skip
		    l" Science? " l" Science questions everything.  But it conducts experiments to check." b\\
		    l"  " l" Masks work.  Lockdowns work.  Wuhan lab didn’t leak.  Vaccines are safe." b\\
		    l"  " l" Invermectin/Chloroquin/Vitamine D are no miracle cure." b\\
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
		l" How to filter?" /title
		l" Actually the hard problem" /subtitle
		\skip
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
		l" It’s not bad words" /subtitle
		\skip
		vt{{
		    l" • " l" Teh spellink is aweful" b\\
		    l" • " l" SHOUTING ALL THE TIME" b\\
		    l" • " l" Number (and color — needs Harfbuzz to show here) of exclamation marks!!!!!" b\\
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
