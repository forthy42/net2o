\ acknowledge time printout

variable oldserv
variable oldclient
variable clientavg
variable clientavg#
variable firstdiff
variable -first

: acktime ( serv client -- )
    clientavg# @
    IF
	over oldserv @ - .
	dup oldclient @ - dup . clientavg @
	clientavg# @ / dup . clientavg# @ $10 = IF - ELSE drop THEN clientavg +!
	2dup swap - firstdiff @ - . cr
    ELSE  2dup swap - firstdiff !  THEN
    clientavg# @ 1+ $10 min clientavg# !
    oldclient ! oldserv ! -first off ;

: acktime-first  ( serv client -- )
    -first on  clientavg# off  acktime ;