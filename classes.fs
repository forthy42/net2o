\ net2o classes

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

\ job context structure and subclasses

current-o

Variable contexts \G contains all command objects

0 Value my-key-default \G default own key

object class
    field: token-table
    value: parent
    value: my-key        \ key used for this context
    field: req?
    field: c-state \ state for checks whether everything is there
    method start-req
    method nest-sig \ check sig first and then nest
end-class cmd-class \ command interpreter
' noop cmd-class to start-req
:noname ( addr u -- flag ) 2drop -1 ; cmd-class to nest-sig

Variable cmd-table
Variable reply-table
Variable log-table
Variable setup-table
Variable connect-table
Variable ack-table
Variable msging-table
Variable msg-table
Variable term-table
Variable address-table
Variable context-table
Variable key-entry-table
Variable vault-table
Variable pay-table

Vocabulary mapc

also mapc definitions

cmd-class class
    64value: dest-vaddr
    value: dest-size
    value: dest-raddr
    $value: dest-ivs$
    value: dest-ivsgen
    cvalue: dest-ivslastgen
    cvalue: dest-req   \ -/-                    true if ongoing request
    $value: dest-ivsrest$
    value: dest-timestamps
    value: dest-replies
    \                   sender:                receiver:
    value: dest-top   \ -/-                    sender read up to here
    value: dest-head  \ read up to here        received some
    value: dest-tail  \ send from here         received all
    value: dest-back  \ flushed on destination flushed
    field: dest-end   \ -/-                    true if last chunk
    field: do-slurp
    method free-data
    method regen-ivs
    method handle
    method rewind-timestamps
    method rewind-partial
end-class code-class
' drop  code-class to regen-ivs
' noop  code-class to rewind-timestamps
' 2drop code-class to rewind-partial

code-class class
    field: data-resend# \ resend tokens; only for data
    value: send-ack#
end-class data-class

code-class class
    field: data-ackbits
    field: data-ackbits-buf
    field: data-ack#     \ fully acked bursts
    field: ack-bit#      \ actual ack bit
    field: data-resend#-buf
    cvalue: ack-advance?  \ ack is advancing state
end-class rcode-class

rcode-class class
    value: rec-ack#
end-class rdata-class

previous definitions

cmd-class class
    field: timing-stat
    field: track-timing
    field: flyburst
    field: flybursts
    field: timeouts
    field: window-size \ packets in flight
    64field: rtdelay \ ns
    64field: last-time
    64field: lastack \ ns
    64field: recv-tick
    64field: ns/burst
    64field: last-ns/burst
    64field: bandwidth-tick \ ns
    64field: next-tick \ ns
    64field: extra-ns
    64field: slackgrow
    64field: slackgrow'
    64field: lastslack
    64field: min-slack
    64field: max-slack
    64field: time-offset  \ make timestamps smaller
    64field: lastdeltat
end-class ack-class

cmd-class class
    field: peers[]
    field: silent-last#
    field: otr-shot
end-class msging-class

cmd-class class
scope: msg
    method start
    method tag
    method chain
    method signal
    method re
    method text
    method object
    method id
    method action
    method coord
    method payment
    method end
}scope
end-class msg-class

cmd-class class
scope: pay
    field: pks[]        \ all the pks stored here, an array
    field: sigs[]       \ all the signatures stored here, an array
    field: wallets[]    \ array of wallets
    field: assets[]     \ all selected assets
    value: current-pk
    value: current-asset
    method last-contract
    method source
    method #source
    method sink
    method asset
    method #asset
    method amount
    method balance
    method comment
}scope
end-class pay-class

begin-structure wallet
    field: contract#
    field: assets[]
    field: amounts[]
    field: $comments[]
    field: $sig
end-structure

\ object/reference types

scope{ msg
0
enum image#
enum thumbnail#
enum patch#
enum snapshot#
enum message#
drop
}scope

scope: invit
0
enum none#
enum pend#
enum qr#
drop
}scope

