\ scan color QR codes on Android

\ Copyright (C) 2016-2018   Bernd Paysan

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

require minos2/gl-helper.fs

\ scan matrix manipulation

Create scan-matrix
1.0e sf, 0.0e sf, 0.0e sf, 0.0e sf,
0.0e sf, 1.0e sf, 0.0e sf, 0.0e sf,
0.0e sf, 0.0e sf, 1.0e sf, 0.0e sf,
0.0e sf, 0.0e sf, 0.0e sf, 1.0e sf,

32 sfloats buffer: scan-inverse

20.0e FValue x-scansize
20.75e FValue y-scansize

0.5e FValue y-offset
0.0e FValue x-offset

\ matrix inversion

' dfloats alias 8*
: .mat { mat -- }
    4 0 DO  cr
	8 0 DO
	    mat J 8* I + sfloats + sf@ f.
	LOOP
    LOOP ;
: init-scan' ( -- )
    scan-inverse [ 32 sfloats ]L 2dup erase  bounds ?DO
	1e fdup I sf! I [ 4 sfloats ]L + sf!
    [ 9 sfloats ]L +LOOP ;
: sfax+y8 ( ra addr1 addr2 -- )
    [ 8 sfloats ]L bounds ?DO
	dup sf@ fover I sf@ f* f+ dup sf! sfloat+
    [ 1 sfloats ]L +LOOP  drop fdrop ;
: sfax8 ( ra addr -- )
    [ 8 sfloats ]L bounds ?DO
	fdup I sf@ f* I sf!
    [ 1 sfloats ]L +LOOP  fdrop ;
: tij8 ( addr1 addr2 -- )
    [ 8 sfloats ]L bounds ?DO
	dup sf@ I sf@ dup sf! I sf! sfloat+
    [ 1 sfloats ]L +LOOP  drop ;
    
: matrix-invert4 { mat -- } \ shortcut to invert typical matrix
    mat sf@ fabs mat [ 8 sfloats ]L + sf@ fabs f< IF
	mat dup [ 8 sfloats ]L + tij8 \ exchange two lines
    THEN
    4 0 DO
	4 0 DO
	    mat J [ 9 sfloats ]L * + sf@ 1/f
	    I J <> IF
		mat I 8* sfloats +
		mat J 8* sfloats +
		over J sfloats + sf@ f* fnegate sfax+y8
	    ELSE
		mat J 8* sfloats + sfax8
	    THEN
	    ( mat .mat cr ) \ debugging output
	LOOP
    LOOP ;

scan-inverse  0 sfloats + Constant x-scale
scan-inverse  1 sfloats + Constant y-rots
scan-inverse  8 sfloats + Constant x-rots
scan-inverse  9 sfloats + Constant y-scale
scan-inverse 24 sfloats + Constant x-spos
scan-inverse 25 sfloats + Constant y-spos

: >scan-matrix ( -- )
    scan-inverse matrix-invert4
    scan-matrix [ scan-inverse 4 sfloats + ]L [ 32 sfloats ]L bounds ?DO
	I over [ 4 sfloats ]L move  [ 4 sfloats ]L +
    [ 8 sfloats ]L +LOOP  drop ;

$40 Value scan-w
scan-w dup * 1- 2/ 1+ Constant buf-len

: v2scale ( x y scale -- ) ftuck f* frot frot f* fswap ;

also opengl

: draw-scan ( direction -- )
    \G draw a scan rotated by rangle
    v0 i0 >v
    1e fdup fnegate { f: s f: -s }
     -s  s >xy n> rot>st   $FFFFFFFF rgba>c v+
      s  s >xy n> rot>st   $FFFFFFFF rgba>c v+
      s -s >xy n> rot>st   $FFFFFFFF rgba>c v+
     -s -s >xy n> rot>st   $FFFFFFFF rgba>c v+
    v> drop 0 i, 1 i, 2 i, 0 i, 2 i, 3 i,
    GL_TRIANGLES draw-elements ;

Variable scan-buf-raw
Variable scan-buf0
Variable scan-buf1
Variable red-buf
Variable green-buf
Variable blue-buf

$60 Value blue-level#
$60 Value green-level#
$40 Value red-level#

