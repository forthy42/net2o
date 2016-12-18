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

scan-matrix  0 sfloats + Constant x-scale
scan-matrix  1 sfloats + Constant y-rots
scan-matrix  4 sfloats + Constant x-rots
scan-matrix  5 sfloats + Constant y-scale
scan-matrix 12 sfloats + Constant x-spos
scan-matrix 13 sfloats + Constant y-spos

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

$D0 Value color-level#

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
    p0 2@ s>f s>f fswap { f: x0 f: y0 }
    p3 2@ s>f s>f fswap { f: x1 f: y1 }
    p1 2@ s>f s>f fswap { f: x2 f: y2 }
    p2 2@ s>f s>f fswap { f: x3 f: y3 }
    x0 y1 f* y0 x1 f* f- { f: dxy01 }
    x2 y3 f* y2 x3 f* f- { f: dxy23 }
    x0 x1 f- y2 y3 f- f* y0 y1 f- x2 x3 f- f* f- { f: det1 }
    dxy01 x2 x3 f- f* dxy23 x0 x1 f- f* f- det1 f/ { f: x }
    dxy01 y2 y3 f- f* dxy23 y0 y1 f- f* f- det1 f/ { f: y }
    x f>s y f>s px 2!  x y ;

: p+ ( x1 y1 x2 y2 -- x1+x2 y1+y2 )
    rot + >r + r> ;
: p2* ( x1 y1 -- x2 y2 )
    2* swap 2* swap ;
: p2/ ( x1 y1 -- x2 y2 )
    2/ swap 2/ swap ;
: p- ( x1 y1 x2 y2 -- x1-x2 y1-y2 )
    rot swap - >r - r> ;


: delta-x ( -- r )
    p0 2@ p1 2@ p- dup * swap dup * + s>f fsqrt
    p2 2@ p3 2@ p- dup * swap dup * + s>f fsqrt f+ f2/ ;
: delta-y ( -- r )
    p0 2@ p2 2@ p- dup * swap dup * + s>f fsqrt
    p1 2@ p3 2@ p- dup * swap dup * + s>f fsqrt f+ f2/ ;

: compute-angle { f: dx f: dy -- rangle }
    p0 2@ p1 2@ p+ px 2@ p2* p- s>f s>f         fswap         fatan2
    p2 2@ p3 2@ p+ px 2@ p2* p- s>f s>f fnegate fswap fnegate fatan2
    f+ f2/ ;

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

: new-scantex ( -- )
    scan-tex  0e 0e 0e 1e glClearColor
    scan-w 2* dup GL_RGBA new-textbuffer to scan-fb ;
: scan-legit ( -- ) \ resize a legit QR code
    delta-x delta-y { f: dx f: dy }
    compute-xpoint
    dx dy compute-angle { f: angle }
    $13 s>f dx f/ { f: sx } $13 s>f dy f/ { f: sy }
    angle fsincos fover fover
    sx f* x-scale sf! sy f* y-rots  sf!
    fswap fnegate
    sx f* x-rots  sf! sy f* y-scale sf!
    scan-w negate fm/ fswap  scan-w negate fm/  fover fover  fswap
    y-scale sf@ f* fswap y-rots sf@ f* f+ y-spos sf!
    x-scale sf@ f* fswap x-rots sf@ f* f+ x-spos sf!
    scan-matrix MVPMatrix set-matrix
    scan-matrix MVMatrix set-matrix clear
    0 draw-scan scan-grab ;

: visual-frame ( -- )
    oes-program init
    unit-matrix MVPMatrix set-matrix
    unit-matrix MVMatrix set-matrix
    media-tex nearest-oes
    screen-orientation draw-scan sync ;

: scan-once ( -- )
    camera-init scan-w 2* dup scan-fb >framebuffer
    scan-frame0 scan-grab search-corners
    ?legit IF  scan-legit  0>framebuffer
	visual-frame x-spos sf@ y-spos sf@ .xpoint
	extract-red extract-green >guess 85type cr
    ELSE  0>framebuffer ." not legit" cr  THEN
    need-sync off ;
: scan-loop ( -- )
    1 level# +!  BEGIN  scan-once >looper level# @ 0= UNTIL ;
: scan-start ( -- )  hidekb
    c-open-back to camera  scan-fb 0= IF  new-scantex  THEN
    ['] VertexShader ['] FragmentShader create-program to program
    .01e 100e dpy-w @ dpy-h @ min s>f f2/ 100 fm* >ap
    cam-prepare ;

: scan-key? ( -- flag )  defers key?  scan-once ;

: scan-bg ( -- )  scan-start ['] scan-key? is key? ;
: scan-end ( -- )
    [ what's key? ]L is key? cam-end ;

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