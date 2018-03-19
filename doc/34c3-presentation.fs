\ Presentation on MINOS2 made in MINOS2

\ Copyright (C) 2017 Free Software Foundation, Inc.

\ This file is part of Gforth.

\ Gforth is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public License
\ as published by the Free Software Foundation, either version 3
\ of the License, or (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

\ You should have received a copy of the GNU General Public License
\ along with this program. If not, see http://www.gnu.org/licenses/.

require minos2/widgets.fs

[IFDEF] android
    hidekb also android >changed hidestatus >changed previous
[THEN]

also minos

dpy-w @ s>f 42e f/ fround FValue fontsize#
fontsize# 70% f* fround FValue smallsize#
fontsize# f2* FValue largesize#
dpy-h @ s>f dpy-w @ s>f f/ .42e f/ FValue baselinesmall#
dpy-h @ s>f dpy-w @ s>f f/ .33e f/ FValue baselinemedium#
dpy-w @ s>f 1280e f/ FValue pixelsize#

: update-size# ( -- )
    dpy-w @ s>f 42e f/ fround to fontsize#
    fontsize# 2 3 fm*/ fround to smallsize#
    fontsize# f2* to largesize#
    dpy-h @ s>f dpy-w @ s>f f/ .42e f/ to baselinesmall#
    dpy-h @ s>f dpy-w @ s>f f/ .33e f/ to baselinemedium#
    dpy-w @ s>f 1280e f/ to pixelsize# ;

also freetype-gl

[IFDEF] android
    "/system/fonts/DroidSans.ttf"
[ELSE]
    "/usr/share/fonts/truetype/NotoSans-Regular.ttf"
    2dup file-status nip [IF]
	2drop "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf"
	2dup file-status nip [IF]
	    "/usr/share/fonts/truetype/LiberationSans-Regular.ttf"
	    2dup file-status nip [IF]
		2drop "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf"
		[THEN]
	[THEN]
    [THEN]
[THEN]
2dup file-status throw drop 2Constant latin-font

[IFDEF] android
    "/system/fonts/DroidSansMono.ttf"
[ELSE]
    "/usr/share/fonts/truetype/LiberationMono-Regular.ttf"
    2dup file-status nip [IF]
	2drop "/usr/share/fonts/truetype/liberation/LiberationMono-Regular.ttf"
	2dup file-status nip [IF]
	    2drop "/usr/share/fonts/truetype/NotoSans-Regular.ttf"
	    2dup file-status nip [IF]
		2drop "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf"
	    [THEN]
	[THEN]
    [THEN]
[THEN]
2dup file-status throw drop 2Constant mono-font

[IFDEF] android
    "/system/fonts/DroidSans.ttf"
[ELSE]
    "/usr/share/fonts/truetype/NotoSans-Italic.ttf"
    2dup file-status nip [IF]
	2drop "/usr/share/fonts/truetype/noto/NotoSans-Italic.ttf"
	2dup file-status nip [IF]
	    2drop "/usr/share/fonts/truetype/LiberationSans-Italic.ttf"
	    2dup file-status nip [IF]
		2drop "/usr/share/fonts/truetype/liberation/LiberationSans-Italic.ttf"
	    [THEN]
	[THEN]
    [THEN]
[THEN]
2dup file-status throw drop 2Constant italic-font

[IFDEF] android
    "/system/fonts/DroidSansFallback.ttf"
    2dup file-status nip [IF]
	2drop "/system/fonts/NotoSansSC-Regular.otf" \ for Android 6
	2dup file-status nip [IF]
	    2drop "/system/fonts/NotoSansCJK-Regular.ttc" \ for Android 7
	[THEN]
    [THEN]
[ELSE]
    "/usr/share/fonts/truetype/gkai00mp.ttf"
    2dup file-status nip [IF]
	2drop "/usr/share/fonts/truetype/arphic-gkai00mp/gkai00mp.ttf"
	2dup file-status nip [IF]
	    "/usr/share/fonts/truetype/NotoSerifSC-Regular.otf"
	    2dup file-status nip [IF]
		2drop "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc"
	    [THEN]
	[THEN]
    [THEN]
[THEN]
2dup file-status throw drop 2constant chinese-font

0 0
[IFDEF] android \ system default is noto-emoji
    2drop "/system/fonts/SamsungColorEmoji.ttf"
    2dup file-status nip [IF]
	2drop "/system/fonts/NotoColorEmoji.ttf"
	2dup file-status nip [IF]
	    2drop 0 0
	[THEN]
    [THEN]
[ELSE] \ noto-emoji, emojione, twemoji in that order
    2drop "/usr/share/fonts/truetype/NotoColorEmoji.ttf"
    2dup file-status nip [IF]
	2drop "/usr/share/fonts/truetype/emoji/NotoColorEmoji.ttf"
	2dup file-status nip [IF]
	    2drop "/usr/share/fonts/truetype/emojione-android.ttf"
	    2dup file-status nip [IF]
		2drop "/usr/share/fonts/truetype/emoji/emojione-android.ttf"
		2dup file-status nip [IF]
		    2drop "/usr/share/fonts/truetype/TwitterColorEmojiv2.ttf
		    2dup file-status nip [IF]
			2drop "/usr/share/fonts/truetype/emoji/TwitterColorEmojiv2.ttf
			2dup file-status nip [IF]
			    2drop 0 0
			[THEN]
		    [THEN]
		[THEN]
	    [THEN]
	[THEN]
    [THEN]
[THEN]
2dup d0<> [IF] 2constant emoji-font [ELSE] 2drop [THEN]
	

atlas fontsize# latin-font open-font   Value font1
atlas fontsize# mono-font  open-font   Value font1m
atlas smallsize# latin-font open-font  Value font1s
atlas largesize# latin-font open-font  Value font1l
atlas fontsize# italic-font open-font  Value font1i
atlas fontsize# chinese-font open-font Value font2
[IFDEF] emoji-font
    atlas-bgra fontsize# emoji-font open-font Value font-e
[THEN]
previous

$000000FF Value x-color
font1 Value x-font
largesize# FValue x-baseline
: small font1s to x-font ;
: medium font1 to x-font ;
0 warnings !@
: italic font1i to x-font ;
warnings ! \ we already have italic for ANSI
: mono   font1m to x-font ;
: large font1l to x-font ;
: chinese font2 to x-font ;
: blackish $000000FF to x-color ;
: dark-blue $0000bFFF to x-color ;
0e FValue x-border
: }}text ( addr u -- o )
    text new >o x-font text! x-color to text-color  x-border to border o o> ;
