# Proof of What?

It is pretty obvious that BitCoin went back all the way through human money
history, and uses the same principle shell money had to give value to a coin:
the miners do a proof of work.  Like the shells, the amount is limited, and
the difficulty raises with the amount of work available.  The limited amount
of mineable BitCoins mimics another property of shell money, and with a
quickly growing economy that actually uses cryptocurrencies, it becomes a
quickly deflating currency.  That makes it ideal as speculation object, and
very bad as trade token.

There is a disastrous side effect to this: Mining burns energy, ASICs are
developed and manufactured, if you try to avoid ASIC mining, all available
cost-efficient GPUs are bought and gamers are frustated, because they simply
can't get hold of them.  This is far worse to hoarding gold, because nobody
actually needs gold anywhere near the available quantities used as money.

And all that big amount of work is converted into very few actual
transactions.  BitCoin exchanges pop up that allow people to trade with them
without actually making transactions in the BitCoin protocol; and without
those transactions, that money is not secured.  Virtual bankrobbery occurs,
and it shows that people who don't know history are doomed to repeat it:
Regulation is there for good reasons.

Note that the energy consumption is not a function of how many transactions
happen, it's a function of how much reward the miners get.  That means higher
bitcoin prices result in higher energy consumption, because the proof of work
scales with the amount of available work, and that inflates with the price per
coin.

That means that the lightning network, while improving scalability, does not
cut down the hidden transaction costs: the liquidity required for the
lightning network will drive the price per coin up, so that mining is so
lucrative that miners will buy more ASICs and burn more coal in turn.

## What is a BlockChain?

We need an actual definition; technically, even a git repository has
some important properties of a BlockChain.  The chain of hashed blocks
is one aspect, the consensus algorithm the other:

  + Merkle-tree or equivalent hash-it-all approach (loose definition)
  + no single point of trust
  + consensus algorithm based on the contents only (no external arbiter)

## How to cheaply secure the BlockChain

So let's take a step back, and look why there's a need for the proof of work
(the consensus algorithm): The basic idea is that of securing the BlockChain
against an attack that allows double spending.  The BlockChain itself is
immutable if you have access to the last hashed block: Through a link of
hashes, every other block before can't be changed without changing the last
block, too.  The problem is: how do you know it's _the_ valid last block?

BitCoin's proof of work concept is that you need to invest a certain amount of
work to sign a block, so the older a block is, the more work it takes to forge
it, and reciprocal, the more work that went into a chain, the more “true” it
is.  That concept originates from a crypto-anarchic design: in the BitCoin
world, everybody is pseudonymous, so even the signature for the blocks are
done by anonymous cowards (and that is just a hash, there's no identity
associated with that hash).  We are back before the first promissory notes,
who at least had identifyable individuals or institutions as signers.

We have to go one more step back, to see where that attack originates
in the threat model: it's a man in the middle (MITM) attack to prevent
the proper spread-out of the current block.  A MITM attack to a P2P
network.  Preventing MITM attacks has other attempts to solve them,
either, even trust on first use (TOFU) does a descent job.  And that's
likely the explanation, why even AltCoins with very little work didn't
get hacked on that part.

Any sane secure peer to peer network ought to have something better
than nothing: TOFU or PKIs that improve trust.  And that a full-blown
PKI takes away the anonymity is not a problem: A big warehouse of
ASICs to mine BitCoins also completely blows the anonymity of the
miner.  The miner or signer doesn't need anonymity; the parties that
actually exchange coins are the ones who want anonymity.

So to secure the BlockChain requires two things: First, the operators
of full nodes (those who take and validate transactions) need to have
known keys, so you can connect to them without MITM attacks.

Second, you can just make sure you have enough sanctions and auditable
signers of these blocks.  The block chain with the highest amount of
trust wins.  How do you **measure** trust?  Can you enumerate trust?
Reliable signers have signed many blocks.  Game theory shows that
repeated collaborative interactions are more rewarding than cheating,
which can be punished with long-term effects.  Also: the more signers
you have, the better.  Verified signers are better than anonymous
signers, because anonymous signers can be a sybill attack.

