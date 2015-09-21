Get it
======

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
a few libraries.  After completion, you can generate your own key and
run some tests:

    ./n2o help
    ./n2o keygen <nick>

This will ask for your password and generate a key.  Write down the
base85 code it gives for revoking that key; this revocation key is not
stored anywhere else.

Check what keys you have already importet:

    ./n2o keylist

This should be your key and the net2o-dhtroot key.  Import my key

    ./n2o keysearch kQusJ

If you have installed it on two computers, you can chat with yourself:

   ./n2o chat @<yourself>