\ init wurstkessel to be actually useful

s" /dev/random" r/o open-file throw value randfd
wurst-salt state# randfd read-file throw drop
randfd close-file throw