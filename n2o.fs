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

: qr-me ( -- ) pk@ qr:ownkey# .keyqr ;
: qr-nicks ( -- )
    [: nick-key ?dup-IF  >o ke-pk $@
	    qr:ownkey# qr:key# ke-sk sec@ nip select o>
	    .keyqr  THEN ;] @arg-loop ;

: args>keylist ( -- )
    [: nick-key ?dup-IF  >o ke-pk $@ o> keysize umin key-list $+[]!  THEN ;]
    @arg-loop ;

$20 value hash-size#

: hash-file ( addr u -- hash u' )
    c:0key slurp-file 2dup c:hash drop free throw pad c:key>
    pad hash-size# ;

: hash-file-blocks ( addr u -- )
    slurp-file over { start } bounds ?DO
	c:0key I dup $400 + I' umin over - c:hash
	pad c:key>
	I start - $400 / hex. pad hash-size# .85info cr
    $400 +LOOP
    start free throw ;

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

: set-net2o-cmds ( -- )
    ['] n2o >body 1 set-order
    ['] rec-word 1 set-recognizers ;

: do-net2o-cmds ( xt -- )
    get-order n>r get-recognizers n>r
    set-net2o-cmds catch
    nr> set-recognizers nr> set-order throw ;

: (n2o-quit) ( -- )
    \ exits only through THROW etc.
    BEGIN
	.status cr refill  WHILE
	    interpret prompt
    REPEAT  -56 throw ;

: n2o-quit ( -- )
    clear-tibstack
    BEGIN
	[compile] [  ['] (n2o-quit) catch dup #-56 <> and dup
    WHILE
	    <# \ reset hold area, or we may get another error
	    DoError
	    \ stack depths may be arbitrary still (or again), so clear them
	    clearstacks
	    clear-tibstack
    REPEAT
    drop clear-tibstack #-56 throw ;

: n2o-cmds ( -- )
    init-client word-args ['] n2o-quit ['] do-net2o-cmds catch
    dup #-56 = swap #-28 = or IF  drop subme bye  ELSE  throw  THEN ;

: .usage ( addr u -- addr u )
    source 7 /string type cr ;

: ?cr ( -- ) script? 0= IF  cr  THEN ;

scope{ n2o

: help ( -- )
    \U help [cmd1 .. cmdn]
    \G help: print commands or details about specified command
    ?cr ?nextarg IF
	BEGIN
	    2dup over c@ '-' = IF
		." === Options ===" cr
		[: ."     \O " type ;]
	    ELSE
		." === Commands ===" cr
		[: ."     \U " type ;]
	    THEN  $tmp ['] .usage search-help
	    ." === Details ===" cr
	    [: ."     \G " type ':' emit ;] $tmp ['] .cmd search-help
	?nextarg 0= UNTIL
    ELSE
	." === Options (preceed commands) ===" cr
	s"     \O " ['] .usage search-help
	." === Commands ===" cr
	s"     \U " ['] .usage search-help
    THEN ;

}scope

: next-cmd ( -- )
    ?nextarg 0= IF  n2o:help  EXIT  THEN
    2dup ['] n2o >body search-wordlist
    IF  nip nip execute  ELSE
	[: <err> ." n2o command not found: " mark-start type mark-end
	    <default> cr ;] do-debug
	['] 2drop arg-loop n2o:help
    THEN ;

scope{ n2o

: keyin ( -- )
    \U keyin|inkey file1 .. filen
    \G keyin: read a .n2o key file in
    ?get-me  key>default
    BEGIN  ?nextarg WHILE
	    import#manual import-type !  do-keyin  last-key .?perm
    REPEAT  save-pubkeys ;
: keyout ( -- )
    \U keyout|outkey [@user1 .. @usern]
    \G keyout: output pubkey of your identity
    \G keyout: optional: output pubkeys of other users
    ?get-me ?peekarg IF  2drop out-nicks  ELSE  out-me  THEN ;
: keygen ( -- )
    \U keygen|genkey nick
    \G keygen: generate a new keypair
    gen-keys-dir ?nextarg 0= IF  get-nick  THEN
    new-key ?cr .keys ?rsk ;
: keylist ( -- )
    \U keylist|listkey
    \G keylist: list all known keys
    ?get-me ?cr list-keys ." === groups ===" cr .groups ;

: keyqr ( -- )
    \U keyqr|qrkey [@user1 .. @usern]
    \G keyqr: print qr of own key (default) or selected user's qr
    ?get-me init-client announce-me
    ?peekarg IF  2drop qr-nicks  ELSE  qr-me  THEN ;

: keyscan ( -- )
    \U keyscan|scankey|scanqr|qrscan
    \G keyscan: scan a key in color QR form
    ?get-me init-client
    ?nextarg IF  s" -many" str= 0=  ELSE  true  THEN  to scan-once?
[IFDEF] scan-qr
    scan-qr
[ELSE]
    ." I'm sorry, no scanner implemented yet." cr
[THEN] ;

: keysearch ( -- )
    \U keysearch|searchkey 85string1 .. 85stringn
    \G keysearch: search for keys prefixed with base85 strings,
    \G keysearch: and import them into the key chain
    ?get-me init-client
    keys>search search-keys insert-keys save-pubkeys
    ?cr keylist ;

: perm ( -- )
    \U perm @user1 .. @usern permissions ..
    \G perm: Change or set permissions. permission starts with
    \G perm: + for adding permissions
    \G perm: - for taking away permissions
    \G perm: = sets defaults, add or subtract permissions afterwards
    \G perm: no prefix for setting permissions exactly
    \G perm: c onnect  - allows connections
    \G perm: b locked  - denies connections
    \G perm: d ht      - DHT read/write access
    \G perm: m sg      - can send messages
    \G perm: r ead     - allow to read files
    \G perm: w rite    - allow to write files
    \G perm: n ame     - allow to access file by name
    \G perm: h ash     - allow to access file by hash
    \G perm: s ocket   - allow to access sockets (VPN)
    \G perm: t erminal - allow to access terminal
    \G perm: v termserVer - allow to access termserver
    \G perm: y sYnc    - allow to sync
    \G perm: i ndirect - deny direct connection
    ?get-me
    BEGIN  chat-keys $[]off  @nicks>chat ?nextarg WHILE  >perm
	    chat-keys [: key| key-exist? ?dup-IF .apply-permission THEN ;]
	    $[]map 2drop  REPEAT
    save-keys ;

: group ( -- )
    \U group @user1 .. @usern [+-]group1 .. [+-]groupn ..
    \G group: Set, add or remove a group from a set of users
    ?get-me
    BEGIN  chat-keys $[]off  @nicks>chat
	BEGIN  ?nextarg  WHILE  over c@ '@' <>  WHILE
		    chat-keys [: key| key-exist? ?dup-IF
			  >o 2dup apply-group o>  THEN ;]
		    $[]map 2drop  REPEAT  2drop  THEN
    ?nextarg 0= UNTIL
    save-keys ;

: chgroup ( -- )
    \U chgroup group permissions
    \G chgroup: Add or change a group
    ?nextarg IF  2dup >group-id  dup 0< IF  drop
	    [: #0. { d^ slot } slot 2 cells type type ;] $tmp groups[] $+[]!
	    groups[] $[]# 1- >r  ELSE  >r 2drop  THEN
	?nextarg IF
	    >perm r@ groups[] $[]@ drop 2!
	    write-groups .groups
	THEN  rdrop
    THEN ;

: mvgroup ( -- )
    \U mvgroup oldname1 newname1 .. oldnamen newnamen
    \G mvgroup: Change group's name
    BEGIN  ?nextarg  WHILE
	    >group-id dup 0< !!no-group!! >r ?nextarg  WHILE
		2 cells r@ groups[] $[] $!len
		r> groups[] $[]+!  REPEAT  rdrop  THEN
    write-groups .groups ;

: passwd ( -- )
    \U passwd [pw-level]
    \G passwd: Change the password for the selected secret key
    ?get-me +newphrase key>default
    pk@ key| key-exist? ?dup-IF  >o >storekey @ ke-storekey !
	?nextarg IF  >number drop 0 max config:pw-maxlevel# @ min  ke-pwlevel !  THEN  o>
	save-keys
    THEN ;

: nick ( -- )
    \U nick newnick
    \G nick: Change your nick to <newnick>
    ?get-me ?nextarg IF  pk@ key| key-exist? ?dup-IF
	    >o ke-nick $! key-sign o> save-keys
	ELSE  2drop  THEN  THEN ;

: pet ( -- )
    \U pet nick1|pet1 petnew1 .. nickn|petn petnewn
    \G pet: Add a new petname to existing <nick> or <pet>
    ?get-me
    [: nick-key dup 0= IF  drop EXIT  THEN
      >o ?nextarg IF  ke-pets[] $+[]! pet!  THEN  o> ;] arg-loop
    save-keys keylist ;

: pet- ( -- )
    \U pet- pet1 .. petn
    \G pet-: remove pet name
    ?get-me
    [: 2dup nick-key dup 0= IF  drop 2drop  EXIT  THEN
      >o ke-pets[] [: 2over str= 0= ;] $[]filter 2drop o o>
      last# cell+ del$cell
      last# cell+ $@len 0= IF  last# bucket-off  THEN ;] arg-loop
    save-keys ?cr keylist ;

synonym inkey keyin
synonym outkey keyout
synonym genkey keygen
synonym listkey keylist
synonym qrkey keyqr
synonym searchkey keysearch
synonym scankey keyscan
synonym qrscan keyscan
synonym scanqr keyscan

\ encryption subcommands

: -threefish ( -- )
    \O -threefish
    \G -threefish: use threefish encryption for vaults
    enc-threefish next-cmd ;
: -keccak ( -- )
    \O -keccak
    \G -keccak: use keccak encryption for vaults
    enc-keccak next-cmd ;

: enc ( -- )
    \U enc @user1 .. @usern file1 .. filen
    \G enc: encrypt files for the listed users
    ?get-me args>keylist
    [: key-list encrypt-file ;] arg-loop ;
: dec ( -- )
    \U dec file1 .. filen
    \G dec: decrypt files
    ?get-me [: decrypt-file ;] arg-loop ;
: cat ( -- )
    \U cat file1 .. filen
    \G cat: cat encrypted files to stdout
    ?cr vault>out dec ;

\ hash+signature

0 warnings !@ \ these options look like numbers, don't warn
: -256 ( -- )
    \O -256
    \G -256: set hash output to 256 bits (default)
    $20 to hash-size# next-cmd ;

: -512 ( -- )
    \O -512
    \G -512: set hash output to 512 bits
    $40 to hash-size# next-cmd ;
warnings !

: hash ( -- )
    \U hash file1 .. filen
    \G hash: hash the files and print it base85
    \G hash: use -256 or -512 to select hash size
    \G hash: use -threefish or -keccak to select hash algorithm
    ?cr enc-mode @ 8 rshift $FF and >crypt
    [: 2dup hash-file .85info space type cr ;] arg-loop 0 >crypt ;

: hash-blocks ( -- )
    \U hash-blocks file1 .. filen
    \G hash: hash the files in blocks of 1k and print it base85
    \G hash: use -256 or -512 to select hash size
    \G hash: use -threefish or -keccak to select hash algorithm
    ?cr enc-mode @ 8 rshift $FF and >crypt
    [: 2dup type ." :" cr
	hash-file-blocks ;] arg-loop 0 >crypt ;

: sign ( -- )
    \U sign file1 .. filen
    \G sign: create detached .s2o signatures for all files
    ?get-me now>never
    [: 2dup hash-file 2drop
      [: type ." .s2o" ;] $tmp w/o create-file throw >r
      [: .pk .sig ;] $tmp r@ write-file r> close-file throw throw ;]
    arg-loop ;

: verify ( -- )
    \U verify file1 .. filen
    \G verify: check integrity of files vs. detached signature
    ?get-me ?cr
    [: 2dup hash-file 2drop 2dup type
      [: type ." .s2o" ;] $tmp slurp-file
      over date-sig? dup >r  err-color info-color r> select  attr! .check
      space over keysize .key-id <default>
      drop free throw cr ;]
    arg-loop ;

\ select config file

: -config ( -- )
    \O -config <filename>
    \G -config: Set filename for config file
    ?nextarg 0= ?EXIT  config-file$ $! next-cmd ;

: -conf ( -- )
    \O -conf <value>=<thing>
    \G -conf: Set a config value
    ?.net2o-config \ read config if necessary
    ?nextarg 0= ?EXIT ['] config-line execute-parsing next-cmd ;

\ server mode

: -lax ( -- )
    \O -lax
    \G -lax: open for all keys
    perm%default to perm%unknown  next-cmd ;

: server ( -- )
    \U server
    ?get-me init-server announce-me server-loop-catch ;

: rootserver ( -- )
    \U rootserver
    perm%default to perm%unknown
    \ well known 0key of a dht root, !!FIXME!! to allow changing that
    85" 4mzuLhu{7P*8hkQlK%$h7DJC%X}$dzEru6B{BdlX" my-0key sec!
    ?get-me init-server addme-owndht server-loop-catch ;

\ dht commands

: announce ( -- )
    \U announce
    \G announce: Only announce ID
    ?get-me init-client announce-me ;

: lookup ( -- )
    \U lookup
    \G lookup: query DHT for addresses
    ?get-me ?cr init-client nicks>search search-addrs
    search-key[] [: 2dup .simple-id ." :" cr
      >d#id >o dht-host [: .host cr ;] $[]map o> ;] $[]map ;

: ping ( -- )
    \U ping
    \G ping: query DHT and send a ping to the observed addresses
    \G ping: is not ready yet
    ?get-me init-client nicks>search search-addrs pings[] $[]off
    search-key[] [: >d#id >o dht-host [: pings[] $+[]! ;] $[]map o> ;] $[]map
    pings[] [: send-ping ;] $[]map  receive-pings ;

\ chat mode

: -root ( -- )
    \O -root <address[:port]>
    ?nextarg 0= ?EXIT ':' $split dup IF  s>number drop to net2o-port
    ELSE  2drop  THEN  net2o-host $!  dhtroot-off
    next-cmd ;

: -rootnick ( -- )
    \O -rootnick <nick>
    ?nextarg 0= ?EXIT  dhtnick $!  dhtroot-off  next-cmd ;

: -port ( -- )
    \O -port <port#>
    \G -port: sets port to a fixed number for reachability from outside,
    \G -port: allows to define port forwarding rules in the firewall
    \G -port: only for client; server-side port is different
    ?nextarg 0= ?EXIT  s>number drop to net2o-client-port
    next-cmd ;

: -otr ( --- )
    \O -otr
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
    ?get-me ?cr init-client
    BEGIN  ?nextarg  WHILE  ." ====== Chat log for " 2dup type
	    over c@ '@' = IF  1 /string nick>pk key| ."  key: " 2dup 85type  THEN
	    ."  ======" cr msg-group$ $!
	    msg-group$ $@ [ -1 1 rshift cell/ ]l load-msgn REPEAT ;

: invite ( -- )
    \U invite @user ["Invitation text"]
    \G invite: send or accept an invitation to another user
    announce @nicks>chat
    ?nextarg IF  config:invite$ $!  THEN
    chat-keys [: n2o:pklookup send-invitation ;] $[]map
    ." invitation" chat-keys $[]# 1 > IF ." s" THEN  ."  send" forth:cr ; 

\ script mode

: cmd ( -- )
    \U cmd
    \G cmd: Offer a net2o command line for client stuff
    get-me ." net2o interactive shell, type 'bye' to quit"
    0 to script? n2o-cmds ;

: script ( -- )
    \U script file
    \G script: read a file in script mode
    ?nextarg 0= IF  help  EXIT  THEN
    get-me init-client word-args ['] included do-net2o-cmds ;

: sh ( -- )
    \U sh cmd
    \G sh: evaluate rest of command as shell command
    source >in @ /string system  source nip >in ! ;

: debug ( -- )
    \U debug [+|-<switch>]
    \G debug: set or clear debugging switches
    also forth
    BEGIN  ?peekarg  WHILE  +-?  WHILE  ?nextarg drop set-debug  REPEAT  THEN
    previous ;

: see ( -- )
    \U see [::scope1] file1 .. [::scopen] filen
    \G see: decomile a file, scopes are a global state;
    \G see: default scope is determined by file extension,
    \G see: setup scope if not defined
    setup-table @ see:table !
    [: 2dup s" ::" string-prefix? IF
	    2 /string [: type ." -table" ;] $tmp find-name
	    name>int execute @ see:table !
	ELSE
	    see:table @ >r
	    ." ===== " 2dup forth:type ."  =====" forth:cr
	    2dup suffix>table see:table !
	    0 { w^ content } content $slurp-file
	    content $@ see:table n2o:see-table forth:cr
	    content $free  r> see:table !
	THEN
    ;] arg-loop ;

\ file copy

: get ( -- )
    \U get @user file1 .. filen
    \G get: get files into current directory
    ?get-me init-client
    ?@nextarg IF
	dvcs-bufs# nick-connect ." connected" cr !time
	BEGIN  +resend
	    net2o-code expect+slurp  close-all
	    $10 blocksize! $A blockalign!
	    $10 [: 2dup basename n2o:copy ;] arg-loop#
	    end-code|  file:close-all
	    ?peekarg  WHILE  2drop
	REPEAT
	c:disconnect  THEN ;

: get# ( -- )
    \U get# @user hash1 .. hashn
    \G get#: get files by hash into hash directory
    ?get-me init-client
    ?@nextarg IF
	dvcs-bufs# nick-connect ." connected" cr !time
	BEGIN  +resend
	    net2o-code expect+slurp  close-all
	    $10 blocksize! $A blockalign!
	    $10 [: base85>$ n2o:copy# ;] arg-loop#
	    end-code|  file:close-all
	    ?peekarg  WHILE  2drop
	REPEAT
	c:disconnect  THEN ;

\ dvcs commands

: init ( -- )
    \U init [name [branchname]]
    \G init: Setup a dvcs project in the current folder
    \G init: The default branch name is "master"
    \G init: The default project name is the directory it resides in
    ".n2o" $1FF init-dir drop
    ".n2o/files" touch
    ?nextarg 0= IF  pad $1000 get-dir 2dup '/' -scan nip /string THEN
    n2o:new-dvcs >o project:project$ $!
    ?nextarg 0= IF  "master"  THEN  project:branch$ $!
    save-project  n2o:dispose-dvcs o> ;

: add ( -- )
    \U add file1 .. filen
    \G add: add files to the dvcs project in the current folder
    n2o:new-dvcs >o files>dvcs
    ['] dvcs-add arg-loop  n2o:dispose-dvcs o> ;

: ref ( -- )
    \U ref file1 .. filen
    \G ref: add files to the dvcs project in the current folder
    \G ref: as references
    n2o:new-dvcs >o files>dvcs
    ['] dvcs-ref arg-loop  n2o:dispose-dvcs o> ;

: ci ( -- )
    \U ci "message"
    \G ci: check added and modified files into the dvcs project
    ?get-me ci-args dvcs-ci ;

: co ( -- )
    \U co revision|@branch|revision@branch
    \G co: check out a specific revision
    ?get-me ?nextarg IF  dvcs-co  THEN
;

: fetch ( -- )
    \U fetch project1@user1... projectn@usern
    \G fetch: get the updates from other users (possible multiple)
    \G fetch: Similar syntax as for chats
    ?get-me init-client nicks>chat handle-fetch ;

: up ( -- )
    \U up
    \G up: check out last revision of current branch
    ?get-me dvcs-up ;

: pull ( -- )
    \U pull project1@user1... projectn@usern
    \G pull: get the updates from other users (possible multiple)
    \G pull: and checkout the last revision (fetch+up).
    \G pull: Similar syntax as for chats
    ?get-me init-client nicks>chat handle-fetch dvcs-up ;

: revert ( -- )
    \U revert
    \G revert: revert changes
    ?get-me dvcs-revert ;

: fork ( -- )
    \U fork branch
    \G fork: create a branch, not yet implemented
    !!FIXME!!
;

: snap ( -- )
    \U snap
    \G snap: create a snapshot of the current revision
    ci-args dvcs-snap ;

: diff ( -- )
    \U diff
    \G diff: diff between last checkin state and current state
    ?get-me ?cr dvcs-diff ;

: log ( -- )
    \U log
    \G log: print out log of current branch
    ?get-me ?cr dvcs-log ;

\ manage your hash objects directly (no list available)

: add# ( -- )
    \U add# file1 .. filen
    \G add#: add files to hash storage
    ?get-me ['] hash-add arg-loop ;

: out# ( -- )
    \U out# hash1 .. hashn
    \G out#: get files out of hash storage in clear
    ?get-me ['] hash-out arg-loop ;

\ others

: bye ( -- )
    \U bye
    \G bye: quit command mode and terminate program
    subme bye ;

: -bw ( -- )
    \O -bw
    \G -bw: disable color codes
    ['] drop is attr!  next-cmd ;

: -yes ( -- )
    \O -yes
    \G -yes: say yes to every question
    true to ?yes  next-cmd ;

: -backtrace ( -- )
    \O -backtrace
    \G -backtrace: Provide full error reporting ;
    [ what's DoError ]l is DoError next-cmd ;

' /chat:version alias version
    \U version
    \G version: print version string

: rng ( -- )
    \U rng
    \G rng: check rng and give a 32 byte random value
    ?check-rng $20 rng$ 85type cr
    check-old$ $@ ['] .rngstat stderr outfile-execute  check-old$ $free ;

}scope

\ use a different history file for net2o

: n2o-history ( -- )
    history ?dup-IF  close-file throw  THEN
    "~/.n2o-history" get-history ;

n2o-history

\ user friendly, but way less informative doerror

: ?set-debug ( -- )
    debugging-method 0= IF
	[: [: <err> .error-string <default> cr ;] do-debug ;] is DoError
    ELSE
	[ what's DoError ]L is DoError
    THEN ;
?set-debug

:noname defers 'cold ?set-debug n2o-history ; is 'cold

\ allow issuing commands during chat

scope{ /chat

: n2o [: word-args ['] evaluate do-net2o-cmds ;] catch
    ?dup-IF  <err> ." error: " error$ type cr <default>  THEN ;

}scope

: start-n2o ( -- )
    cmd-args ++debug %droprate \ read in all debugging stuff
    argc @ 1 > IF next-cmd ELSE n2o:help THEN
    n2o:bye ;

' start-n2o is process-args