cmd-class class
    \ callbacks
    defer: timeout-xt    \ callback for timeout
    defer: setip-xt      \ callback for set-ip
    defer: ack-xt        \ callback for acknowledge
    defer: punch-done-xt \ callback for NAT traversal ok
    defer: sync-done-xt  \ callback for sync done
    \ maps for data and code transfer
    0 +field start-maps
    value: code-map
    value: code-rmap
    value: data-map
    value: data-rmap
    0 +field end-maps
    \ strings
    0 +field start-strings
    field: resend0
    field: data-resend
    field: pubkey        \ other side official pubkey
    field: punch-addrs
    field: rqd-xts       \ callbacks for request done (array)
    field: my-error-id
    field: beacon-hash
    0 +field end-strings
    field: request-gen   \ pre-generated request number
    field: perm-mask
    \ secrets
    0 +field start-secrets
    field: crypto-key
    field: dest-0key     \ key for stateless connections
    0 +field end-secrets
    \ semaphores
    0 +field start-semas
    1 pthread-mutexes +field filestate-sema
    1 pthread-mutexes +field code-sema
    0 +field end-semas
    \ contexts for subclasses
    field: next-context  \ link field to connect all contexts
    field: log-context
    field: ack-context
    field: msging-context
    field: msg-context
    field: file-state    \ files
    \ rest of state
    field: codebuf#
    field: context#
    field: wait-task
    $10 +field return-address \ used as return address
    $10 +field r0-address \ used for resending 0
    64field: recv-addr
    field: read-file#
    field: write-file#
    field: residualread
    field: residualwrite
    field: blocksize
    field: blockalign
    field: reqmask \ per connection request mask
    field: reqcount \ per connection request count (for non cookie-requests)
    field: request#
    field: filereq#
    field: file-count \ open file count
    field: file-reg#  \ next file id to request
    
    field: data-b2b
    
    value: ack-resends#
    cfield: ack-state
    cvalue: ack-receive
    cvalue: ack-resend~
    
    cvalue: req-codesize
    cvalue: req-datasize

    cvalue: key-setup?     \ true if key setup is done
    cvalue: invite-result# \ invitation result
    \ flow control, sender part

    64field: next-timeout \ ns
    64field: resend-all-to \ ns
    \ flow control, receiver part
    64field: burst-ticks
    64field: firstb-ticks
    64field: lastb-ticks
    64field: delta-ticks
    64field: max-dticks
    64field: last-rate
    \ experiment: track previous b2b-start
    64field: last-rtick
    64field: last-raddr
    field: acks
    field: received
    \ cookies
    field: last-ackaddr
    \ statistics
    KEYBYTES +field tpkc
    KEYBYTES +field tskc
end-class context-class

cmd-class class
    field: host-pri#
    field: host-id
    lfield: host-ipv4
    $10 +field host-ipv6
    wfield: host-portv4
    wfield: host-portv6
    field: host-anchor \ net2o anchor (is a pubkey)
    field: host-route  \ net2o route
    field: host-key    \ psk for connection setup
    field: host-revoke
    field: host-ekey   \ ephemeral key a la MinimaLT
    64field: host-ekey-to \ ephemeral key timeout
end-class address-class

\ cookies

object class
    64field: cc-timeout
    field: cc-context
end-class con-cookie

con-cookie >osize @ Constant cookie-size#

\ permissions

1
bit perm%connect    \ not set for banned people
bit perm%blocked    \ set for banned people - makes sure one bit is set
bit perm%dht        \ can write into the DHT
bit perm%msg        \ can send messages
bit perm%filerd     \ can read files
bit perm%filewr     \ can write files
bit perm%filename   \ can access named files
bit perm%filehash   \ can access files by hash
bit perm%socket     \ can access sockets
bit perm%terminal   \ can access terminal
bit perm%termserver \ can access termserver
bit perm%sync       \ is allowed to sync
bit perm%indirect   \ force indirect connection
drop

perm%connect perm%dht perm%msg perm%filerd perm%filehash or or or or Value perm%default
perm%connect perm%dht perm%indirect or or Value perm%dhtroot
perm%blocked perm%indirect or Value perm%unknown
perm%blocked perm%indirect or invert Value perm%myself
Create perm$ ," cbdmrwnhstvyi"

\ QR tags

scope: qr
0
enum ownkey#
enum key#
enum keysig#
enum hash#
enum sync#    \ sychnronizing info: key+secret
enum payment# \ payment is value+cointype+wallet
drop
}scope

\ timestasts structure

begin-structure timestats
sfvalue: ts-delta
sfvalue: ts-slack
sfvalue: ts-reqrate
sfvalue: ts-rate
sfvalue: ts-grow
end-structure

\ io per-task variables

user-o io-mem

