\ acknowledge time printout

variable oldserv
variable oldclient
variable first  first on

: acktime ( serv client -- )
    first @ 0=
    IF  over oldserv @ - . dup oldclient @ - . 2dup swap - . cr  THEN
    oldclient ! oldserv !  first off ;