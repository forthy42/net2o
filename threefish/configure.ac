## ExampleLib Example: an example of using Automake to link with a library

AC_INIT([THREEFISH], [1:0], [bernd@net2o.de], [threefish for Linux],
        [https://www.schneier.com/threefish.html])
AC_PREREQ([2.59])
AM_INIT_AUTOMAKE([1.10 -Wall no-define])
AC_CONFIG_MACRO_DIR([m4])
AC_CONFIG_HEADERS([config.h])

CFLAGS=${CFLAGS--O3}

# special optimizations for some plattforms
case "$host_alias" in
    i686-*)
	# if you don't want sse, specify i586-linux-gnu
	CFLAGS="-O3 -m32 -mssse3 -DUseSSE"
	;;
esac

AC_PROG_CC
AM_PROG_AS
m4_ifdef([AM_PROG_AR], [AM_PROG_AR])
LT_INIT

PRECC=${CC%gcc*}
PRECLANG=${PRECC%clang*}
LIBTOOL=${PRECLANG}libtool
echo "Libtool: $PRECC, $PRECLANG, $LIBTOOL"

AC_SUBST(LIBTOOL)
AC_CONFIG_FILES([Makefile])
AC_OUTPUT
