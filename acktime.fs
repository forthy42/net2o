\ acknowledge time printout

variable oldserv
variable oldclient
variable first  first on
variable clientavg

: acktime ( serv client -- )
    first @ 0=
    IF
	over oldserv @ - .
	dup oldclient @ - dup . clientavg @ 4 rshift dup . - clientavg +!
	2dup swap - . cr  THEN
    oldclient ! oldserv !  first off ;