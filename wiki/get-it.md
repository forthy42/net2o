Get it
======

Get it for Debian GNU/Linux
---------------------------

I've created a Debian repository to make it easy to install net2o.

Create a file `/etc/apt/sources.list.d/net2o.list` with the content:

    deb http://net2o.de/debian testing main

There are actually three repositories, stable, testing and unstable;
at the moment, all packages are the same.  Add my key to the trust db
so that Debian can verify the packets:

    wget -O - https://net2o.de/bernd@net2o.de.gpg.asc | apt-key add -

And then run the following commands as root:

    aptitude update
    aptitude install net2o

Since net2o requires a way more recent Gforth than in Debian's
repository, you will need aptitude to resolve this; if the first
solution doesn't work out, say "n" to get the second solution.

Get it for Android
------------------

You need: An Android phone with at least Android 2.3, and Gforth,
either from the app store, or from [here](https://net2o.de/Gforth.apk).

This installs Gforth with the Gforth icons, and a ready-to-run net2o
icon.  Just tap on the net2o icon to run net2o; you'll be asked to
create a key on the first run, and to open up a key on any further run.

Get it for PC from source
-------------------------

You need: A Linux machine, Windows with Cygwin or better Cygwin64, Mac
OS X with fink development tools (please use GCC, don't use XCode's
clang, it takes ages to compile Gforth with clang).  You could also
compile the Android version with Android SDK+NDK, but that's a different story.

You want to have the following packets installed: git automake
autoconf make gcc libtool libltdl7 fossil (libtool-ltdl on
RedHat/Centos). Or get Fossil here:
[fossil](http://www.fossil-scm.org/index.html/doc/tip/www/index.wiki)

Get the [do](https://fossil.net2o.de/net2o/doc/trunk/do) file
(latest revision), put it into your net2o folder, and let it run.

This script will ask for your root password to install Gforth and the
a few libraries.

You can try net2o (see below) either by starting net2o with

    ./n2o cmd

and enter net2o command mode, or by running each command with

    ./n2o <command>

in the second case, you'll be asked again and again for your password
when needed.

Try it
------

You can execute the following net2o script commands either by entering

    help
    keygen <nick>

This will ask for your password and generate a key.  Write down the
base85 code it gives for revoking that key; this revocation key is not
stored anywhere else.  For some tests, you want to create a second key
(e.g. to test chat).  Give it a different pass phrase; you can create
as many IDs as you like; if they have different passphrases, only one
of them opens at a time.

Check what keys you have already importet:

    keylist

This should be your key and the net2o-dhtroot key.  Import my key

    keysearch kQusJ

At the moment, a 32 bit ID should do it...  Your own pubkeys have been
exported with the keygen command into a <nick>.n2o file.  You can
import that in your other id(s) with

    keyin <nick>.n2o

Try encrypt and decrypt a test file for yourself:

    <create a file>
    enc <file>
    cat <file>.v2o
    dec <file>.v2o

You can also create detached signatures (<file>.s2o):

    sign <file>
    verify <file>

You can try a group chat with several ids, start the group "test" with <id1>

    chat test

And connect from the other ids with

    chat test@<id1>

Or do a 1:1 chat with

    chat @<id2>

from <id1> and

    chat @<id1>

from <id2> on several computers/terminals.  The chat mode works a bit like IRC,
you can use /help to list the commands, /peers to see the direct
neighbors, and /me <action> if you aren't actually talking.

You can copy small or large files:

    server

to supply things on <id1>, and e.g.

    get @<id1> data/2011-05-20_17-01-12.jpg

to copy one of the test images.  A fine-grained access right system
with default deny will follow, so this section will change.