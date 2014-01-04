## ExampleLib Example: an example of using Automake to link with a library

AC_INIT([KECCAK], [3.2], [bernd@net2o.de], [keccak for Linux],
        [http://keccak.noekeon.org/])
AC_PREREQ([2.59])
AM_INIT_AUTOMAKE([1.10 -Wall no-define])
AC_CONFIG_MACRO_DIR([m4])
AC_CONFIG_HEADERS([config.h])
AC_PROG_CC
m4_ifdef([AM_PROG_AR], [AM_PROG_AR])
LT_INIT

AC_CHECK_SIZEOF(void *)
case "$ac_cv_sizeof_void_p" in
  4)
    ws=32
    ;;
  8)
    ws=64
    ;;
esac

LIBTOOL=${CC%gcc}libtool
AC_SUBST(ws)
AC_SUBST(LIBTOOL)
AC_CONFIG_FILES([Makefile])
AC_OUTPUT
