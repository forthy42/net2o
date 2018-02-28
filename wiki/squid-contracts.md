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

## State of an Asset Account

An asset account contains the following state:

+ A hash of the contract that last changed the state
+ A table of assets and their values (how many)
+ A timestamped signature of all that in canonical form

An asset account is addressed by its pubkey.  Contracts are addressed by their
hashes.

A wallet has a securely created random number as seed to create a sequence of
secret keys and their corresponding pubkeys.

The merkle tree to calculate the hashes of a block starts with the signatures
only; the R and S values of the ed25519 signature are xored to compress one
signature to a 256 bit value (enough for the security guarantee of the
signature).

A chain also contains metadata (e.g. asset types, permission rules,
granularities of assets), metadata is stored in a DVCS repository, and the
current revision's hash represents that metadata.  Updates can only be checked
in by consensus, i.e. if a new version is available, signers who accept the
new version of the metadata first try to sign and check in the new version,
and if that doesn't reach a consensus, try again with the old version.

## What's a Dumb Contract?

A dumb contract represents the state transition of the active data in the
BlockChain to fulfill that contract.  A contract is valid if it preserves all
the transacted goods (allows creation and annihilation of assets+obligations,
all other values have to be preserved).  Obligations are just another name for
an asset with a negative value.

Contracts are stored as shortened forms of the state transitions, only those
values that change are recorded in a form that is by design valid, and only
needs checking of the sources.  The new states need a valid signature of the
respective owner.  To execute a contract, all sources are fetched, the
transitions (assets moved from one account to the other) are performed, and
the signatures (destinations) of the new states are checked.

Contracts can be offered in open form, where parts of the transaction are left
open to be filled in, e.g. source or destination addresses.  If the open form
contains a destination, and the relevant information to update it is completed
by then, it can be appended by further informations without needing a
signature of the contract again.

To formalize a contract, Sources are written as S (timestamp), destinations as
D (new timestamp + signature), obligations as O, assets as A, delta amounts as
+ or - (give or take), and numbers to select the correct source if unclear
(the last source is always the active one).

All sources specify the date of the source state, so that a contract can be
performed only once — the destination date must be later than the source date.

+ Claimed money cheque (anybody who has the transaction can claim the money;
  requires trust to the ledger node that accepts the cheque): SA-DSA+D
+ Money transfer (only the designated recipient can claim the money): SA-SA+D1D
+ Creation of asset and obligation: SA+O-D
+ Two party purchase: SA¹+A²-SA¹-A²+1D2D
+ Two party purchase delivery: SA-SO+1D2D (annihilates the asset)
+ Bid/Ask in an exchange: SA¹+A²-D, finalized by SA¹+A²-DSA¹-A²+D. Note that
  bids/asks in an exchange can be more complicated when they are only partly
  fulfilled; the splitting requires action by the bidder; and also note that
  this kind of bid requires, like the cheque, trust in the ledger node; but
  less than: The ledger node can only buy for the same price, not steal the
  money.
  Better finalize the contract with the other side.
+ Auction offer: SA¹-, auction bid: SA¹-SA¹+A²-D, auction conclusion:
  SA¹-SA¹+A²-D1A²+D. Auction offers are signed with an end-of-auction
  beginning to indicate the timeout, and the offering party can select the
  best match, allowing other algorithms as maximum price, too, or other
  timeout algorithms than the fixed deadline; e.g. 15 minutes after last bid
  or so.

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
sources are never reordered.  Sources are written as pk+timestamp, but hashed
in as their signature, so you can only verify the destination signature if you
actually have checked for the source state in the corresponding ledger.

## Size of a transaction

+ Opcodes are one byte (there aren't that many); literals are bytewise encoded
  and strings have a length preceeding the raw data — see
  [commands](commands.md)
+ Sources are 8+32=40 bytes strings
+ Assets are an integer (index into the set of assets), an optional describing
  string (not needed for a currency)
+ Values are (for negative values zigzag encoded) 64 bit integers, for a legal
  tender, the scale is in cents, for deflationary coins the scale can be
  considerably larger.  Sums are always kept in 128 bits, so for really large
  transactions, you can use double values (two 64 bit integers, one normal,
  one zigzag encoded).
+ Destinations are signatures with timestamp and expiration, i.e. 80 bytes
  strings.

A minimal transaction is somewhat less than 300 bytes, and that's already
non-emtpy to non-empty account.

[up](squid.md) [back](squid-fed.md) [next](squid-literature.md)