: }}smalltext ( addr u -- o )
    text new >o font1s text! x-color to text-color  x-border to border o o> ;
: }}emoji ( addr u -- o )
    emoji new >o font-e text! $FFFFFFFF to text-color  x-border to border o o> ;
: }}edit ( addr u -- o )
    edit new >o x-font edit! x-color to text-color  x-border to border o o> ;
: >bl ( o -- o' )
    >o x-baseline to baseline o o> ;
: >bdr ( o -- o' )
    >o fontsize# to border o o> ;
: /center ( o -- o' )
    >r {{ glue*1 }}glue r> glue*1 }}glue }}h box[] >bl ;
: /left ( o -- o' )
    >r {{ r> glue*1 }}glue }}h box[] >bl ;
: \\ }}text /left ;
: e\\ }}emoji >r }}text >r {{ r> r> glue*1 }}glue }}h box[] >bl ;
: /right ( o -- o' )
    >r {{ glue*1 }}glue r> }}h box[] >bl ;
: /flip ( o -- o )
    >o box-hflip# box-flags ! o o> ;
: /flop ( o -- o )
    >o 0 box-flags ! o o> ;
: }}image-file ( xt addr u r -- o glue-o ) pixelsize# f*
    2 pick execute
    load-texture glue new >o
    s>f fover f* vglue-c df!
    s>f       f* hglue-c df! o o> dup >r
    $ffffffff rot }}image r> ;
: }}image-tex ( xt glue -- o )
    $ffffffff rot }}image ;

glue new Constant glue-left
glue new Constant glue-right
glue new Constant glue*wh
glue new Constant glue*b0
glue new Constant glue*b1
glue new Constant glue*b2

: update-glue
    glue*wh >o 0g 0g dpy-w @ s>f smallsize# f2* f- hglue-c glue!
    0glue dglue-c glue! 1glue vglue-c glue! o>
    glue*b0 >o dpy-w @ s>f .05e f* 0g 0g hglue-c glue! o>
    glue*b1 >o dpy-w @ s>f .12e f* 0g 0g hglue-c glue! o>
    glue*b2 >o dpy-w @ s>f .20e f* 0g 0g hglue-c glue! o> ;

