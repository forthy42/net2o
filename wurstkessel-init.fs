\ init wurstkessel to be actually useful

: salt-init ( -- )
    s" /dev/random" r/o open-file throw >r
    wurst-salt state# r@ read-file throw drop
    r> close-file throw ;

salt-init
