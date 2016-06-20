\ net2o command line interface

\ Copyright (C) 2015   Bernd Paysan

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

require net2o.fs

Variable key-readin

: out-nicks ( -- )
    [: nick-key ?dup-IF  out-key  THEN ;] @arg-loop ;

: qr-me ( -- ) pkc keysize2 .keyqr ;
: qr-nicks ( -- )
    [: nick-key ?dup-IF  .ke-pk $@ .keyqr  THEN ;] @arg-loop ;

: +pk ( "name" -- )  pk' keysize umin key-list $+[]! ;

: args>keylist ( -- )
    [: nick-key ?dup-IF  >o ke-pk $@ o> keysize umin key-list $+[]!  THEN ;]
    @arg-loop ;

$20 value hash-size#

: hash-file ( addr u -- hash u' )
    c:0key slurp-file 2dup c:hash drop free throw pad c:key>
    pad hash-size# ;

: do-keyin ( addr u -- )
    key-readin $slurp-file  64#-1 key-read-offset 64!
    key-readin $@ do-key ;

: keys>search ( -- )
    search-key[] $[]off [: dup 5 mod IF
	    ." keys in multiples of 5 base85 characters, please, ignoring '"
	    type ." '" cr
	ELSE  base85>$ search-key[] $+[]!  THEN ;] arg-loop ;

: nicks>search ( -- )
    search-key[] $[]off
    [: nick>pk dup 0= !!no-nick!! search-key[] $+[]! ;] @arg-loop ;

: handle-chat ( -- )
    chat-connects ?wait-chat do-chat ;

\ commands for the command line user interface

Vocabulary n2o

: set-net2o-cmds ( -- )
    ['] n2o >body 1 set-order
    ['] rec:word 1 set-recognizers ;

: do-net2o-cmds ( xt -- )
    get-order n>r get-recognizers n>r
    set-net2o-cmds catch
    nr> set-recognizers nr> set-order throw ;

: n2o-cmds ( -- )
    init-client word-args ['] quit do-net2o-cmds ;

: .usage ( addr u -- addr u )
    source 7 /string type cr ;

scope{ n2o

: help ( -- )
    \U help [cmd1 .. cmdn]
    \G help: print commands or details about specified command
    ?nextarg IF
	BEGIN
	    2dup [: ."     \U " type ;] $tmp ['] .usage search-help
	    [: ."     \G " type ':' emit ;] $tmp ['] .cmd search-help
	?nextarg 0= UNTIL
    ELSE
	s"     \U " ['] .usage search-help
    THEN ;

}scope

: next-cmd ( -- )
    ?nextarg 0= IF  n2o:help  EXIT  THEN
    ['] n2o >body search-wordlist
    IF  execute  ELSE  ['] 2drop arg-loop n2o:help  THEN ;

scope{ n2o

: keyin ( -- )
    \U keyin|inkey file1 .. filen
    \G keyin: read a .n2o key file in
    get-me  import#manual import-type !  key>default
    BEGIN  ?nextarg WHILE  do-keyin  REPEAT  save-pubkeys ;
: keyout ( -- )
    \U keyout|outkey [@user1 .. @usern]
    \G keyout: output pubkey of your identity
    \G keyout: optional: output pubkeys of other users
    get-me ?peekarg IF  2drop out-nicks  ELSE  out-me  THEN ;
: keygen ( -- )
    \U keygen|genkey nick
    \G keygen: generate a new keypair
    ?nextarg 0= IF  get-nick  THEN
    new-key .keys ?rsk ;
: keylist ( -- )
    \U keylist|listkey
    \G keylist: list all known keys
    get-me list-keys ;

: keyqr ( -- )
    \U keyqr|qrkey [@user1 .. @usern]
    \G keyqr: print qr of own key (default) or selected user's qr
    get-me ?peekarg IF  2drop qr-nicks  ELSE  qr-me  THEN ;

: keysearch ( -- )
    \U keysearch|searchkey 85string1 .. 85stringn
    \G keysearch: search for keys prefixed with base85 strings,
    \G keysearch: and import them into the key chain
    get-me  init-client
    keys>search search-keys insert-keys save-pubkeys
    keylist ;

: perm ( -- )
    \U perm @user1 .. @usern permissions
    \G perm: Change or set permissions. permission starts with
    \G perm: + for adding permissions
    \G perm: - for taking away permissions
    \G perm: = sets defaults, add or subtract permissions afterwards
    \G perm: no prefix for setting permissions exactly
    \G perm: c onnect - allows connections
    \G perm: b locked - denies connections
    \G perm: d ht     - DHT read/write access
    \G perm: m sg     - can send messages
    \G perm: r ead    - allow to read files
    \G perm: w rite   - allow to write files
    \G perm: n ame    - allow to access file by name
    \G perm: h ash    - allow to access file by hash
    \G perm: s ync    - allow to synchronize
    get-me
    BEGIN  chat-keys $[]off  @nicks>chat ?nextarg WHILE  >perm
	    chat-keys [: key| key-exist? ?dup-IF .apply-permission THEN ;]
	    $[]map 2drop  REPEAT
    save-keys ;

: passwd ( -- )
    \U passwd [pw-level]
    \G passwd: Change the password for the selected secret key
    get-me +newphrase key>default
    pkc keysize key-exist? ?dup-IF  >o >storekey @ ke-storekey !
	?nextarg IF  >number drop 0 max pw-maxlevel# min  ke-pwlevel !  THEN  o>
	save-keys
    THEN ;

: nick ( -- )
    \U nick newnick
    \G nick: Change your nick to <newnick>
    get-me ?nextarg IF  pkc keysize key-exist? ?dup-IF
	    >o ke-nick $! key-sign o> save-keys
	ELSE  2drop  THEN  THEN ;

: pet ( -- )
    \U pet nick1|pet1 petnew1 .. nickn|petn petnewn
    \G pet: Add a new petname to existing <nick> or <pet>
    get-me
    [: nick-key dup 0= IF  drop EXIT  THEN
      >o ?nextarg IF  ke-pets $+[]! pet!  THEN  o> ;] arg-loop
    save-keys keylist ;

: pet- ( -- )
    \U pet- pet1 .. petn
    \G pet-: remove pet name
    get-me
    [: 2dup nick-key dup 0= IF  drop 2drop  EXIT  THEN
      >o ke-pets [: 2over str= 0= ;] $[]filter 2drop o o>
      last# cell+ del$cell
      last# cell+ $@len 0= IF  last# bucket-off  THEN ;] arg-loop
    save-keys keylist ;

synonym inkey keyin
synonym outkey keyout
synonym genkey keygen
synonym listkey keylist
synonym qrkey keyqr
synonym searchkey keysearch

\ encryption subcommands

: -threefish ( -- )
    \U -threefish <next-cmd>
    \G -threefish: use threefish encryption for vaults
    enc-threefish next-cmd ;
: -keccak ( -- )
    \U -keccak <next-cmd>
    \G -keccak: use keccak encryption for vaults
    enc-keccak next-cmd ;

: enc ( -- )
    \U enc @user1 .. @usern file1 .. filen
    \G enc: encrypt files for the listed users
    get-me args>keylist
    [: key-list encrypt-file ;] arg-loop ;
: dec ( -- )
    \U dec file1 .. filen
    \G dec: decrypt files
    get-me [: decrypt-file ;] arg-loop ;
: cat ( -- )
    \U cat file1 .. filen
    \G cat: cat encrypted files to stdout
    vault>out dec ;

\ hash+signature

: -256 ( -- )
    \U -256 <next-cmd>
    \G -256: set hash output to 256 bits (default)
    $20 to hash-size# next-cmd ;

: -512 ( -- )
    \U -512 <next-cmd>
    \G -512: set hash output to 512 bits
    $40 to hash-size# next-cmd ;

: hash ( -- )
    \U hash file1 .. filen
    \G hash: hash the files and print it base85
    \G hash: use -256 or -512 to select hash size
    \G hash: use -threefish or -keccak to select hash algorithm
    enc-mode @ 8 rshift $FF and >crypt
    [: 2dup hash-file .85info space type cr ;] arg-loop 0 >crypt ;

: sign ( -- )
    \U sign file1 .. filen
    \G sign: create detached .s2o signatures for all files
    get-me now>never
    [: 2dup hash-file 2drop
      [: type ." .s2o" ;] $tmp w/o create-file throw >r
      [: .pk .sig ;] $tmp r@ write-file r> close-file throw throw ;]
    arg-loop ;

: verify ( -- )
    \U verify file1 .. filen
    \G verify: check integrity of files vs. detached signature
    get-me
    [: 2dup hash-file 2drop 2dup type
      [: type ." .s2o" ;] $tmp slurp-file
      over date-sig? dup >r  err-color info-color r> select  attr! .check
      space over keysize .key-id <default>
      drop free throw cr ;]
    arg-loop ;

\ select config file

: -config ( -- )
    \U -config <filename> <next-cmd>
    \G -config: Set filename for config file
    ?nextarg 0= ?EXIT  config-file$ $! next-cmd ;

\ server mode

: -lax ( -- )
    \U -lax <next-cmd>
    \G -lax: open for all keys
    perm%default to perm%unknown  next-cmd ;

: server ( -- )
    \U server
    get-me init-server announce-me server-loop ;

: rootserver ( -- )
    \U rootserver
    perm%default to perm%unknown
    ['] no0key( >body on \ rootserver has no 0key
    get-me init-server addme-owndht server-loop ;

\ dht commands

: announce ( -- )
    \U announce
    \G announce: Only announce ID
    get-me init-client announce-me ;

: lookup ( -- )
    \U lookup
    \G lookup: query DHT for addresses
    get-me init-client nicks>search search-addrs
    search-key[] [: 2dup .simple-id ." :" cr
      >d#id >o dht-host [: .host cr ;] $[]map o> ;] $[]map ;

: ping ( -- )
    \U ping
    \G ping: query DHT and send a ping to the observed addresses
    \G ping: is not ready yet
    get-me init-client nicks>search search-addrs pings[] $[]off
    search-key[] [: >d#id >o dht-host [: pings[] $+[]! ;] $[]map o> ;] $[]map
    pings[] [: send-ping ;] $[]map  receive-pings ;

\ chat mode

: -root ( -- )
    \U -root <address[:port]> <next-cmd>
    ?nextarg 0= ?EXIT ':' $split dup IF  s>number drop to net2o-port
    ELSE  2drop  THEN  net2o-host $!  dhtroot-off
    next-cmd ;

: -rootnick ( -- )
    \U -rootnick <nick> <next-cmd>
    ?nextarg 0= ?EXIT  dhtnick $!  dhtroot-off  next-cmd ;

: -port ( -- )
    \U -port <port#> <next-cmd>
    \G -port: sets port to a fixed number for reachability from outside,
    \G -port: allows to define port forwarding rules in the firewall
    \G -port: only for client; server-side port is different
    ?nextarg 0= ?EXIT  s>number drop to net2o-client-port
    next-cmd ;

: -otr ( --- )
    \U -otr <next-cmd>
    \G -otr: Turn off-the-records mode on, so chats are not logged
    otr-mode on next-cmd ;

: chat ( -- )
    \U chat @user1|group1@user1|group1 ... @usern|groupn@usern|groupn
    \G chat: @user:      to chat privately with a user
    \G chat: group@user: to chat with the chatgroup managed by user
    \G chat: group:      to start a group chat (peers may connect)
    \G chat: chat with an user, a group managed by an user, or start
    \G chat: your own group
    announce nicks>chat handle-chat ;

: chatlog ( -- )
    \U chatlog @user1|group1 .. @usern|groupn 
    \G chatlog: dump chat log
    get-me init-client
    BEGIN  ?nextarg  WHILE  ." === Chat log for " 2dup type
	    over c@ '@' = IF  1 /string nick>pk key| ."  key: " 2dup 85type  THEN
	    ."  ===" cr msg-group$ $!
	    msg-group$ $@ [ -1 1 rshift cell/ ]l load-msgn REPEAT ;

: invite ( -- )
    \U invite @user
    \G invite: send or accept an invitation to another user
    announce @nicks>chat 
    chat-keys [: 2dup n2o:pklookup send-invitation
      n2o:dispose-context ;] $[]map
    ." invitation" chat-keys $[]# 1 > IF ." s" THEN  ."  send" forth:cr ; 

\ script mode

: cmd ( -- )
    \U cmd
    \G cmd: Offer a net2o command line for client stuff
    get-me ." net2o interactive shell, type 'bye' to quit"
    n2o-cmds ;

: script ( -- )
    \U script file
    \G script: read a file in script mode
    ?nextarg 0= IF  help  EXIT  THEN
    get-me init-client word-args ['] included do-net2o-cmds ;

: debug ( -- )
    \U debug [+|-<switch>]
    \G debug: set or clear debugging switches
    also forth
    BEGIN  ?peekarg  WHILE  +-?  WHILE  ?nextarg drop set-debug  REPEAT  THEN
    previous ;

\ file copy

: get ( -- )
    \U get @user file1 .. filen
    \G get: get files into current directory
    get-me init-client
    ?@nextarg IF
	$A $E nick-connect ." connected" cr !time
	net2o-code expect-reply
	$10 blocksize! $A blockalign!
	[: 2dup basename n2o:copy ;] arg-loop
	n2o:done end-code| n2o:close-all
	c:disconnect  THEN ;

\ diff and patch

: diff ( -- )
    \U diff file1 file2 patchfile
    \G diff: compute diff between two files, and write patch-file
    ?nextarg IF  ?nextarg IF  bdelta
	    ?nextarg IF  spit-file  b$off  EXIT  THEN THEN  THEN
    ." diff needs three arguments!" cr b$off ;

: patch ( -- )
    \U patch file1 patchfile file2
    \G patch: take a patchfile for file1 and produce file2
    ?nextarg IF  ?nextarg IF  bpatch
	    ?nextarg IF  spit-file b$off  EXIT  THEN THEN THEN
    ." patch needs three arguments!" cr b$off ;

\ !!stubs!!

: init ( -- )
    \U init [name [branchname]]
    \G init: Setup a dvcs project in the current folder
    \G init: The default branch name is "master"
    \G init: The default project name is the directory it resides in
    ".n2o" $1FF init-dir drop
    ".n2o/files" touch \ create empty file
    ?nextarg 0= IF  pad $200 get-dir 2dup '/' -scan nip /string THEN
    project:project$ $!
    ?nextarg 0= IF  "master"  THEN  project:branch$ $!
    ".n2o/config" ['] project >body write-config ;

: add ( -- )
    \U add file1 .. filen
    \G add: add files to the dvcs project in the current folder
    ".n2o/newfiles" args>file ;

: ci ( -- )
    \U ci "message"
    \G ci: check added and modified files into the dvcs project
    ?nextarg 0= IF "untitled checkin" THEN  dvcs-ci ;

: co ( -- )
    \U co revision|@branch|revision@branch
    \G co: check out a specific revision
;

: fork ( -- )
    \U fork branch
    \G fork: create a branch
;

\ others

: bye ( -- )
    \U bye
    \G bye: quit command mode and terminate program
    subme bye ;

: -bw ( -- )
    \U -bw <cmd>
    \G -bw: disable color codes
    ['] drop is attr!  next-cmd ;

: -backtrace ( -- )
    \U -backtrace <cmd>
    \G -backtrace: Provide full error reporting ;
    [ what's DoError ]l is DoError next-cmd ;

: version ( -- )
    \U version
    \G version: print version string
    ." n2o version " net2o-version type cr ;

}scope

\ user friendly, but way less informative doerror

debugging-method 0= [IF]
    :noname [: <err> .error-string <default> cr ;] do-debug ; is DoError
[THEN]

\ allow issuing commands during chat

scope{ /chat

: n2o [: word-args ['] evaluate do-net2o-cmds ;] catch
    ?dup-IF  <err> ." error: " error$ type cr <default>  THEN ;

}scope
