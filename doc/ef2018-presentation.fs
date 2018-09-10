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
    >o $00000000 to frame-color o> ;
: solid-frame ( o -- )
    >o $FFFFFFFF to frame-color o> ;
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
    $FF fm* f>s $FFFFFF00 or dup
    r> >o to frame-color parent-w .parent-w /flop drop o> invert $FFFFFF00 or
    r> >o to frame-color parent-w .parent-w /flop drop o> ;
: fade!slides ( r0..1 n -- )
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
	dup 1- swap !slides +sync +config  EXIT
    THEN
    1e fswap f-
    fade!slides 1- sin-t anim!slides +sync +config ;

: next-anim ( n r0..1 -- )
    dup slides[] $[]# 1- u>= IF  drop fdrop  EXIT  THEN
    fdup 1e f>= IF  fdrop
	dup 1+ swap !slides +sync +config  EXIT
    THEN
    1+ fade!slides sin-t anim!slides +sync +config ;

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
    [ box-actor :: clicked ] ; slide-actor to clicked
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
    endcase ; slide-actor to ekeyed
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
' net2o-logo "net2o-200.png" 0.666e }}image-file Constant net2o-glue
' minos2 "net2o-minos2.png" 0.666e }}image-file Constant minos2-glue
' $quid  "squid-logo-200.png" 0.5e }}image-file Constant $quid-glue

: logo-img ( xt xt -- o o-img ) 2>r
    baseline# 0e to baseline#
    {{ 2r> }}image-tex dup >r /right
    glue*l }}glue
    }}v outside[] >o font-size# f2/ to border o o>
    to baseline# r> ;

: pres-frame ( color -- o1 o2 ) \ drop $FFFFFFFF
    color, glue*wh swap slide-frame dup .button1 simple[] ;

{{
{{ glue-left }}glue

\ page 0
{{
$FFFFFFFF pres-frame
{{
    glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
    ' }}i18n-text is }}text'
    l" net2o: ΜΙΝΩΣ2 GUI, $quid “crypto”" /title
    l" ($quid = ethical micropayment with efficient BlockChain)" /subtitle
    {{
	tex: edinburgh-coa
	{{
	    glue*l }}glue
	    ' edinburgh-coa "edinburgh-coa.jpg" 0.5e }}image-file
	    Constant coa-glue /center
	    0e to x-baseline
	    glue*l }}glue
	}}v
	glue*2 }}glue }}z
    l" Bernd Paysan" /author
    l" EuroForth 2018, Edinburgh" /location
    glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
}}v box[] >o font-size# to border o Value title-page o o>
}}z box[] dup >slides

\ page 1
{{
$FFFFFFFF pres-frame
{{
l" Motivation" /title
glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
tex: bad-gateway
' bad-gateway "bad-gateway.png" 0.666e }}image-file
Constant bgw-glue /center
glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
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
	blackish
	l"   Upload filters everywhere, please" "🤦" e\\
	l"   The link needs to be taxed!" "🤦🤦" e\\
	l"   Backdoors still wanted (“reasonable encryption”)" "🤦🤦🤦" e\\
	l"   Legalize it (dragnet surveillance)" "🤦🤦🤦🤦" e\\
	l"   You can't reasonably expect privacy on your own PC" "🤦🤦🤦🤦🤦" e\\
	l"   “Crypto” now means BitCoin" "🤦🤦🤦🤦🤦🤦" e\\
	\skip
	l" Competition" /subsection
	l"   Cambridge Analytica scandal (Facebook)" \\
	l"   Passwords pawned (too many to list)" \\
	
	\skip
	l" Solutions" /subsection
	l"   net2o becomes more and more usable" \\
	glue*l }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
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
\sans \latin \normal
l" text " l" text element/Emoji/中文/… 😀🤭😁😂😇😈🙈🙉🙊💓💔💕💖💗💘🍺🍻🎉🎻🎺🎷" b\\
l" part-text " l" pseudo–element for paragraph breaking" b\\
l" canvas " l" vector graphics (TBD)" b\\
l" video " l" video player (TBD)" b\\
}}vt
glue*l }}glue
tex: vp0 glue*l ' vp0 }}vp vp[]
$FFBFFFFF color, dup to slider-color to slider-fgcolor
font-size# f2/ f2/ to slider-border
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

