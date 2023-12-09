## keccac Autoconf

AC_INIT([keccaklow], [1:0], [bernd@net2o.de], [keccak primitives],
        [https://github.com/XKCP/XKCP])
AC_PREREQ([2.59])
AM_INIT_AUTOMAKE([1.10 -Wall no-define])
AC_CONFIG_MACRO_DIR([m4])
AC_CONFIG_HEADERS([config.h])

CFLAGS=${CFLAGS--O3}

AC_PROG_CC
AM_PROG_AS
m4_ifdef([AM_PROG_AR], [AM_PROG_AR])
LT_INIT

# special optimizations for some plattforms
arm64=false
arm32=false
x32=false
x64=false

AC_CHECK_SIZEOF(void *)
case "$ac_cv_sizeof_void_p" in
    4)
        bits=32
	;;
    8)
        bits=64
	;;
esac

case "$host" in
    aarch64*)
	eval arm$bits=true
	;;
    arm*)
	arm32=true
	CCASFLAGS=`echo $CCASFLAGS | sed -e 's/ -mfpu=vfp / /g'`
	;;
    x86_64*)
        eval x$bits=true
	;;
    i*86*)
	x32=true
	;;
esac

AC_ARG_ENABLE([bit64],
[  --enable-bit64    Turn on compilation for 64 bits],
[case "${enableval}" in
  yes) bit64=true ;;
  no)  bit64=false ;;
  *) AC_MSG_ERROR([bad value ${enableval} for --enable-bit64]) ;;
esac],)
AM_CONDITIONAL([bit64], [test x$bits = x64])

AC_ARG_ENABLE([x64],
[  --enable-x64    Turn on compilation for amd64/x86_64],
[case "${enableval}" in
  yes) x64=true ;;
  no)  x64=false ;;
  *) AC_MSG_ERROR([bad value ${enableval} for --enable-x64]) ;;
esac],)
AM_CONDITIONAL([X64], [test x$x64 = xtrue])

avx2=no
avx512=no
if test x$x64 = xtrue
then
    AC_MSG_CHECKING([if $CC understands -mavx2])
    CFLAGS_1="$CFLAGS"
    CFLAGS="$CFLAGS -mavx2"
    AC_COMPILE_IFELSE([AC_LANG_PROGRAM([[]], [[]])],[avx2=yes],[avx2=no])
    CFLAGS="$CFLAGS_1"
    AC_MSG_RESULT($avx2)

    AC_MSG_CHECKING([if $CC understands -mavx512f -mavx512vl])
    CFLAGS_1="$CFLAGS"
    CFLAGS="$CFLAGS -mavx512f -mavx512vl"
    AC_COMPILE_IFELSE([AC_LANG_PROGRAM([[]], [[]])],[avx512=yes],[avx512=no])
    CFLAGS="$CFLAGS_1"
    AC_MSG_RESULT($avx512)
fi
AM_CONDITIONAL([avx2], [test x$avx2 = xyes])
AM_CONDITIONAL([avx512], [test x$avx512 = xyes])

AC_ARG_ENABLE([x86],
[  --enable-x86    Turn on compilation for i686],
[case "${enableval}" in
  yes) x32=true ;;
  no)  x32=false ;;
  *) AC_MSG_ERROR([bad value ${enableval} for --enable-x86]) ;;
esac],)
AM_CONDITIONAL([X86], [test x$x32 = xtrue])

AC_ARG_ENABLE([arm64],
[  --enable-arm64    Turn on compilation for arm64],
[case "${enableval}" in
  yes) arm64=true ;;
  no)  arm64=false ;;
  *) AC_MSG_ERROR([bad value ${enableval} for --enable-arm64]) ;;
esac],)
AM_CONDITIONAL([ARM64], [test x$arm64 = xtrue])

AC_ARG_ENABLE([arm32],
[  --enable-arm32    Turn on compilation for arm32],
[case "${enableval}" in
  yes) arm32=true ;;
  no)  arm32=false ;;
  *) AC_MSG_ERROR([bad value ${enableval} for --enable-arm32]) ;;
esac],)
AM_CONDITIONAL([ARM32], [test x$arm32 = xtrue])

AC_SUBST(CC)
AC_SUBST(AS)
AC_SUBST(LIBTOOL)
AC_CONFIG_FILES([Makefile])
AC_OUTPUT
