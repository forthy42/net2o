#!/bin/bash

echo "This script builds net2o from scratch"

GFORTH=gforth-0.7.9_20131227

# get net2o itself

if [ ! -f net2o.fossil ]
then
    fossil clone http://fossil.net2o.de/net2o net2o.fossil
    fossil open net2o.fossil
fi

# get, build, and install Gforth
# we test for an existing Gforth that can load net2o.fs

which gforth 1>/dev/null 2>/dev/null && GF=$(gforth --version 2>&1 | tr ' ' '-')
(which gforth 1>/dev/null 2>/dev/null && gforth-fast net2o.fs -e bye && test "$GF" == "$GFORTH") || (
    wget -c http://www.complang.tuwien.ac.at/forth/gforth/Snapshots/$GFORTH.tar.gz
    (tar zxf $GFORTH.tar.gz; cd $GFORTH; ./configure && make && sudo make install)
)

# get, build, and install ed25519-donna

git clone https://github.com/forthy42/ed25519-donna.git -b bernd
(cd ed25519-donna; autoconf && ./configure && make && sudo make install)

# build and install keccak

(cd keccak; ./autogen.sh && make && sudo make install)
