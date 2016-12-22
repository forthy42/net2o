\ scan color QR codes on Android

\ Copyright (C) 2016   Bernd Paysan

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

require minos2/android-recorder.fs

\ scan matrix manipulation

Create scan-matrix
1.0e sf, 0.0e sf, 0.0e sf, 0.0e sf,
0.0e sf, 1.0e sf, 0.0e sf, 0.0e sf,
0.0e sf, 0.0e sf, 1.0e sf, 0.0e sf,
0.0e sf, 0.0e sf, 0.0e sf, 1.0e sf,

32 sfloats buffer: scan-inverse

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

also opengl also android

: v2scale ( x y scale -- ) ftuck f* frot frot f* fswap ;

: draw-scan ( direction -- )
    \G draw a scan rotated by rangle
    v0 i0 >v
    1e fdup fnegate { f: s f: -s }
     -s  s >xy n> rot>st   $000000FF rgba>c v+
      s  s >xy n> rot>st   $000000FF rgba>c v+
      s -s >xy n> rot>st   $000000FF rgba>c v+
     -s -s >xy n> rot>st   $000000FF rgba>c v+
    v> drop 0 i, 1 i, 2 i, 0 i, 2 i, 3 i,
    GL_TRIANGLES draw-elements ;

: scan-frame0 ( -- )
    0e fdup x-pos sf! >y-pos
    unit-matrix MVMatrix set-matrix 0 draw-scan ;

Variable scan-buf1
Variable red-buf
Variable green-buf
Variable blue-buf

$E8 Value color-level#

: extract-buf ( offset buf -- )
    buf-len over $!len
    $@ drop swap
    scan-buf1 $@ >r + r> bounds ?DO  0
	I 8 sfloats bounds DO
	    2* I c@ color-level# > -
	cell +LOOP  over c! 1+
    8 sfloats +LOOP  drop ;

: extract-red   ( -- )  0 red-buf   extract-buf ;
: extract-green ( -- )  1 green-buf extract-buf ;
: extract-blue  ( -- )  2 blue-buf  extract-buf ;

: .buf ( addr -- )
    [: 0 swap $@ bounds ?DO  cr
	    dup 3 .r space 1+
	    I scan-w 2 rshift bounds ?DO
		I c@ 0 <# # # #> type
	    LOOP
	scan-w 2 rshift +LOOP drop ;] $10 base-execute ;

: .red   ( -- ) red-buf .buf ;
: .green ( -- ) green-buf .buf ;
: .blue  ( -- ) blue-buf .buf ;

: mixgr>32 ( 16red 16green -- 32result )
    0 $10 0 DO
	2* 2*
	over $E rshift 2 and or >r
	over $F rshift 1 and r> or >r
	2* swap 2* swap r>
    LOOP  nip nip ;

$40 buffer: guessbuf

: >guess ( -- addr u )
    guessbuf
    [ scan-w 2 rshift dup scan-w 9 - * swap 2/ 1- + ]L
    [ scan-w 2 rshift dup scan-w 7 + * swap 2/ 1- + ]L DO
	red-buf   $@ drop I + be-uw@
	green-buf $@ drop I + be-uw@ mixgr>32
	over be-l! 4 +
    [ scan-w 2 rshift ]L -LOOP
    drop guessbuf $40 ;

$8 buffer: guessecc1
#18 buffer: guessecc2

: ecc-hor@ ( off -- w1 w2 ) >r
    red-buf   $@ drop r@ + be-uw@
    green-buf $@ drop r> + be-uw@ ;
