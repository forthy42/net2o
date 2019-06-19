\ Presentation on ΜΙΝΩΣ2 made in ΜΙΝΩΣ2

\ Copyright (C) 2018 Bernd Paysan

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

: update-size# ( -- )
    dpy-w @ s>f 42e f/ fround to font-size#
    font-size# 16e f/ m2c:curminwidth% f!
    dpy-h @ s>f dpy-w @ s>f f/ 45% f/ font-size# f* fround to baseline#
    dpy-w @ s>f 1280e f/ to pixelsize# ;

update-size#

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
    over >r
    n2-img m2-img $q-img
    r@ m/$-switch u>= IF swap THEN
    r> n/m-switch u>= IF rot  THEN
    rot dup .parent-w .parent-w /flop drop
    rot dup .parent-w .parent-w /flop drop
    rot dup .parent-w .parent-w /flip drop
    trans-frame trans-frame solid-frame
    update-size# update-glue
    over slide# !
    slides[] $[] @ /flip drop
    slides[] $[] @ /flop drop glue0 ;
: fade-img ( r0..1 img1 img2 -- ) >r >r
    [ whitish x-color 1e f+ ] Fliteral fover f-
    r> >o to frame-color parent-w .parent-w /flop drop o>
    [ whitish x-color ] Fliteral f+
    r> >o to frame-color parent-w .parent-w /flop drop o> ;
: fade!slides ( r0..1 n -- r0..1 n )
    dup m/$-switch = IF
	fdup $q-img m2-img fade-img
    THEN
    dup n/m-switch = IF
	fdup m2-img n2-img fade-img
    THEN ;
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
    fade!slides 1- sin-t anim!slides +sync +resize ;

: next-anim ( n r0..1 -- )
    dup slides[] $[]# 1- u>= IF  drop fdrop  EXIT  THEN
    fdup 1e f>= IF  fdrop
	dup 1+ swap !slides +sync +resize  EXIT
    THEN
    1+ fade!slides sin-t anim!slides +sync +resize ;

1e FValue slide-time%

: prev-slide ( -- )
    slide-time% anims[] $@len IF  anim-end .2e f*  THEN
    slide# @ ['] prev-anim >animate ;
: next-slide ( -- )
    slide-time% anims[] $@len IF  anim-end .2e f*  THEN
    slide# @ ['] next-anim >animate ;

: slide-frame ( glue color -- o )
    font-size# 70% f* }}frame ;

box-actor class
    \ sfvalue: s-x
    \ sfvalue: s-y
    \ sfvalue: last-x
    \ sfvalue: last-t
    \ sfvalue: speed
end-class slide-actor

:noname ( axis dir -- ) nip
    0< IF  prev-slide  ELSE  next-slide  THEN ; slide-actor is scrolled
:noname ( rx ry b n -- )  dup 1 and 0= IF
	over $8  and IF  prev-slide  2drop fdrop fdrop  EXIT  THEN
	over $10 and IF  next-slide  2drop fdrop fdrop  EXIT  THEN
	over -$2 and 0= IF
	    fover caller-w >o x f- w f/ o>
	    fdup 0.1e f< IF  fdrop  2drop fdrop fdrop  prev-slide  EXIT
	    ELSE  0.9e f> IF  2drop fdrop fdrop  next-slide  EXIT  THEN  THEN
	THEN  THEN
    [ box-actor :: clicked ] +sync +resize ; slide-actor to clicked
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
tex: minos2
tex: $quid
' net2o-logo "net2o-200.png" 0.666e }}image-file Constant net2o-glue drop
' minos2 "net2o-minos2.png" 0.666e }}image-file Constant minos2-glue drop
' $quid  "squid-logo-200.png" 0.5e }}image-file Constant $quid-glue drop

: logo-img ( xt xt -- o o-img ) 2>r
    baseline# 0e to baseline#
    {{ 2r> }}image-tex dup >r /right
    glue*l }}glue
    }}v >o font-size# f2/ to border o o>
    to baseline# r> ;

: pres-frame ( color -- o1 o2 ) \ drop $FFFFFFFF
    color, glue*wh slide-frame dup .button1 simple[] ;

' }}i18n-text is }}text'

