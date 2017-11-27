# The $quid – CryptoCurrency and BlockChain

![Squid](../doc/squid-logo.png)

## Abstract

*10 years after BitCoins “whitepaper”, the BlockChain and crypto
currencies are a big hype. Time to look at the results of the
experiment, see what failed and what works, check the consequences for
society, and propose improvements.*

*BitCoin's technology has three problems which need to be fixed:*

  + *The unfair distribution of coins*
  + *The energy consumption of proof of work*
  + *The non-scaleable replicated, but not partitioned ledger*

The lightning network tries to address the last point, by doing transactions
off-chain.

## Introduction

I figured out that the user layer of my net2o peer to peer protocol
needs some sort of asset transaction protocol (“cryptocurrency”), and
when secure protocols meet money, it's not just technology that
matters. This paper will therefore take both sides into consideration.

Here's the bingo chart for all valid critics on BitCoins:

<table>
<tr>
<th>BitCoin</th>
<td>doesn't scale</td>
<td>is a snowball system</td>
<td>will be destroyed by China</td>
<td>is a plot to destoy capitalism</td>
<td>is used by drug dealers and terrorists</td>
</tr>
<tr>
<th>Bubble</th>
<td>busts next week</td>
<td>already is busted</td>
<td>inflated too much</td>
<td>deflated too much</td>
<td>is obviously a bubble</td>
</tr>
<tr>
<th></th>
<td>BitCoin is deflationary</td>
<td>The BlockChain is too big</td>
<td class="joker">Tulipmania!</td>
<td>blocks are too big</td>
<td>blocks are too small</td>
</tr>
<tr>
<th>BitCoin is</th>
<td>unregulated</td>
<td>illegal</td>
<td>only an experiment and now ended</td>
<td>worse than Fiat because not backed by material things</td>
<td>worse than my fork</td>
</tr>
<tr>
<th></th>
<td>Too many digits behind the point</td>
<td>Too few dicits behind the point</td>
<td>BitCoin is not anonymous</td>
<td>BitCoin is used for money laundering</td>
<td>Told you so!</td>
</tr>
</table>

The ideology behind BitCoins certainly is that of a libertarian
cypherpunk, with no trust in the state, but a lot of trust in anarchic
communities.  With fear of inflation, offer the other disease of
currencies, deflation, as “solution”.  Plague is not a solution to
cholera, either.

BTW: BlockChains cure cancer, protect you from evil eye, prevent
flood, plague (though not cholera), and drought, and they can walk
over water (even when not frozen).

## Purpose of a currency (with history lesson)

_Where does the need for money come from?  And what's money?_

We'll mostly look into Chinese history, because all the important
inventions in currencies happened there.  Except maybe the cowry shell
money, but we don't know.

### Terms

  + Commodity money: Objects with inherent value used as money
  + Representative money: Note promising exchange with objects with
    inherent values used as money (also: promissory notes)
  + Fiat money: Medium with no inherent value and no promise for
    exchange with such an object used as money
  + Legal tender: Medium of payment by law, can be any of the above

People tend to confuse legal tender with fiat money, because nobody
would accept a fiat money if it's not a legal tender.  Or would you?
So the common English definition of “fiat money” implies that it is a
legal tender.

### Early forms of money

Stone age society started with share economy: When you had something
in abundance, you'd share it with your larger family, the small tribe
you were associated with.  You'd expect the other members to share
their prey or whatever they had to share with you, too.  You can do
that, because there's trust and established relationship.

When society grew larger, this no longer worked.  You would trade
things, e.g. meat vs. plants or tools.  The obvious problem with that
is: you need to find someone who has a need for your thing and at the
same time provides something you have a need for.

That could be solved by exchanging tokens that everybody considered
valuable, that were easy to carry, but rare enough nobody could get
them too easily: That is the birth of money.  The first known money
mankind used (earliest at least 30000 years ago) was “shell money” or
“cowry money”, shells of maritime snails (in Chinese: 貝/贝 “bei”,
still means “money” as well as “cowry”).  They were picked up at the
shore, so you had to invest a certain amount of work to get them.

