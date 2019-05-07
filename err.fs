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
    [: ( flag -- ) @ and throw ;] set-does>
    [: >body @ >r ]] IF [[ r> ]] literal throw THEN [[ ;] set-optimizer ;

\ make sure we start at user defined exeption
\ net2o exception codes should be system-independent
-$1000 next-exception !@

s" gap in file handles"          throwcode !!gap!!
s" invalid file id"              throwcode !!fileid!!
s" could not send"               throwcode !!send!!
s" wrong packet size"            throwcode !!size!!
s" map size too big"             throwcode !!mapsize!!
s" unimplemented net2o function" throwcode !!function!!
s" invalid net2o function"       throwcode !!invalid!!
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
s" Invalid file name"            throwcode !!filename!!
s" invalid packet destination"   throwcode !!inv-dest!!
s" unknown key"                  throwcode !!unknown-key!!
s" wrong key"                    throwcode !!wrong-key!!
s" no key file"                  throwcode !!nokey!!
s" invalid Ed25519 key"          throwcode !!no-ed-key!!
s" wrong key size"               throwcode !!keysize!!
s" no signature appended"        throwcode !!no-sig!!
s" future signature"             throwcode !!new-sig!!
s" expired signature"            throwcode !!old-sig!!
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
s" net2o id not found"           throwcode !!host-notfound!!
s" too many revokes chained"     throwcode !!maxlookup!!
s" file class denied"            throwcode !!fileclass!!
s" no free termservers"          throwcode !!no-termserver!!
s" decryption failed"            throwcode !!no-decrypt!!
s" no data"                      throwcode !!no-data!!
s" invalid command order"        throwcode !!inv-order!!
s" nick not found"               throwcode !!no-nick!!
s" passphrases don't match"      throwcode !!passphrase-unmatch!!
s" data needs to be signed"      throwcode !!unsigned!!
s" data needs to be unsigned"    throwcode !!signed!!
s" invalid DHT key"              throwcode !!no-dht-key!!
s" dht exhausted - this should not happen" throwcode !!dht-full!!
s" 4cc wants 3 characters"       throwcode !!4cc!!
s" key setup already done"       throwcode !!doublekey!!
s" host or id not found"         throwcode !!no-address!!
s" hash mismatch"                throwcode !!wrong-hash!!
s" connection refused"           throwcode !!connect-perm!!
s" DHT permission denied"        throwcode !!dht-perm!!
s" MSG permission denied"        throwcode !!msg-perm!!
s" file read permission denied"  throwcode !!filerd-perm!!
s" file write permission denied" throwcode !!filewr-perm!!
s" file access by name denied"   throwcode !!filename-perm!!
s" file access by hash denied"   throwcode !!filehash-perm!!
s" socket access denied"         throwcode !!socket-perm!!
s" terminal access denied"       throwcode !!terminal-perm!!
s" termserver access denied"     throwcode !!termserver-perm!!
s" sync access denied"           throwcode !!sync-perm!!
s" patch size exceeds limit"     throwcode !!patch-limit!!
s" patch size wrong"             throwcode !!patch-size!!
s" insufficiend randomness"      throwcode !!insuff-rnd!!
s" no key opened"                throwcode !!no-key-open!!
s" unknwon protocol"             throwcode !!unknown-protocol!!
s" unsaulted random number"      throwcode !!no-salt!!
s" unhealthy RNG state"          throwcode !!bad-rng!!
s" repeated tmpkey"              throwcode !!repeated-tmpkey!!
s" Unknown group"                throwcode !!no-group!!
s" Challenge failed"             throwcode !!challenge!!
s" Stack should always be empty" throwcode !!depth!!
s" hashed object not read in"    throwcode !!dvcs-hash!!
s" Value has no unit"            throwcode !!no-unit!!
s" Token/Coin doesn't exist"     throwcode !!no-coin!!
s" not a wallet"                 throwcode !!wallet!!
s" no group name"                throwcode !!no-group-name!!
s" no base85 digit"              throwcode !!no-85-digit!!
s" Vault auth block error"       throwcode !!vault-auth!!
s" Deprecated command"           throwcode !!deprecated!!
s" Connection successful"        throwcode !!connected!!
s" Invalid index"                throwcode !!inv-index!!
s" hash not last pk's state"     throwcode !!squid-hash!!
s" Double transaction!"          throwcode !!double-transaction!!
s" Insufficient asset!"          throwcode !!insufficient-asset!!
s" Transaction not balanced!"    throwcode !!not-balanced!!
s" Sink already cleared!"        throwcode !!sink-cleared!!
s" Sink not cleared!"            throwcode !!not-sunk!!

next-exception !

: sig-enum>throw ( enum -- throwcode )
    [ ' !!inv-sig!! >body @ 1- ]L swap - ;
: !!sig!! ( n -- )
    ?dup-IF  sig-enum>throw throw  THEN ;
