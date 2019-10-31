\ scan color QR codes on Android

\ Copyright © 2016   Bernd Paysan

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

also opengl also android also jni

: scan-start ( -- )
    hidekb hidestatus >changed  screen+keep
    "android.permission.CAMERA"
    "android.permission.RECORD_AUDIO"
    "android.permission.RECORD_VIDEO" 3 ask-permissions
    c-open-back to camera
    program 0= IF
	['] VertexShader ['] FragmentShader create-program to program
    THEN
    cam-prepare  new-scantexes ;

: draw-cam ( -- )
    0>framebuffer
    camera-init screen-orientation 1e 1e draw-scan sync
    cam-w cam-h scan-fb-raw >framebuffer
    1 1e 1e draw-scan
    scan-tex-raw linear-mipmap mipmap ;

previous previous previous

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