' }}text is }}text'

\ page 10
{{
$D4AF37FF pres-frame
{{
"$quid & SwapDragonChain" /title
"Inhalt:" /subsection
\skip
vt{{
"Geld " "Worum geht es da überhaupt?" b\\
"BitCoin " "Mängel einer Machbarkeitsstudie" b\\
"Wealth " "Ethische Konsequenzen einer deflationären Welt" b\\
"Proof of " "Vertrauen statt Arbeit" b\\
"BlockChain " "Wozu braucht man das überhaupt?" b\\
"Scale " "Wie skaliert man eine BlockChain?" b\\
"Contracts " "Smart oder dumb?" b\\
"$quid " "Kann man ethisch Geld schaffen?" b\\
}}vt
glue*l }}glue
}}v box[] >bdr
{{
glue*l }}glue
tex: $quid-logo-large
' $quid-logo-large "squid-logo.png" 0.666e }}image-file
drop >o $FFFFFFC0 to frame-color o o>
/right
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 11
{{
$e4cF77FF pres-frame
{{
"Was ist Geld?" /title
vt{{
"Primitiv~: " "Objekte mit inhärentem Wert" b\\
"Wechsel: " "Tauschversprechen einer Bank gegen Primitivgeld" b\\
"Repräsentatives ~: " "Staatliches Versprechen zum Tausch gegen „Geldstandard“" b\\
"Fiat~: " "Kein inhärenter Wert, Versprechen ggf. als gesetzliches…" b\\
"Zahlungsmittel: " "Vom Gesetzgeber vorgeschriebenes Zahlungsmittel" b\\
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
"BitCoins — Mängel früher “Cryptos”" /title
vt{{
"• " "Proof of work: Verschwendet Ressourcen, bei zweifelhafter Sicherheit" b\\
"• " "Inflation ist der Krebs des Geldes, Deflation sein Infarkt" b\\
"• " "Konsequenzen: instabiler Kurs, hohe Transaktionskosten" b\\
"• " "Ponzi–Schema–artige Blase" b\\
"• " "(statt Viagra bekomme ich jetzt BitCoin–Spam)" b\\
"• " "Es kann nicht mal das Spekulationsgeschäft in der Chain abwickeln" b\\
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
"Reichtum & Ethik" /title
vt{{
"• " "Enormer Vorteil des ersten Handelnden" b\\
"• " "Hat schon eine schlimmere Vermögensverteilung als der Neoliberalismus" b\\
"• " "Große Ungleichheit führt zu Knechtschaft, nicht zu Freiheit" b\\
"• " "Es gibt nicht mal das Konzept des Kredits" b\\
"• " "Das neue Lightning Network bindet auch Vermögen (Folge: Gebühren)" b\\
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
"Proof von was?!" /title
vt{{
"Herausforderung " "Jede Coin darf nur einmal ausgegeben werden" b\\
"Stand der Technik: " "Proof of work" b\\
"Problem: " "PoW verbrennt Energie und GPUs/ASICs" b\\
"Vorschlag 1: " "Proof of Stake (Geld kauft Einfluss)" b\\
"Problem: " "Geld korrumpiert, korrupte Teilnehmer betrügen" b\\
"Vorschlag 2: " "Beweis von Wohlverhalten/Vertrauen" b\\
"Wie? " "Wer viele Blöcke signiert hat, bekommt viele Punkte" b\\
"Viele Signierer " "Nicht nur einer (und damit byzantine Fehlertoleranz)" b\\
"Verdacht " "Transaktionen aus Blöcken niedriger Konfidenz nicht annehmen" b\\
"Idee " "Wiederholtes Gefangenendilemma belohnt Kooperation" b\\
}}vt
\skip
"BTW: Der Angriff für “double spending” bedarf einer MITM–Attacke" \\
glue*l }}glue
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 15
{{
$a4d8f7ff pres-frame
{{
"SwapDragon BlockChain" /title
vt{{
"• " "Banken misstrauen sich gegenseitig (d.h. GNU Taler ist keine Lösung)" b\\
"• " "Problemgröße: WeChat Pay peak ~ 0.5MTPS (BTC bei 5TPS)" b\\
"• " "Überrenne den Arbiter: Problem für Lightning Network" b\\
"• " "Also muss die BlockChain selbst skalieren" b\\
\skip
"• " "Doppelte Buchführung für die verteilte Buchhaltung" b\\
"• " "Fragmentiere die Datenbank nach coin pubkey" b\\
"• " "Route Transaktionen in einem n–dimensionalen Raum" b\\
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
"Dumb Contracts" /title
vt{{
"• " "Smart Contracts: Token–Forth–Subset (BitCoin), JavaScript (Ethereum)" b\\
{{ "• " b0 blackish "Für Smart Contracts braucht man einen Rechtsanwalt " }}text \italic "und" }}text \regular " einen Programmierer" }}text glue*l }}glue }}h box[] >bl
"• " "Keep it simple: Ein Kontrakt muss eine ausgeglichene Bilanz haben" b\\
"• " "Auswahl der Quellen (S), Auswahl der Assets (A) dort, Setzen des Wertes (±)" b\\
"• " "Auswahl des/der Ziele (D), Setzen des Assets und Wert dort" b\\
"• " "Abkürzung: Wert des Assets ausgleichen (B)" b\\
"• " "Obligations für Schulden und Terminkontrakte (O)" b\\
"• " "Signieren der Ziel–Wallets mit neuem Inhalt+Hash des Kontrakts" b\\
}}vt
\skip
"Beispiele:" /subsection    
vt{{
"Überweisung " "SA-SBD1D" b\\
"Scheck " "SA-D, Einlösen: SA-DSBD" b\\
"Umtausch/Kauf " "SA+A-DSB¹BD" b\\
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
"$quid: Ethisches Mining" /title
vt{{
"• " "Konzept des Minings: Bewerkstellige harte Arbeit mit rarem Ergebnis" b\\
"• " "Vorschlag: Coupons für die Unterstützung der Entwicklung freier Software" b\\
"• " "Diese Coupons wären dann handelbar" b\\
"• " "Freie Software ist öffentliche Infrastruktur im Informationszeitalter" b\\
"• " "Damit regen wir die Leute an, FOSS aus Eigeninteresse zu unterstützen" b\\
"• " "Sie bekommen ein nutzbares und wertvolles Token zurück" b\\
"• " "Oder sie entwickeln selbst FOSS, weil es Fiatgeld einbringt" b\\
\skip
"Dezentralbank?" /subsection
"• " "Zentralbank gibt Kredite an Großbanken, die sie dann an der Börse verzocken" b\\
"• " "Die Dezentralbank gibt Kredite an Kleinunternehmen" b\\
"• " "Bonitätsprüfung eher wie Crowdfunding" b\\
}}vt
glue*l }}glue
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 17
{{
$FFFFFFFF pres-frame
{{
"Literatur & Links" /title
vt{{
"Bernd Paysan " "net2o fossil repository" bi\\
"" "https://fossil.net2o.de/net2o/" bm\\
"Bernd Paysan " "$quid cryptocurrency & SwapDragonChain" bi\\
"" "https://squid.cash/" bm\\
}}vt
glue*l }}glue
tex: qr-code
' qr-code "qr-code.png" 13e }}image-file drop /center
qr-code nearest
glue*l }}glue
}}v box[] >bdr
}}z box[] /flip dup >slides

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

: !widgets ( -- ) top-widget .htop-resize
    1e ambient% sf! set-uniforms ;

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

0 [IF]
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
     (("{{") (0 . 2) (0 . 2) immediate)
     (("}}h" "}}v" "}}z" "}}vp" "}}p") (-2 . 0) (-2 . 0) immediate)
    )
End:
[THEN]
