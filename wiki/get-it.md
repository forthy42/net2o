Get it
======

net2o currently is still experimental and the protocol can have incompatible
changes at any time, so keep net2o up to date when you try it.  Any day can be
a “flag day”.

Get it for Debian GNU/Linux
---------------------------

I've created a Debian repository to make it easy to install net2o.

If you don't have https transport for apt installed, do that first,
since I'll redirect you to https in any case:

    sudo apt-get install apt-transport-https

Create a debian sources.list file pointing to the net2o repository,
and add my key to the trust db so that Debian can verify the packets,
update the repository data and install net2o, so enter:

    sudo su -
    cat >/etc/apt/sources.list.d/net2o.list <<EOF
    deb [arch=i386,amd64,armhf,armel,arm64,powerpc,mips.mipsel,all] https://net2o.de/debian testing main
    EOF
    wget -O - https://net2o.de/bernd@net2o.de-yubikey.pgp.asc | apt-key add -
    aptitude update
    aptitude install net2o
    exit

Remove the architectures on the list above which you don't need; on
Debian testing, the list is not necessary, on older versions, the all
part is not searched if you don't have that list, then Gforth fails to
install the gforth-common part.

There are actually three repositories: stable, testing and unstable; at the
moment, all packages are the same; when net2o matures, stable/testing/unstable
will get different roles, just like Debian (stable=old and rusted, testing=new
and somewhat tested, unstable=most recent).  Binaries are available for amd64,
i386, armhf, arm64, powerpc, armel, mips, and mipsel (in order of how often
they get updated).  More depend on availability of Debian distributions that
run on qemu…

### Key information (new Key from October 18th, 2017)

I changed to a Yubikey-based signature.  The key's fingerprint is:

    60E71A15 93575330 99A0AAF9 CAF021DB 3B7FA946

When you do an `apt-key list`, the result should contain this key:

    pub   4096R/3B7FA946 2017-09-20
    uid                  Bernd Paysan (yubikey) <bernd@net2o.de>
    sub   4096R/3E1896A1 2017-09-20
    sub   4096R/50C9A69B 2017-09-20

Get it for Android
------------------

You need: An Android phone with at least Android 4.0, and Gforth,
either from the app store, or from [net2o.de/android](https://net2o.de/android/Gforth.apk).

This installs Gforth with the Gforth icons, and a ready-to-run net2o
icon in the app drawer.  Just tap on the net2o icon to run net2o;
you'll be asked to create a key on the first run, and to open up a key
on any further run.

### Key information

The [certificate](https://net2o.de/bernd@net2o.de-android.cer) has the
SHA-1 fingerprint and the informations as follows:

    00:44:1B:9D:F8:0B:9D:9E:2F:68:9D:0F:B9:B4:85:28:D4:10:5C:7E
    CN=Bernd Paysan, OU=dev, O=net2o, L=München, ST=Deutschland, C=DE

and signs with sha1rsa2048 (Google!).  If you want to verify the apk yourself,
download the [certificate](https://net2o.de/bernd@net2o.de-android.cer), add
it to your keyring and check:

    keytool -importcert -file bernd@net2o.de-android.cer
    jarsigner -verify -verbose Gforth.apk

Get it for Windows
------------------

You need: A 32/64 bit x86/amd64 Windows. You need to install
[Gforth](http://www.complang.tuwien.ac.at/forth/gforth/Snapshots/current/gforth.exe)
from the latest Snapshot first.

Then you install the current [net2o](https://net2o.de/windows/net2o.exe)
snapshot.

### Key information (new key for September 28th 2017)

I changed my key to a Certum smartcard based rsa2048 key.

These files are now signed with a [sha256rsa2048
certificate](https://net2o.de/bernd@net2o.de-windows.crt) with the
SHA-1 fingerprint (why are fingerprints still SHA-1?)

    f0:db:1a:7c:c4:22:49:d6:9f:cd:40:3b:dc:6b:a3:9c:6b:a4:ac:4b
    E=bernd@net2o.de, CN=Open Source Developer, Bernd Paysan, L=München, O=Open Source Developer, C=DE

Get it for PC from source
-------------------------

You need: A Linux machine, Windows with Cygwin or better Cygwin64, Mac OS X
with fink/brew development tools (please use GCC, don't use XCode's clang, it
takes ages to compile Gforth with clang).  You could also compile the Android
version with Android SDK+NDK, but that's a different story.

You want to have the following packets installed: git automake
autoconf make gcc libtool libtool-bin libltdl7-dev yodl emacs
libpcre3-dev bison fossil (libtool-ltdl on RedHat/Centos; the
libtool-bin is for Debian). Or get Fossil here:
[fossil](http://www.fossil-scm.org/index.html/doc/tip/www/index.wiki)

Get the [do](https://fossil.net2o.de/net2o/doc/trunk/do) file
(latest revision), put it into your net2o folder, and let it run.

    mkdir net2o
    cd net2o
    wget https://fossil.net2o.de/net2o/doc/trunk/do
    chmod +x do
    ./do

This script will ask for your root password to install Gforth and the
a few libraries.

[Try it](try-it.md)
-------------------
