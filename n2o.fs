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

: del-last-key ( -- )
    keys $[]# 1- keys $[] sec-off
    keys $@len cell- keys $!len ;

: choose-key ( -- o )
    0 BEGIN  drop
	." Choose nick:" cr .secret-nicks
	key '0' - 0 max secret-key dup
    UNTIL ;

\ will ask for your password and if possible auto-select your id

: get-me ( -- )  secret-keys#
    BEGIN  dup 0= WHILE drop
	    ." Passphrase: " +passphrase
	    read-keys secret-keys# dup 0= IF
		."  wrong passphrase, no key found" del-last-key
	    THEN  cr
    REPEAT
    1 = IF  0 secret-key  ELSE  choose-key  THEN  >raw-key ;

: get-me-again ( -- )
    secret-keys# ?EXIT  get-me ;

Variable key-readin

: out-key ( o -- )
    >o pack-pubkey o o>
    [: ..nick-base ." .n2o" ;] $tmp w/o create-file throw
    >r cmdbuf$ r@ write-file throw r> close-file throw ;
: out-me ( -- )
    pkc keysize key-table #@ 0= !!unknown-key!!
    cell+ out-key ;

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

: ?dhtroot ( -- )
    "net2o-dhtroot" nick-key 0= IF
	key>default  import#manual import-type !  64#-1 key-read-offset 64!
	85" ~IIV0ZJeF4b8VIGKy;kXRvO*Z%#uVj>w`?m20n!c*_`(z=^#U_^cShx_>*%pu%=TW2JYz74d9LmgS%xC^mFi7GHGJ2fU|6V5=@_=|V?<pUYd)MQ}Gh(x%jN|0CsN|#@I{mF=Dxfzz_guXQYYMsol1ZRlwF^ol10Y)Pre*WH3YB_*P@2I1dG*gzgaEb2BFV*itIgSylm&Z7!5JeqpL350" do-key
    THEN ;

Variable chat-keys

: nick>chat ( addr u -- )
    host.nick>pk dup 0= !!no-nick!! chat-keys $+[]! ;

