[up](squid.md) [back](squid-fed.md) [next](squid-literature.md)

# Dumb Contracts

To keep things simple, I chose to not have smart contracts; rather the
opposite: dumb contracts.  The main reason is that people don't understand
complicated contracts in legalese, and much less so in Forth or JavaScript.
For most financial transactions, you don't need a smart contract, anyways.
The basics of a contract is that you offer something and want something else
in exchange.  We assume for simplicity that all things offered or asked for
are recorded in the same BlockChain.

## Assets and Obligations

To allow arbitrary real world goods to be represented by a token on the
BlockChain, we allow people to create those as future assets.  When the future
expires, the creator has an obliation to deliver that asset to the current
owner; it will be removed from the BlockChain afterwards.  Verification of
actual delivery of that asset may be up to some notary or other arbiter,
usually complaint-based (i.e. the arbiter is only there to resolve conflicts;
conflicts should be public and lower the score of an asset creator).  Future
assets have the same form as credits, they create two tokens, an asset and an
obligation.  Both can be traded, and when the obligation is fulfilled, asset
and obligation meet each other again, and are annihilated.

Legal tenders inserted into the BlockChain are handled that way, too: A bank
issues a pair of assets and obligations, the obligation stays at the bank, the
asset is sold for that legal tender to the purchaser (the legal tender is
transmitted by normal bank transfer).  The purchaser can trade that legal
tender with others, and if you want to leave the BlockChain, you sell it (for
normal bank transfer) to one of those banks that have obligations in that
currency; they are now free to annihilate those obligations, as they have
fulfilled them.

Mortgages and loans with interest rates are too complicated for a dumb
contract, which is a good thing.

## What's a Dumb Contract?

A dumb contract represents the state transition of the active data in the
BlockChain to fulfill that contract.  A contract is valid if it preserves all
the transacted goods (allows creation and annihilation of assets+obligations,
all other values have to be preserved).

Contracts are stored as shortened forms of the state transitions, only those
values that change are recorded in a form that is by design valid, and only
needs checking of the sources.  The new states need a valid signature of the
respective owner.  To execute a contract, all sources are fetched, the
transitions (assets moved from one account to the other) are performed, and
the signatures of the new states are checked.  The contract itself must be
signed by all parties, too.

Contracts can be offered in open form, where parts of the transaction are left
open to be filled in, e.g. source or destination addresses.  In this case, a
partial contract is signed by one party, and the full contract by another.

To formalize a contract, Sources are written as S (timestamp), sinks as D
(timestamp + signature), obligations as O, assets as A, delta amounts as #,
and contract signatures bracketing a contract as ), followed by the signer
number.

All sources specify the date of the source state, so that a contract can be
performed only once — the destination date must be later than the source date.

+ Money check: S1A-#)1D1S2A#)2D2
+ Money transfer: S1A-#S2A#)1)2D1D2
+ Creation of asset and obligation: S1A#O#)1D1
+ Two party purchase: S1A1#1A2-#2S2A1-#1A#2)1)2D1D2
+ Two party purchase delivery: S1O1-#1S2A1-#1)1)2D1D2 (annihilates the asset)
+ Bid/Ask in an exchange: S1A1#1A2-#2)1D1, finalized by
  S1A1#1A2-#2)1D1S2A1-#1A2#2)2D2, note that bids/asks in an exchange can be
  more complicated when they are only partly fulfilled; the splitting requires
  action by the bidder.
+ Auction offer: S1A1-#1)1, auction bid: S1A1-#1)1S2A1#1A2-#2)2D2, auction
  conclusion: S1A1-#1)1S2A1#1A2-#2)2D2D1. Auction offers are signed with
  an end-of-auction beginning to indicate the timeout, and the offering party
  can select the best match, allowing other algorithms as maximum price, too,
  or other timeout algorithms than the fixed deadline; e.g. 15 minutes after
  last bid or so.

The evaluation of a dumb contract is rather easy: All sources and destinations
must balance (i.e. the sums of all sources with their respective units must be
equal to the sums of all sinks with their units) and all destinations must
have a signature for their state.  The contract must have signatures of all
destinations inside, but the signature is allowed to cover only part of the
contract.  As and Os can be created in pairs or annihilated in pairs,
permission who is allowed to create depend on the type of asset and obligation
(everybody is allowed to annihilate).

Sources are seen as selector (a source is a pk+timestamp), assets select the
asset value in the source, values operate on that asset.  Destinations are
signatures and refer to the corresponding source by number of occurance — the
sources are never reordered.

[up](squid.md) [back](squid-fed.md) [next](squid-literature.md)
