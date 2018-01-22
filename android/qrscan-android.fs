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

: tex-frame ( -- )
    program init
    unit-matrix MVPMatrix set-matrix
    unit-matrix MVMatrix set-matrix
    scan-tex-raw linear-mipmap mipmap ;

Variable skip-frames
8 Value skip-frames#

: draw-cam ( -- )
    0>framebuffer screen-orientation draw-scan sync ;
: draw-raw ( -- )
    cam-w cam-h scan-fb-raw >framebuffer  1 draw-scan
    scan-grab-raw ;
: draw-scaled ( -- )
    tex-frame scan-w 2* dup scan-fb0 >framebuffer
    scan-tex-raw 0 draw-scan
    scan-grab0 ;
: scan-once ( -- )
    camera-init draw-cam draw-raw draw-scaled
    search-corners
    ?legit IF  scan-legit?
	skip-frames @ 0= and IF
	    msg( ." scanned ok" cr )
	    guessecc $10 + c@ scan-result
	ELSE  2drop  THEN
    THEN
    skip-frames @ 0> skip-frames +! ;
: scan-loop ( -- )  scanned-flags off \ start with empty flags
    1 level# +!  BEGIN  scan-once >looper level# @ 0= UNTIL ;
: scan-start ( -- )
    hidekb >changed  hidestatus >changed  screen+keep
    c-open-back to camera
    program 0= IF
	['] VertexShader ['] FragmentShader create-program to program
    THEN
    cam-prepare
    scan-fb-raw 0= IF  new-scantex-raw new-scantex0 new-scantex1  THEN
    skip-frames# skip-frames ! ;

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