To avoid intruders re-signing older blocks, rotate signature subkeys each
round.  A well-behaving signer will lose the old key each round (just keep it
long enough so that the commitment of the new key in the next block is
confirmed), and therefore is unable to tamper old blocks; similar to ephemeral
encryption, where you are unable to decrypt the traffic yourself later.

You can have a proof of work to prevent sybill attacks, e.g. mandating that to
enter the trust ring, you need to have a key with a certain prefix.  That
would be one-off work, because then you want to stay there with that identity,
and accumulate more trust by signing in consensus.  It just creates an entry
barrier and avoids DDoSing the ledgers with applications for participation.

Of course, every transaction within the block ought to include the
previous block's hash as starting key for the hash calculation, so
that they contribute to the unchangeable chain, and can't be moved to
any other fake chain (they won't verify there).  Furthermore, each
transaction (despite being anonymous) adds to the trust value: more
transactions in one block means that it is more trustworthy, because
more people found its way to this branch of BlockChain reality.

Note that the partitioned BlockChain below makes it far more expensive
to fake a chain: You need to generate signatures and activities in all
of them; the fake activities you generate are with the coins you own;
you have no others.  Sanctions for misbehavoir can make sure these
coins are lost; the fake chain is the proof of misbehavior.

Those additional coins could be used to compensate for the loss of the
victim of double spending.

## Where to hijack the proof of work BlockChain

Let's assume we can attack BitCoins block chain: Where would we attack it?  At
the end, which allows us to do double spending of the coins we own?  Who would
do that?  Probably someone with a lot of coins inside, so proof of stake is a
bad idea (especially, since that allows you to spend the same coin not just
twice, but many times; even if you lose the stake in question, it's still a
big win).

Or attack it at the front, where most coins have not yet been mined,
and by producing a fake fork of all the transactions afterwards, you
could turn over all the coins in the entire BitCoin universe to you.
All you need is enough power to calculate a full chain considerably
faster than the miners.

Is that viable?  It won't go in undetected, but since the early mining
was more profitable in numbers of coins, and far easier (because the
difficulty was much less than today), it's technically not that hard.
And “longest chain” is not sufficient to defend that attack: It needs
to be the chain with most work involved in.  The fake chain could be
one where the adjustment for the difficulty is set too low.

BitCoin addresses that, the chain length is the sum of the
difficulties.  But the problem remains: Let's say China confiscates
the ASIC miner's equipment, which will result in a significantly
reduced difficulty in the rest of the world's BlockChain.  And then it
uses the confiscated equipment to construct a chain that has more
difficulty in it than the entire chain from the rest of the world — it
might take a year or two, but it's doable.

And then it busts the entire BitCoin ledger by releasing that chain, which
essentially has only unspendable coins inside (coins owned by the Chinese),
because in that revision of history, they were all mined by someone else.

You still need to spend more effort on that as the miners spend, but
you then own all the cheap, easy to earn early coins.

But in fact the by far easiest hijack is to create a slightly incompatible
protocol.  This is deliberately splitting the network, out in the open, with
effectively not much work required, and this allows to double-spend, even
though the BitCoin fork doesn't have the same price. But the price is not the
point: The point is the promise of the unique asset.  Just think of real
estate in the BlockChain.  By having forks, BitCoin shows that it can only
fulfill that within a consent of the protocol, and that's actually outside the
chain itself.

So the executable protocol spec, the code for checking a block for
validity itself should be part of the chain, and only updated in
consensus.  And any transaction need to link to the protocol block,
and if a transaction is found that links to a not accepted protocol
block, it will cause a quarantine of the corresponding coin.  That
means you get punished for spending it in the fork.

It needs to be done in a way to keep the balance.
