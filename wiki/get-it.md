Get it
======

Get it for PC
-------------

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

Get it for Android
------------------

You need: An Android phone with at least Android 2.3, and Gforth,
either from the app store, or from [here](https://net2o.de/Gforth.apk).

To load net2o into Gforth, start Gforth and enter

    include net2o/n2o.fs

Then turn on the net2o command mode for net2o with

    n2o-cmds

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