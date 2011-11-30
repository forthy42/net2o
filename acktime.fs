\ acknowledge time printout

variable oldserv
variable oldclient
variable first  first on
variable clientavg
variable firstdiff

: acktime ( serv client -- )
    first @ 0=
    IF
	over oldserv @ - .
	dup oldclient @ - dup . clientavg @ 4 rshift dup . - clientavg +!
	2dup swap - firstdiff @ - . cr
    ELSE  2dup swap - firstdiff !  THEN
    oldclient ! oldserv !  first off ;