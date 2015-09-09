## ExampleLib Example: an example of using Automake to link with a library

AC_INIT([THREEFISH], [1:0], [bernd@net2o.de], [threefish for Linux],
        [https://www.schneier.com/threefish.html])
AC_PREREQ([2.59])
AM_INIT_AUTOMAKE([1.10 -Wall no-define])
AC_CONFIG_MACRO_DIR([m4])
AC_CONFIG_HEADERS([config.h])

CFLAGS=${CFLAGS--O3}

flags="$CFLAGS"
nomflags="`echo $CFLAGS | sed -e 's/-m[[^ ]]* //g'`"

# special optimizations for some plattforms
case "$host_alias" in
    arm-linux-androideabi)
	flags="$CFLAGS"
	#may try with -mneon-for-64bits
	fastflags="-O3 -mfpu=neon $nomflags"
	enable_fast=yes
	CFLAGS=""
	;;
    arm-*)
	CFLAGS="-O3 -mfpu=neon $nomflags"
	;;
    i686-linux-android)
	flags="$CFLAGS"
	fastflags="-O3 -m32 -mssse3 $nomflags"
	enable_fast=yes
	CFLAGS=""
	;;
    i686-*)
	# if you don't want sse, specify i586-linux-gnu
	CFLAGS="-O3 -m32 -mssse3 $nomflags"
	;;
esac

AC_ARG_ENABLE([fast],
[  --enable-fast    Turn on optional fast library with non-standard CPU features],
[case "${enableval}" in
  yes) fast=true ;;
  no)  fast=false ;;
  *) AC_MSG_ERROR([bad value ${enableval} for --enable-fast]) ;;
esac],[fast=false])
AM_CONDITIONAL([FAST], [test x$fast = xtrue])

AC_PROG_CC
AM_PROG_AS
m4_ifdef([AM_PROG_AR], [AM_PROG_AR])
LT_INIT

PRECC=${CC%gcc*}
PRECLANG=${PRECC%clang*}
LIBTOOL=${PRECLANG}libtool
echo "Libtool: $PRECC, $PRECLANG, $LIBTOOL"

AC_SUBST(fastflags)
AC_SUBST(flags)
AC_SUBST(LIBTOOL)
AC_CONFIG_FILES([Makefile])
AC_OUTPUT
