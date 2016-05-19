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

16 sfloats buffer: scan-matrix

: matrix-init ( -- )
    ap-matrix scan-matrix 16 sfloats move ;

matrix-init

also opengl also android

: scan-frame0 ( -- )
    ap-matrix MVPMatrix set-matrix
    ap-matrix MVMatrix set-matrix
    screen-orientation v0 i0 >v
    -0.25e -0.25e >xy n> rot>st   $000000FF rgba>c v+
     0.25e -0.25e >xy n> rot>st   $000000FF rgba>c v+
     0.25e  0.25e >xy n> rot>st   $000000FF rgba>c v+
    -0.25e  0.25e >xy n> rot>st   $000000FF rgba>c v+
    v>  drop  0 i, 1 i, 2 i, 0 i, 2 i, 3 i,
    GL_TRIANGLES draw-elements ;

Variable scan-buf1
Variable red-buf
Variable green-buf
Variable blue-buf

$80 Value color-level#

: scan-w ( -- n )  dpy-w @ dpy-h @ min 2/ 2/ 2/ $-4 and ;
: buf-len ( -- n ) scan-w dup * 1- 2/ 1+ ;

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

: |min| ( a b -- ) over abs over abs < select ;

: search-corner { mask -- x y } $8000 dup { x y }
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

2Variable p0
2Variable p1
2Variable p2
2Variable p3
2Variable px ( cross of the two lines )

: search-corners ( -- )
    4 search-corner p0 2! \ top left
    5 search-corner p1 2! \ top right
    6 search-corner p2 2! \ bottom left
    7 search-corner p3 2! \ bottom right
;

: compute-xpoint ( -- rx ry )
    p0 2@ s>f s>f fswap { f: x0 f: y0 }
    p2 2@ s>f s>f fswap { f: x1 f: y1 }
    p1 2@ s>f s>f fswap { f: x2 f: y2 }
    p3 2@ s>f s>f fswap { f: x3 f: y3 }
    x0 y1 f* y0 x1 f* f- { f: dxy01 }
    x2 y3 f* y2 x3 f* f- { f: dxy23 }
    x0 x1 f- y2 y3 f- f* y0 y1 f- x2 x3 f- f* f- { f: det1 }
    dxy01 x2 x3 f- f* dxy23 x0 x1 f- f* f- det1 f/ { f: x }
    dxy01 y2 y3 f- f* dxy23 y0 y1 f- f* f- det1 f/ { f: y }
    x f>s y f>s px 2!  x y ;

: scan-grab ( -- )
    dpy-w @ 2/ dpy-h @ 2/  scan-w >r
    r@ - swap r@ - swap r@ 2* dup
    r> 2* dup * sfloats scan-buf1 $!len
    GL_RGBA GL_UNSIGNED_BYTE scan-buf1 $@ drop glReadPixels ;

: .xpoint ( x y -- )
    p0 2@ swap . . space
    p1 2@ swap . . space
    p2 2@ swap . . space
    p3 2@ swap . . space
    fswap f. f. cr ;

: scan-once ( -- )
    camera-init scan-frame0 sync scan-grab search-corners
    compute-xpoint .xpoint ;
: scan-loop ( -- )
    1 level# +!  BEGIN  scan-once >looper level# @ 0= UNTIL ;
: scan-start ( -- )  hidekb
    c-open-back to camera
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