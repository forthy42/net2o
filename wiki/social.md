# Using net2o as social network #

When Google+ shut down, I took the opportunity to accelerate my plans on
running social networks over net2o.  How does a social network over a
peer2peer network look like and what's the challenge?

## Data structure of social network postings ##

* Timelines (of collections and groups) are kept as chat logs.  Each posting
  is a chat log entry, which contains a short teaser of the posting, possibly
  a thumbnail image, and a link to a DVCS project.
* A thread (posting+comments) is a DVCS project.  That by itself is another
  chat log, with each checkin as log entry.  Checkins contain a reference to a
  file that highlights the posting/comment added in the checkin.  Likes are
  just messages with a like emoji and a reference to the checkin that is
  liked.  The DVCS as base to organize a thread allows editing.  It's even
  possible to collaboratively edit if permissions allow.
* Checkins can be pull–request style, then they need interaction from the
  owner.  Normally, however, you'd want to have them automatically checked
  against permissions and conflicts and pulled when they fit.
* A posting itself is a markdown file.  Images and videos are in jpeg/png and
  mkv format.
* Likes/dislikes and votes are short messages containing only an emoji code.
  Permission to send these short messages is limited to make sure you can only
  like once, so you need to face out your previous like if you want to send a
  new one.  Votes can be closed, so the call for vote shall have a deadline.

## Importing data from other networks ##

One thing that is annoying with new networks is that they start all empty.
You leave all your postings behind in the old network.  But the GDPR allows to
take out data, and if everything is fine, it's possible to convert that data.

I wrote an importer for Google+, which is mostly complete by now, and also
started with Blogger.com, Twitter, and Facebook importers.  Other importers
will follow.

Twitter’s takeout is very limited, it only contains your own tweets, no
answers to your tweets from others, so it takes the communication part out of
the takeout.  Tweets you answered to are referred, so you can use Twitter’s
API to access them, you can probably also search for answers to a particular
tweet and get those.

## Connecting to other social networks ##

While in general, a connector to a plattform is a bad idea, for social
networks, where publicity matters, connectors can have some place, at least
for the transition period.  Though I have not attempted to actually write one;
but at least for networks that have an API, you could import feeds into
net2o, and (with severe limitations) crosspost from net2o.

## Permissions ##

Social networks require a more fine–grained permission system than mere
chats.  Who’s allowed to write, to read, and to delete what?  In a chat,
participants are allowed to read, write, and delete their own messages.  In a
social network, we can have wider ranges of permissions.

Groups can have all members create new postings, and Wikis allow all members
to edit postings.  Moderators can change state, like protect an entry and
resort to manual handling of pull requests if an edit war is going on, or they
can move members to read–only status.  Comments for a particular posting can
be disabled, if the discussion gets out of control.  And of course, as a basic
measure already necessary for chats, people can be kicked out or denied
anything but read access.

Moderating quality depends on the interface.  If the moderator interface is
cumbersome, people don’t want to moderate, and the mud from a bad discussion
causes problems.  Moderation can also happen under alternative IDs, so
normally, you don't use your moderator ID when discussing, only for
moderating.  The interface shall facilitate that.

Write permissions are performed at automatic pulls.  Someone sends you a
message, you fetch the patch set that is associated, and if it fits, you merge
it.  Since chats are connected by a mesh, everybody checks for write
permission, and forwards if they think it’s ok.  Under strict moderation
guidelines, you might want to have a moderator approval signature before you
make those messages visible.  Moderators can reply to a commit with an “👌”
emoji, without that, it‘s not processed.

Read access is much more critical: This needs to be protected by encryption.
Such encryption is already available for private/protected chats, so use
that.  You can also use encryption for shadow–bans: Here, the “👌” reply is
only encrypted for the shadow—banned person or group, and the others will not
see it.

## Circle sharing ##

Google+ in the beginning had a good way to facilitate quick growth of peers:
circle sharing.  You can recommend a group of people you find worthwile to
listen to to other people.  Google+ disabled that function later, increasing
the ghost town problem.  If you think about permissions, circle sharing should
also work for blocklist sharing: If people already know who's best blocked,
and they can share their lists, bad actors (spammers and trolls) have more
trouble getting through.

Circle sharing can be implicitly also offered when you have groups: Once you
know the members, you have such a circle.  However, people just lurking in a
group may not have their identity exposed.  Hidden readers are problematic in
private groups, but they are not problematic in public groups where hidden
readers can be the vast majority.

## Meta–information ##

An ID needs meta information, what kind of collections and groups it has, and
that needs to be a chatlog under a well-known name.  After contacting an ID,
you fetch that and off you go.  This meta information already needs
permissions, e.g. deny read access for private collections and groups — that
is done through encryption.
