# Random Number Seat Belts

Random number generators are a known attack vector to weaken cryptography.  I
use all techniques I know to make sure net2o uses a cryptographically strong
random number (CSPRNG).

## What do you need for a CSPRNG?

  + An entropy source — I use the OS for that, ''/dev/urandom'' is known good.
  + A secure, non-reversible expansion algorithm — I use keccak, which
    encrypts again and again the same output buffer using an ever-changing
    secret state (key erasure).  An attacker should not be able to guess past
    random numbers from the current state, and should have difficulties to
    guess future ones after re-injection of entropy.

These two things are good enough, but here's about the seat belts, the
additional level of security to make sure even if one of these two fails
suddenly, it's not a debacle.

## Detection of low-entropy PRNG

I store a 128 bit short extraction of the random number pool in a history
file, and compare each extraction with the contents of that file.  It should
not appear twice (likelyhood: 2^-64).  It's not long enough to recover
previous random number states, and it is not short enough to accidently have
collissions.  You can restart net2o 2^64 times to get a 50% chance of
collission.  Your history file will be far too long by then, and you will want
to delete it.  This is the check part of the seat belt: if it's not attached,
it will beep.

## Key erasure and rolling tag

I store an initializing state for the PRNG, first generated together with your
sekret key.  This is the time when a low-entropy system can ask the user to
add more entropy by e.g. moving the mouse or walking over the keyboard.  That
initial state then has enough randomness.

On every start of net2o, I mix it together with entropy from ''/dev/random''
and replace the previous saved content.  This is to prevent a forward security
attack.  To make sure the initial state can't be used to recover forward
secrecy, it's just a part of the overall state, and overwritten by generating
more random numbers afterwards; generating more random numbers will replace
the secret state with a new one.  This technique is called _“key erasing
PRNG”_.  This is important.

Note that a revision controlling file system can know the save time and all
the states of the previous init files.  If the entropy is very lousy, and only
related to the system time when reading it, recovery of old keys is still
possible.  Therefore, you should not store the random number initializer on a
version controlling file system.

## Literature

1. [DJB on key erasure](https://blog.cr.yp.to/20170723-random.html)
