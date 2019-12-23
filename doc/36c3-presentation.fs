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

Variable slides[]
Variable slide#

0 Value n2-img
0 Value m2-img
0 Value $q-img

3 Constant n/m-switch
8 Constant m/$-switch

: >slides ( o -- ) slides[] >stack ;

glue ' new static-a with-allocater Constant glue-left
glue ' new static-a with-allocater Constant glue-right

: glue0 ( -- ) 0e fdup
    [ glue-left  .hglue-c ]L df!
    [ glue-right .hglue-c ]L df! ;
: trans-frame ( o -- )
    >o transp# to frame-color o> ;
: solid-frame ( o -- )
    >o white# to frame-color o> ;
: !slides ( nprev n -- )
    44e update-size# update-glue
    over slide# !
    slides[] $[] @ /flip drop
    slides[] $[] @ /flop drop glue0 ;
: fade-img ( r0..1 img1 img2 -- ) >r >r
    [ whitish x-color 1e f+ ] Fliteral fover f-
    r> >o to frame-color parent-w .parent-w /flop drop o>
    [ whitish x-color ] Fliteral f+
    r> >o to frame-color parent-w .parent-w /flop drop o> ;
: anim!slides ( r0..1 n -- )
    slides[] $[] @ /flop drop
    fdup fnegate dpy-w @ fm* glue-left  .hglue-c df!
    -1e f+       dpy-w @ fm* glue-right .hglue-c df! ;

: prev-anim ( n r0..1 -- )
    dup 0<= IF  drop fdrop  EXIT  THEN
    fdup 1e f>= IF  fdrop
	dup 1- swap !slides +sync +resize  EXIT
    THEN
    1e fswap f-
    1- sin-t anim!slides +sync +resize ;

: next-anim ( n r0..1 -- )
    dup slides[] $[]# 1- u>= IF  drop fdrop  EXIT  THEN
    fdup 1e f>= IF  fdrop
	dup 1+ swap !slides +sync +resize  EXIT
    THEN
    1+ sin-t anim!slides +sync +resize ;

1e FValue slide-time%

: prev-slide ( -- )
    slide-time% anims[] $@len IF  anim-end .2e f*  THEN
    slide# @ ['] prev-anim >animate ;
: next-slide ( -- )
    slide-time% anims[] $@len IF  anim-end .2e f*  THEN
    slide# @ ['] next-anim >animate ;

: slide-frame ( glue color -- o )
    font-size# 70% f* }}frame ;
: vp-frame ( color -- o ) \ drop $FFFFFFFF
    color, glue*wh slide-frame dup .button3 simple[] ;
: -25%b >o current-font-size% -25% f* to border o o> ;

box-actor class
    \ sfvalue: s-x
    \ sfvalue: s-y
    \ sfvalue: last-x
    \ sfvalue: last-t
    \ sfvalue: speed
end-class slide-actor

0 Value scroll<<

:noname ( axis dir -- ) nip
    0< IF  prev-slide  ELSE  next-slide  THEN ; slide-actor is scrolled
:noname ( rx ry b n -- )  dup 1 and 0= IF
	over $180 and IF  4 to scroll<<  THEN
	over $08 scroll<< lshift and IF  prev-slide  2drop fdrop fdrop  EXIT  THEN
	over $10 scroll<< lshift and IF  next-slide  2drop fdrop fdrop  EXIT  THEN
	over -$2 and 0= IF
	    fover caller-w >o x f- w f/ o>
	    fdup 0.1e f< IF  fdrop  2drop fdrop fdrop  prev-slide  EXIT
	    ELSE  0.9e f> IF  2drop fdrop fdrop  next-slide  EXIT  THEN  THEN
	THEN  THEN
    [ box-actor :: clicked ] +sync +resize ; slide-actor is clicked
:noname ( ekey -- )
    case
	k-up      of  prev-slide  endof
	k-down    of  next-slide  endof
	k-prior   of  prev-slide  endof
	k-next    of  next-slide  endof
	k-volup   of  prev-slide  endof
	k-voldown of  next-slide  endof
	s-k3      of  1e ambient% sf!
	    Ambient 1 ambient% opengl:glUniform1fv  +sync endof
	k-f3      of  ambient% sf@ 0.1e f+ 1e fmin  ambient% sf!
	    Ambient 1 ambient% opengl:glUniform1fv  +sync endof
	k-f4      of  ambient% sf@ 0.1e f- 0e fmax  ambient% sf!
	    Ambient 1 ambient% opengl:glUniform1fv  +sync endof
	s-k5      of  1e saturate% sf!
	    Saturate 1 saturate% opengl:glUniform1fv  +sync endof
	k-f5      of  saturate% sf@ 0.1e f+ 3e fmin saturate% sf!
	    Saturate 1 saturate% opengl:glUniform1fv  +sync endof
	k-f6      of  saturate% sf@ 0.1e f- 0e fmax saturate% sf!
	    Saturate 1 saturate% opengl:glUniform1fv  +sync endof
	k-f1      of  top-widget ..widget  endof
	[ box-actor :: ekeyed ]  EXIT
    endcase +sync +resize ; slide-actor to ekeyed
