# Searching with net2o #

Privacy enhanced networks (“darknets”) are notoriously difficult to search
with traditional approaches.  For a start, crawlers will not work, because
most material is not actually public.  Furthermore, you actually don’t want to
send your queries to a centralized search engine, anyhow — there is a reason,
why net2o is a peer to peer network.  This means you want a non–traditional
search engine, anyhow.

So how is search in net2o going to be implemented?

## Search locally ##

Indexing local data is easy.  Your local data is available to you, and you can
create a keyword,document key–value–list without problems.  Too frequent terms
are not stored (stopwords), and multiple search terms are a self join of the
database query.  Local search is the starting point of a peer 2 peer search;
it relies on humans crawling the net instead of robots.  When everybody is
participating in global search, this local search as a start is good.  It has
a number of advantages:

* The “crawler” sees the same thing as a normal user — SEOs can't provide
  misleading, different content to crawlers
* People do rank stuff they see, by clicking on likes and resharing.
  Providing dislikes allows to rank down, too.
* People can add tags when they reshare, making a search more specific
* Hashtags can improve searching by highlighting which terms are supposed to
  be important and relevant
* Dictionary based approaches can identify relevant search terms
  automatically: If a word occurs with a significantly much higher frequency
  than usual, it is likely a relevant term, if it occurs in a title or
  subtitle, it is likely more relevant.  Dictionary based analysis should map
  variants (inflections, spelling mistakes) to the same stem
  (e.g. “searching”, “search”, and “seach”) are the same search term).

Local search is the only search that is possible for closed communication.

## Global decentralized search ##

Only publically available (unrestricted) data is supposed to be globally
searchable.  A peer to peer network however does not guarantee that first
privately posted stuff remains private — this depends on the cooperation of
your peers, or if they decide to leak your message.  For material that is
intented to be publically visible, there needs to be a p2p method to spread
search results.  The indexing is done by local search index, usually by the
author himself (only in the leak case not).  The indexing extracts relevant
search terms (by looking at hash tags and dictionary based automatic search
terms).  Once indexed, the document anchor is submitted to the search term
servers responsible for the relevant terms, together with a short list of who
has this document.

Search engines like Google already have algorithms that distribute the load
within a large data center.  The concept can be used for a decentralized
search:  First stage is an index of responsibility.  So if you search, you ask
the index who carries the list of search results for that term.  To reduce
load of individual carriers of search result lists, frequently used terms can
be combined with secondary terms, e.g. “forth” and “program[ming]” can be
combined to be responsible for searching for that programming language, while
“back and forth” can be combined for that common phrase.

After getting responsible nodes from the index, you ask one of them with
secondary search terms to deliver the self join of that list.  By doing so,
you can now announce responsibility for that particular combination of search
results, and offer that if other people ask for it.  This allows to scale
frequent searches, because the more often people search for a term, the more
copies of a search result list are around.

There’s still a privacy problem that you should not attach search queries to
your permanent ID.  However, there is a severe problem with complete anonymity
for asking (e.g. using hash(search term) as secret key), because that allows
spammers to capture a search term.  It is necessary to tie reputations to
answer search queries.  The good thing is that the original search query list
can be signed by the responsible node, and cache nodes only hand out that
signature, so cache nodes can use throw-away one-time IDs.

Search lists effectively are chat logs with some special properties.  They
contain a reference to the document, a short excerpt that contains the most
relevant search terms, and a list of tags: search terms in decreasing order of
relevance.
