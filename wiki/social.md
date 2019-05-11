# Using net2o as social network #

When Google+ shut down, I took the opportunity to accelerate my plans on
running social networks over net2o.  How does a social network over a
peer2peer network look like and what's the challenge?

## Data structure of social network postings ##

* Timelines are kept as chat logs.  Each posting is a chat log entry, which
  contains a short teaser of the posting, possibly a thumbnail image, and a
  link to a DVCS project.
* A thread (posting+comments) is a DVCS project.  That by itself is another
  chat log, with each checkin as log entry.  Checkins contain a reference to a
  file that highlights the posting/comment added in the checkin.  Likes are
  just messages with a like emoji and a reference to the checkin that is
  liked.  The DVCS as base to organize a thread allows editing.  It's even
  possible to collaboratively edit if permissions allow.
* A posting itself is a markdown file.  Images and videos are in jpeg/png and
  mkv format.

## Importing data from other networks ##

One thing that is annoying with new networks is that they start all empty.
You leave all your postings behind in the old network.  But the GDPR allows to
take out data, and if everything is fine, it's possible to convert that data.

I wrote an importer for Google+, which is mostly complete by now, and also
started with Blogger.com, Twitter, and Facebook importers.  Other importers
will follow.

## Connecting to other social networks ##

While in general, a connector to a plattform is a bad idea, for social
networks, where publicity matters, connectors can have some place, at least
for the transition period.  Though I have not attempted to actually write one;
but at least for networks that have an API, you could import feeds into
net2o, and (with severe limitations) crosspost from net2o.
