#!/bin/bash -e
test -n "$srcdir" || srcdir=`dirname "$0"`
test -n "$srcdir" || srcdir=.

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

(cd $srcdir; git-get https://git.net2o.de/bernd ed25519-donna)

libtoolize --force --copy --install || glibtoolize --force --copy --install
autoreconf --force --install --verbose "$srcdir"
for i in ./*/autogen.sh
do
    echo "autogen.sh in ${i%autogen.sh}"
    (cd ${i%autogen.sh}; ./autogen.sh)
done
test -n "$NOCONFIGURE" || "$srcdir/configure" "$@"
