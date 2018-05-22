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
[IFDEF] android
    require minos2/android-recorder.fs
[ELSE]
    [IFUNDEF] cam-w
	$100 value cam-w
	$100 value cam-h
    [THEN]
[THEN]

\ replace some tools available under net2o

[IFUNDEF] qr(
    debug: qr(
    +db qr( \ turn it on )
[THEN]

[IFUNDEF] xtype
    : hex[ ]] [: [[ ; immediate
    : ]hex ]] ;] $10 base-execute [[ ; immediate
    : xtype ( addr u -- )  hex[
	bounds ?DO  I c@ 0 <# # # #> type  LOOP  ]hex ;
[THEN]

[IFUNDEF] taghash?
    : taghash? ( addrkey u1 addrecc u2 tag -- flag )
	drop 2drop 2drop true ;
[THEN]

\ scan matrix manipulation

Create scan-matrix
1e sf, 0e sf, 0e sf, 0e sf,
0e sf, 1e sf, 0e sf, 0e sf,
0e sf, 0e sf, 1e sf, 0e sf,
0e sf, 0e sf, 0e sf, 1e sf,

scan-matrix 11 sfloats + Constant 3d-enabler

32 sfloats buffer: scan-inverse
32 sfloats buffer: inverse-default
inverse-default 32 sfloats bounds
[?DO] 1e fdup [I] sf! [I] 4 sfloats + sf! 9 sfloats [+LOOP]

83e FValue x-scansize
83e FValue y-scansize

0e FValue y-offset
0e FValue x-offset

\ matrix inversion

' dfloats alias 8*
: .mat { mat -- }
    4 0 DO  cr
	8 0 DO
	    mat J 8* I + sfloats + sf@ f.
	LOOP
    LOOP ;
: init-scan' ( -- )
    inverse-default scan-inverse [ 32 sfloats ]L move ;
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

scan-inverse  0 sfloats + Constant x-scl
scan-inverse  1 sfloats + Constant y-rots
scan-inverse  8 sfloats + Constant x-rots
scan-inverse  9 sfloats + Constant y-scl
scan-inverse 24 sfloats + Constant x-spos
scan-inverse 25 sfloats + Constant y-spos

: >scan-matrix ( -- )
    scan-inverse matrix-invert4
    scan-matrix [ scan-inverse 4 sfloats + ]L [ 32 sfloats ]L bounds ?DO
	I over [ 4 sfloats ]L move  [ 4 sfloats ]L +
    [ 8 sfloats ]L +LOOP  drop
    -1e 3d-enabler sf! ;

\ scan constants

$40 Value scan-w
scan-w 2/ dup * 1- 2/ 2/ 1+ Constant buf-len
scan-w 2* Value scan-right
scan-w 2* dup 3 rshift + negate Value scan-left

also opengl

: draw-scan ( direction xscale yscale -- )
    \G draw a scan rotated/tilted by scan matrix
    fover fnegate fover fnegate 0e { f: sx f: sy f: -sx f: -sy f: z }
    vi0 >v
     -sx  sy z >xyz n> rot>st   $FFFFFFFF rgba>c v+
      sx  sy z >xyz n> rot>st   $FFFFFFFF rgba>c v+
      sx -sy z >xyz n> rot>st   $FFFFFFFF rgba>c v+
     -sx -sy z >xyz n> rot>st   $FFFFFFFF rgba>c v+
    v> drop 0 i, 1 i, 2 i, 0 i, 2 i, 3 i,
    GL_TRIANGLES draw-elements ;

Variable scan-buf-raw
Variable scan-buf0
Variable scan-buf1
Variable red-buf
Variable green-buf
Variable blue-buf

$28 Value blue-level#
$70 Value green-level#
$70 Value red-level#

' sfloats alias rgbas ( one rgba is the size of an sfloat )
' sfloat+ alias rgba+

[IFDEF] distdebug
    3 cells buffer: dist0
    dist0 cell+ Constant dist0-max
    dist0-max cell+ Constant dist0-min
    3 cells buffer: dist1
    dist1 cell+ Constant dist1-max
    dist1-max cell+ Constant dist1-min
    
    : ?? ( value level -- flag )
	- dup dup 0< IF  negate dist0  ELSE  dist1  THEN
	2dup +! cell+
	2dup @ umax over ! cell+
	tuck @ umin swap !
	0< ;
[ELSE]
    ' < alias ??
[THEN]

: rgb@ ( addr -- r g b )
    >r r@ c@ r@ 1+ c@ r> 2 + c@ ;

: extract-strip ( addr u step -- strip ) rgbas { step }
    0 -rot bounds U+DO
	I rgb@ drop 2>r
	2* r> green-level# ?? -
	2* r> red-level#   ?? -
    step +LOOP ;

$51 buffer: guessbuf
guessbuf $40 + Constant guessecc
guessecc $10 + Constant guesstag

scan-w 3 rshift Constant scan-step

: >strip ( index --- addr )
    2* 2* scan-w + scan-w 2* * scan-w + rgbas
    scan-buf1 $@ rot safe/string drop ;
: >strip32 ( addr -- addr' u step )
    scan-right - $100 4 ;
: >guess ( -- addr u )
    guessbuf $40 2dup bounds -8 -rot U+DO
	dup >strip >strip32
	extract-strip I be-l!
	1+
    4 +LOOP  drop ;
: ecc-hor@ ( off -- l )
    >strip >strip32 extract-strip ;
: ecc-ver@ ( offset -- ul )
    #-8 >strip + scan-w dup 3 lshift * scan-w 3 lshift extract-strip ;
: tag@ ( -- tag )
    #-9 >strip scan-left + $130 [ #17 4 * ]L extract-strip 4 lshift
    #08 >strip scan-left + $130 [ #17 4 * ]L extract-strip or ;

: >guessecc ( -- )
    #-9        ecc-hor@ guessecc      be-l!
    #08        ecc-hor@ guessecc  4 + be-l!
    scan-left  ecc-ver@ guessecc  8 + be-l!
    scan-right ecc-ver@ guessecc $C + be-l! ;

: ecc-ok? ( addrkey u1 addrecc u2 -- flag )
    2dup + c@ taghash? ;

$8000 Constant init-xy

: get-minmax-rgb ( addr u -- minr maxr ming maxg minb maxb )
    $FFF -$FFF 2dup 2dup { minr maxr ming maxg minb maxb }
    bounds ?DO
	I rgb@
	dup maxb max to maxb  minb min to minb
	dup maxg max to maxg  ming min to ming
	dup maxr max to maxr  minr min to minr
    4 +LOOP minr maxr ming maxg minb maxb ;

#10 Cells buffer: p0
p0 2 cells + Constant p1
p1 2 cells + Constant p2
p2 2 cells + Constant p3
p3 2 cells + Constant px

: min²! ( x y addr -- ) >r
    over dup * over dup * +
    r@ 2@ dup * swap dup * + u< IF
	r> 2!  EXIT
    THEN  2drop rdrop ;

: search-corners ( -- )
    init-xy p0 !  p0 p0 cell+ 7 cells cmove \ fill all with the same contents
    scan-buf0 $@ drop
    scan-w dup negate DO
	scan-w dup negate DO
	    dup rgb@ blue-level#  < IF
		green-level#     < 2*
		swap red-level#  < - 3 and 2 xor
		2* cells p0 + I J rot min²!
	    ELSE
		2drop
	    THEN
	    rgba+
	LOOP
    LOOP  drop ;

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
    x0 x1 f- x2 x3 f- { f: dx01 f: dx23 }
    y0 y1 f- y2 y3 f- { f: dy01 f: dy23 }
    dx01 dy23 f* dy01 dx23 f* f- 1/f { f: /det1 }
    dxy01 dx23 f* dxy23 dx01 f* f- /det1 f* \ x
    dxy01 dy23 f* dxy23 dy01 f* f- /det1 f* \ y
    fover f>s fdup f>s px 2! ;

: p+ ( x1 y1 x2 y2 -- x1+x2 y1+y2 )
    rot + >r + r> ;
: p2/ ( x1 y1 -- x2 y2 )
    2/ swap 2/ swap ;
: p- ( x1 y1 x2 y2 -- x1-x2 y1-y2 )
    rot swap - >r - r> ;

: scan-grab ( w h addr -- )
    >r  0 0 2swap
    2dup * rgbas r@ $!len
    GL_RGBA GL_UNSIGNED_BYTE r> $@ drop glReadPixels ;
: scan-grab-buf ( addr -- )
    scan-w 2* dup rot scan-grab ;
: scan-grab-cam ( addr -- )
    cam-w cam-h rot scan-grab ;

tex: scan-tex-raw
tex: scan-tex

0 Value scan-fb-raw
0 Value scan-fb

: scan-grab-raw ( -- )
    cam-w cam-h scan-fb-raw >framebuffer scan-buf-raw scan-grab-cam ;
: scan-grab0 ( -- )
    scan-buf0 scan-grab-buf ;
: scan-grab1 ( -- )
    scan-buf1 scan-grab-buf ;

also soil

0 Value scan#

: save-png0 ( -- )
    [: ." scanimg0-" scan# 0 .r ." .png" ;] $tmp
    SOIL_SAVE_TYPE_PNG 128 dup 4 scan-buf0 $@ drop SOIL_save_image ;
: save-png1 ( -- )
    [: ." scanimg1-" scan# 0 .r ." .png" ;] $tmp
    SOIL_SAVE_TYPE_PNG 128 dup 4 scan-buf1 $@ drop SOIL_save_image ;
: save-png-raw ( -- )
    [: ." scanimgraw-" scan# 0 .r ." .png" ;] $tmp
    SOIL_SAVE_TYPE_PNG cam-w cam-h 4 scan-buf-raw $@ drop SOIL_save_image ;
: save-pngs ( -- )
    scan-grab-raw save-png-raw
    save-png0 save-png1
    1 +to scan#
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
: new-scantex ( -- )
    scan-tex 0>clear
    scan-w 2* dup GL_RGBA new-textbuffer to scan-fb ;
: new-scantexes ( -- )
    scan-fb 0= IF
	new-scantex-raw new-scantex 0>framebuffer
    THEN ;
: scale+rotate ( -- )
    p1 2@ p0 2@ p- p3 2@ p2 2@ p- p+ p2/
    s>f y-scansize f/ y-rots sf!  s>f x-scansize f/ x-scl sf!
    p0 2@ p2 2@ p- p1 2@ p3 2@ p- p+ p2/
    s>f y-scansize f/ y-scl sf!  s>f x-scansize f/ x-rots sf! ;
: set-scan' ( -- )
    compute-xpoint ( .. x y )
    scale+rotate
    y-offset f+ scan-w fm/ y-spos sf!
    x-offset f+ scan-w fm/ x-spos sf! ;

: scan-xy ( -- sx sy )
    1e cam-h cam-w over umin swap fm*/
    1e cam-w cam-h over umin      fm*/ ;

: scan-legit ( -- ) \ resize a legit QR code
    clear init-scan' set-scan' >scan-matrix
    scan-matrix MVPMatrix set-matrix
    scan-matrix MVMatrix  set-matrix
    scan-tex-raw linear-mipmap 0 scan-xy draw-scan scan-grab1 ;

: scan-legit? ( -- addr u flag )
    [IFDEF] distdebug
	dist0 off dist0-max off dist0-min on
	dist1 off dist1-max off dist1-min on
    [THEN]
    scan-legit >guess
    >guessecc tag@ guesstag c!
    2dup guessecc $10 ecc-ok? ;

: tex-frame ( -- )
    program init-program set-uniforms
    unit-matrix MVPMatrix set-matrix
    unit-matrix MVMatrix set-matrix ;
: draw-scaled ( -- )
    tex-frame scan-w 2* dup scan-fb >framebuffer
    scan-tex-raw linear-mipmap 0 scan-xy draw-scan
    scan-grab0 ;

previous

[IFDEF] android
    require android/qrscan-android.fs
    also android
[ELSE]
    [IFDEF] linux
	require linux/qrscan-linux.fs
    [THEN]
[THEN]

[IFUNDEF] scan-result
    : scan-result ( addr u tag -- )
	qr( >r
	bounds ?DO  ." qr : " I $10 xtype cr $10 +LOOP
	r> ." tag: " dup hex. cr
	." ecc: " guessecc $10 xtype cr
	[IFDEF] distdebug
	    ." dist/min/max: "
	    dist0 @ s>f [ 18 18 * ]L fm/ f>s . dist0-min ? dist0-max ? space
	    dist1 @ s>f [ 18 18 * ]L fm/ f>s . dist1-min ? dist1-max ? cr
	[THEN]
	) ;
[THEN]

: adapt-rgb ( -- )
    scan-buf0 $@ get-minmax-rgb
    over - 2/ 2/   + to blue-level#   \ blue level is 1/4 of total
    over - 2 5 */  + to green-level#  \ green at 40% of total
    over - 2/      + to red-level# ;  \ red at 50% of total

: scan-once ( -- )
    draw-cam
    !time draw-scaled adapt-rgb
    search-corners
    ?legit IF  scan-legit? IF
	    guessecc $10 + c@ scan-result qr( ." took: " .time cr )
	    qr( save-png1 1 +to scan# )
	ELSE  2drop  THEN
    THEN
    ekey? IF  ekey dup k-volup = swap bl = or  IF  save-pngs  THEN  THEN ;
: scan-loop ( -- )
    1 level# +!@ >r  BEGIN  scan-once >looper level# @ r@ <= UNTIL
    rdrop ;

: scan-qr ( -- )
    [IFDEF] lastscan$  lastscan$ $free  [THEN]
    scan-start  ['] scan-loop catch  level# off
    cam-end 0>framebuffer
    [IFDEF] saturate% 1.0e saturate% sf! [THEN]
    [IFDEF] showstatus showstatus [THEN]
    [IFDEF] terminal-program terminal-program terminal-init [THEN]
    dup IF
	." Scan failed" cr
    ELSE
	." Scan completed" cr
    THEN
    throw ;

previous

:noname ( -- )
    ?get-me init-client
    ?nextarg IF  s" -many" str= 0=  ELSE  true  THEN  to scan-once?
    scan-qr ; is run-scan-qr

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
