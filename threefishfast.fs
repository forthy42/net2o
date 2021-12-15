\ threefish fast wrapper

\ Copyright © 2012-2015   Bernd Paysan

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

require rec-scope.fs
[IFDEF] android
    s" libthreefishfast.so" c-lib:open-path-lib drop
[THEN]

c-library threefishfast
    s" threefishfast" add-lib
\    s" threefish/.libs" add-libpath \ find library during build
    include threefishlib.fs
end-c-library