update-glue

: b0 ( addr1 u1 -- o )
    dark-blue }}text >r
    {{ glue*b0 }}glue {{ glue*1 }}glue r> }}h box[] }}z box[] ;
: b1 ( addr1 u1 -- o )
    dark-blue }}text >r
    {{ glue*b1 }}glue {{ glue*1 }}glue r> }}h box[] }}z box[] ;
: b2 ( addr1 u1 -- o )
    dark-blue }}text >r
    {{ glue*b2 }}glue {{ glue*1 }}glue r> }}h box[] }}z box[] ;
: b\\ ( addr1 u1 addr2 u2 -- o ) \ blue black newline
    2swap b0 >r
    blackish }}text >r
    {{ r> r> swap glue*1 }}glue }}h box[] >bl ;
: bb\\ ( addr1 u1 addr2 u2 -- o ) \ blue black newline
    2swap b1 >r
    blackish }}text >r
    {{ r> r> swap glue*1 }}glue }}h box[] >bl ;
: bbe\\ ( addr1 u1 addr2 u2 addr3 u3 -- o ) \ blue black emoji newline
    2rot b1 >r
    2swap blackish }}text >r
    }}emoji >r
    {{ r> r> r> swap rot glue*1 }}glue }}h box[] >bl ;
: b2\\ ( addr1 u1 addr2 u2 -- o ) \ blue black newline
    2swap b2 >r
    blackish }}text >r
    {{ r> r> swap glue*1 }}glue }}h box[] >bl ;
: b2i\\ ( addr1 u1 addr2 u2 -- o ) \ blue black newline
    2swap b2 >r
    blackish italic }}text >r
    {{ r> r> swap glue*1 }}glue }}h box[] >bl ;
: \LaTeX ( -- )
    "L" }}text
    "A" }}smalltext >o fontsize# fdup -0.23e f* to raise -0.3e f* to kerning o o>
    "T" }}text >o fontsize# -0.1e f* to kerning o o>
    "E" }}text >o fontsize# -0.23e f* fdup fnegate to raise to kerning o o>
    "X" }}text >o fontsize# -0.1e f* to kerning o o> ;

Variable slides[]
Variable slide#

0 Value n2-img
0 Value m2-img
0 Value $q-img

5 Constant n/m-switch
10 Constant m/$-switch

: >slides ( o -- ) slides[] >stack ;

: glue0 ( -- )
    glue-left  >o 0glue hglue-c glue! o>
    glue-right >o 0glue hglue-c glue! o> ;
: !slides ( nprev n -- )
    over >r
    n2-img m2-img $q-img
    r@ m/$-switch u>= IF swap THEN
    r> n/m-switch u>= IF rot  THEN
    /flip drop /flip drop /flop drop
    update-size# update-glue
    slides[] $[] @ /flip drop
    dup slide# ! slides[] $[] @ /flop drop glue0 ;
: anim!slides ( r0..1 n -- )
    slides[] $[] @ /flop drop
    fdup fnegate dpy-w @ fm* glue-left  .hglue-c df!
    -1e f+       dpy-w @ fm* glue-right .hglue-c df! ;

: prev-anim ( n r0..1 -- )
    dup 0<= IF  drop fdrop  EXIT  THEN
    fdup 1e f>= IF  fdrop
	dup 1- swap !slides  EXIT
    THEN
    sin-t 1e fswap f- 1- anim!slides +sync ;

: next-anim ( n r0..1 -- )
    dup slides[] $[]# 1- u>= IF  drop fdrop  EXIT  THEN
    fdup 1e f>= IF  fdrop
	dup 1+ swap !slides  EXIT
    THEN
    sin-t 1+ anim!slides +sync ;

1e FValue slide-time%

: prev-slide ( -- )
    slide-time% anims[] $@len IF  anim-end .2e f*  THEN
    slide# @ ['] prev-anim >animate ;
: next-slide ( -- )
    slide-time% anims[] $@len IF  anim-end .2e f*  THEN
    slide# @ ['] next-anim >animate ;

: slide-frame ( glue color -- o )
    smallsize# }}frame ;

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
	k-f5      of  saturate% sf@ 0.1e f+ saturate% sf!
	    Saturate 1 saturate% opengl:glUniform1fv  +sync endof
	k-f6      of  saturate% sf@ 0.1e f- saturate% sf!
	    Saturate 1 saturate% opengl:glUniform1fv  +sync endof
	k-f7      of  ambient% sf@ 0.1e f+  ambient% sf!
	    Ambient 1 ambient% opengl:glUniform1fv  +sync endof
	k-f8      of  ambient% sf@ 0.1e f-  ambient% sf!
	    Ambient 1 ambient% opengl:glUniform1fv  +sync endof
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

