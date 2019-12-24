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
tex: 36c3-logo
' net2o-logo "net2o-200.png" 0.666e }}image-file 2Constant net2o-img
' 36c3-logo "36c3-logo.png" 0.444e }}image-file 2Constant 36c3-img

: logo-img ( xt xt -- o o-img ) 2>r
    baseline# 0e to baseline#
    {{ 2r> }}image-tex dup >r /right
    glue*l }}glue
    }}v >o font-size# f2/ to border o o>
    to baseline# r> ;

: logo-img2 ( o1 o2 -- o o-img ) { leftimg rightimg }
    baseline# 0e to baseline#
    {{  {{ leftimg glue*ll }}glue rightimg }}h
    glue*l }}glue
    }}v >o font-size# f2/ to border o o>
    to baseline# ;

' }}i18n-text is }}text'

day-mode
$005555FF text-color: author#
night-mode
$44FFFFFF re-color author#

{{
    {{
	glue-left @ }}glue
	
	\ page 0
	{{
	    $FFFFFF00 dup pres-frame

	    tex: cloudcalypse
	    ' cloudcalypse "cloudcalypse-16-9.jpg" 2e 3e f/ }}image-file drop /center
	    {{
		{{
		    glue*l }}glue
		    l" CloudCalypse 2" /title
		    l" Social network with net2o" /subtitle
		    glue*2 }}glue	
		    l" Bernd Paysan" /author
		    l" 36c3 Leipzig, Open Infrastructure Orbit, #wefixthenet" /location
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
		l" 6 Years after Snowden" /title
		l" What changed?" \\
		\skip
		l" Politics" /subsection
		{{
		    l"   Germany: Telemedia providers = ISPs" "🤦" e\\
		    l"   Germany: Providers have to hand out passwords" "🤦🤦" e\\
		    l"   Germany: online search of cloud data" "🤦🤦🤦" e\\
		    l"   Backdoors still wanted (“reasonable encryption”)" "🤦🤦🤦🤦" e\\
		    l"   Terrorism/Child Porn/Protection of Minors rotated as reasons" "🤦🤦🤦🤦🤦" e\\
		}}v box[]
		\skip
		l" Progress" /subsection
		l"   net2o becomes more and more usable" \\
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
		l" On social networks" /subsection
		\italic
		l" … Few of us understood it at that time, but none of the things that we’d go on to share would belong to us anymore. The successors to the e–commerce companies that had failed because they couldn’t find anything we were interested in buying now had a new product to sell." p\\
		\skip
		l"   That new product was Us." p\\
		l"   Our attention, our activities, our locations, our desires—everything about us that we revealed, knowingly or not, was being surveilled and sold in secret, so as to delay the inevitable feeling of violation that is, for most of us, coming only now. And this surveillance would go on to be actively encouraged, and even funded by an army of governments greedy for the vast volume of intelligence they would gain." p\\
		\regular \skip
		l" Edward Snowden" }}text' /right
		\skip
		l" Note: This is a libertarian framing; corporate vs. government power evilness" p\\
		glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
	    }}v box[] >o o Value snowden-page2 font-size# to border o o>
	}}z box[] /flip dup >slides

	\ page 4
	{{
	    $5F0000FF $FF7777FF pres-frame
	    {{
		l" Associal Hateworks" /title
		l" Problems with People since Eternal September" /subsection
		vt{{
		    l" Opinions " l" are not facts, but values people believe in" b\\
		    l" Believes " l" are not up to discussion, but part of identity" b\\
		    l" Identity " l" is vigurously defended and used to segregate people" b\\
		    l" Walls "    l" are in the head, and tearing them down causes aggression" b\\
		    \skip
		    l" Free Speech " l" is a concept of a time where religion was strong and science weak" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 5
	{{
	    $200020FF $FFCCFFFF pres-frame
	    {{
		l" net2o in a nutshell" /title
		l" net2o consists of the following 6 layers (implemented bottom up):" /subsection
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
		    l" 7. " l" Apps in a sandboxed environment for displaying content (ΜΙΝΩΣ2)"
		    b\\
		}}vt
		glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
	    }}v box[] >bdr
	}}z box[] /flip dup >slides
	
	\ page 6
	{{
	    $200020FF $FFCCFFFF pres-frame
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

	\ page 6
	{{
	    $202000FF $FFFFCCFF pres-frame
	    {{
		l" Last year’s things still to do" /title
		vt{{
		    l" + " l" Finish bulk importer for Google+" b\\
		    l" – " l" Write bulk importers for Facebook/Twitter/Blogger/etc." b\\
		    l" + " l" Use avatars to display users's ID" b\\
		    l" + " l" Markdown renderer" b\\
		    l" + " l" Album viewer" b\\
		    l" – " l" Movie player" b\\
		    l" – " l" Key handover to contact in net2o world (temporary keypair)" b\\
		    l" + " l" Mark imported keys as not trustworthy" b\\
		}}vt
		l" Hands on presentation" /subsection
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 7
	{{
	    $200030FF $EECCFFFF pres-frame
	    {{
		l" New Challenges found" /title
		l" This endeaver is an exploration of what’s actually needed" /subsection
		vt{{
		    l" • " l" Hackers need a night mode (color theme) ✅" b\\
		    l" • " l" Some JPEGs don't have thumbnails (use epeg?)" b\\
		    l" • " l" Protocol to provide “who has what” with privacy in mind (✅½)" b\\
		    l" • " l" Comfortable ID cloning (see IETF MEDUP task group)" b\\
		    l" • " l" Permissions for DVCS updates/posting&comment submission" b\\
		    l" • " l" Likes/+1s/etc.: only the last one (per user) counts" b\\
		    l" • " l" Closed group chats ✅" b\\
		    l" • " l" Permissions for moderators" b\\
		    l" • " l" Shareable list of collections/groups" b\\
		    l" • " l" What about port 53/80/443–only networks?" b\\
		}}vt
	    }}v box[] >bdr
	}}z box[] /flip dup >slides

	\ page 8
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

	\ page 9
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

	\ page 10
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

	\ page 11
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
    36c3-img drop net2o-img drop  logo-img2
}}z slide[]
to top-widget

also opengl

[IFDEF] writeout-en
    lsids ' .lsids s" ef2018/en" r/w create-file throw
    dup >r outfile-execute r> close-file throw
[THEN]

previous

night-mode

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
