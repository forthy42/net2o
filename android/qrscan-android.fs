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

also opengl also android

: visual-frame ( -- )
    oes-program init
    0e fdup x-pos sf! >y-pos
    unit-matrix MVPMatrix set-matrix
    unit-matrix MVMatrix set-matrix
    media-tex nearest-oes ;
: tex-frame ( -- )
    program init
    scan-tex-raw linaer-mipmap mipmap ;

Variable skip-frames
8 Value skip-frames#

: scan-once ( -- )
    camera-init
    cam-w cam-h scan-fb-raw >framebuffer
    visual-frame 0 draw-scan
    scan-w 2* dup scan-fb0 >framebuffer
    tex-frame 0 draw-scan
    scan-grab0 search-corners
    ?legit IF  scan-legit?
	skip-frames @ 0= and IF
	    0>framebuffer  visual-frame  screen-orientation draw-scan sync
	    msg( ." scanned ok" cr )
	    guessecc $10 + c@ scan-result
	ELSE  2drop  THEN
    THEN
    0>framebuffer skip-frames @ 0= IF visual-frame  THEN
    -sync skip-frames @ 0> skip-frames +! ;
: scan-loop ( -- )  scanned-flags off \ start with empty flags
    1 level# +!  BEGIN  scan-once >looper level# @ 0= UNTIL ;
: scan-start ( -- )
    hidekb >changed  hidestatus >changed  screen+keep
    c-open-back to camera
    scan-fb0 0= IF  new-scantex0 new-scantex1  THEN
    ['] VertexShader ['] FragmentShader create-program to program
    .01e 100e dpy-w @ dpy-h @ min s>f f2/ 100 fm* >ap
    cam-prepare  skip-frames# skip-frames ! ;

: scan-qr ( -- )
    scan-start  ['] scan-loop catch  level# off
    cam-end
    level# @ 0= IF
	terminal-program terminal-init
	unit-matrix MVPMatrix set-matrix
	unit-matrix MVMatrix set-matrix
	screen-keep showstatus
    THEN
    dup IF
	." Scan failed"
    ELSE
	." Scan completed"
    THEN  forth:cr
    throw ;

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