object class
    pollfd 4 *      uvar pollfds \ up to four file descriptors
    sockaddr_in     uvar sockaddr
    sockaddr_in     uvar sockaddr1
    [IFDEF] no-hybrid
	sockaddr_in uvar sockaddr2
    [THEN]
    file-stat       uvar statbuf
    aligned
    cell            uvar ind-addr
    cell            uvar task#
    $F + -$10 and \ align by $10
    maxdata         uvar aligned$
    $10             uvar cmdtmp
    $10             uvar return-addr
    $10             uvar temp-addr
    timestats       uvar stat-tuple
    maxdata 2/ key-salt# + key-cksum# + uvar init0buf
    aligned
    cell            uvar code0-buf^
    cell            uvar code-buf^
    cell            uvar code-buf$^
    cell            uvar code-key^
    \ vault variables
    cell            uvar enc-filename
    cell            uvar enc-file
    cell            uvar enc-padding
    cell            uvar key-list
    \ mapping buffers
    1 64s           uvar new-code-s
    1 64s           uvar new-code-d
    1 64s           uvar new-data-s
    1 64s           uvar new-data-d
    cell            uvar new-code-size
    cell            uvar new-data-size
    cell            uvar do-keypad
    cell            uvar tmp-ivs
    cell            uvar tmp-pubkey
    cell            uvar tmp-my-key
    cell            uvar tmp-perm
    cell            uvar $error-id
end-class io-buffers

\ reply structure

begin-structure reply
    field: reply-len
    field: reply-offset
    64field: reply-dest
    64field: reply-time
    defer: reply-xt  \ execute when receiving an ok
    defer: send-xt   \ executed to (re)send a message
\    field: reply-timeout# \ per-reply timeout counter
\    field: reply-timeout-xt \ per-reply timeout xt
end-structure

\ address to index computations

: addr>bits ( addr -- bits )
    chunk-p2 rshift ;
: addr>bytes ( addr -- bytes )
    [ chunk-p2 3 + ]L rshift ;
: bytes>addr ( bytes addr -- )
    [ chunk-p2 3 + ]L lshift ;
: bits>bytes ( bits -- bytes )
    1- 2/ 2/ 2/ 1+ ;
: bytes>bits ( bytes -- bits )
    3 lshift ;
: addr>ts ( addr -- ts-offset )
    addr>bits 64s ;
: addr>64 ( addr -- ts-offset )
    [ chunk-p2 3 - ]L rshift -8 and ;
: addr>replies ( addr -- replies )
    addr>bits reply * ;
: addr>keys ( addr -- keys )
    max-size^2 rshift [ min-size negate ]L and ;

\ net2o header structure

begin-structure net2o-header
    1 +field hdrflags
    1 +field hdrtags
   16 +field destination
    8 +field mapaddr
end-structure

\ key class

cmd-class class
    field: ke-sk       \ secret key
    field: ke-pk       \ public key
    field: ke-rsk      \ revoke secret (temporarily stored)
    field: ke-wallet   \ wallet
    field: ke-type     \ key type
    field: ke-nick     \ key nick
    field: ke-nick#    \ to avoid colissions, add a number here
    field: ke-pets[]   \ key petnames
    field: ke-pets#    \ to avoid colissions, add a number here
    field: ke-prof     \ profile object
    field: ke-selfsig
    field: ke-sigs[]
    field: ke-imports  \ bitmask of key import
    field: ke-storekey \ used to encrypt on storage
    field: ke-mask     \ permission mask
    field: ke-groups   \ premission groups
    64field: ke-offset \ offset in key file
    field: ke-pwlevel  \ password strength level
    field: ke-sksig    \ signature secret, computed, never stored
    0 +field ke-end
end-class key-entry

\ key related  constants

64 Constant state#
state# 2* Constant state2#
KEYBYTES Constant keysize \ our shared secred is only 32 bytes long
KEYBYTES 2* Constant keysize2 \ pubkey+revkey=64 bytes

: key| ( size -- size' ) keysize umin ;
: key2| ( size -- size' ) keysize2 umin ;

\ specify strength (in bytes), not length! length is 2*strength
32 Constant hash#128 \ 128 bit hash strength is enough!
64 Constant hash#256 \ 256 bit hash strength is more than enough!

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     (("64value:")
       non-immediate (font-lock-type-face . 2)
       "[ \t\n]" t name (font-lock-variable-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]