: extract-buf ( offset buf level -- ) { level }
    buf-len over $!len
    $@ drop swap
    scan-buf1 $@ >r + r> bounds ?DO  0
	I 8 sfloats bounds DO
	    2* I c@ level u< -
	cell +LOOP  over c! 1+
    8 sfloats +LOOP  drop ;

: extract-red   ( -- )  0 red-buf   red-level#   extract-buf ;
: extract-green ( -- )  1 green-buf green-level# extract-buf ;
: extract-blue  ( -- )  2 blue-buf  blue-level#  extract-buf ;

: .buf ( addr -- )
    [: 0 swap $@ bounds ?DO  cr
	    dup 3 .r space 1+
	    I scan-w 2 rshift bounds ?DO
		I c@ 0 <# # # #> type
	    LOOP
	scan-w 2 rshift +LOOP drop ;] $10 base-execute ;

: .red   ( -- ) red-buf   .buf ;
: .green ( -- ) green-buf .buf ;
: .blue  ( -- ) blue-buf  .buf ;

: mixgr>32 ( 16red 16green -- 32result )
    0 $10 0 DO
	2* 2*
	over $E rshift 2 and or >r
	over $F rshift 1 and r> or >r
	2* swap 2* swap r>
    LOOP  nip nip ;

$51 buffer: guessbuf
guessbuf $40 + Constant guessecc
guessecc $10 + Constant guesstag

scan-w 2 rshift constant scan-step
scan-step dup scan-w 9 - * swap 2/ 1- + Constant scan-top
scan-step dup scan-w 7 + * swap 2/ 1- + Constant scan-bot
scan-step dup scan-w 8 + * swap 2/ 1- + Constant scan-ecc

: ecc-hor@ ( off -- w1 w2 ) >r
    red-buf   $@ drop r@ + be-uw@
    green-buf $@ drop r> + be-uw@ mixgr>32 ;
: >guess ( -- addr u )
    guessbuf
    scan-top scan-bot DO
	I ecc-hor@ over be-l! 4 +
    scan-step -LOOP
    drop guessbuf $40 ;

: tag1@ { addr bit -- tag }
    addr red-buf   $@ drop + c@ bit rshift 1 and
    addr green-buf $@ drop + c@ bit rshift 1 and 2* or ;
: ecc-ver@ { off bit -- ul } 0
    scan-top scan-bot DO
	2* 2* I off + bit tag1@ or
    scan-step -LOOP ;
: tag2@ ( addr -- )
    dup 1- 0 tag1@ 2 lshift swap 2 + 7 tag1@ or ;
: tag@ ( -- tag )
    scan-ecc tag2@ 4 lshift
    scan-top tag2@ or ;

: >guessecc ( -- )
    scan-ecc ecc-hor@ guessecc     be-l!
    scan-top ecc-hor@ guessecc 4 + be-l!
    -1 0 ecc-ver@ guessecc 8 + be-l!
    2  7 ecc-ver@ guessecc $C + be-l! ;

[IFDEF] taghash?
    : ecc-ok? ( addrkey u1 addrecc u2 -- flag )
	msg( ." ecc? " 2over xtype space 2dup xtype space x-scansize f. y-scansize f. x-offset f. y-offset f. cr )
	2dup + c@ taghash? ;
[ELSE]
    : ecc-ok? ( addrkey u1 addrecc u2 -- flag )
	2drop 2drop true ;
[THEN]

: |min| ( a b -- ) over abs over abs < select ;

$8000 Constant init-xy

: get-minmax ( addr u -- min max )
    $FF $00 2swap
    bounds ?DO
	I c@ tuck umax >r umin r>
    4 +LOOP ;
: get-minmax-rgb ( -- minr maxr ming maxg minb maxb )
    scan-buf1 $@ swap 3 bounds DO
	I over get-minmax rot
    LOOP  drop ;

: search-corner { mask -- x y } init-xy dup { x y }
    scan-buf0 $@ drop
    scan-w dup negate DO
	scan-w dup negate DO
	    dup      c@ red-level#   u< 1 and
	    over 1+  c@ green-level# u< 2 and or
	    over 2 + c@ blue-level#  u< 4 and or
	    mask = IF
		I dup * J dup * +
		x dup * y dup * + u< IF
		    I to x  J to y
		THEN
	    THEN
	    sfloat+
	LOOP
    LOOP  drop x y ;

