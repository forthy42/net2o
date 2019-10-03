\ net2o key storage tests

\ Copyright © 2014   Bernd Paysan

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

require ../net2o.fs

+debug

"testkey.n2o" r/w create-file throw to key-sfd

"This is a Test" ">passphrase key>default
"test" 2dup key#anon +gen-keys .rsk
"anonymous" 2dup key#anon +gen-keys .rsk
"alice" 2dup key#user +gen-keys .rsk
"bob" 2dup key#user +gen-keys .rsk
"eve" 2dup key#user +gen-keys .rsk

read-keys
.keys

bye