: net2o-img ( -- o )
    x-baseline 0e to x-baseline
    {{
    ['] net2o-logo net2o-glue }}image-tex /right
    glue*1 }}glue
    }}v outside[] >o fontsize# f2/ to border o o>
    to x-baseline ;
: minos2-img ( -- o )
    x-baseline 0e to x-baseline
    {{
    ['] minos2 minos2-glue }}image-tex /right
    glue*1 }}glue
    }}v outside[] >o fontsize# f2/ to border o o>
    to x-baseline ;
: $quid-img ( -- o )
    x-baseline 0e to x-baseline
    {{
    ['] $quid $quid-glue }}image-tex /right
    glue*1 }}glue
    }}v outside[] >o fontsize# f2/ to border o o>
    to x-baseline ;

: pres-frame ( color -- o1 o2 )
    glue*wh swap slide-frame dup .button1 simple[] ;

{{
{{ glue-left }}glue

\ page 0
{{
$FFFFFFFF pres-frame
{{
dark-blue
largesize# to x-baseline
glue*1 }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
large "net2o: GUI, realtime mixnet, $quid " }}text /center
small "($quid = Ethical micropayment with efficient BlockChain)" }}text /center
glue*2 }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
medium "Bernd Paysan" }}text /center
"34c3 Leipzig, #wefixthenet" }}text /center
glue*1 }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
}}v box[] >o fontsize# to border o Value title-page o o>
}}z box[] dup >slides

\ page 1
{{
$FFFFFFFF pres-frame
{{
dark-blue
largesize# to x-baseline
large "Motivation" }}text /center
medium
glue*1 }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
tex: bad-gateway
' bad-gateway "bad-gateway.png" 0.666e }}image-file
Constant bgw-glue /center
glue*1 }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 2
{{
$FF7F7FFF pres-frame
{{
dark-blue
largesize# to x-baseline
large "4 Years after Snowden" }}text /center
blackish
medium "What has changed?" \\
dark-blue "Politics " \\
fontsize# baselinesmall# f* to x-baseline
blackish
"    Fake News/Hate Speech as excuse for censorship #NetzDG" "🤦" e\\
"    Crypto Wars rebranded as “reasonable encryption”" "🤦🤦" e\\
"    Legalize it (dragnet surveillance)" "🤦🤦🤦" e\\
"    Kill the link (EuGH and LG Humbug)" "🤦🤦🤦🤦" e\\
"    Privacy: nobody is forced to use the Interwebs (Jim Sensenbrenner)" "🤦🤦🤦🤦🤦" e\\
"    “Crypto” now means BitCoin" "🤦🤦🤦🤦🤦🤦" e\\
dark-blue "Competition" \\
blackish
"    faces Stasi–like Zersetzung (Tor project)" \\
dark-blue "Solutions" \\
blackish
"    net2o starts becoming useable" \\
glue*1 }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
}}v box[] >o o Value snowden-page fontsize# to border o o>
}}z box[] /flip dup >slides

