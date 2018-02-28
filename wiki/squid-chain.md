[up](squid.md) [back](squid-speculation.md) [next](squid-mining.md)

# Why a BlockChain?

So when I'm kind of ok with fiat money (at least with the big ones where the
regulation works), and would use the BlockChain only for trading with
valueables created outside, like fiat money or real estate, then why not use a
system like [GNU Taler](https://taler.net/), which hands over the job of
checking the double spending to the banks?

Well, seems to be a good idea, but it turned out that the banks have
deep troubles with trust, as well.

![In capitalist America, bank robs you](https://pics.me.me/do-you-think-russians-have-in-capitalist-america-memes-in-27816895.png)

So actually, the banks see the BlockChain as the saviour of their
inability to mutual trust.  Or maybe they just have those dollar signs
in the eyes, and fall for the hype?  Who knows.  Proper
decentralization is also a way to create reliable software, so it's
still worth to pursue.

## How to really distribute book-keeping

_For the record: WeChat's payment system has in the order of 1 million TPS
peak on Chinese New Year_

A “distributed database” can be replicated or partitioned (or both).
BlockChains as of now are replicated; that's the scaling problem mentioned in
the bullshit bingo sheet above.  They also need to be partitioned to gain all
the benefits of modern distributed databases.  Doing the partitioning/sharding
off-chain is sidestepping the problem instead of solving it.  (Note that the
lightning network (LN) hints at possible solutions, without being one.  The
main problem of the LN is that it requires on-chain conflict resolution, and
due to the limited amount of on-chain transactions, rogue actors are
encouraged to produce so many conflicts that the external arbiter, the
BlockChain, collapses under the load, and most conflicts are never resolved.)

An important design goal for me is to handle massive ammounts of
micropayments, because that's an application where I see a legitime need for
cryptographic payment protection.  Ransomware fees, tax evasion, and illegal
business are fringe cases without benefit to mankind, and therefore not
interesting.

All coins/accounts have a value, a unit (if you want to keep different kinds
of values in the same ledger, you need that), a creation date (time of the
creating transaction, which also is the index into the corresponding block),
and an owner pubkey+signature, making it immutable without consent of the
owner.

All transactions contain an implicit block hash that refers to the
state of the ledger at the beginning of this transaction, an origin state
transition, and a destination state transition with the same delta
(possible transaction fees left out for simplification).

These are the operations you want to perform in the ledger:

  1. You want to move a coin from one owner to another (may imply
     ledger/shard change)

  2. You want to be able to join several coins into one

  3. You want to split a coin

Transactions are handled by [dump contracts](squid-contracts.md), the rules
are simple enough that even multi-participant transactions are easy to
verify.  All transactions must happen in the active ledger group, i.e. spooky
actions at a distance are strictly limited to the one active dimension of the
hypercube.

![Coin transaction](https://upload.wikimedia.org/wikipedia/commons/thumb/c/ca/Feynmandiagram.svg/220px-Feynmandiagram.svg.png)

I represent this transaction as Feynman diagram, because that almost fits the
concept.  If the source coin (or account) results in a value of 0, it can
disappear from the active states, if the destination coin doesn't yet exist,
it starts with the transaction as initial value.

Coins are pseudonymous, and you can have many different pseudonyms.  However,
coins as proposed here have readable values attached.  It's likely that mix
services will be offered for gaining anonymity.  You transfer to one coin of
the mix service, and you get back several coins from other coins of that same
mix service, and you split and merge them accordingly when you want to do your
next transaction.

There is no need for a nonce, the key as id of the owner is
sufficient.  The check of the balance is sufficient to prevent any
abuse.

## Double entry bookkeeping system

First of all, if you are somehow familiar with bookkeeping, “a ledger”
has something fundamentally wrong in it: The singular.  You are
supposed to have more than one ledger, and every transaction needs to
go to two columns, one as debit, one as credit.  All values are outside the
accountant's hands, so the accountant's balance is always zero (poor gal).

If you want to scale a crypto currency, you want to separate ledgers by some
arbitrary criteria (sharding in database language) so that the individual
nodes are not overloaded, and millions of bookings can get in per second;
which is a fairly reasonable number for micropayment.

Since you want to check if a coin someone offers you has already been
spent, you want to ask the corresponding ledger for it.  The ledger
records incoming and outgoing (credit and debit) in two log files, and
keeps the active coins available for query.  So you want to select the
ledger based on the pubkey of the coin — a short part of it (or its
hash) to a reasonable size.  DDoS attacks at particular ledgers can be
easily mittigated: if the ledger you want to book to is attacked, you
just select a new pubkey; if the ledger you want to book from is
attacked, you may need to use another coin in your wallet, and hope
the attacked ledger is getting unstuck over time.

The ledger units responsible for the same ledger each verify that they
have a consensus over the transactions: by syncing their positions.
And then we combine the ledgers and check the balance: It ought to be
zero.

That will make the transaction protocol a bit tricky, because you have
to make sure that both ledgers really enter your transaction in the
same time slot.  The easy way to do that is to do a staged entry:
First, you queue the credit entries (the „take out“), then you send
them to the debit ledger, and if that succeeds, you can commit both.

To enter a coin from outside into a double booking system, you need two
transactions: One is the coin itself (debit), and the other is a promise to
buy it back (credit).  The promise might not actually be truthful, in which
case the credit is written off: it is however still in the books as loss.

The Purpose of that is to make it easy to cross-check the ledgers for
consistency.  If you have _n_ ledgers, you sum all the columns in them
(which can be done in parallel), and you cross-check by summing all
the _n_ results: The balance ought to be zero.

## Subdividing further

So the first stage scaling is that of individual ledgers for parts of the coin
space, and cross-checking the balance of all of them in one go.  I don't know
when that starts to become an issue, but you have to think about it to scale
further.  You might want to have too many ledgers to sum them up all in one
go, and it's not feasible to connect each of them to every other.

The single balance makes operation easy: You can take coins from any ledger to
any other in one single step (all ledgers are peers of each other).

But that may be too many connections from one ledger server to all the
others.  So here's a way to scale:

We break the ledgers into groups, which are supposed to balance within
one transaction.  This is a 2D example, but it works in any number of
dimensions.  We have two maps of the leger groups, and the
transactions alternate between one and the other group.  That means we
can do either transactions within one local group, or within a global
group that has one element of each local group as possible
destination.  We have 16 groups here, and each ledger needs 30 open
connections, instead of 255 if all were connected to every other
ledger.  So we get to _2\*sqrt(n)-2_ instead of _n-1._  More dimensions
mean higher roots, so with _m_ dimensions, it is _m\*(n)^(1/m)-m._

![Stage 1](../doc/ledger-stage1.svg)
![Stage 2](../doc/ledger-stage2.svg)

In each stage, the ledgers of the same color are connected.  The
balance is calculated only for the group of connected ledgers; the
signatures for the verified balance goes into the next block.  By
interleaving the two modes of connectivity, after only two cycles (or
_m_ in the general case), all previous records from all ledgers are
chained together.

Now you have to route your coin to a destination.  There are two possible
approaches: Let the ledgers do that for you (which is then out of your control
and requires trust that they handle it correctly), or you do it yourself.  I
suppose “do it yourself“ is a good option.

A transaction can move within the currently active ledger group, since that's
where the balance is computed.  You present the original payer a target that
is in his currently active ledger group, doing the first step of the route
there.

Having many accounts active can become a backup problem; remember, quite some
BitCoins where lost by people who scrapped their hard disks.  So ideally,
you'd write down the secret of your wallet on a piece of paper (which is known
to last quite well, and can be easily stored away securely, e.g. in a small
safe).  Now with thousands of wallets spread all over the ledger space, that
could become a big stack of paper, and impossible to reenter.

Suggestion: Only store the seed of a secure PRNG.  The seed (128 bits is
sufficiently strong for Ed25519 keys) generates the secret keys for all the
accounts you want to use.  You run the PRNG long enough to get a pubkey for
every ledger you need.  If the number of ledgers go up over time (due to
scaling), you need to compute more pubkeys, but the same pubkeys you found in
the shorter run remain active.

I call it the SwapDragonChain, as the swap dragon is the mascott of
Forth (the SWAP operation).  I'm sure the swap dragon can handle
double booking quite well with his two heads.

Note that unlike the lightning network, there is no need to worry about the
transactions: they are all public.  Nothing can be lost or stolen.  The
topology is derived from the pubkey values, and not from randomly connected
peers.  The full nodes who want to be responsible for a certain part of the
ledgers know where to connect to.

## Zero-Knowledge Proof?

Zcash tries to hide the transactions by using zero-knowledge proofs
for accounting them.  These proofs are a bit difficult and especially
quite time-consuming.  So if someone has an efficient zero-knowledge
proof that works for this simple transaction, then I could use that.
Homomorphic encryption could do that job.

It needs to proof that if you shift an amount of money from account A
to B (where both accounts share a secret unknown to the auditor), you
can audit that the sums are correct.  And if you change the secret,
you can audit that the change didn't affect the account's value.

That allows you to split a transaction into several steps: First
separate the desired amount from your account, then change the secret
and hand it over to the recipient (who gains knowledge of that secret),
change the secret again, and finally merge it with the destination
account (if wanted).

[up](squid.md) [back](squid-speculation.md) [next](squid-mining.md)
