\ Copyright (C) 2010-2014   Bernd Paysan

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

\ defined exceptions

: throwcode ( addr u -- )  exception Create ,
    [: >body @ >r ]] IF [[ r> ]] literal throw THEN [[ ;] set-compiler
  DOES> ( flag -- ) @ and throw ;

s" gap in file handles"          throwcode !!gap!!
s" invalid file id"              throwcode !!fileid!!
s" could not send"               throwcode !!send!!
s" wrong packet size"            throwcode !!size!!
s" map size too big"             throwcode !!mapsize!!
s" unimplemented net2o function" throwcode !!function!!
s" too many commands"            throwcode !!commands!!
s" float does not fit"           throwcode !!floatfit!!
s" string does not fit"          throwcode !!stringfit!!
s" cmd does not fit"             throwcode !!cmdfit!!
s" ivs must be 64 bytes"         throwcode !!ivs!!
s" net2o timed out"              throwcode !!timeout!!
s" maximum nesting reached"      throwcode !!maxnest!!
s" nesting stack empty"          throwcode !!minnest!!
s" invalid nest"                 throwcode !!nest!!
s" invalid tmpnest"              throwcode !!tmpnest!!
s" cookie recieved twice"        throwcode !!double-cookie!!
s" code destination is 0"        throwcode !!no-dest!!
s" no IP addr"                   throwcode !!no-addr!!
s" absolute path not allowed!"   throwcode !!abs-path!!
s" invalid packet destination"   throwcode !!inv-dest!!
s" wrong key size"               throwcode !!keysize!!
s" unknown key"                  throwcode !!unknown-key!!
s" wrong key"                    throwcode !!wrong-key!!
s" no key file"                  throwcode !!nokey!!
s" invalid Ed25519 key"          throwcode !!no-ed-key!!
s" invalid signature"            throwcode !!inv-sig!!
s" no temporary key"             throwcode !!no-tmpkey!!
s" generic stack empty"          throwcode !!stack-empty!!
s" String stack full"            throwcode !!string-full!!
s" String stack empty"           throwcode !!string-empty!!
s" Object stack full"            throwcode !!object-full!!
s" Object stack empty"           throwcode !!object-empty!!
s" Unknown crypto function"      throwcode !!unknown-crypt!!
s" Wrong revocation secret"      throwcode !!not-my-revsk!!
s" krypto mem request too big"   throwcode !!kr-size!!
s" secret storage size wrong"    throwcode !!sec-size!!
s" host not found"               throwcode !!host-notfound!!
s" too many revokes chained"     throwcode !!maxlookup!!
s" file class denied"            throwcode !!fileclass!!
s" no free termservers"          throwcode !!no-termserver!!
s" decryption failed"            throwcode !!no-decrypt!!
s" no data"                      throwcode !!no-data!!
s" invalid command order"        throwcode !!inv-order!!
s" nick not found"               throwcode !!no-nick!!
s" passphrases don't match"      throwcode !!passphrase-unmatch!!