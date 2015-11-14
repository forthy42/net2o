Threat Model
============

net2o's original motivation and development start was before the
Snowden leaks.  Therefore, the threat model has been influenced by two
or three forms of adversaries:

1. The criminal, who spread malware and phishing, and break into
centralized servers to steal passwords.

2. The authoritarian state, e.g. China, which monitors activities and
censors/blocks content, often by using MITM attacks on secure
transports.  A variation of the authoritarian state is the corporate
IT, which does the same.

3. The corporate data harvester, who stalks you through the net, and
collects data to place ads; often enough data that is relatively
close to metadata, like which page did you visit when.  That data can
be abused in different ways, too.

It turned out that the NSA is a combination of all three threats, plus
a new threat, the wide collection of metadata, and corresponding
actions taken when people are in contact with "targets", including
indirect contacts.

Threats in detail with solution sketch
--------------------------------------

1. Remote execution: Many security holes allow remote code execution.
This paves the way for malware.  Since defect density of well-designed
and -debugged code is only two orders of magnitude better than lousy
code, the overall code size has to be limited.  And for this code, all
bugs simply have to be found and fixed.

2. Eavesdropping, passive: Wiretapping allows to record all
communication, and collect data and metadata.  Data can be protected
by encryption.  However, metadata is available at several places:
Direct connections (without onion routing) are revealing.  Queries in
DHT nodes, which can be operated by enemies, can be revealing, too.
When the adversary taps big interchange nodes, direct connections may
be hidden by taking a shorter route.  Onion routing can be
ineffective, if the adversary controls enough relays, especially the
most vulnerable entry nodes.  So good anonymization is way more
difficult to reach than good data protection.  For a start, all nodes
in an onion router mesh must be equal, and none of them may know if
they are entry or rendesvouz nodes.

3. Eavesdropping, active: The adversary can perform a man in the
middle attack (MITM).  Public key exchange only protects against
passive eavesdropping.  So keys have to be verified that they belong
to the person you want to talk to; to reduce the burden of
verification, a trust on first use (TOFU) model is used.  Key
revokation is based on proof of creation, so once you trust a key, you
can revoke and generate a new key, which is trusted, too.

4. Censorship, blocking: Adversaries want to censor based on keywords,
persons and sites you want to visit.  Encryption protect against
keyword search, relays outside the control of the adversary allow to
connect to persons and sites as you like.  The adversary still can cut
you entirely off the net, or insulate the people under his control
from the rest of the world, by dropping all packets at his borders.

5. Censorship by harassing the origin: Anonymity can shield you from
being detected as the origin; easy use of multiple, seemingly
unrelated ids can help to hide the identity of particular activities,
plus all means to hide metadata.  However, persons with high influence
can often be detected by other means, which means anybody who angers
the authority must make no mistakes, and eventually has to move to a
secure place, before he's identifed.  This is difficult, as it is not
clear who is your adversary, and who is your friend.  Hiding the
identity is particularly difficult for celebrities whos fanbase want
to have a verified contact, and who speak to the public.  These are
usually also the most influential people.  Technical solutions can
only rise the price for being detected, so ideally the price to get
you corresponds to the evil you did.  The primary effort of net2o is
to make mass surveillance prohibitively expensive, but targeted
individuals very likely still face danger.

6. Confiscating and searching devices: Adversaries may be able to get
physical access to your devices or the devices with your backups.
Full disk encryption can help in off mode, file encryption can help
you on multi-user systems where other users may legitimely access some
files, but not others.  File encryption is also necessary when you
store files on devices owned by other people (e.g. online off-site
backups).  Tamper detection is necessary to avoid using bugged device.
Several solutions here are outside the scope of net2o.

Trust model
-----------

Actually, a threat model is the wrong way to design secure software.
A threat model makes the assumption of "default permit", and deny
access to the threat.  "Default deny" is more important.  Who can you
trust?

1. Nobody.  You can't even trust yourself, because you will make
mistakes.

Of course, that's an extreme position, but we are dealing with that
sort of trust model all the time. So what's a realistic trust model?
A realistic trust model follows a "need to know" principle - you can't
distrust those who need to know.  But you verify that they deserve
your trust.

1. You trust yourself.  You educate youself to reduce the risk of
making stupid mistakes.

2. You trust those you communicate with to the extent that they also
have access to the communication you have with them.  You use your
communication to verify that they deserve that trust.

3. You trust the authors of the software you use, and the auditors of
this software, but to make sure that trust is deserved, they make
their sources available.

4. You keep as much data on your own systems, and encrypt it when it
is stored or transmitted elsewhere, but to enable public contacts, you
need information in the public.

5. Anonymous communication relies on a combination of trust,
unrelatedness, and cover traffic (which requires being central enough
to deserve trust from many users).  This is why it's hard.