Shell money was the more valuable the further away from the shore, so
when metallurgy was invented, the people from the mountain came up
with shiny metal coins as hard to get but easy to carry and
longlasting valuables, creating silver and gold standards for
currencies.  Both shells and noble metals can serve as jewellery, so
it's not completely useless, but the main purpose for money is a proxy
for exchanged values, so you can exchange the things you have and the
things you need with different persons.  The money itself decouples
the individual acts of trade.

The work to get the shell or the coin is only invested once, but the
coin remains an interchangeable object of value, it can be traded many
times.

### Banknotes

These scarce tokens were chosen to ensure that you need a certain
amount of work to gather them, so when your time has a value, you
wouldn't waste too much on collecting shells or mining gold.  The
problem is: these things have an upper limit of availability (even if
more people collect or mine them), and when society and economy grows,
you run out of tokens, and grafting more just for the sake to haven
enough coins is a waste of ressources: You'd rather spend your time
with the actual real products, instead of running after the next gold
rush.

This leads to deflation, and in deflation, people tend to hoard
their money (spending it tomorrow gets you more than spending today),
which creates an economic downcycle, and deflation gets worse.  If it
was just lack of work to get the tokens, society would ramp up
collecting shells and mining noble metals, but if there is a natural
limit to get more shells or gold, you need another idea.

The first early forms of banknotes were financial products, the
promissory note.  Instead of actually paying, you just gave the
promise.  That allows to inflate the amount of available money to keep
business going, because now almost any valuable, even real estate,
could be converted into easy to carry paper.  Song dynasty China
e.g. had a booming economy (with an estimated 200 billion coins
minted), and the availability of cash to exchange the vast amount of
traded goods was always a problem, even with this large amount of
minted coins.

The earliest form of banknotes were check-like promissory notes,
issued by banks, which promised coins in exchange; the notes themself
(飞钱, feiqian, flying money) were very popular due to the shortage of
actual coins.  In the late Tang dynasty the first such notes appeared,
and became quickly popular.