\ :noname ( $xy b -- )  dup 1 > IF
\ 	[ box-actor :: touchdown ] EXIT
\     THEN  drop
\     xy@ to s-y to s-x ftime to last-t
\     true to grab-move? ; slide-actor is touchdown
\ :noname ( $xy b -- ) dup 1 > IF
\ 	[ box-actor :: touchmove ] EXIT
\     THEN  drop xy@ fdrop
\     ftime last-t fover to last-t f- \ delta-t
\     last-x fover to last-x f-       \ delta-x
\     fswap f/ caller-w .w f/ to speed
\     last-x s-x f- caller-w .w f/ fdup f0< IF \ to the right
\ 	1e f+ slide# @ prev-anim
\     ELSE \ to the left
\ 	slide# @ next-anim
\     THEN ; slide-actor is touchmove
:noname ( $xy b -- ) 2dup [ box-actor :: touchmove ] drop
    xy@ dpy-h @ s>f fswap f- dpy-h @ 2/ fm/ lightpos-xyz sfloat+ sf!
    dpy-w @ s>f f- dpy-w @ 2/ fm/ lightpos-xyz sf!
    3.0e lightpos-xyz 2 sfloats + sf!
    LightPos 1 lightpos-xyz opengl:glUniform3fv  +sync ; slide-actor is touchmove
\ :noname ( $xy b -- )  dup 1 > IF
\ 	[ box-actor :: touchup ] EXIT
\     THEN  2drop
\     slide# @ 1e next-anim
\     false to grab-move? ; slide-actor is touchup

: slide[] ( o -- o )
    >o slide-actor new to act o act >o to caller-w o> o o> ;

glue-left  >o 1glue vglue-c glue! 1glue dglue-c glue! o>
glue-right >o 1glue vglue-c glue! 1glue dglue-c glue! o>

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

: pres-frame ( color -- o1 o2 ) \ drop $FFFFFFFF
    color, glue*wh slide-frame dup .button1 simple[] ;

$FFFFBBFF text-color: redish

$10 stack: vp-tops

' }}i18n-text is }}text'

{{
    {{ glue-left }}glue
	
	\ page 0
	{{
	    $FFFFFF00 pres-frame
	    ' redish >body f@ ' dark-blue >body f!
	    $00CCCCFF dup text-emoji-color, ' blackish >body f!

	    tex: cloudcalypse
	    \ 1 ms
	    ' cloudcalypse "cloudcalypse-16-9.jpg" 2e 3e f/ }}image-file drop /center
	    {{
		{{
		    glue*l }}glue \ ) $CCDDDD3F color, 4e }}frame dup .button1
		    l" CloudCalypse 2" /title
		    l" Social network with net2o" /subtitle
		    glue*2 }}glue	
		    l" Bernd Paysan" /author
		    l" 36c3 Leipzig, Open Infrastructure Orbit, #wefixthenet" /location
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

\ page 1
{{
    ' whitish >body f@ ' blackish >body f!
    $000000FF pres-frame
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
    $3F0000FF pres-frame
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
    $3F0000FF pres-frame
    {{
	l" Permanent Record" /title
	l" On social networks" /subsection
	\italic
	l" Few of us understood it at that time, but none of the things that we’d go on to share would belong to us anymore. The successors to the e–commerce companies that had failed because they couldn’t find anything we were interested in buing now had a new product to sell." p\\
	\skip
	l" That new product was Us." p\\
	l" Our attention, our activities, our locations, our desires—everything about us that we revealed, knowingly or not, was being surveilled and sold in secret, so as to delay the inevitable feeling of violation that is, for most of us, coming only now. And this surveillance would go on to be actively encouraged, and even funded by an army of governments greedy for the vast volume of intelligence they would gain." p\\
	\regular
	glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
    }}v box[] >o o Value snowden-page2 font-size# to border o o>
}}z box[] /flip dup >slides


\ page 4
{{
    $200020FF pres-frame
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
}}z box[] /flip dup >slides

\ page 5
{{
    $200020FF pres-frame
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
    $202000FF pres-frame
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
    $200030FF pres-frame
    {{
	l" New Challenges found" /title
	l" This is an exploration of what’s actually needed" /subsection
	vt{{
	    l" • " l" Hackers need a night mode (color theme)" b\\
	    l" • " l" Some JPEGs don't have thumbnails" b\\
	    l" • " l" Protocol to provide “who has what” with privacy in mind" b\\
	    l" • " l" Comfortable ID cloning (see IETF MEDUP task group)" b\\
	    l" • " l" Permissions for DVCS updates/posting&comment submission" b\\
	    l" • " l" Likes/+1s/etc.: only the last one (per user) counts" b\\
	    l" • " l" Closed group chats" b\\
	    l" • " l" Permissions for moderators" b\\
	    l" • " l" Shareable directory of collections/groups" b\\
	}}vt
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 12
{{
    $000000FF pres-frame
    {{
	l" The non–technical problems" /title
	vt{{
	    l" • " l" Get your contacts over to net2o" b\\
	    l" • " l" How to make a social network a nice place?" b\\
	    l" • " l" Funding of net2o?" b\\
	}}vt
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 13
{{
    $000000FF pres-frame
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
glue-right }}glue
}}h box[]
36c3-img drop net2o-img drop  logo-img2
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
