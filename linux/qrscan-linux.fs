\ scan color QR codes on Linux

\ Copyright © 2026   Bernd Paysan

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

require minos2/soil-texture.fs
require minos2/v4l2.fs

also v4l2

MJPG Value video-format
0 0 2Value video-wh
Variable scans

: redisplay-image ( addr u index -- ) >r
    scan-tex-raw
    case  video-format
	MJPG of  img>mem  endof
	YUYV of  video-wh yuyv>mem  endof
	abort" Unhandled format"
    endcase
    2dup to cam-h to cam-w >texture
    [: 1 scans +! ;] [ up@ ]L send-event
    r> bg-queue ;

: draw-cam ( -- )
    0>framebuffer
    unit-matrix MVPMatrix set-matrix
    unit-matrix MVMatrix set-matrix
    scan-tex-raw 1 1e 1e draw-scan sync
    scans @ BEGIN  stop dup scans @ <>  UNTIL  drop
    cam-w cam-h scan-fb-raw >framebuffer
    scan-tex-raw linear-mipmap mipmap ;
: cam-end ( -- ) ;
: scan-start ( -- )
    dpy 0= IF window-init THEN
    new-scantexes
    0 open-video .fmts
    [IFDEF] use-yuyv
	#800 #600 2dup to video-wh YUYV
    [ELSE]
	#1920 #1080 2dup to video-wh MJPG
    [THEN]
    dup to video-format set-format
    start-capture start-streaming
    ['] redisplay-image bg-capture ;

previous

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