![feiqian](http://lishi.dugoogle.com/wp-content/uploads/2015/04/48c3b774accb494a80636253fc950dd2.jpg)

These notes were produced and cashed in by banks.  In 1024, the first
state-issued feiqians were issued, and in the 12th century (in the
southern Song dynasty), the 会子(huizi) was established as legal
tender.

![Huizi](https://upload.wikimedia.org/wikipedia/commons/6/6a/Hui_zi.jpg)

Song dynasty banknotes had similar properties to gold standard
dollars, they could be turned into metal coins.  And of course,
forgery became an issue, because the whole point of paper money is to
make it quick and easy to produce, and regulate the amount of money in
circulation by authoritive decisions, not by how much work was
available for doing that job.  The money got stamped with seals which
were difficult to make, but could be used for many notes, and can be
kept under control.  That is a proof of ownership and authority: You
can issue a note if you prove that you own the seal.  The seal owner
is not anonymous, it's a government clerk.

The Song dynasty's huizi was somehow experimental, and took care to
base the paper money's value on bronze (northern Song) and silver
(southern Song), with bronze price fluctuating a lot in southern Song.

The following mongolian Yuan dynasty took the concept further to the
first fiat money.  They didn't allow private ownership of silver and
gold (compare: Roosevelt's America after the great depression), so the
banknote was not a promise to give you noble metal.  Inflation was a
problem in both the Yuan dynasty and the early Ming dynasty following,
which stopped issuing banknotes in 1450; the circulating notes however
were still accepted — and the currency system was privatized,
i.e. they went back to the feiqian promissory notes.

Thanks to Marco Polo, Europe soon got a pretty good description of how
it worked, because Marco Polo as merchant was interested in this kind
of things.  One reason why the banknote didn't catch on at that time
was that neither paper nor a sufficiently good printing press were
invented here.  In the western world, banknotes were usually
exchangeable with noble metal coins until 1970, when Bretton Woods
broke.

However, most contemporary money transactions are no longer cash,
neither coins nor banknotes, they are “fiat money”, money that's only
in the books, and only has a promise to be exchanged with banknotes,
and the notes are not a promise to get you noble metal.

The act of creating “fiat money” is an act of state, the central bank
lends money to other banks, money it doesn't actually have, and when
it gets it back, it is removed from circulation.  This is something
that seems to be pretty difficult to understand, because the concept
of money used to be valuable tangible things, not numbers in an
electronic ledger.  And that it is a political decision how much money
to create or let disappear is of concern to those who don't trust
politicians — even if the lawmaker only sets rules for the conditions
to create and annihilate book money, it is unclear to many people why
these rules make sense.  A lot of early fiat money experiments ended
in hyperinflation, and a return to some noble metal standard.

However, the lessons were learned, and today, most fiat currencies are
pretty stable, and survive even major disruptions of the economy.
Money in the information age is just an entry in an electronic ledger.
It's backed by the economy, and it's availability is adjusted to keep
it pretty stable.

But the only security these ledgers have nowadays is that it require
physical access to the computers that hold it.  And we know how secure
computers are.  The idea of a crypto currency therefore is to provide
security of these ledgers through mathematical magic.

## Proof of What?

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
skales with the amount of available work, and that inflates with the price per
coin.

That means that the lightning network, while improving scalability, does not
cut down the hidden transaction costs: the liquidity required for the
lightning network will drive the price per coin up, so that mining is so
lucrative that miners will buy more ASICs and burn more coal in turn.

### What is a BlockChain?

We need an actual definition; technically, even a git repository has
some important properties of a BlockChain.  The chain of hashed blocks
is one aspect, the consensus algorithm the other:

  + Merkle-tree or equivalent hash-it-all approach (loose definition)
  + no single point of trust
  + consensus algorithm based on the contents only (no external arbiter)

### How to cheaply secure the BlockChain

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
repeated collaorative interactions are more rewarding than cheating,
which can be punished with long-term effects.  Also: the more signers
you have, the better.  Verified signers are better than anonymous
signers, because anonymous signers can be a sybill attack.

To avoid intruders re-signing older blocks, rotate signatures
frequently.  net2o's [key revokation](key-revocation.md) allows doing
that without losing trust, and allows it to be done outside the
signing machine.  All trust anchors must be part of the chain, with
the root anchor as a-priory knowledge.

You can have a proof of work to prevent sybill attacks, e.g. mandating
that to enter the trust ring, you need to have a key with a certain
prefix.  That would be one-off work, because then you want to stay
there with that identity, and accumulate more trust by signing in
consensus.  It just creates an entry barrier.

Of course, every transaction within the block ought to include the
previous block's hash as starting key for the hash calculation, so
that they contribute to the unchangeable chain, and can't be moved to
any other fake chain (they won't verify there).  Furthermore, each
transaction (despite anonymous) adds to the trust value: more
transactions in one block means that it is more trustworthy, because
more people found its way to this branch of BlockChain reality.

Note that the partitioned BlockChain below makes it far more expensive
to fake a chain: You need to generate signatures and activities in all
of them; the fake activities you generate are with the coins you own;
you have no others.  Sanctions for misbehavoir can make sure these
coins are lost; the fake chain is the proof of misbehavior.

Those additional coins could be used to compensate for the loss of the
victim of double spending.

### Where to hijack the proof of work BlockChain

Let's assume we can attack BitCoins block chain: Where would we attack
it?  At the end, which allows us to do double spending of the coins we
own?  Who would do that?  Probably someone with a lot of coins inside,
so proof of stake is a bad idea (unless you make a rule that whoever
gets caught double spending loses his stake).

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
one where the adjustment for the difficulty is set to low.

BitCoin addresses that, the chain length is the sum of the
difficulties.  But the problem remains: Let's say China confiscates
the ASIC miner's equipment, which will result in a significantly
reduced difficulty in the rest of the world's BlockChain.  And then it
uses the confiscated equipment to construct a chain that has more
difficulty in it than the entire chain from the rest of the world — it
might take a year or two, but it's doable.

And then it busts the entire BitCoin ledger by releasing that chain,
which essentially has only unspendable coins inside, because in that
revision of history, they were all mined by someone else.

You still need to spend more effort on that as the miners spend, but
you then own all the cheap, easy to earn early coins.

But in fact the by far easiest hijack is to create a slightly
incompatible protocol.  This is deliberately splitting the network,
out in the open, with effectively not much work required, and this
allows to double-spend, even though the BitCoin fork doesn't have the
same price. But the price is not the point: The point is the promise
of the unique asset.  By having forks, BitCoin shows that it can only
fulfill that within a consent of the protocol, and that's actually
outside the chain itself.

So the executable protocol spec, the code for checking a block for
validity itself should be part of the chain, and only updated in
consensus.  And any transaction need to link to the protocol block,
and if a transaction is found that links to a not accepted protocol
block, it will cause a quarantine of the corresponding coin.  That
means you get punished for spending it in the fork.

It needs to be done in a way to keep the balance.

## Money and wealth — Society in a deflationary world

How money shapes a society, and why the limited supply of BitCoins is
far worse than neoliberalism.

### Speculation object

Is BitCoin's price a bubble?  Who could have that strange idea?

![Stages in a bubble](https://people.hofstra.edu/geotrans/eng/ch7en/conc7en/img/stages_bubble.png)

Mammals like to gamble.  The reason for why gambling is so rewarding
has been found by B. F. Skinner.  Erratic, random reward reinforces
behavior better than constant, predictable.  Humans don't show any
more meta-insights than mice in this respect.

The pattern of gambling with speculation objects are remarkable
similar.  Even smart persons like Isaac Newton lost a fortune in
bursting bubbles.  The madness of humans however can be calculated,
though certainly not to the finest details.  The BitCoin chart however
looks like a classic:

![BitCoin Chart](https://assets.bwbx.io/images/users/iqjWHBFdfxIU/iFKLTQNYjslA/v4/800x-1.png)

Speculation bubbles don't mean the money disappears.  The money is
still there, it's just owned by someone else now; only if you include
the market cap of the speculation object, it disappeared.  Despite
that the total amount of money is constant through a speculation
bubble, they have a deep impact on economy.

That's because those who entered the bubble early and sold at the
right time got the money, and those who entered late lost.  The
tragedy is that the early ones are smart and rare, and the late ones
are stupid and many.  And that means a huge concentration in money,
and especially a loss for those who didn't really have that money,
causing them to default, and sink into poverty.  Speculation
inevitably is a move from the crowd to the few who behaved
differently, not necessarily smart; a counter-action to the crowd can
improve your chances of winning.  You shouldn't go to “buy high, sell
low”, though.

### BitCoin is Reactionary

But let's assume BitCoin is not a bubble going bust, but it's supposed
to last (all 21 millions), and used as currency for everything by
everybody.  That means the price per coin would soar until the total
market cap equals the wealth of the entire planet (plus hot air for
the first movers).  BitCoin's wealth allocation is that of
colonization or gold rushs: the first to arrive are the ones who get
the by far largest claims.

And that would mean all those people who were there in the early phase
would be incredible rich, just by being early.  Instead of creating
most coins in the last, big growth phase, BitCoin created them in the
early phase, where it was essentially worthless and no effort needed
to mine the coins.

So maybe that wealth is going to trickle down?  Trickle down however
doesn't work.  Money inherently trickles up.  In a deflationary
system, there is no decay of money; instead the money of the wealthy
becomes more and more just on its own.  They don't even have to invest
it to gain wealth.  The result will be an increasingly absurd
distribution of wealth, and all the assorted problems: economic power
leads to political power, corruption, tax cuts for the rich,
slavery-like work relationships, because only the rich can afford
hiring people; the poor can't.  The instability of deflationary money
leads to speculation bubbles, which burst, and this instability moves
the money even faster to the first movers, who don't lose anything in
the downcycles.

That's not what I think as future I want.  Fiat money has its
downsides, too; but mostly when the state that controls it is in deep
troubles, and fails to create or establish rules that work for the
greater good.

### Driving forces to servitude

Historical societies often had driven most of the population into one
or the other form of servitude.  Ownership of production means,
i.e. land in agricultural societies, factories in industrial
societies, and a strict copyright regime in a knowledge society are
means to give power to few and exploit the many.

Sometimes, the concentration of power is a natural consequence,
e.g. for industrial production, economy of scale results in
concentration of wealth and power.  Or, in medieval Europe, the choice
was either become a knight and serve the king (and that includes the
risk of being killed in the next war), or become a serf, and thus
being owned by the local knight.  Even when servitude was abolished, a
similar relation between gentry and tenants were common, and the
gentry makes sure that their political influence works to stabilize
their properties.

Fiat money requires an intermediary (banks) to distribute the money
handed out by the central bank; this gives a lot of power to the
banks.  Crypto currencies, which don't require banks, could lift that
bane, but they way they are constructed now, they just create a new,
worse one.

And finally, in the age of information, copyright and concepts like
“work for hire” (i.e. the copyright is handed over as part of an
employment contract) concentrate the available knowledge in the hand
of the few.  Since Bill Gates, the super-rich all base their wealth on
author's rights of things they actually didn't author.

### The (in)stability of spending money

When trade happens in an inflationary system, people buy quick,
because money is the hot potato.  That by itself increases the amount
of money in circulation, because there are more hot potato
transactions (money spent per time increases).  More demand increases
the prices, so the inflation accellerates.

If you don't print new money, the inflation will inevitable stop: The
increased prices now match the increased turnaround speed, and
inherent delays stop the speed to increase even more.  So people will
stop spending their money early, and return to normal (and first
consume the stocked goods).  That causes deflation, so people will
rather keep their money, instead of spending it, waiting for the price
to go down further.  Low demand causes the prices to go lower, indeed.
If you don't take out money of the system, the lower circulation speed
will find a stable point, too.

We know these periodic oscillations of prices in many parts of the
economy, they are a consequence of a badly regulated closed loop
system; they tend to oscillate if not properly compensated.  If the
compensation is very bad, the oscillation is high, and annoying
everybody.  Big osciallation is shifting wealth from the poor (who
can't survive the downturns) to the rich (who can).

To remove such oscillation by proper compensation is just solid
engineering.  We don't want pork cycles.

### Why a BlockChain?

So when I'm ok with fiat money (at least with the big ones where the
regulation works), and would use the BlockChain only for trading with
valueables created outside, like fiat money or real estate, then why
not use a system like [GNU Taler](https://taler.net/), which hands
over the job of checking the double spending to the banks?

Well, seems to be a good idea, but it turned out that the banks have
deep troubles with trust, either.

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
the benefits of modern distributed databases.  Doing the partitioning
off-chain is sidestepping the problem instead of solving it.  Note that the
lightning network hints at possible solutions, without being one.

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
(transaction fees left out).

These are the operations you want to perform in the ledger:

  1. You want to move a coin from one owner to another (may imply
  ledger change)

  2. You want to be able to join several coins into one

  3. You want to split a coin

I don't want to have complex multi-source and -sink transaction which
are difficult to verify.  Instead, I propose a single transaction,
which takes a part of one coin, and adds it up to the destination coin.

![Coin transaction](https://upload.wikimedia.org/wikipedia/commons/thumb/c/ca/Feynmandiagram.svg/220px-Feynmandiagram.svg.png)

I represent this transaction as Feynman diagram, because that almost fits the
concept.  It the source coin (or account) results in a value of 0, it can
disappear, if the destination coin doesn't yet exist, it starts with the
transaction as initial value.

Coins are pseudonymous, and you can have many different pseudonyms.  However,
coins as proposed here have readable values attached.  It's likely that mix
services will be offered for gaining anonymity.  You transfer to one coin of
the mix service, and you get back several coins from other coins of that same
mix service, and you split and merge them accordingly when you want to do your
next transaction.

There is no need for a nonce, the key as id of the owner is
sufficient.  The check of the balance is sufficient to prevent any
abuse.

### Double entry bookkeeping system

First of all, if you are somehow familiar with bookkeeping, “a ledger”
has something fundamentally wrong in it: The singular.  You are
supposed to have more than one ledger, and every transaction needs to
go to two columns, one as debit, one as credit.  All values are outside the
accountant's hands, so the accountant's balance is always zero (poor gal).

If you want to scale a crypto currency, you want to separate ledgers
by some arbitrary criteria so that the individual nodes are not
overloaded, and millions of bookings can get in per second; which is a
fairly reasonable number for micropayment.

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

### Subdividing further

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

In order to allow routing destinations, ledgers take responsibility of
out-group destinations for one cycle: If the current mode is local,
the ledger is responsible that can move the transaction to the correct
foreign group in the next cycle.  If the current mode is global, the
ledger is responsible that can move the transaction to the correct
local ledger in the next cycle.  So after two further cycles, the
transaction has reached its destination.  All the transactions satisfy
the balance rules, the better scalability only comes at the cost of
more cycles to complete a transaction.  But better scalability means
that the cycle time can be shorter.  If you settle down to a fixed
group size, and let the numbers of groups grow appropriate, it's an
_O(log n)_ algorithm.

I call it the SwapDragonChain, as the swap dragon is the mascott of
Forth (the SWAP operation).  I'm sure the swap dragon can handle
double booking quite well with his two heads.

Note that unlike the lightning network, there is no need to worry about the
transactions: they are all public.  Nothing can be lost or stolen.  The
topology is derived from the pubkey values, and not from randomly connected
peers.  The full nodes who want to be responsible for a certain part of the
ledgers know where to connect to.

### Zero-Knowledge Proof?

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

## The $quid: Useful Investment

_What valuable to base a crypto currency on? Or what is a worse crime
than robbing a bank?  What's worse than inventing a crypto currency?_

People who defend BitCoins against the “it's a bubble” argument told
me they think of this (and of ICOs of other crypto currency projects)
as investment into a technology.  I doubt that Satoshi Nakamoto will
actually sell his coins, so it doesn't quite work, but I'm fine with
the idea of investment as such.

I therefore propose a useful speculation object for humans who like
the proof of work concept to back a currency (not to back the security
of the transactions!) that is in the collection of cowry shells and
the mining of gold, or in good rules for fiat money, which are also
backed by big, real economies, and the real work that happens there.
The common idea between these concepts is that they are valuable,
because it is hard work to obtain them.  And once you have them, you
can exchange them for other goods that are equally hard work, and they
retain their value.

But first, I want to explain the name: $quid a combination of the $
symbol (pronounced simply “S” here), and the word quid.  Quid is a
word for a metal-backed currency, the pound sterling (240 pennies of
silver of sterling quality are a imperial pound).  But it also is the
first word in “quid pro quo”, a very important concept in society, and
the foundation why trade actually works.  It's about cooperative
behavior even when the persons participating are egoists, forced by
game theory to be cooperative.  But after all, “quid” on its own just
means “what?“, a question always valid to ask.

One thing we have in society that lacks a bit quid pro quo is free
software development.  You give, people take, most of them without
giving back.  Developers participating in free software development
take and give back, and that's why we do it: We all stand on the
shoulders of others.  Even if you scroll through the licenses of a
proprietary OS like iOS, you see an amazing amount of free software
that has been used there.  The value of free software is incredible.

So how do you mine $quids?  You create useful free software, and then
you get the right to issue $quids.  It's up to organizations like the
Linux Foundation or the GNU project to figure out who qualifies and if
the amount of work that allegedly went into the project is plausible,
this supervision will avoid fraud; but the market liberals probably
would suggest that a bidding system like for ICOs (initial coin
offerings) is completely sufficient.  You could have a combination of
both: Bidding and evaluation plus recommendation from trustworthy
organizations.  Of course, like BitCoin, the $quid is an experiment,
and the number of $quids that can go onto that market depend on the
acceptance.  If the acceptance is high, many projects will ask for
funding, if it's low, few, so the value of the $quid is
self-stabilizing.

A bidding system would work like this: _n_ $quids are offered.  When
you buy _m,_ you also offer a factor _x>1._ The buyers are sorted by
factor, and the highest bidder gets his _x*m_ $quids, paying _x*m_ the
price of a nominal $quid, but that deduces just _m_ from the offered
_n,_ so more $quids are generated from higher biddings.  High bidding
thus makes other projects want to be funded through that channel, so
it increases the amount of coins quickly.  Low bidding makes that
funding channel unattractive.

The difference between normal sponsorship and this approach is that
while the issuers of $quids get paid for their work, the people who
pay can trade the $quid, like investors in corporations can sell
stocks.  And while the effort of a corporation to develop proprieatary
software is ultimately lost to humanity, and the pay-back for the
investors is through profits, the effort of free software developers
is not lost; it can be shared and it can be used to improve and base
upon; so paying them and still having the $quid as currency to trade
is a fair deal: Society as a whole got richer through the creation of
free software, so increasing the amount of money in circulation is ok.
I view free software as infrastructure, and investment in
infrastructure pays off for the entire economy.  Increasing the amount
of liquidity therefore is a good way to finance infrastructure.

Free software is a non-rivalrous good.  You can copy it as much as you
like, you can change it and fit it to your purpose (supposed it is
constructed lean enough, and you have the qualification for doing
that), so it's not reasonable to trade it on its own.  The whole
concept behind copyright to turn knowledge and wisdom into rivalrous
goods is a bad idea.  To society, they are much more worth when they
are non-rivalrous.

So therefore I propose to turn free software development effort, which
is a scarce resource, and creates knowledge (public infrastructure of
the information age), into a tradeable currency, and thereby make this
work valuable.  Instead of financing startups, which are classical
economy, the intial $quid offer goes into work for the public.

What's missing for a real fiat money: There's no way to actually
remove coins out of the system to reduce the money supply.  The
authorities (and the market) that grant projects with funding can only
create money, not take it out of the system.  But a coin system works
the same way: you can mint coins, and they are going to last.

_Ah, yes, and penguins eat squids, too._

### Share and enjoy!

I want to point out that a service-oriented business model for free
software works, but inevitably pushes the companies into a business
model, where only the complaint department makes money.  If you don't
want to stick your head into a pig, because the depressive AI of the
useless Sirius Cypernetics Corporation products you got, make sure
that there is a business model for creating good, easy to use, and
cheap to maintain software.

Incentives matter.  Economy is all about game theory.

## Literature

  1. [Stages in a bubble](https://people.hofstra.edu/geotrans/eng/ch7en/conc7en/stages_in_a_bubble.html)
  2. [Historic Stock Market Crashes, Bubbles & Financial Crises](http://www.thebubblebubble.com/historic-crashes/)
  3. [Extraordinary Popular Delusions and the Madness of Crowds (1841)](https://en.wikipedia.org/wiki/Extraordinary_Popular_Delusions_and_the_Madness_of_Crowds)
  4. [Online resource for double entry bookkeeping](http://www.double-entry-bookkeeping.com/double-entry-bookkeeping-tutorial/)
  5. [Distributed atomic transactions](https://www.cockroachlabs.com/blog/how-cockroachdb-distributes-atomic-transactions/)
  6. [Adam Ludwin explains crypto currencies](https://blog.chain.com/a-letter-to-jamie-dimon-de89d417cb80)
  7. [Bangladesh Bank Robbery](https://en.wikipedia.org/wiki/Bangladesh_Bank_robbery)
  8. [Flying Money](http://www.baike.com/wiki/%E9%A3%9E%E9%92%B1)
  9. [Is a git repository a BlockChain](https://medium.com/@shemnon/is-a-git-repository-a-blockchain-35cb1cd2c491)
  10. [Is my blockchain a blockchain](https://gist.github.com/joepie91/e49d2bdc9dfec4adc9da8a8434fd029b)
  11. [Der „Wolf of Wall Street“ warnt vor ICOs](http://www.handelsblatt.com/finanzen/maerkte/devisen-rohstoffe/hype-um-krypto-boersengaenge-der-wolf-of-wall-street-warnt-vor-icos/20490646.html)
  12. [„Warum BitCoin (jetzt aber wirklich!) TOT ist“-Kwizz](https://www.heise.de/forum/heise-online/News-Kommentare/Bitcoin-klettert-auf-ueber-7000-US-Dollar/Das-monatliche-Warum-Bitcoin-jetzt-aber-wirklich-TOT-ist-Kwizz/posting-31300715/show/)
  13. [Bitcoin Mining Electricy Consumption](https://motherboard.vice.com/en_us/article/ywbbpm/bitcoin-mining-electricity-consumption-ethereum-energy-climate-change
)
  14. [BitCoin Stromverbrauch Energie](http://t3n.de/news/bitcoin-stromverbrauch-energie-872715/)
  15. [BitCoin Energy Consumption Index](https://digiconomist.net/bitcoin-energy-consumption)
  16. [BitCoin is reactionary](http://www.ianwelsh.net/bitcoin-is-a-bad-way-to-do-something-necessary/)
  17. [Skinner box](https://en.wikipedia.org/wiki/Operant_conditioning_chamber)
  18. [BitCoin bubble from 2013](https://www.forbes.com/sites/jessecolombo/2013/12/19/bitcoin-may-be-following-this-classic-bubble-stages-chart/#61f0969d36b8)
  19. [760,000 transactions per second](http://shanghaiist.com/2017/01/31/14-billion-virtual-hongbao.php)
  20. [Math proof of why lightning network won't work](https://medium.com/@jonaldfyookball/mathematical-proof-that-the-lightning-network-cannot-be-a-decentralized-bitcoin-scaling-solution-1b8147650800)
