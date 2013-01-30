#!/bin/sh -e
test -n "$srcdir" || srcdir=`dirname "$0"`
test -n "$srcdir" || srcdir=.

aclocal --install -I m4
autoreconf --force --install --verbose "$srcdir"
automake --add-missing
test -n "$NOCONFIGURE" || "$srcdir/configure" "$@"
