\ scan color QR codes on Android

\ Copyright © 2016-2018   Bernd Paysan

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
    debug: health(
    debug: msg(
    +db qr( \ turn it on )
    -db msg( \ turn it on )
[THEN]

[IFUNDEF] xtype
    : hex[ ]] [: [[ ; immediate
    : ]hex ]] ;] $10 base-execute [[ ; immediate
    : xtype ( addr u -- )  hex[
	bounds ?DO  I c@ 0 <# # # #> type  LOOP  ]hex ;
[THEN]

[IFUNDEF] taghash?
    : smove ( a-from u-from a-to u-to -- )
	rot 2dup u< IF
	    drop move -9 throw
	ELSE
	    nip move
	THEN ;
    : -skip ( addr u char -- ) >r
	BEGIN  1- dup  0>= WHILE  2dup + c@ r@ <>  UNTIL  THEN  1+ rdrop ;
    : throwcode ( addr u -- )  exception Create ,
	[: ( flag -- ) @ and throw ;] set-does>
	[: >body @ >r ]] IF [[ r> ]] literal throw THEN [[ ;] set-optimizer ;
    s" krypto mem request too big"   throwcode !!kr-size!!
    s" insufficiend randomness"      throwcode !!insuff-rnd!!
    s" unhealthy RNG state"          throwcode !!bad-rng!!
    s" unsaulted random number"      throwcode !!no-salt!!
    : .net2o-config/ ;
    : <default> default-color ;
    : <info>    info-color    ;
    : <err>     error-color   ;
    require mkdir.fs
    2 Constant ENOENT
    #-512 ENOENT - Constant no-file#
    : init-dir ( addr u mode -- flag ) \ net2o
	\G create a directory with access mode,
	\G return true if the dictionary is new, false if it already existed
	>r 2dup file-status nip no-file# = IF
	    r> mkdir-parents throw  true
	ELSE  2drop rdrop  false  THEN ;
    require kregion.fs
    require crypto-api.fs
    require 64bit.fs
    require keccak.fs
    require rng.fs
    32 Constant keysize \ our shared secred is only 32 bytes long
    keysize buffer: qr-key \ key used for QR challenge (can be only one)
    $10 buffer: sigdate
    $10 buffer: qrecc
    : >qr-key ( addr u -- ) qr-key keysize move-rep ;
    : rng>qr-key ( -- )  $8 rng$ >qr-key ;
    : date>qr-key ( -- )  sigdate $8 >qr-key ;
    : taghash-rest ( addr1 u1 addrchallenge u2 tag -- tag )  >r
	msg( ." chal=" 2dup xtype cr 2over dump )
	c:0key $8 umin qrecc $8 smove r@ qrecc $8 + c!
	qrecc $9 c:shorthash c:shorthash qrecc $8 + $8 c:hash@ r>
	msg( ." ecc= " qrecc $10 xtype space dup h. cr ) ;
    : >taghash ( addr u tag -- tag )
	qr-key $8 rot taghash-rest ;
    : taghash? ( addr u1 ecc u2 tag -- flag )
	>r 2tuck over $8 >qr-key
	r> taghash-rest drop 8 /string qrecc 8 + 8 str=
	qr( dup IF  ." ecc= " qrecc $10 xtype cr  THEN ) ;
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

: draw-scan ( direction xscale yscale -- ) \ net2o
    \G draw a scan rotated/tilted by scan matrix
    fover fnegate fover fnegate 0e { f: sx f: sy f: -sx f: -sy f: z }
    vi0 >v
     -sx  sy z >xyz n> rot>st   white# i>c v+
      sx  sy z >xyz n> rot>st   white# i>c v+
      sx -sy z >xyz n> rot>st   white# i>c v+
     -sx -sy z >xyz n> rot>st   white# i>c v+
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
    dup >r c@ r@ 1+ c@ r> 2 + c@ ;

0 Value rgb-xor

: rgb? ( addr -- rgbbit )
    rgb@
    blue-level#  ?? negate 2* swap
    green-level# ?? - 2* swap
    red-level#   ?? - rgb-xor xor ;

: extract-strip ( addr u step -- strip ) rgbas { step }
    0 -rot bounds U+DO
	2* 2* I rgb? 3 and +
    step +LOOP ;

$51 buffer: guessbuf
guessbuf $40 + Constant guessecc
guessecc $10 + Constant guesstag

scan-w 3 rshift Constant scan-step

0 Value strip+x
0 Value strip+y

: >strip ( index --- addr )
    2* 2* scan-w + strip+y + scan-w 2* *
    scan-w + strip+x + rgbas
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

: rgb?-min ( addr x y -- addr ) 2>r
    dup rgb? dup 4 and IF
	over rgba+ rgb? over = IF
	    over scan-w 3 lshift + rgb? over = IF
		over rgba+ scan-w 3 lshift + rgb? over = IF
		    3 and 2 xor
		    2* cells p0 + 2r> rot min²!
		    EXIT
		THEN
	    THEN
	THEN
    THEN
    drop 2rdrop ;

: search-corners ( -- )
    init-xy p0 !  p0 p0 cell+ 7 cells cmove \ fill all with the same contents
    scan-buf0 $@ drop
    scan-w 1- dup invert DO
	scan-w 1- dup invert DO
	    i j rgb?-min rgba+
	LOOP  rgba+
    LOOP  drop
    qr( msg( p0 2@ . . space p1 2@ . . space p2 2@ . . space p3 2@ . . cr ) )
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
tex: scan-tex-final

0 Value scan-fb-raw
0 Value scan-fb
0 Value scan-fb-final

: scan-grab-raw ( -- )
    cam-w cam-h scan-fb-raw >framebuffer scan-buf-raw scan-grab-cam ;
: scan-grab0 ( -- )
    scan-buf0 scan-grab-buf ;
: scan-grab1 ( -- )
    scan-buf1 scan-grab-buf ;

\ also soil

require unix/stb-image-write.fs

0 Value scan#

: save-png0 ( -- )
    [: ." scanimg0-" scan# 0 .r ." .png" ;] $tmp
    128 dup 4 scan-buf0 $@ drop 0 stbi_write_png ;
: save-png1 ( -- )
    [: ." scanimg1-" scan# 0 .r ." .png" ;] $tmp
    128 dup 4 scan-buf1 $@ drop 0 stbi_write_png ;
: save-png-raw ( -- )
    [: ." scanimgraw-" scan# 0 .r ." .png" ;] $tmp
    cam-w cam-h 4 scan-buf-raw $@ drop 0 stbi_write_png ;
: save-pngs ( -- )
    scan-grab-raw save-png-raw
    save-png0 save-png1
    1 +to scan#
    0>framebuffer ;

\ previous

: .xpoint ( x y -- )
    p0 2@ swap . . space
    p1 2@ swap . . space
    p2 2@ swap . . space
    p3 2@ swap . . space
    fswap f. f. cr ;

: new-scantex-raw ( -- )
    scan-tex-raw 0>clear
    cam-w cam-h GL_RGBA new-textbuffer to scan-fb-raw drop ;
: new-scantex ( -- )
    scan-tex 0>clear
    scan-w 2* dup GL_RGBA new-textbuffer to scan-fb drop ;
: new-scantex-final ( -- )
    scan-tex-final 0>clear
    scan-w 2* dup GL_RGBA new-textbuffer to scan-fb-final drop ;
: new-scantexes ( -- )
    scan-fb 0= IF
	new-scantex-raw new-scantex new-scantex-final 0>framebuffer
    THEN ;
: scale+rotate ( -- )
    p1 2@ p0 2@ p- p3 2@ p2 2@ p- p+ p2/
    s>f y-scansize f/ y-rots sf!  s>f x-scansize f/ x-scl sf!
    p0 2@ p2 2@ p- p1 2@ p3 2@ p- p+ p2/
    s>f y-scansize f/ y-scl sf!  s>f x-scansize f/ x-rots sf! ;
: pf+ ( fx fy fx' fy' -- fx+x' fy+y' )
    frot f+ f-rot f+ fswap ;
: perspective { f: x f: y -- x' y' }
    p0 2@ s>f y f- s>f x f-
    p1 2@ s>f y f- s>f x f- fnegate fswap pf+
    p2 2@ s>f y f- s>f x f- fnegate fswap fnegate fswap pf+
    p3 2@ s>f y f- s>f x f- fswap fnegate pf+
    f2/ f2/ fswap f2/ f2/  ;
: set-scan' ( -- )
    compute-xpoint ( .. x y )
\    fover fover .xpoint
    fover fover perspective f. f. cr
    scale+rotate
    y-offset f+ scan-w fm/ y-spos sf!
    x-offset f+ scan-w fm/ x-spos sf! ;

: scan-xy ( -- sx sy )
    1e cam-h cam-w over umin swap fm*/
    1e cam-w cam-h over umin      fm*/ ;

: scan-legit ( -- ) \ resize a legit QR code
    [IFDEF] distdebug
	dist0 off dist0-max off dist0-min on
	dist1 off dist1-max off dist1-min on
    [THEN]
    clear init-scan' set-scan' >scan-matrix
    scan-matrix MVPMatrix set-matrix
    scan-matrix MVMatrix  set-matrix
    scan-tex-raw linear-mipmap 0 scan-xy draw-scan scan-grab1 ;

: scan-legit? ( -- addr u flag )
    >guess
    >guessecc tag@ guesstag c!
    2dup guessecc $10 ecc-ok? ;

Create sat%s 1.0e sf, 2.0e sf, 1.5e sf, 3.0e sf, \ 1.5 and 3 not needed
does> ( n -- ) swap sfloats + sf@ ;

: tex-frame ( -- )
    program init-program load-colors 0.5e ColorMode! set-uniforms
    unit-matrix MVPMatrix set-matrix
    unit-matrix MVMatrix set-matrix ;
: draw-scaled ( i -- )
    3 and sat%s saturate% sf!
    Saturate 1 saturate% glUniform1fv
    tex-frame   scan-w 2* dup scan-fb >framebuffer
    scan-tex-raw linear-mipmap 0 scan-xy draw-scan
    scan-grab0  scan-w 2* dup scan-fb-final >framebuffer ;
: sat-reset ( sat -- )
    saturate% sf! Saturate 1 saturate% glUniform1fv ;

previous

[IFDEF] android
    require android/qrscan-android.fs
    also android
[ELSE]
    [IFDEF] linux
	require linux/qrscan-linux.fs
    [THEN]
[THEN]

: debug-scan-result ( addr u tag -- )
    >r dump
    r> ." tag: " h. cr
    ." ecc: " guessecc $10 xtype cr
    [IFDEF] distdebug
	." dist/min/max: "
	dist0 @ s>f [ 18 18 * ]L fm/ f>s . dist0-min ? dist0-max ? space
	dist1 @ s>f [ 18 18 * ]L fm/ f>s . dist1-min ? dist1-max ? cr
    [THEN] ;
[IFUNDEF] scan-result
    : scan-result ( addr u tag -- )
	." scan result: " h. ." sat: " saturate% sf@ f.
	x-scansize f. y-scansize f. strip+x . strip+y . cr
	bounds U+DO
	    I $10 xtype cr
	    $10 +LOOP ;
    0 Value scan-once?
[THEN]

#0. 2Value rminmax
#0. 2Value gminmax
#0. 2Value bminmax

: adapt-rgb ( -- )
    scan-buf0 $@ get-minmax-rgb to bminmax to gminmax to rminmax
    msg(
    ." rminmax: " rminmax h. h. cr
    ." gminmax: " gminmax h. h. cr
    ." bminmax: " bminmax h. h. cr
    ) ;
: white-bg-levels ( -- )
    bminmax over - 2/ 2/   + to blue-level#    \ blue level is 1/4 of total
    gminmax over - 2 5 */  + to green-level#   \ green at 40% of total
    rminmax over - 2/      + to red-level# ;   \ red at 50% of total
: black-bg-levels ( -- )
    bminmax over - 2/  + to blue-level#    \ blue level is 50% of total
    gminmax over - 2/  + to green-level#   \ green at 50% of total
    rminmax over - 2/  + to red-level# ;   \ red at 50% of total

0 value scanned?

: scan-it ( -- flag )
    ?legit IF
	scan-legit
	$10 0 DO
	    I 3 and    to strip+x
	    I 2 rshift to strip+y
	    scan-legit? IF
		guesstag c@
		msg( dup 2over rot debug-scan-result )
		scan-result dup to scanned?
		qr( ." took: " .time cr )
		qr( save-png1 1 +to scan# )
		UNLOOP  EXIT
	    ELSE
		2drop
		msg( ." not legit?" cr  save-png0 save-png1 1 +to scan# )
	    THEN
	LOOP
    ELSE
	msg( ." not ?legit" cr  save-png0 1 +to scan# )
    THEN  false ;

: scan-its ( -- )
    85 82 DO
	I s>f to x-scansize
	85 82 DO
	    I s>f to y-scansize
	    scan-it IF  true
		UNLOOP UNLOOP
		EXIT  THEN
	2 +LOOP
    2 +LOOP  false ;

: scan-once ( -- )
    saturate% sf@ { f: sat }
    draw-cam qr( !time ) 2 0 DO
	I draw-scaled adapt-rgb
	black-bg-levels 7 to rgb-xor search-corners scan-its  ?LEAVE
	white-bg-levels 0 to rgb-xor search-corners scan-its  ?LEAVE
    LOOP  sat sat-reset
    ekey? IF  ekey dup k-volup = swap bl = or  IF  save-pngs  THEN  THEN ;
: scan-loop ( -- )
    1 level# +!@ >r
    BEGIN  scan-once >looper level# @ r@ <= UNTIL
    rdrop ;

: scan-qr ( -- )
    scan-start  false to scanned?
    ['] scan-loop catch >r  level# off
    cam-end 0>framebuffer
    [IFDEF] saturate% 1.0e saturate% sf! [THEN]
    [IFDEF] showstatus showstatus [THEN]
    [IFDEF] terminal-program terminal-program terminal-init [THEN]
    scanned? IF
	." Scan completed" cr
    ELSE
	." Scan failed" cr
    THEN
    r> throw ;

previous

: run-scan-qr ( -- )
    ?get-me init-client
    ?nextarg IF  s" -many" str= 0=  ELSE  true  THEN  to scan-once?
    scan-qr ;

\\\
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