\ page 3
{{
$BFFFBFFF pres-frame
{{
largesize# to x-baseline
large dark-blue "net2o in a nutshell" }}text /center
medium blackish
"net2o consists of the following 6 layers (implemented bottom up):" \\
{{
"2. " b0 blackish " Path switched packets with 2" }}text
"n" }}smalltext >o fontsize# -0.4e f* to raise o o>
" size writing into shared memory buffers" }}text  glue*1 }}glue }}h box[] >bl
fontsize# baselinesmall# f* to x-baseline
"3. " " Ephemeral key exchange and signatures with Ed25519," b\\
"" " symmetric authenticated encryption+hash+prng with Keccak," b\\
"" " symmetric block encryption with Threefish" b\\
"" " onion routing camouflage probably with AES" b\\
"4. " " Timing driven delay minimizing flow control" b\\
"5. " " Stack–oriented tokenized command language" b\\
"6. " " Distributed data (files, messages) and distributed metadata (DHT)" b\\
"7. " " Apps in a sandboxed environment for displaying content" b\\
glue*1 }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 4
{{
$BFFFBFFF pres-frame
{{
largesize# to x-baseline
large dark-blue "Realtime Mixnet" }}text /center
medium blackish
"Problem with onion routing: Timing correlation" \\
fontsize# baselinesmall# f* to x-baseline
"Problem with mixnets: need to wait for enough messages" \\
"Solution: Fill up the output with constant bandwidth garbage" \\
"and otherwise set up the network as with a mixnet" \\
"Bonus: Evenly distribute packets over a set of mix routes, to get more bandwidth" \\
glue*1 }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 5
{{
$BFBFFFFF pres-frame
{{
largesize# to x-baseline
large dark-blue "ΜΙΝΟΣ2 technology" }}text /center
medium blackish
"ΜΙΝΟΣ2 starts at the DOM layer" \\
"Rendering:" " OpenGL (ES), Vulkan backend possible" b2\\
fontsize# baselinesmall# f* to x-baseline
"Font to texture:" " Freetype–GL (with own improvements)" b2\\
"Image to texture:" " SOIL2 (needs some bugs fixed)" b2\\
"Video to texture:" " OpenMAX AL (Android), gstreamer for Linux (planned)" b2\\
"Coordinates:" " Single float, origin bottom left" b2\\
{{ "Typesetting:" b2 blackish
" Boxes&Glues close to " }}text
\LaTeX
" — including ascender&descender" }}text glue*1 }}h box[] >bl
"" " Glues can shrink, not just grow" b2\\
"Object System:" " extremely lightweight Mini–OOF2" b2\\
"Class number:" " Few classes, many possible combinations" b2\\
glue*1 }}glue \ ) $CCDDDD3F 4e }}frame dup .button1
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 6
{{
$FFBFFFFF pres-frame
{{
largesize# to x-baseline
large dark-blue "ΜΙΝΟΣ2 Widgets" }}text /center
medium blackish
"Design principle is a Lego–style combination of many extremely simple objects" \\
fontsize# baselinesmall# f* to x-baseline
"actor" " base class that reacts on all actions (clicks, touchs, keys)" bb\\
"widget" " base class for all visible objects" bb\\
{{ "edit" b1 blackish " editable text element " }}text
chinese "新年快乐！" }}edit dup Value edit-field glue*1 }}glue }}h edit-field edit[] >bl
medium "glue" " base class for flexible objects" bb\\
"tile" " colored rectangle" bb\\
"frame" " colored rectangle with borders" bb\\
"text" " text element" bb\\
[IFDEF] emoji-font
    "emoji" " emoji element " "😀🤭😁😂😇😈🙈🙉🙊💓💔💕💖💗💘🍺🍻🎉🎻🎺🎷" bbe\\
[ELSE]
    "emoji" " emoji element (no emoji font found)" bb\\
[THEN]
"icon" " image from an icon texture" bb\\
"image" " larger image" bb\\
"animation" " action for animations" bb\\
"canvas" " vector graphics (TBD)" bb\\
"video" " video player (TBD)" bb\\
glue*1 }}glue
}}v box[] >bdr
}}z box[]
/flip dup >slides

