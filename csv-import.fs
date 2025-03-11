\ csv importer (for messages)

\ Copyright © 2025   Bernd Paysan

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

require csv.fs

$[]Variable msg-fields[]

: import-messages ( addr u field line -- )
    1 = IF
	third third msg-group$ $! msg-fields[] $[]! 0 ?load-msgn
	msg-group-o .msg:+silent
    ELSE  msg-fields[] $[]@ 2dup msg-group$ $! >group chat-line  THEN ;

: csv-importer ( addr u -- )
    ['] import-messages read-csv
    msg-fields[] [: 2dup msg-group$ $! >group msg-group-o .msg:-silent ;]
    $[]map ;
