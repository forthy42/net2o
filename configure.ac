dnl Process this file with autoconf to produce a configure script.

# Copyright (C) 2015   Bernd Paysan

# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU Affero General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.

# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU Affero General Public License for more details.

# You should have received a copy of the GNU Affero General Public License
# along with this program.  If not, see <http://www.gnu.org/licenses/>.

AC_INIT([net2o], [0.1.2-20160121], [bernd@net2o.de], [net2o], [https://fossil.net2o.de/net2o/reportlist])
AC_PREREQ(2.54)

AC_MSG_CHECKING([for gforth])

GFX=""

case "$CC"
in
    *-m32*)
	GFX="-386"
	;;
esac

GFORTH="`cd / && which gforth$GFX 2>/dev/null`"
GFORTHFAST="`cd / && which gforth-fast$GFX 2>/dev/null`"
test -z "$GFORTH" || read dummy GFVER GFARCH <<EOF
$(eval $GFORTH -v 2>&1)
EOF
GFDEBVER=$(echo $GFVER | tr '_' '-')

FAST=no
case "$host_alias" in
    arm-linux-androideabi)
	FAST=yes
	;;
    i686-linux-android)
	FAST=yes
	;;
esac

AC_PROG_INSTALL

PRECC=${CC%gcc*}
PRECLANG=${PRECC%clang*}
dnl MacOS X has a libtool that does something else
AC_CHECK_PROGS(GNU_LIBTOOL,"glibtool --tag=CC" libtool)

GNU_LIBTOOL=${PRECLANG}${GNU_LIBTOOL}
echo "Libtool: $PRECC, $PRECLANG, $GNU_LIBTOOL"

AC_SUBST(GFORTH)
AC_SUBST(GFORTHFAST)
AC_SUBST(GFVER)
AC_SUBST(GFDEBVER)
AC_SUBST(GFARCH)
AC_SUBST(FAST)
AC_SUBST(GNU_LIBTOOL)

test "x$prefix" = xNONE && prefix=$ac_default_prefix
DATADIR=$(eval echo $(eval echo $datadir))
AC_SUBST(DATADIR)

AC_CONFIG_FILES([Makefile n2o debian/changelog debian/control])
AC_CONFIG_COMMANDS([stamp-h],[[date > stamp-h
chmod +x n2o]],[PACKAGE_VERSION=$PACKAGE_VERSION
 machine=$machine
 ARCH=$ARCH])

AC_OUTPUT
AC_CONFIG_SUBDIRS(keccak threefish ed25519-donna)
