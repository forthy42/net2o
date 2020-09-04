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
		l"   “We have legalised the abuse of the person, and entrenched a system that makes populations vulnerable for the benefit of private companies.”" p\\
		l"   “The problem is not data protection. It is data collection. GDPR assumes the data was all collected properly in the first place. It is as if it is okay to spy on everyone, as long as the data never leaks. When it does, it is not data being exploited. It is people.”" p\\
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

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Video Streaming&Conferences" /title
		vt{{
		    l" • " l" Life Streaming means sending unfinished files" b\\
		    l" • " l" Different approach to multiplexing (metadata for chunk size)" b\\
		    l" • " l" Split files into indexes, data, and metadata" b\\
		    l" • " l" Quality depends on available bandwidth" b\\
		    l" • " l" Use available hardware encoders" b\\
		    l" • " l" Thumbnail/fullscreen Stream for conferences" b\\
		    l" • " l" Synchrone audio for movies/async for conferences" b\\
		}}vt
		glue*l }}glue
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Avalanche Tree" /title
		tex: avalanche-tree \normal \sans
		' avalanche-tree "avalanche.png" 0.66e }}image-file drop /center
		glue*l }}glue
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Collection Tree and Mixing" /title
		vt{{
		    l" • " l" Leaf nodes send their stream upwards." b\\
		    l" • " l" Participants can join leaf stream or root stream" b\\
		    l" • " l" Branch nodes mix leaf node streams and own stream" b\\
		    l" • " l" Root node combines final downmix, and distribute that as life stream" b\\
		    l" • " l" Every participant upstreams once, branches downstream multiple times" b\\
		    l" • " l" Moderator provides instructions for mix&downscale" b\\
		}}vt
		glue*l }}glue
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Wishlist 1/2" /title
		vt{{
		    l" • " l" Stream from camera, cropped vertically (portrait)" b\\
		    l" • " l" Stream from desktop/window" b\\
		    l" • " l" Scaled down stream for all participants" b\\
		    l" • " l" Scaled up stream for talking participants" b\\
		    l" • " l" Two talking participants side-by-side with upscaled stream" b\\
		    l" • " l" Audio mixer&autolevel (avoid leveling background noise up)" b\\
		    l" • " l" Warn “you are muted” if you talk while being muted" b\\
		    l" • " l" Hotkey to turn audio and video on/off" b\\
		    l" • " l" Display FFT of audio for visualisation" b\\
		    l" • " l" Audio biquad filters, e.g. notch, to improve bad sound quality" b\\
		    l" • " l" Push to talk, hotkeys for everything" b\\
		    l" • " l" Hotkey for cut mark + keyframe" b\\
		}}vt
		glue*l }}glue
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 12
	{{
	    $000000FF $FFFFFFFF pres-frame
	    {{
		l" Wishlist 2/2" /title
		vt{{
		    l" • " l" Allow multiple devices (cameras, mics) for one participant" b\\
		    l" • " l" Templates for nice presentation recording" b\\
		    l" • " l" Set a logo for the talk" b\\
		    l" • " l" Have a countdown clock for talk slots" b\\
		    l" • " l" Share slides directly (slides in Markdown)" b\\
		    l" • " l" Subtitles as option for slides (possibly multiple languages)" b\\
		    l" • " l" Translated audio tracks to select from (for bigger conferences)" b\\
		    l" • " l" Rooms for small-group side conversation" b\\
		    l" • " l" Moderator side channel to presenter/participants" b\\
		    l" • " l" Try connections to different participants and take best one" b\\
		    l" • " l" Mix additional audio sources (background music, desktop sounds, claps/laughers)" b\\
		    l" • " l" Spatial audio (different participants from different directions)" b\\
		}}vt
		glue*l }}glue
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