: ecc-ver@ ( -- )
    guessecc2
    [ scan-w 2 rshift dup scan-w #10 - * swap 2/ 1- + ]L
    [ scan-w 2 rshift dup scan-w 8 + * swap 2/ 1- + ]L DO
	red-buf   $@ drop I + 1 - c@ 1 and 2*
	green-buf $@ drop I + 1 - c@ 1 and or 2*
	red-buf   $@ drop I + 2 + c@ 7 rshift 1 and or 2*
	green-buf $@ drop I + 2 + c@ 7 rshift 1 and or 6 xor
	over c! 1+
    [ scan-w 2 rshift ]L -LOOP drop ;

: >guessecc ( -- )
    [ scan-w 2 rshift dup scan-w 9 - * swap 2/ 1- + ]L ecc-hor@
    mixgr>32 guessecc1 be-l!
    [ scan-w 2 rshift dup scan-w 8 + * swap 2/ 1- + ]L ecc-hor@
    mixgr>32 guessecc1 4 + be-l!
    ecc-ver@ ;
: >ecc-row ( addr u -- value )
    0 -rot bounds ?DO  I be-ul@ xor  4 +LOOP ;
: >ecc-row2 ( addr u -- value )
    0 -rot bounds ?DO
	I     be-ul@ xor
	I 4 + be-ul@ dup $AAAAAAAA and 1 rshift swap $55555555 and 2* or xor
    8 +LOOP ;
: ecc-ok? ( addr u -- flag )  2dup 0 skip nip 0<> >r
    >ecc-row  guessecc1     be-ul@ invert = >r
    >ecc-row2 guessecc1 4 + be-ul@        = r> and r> and ;

: |min| ( a b -- ) over abs over abs < select ;

$8000 Constant init-xy

: search-corner { mask -- x y } init-xy dup { x y }
    scan-buf1 $@ drop
    scan-w dup negate DO
	scan-w dup negate DO
	    dup c@ color-level# u>= 1 and
	    over 1+ c@ color-level# u>= 2 and or
	    over 2 + c@ color-level# u>= 4 and or
	    mask = IF
		I dup * J dup * +
		x dup * y dup * + u< IF
		    I to x  J to y
		THEN
	    THEN
	    sfloat+
	LOOP
    LOOP  drop x y ;

: scanbuf@ ( x y -- rgba )
    scan-w + >r scan-w + r>
    scan-w 2* * + sfloats scan-buf1 $@ drop + be-ul@ ;

2Variable p0 \ top left
2Variable p1 \ top right
2Variable p2 \ bottom left
2Variable p3 \ bottom right
2Variable px ( cross of the two lines )

: search-corners ( -- )
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

: scan-grab ( -- )
    0 0 scan-w 2* dup
    2dup * sfloats scan-buf1 $!len
    GL_RGBA GL_UNSIGNED_BYTE scan-buf1 $@ drop glReadPixels ;

: .xpoint ( x y -- )
    p0 2@ swap . . space
    p1 2@ swap . . space
    p2 2@ swap . . space
    p3 2@ swap . . space
    fswap f. f. cr ;

tex: scan-tex
0 Value scan-fb
20.2e FValue scansize

: new-scantex ( -- )
    scan-tex  0e 0e 0e 1e glClearColor
    scan-w 2* dup GL_RGBA new-textbuffer to scan-fb ;
: scale+rotate ( -- )
    p1 2@ p0 2@ p- p3 2@ p2 2@ p- p+ p2/
    s>f scansize f/ y-rots sf!  s>f scansize f/ x-scale sf!
    p0 2@ p2 2@ p- p1 2@ p3 2@ p- p+ p2/
    s>f scansize f/ y-scale sf!  s>f scansize f/ x-rots sf! ;
: set-scan' ( -- )
    compute-xpoint ( .. x y )
    scale+rotate
    scan-w fm/ y-spos sf!
    scan-w fm/ x-spos sf! ;

: scan-legit ( -- ) \ resize a legit QR code
    init-scan' set-scan' >scan-matrix
    scan-matrix MVPMatrix set-matrix
    scan-matrix MVMatrix set-matrix clear
    0 draw-scan scan-grab ;

: visual-frame ( -- )
    oes-program init
    unit-matrix MVPMatrix set-matrix
    unit-matrix MVMatrix set-matrix
    media-tex nearest-oes
    screen-orientation draw-scan sync ;

Defer scan-result ( -- )

Variable skip-frames
8 Value skip-frames#

: scan-once ( -- )
    camera-init scan-w 2* dup scan-fb >framebuffer
    scan-frame0 scan-grab search-corners
    ?legit IF  scan-legit  0>framebuffer
	skip-frames @ 0= IF  visual-frame  THEN
	extract-red extract-green >guess
	>guessecc 2dup ecc-ok? skip-frames @ 0= and IF
	    scan-result  level# @ 0> level# +!
	ELSE  2drop  THEN
    ELSE  0>framebuffer skip-frames @ 0= IF  visual-frame  THEN THEN
    need-sync off skip-frames @ 0> skip-frames +! ;
: scan-loop ( -- )
    1 level# +!  BEGIN  scan-once >looper level# @ 0= UNTIL ;
: scan-start ( -- )
    hidekb >changed  hidestatus >changed  screen+keep
    c-open-back to camera  scan-fb 0= IF  new-scantex  THEN
    ['] VertexShader ['] FragmentShader create-program to program
    .01e 100e dpy-w @ dpy-h @ min s>f f2/ 100 fm* >ap
    cam-prepare  skip-frames# skip-frames ! ;

: scan-key? ( -- flag )  defers key?  scan-once ;

: scan-bg ( -- )  scan-start ['] scan-key? is key?
    [: 85type space guessecc1 8 xtype cr ;] is scan-result ;
: scan-end ( -- )
    [ what's key? ]L is key? cam-end screen-keep showstatus ;
: scan-qr ( -- )
    scan-start  scan-loop  cam-end  screen-keep showstatus ;

previous previous

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