2Variable p0 \ top left
2Variable p1 \ top right
2Variable p2 \ bottom left
2Variable p3 \ bottom right
2Variable px ( cross of the two lines )

: search-corners ( -- )
    \ get-minmax-rgb . . . . . . cr
    4 search-corner p0 2! \ top left
    5 search-corner p1 2! \ top right
    6 search-corner p2 2! \ bottom left
    7 search-corner p3 2! \ bottom right
;

: ?legit ( -- flag )
    p0 2@ init-xy dup d<>
    p1 2@ init-xy dup d<> and
    p2 2@ init-xy dup d<> and
    p3 2@ init-xy dup d<> and ;

: compute-xpoint ( -- rx ry )
    p0 2@ s>f s>f { f: y0 f: x0 }
    p3 2@ s>f s>f { f: y1 f: x1 }
    p1 2@ s>f s>f { f: y2 f: x2 }
    p2 2@ s>f s>f { f: y3 f: x3 }
    x0 y1 f* y0 x1 f* f- { f: dxy01 }
    x2 y3 f* y2 x3 f* f- { f: dxy23 }
    x0 x1 f- y2 y3 f- f* y0 y1 f- x2 x3 f- f* f- 1/f { f: det1 }
    dxy01 x2 x3 f- f* dxy23 x0 x1 f- f* f- det1 f* { f: x }
    dxy01 y2 y3 f- f* dxy23 y0 y1 f- f* f- det1 f* { f: y }
    x f>s y f>s px 2!  x y ;

: p+ ( x1 y1 x2 y2 -- x1+x2 y1+y2 )
    rot + >r + r> ;
: p2* ( x1 y1 -- x2 y2 )
    2* swap 2* swap ;
: p2/ ( x1 y1 -- x2 y2 )
    2/ swap 2/ swap ;
: p- ( x1 y1 x2 y2 -- x1-x2 y1-y2 )
    rot swap - >r - r> ;

[IFUNDEF] cam-w
    $100 value cam-w
    $100 value cam-h
[THEN]

: scan-grab-buf ( addr -- )
    >r  0 0 scan-w 2* dup
    2dup * sfloats r@ $!len
    GL_RGBA GL_UNSIGNED_BYTE r> $@ drop glReadPixels ;
: scan-grab-cam ( addr -- )
    >r  0 0 cam-w cam-h
    2dup * sfloats r@ $!len
    GL_RGBA GL_UNSIGNED_BYTE r> $@ drop glReadPixels ;

tex: scan-tex-raw
tex: scan-tex0
tex: scan-tex1

0 Value scan-fb-raw
0 Value scan-fb0
0 Value scan-fb1

: scan-grab-raw ( -- )
    cam-w cam-h scan-fb-raw >framebuffer scan-buf-raw scan-grab-cam ;
: scan-grab0 ( -- )
    scan-w 2* dup scan-fb0 >framebuffer scan-buf0 scan-grab-buf ;
: scan-grab1 ( -- )
    scan-w 2* dup scan-fb1 >framebuffer scan-buf1 scan-grab-buf ;

also soil

: save-png0 ( -- )
    s" scanimg0.png" SOIL_SAVE_TYPE_PNG 128 dup 4 scan-buf0 $@ drop SOIL_save_image ;
: save-png1 ( -- )
    s" scanimg1.png" SOIL_SAVE_TYPE_PNG 128 dup 4 scan-buf1 $@ drop SOIL_save_image ;
: save-png-raw ( -- )
    s" scanimgraw.png" SOIL_SAVE_TYPE_PNG cam-w cam-h 4 scan-buf-raw $@ drop SOIL_save_image ;
: save-pngs ( -- )
    scan-grab0 save-png0
    scan-grab1 save-png1
    scan-grab-raw save-png-raw
    0>framebuffer ;

previous

: .xpoint ( x y -- )
    p0 2@ swap . . space
    p1 2@ swap . . space
    p2 2@ swap . . space
    p3 2@ swap . . space
    fswap f. f. cr ;

