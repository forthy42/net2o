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

    sudo apt install apt-transport-https

Create a debian sources.list file pointing to the net2o repository,
and add my key to the trust db so that Debian can verify the packets,
update the repository data and install net2o, so enter:

    sudo -s
    cat >/etc/apt/sources.list.d/net2o.list <<EOF
    deb [arch=i386,amd64,armhf,armel,arm64,powerpc,mips,mipsel,all] https://net2o.de/debian testing main
    EOF

Remove the architectures on the list above which you don't need; change
`testing` to `stable` or `oldstable` if you are running on older versions of
Debian.  On Debian `testing` (and in the meantime `stable`), the list of
architecture is not necessary.  On older versions, the “`all`” part is not
searched if you don't have that list, then Gforth fails to install the
“`gforth-common`” part.

    wget -O - https://net2o.de/bernd@net2o.de-yubikey.pgp.asc | apt-key add -
    apt update
    apt install net2o-gui
    exit

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

Get it in a Docker Container
----------------------------

Pull the docker image from my [Dockerhub account](https://hub.docker.com/r/forthy42/net2o) with

    docker pull forthy42/net2o

Create a directory for the files net2o will use.  This will be mounted as
`/net2o` in the container.  A minimal config file is needed for net2o to find
the other data:

    mkdir ~/net2o.dk
    cat <<EOF  >~/net2o.dk/config
	.net2o="/net2o"
	.net2o-config="/net2o"
	.net2o-cache="/net2o"
	EOF

optionally copy *other* existing net2o files into this directory keeping the
directory structure intact.

Create an alias to run the docker:

    alias n2o="docker run -ti --rm -v ~/net2o.dk:/net2o --user $(id -u):$(id -g) forthy42/net2o"

If you want to run the GUI, you need to fetch

    docker pull forthy42/net2o-gui

and define your alias as:

    alias n2o-gui="docker run -ti --rm -v ~/net2o.dk:/net2o --user $(id -u):$(id -g) -e USER=$USER -e DISPLAY=$DISPLAY -v /tmp/.X11-unix/:/tmp/.X11-unix/ -v /dev/dri:/dev/dri -v /usr/share/fonts:/usr/share/fonts -v $XAUTHORITY:/home/gforth/.Xauthority -v ~/.fonts:/home/gforth/.fonts -v ${XDG_RUNTIME_DIR}/pulse:/run/user/1000/pulse forthy42/net2o-gui gui"

Get it in a Flatpak Container
-----------------------------

You need flatpak for your Linux distribution.  Then add the net2o flatpak
repository:

    flatpak remote-add --if-not-exists net2o https://flathub.net2o.net/repo/net2o.flatpakrepo

Now you can install the net2o container:

    flatpak install net.net2o.net2o

Alias for the TUI:

    alias n2o="flatpak run --share=network net.net2o.net2o"

Alias for the GUI:

    alias n2o-gui="flatpak run --share=network --socket=x11 --device=dri --socket=pulseaudio net.net2o.net2o gui"

Your app data and config is in `~/.var/app/net.net2o.net2o`

Get it in a Snap Container
--------------------------

I created a [snap container](https://snapcraft.io/net2o).  You need to get
snap for your Linux distribution (in Ubuntu, it's already there).  The snap
container is currently outdated, because the build process failed to work on
my Linux for some time.

[![Get it from the Snap Store](https://snapcraft.io/static/images/badges/en/snap-store-white.svg)](https://snapcraft.io/net2o)

or the CLI way:

    sudo snap install net2o

The net2o snap needs several manual connectors:

    snap connect net2o:netlink-connector :netlink-connector
	snap connect net2o:locale-control :locale-control
	snap connect net2o:audio-record :audio-record

The netlink connector is needed to detect changing interfaces

* if interfaces change, connections to peers and DHT entries may need to
  changes.
* locale from the host's settings
* audio recording

And then set an alias

    alias n2o=/snap/bin/net2o.n2o

Your data currently resides inside your `~/snap/net2o/current/` hierarchy, but
that needs to change to make it more useful.

Get it for Android
------------------

You need: An Android phone with at least Android 4.0, and Gforth, either from
the [Play Store](https://play.google.com/store/apps/details?id=gnu.gforth), or
from [net2o.de/android](https://net2o.de/android/Gforth.apk).

This installs Gforth with the Gforth icons, and a ready-to-run net2o
icon in the app drawer.  Just tap on the net2o icon to run net2o;
you'll be asked to create a key on the first run, and to open up a key
on any further run.

### Key information

The [certificate](https://net2o.de/bernd@net2o.de-android.cer) has the
SHA-1/SHA256 fingerprint and the informations as follows:

    sha-1:  00:44:1B:9D:F8:0B:9D:9E:2F:68:9D:0F:B9:B4:85:28:D4:10:5C:7E
	sha256: 87:21:D8:3A:FF:47:8D:50:D0:02:00:C7:06:A1:00:6A:69:1C:37:47:88:52:94:45:C7:E0:DA:8A:47:99:F2:97
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

### Key information (new key for September 22th 2019)

I changed my key to a Certum smartcard based rsa2048 key, this is the first
update, and I generated a new key pair for that.

These files are now signed with a [sha256rsa2048
certificate](https://net2o.de/bernd@net2o.de-windows.crt) with the
SHA-1/SHA256 fingerprint

    sha-1:  96:0b:7e:b8:cb:7f:52:f6:70:00:bf:23:5e:25:66:c9:eb:9c:d0:3c
	sha256: 72:BE:1C:EE:D9:4E:20:94:92:7A:13:BC:C6:8F:7C:E9:3F:15:81:F6:6E:91:85:6B:F6:C5:E1:BA:15:22:01:DA
	E = bernd@net2o.de
	CN = Open Source Developer, Bernd Paysan
	L = München
	O = Open Source Developer
	C = DE

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
