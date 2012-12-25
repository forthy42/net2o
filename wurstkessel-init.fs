\ init wurstkessel to be actually useful

: random-init ( -- )
    s" /dev/random" r/o open-file throw >r
    wurst-salt state# r@ read-file throw drop
    r> close-file throw ;

: read-wurstrng ( fd -- flag )  { fd }
    0. fd reposition-file throw
    wurst-salt state# fd read-file throw state# = >r
    state-init state# fd read-file throw state# = r> and
    fd close-file throw ;

: write-wurstrng ( -- )
    s" ~/.wurstrng" r/w create-file throw >r
    rng-buffer cell+ state# 2* r@ write-file throw
    r> close-file throw ;

: salt-init ( -- )
    s" ~/.wurstrng" r/o open-file IF  drop random-init
    ELSE  read-wurstrng  0= IF  random-init  THEN  THEN
    rng-step write-wurstrng rng-step ;

wurst-init
salt-init