: new-scantex-raw ( -- )
    scan-tex-raw 0>clear
    cam-w cam-h GL_RGBA new-textbuffer to scan-fb-raw ;
: new-scantex0 ( -- )
    scan-tex0 0>clear
    scan-w 2* dup GL_RGBA new-textbuffer to scan-fb0 ;
: new-scantex1 ( -- )
    scan-tex1 0>clear
    scan-w 2* dup GL_RGBA new-textbuffer to scan-fb1 ;
: new-scantex ( -- )
    scan-fb-raw 0= IF
	new-scantex-raw new-scantex0 new-scantex1
	0>framebuffer
    THEN ;
: scale+rotate ( -- )
    p1 2@ p0 2@ p- p3 2@ p2 2@ p- p+ p2/
    s>f y-scansize f/ y-rots sf!  s>f x-scansize f/ x-scale sf!
    p0 2@ p2 2@ p- p1 2@ p3 2@ p- p+ p2/
    s>f y-scansize f/ y-scale sf!  s>f x-scansize f/ x-rots sf! ;
: set-scan' ( -- )
    compute-xpoint ( .. x y )
    scale+rotate
    y-offset f+ scan-w fm/ y-spos sf!
    x-offset f+ scan-w fm/ x-spos sf! ;

: scan-legit ( -- ) \ resize a legit QR code
    init-scan' set-scan' >scan-matrix
    scan-w 2* dup scan-fb1 >framebuffer
    scan-matrix MVPMatrix set-matrix
    scan-matrix MVMatrix  set-matrix
    scan-tex-raw linear-mipmap 0 draw-scan scan-grab1 ;

: scan-legit? ( -- addr u flag )
    scan-legit extract-red extract-green >guess
    >guessecc tag@ guesstag c!
    2dup guessecc $10 ecc-ok? ;
: scan-legits? ( -- addr u flag )
    5 0 DO
	I s>f f2/ f2/ to y-offset
	85 80 DO  I s>f f2/ f2/ to y-scansize
	    scan-legit? IF  unloop unloop true  EXIT  THEN
	    2drop
	LOOP
    LOOP  0 0  false ;

: tex-frame ( -- )
    program init-program set-uniforms
    unit-matrix MVPMatrix set-matrix
    unit-matrix MVMatrix set-matrix ;
: draw-scaled ( -- )
    tex-frame scan-w 2* dup scan-fb0 >framebuffer
    scan-tex-raw linear-mipmap 0 draw-scan
    scan-grab0 ;

previous

Variable skip-frames
8 Value skip-frames#

[IFDEF] android
    require android/qrscan-android.fs
[ELSE]
    [IFDEF] linux
	require linux/qrscan-linux.fs
    [THEN]
[THEN]

[IFUNDEF] scan-result
    : scan-result ( addr u tag -- )
	." Scan tag: " hex. cr
	." Scan result: " dump ;
    Variable scanned-flags
[THEN]

: scan-once ( -- )
    draw-cam  draw-scaled
    search-corners
    ?legit IF  scan-legit?
	skip-frames @ 0= and IF
\	    msg( ." scanned ok" cr )
	    guessecc $10 + c@ scan-result
	ELSE  2drop  THEN
    THEN
    skip-frames @ 0> skip-frames +! ;
: scan-loop ( -- )  scanned-flags off \ start with empty flags
    1 level# +!  BEGIN  scan-once >looper level# @ 0= UNTIL ;

[IFDEF] terminal-progam
    : reset-terminal ( -- )
	terminal-program terminal-init
	unit-matrix MVPMatrix set-matrix
	unit-matrix MVMatrix set-matrix
	[IFDEF] screen-keep
	    screen-keep showstatus
	[THEN] ;
[THEN]

: scan-qr ( -- )
    new-scantex  scan-start  ['] scan-loop catch  level# off
    cam-end
    [IFDEF] reset-terminal
	level# @ 0= IF  reset-terminal  THEN
    [THEN]
    dup IF
	." Scan failed" cr
    ELSE
	." Scan completed" cr
    THEN
    throw ;

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]