: nicks>chat ( -- )
    ['] nick>chat @arg-loop ;

: keys>search ( -- )
    search-key$ $[]off [: base85>$ search-key$ $+[]! ;] arg-loop ;

: wait-key ( -- )
    BEGIN  1 key lshift [ 1 ctrl L lshift 1 ctrl Z lshift or ]L
    and 0=  UNTIL ;

: wait-chat ( -- )
    chat-keys [: 2dup keysize2 /string tuck <info> type IF '.' emit  THEN
	.key-id space ;] $[]map
    ." is not online. press key to recheck."
    [: 0 to connection -56 throw ;] is do-disconnect
    [: false chat-keys [: keysize umin pubkey $@ str= or ;] $[]map
	IF  bl inskey  THEN  up@ wait-task ! ;] is do-connect
    wait-key cr [: up@ wait-task ! ;] IS do-connect ;

: last-chat-peer ( -- chat )
    msg-group$ $@ msg-groups #@ dup cell- 0 max /string
    IF  @  ELSE  drop 0  THEN ;

: search-peer ( -- chat )
    false chat-keys
    [: keysize umin rot dup 0= IF drop search-connect
      ELSE  nip nip  THEN ;] $[]map ;

: search-chat ( -- chat )
    group-master @ IF  last-chat-peer  EXIT  THEN
    search-peer ;

: +group ( -- )
    msg-group$ $@ dup IF
	o { w^ group } 2dup msg-groups #@ d0<> IF
	    group cell last# cell+ $+!
	ELSE  group cell 2swap msg-groups #!  THEN
    ELSE  2drop  THEN ;

: chat-connect ( -- )
    0 chat-keys $[]@ $A $A pk-connect !time
    +resend-msg  greet +group ;

: ?chat-connect ( -- )
    BEGIN  0 chat-keys $[]@ pk-peek? 0= WHILE
	    wait-chat  search-chat ?dup UNTIL  ELSE  0  THEN
    ?dup-IF  >o rdrop  ELSE  10 ms chat-connect  THEN ;

: chat-user ( -- )
    ?chat-connect
    msg-group$ $@len 0= IF  pubkey $@ key| msg-group$ $!  THEN
    o { w^ connect }
    connect cell msg-group$ $@ msg-groups #!
    ret+beacon group-chat ;

: handle-chat ( char -- )
    '@' <> IF \ group chat
	?nextarg drop
	'@' $split 2swap msg-group$ $!
	"" msg-group$ $@ msg-groups #!
	dup 0<> IF  nick>chat  ELSE  2drop group-master on  THEN
    THEN
    nicks>chat group-master @ IF  group-chat  ELSE  chat-user  THEN ;

\ commands for the command line user interface

Vocabulary n2o

: do-net2o-cmds ( xt -- )
    get-order n>r get-recognizers n>r
    ['] n2o >body 1 set-order
    ['] rec:word 1 set-recognizers catch
    nr> set-recognizers nr> set-order throw ;

: n2o-cmds ( -- )
    init-client word-args ['] quit do-net2o-cmds ;

synonym \U \G

: search-help ( pattern xt -- )
    [ loadfilename 2@ ] sliteral open-fpath-file throw
    [: >r BEGIN  refill  WHILE
		source 2over string-prefix? IF  r@ execute  THEN
	REPEAT rdrop 2drop ;] execute-parsing-named-file ;

: .usage ( addr u -- addr u )
    ." n2o " source 7 /string type cr ;
: .cmd ( addr u -- addr u )
    source 2over nip /string type cr ;

get-current also n2o definitions

: help ( -- )
    \U help [cmd]
    \G help: print commands or details about specified command
    ?nextarg IF  BEGIN
	2dup [: ."     \U " type ;] $tmp ['] .usage search-help
	[: ."     \G " type ':' emit ;] $tmp ['] .cmd search-help
    ?nextarg 0= UNTIL  ELSE
	s"     \U " ['] .usage search-help
    THEN ;

set-current

: next-cmd ( -- )
    ?nextarg 0= IF  help  EXIT  THEN  ['] n2o >body search-wordlist
    IF  execute  ELSE  help  THEN ;

get-current n2o definitions

: keyin ( -- )
    \U keyin/inkey file1 .. filen
    \G keyin: read a .n2o key file in
    get-me  import#manual import-type !  key>default
    BEGIN  ?nextarg WHILE  do-keyin  REPEAT  save-pubkeys ;
: keyout ( -- )
    \U keyout/outkey [@user1 .. @usern]
    \G keyout: output pubkey of your identity
    \G keyout: optional: output pubkeys of other users
    get-me ?peekarg IF  2drop out-nicks  ELSE  out-me  THEN ;
: keygen ( -- )
    \U keygen/genkey nick
    \G keygen: generate a new keypair
    ?nextarg 0= IF  get-nick  THEN
    +newphrase key>default
    2dup key#user +gen-keys .rsk .keys
    secret-keys# 1- secret-key >raw-key
    out-me ?dhtroot  save-keys ;
: keylist ( -- )
    \U keylist/listkey
    \G keylist: list all known keys
    get-me
    key-table [: cell+ $@ drop cell+ ..key-list ;] #map ;

: keyqr ( -- )
    \U keyqr/qrkey [@user1 .. @usern]
    \G keyqr: print qr of own key (default) or selected user's qr
    get-me ?peekarg IF  2drop qr-nicks  ELSE  qr-me  THEN ;

: keysearch ( -- )
    \U keysearch/searchkey 85string1 .. 85stringn
    \G keysearch: search for keys prefixed with base85 strings,
    \G keysearch: and import them into the key chain
    get-me  init-client
    keys>search search-keys insert-keys save-pubkeys
    keylist ;

synonym inkey keyin
synonym outkey keyout
synonym genkey keygen
synonym listkey keylist
synonym qrkey keyqr
synonym searchkey keysearch

\ encryption subcommands

: -threefish ( -- )
    \U -threefish <next-cmd>
    enc-threefish next-cmd ;
: -keccak ( -- )
    \U -keccak <next-cmd>
    enc-keccak next-cmd ;

: enc ( -- )
    \U enc @user1 .. @usern file1 .. filen
    get-me args>keylist
    [: key-list encrypt-file ;] arg-loop ;
: dec ( -- )
    \U dec file
    get-me [: decrypt-file ;] arg-loop ;
: cat ( -- )
    \U cat file
    vault>out dec ;

\ hash+signature

: -256 ( -- )
    \U -256 <next-cmd>
    \G +256: set hash output to 256 bits (default)
    $20 to hash-size# next-cmd ;

: -512 ( -- )
    \U -512 <next-cmd>
    \G +512: set hash output to 512 bits
    $40 to hash-size# next-cmd ;

: hash ( -- )
    \U hash file1 .. filen
    enc-mode @ 8 rshift $FF and >crypt
    [: 2dup hash-file .85info space type cr ;] arg-loop 0 >crypt ;

: sign ( -- )
    \U sign file1 .. filen
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

\ server mode

: -lax ( -- )
    \U -lax <next-cmd>
    \G -lax: open for all keys
    strict-keys off next-cmd ;

: server ( -- )
    \U server
    get-me init-server announce-me server-loop ;

: rootserver ( -- )
    \U rootserver
    strict-keys off get-me init-server server-loop ;

: announce ( -- )
    \U announce
    \G announce: Only announce ID
    get-me announce-me ;

\ chat mode

: -root ( -- )
    \U -root <address[:port]> <next-cmd>
    ?nextarg 0= ?EXIT ':' $split dup IF  s>number drop to net2o-port
    ELSE  2drop  THEN  net2o-host $!
    next-cmd ;

: -rootnick ( -- )
    \U -rootnick <nick> <next-cmd>
    ?nextarg 0= ?EXIT  dhtnick $! next-cmd ;

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
    \U chat @user   to chat privately with a user
    \U chat group@user   to chat with the chatgroup managed by user
    \U chat group   to start a group chat (peers may connect)
    get-me init-client announce-me
    ?peekarg IF  drop c@ handle-chat  THEN ;

: chatlog ( -- )
    \U chatlog @user1/group1 .. @usern/groupn 
    \G chatlog: dump chat log
    get-me
    BEGIN  ?nextarg  WHILE  ." === Chat log for " 2dup type ."  ===" cr
    over c@ '@' = IF  1 /string nick>pk key|  THEN  load-msg  REPEAT ;

: invite ( -- )
    \U invite @user
    \G invite: send or accept an invitation to another user
    get-me init-client announce-me nicks>chat 
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

\ file copy

: get ( -- )
    \U get @user file1 .. filen
    \G get: get files into current directory
    get-me init-client
    ?@nextarg IF
	$A $E nick-connect ." connected" cr
	net2o-code expect-reply
	$10 blocksize! $A blockalign!
	[: 2dup 2dup '/' -scan nip /string n2o:copy ;] arg-loop
	n2o:done end-code| n2o:close-all
	c:disconnect  THEN ;

: bye ( -- )
    subme bye ;

set-current

\ allow issuing commands during chat

get-current also chat-/cmds definitions

: n2o [: word-args ['] evaluate do-net2o-cmds ;] catch
    ?dup-IF  <err> ." error: " error$ type cr <default>  THEN ;

set-current previous

: +? ( addr u -- flag )  0= IF  drop false  EXIT  THEN  c@ '+' = ;

: ++debug ( -- )
    BEGIN  argc @ 1 > WHILE  1 arg +?  WHILE
		1 arg debug-eval $!
		s" db " debug-eval 1 $ins
		s" (" debug-eval $+!
		debug-eval $@ evaluate
		shift-args
	REPEAT  THEN ;

previous
