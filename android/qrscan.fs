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

: scan-frame ( -- )
    camera-init matrix-init
    scan-matrix MVPMatrix set-matrix
    scan-matrix MVMatrix set-matrix
    screen-orientation v0 i0 >v
    -1e -1e >xy n> rot>st   $000000FF rgba>c v+
     1e -1e >xy n> rot>st   $000000FF rgba>c v+
     1e  1e >xy n> rot>st   $000000FF rgba>c v+
    -1e  1e >xy n> rot>st   $000000FF rgba>c v+
    v>  drop  0 i, 1 i, 2 i, 0 i, 2 i, 3 i,
    GL_TRIANGLES draw-elements ;
: scan-once ( -- )
    scan-frame sync ;
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