\ page 7
{{
$BFFFFFFF pres-frame
{{
largesize# to x-baseline
large dark-blue "ΜΙΝΟΣ2 Boxes" }}text /center
medium blackish
{{
"Just like " }}text
\LaTeX
", boxes arrange widgets/text" }}text glue*1 }}h box[]
>bl
fontsize# baselinemedium# f* to x-baseline
"hbox" " Horizontal box, common baseline" bb\\
fontsize# baselinesmall# f* to x-baseline
"vbox" " Vertical box, minimum distance a baselineskip (of the hboxes below)" bb\\
"zbox" " Overlapping several boxes" bb\\
"grid" " Free widget placements (TBD)" bb\\
fontsize# baselinemedium# f* to x-baseline
"There will be some more variants for tables and wrapped paragraphs" \\
glue*1 }}glue
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 8
{{
$FFFFBFFF pres-frame
{{
largesize# to x-baseline
large dark-blue "ΜΙΝΟΣ2 Displays" }}text /center
medium blackish
"Render into different kinds of displays" \\
fontsize# baselinemedium# f* to x-baseline
"viewport" " Into a texture, used as viewport" bb\\
fontsize# baselinesmall# f* to x-baseline
"display" " To the actual display" bb\\
glue*1 }}glue
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 9
{{
$BFDFFFFF pres-frame
{{
largesize# to x-baseline
large dark-blue "Minimize Draw Calls" }}text /center
medium blackish
"OpenGL wants as few draw–calls per frame, so different contexts are drawn" \\
fontsize# baselinesmall# f* to x-baseline
"in stacks with a draw–call each" \\
fontsize# baselinemedium# f* to x-baseline
"init" " Initialization round" bb\\
fontsize# baselinesmall# f* to x-baseline
"bg" " Background round" bb\\
"icon" " draw items of the icon texture" bb\\
"thumbnail" " draw items of the thumbnail texture" bb\\
"image" " images with one draw call per image" bb\\
"marking" " cursor/selection highlight round" bb\\
"text" " text round" bb\\
"emoji" " emoji round" bb\\
glue*1 }}glue
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 10
{{
$D4AF37FF pres-frame
{{
largesize# to x-baseline
large dark-blue "$quid & SwapDragonChain" }}text /center
medium blackish
"Topics:" \\
"Money" " What’s that all about?" bb\\
fontsize# baselinesmall# f* to x-baseline
"BitCoin" " Shortcomings of a first proof of concept" bb\\
"Wealth" " Ethical implication in deflationary systems" bb\\
"Proof of" " Trust instead Work" bb\\
"BlockChain" " What’s the actual point?" bb\\
"Scale" " How to scale a BlockChain?" bb\\
"$quid" " Ethical ways to create money" bb\\
glue*1 }}glue
}}v box[] >bdr
{{
glue*1 }}glue
tex: $quid-logo-large
' $quid-logo-large "squid-logo.png" 0.666e }}image-file drop /right
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 11
{{
{{
$e4cF77FF pres-frame
{{
largesize# to x-baseline
large dark-blue "What’s Money?" }}text /center
medium blackish
"Commodity ~: " "Objects with inherent value" b2\\
fontsize# baselinesmall# f* to x-baseline medium
"Promissory note: " "Bank created paper for commodity" b2\\
"Representative ~: " "Promise to exchange with “standard object” (e.g. gold)" b2\\
"Fiat ~: " "No inherent value; promise, if any, as legal tender" b2\\
"Legal tender: " "Medium of payment by law" b2\\
glue*1 }}glue
}}v box[] >bdr
{{
glue*1 }}glue
{{
tex: shell-coins
tex: feiqian
tex: huizi
tex: chao
glue*1 }}glue
' shell-coins "shell-coins.png" 0.666e }}image-file drop
glue*1 }}glue
' feiqian "feiqian.png" 0.666e }}image-file drop
glue*1 }}glue
' huizi "huizi.png" 0.666e }}image-file drop
glue*1 }}glue
' chao "chao.jpg" 0.666e }}image-file drop
glue*1 }}glue
}}h box[]
}}v box[] >bdr
}}z box[]
tex: vp1 glue*1 ' vp1 }}vp vp[]
/flip dup >slides

