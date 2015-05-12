#!/bin/bash

echo "This script builds net2o from scratch"

GFORTH=gforth-0.7.9_20150510

if [ $(uname -o) -eq Cygwin ]
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
    purl=$1
    pname=$2
    shift; shift
    if [ -d $pname ]
    then
	(cd $pname; git pull)
    else
	git clone $purl/$pname.git $*
    fi
}
function build {
    pname=$1
    (cd $pname; ./autogen.sh && ./configure $CONFOPT && make clean && make && sudo make install)
}

# get net2o itself

if [ ! -f net2o.fossil ]
then
    fossil clone http://fossil.net2o.de/net2o net2o.fossil
    fossil open net2o.fossil
else
    fossil up
fi

# get, build, and install Gforth

which gforth 1>/dev/null 2>/dev/null && GF=$(gforth --version 2>&1 | tr ' ' '-')
(which gforth 1>/dev/null 2>/dev/null && test '!' "$GF" "<" "$GFORTH") || (
    wget -c http://www.complang.tuwien.ac.at/forth/gforth/Snapshots/$GFORTH.tar.gz
    tar zxf $GFORTH.tar.gz
    build $GFORTH
)

# get, build, and install ed25519-donna, keccak and threefish

git-get https://github.com/forthy42 ed25519-donna

# build and install libraries

for i in ed25519-donna keccak threefish
do
    build $i
done

# make sure libraries are found
sudo /sbin/ldconfig

# we test for an existing Gforth that can load net2o.fs
# if the snapshot doesn't, try the git version

gforth-fast net2o.fs -e bye 1>/dev/null 2>/dev/null || (
    git-get git://git.savannah.gnu.org gforth
    build gforth
)
