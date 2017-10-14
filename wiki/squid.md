# The $quid – CryptoCurrency and BlockChain

![Squid](https://fossil.net2o.de/net2o/doc/trunk/doc/net2o-squid-200.png)

## Abstract

10 years after BitCoins original document, the BlockChain and crypto
currencies are a big hype. Time to look at the results of the
experiment, see what failed and what works, check the consequences for
society, and propose improvements.

## Introduction

I figured out that the user layer of my net2o peer to peer protocol
needs some sort of contract/currency transaction protocol, and when
secure protocols meet money, it's not just technology that
matters. This paper therefore will therefore take both sides into
consideration.

Here's the bullshit bingo chart for all valid critics on BitCoins:

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
<th>the bubble</th>
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
<td>Joker</td>
<td>blocks are too big</td>
<td>blocks are too small</td>
</tr>
<tr>
<th>BitCoin</th>
<td>is unregulated</td>
<td>is illeal</td>
<td>is only an experiment and now ended</td>
<td>is worse than Fiat because not backed by material things</td>
<td>is worse than my fork</td>
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

## Purpose of a currency (with history lesson)

### Trade vs. speculation object

Where does the need for money come from?

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
mankind used was “shell money” (in Chinese: 貝/贝 still means
“money”), shells of maritime snails.  They were picked up at the
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

### Paper money

These scarce tokens were chosen to ensure that you need a certain
amount of work to gather them, so when your time has a value, you
wouldn't waste too much on collecting shells or mining gold.  The
problem is: these things have a limited availability, and when society
and economy grows, you run out of tokens.  This leads to deflation,
and in deflation, people tend to hord their money (spending it
tomorrow gets you more than spending today), which is instable, and
deflation gets worse.  If it was just lack of work to get the tokens,
society would ramp up collecting shells and mining noble metals, but
if there is a natural limit to get more shells or gold, you need
another idea.

The first early forms of banknote were financial products, the
promissory note.  Instead of actually paying, you just gave the
promise.  That allows to inflate the amount of available money to keep
business going.  Song dynasty China e.g. had a thriving economy, and
the social reforms of Wang Anshi allowed even small farmers to take
credits and buy the land they were using.  That however comes with a
problem of trust: You have to trust that promise.

Eventually, in the 12th century, the first state took over the role of
the trustee, and China's Song dynasty created the first legal tender
as a banknote, the 会子 (huizi, contemporary Chinese means “a while”),
which still is a promissory note, but now issued by a much larger and
more powerful entity, operating under the rule of law, and therefore
easier to trust.  And the state definitely in power to force you to
use the currency, so if you don't trust, you still have to obey.

Of course, the issue that came up in Song dynasty China quickly was
forgery, because the whole point of paper money is to make it quick
and easy to produce, and regulate the amount of money in circulation
by authoritive decisions, not by how much work was available for doing
that job.  The money got stamped with seals which were difficult to
make, but could be used for many notes, and can be kept under control.
That is a proof of ownership and authority: You can issue a note if
you proof that you own the seal.

The result of using such a legal tender was a boom of trade and
economy; and at the same time, the relation between the silver price
increased by a factor 100 within a few hundred years.  It should be
noted that since the end of Bretton Woods and the gold standard of the
USD in 1971, the gold price inflated by a factor of 50, and that is
not the same as the inflation rate.

The Song dynasty's huizi was somehow experimental, the following Yuan
dynasty kept the concept, and expanded it a lot.  Tanks to Marco Polo,
Europe soon got a pretty good description of how it worked, because
Marco Polo as merchant was interested in this kind of things.  One
reason why the banknote didn't catch on at that time was that neither
paper nor a sufficiently good printing press were invented here.  In
the western world, banknotes were usually exchangeable with noble
metal coins until 1970, when Bretton Woods broke.

However, most money transactions in the meantime are no longer cash,
neither coins nor banknotes, they are “fiat money”, money that's only
in the books, and only has a promise to be exchanged with banknotes,
which were promises to buy gold some time ago (but no longer are).

The act of creating “fiat money” is an act of state, the central bank
lends money to other banks, money it doesn't actually have, and when
it gets it back, it is removed from circulation.  This is something
that seems to be pretty difficult to understand, because the concept
of money used to be valuable tangible things, not numbers in an
electronic ledger.  And that it is a political decision how much money
to create or let disappear is of concern to those who don't trust
politicians — even if the lawmaker only sets rules for the conditions
to create and annihilate book money, it is unclear to many people why
these rules make sense.

And the only security these books have nowadays is that it require
physical access to the computers that hold it.  And we know how secure
computers are.  The idea of a crypto currency therefore is to provide
security of these books through mathematical magic.

## Proof of What?

It is pretty obvious that BitCoin went back all the way through human
money history, and uses the same principle shell money had to give
value to a coin: the miners do a proof of work.  Like the shells, the
amount is limited, and the difficulty raises with the amount of work
available.  The limited amount of mineable BitCoins mimics another
property of shell money, and with a quickly growing economy that
actually uses cryptocurrencies, it becomes a quickly deflating
currency.  That makes it ideal as speculation object, and very bad as
trade token.

There is a disastrous side effect to this: Mining burns energy, ASICs
are developed and manufactured, if you try to avoid ASIC mining, all
available cost-efficient GPUs are bought and gamers are frustated,
because they simply can't get hold of them.  This is far worse to
hoarding gold, because nobody actually needs gold anywhere near the
available quantities used as money.

And all that big amount of work is converted into very few actual
transactions.  BitCoin exchanges pop up that allow people to trade
with them without actually making transactions in the BitCoin
protocol; and without those transactions, that money is not secured.
Virtual bankrobbery occurs, and it shows that people who don't know
history are doomed to repeat it: Regulation is there for good reasons.

### How to cheaply secure the BlockChain

So let's take a step back, and look at what's the point of the proof
of work: The basic idea is that of securing the BlockChain against an
attack.  The BlockChain itself is immutable if you have access to the
last signed block: Through a link of hashes, every other block before
can't be changed without changing the last block, too.  The problem
is: how do you know it's _the_ valid last block?

BitCoin's concept is that you need to invest a certain amount of work
to sign a block, so the older a block is, the more work it takes to
forge it.  That concept originates from a crypto-anarchic design: in
the BitCoin world, everybody is pseudonymous, so even the signature
for the blocks are done by anonymous cowards.  We are back beyond the
first promissory notes, who at least had identifyable individuals as
signers.

We have to go a step back further, to see where that attack comes from
in the threat model: it's a man in the middle (MITM) attack to prevent the
proper spread-out of the current block.  A MITM attack to a P2P
network.  Preventing MITM attacks has other attempts to solve them,
either, even trust on first use (TOFU) does a descent job.

Any sane secure peer to peer network ought to have something better
than nothing, TOFU or PKIs that improve trust.  And that a full-blown
PKI takes away the anonymity is not a problem: A big warehouse of
ASICs to mine BitCoins also completely blows the anonymity of the
miner.  The miner or signer doesn't need anonymity; the parties that
actually exchange coins are the ones who want anonymity.

## Money and wealth

How money shapes a society, and why the limited supply of BitCoins is
far worse than neoliberalism

### Speculation object

![Stages in a bubble](https://people.hofstra.edu/geotrans/eng/ch7en/conc7en/img/stages_bubble.png)

Many humans like to gamble, and the pattern of gambling with
speculation objects are remarkable similar.  Even smart persons like
Isaac Newton lost a fortune in crashes.  The madness of humans however
can be calculated, though certainly not to the finest details.  The
BitCoin chart however looks like a classic.

![BitCoin Chart](https://assets.bwbx.io/images/users/iqjWHBFdfxIU/iFKLTQNYjslA/v4/800x-1.png)

Speculation bubbles don't mean the money disappears.  The money is
still there, it's just owned by someone else now.  Despite that the
total amount of money is constant through a speculation bubble, they
have a deep impact on economy.

That's because those who entered the bubble early got the money, and
those who entered late lost.  The tragedy is that the early ones are
smart and rare, and the late ones are stupid and many.  And that means
a huge concentration in money, and especially a loss for those who
didn't really have that money.

But let's assume BitCoin is not a bubble going bust, but it's supposed
to last (all 21 millions), and used as currency for everything by
everybody.  That means the price per coin would sore until the total
market cap equals the wealth of the entire earth.  And that would mean
all those people who were there in the early phase would be incredible
rich, just by being early.

Trickle down however doesn't work.  Money inherently trickles up.  In
a deflationary system, there is no decay of money; instead the money
of the wealthy becomes more and more just on its own.  They don't even
have to invest it to gain wealth.

## How to really distribute book-keeping

An important design goal for me is to handle massive ammounts of
micropayments, because that's an application where I see a need for a
crypto currency.

### Double entry bookkeeping system

First of all, if you are somehow familiar with bookkeeping, “a ledger”
has something fundamentally wrong in it: The singular.  You are
supposed to have more than one ledger, and every transaction needs to
go to two ledgers, one as debit, one as credit (the two columns of the
ledgers).

If you want to scale a crypto currency, you want to separate ledgers
by some arbitrary criteria so that the individual nodes are not
overloaded, and millions of bookings can get in per second; which is a
fairly reasonable number for micropayment.

Since you want to check if a coin someone offers you has already been
spent, you want to ask the corresponding ledger for it.  The ledger
records incoming and outgoing (credit and debit) in two log files, and
keeps the active coins available for query.  So you want to select the
ledger based on the pubkey of the coin — some reduction of it to a
reasonable size.  DDoS attacks at particular ledgers can be easily
mittigated: if the ledger you want to book to is attacked, you just
select a new pubkey; if the ledger you want to book from is attacked,
you may need to use another coin in your wallet.

The ledger units repsonsible for the same ledger each verify that they
have a consensus over the transactions; by syncing their positions.
They are put together into a tree of balance sheet ledgers, and the
next layer records sums of transactions signed (with reference to the
block) by the ledger units below.  And the top layer does the
bilancing: There can't be a change in value of the sum, it absolutely
has to be zero.

That will make the transaction protocol a bit tricky, because you have
to make sure that both ledgers really enter your transaction.  The
usual way to deal with that is to have a write intent (going to both
sides), and when the write intents have succeeded, do a commit.

To enter a coin into a double booking system, you need two
transactions: One is the coin itself (debit), and the other is a promise to
buy it back for its nominal value (credit).

## Literature

  1. [Stages in a bubble](https://people.hofstra.edu/geotrans/eng/ch7en/conc7en/stages_in_a_bubble.html)
  2. [Historic Stock Market Crashes, Bubbles & Financial Crises](http://www.thebubblebubble.com/historic-crashes/)
  3. [Extraordinary Popular Delusions and the Madness of Crowds (1841)](https://en.wikipedia.org/wiki/Extraordinary_Popular_Delusions_and_the_Madness_of_Crowds)
  4. [Online resource for double entry bookkeeping](http://www.double-entry-bookkeeping.com/double-entry-bookkeeping-tutorial/)
  5. [Distributed atomic transactions](https://www.cockroachlabs.com/blog/how-cockroachdb-distributes-atomic-transactions/)