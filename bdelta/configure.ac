## ExampleLib Example: an example of using Automake to link with a library

AC_INIT([BDELTA], [3:0], [bernd@net2o.de], [bdelta library],
        [https://github.com/jjwhitney/BDelta])
AC_PREREQ([2.59])
AM_INIT_AUTOMAKE([1.10 -Wall no-define])
AC_CONFIG_MACRO_DIR([m4])
AC_CONFIG_HEADERS([config.h])

CFLAGS=${CFLAGS--O2}

flags="$CFLAGS"

AC_PROG_CXX
m4_ifdef([AM_PROG_AR], [AM_PROG_AR])
LT_INIT

PRECC=${CC%gcc*}
PRECLANG=${PRECC%clang*}
dnl MacOS X has a libtool that does something else
AC_CHECK_PROGS(GNU_LIBTOOL,"glibtool --tag=CC" libtool)

LIBTOOL=${PRECLANG}${LIBTOOL}
echo "Libtool: $PRECC, $PRECLANG, $LIBTOOL"

AC_SUBST(LIBTOOL)
AC_CONFIG_FILES([Makefile])
AC_OUTPUT
