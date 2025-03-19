#!/bin/bash -e
test -n "$srcdir" || srcdir=`dirname "$0"`
test -n "$srcdir" || srcdir=.

function git-get {
    purl=$1
    pname=$2
    shift; shift
    echo "=== git clone $purl/$pname.git $* ==="
    if [ -d $pname ]
    then
        (cd $pname; git pull -f)
    else
        git clone $purl/$pname.git $*
    fi
}

if [ ! -f $srcdir/ed25519-donna/ed25519-prims.h ]
then
    (cd $srcdir; git-get https://git.net2o.de/bernd ed25519-donna)
fi

libtoolize --force --copy --install || glibtoolize --force --copy --install
autoreconf --force --install --verbose "$srcdir"
for i in ./*/autogen.sh
do
    echo "autogen.sh in ${i%autogen.sh}"
    (cd ${i%autogen.sh}; ./autogen.sh)
done
test -n "$NOCONFIGURE" || "$srcdir/configure" "$@"
