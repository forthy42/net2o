\ GUI dark mode style

\ Copyright © 2019   Bernd Paysan

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

color,#
1 new-theme
dark-gui
$FFFFFFFF re-text-color blackish
$FFFFBBFF re-text-color dark-blue
$000000FF re-text-color whitish
$40C0FFFF $000000FF $000000FF $FFFFFFFF re-text-emoji-fade-color toggle-color
$FF0040FF re-text-color pw-num-col#
$cc6600FF re-text-color pw-text-col#
$FFFFFFFF re-text-color show-sign-color#
$550000FF $005500FF re-fade-color pw-bg-col#
$88FF00FF re-color dark-blue#
$00FF0020 re-color chbs-col#
$000020BF re-color login-bg-col#
$FF000000 $FF0000FF re-fade-color pw-err-col#
$444444FF re-color chat-col#
$224444FF re-color chat-bg-col#
$113333FF re-color chat-hist-col#
$222222FF re-color posting-bg-col#
$FFFFBBFF re-text-color link-blue
$88FF88FF re-text-color re-green
$FF8888FF re-text-color obj-red
$444444FF re-color edit-bg
$202020C0 re-color log-bg
$408040FF re-color send-color
$333333FF re-color users-color#
$000000CC re-color album-bg-col#
$88FF88FF re-color my-signal
$CCFFCCFF re-color other-signal
$CC00CCFF re-color my-signal-otr
$880088FF re-color other-signal-otr

8 0 [DO]
    imports#rgb-bg [I] sfloats + sf@ floor f>s to color,#
    $222222FF new-color, fdrop
[LOOP]
$FF0000FF
$FF6600FF
$FF8844FF
$EECC55FF
$CCEE55FF
$55DD55FF
$BBDD66FF
$33EE33FF
8 0 [DO]
    imports#rgb-fg [I] sfloats + sf@ floor f>s to color,#
    text-color, fdrop
[LOOP]
dark-gui
to color,#
