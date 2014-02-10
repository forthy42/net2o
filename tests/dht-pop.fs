\ populate DHT test

require ../net2o.fs

"anonymous" >key \ get our anonymous key

pkc keysize >d#id

Variable $tag
: tag-word  name>string $tag $! s" :word" $tag $+!
    $tag $@ pkc keysize gen-tag k#tags d#value+ ;
now>never
forth-wordlist ' tag-word map-wordlist
pad $100000 $f0 d#values, 2drop