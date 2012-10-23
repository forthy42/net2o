\ init wurstkessel to be actually useful

: random-init ( -- )
    s" /dev/random" r/o open-file throw >r
    wurst-salt state# r@ read-file throw drop
    r> close-file throw ;

: read-wurstrng ( fd -- )  >r
    0. r@ reposition-file throw
    wurst-salt state# r@ read-file throw drop
    state-init state# r@ read-file throw drop
    r> close-file throw ;

: write-wurstrng ( -- )
    s" ~/.wurstrng" r/w create-file throw >r
    wurst-salt state# r@ write-file throw
    state-init state# r@ write-file throw
    r> close-file throw ;

: salt-init ( -- )
    s" ~/.wurstrng" r/o open-file IF  drop random-init
    ELSE  read-wurstrng  THEN  rng-step write-wurstrng ;

salt-init
