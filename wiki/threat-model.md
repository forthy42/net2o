Threat Model
============

net2o's original motivation and development start was before the
Snowden leaks.  Therefore, the threat model has been influenced by two
or three forms of adversaries:

1. The criminal, who spread malware and spam, and break into
centralized servers to steal passwords.

2. The authoritarian state, e.g. China, which monitors activities and
censors/blocks content, often by using MITM attacks on secure
transports.  A variation of the authoritarian state is the corporate
IT, which does the same.

3. The corporate data harvester, which stalks you through the net, and
collects data to sell you stuff; often enough data that is relatively
close to metadata, like which page did you visit when.  That data can
be abused in different ways, too.

It turned out that the NSA is a combination of all three threats, plus a
somewhat new threat, the wide collection of metadata.