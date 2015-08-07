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

object class
    field: token-table
    field: parent
    field: req?
    field: c-state \ state for checks whether everything is there
    method start-req
    method nest-sig \ check sig first and then nest
end-class cmd-class \ command interpreter
' noop cmd-class to start-req
:noname ( addr u -- flag ) 2drop false ; cmd-class to nest-sig

Variable cmd-table
Variable reply-table
Variable log-table
Variable setup-table
Variable ack-table
Variable msg-table
Variable term-table
Variable address-table

cmd-class class
    64field: dest-vaddr
    field: dest-size
    field: dest-raddr
    field: dest-ivs
    field: dest-ivsgen
    field: dest-ivslastgen
    field: dest-ivsrest
    field: dest-timestamps
    field: dest-replies
    field: dest-round \ going to be obsoleted
    \                   sender:                receiver:
    field: dest-top   \ -/-                    sender read up to here
    field: dest-head  \ read up to here        received some
    field: dest-tail  \ send from here         received all
    field: dest-back  \ flushed on destination flushed
    field: dest-end   \ -/-                    true if last chunk
    field: do-slurp
    method free-data
    method regen-ivs
    method handle
    method rewind-timestamps
    method rewind-partial
end-class code-class
' drop code-class to regen-ivs
' noop code-class to rewind-timestamps
' drop code-class to rewind-partial

code-class class
    field: data-resend# \ resend tokens; only for data
end-class data-class

code-class class
    field: data-ackbits
    field: data-ackbits-buf
    field: data-ack#     \ fully acked bursts
    field: ack-bit#      \ actual ack bit
    field: ack-advance?  \ ack is advancing state
    field: data-resend#-buf
end-class rcode-class

rcode-class class end-class rdata-class

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
    field: last-msg
    field: last-group
end-class msg-class

cmd-class class
    \ callbacks
    field: timeout-xt \ callback for timeout
    field: setip-xt   \ callback for set-ip
    field: ack-xt     \ callback for acknowledge
    \ maps for data and code transfer
    0 +field start-maps
    field: code-map
    field: code-rmap
    field: data-map
    field: data-rmap
    0 +field end-maps
    \ strings
    0 +field start-strings
    field: resend0
    field: data-resend
    field: pubkey \ other side official pubkey
    field: mpubkey \ our side official pubkey
    field: punch-load
    field: punch-gen
    0 +field end-strings
    \ secrets
    0 +field start-secrets
    field: crypto-key
    field: dest-0key    \ key for stateless connections
    0 +field end-secrets
    \ semaphores
    0 +field start-semas
    1 pthread-mutexes +field filestate-sema
    1 pthread-mutexes +field code-sema
    0 +field end-semas
    \ contexts for subclasses
    field: next-context \ link field to connect all contexts
    field: log-context
    field: ack-context
    field: msg-context
    field: file-state \ files
    \ rest of state
    field: codebuf#
    field: context#
    field: wait-task
    $10 +field return-address \ used as return address
    $10 +field r0-address \ used for resending 0
    64field: recv-addr
    field: recv-flag
    field: read-file#
    field: write-file#
    field: residualread
    field: residualwrite
    field: blocksize
    field: blockalign
    field: reqmask \ per connection request mask
    field: request#
    field: filereq#

    field: data-b2b
    
    cfield: ack-state
    cfield: ack-resend~
    cfield: ack-resend#
    cfield: is-server
    field: ack-receive
    
    field: req-codesize
    field: req-datasize
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
    field: host-route \ net2o route
    field: host-key
end-class address-class

Variable context-table

0 [IF]
Local Variables:
forth-local-words:
    (
     (("net2o:" "+net2o:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     ("[a-z0-9]+(" immediate (font-lock-comment-face . 1)
      ")" nil comment (font-lock-comment-face . 1))
    )
forth-local-indent-words:
    (
     (("net2o:" "+net2o:") (0 . 2) (0 . 2) non-immediate)
    )
End:
[THEN]