{{
{{ glue-left }}glue

\ page 0
{{
    $FFFFFFFF pres-frame
    {{
	glue*l }}glue \ ) $CCDDDD3F color, 4e }}frame dup .button1
	l" net2o: ΜΙΝΩΣ2 GUI, $quid “crypto”" /title
	l" ($quid = ethical micropayment with efficient BlockChain)" /subtitle
	{{
	    {{
		glue*l }}glue \ ) $CCDDDD3F color, 4e }}frame dup .button1
		tex: edinburgh-coa
		' edinburgh-coa "edinburgh-coa.jpg" 0.5e }}image-file
		Constant coa-glue /center
		0e to x-baseline
		glue*l }}glue \ ) $CCDDDD3F color, 4e }}frame dup .button1
	    }}v
	glue*2 }}glue }}z
	l" Bernd Paysan" /author
	l" EuroForth 2018, Edinburgh" /location
	glue*l }}glue \ ) $CCDDDD3F color, 4e }}frame dup .button1
    }}v box[] >o font-size# to border o Value title-page o o>
}}z box[] dup >slides

\ page 1
{{
    $FFFFFFFF pres-frame
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
    $FF7F7FFF pres-frame
    {{
	l" 5 Years after Snowden" /title
	l" What changed?" \\
	\skip
	l" Politics" /subsection
	{{ {{
	    blackish l" " \\
	    l"   EU parliament wants upload filters" "🤦" e\\
	    l"   EU parliament taxes the link (instead: “right”)" "🤦🤦" e\\
	    l"   EU parliament wants filtering “terrorist contents”" "🤦🤦🤦" e\\
	    l"   Germany wants a Cyberadministration like CAC (Medienstaatsvertrag)" "🤦🤦🤦🤦" e\\
	    l"   Backdoors still wanted (“reasonable encryption”)" "🤦🤦🤦🤦🤦" e\\
	    l"   Legalize it (dragnet surveillance)" "🤦🤦🤦🤦🤦🤦" e\\
	    l"   You can't reasonably expect privacy on your own PC" "🤦🤦🤦🤦🤦🤦🤦" e\\
	    l"   “Crypto” now means BitCoin" "🤦🤦🤦🤦🤦🤦🤦🤦" e\\
	    tex: vp-eu glue*l ' vp-eu }}vp vp[]
	    $FFBFFFFF color, fdup to slider-color to slider-fgcolor
	    font-size# f2/ f2/ to slider-border
	    dup font-size# f2/ fdup vslider
	}}h box[]
	\skip
	l" Competition" /subsection
	l"   Cambridge Analytica scandal (Facebook)" \\
	l"   Security fuckups: Passwords pawned, chat log saved unprotected in the cloud, etc." \\
	
	\skip
	l" Progress" /subsection
	l"   The ECHR ruled that GCHQ’s dragnet surveillances violates your rights" \\
	l"   net2o becomes more and more usable" \\
\	glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
    }}v box[] >o o Value snowden-page font-size# to border o o>
}}z box[] /flip dup >slides

\ page 6
{{
    $FFBFFFFF pres-frame
    {{
	l" ΜΙΝΩΣ2 Widgets" /title
	l" Design principle is a Lego–style combination of many extremely simple objects" \\
	{{ {{ vt{{
		    l" actor " l" base class that reacts on all actions (clicks, touchs, keys)" b\\
		    l" animation " l" action for animations" b\\
		    l" widget " l" base class for all visible objects" b\\
		    l" glue " l" base class for flexible objects" b\\
		    l" tile " l" colored rectangle" b\\
		    l" frame " l" colored rectangle with border" b\\
		    l" icon " l" icon from an icon texture" b\\
		    l" image " l" larger image" b\\
		    {{ l" edit " b0 blackish l" editable text: " }}text'
		    "中秋节快乐！ Happy autumn festival! 🌙🌕" }}edit dup Value edit-field glue*l }}glue }}h edit-field ' true edit[] >bl
		    \sans \latin \normal \regular
		    l" text " l" text element/Emoji/中文/… 😀🤭😁😂😇😈🙈🙉🙊💓💔💕💖💗💘🍺🍻🎉🎻🎺🎷" b\\
		    l" part-text " l" pseudo–element for paragraph breaking" b\\
		    l" canvas " l" vector graphics (TBD)" b\\
		    l" video " l" video player (TBD)" b\\
		}}vt
		glue*l }}glue
	    tex: vp0 glue*lll ' vp0 }}vp vp[]
	    dup font-size# f2/ fdup vslider
	}}h box[]
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 7
{{
$BFFFFFFF pres-frame
{{
    l" ΜΙΝΩΣ2 Boxes" /title
    {{
    l" Just like " }}text' \LaTeX l" , boxes arrange widgets/text" }}text' glue*l }}h box[]
    >bl
    \skip
    vt{{
	l" hbox " l" Horizontal box, common baseline" b\\
	l" vbox " l" Vertical box, minimum distance a baselineskip (of the hboxes below)" b\\
	l" zbox " l" Overlapping several boxes" b\\
	l" slider " l" horizontal and vertical sliders (composite object)" b\\
	l" parbox " l" box for breaking paragraphs" b\\
	l" grid " l" Free widget placements (TBD)" b\\
	\skip
	l" Tables uses helper glues, no special boxes needed" \\
    }}vt
    {{ {{ glue*l }}glue
	    {{ \tiny l"  Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo. " }}i18n-text \bold "Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit," }}text \regular " sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui " }}text \italic "dolorem ipsum quia dolor sit amet," }}text \regular " consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem. Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem vel eum iure reprehenderit qui in ea voluptate velit esse quam nihil molestiae consequatur, vel illum" }}text \bold-italic " qui dolorem eum fugiat" }}text \regular " quo voluptas nulla pariatur?" }}text glue*l }}glue }}p cbl dpy-w @ s>f font-size# 140% f* f- 1e text-shrink% f2/ f- f/ dup .par-split unbox
	glue*l }}glue }}v
    glue*2 }}glue }}z  \ ) $CCDDDD3F 4e }}frame dup .button1
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 8
{{
    $FFFFBFFF pres-frame
    {{
	l" ΜΙΝΩΣ2 Displays" /title
	l" Render into different kinds of displays" \\
	\skip
	vt{{
	    l" viewport " l" Into a texture, used as viewport" b\\
	    l" display " l" To the actual display (no class, just the default)" b\\
	}}vt
	glue*l }}glue
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 9
{{
    $BFDFFFFF pres-frame
    {{
	l" Minimize Draw Calls" /title
	l" OpenGL wants as few draw–calls per frame, so different contexts are drawn in stacks with a draw–call each" p\\
	\skip
	vt{{
	    l" init " l" Initialization round" b\\
	    l" bg " l" background round" b\\
	    l" text " l" text round (same draw call as bg round, just different code)" b\\
	    l" image " l" draw images with one draw–call per image" b\\
	}}vt
	glue*l }}glue
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 10
{{
    $D4AF37FF pres-frame
    {{
	l" $quid & SwapDragonChain" /title
	l" Content:" /subsection
	\skip
	vt{{
	    l" Money " l" What’s that all about?" b\\
	    l" BitCoin " l" Shortcomings of a first proof of concept" b\\
	    l" Wealth " l" Ethical implication in deflationary systems" b\\
	    l" Proof of " l" Trust instead Work" b\\
	    l" BlockChain " l" What’s the actual point?" b\\
	    l" Scale " l" How to scale a BlockChain?" b\\
	    l" Contracts " l" Smart oder dumb?" b\\
	    l" $quid " l" Ethical ways to create money" b\\
	}}vt
	glue*l }}glue
    }}v box[] >bdr
    {{
	glue*l }}glue
	tex: $quid-logo-large
	' $quid-logo-large "squid-logo.png" 0.666e }}image-file
	drop >o $FFFFFFC0 color, to frame-color o o>
	/right
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 11
{{
    $e4cF77FF pres-frame
    {{
	l" What’s Money?" /title
	vt{{
	    l" Commodity ~: " l" Objects with inherent value" b\\
	    l" Promissory note: " l" Bank created paper for commodity" b\\
	    l" Representative ~: " l" Promise to exchange with “standard object” (e.g. gold)" b\\
	    l" Fiat ~: " l" No inherent value; promise, if any, as legal tender" b\\
	    l" Legal tender: " l" Medium of payment by law" b\\
	}}vt
	glue*l }}glue
    }}v box[] >bdr
    {{
	glue*l }}glue
	{{
	    {{
		tex: shell-coins
		tex: feiqian
		tex: huizi
		tex: chao
		glue*l }}glue
	    ' shell-coins "shell-coins.png" 0.666e }}image-file drop
		glue*l }}glue
		' feiqian "feiqian.png" 0.666e }}image-file drop
		glue*l }}glue
		' huizi "huizi.png" 0.666e }}image-file drop
		glue*l }}glue
		' chao "chao.jpg" 0.666e }}image-file drop
		glue*l }}glue
	    }}h box[]
	tex: vp1 glue*l ' vp1 }}vp vp[]
    }}v box[] >bdr
}}z box[]
/flip dup >slides