\ page 12
{{
$f4cF57FF pres-frame
{{
largesize# to x-baseline
large dark-blue "BitCoins — early “Crypto” shortcomings" }}text /center
medium blackish
"•  Proof of work: wasteful and yet only marginally secure" \\
fontsize# baselinesmall# f* to x-baseline medium
"•  Inflation is money’s cancer, deflation its infarct" \\
"•  Consequences: unstable exange rate, high transaction fees" \\
"•  Ponzi scheme–style bubble" \\
"•  (Instead of getting Viagra spam I now get BitCoin spam)" \\
"•  Can’t even do the exchange transaction on–chain" \\
glue*1 }}glue
}}v box[] >bdr
{{
glue*1 }}glue
tex: bitcoin-bubble
' bitcoin-bubble "bitcoin-bubble.png" 0.85e }}image-file drop /right
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 13
{{
$e4df67ff pres-frame
{{
largesize# to x-baseline
large dark-blue "Wealth & Ethics" }}text /center
medium blackish
"•  Huge first mover advantage" \\
fontsize# baselinesmall# f* to x-baseline medium
"•  Already worse wealth distribution than neoliberal economy" \\
"•  Huge inequality drives society into servitude, not into freedom" \\
"•  No concept of a credit" \\
"•  Lightning network also binds assets (will have fees as consequence)" \\
glue*1 }}glue
}}v box[] >bdr
{{
glue*1 }}glue
tex: free-market
' free-market "free-market.jpg" 0.666e }}image-file drop /right
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 14
{{
$a4df87ff pres-frame
{{
largesize# to x-baseline
large dark-blue "Proof of What?!" }}text /center
medium blackish
"Challenge" " Avoid double–spending" b2\\
fontsize# baselinesmall# f* to x-baseline medium
"State of the art:" " Proof of work" b2\\
"Problem:" " Proof of work burns energy and GPUs" b2\\
"Suggestion 1:" " Proof of stake (money buys influence)" b2\\
"Problem:" " Money corrupts, and corrupt entities misbehave" b2\\
"Suggestion 2:" " Proof of well–behaving (trust, trustworthyness)" b2\\
"How?" " Having signed many blocks in the chain gains points" b2\\
"Multiple signers" " Not only have one signer, but many" b2\\
"Suspicion" " Don't accept transactions in low confidence blocks" b2\\
"Idea" " Repeated prisoner’s dilemma rewards cooperation" b2\\
largesize# to x-baseline
"BTW: The attack for double spending also requires a MITM–attack" \\
glue*1 }}glue
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 15
{{
$a4df87ff pres-frame
{{
largesize# to x-baseline
large dark-blue "BlockChain" }}text /center
medium blackish
"•  Banks distrust each others, too (i. e. GNU Taler is not a solution)" \\
fontsize# baselinesmall# f* to x-baseline medium
"•  Problem size: WeChat Pay peaks at 0.5MTPS (BTC at 5TPS)" \\
"•  Lightning Network doesn’t stand an overrun–the–arbiter attack" \\
"•  Therefore, the BlockChain itself needs to scale" \\
largesize# to x-baseline
"•  Introduce double entry booking into the distributed ledger" \\
fontsize# baselinesmall# f* to x-baseline medium
"•  Partitionate the ledgers by coin pubkey" \\
"•  Use an n–dimensional ledger space to route transactions" \\
glue*1 }}glue
}}v box[] >bdr
{{
glue*1 }}glue
tex: stage1
tex: stage2
tex: bank-robs-you
{{
{{
glue*1 }}glue
{{
' stage1 "ledger-stage1.png" 0.666e }}image-file drop
' stage2 "ledger-stage2.png" 0.666e }}image-file drop
}}h box[]
}}v box[]
glue*1 }}glue
' bank-robs-you "bank-robs-you.jpg" 0.666e }}image-file drop
}}h box[]
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 16
{{
$a4df87ff pres-frame
{{
largesize# to x-baseline
large dark-blue "$quid: Ethical mining" }}text /center
medium blackish
"•  Concept of mining: Provide difficult and rare work" \\
fontsize# baselinesmall# f* to x-baseline medium
"•  Suggesting: Provide vouchers for free software development sponsorships" \\
"•  These vouchers are tradeable on their own" \\
"•  Free software is public infrastructure for the information age" \\
"•  That way, we can encourage people to sponsor out of self–interest" \\
"•  They get a useful and valueable token back" \\
glue*1 }}glue
}}v box[] >bdr
}}z box[] /flip dup >slides

\ page 17
{{
$FFFFFFFF pres-frame
{{
largesize# to x-baseline
large dark-blue "Literature & Links" }}text /center
medium blackish
"Bernd Paysan " "net2o fossil repository" b2i\\
fontsize# baselinesmall# f* to x-baseline medium
mono "" "https://fossil.net2o.de/net2o/" b2\\
medium blackish
"Bernd Paysan " "$quid cryptocurrency & SwapDragonChain" b2i\\
mono "" "https://squid.cash/" b2\\
glue*1 }}glue
}}v box[] >bdr
}}z box[] /flip dup >slides

\ end
glue-right }}glue
}}h box[]
{{
net2o-img   dup to n2-img
minos2-img  dup to m2-img /flip
$quid-img   dup to $q-img /flip
}}z
}}z slide[]
to top-widget

also opengl

: !widgets ( -- ) top-widget .htop-resize
    .3e ambient% sf! set-uniforms
    -1.6e 1.6e 3e lightpos glUniform3f ;

previous

also [IFDEF] android android [THEN]

: presentation ( -- )  1config
    [IFDEF] hidestatus hidekb hidestatus [THEN]
    !widgets widgets-loop ;

previous

script? [IF]
    next-arg s" time" str= [IF]  +db time( \ ) [THEN]
    presentation bye
[ELSE]
    presentation
[THEN]
