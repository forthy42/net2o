[up](squid.md) [back](squid-fed.md) [next](squid-literature.md)

# Dumb Contracts

To keep things simple, I chose to not have smart contracts; rather the
opposite: dumb contracts.  The main reason is that people don't understand
complicated contracts in legalese, and much less so in Forth or JavaScript.
For most financial transactions, you don't need a smart contract, anyways.
The basics of a contract is that you offer something and want something else
in exchange.  We assume that all things offered or asked for are things in a
BlockChain.

A dumb contract represents the state transition of the active data in the
BlockChain to fulfill that contract.  So you have sources (existing tokens in
existing accounts) and sinks (the same tokens, now in different accounts), and
brackets to hold tokens in a contract together.

To shorten sources, it could be possible to replace them by hashes.  This
however means that a contract on its own, outside the BlockChain, can't be
verified for correctness.  So a dumb contract just lists all state
transitions, and is bracketed together by signatures of all participants.
Each of the signatures covers all the elements up to that point in the
sequence; and a contract is valid when all sources and sinks are balanced.

Brackets can be final or require a continuation (by the same signer); that
allows to put open dumb contracts e.g. in an auction, select the winning bid,
and convert that to a finalized contracts.

To formalize a contract, sources are written as S, sinks as D, soft brackets
as ) and finalizing brackts as ].  The participant number is following the
symbol.

+ Money transfer (non-emptying to non-empty): S1D1]1S2D2]2
+ Money transfer (emptying to empty): S1]1D2]2
+ Bid/Ask in an exchange: S1D1]1, exchange contract after matching:
  S1D1]1S2D2]2 where S2==D1 and S1==D2
+ Auction offer: S1)1 (or S1D1)1 if non-emptying), auction bid: S2D2]2
  (non-empty version left as exercise to the reader), auction conclusion:
  S1)1S2D2]2D1]1, where S1==D2 and S2==D1. Auction offers are signed with an
  end-of-auction beginning to indicate the timeout, and the offering party can
  select the best match, allowing other algorithms as maximum price, too, or
  other timeout algorithms than the fixed deadline; e.g. 15 minutes after last
  bid or so.

The soft and hard bracket allows to have arbitrary off-chain logic for
finalizing contracts, and since these off-chain logic can be scripted, as
well, all kinds of turing-complete smart contracts are possible. But they
don't burden the chain with evaluating them.

The evaluation of a dumb contract is rather easy: All sources and sinks must
balance (i.e. the sums of all sources with their respective units must be
equal to the sums of all sinks with their units) and all owners must have a
closing hard bracket that encloses all their sources and sinks.

[up](squid.md) [back](squid-fed.md) [next](squid-literature.md)