\ page 12
{{
    $f4cF57FF pres-frame
    {{
	l" BitCoins — early “Crypto” shortcomings" /title
	vt{{
	    l" • " l" Proof of work: wasteful and yet only marginally secure" b\\
	    l" • " l" Inflation is money’s cancer, deflation its infarct" b\\
	    l" • " l" Consequences: unstable exange rate, high transaction fees" b\\
	    l" • " l" Ponzi scheme–style bubble" b\\
	    l" • " l" (Instead of getting Viagra spam I now get BitCoin spam)" b\\
	    l" • " l" Can’t even do the exchange transaction on–chain" b\\
	}}vt
	glue*l }}glue
    }}v box[] >bdr
    {{
	glue*l }}glue
	tex: bitcoin-bubble
	' bitcoin-bubble "bitcoin-bubble.png" 0.85e }}image-file drop /right
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 13
{{
    $e4df67ff pres-frame
    {{
	l" Wealth & Ethics" /title
	vt{{
	    l" • " l" Huge first mover advantage" b\\
	    l" • " l" Already worse wealth distribution than neoliberal economy" b\\
	    l" • " l" Huge inequality drives society into servitude, not into freedom" b\\
	    l" • " l" No concept of a credit" b\\
	    l" • " l" Lightning network also binds assets (will have fees as consequence)" b\\
	}}vt
	glue*l }}glue
    }}v box[] >bdr
    {{
	glue*l }}glue
	tex: free-market
	' free-market "free-market.jpg" 0.666e }}image-file drop /right
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 14
{{
    $a4df87ff pres-frame
    {{
	l" Proof of What?!" /title
	vt{{
	    l" Challenge " l" Avoid double–spending" b\\
	    l" State of the art: " l" Proof of work" b\\
	    l" Problem: " l" Proof of work burns energy and GPUs" b\\
	    l" Suggestion 1: " l" Proof of stake (money buys influence)" b\\
	    l" Problem: " l" Money corrupts, and corrupt entities misbehave" b\\
	    l" Suggestion 2: " l" Proof of well–behaving (trust, trustworthyness)" b\\
	    l" How? " l" Having signed many blocks in the chain gains points" b\\
	    l" Multiple signers " l" Not only have one signer, but many" b\\
	    l" Suspicion " l" Don't accept transactions in low confidence blocks" b\\
	    l" Idea " l" Repeated prisoner’s dilemma rewards cooperation" b\\
	}}vt
	\skip
	l" BTW: The attack for double spending also requires a MITM–attack" \\
	glue*l }}glue
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 15
{{
    $a4d8f7ff pres-frame
    {{
	l" SwapDragon BlockChain" /title
	vt{{
	    l" • " l" Banks distrust each others, too (i. e. GNU Taler is not a solution)" b\\
	    l" • " l" Problem size: WeChat Pay peaks at 0.5MTPS (BTC at 5TPS)" b\\
	    l" • " l" Lightning Network doesn’t stand an overrun–the–arbiter attack" b\\
	    l" • " l" Therefore, the BlockChain itself needs to scale" b\\
	    l" • " l" Introduce double entry booking into the distributed ledger" b\\
	    l" • " l" Partitionate the ledgers by coin pubkey" b\\
	    l" • " l" Use n–dimensional ledger space to route transactions" b\\
	}}vt
	glue*l }}glue
	{{
	    tex: stage1
	    tex: stage2
	    ' stage1 "ledger-stage1.png" 0.666e }}image-file drop
	    "   " }}text
	    ' stage2 "ledger-stage2.png" 0.666e }}image-file drop
	    glue*l }}glue
	}}h box[]
    }}v box[] >bdr
    {{
	glue*l }}glue
	tex: bank-robs-you
	' bank-robs-you "bank-robs-you.jpg" 0.666e }}image-file drop /right
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 16
{{
$a487dfff pres-frame
    {{
	l" Dumb Contracts" /title
	vt{{
	    l" • " l" Smart Contracts: Token–Forth subset (BitCoin), JavaScript (Ethereum)" b\\
	    {{ l" • " b0 blackish l" For Smart Contracts you need a lawyer, a programmer, " }}text' \italic l" and" }}text' \regular l"  a pentester" }}text' glue*l }}glue }}h box[] >bl
	    l" • " l" Keep it simple: A contract must have a balanced balance" b\\
	    l" • " l" Select sources (S), select their assets (A), debit them (±)" b\\
	    l" • " l" select destinations (D), set assets&credit them" b\\
	    l" • " l" Shortcut: balance an asset (B)" b\\
	    l" • " l" Obligations for debt and futures (O)" b\\
	    l" • " l" Sign the target account with new content+hash of the contrat" b\\
	}}vt
	\skip
	l" Examples:" /subsection    
	vt{{
	    l" Transfer " l" SA–SBDD" b\\
	    l" Cheque " l" SA–D, cash: SA–DSBD" b\\
	    l" Exchange/Purchase " l" SA+A–DSBBD" b\\
	}}vt
	glue*l }}glue
    }}v box[] >bdr
    {{
	glue*l }}glue
	tex: feynman-diag
	' feynman-diag "feynman-diag.png" 1.333e }}image-file drop /right
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 17
{{
    $df87a4ff pres-frame
    {{
	l" $quid: Ethical mining" /title
	vt{{
	    l" • " l" Concept of mining: Provide difficult and rare work" b\\
	    l" • " l" Suggesting: Provide vouchers for free software development sponsorships" b\\
	    l" • " l" These vouchers are tradeable on their own" b\\
	    l" • " l" Free software is public infrastructure for the information age" b\\
	    l" • " l" That way, we can encourage people to sponsor out of self–interest" b\\
	    l" • " l" They get a useful and valueable token back" b\\
	    l" • " l" Or they develop FOSS themselves to earn (fiat) money" b\\
\skip
	    l" Decentral bank?" /subsection
	    l" • " l" Central bank grants big banks credits, which then are gambled in the stock market" b\\
	    l" • " l" The decentral bank gives credits to small business" b\\
	    l" • " l" Credit assessment more like croudfunding" b\\
	}}vt
	glue*l }}glue
    }}v box[] >bdr
}}z box[] /flip dup >slides

