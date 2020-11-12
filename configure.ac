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

AC_INIT([net2o], [0.9.9-20201112top-20201112], [bernd@net2o.de], [net2o], [https://fossil.net2o.de/net2o/reportlist])
AC_PREREQ([2.59])
AC_CONFIG_MACRO_DIR([m4])
AC_USE_SYSTEM_EXTENSIONS
LT_INIT

AC_MSG_CHECKING([for gforth])

GFX=""

case "$CC"
in
    *-m32*)
	GFX="-386"
	;;
esac

AC_CHECK_PROGS(GFORTH, gforth$GFX)
AC_CHECK_PROGS(GFORTHFAST, gforth-fast$GFX)

test -z "$GFORTH" || read dummy GFVER GFARCH <<EOF
$(eval $GFORTH -v 2>&1)
EOF
GFDEBVER=$(echo $GFVER | tr '_' '-')

GFORTH=$(which $GFORTH)
GFORTHFAST=$(which $GFORTHFAST)

FAST=no
case "$host" in
    arm*-*linux*)
	FAST=yes
	;;
    i686-*linux*|i686-*cygwin*)
	FAST=yes
	;;
esac

VERDIR=
NOVERDIR='#'

case "$host" in
    *android*)
	VERDIR='#'
	NOVERDIR=
	;;
esac

AC_SUBST(VERDIR)
AC_SUBST(NOVERDIR)

AC_PROG_CC
AC_PROG_INSTALL

PRECC=${CC%gcc*}
PRECLANG=${PRECC%clang*}
dnl MacOS X has a libtool that does something else
AC_CHECK_PROGS(GNU_LIBTOOL,glibtool libtool)

GNU_LIBTOOL=${PRECLANG}${GNU_LIBTOOL}
echo "Libtool: $PRECC, $PRECLANG, $GNU_LIBTOOL"

AC_ARG_ENABLE(gps,
	AC_HELP_STRING([--enable-gps],
		       [  Enable GPS (default enabled).]),
	,enable_gps=yes)

HAS_GPSD=0
if test x"$enable_gps" = xyes; then
   AC_CHECK_LIB(gps, gps_open, , enable_gps=no)
   if test x"$enable_gps" = xyes; then
      HAS_GPSD=1
   fi
fi
AC_SUBST(HAS_GPSD)

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

AC_CONFIG_SUBDIRS([keccakp threefish ed25519-donna bdelta])

AC_CONFIG_FILES([Makefile n2o qrscan version.fs debian/changelog debian/control])
AC_CONFIG_COMMANDS([stamp-h],[[date > stamp-h
chmod +x n2o qrscan]],[PACKAGE_VERSION=$PACKAGE_VERSION
 machine=$machine
 ARCH=$ARCH])

AC_OUTPUT
