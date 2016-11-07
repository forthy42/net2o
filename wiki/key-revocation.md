# Key Revocation #

Key revocation (for PKIs without certification authorities) usually is done
with a signature of the lost key, i.e. both the owner and the adversary can
revoke a compromised key.  However, the important function in case of a
revocation is declaring the successor key, which reestablishs trust. To do
that, you must actually proof that you are the legitimate owner of the exposed
secret key, so how do you do that?  After all, the adversary has stolen
it!

Therefore, the requirements are as follows:

  + Only the creator of the secret key can revoke it
  + A thief of the secret key can't (i.e. further information is necessary)
  + Revocation must present a trustworthy replacement key
  + Third parties must trust both the revocation and the replacement key
    without another trustworthy instance, i.e. trusting only their communication
    partner

I create two random number s1 and s2.  Using these numbers, I create
pubkeys p1=[s1]base and p2=[s2]base.  Points can be compressed to a
32 byte number using the compress() function, and then can be treated
as scalar values like [s].  I compute [s]=[s1×compress(p2)] as "work
secret" (i.e. the secret key that is proving my identity), and
p=[s]base, my pubkey.  I publish p and p1, which together are stored
as identity.  The assumption is that p1 can't be reversed to get s1,
and p won't reveal s.  An attacker who stole s can't guess s1, because
he doesn't have p2, and so it's even more difficult to get s2.  An
attacker who stole s can generate a new pair of p1, p2, but that would
give him a different identity (a suspicious identity, though).  After
generating the key, s1 is destroyed; it is no longer needed, though it
can be recomputed using s and p2 and the extended euclidean algorithm.

I keep s2 as offline copy (it's just 64 hex digits or 40 base85
characters), and s as protected online copy in my device; s is subject
to attacks and backdoors, and therefore at risk.  To revoke a key, I
publish p2, which the recipient can validate by [compress(p2)]p1==p.

To sign a new key, I use s2 as signature key, i.e. the recipient can
use the just published p2 to verify the transition to the replacement
key.  Of course, the new key also has a signature with s, the old key.
The format of the revocation attribute is actually ‹new pubkeys: pnew,
p1new› ‹p2, sig using s2› ‹sig using snew› ‹date:never› ‹sig using s›.

Both signatures must have the same signing date, and a never expiration
date (a revocation doesn't expire).  The revocation is in the form of an
address, so if you look up the address of your contact in the DHT, and there's
a revocation, you'll find it.

An alternative way would be to create a signature key (which would be s2),
and use that to sign the working key.  Cross-signing would still prevent
identity theft if just the signature key is stolen.