\ page 17
{{
    $FFFFFFFF pres-frame
    {{
	l" Literatur & Links" /title
	vt{{
	    l" Bernd Paysan " l" net2o fossil repository" bi\\
	    l" 🔗" l" https://net2o.de/" bm\\
	    [: s" xdg-open https://net2o.de/" system ;] 0 click[]
	    l" Bernd Paysan " l" $quid cryptocurrency & SwapDragonChain" bi\\
	    l" 🔗" l" https://squid.cash/" bm\\
	    [: s" xdg-open https://squid.cash/" system ;] 0 click[]
	}}vt
	glue*l }}glue
	tex: qr-code
	' qr-code "qr-code.png" 13e }}image-file drop /center
	qr-code nearest
	glue*l }}glue
    }}v box[] >bdr
}}z box[] /flip dup >slides

' }}text is }}text'

\ end
glue-right }}glue
}}h box[]
{{
' net2o-logo net2o-glue  logo-img to n2-img
' minos2     minos2-glue logo-img dup to m2-img trans-frame /flip
' $quid      $quid-glue  logo-img dup to $q-img trans-frame /flip
}}z
}}z slide[]
to top-widget

also opengl

: !widgets ( -- )
    set-fullscreen-hint 1 set-compose-hint
    top-widget .htop-resize
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
