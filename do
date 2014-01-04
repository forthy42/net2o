#!/bin/bash

echo "This script builds net2o from scratch"

GFORTH=gforth-0.7.9_20131227

# get net2o itself

fossil clone http://fossil.net2o.de/net2o net2o.fossil
fossil open net2o.fossil

# get, build, and install Gforth

which gforth 1>/dev/null 2>/dev/null || (\
GF=$(gforth --version 2>&1 | tr ' ' '-')
if [ "$GF" != "GFORTH" ]
then
    wget http://www.complang.tuwien.ac.at/forth/gforth/Snapshots/$GFORTH.tar.gz
    (tar zxf $GFORTH.tar.gz; cd $GFORTH; ./configure && make && sudo make install)
fi)

# get, build, and install ed25519-donna

git clone https://github.com/forthy42/ed25519-donna.git -b bernd
(cd ed25519-donna; autoconf && ./configure && make && sudo make install)

# build and install keccak

(cd keccak; ./autogen.sh && make && sudo make install)