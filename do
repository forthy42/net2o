#!/bin/bash

echo "This script builds net2o from scratch"

GFORTH=gforth-0.7.9_20190606

if [ "$(uname -o)" = "Cygwin" ]
then
    CONFOPT="--prefix=/usr $*"
else
    CONFOPT="$*"
fi

# helper functions

if which sudo >/dev/null
then
    echo "sudo available"
else
    if which su >/dev/null
    then
	function sudo {
            su --command="$*"
	}
    else
	function sudo {
	    eval "$@"
	}
    fi
    export sudo
fi

function git-get {
    echo "=== git clone $purl/$pname.git $* ==="
    purl=$1
    pname=$2
    shift; shift
    if [ -d $pname ]
    then
	(cd $pname; git pull -f)
    else
	git clone $purl/$pname.git $*
    fi
}
function build {
    pname=$1
    (cd $pname; ./autogen.sh && ./configure $CONFOPT && make && sudo make install)
}
function build-clean {
    pname=$1
    (cd $pname; ./autogen.sh $CONFOPT && ./configure $CONFOPT && make clean && make && sudo make install)
}

# ask for sudo password

echo "We'll install several things, caching root password..."
sudo true

# get net2o itself

if [ ! -f net2o.fossil ]
then
    fossil clone https://fossil.net2o.de/net2o net2o.fossil
    fossil open net2o.fossil
else
    if [ ! -f n2o.fs ]
    then
	fossil open net2o.fossil
    fi
    fossil up
fi

# get, build, and install ed25519-donna, keccak and threefish

git-get https://github.com/forthy42 ed25519-donna

./autogen.sh

make configs
make no-config
sudo make install-libs

# get, build, and install Gforth if needed

which gforth 1>/dev/null 2>/dev/null && GF=$(gforth --version 2>&1 | cut -f1-2 -d' ' | tr ' ' '-')
(which gforth 1>/dev/null 2>/dev/null && test '!' "$GF" "<" "$GFORTH") || (
    wget -c http://www.complang.tuwien.ac.at/forth/gforth/Snapshots/${GFORTH#gforth-}/$GFORTH.tar.xz
    tar Jxf $GFORTH.tar.xz
    if swig -forth $GFORTH/unix/test.i
    then
	echo "A Forth-capable swig is found, everything fine"
    else
	echo "Build a Forth-capable swig"
	git-get https://github.com/GeraldWodni swig
	build swig
    fi
    build $GFORTH
)

# clean up set to root stuff
sudo chown -R "$(whoami)" ~/.gforth .

# make sure libraries are found
test "$(uname -o)" = "Cygwin" || sudo /sbin/ldconfig

# we test for an existing Gforth that can load net2o.fs
# if the snapshot doesn't, try the git version

gforth-fast n2o.fs -e bye 1>/dev/null 2>/dev/null || (
    git-get git://git.savannah.gnu.org gforth
    build-clean gforth
)

./configure # reconfigure, as we now have a working Gforth
make libcc
sudo make install
make TAGS
