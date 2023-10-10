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
x86=false
x64=false
plain=true

case "$host" in
    aarch64*)
	arm64=true
	plain=false
	;;
    arm*)
	arm32=true
	plain=false
	;;
    i686-*)
	x86=true
	plain=false
	;;
    x86_64*)
	x64=true
	plain=false
	;;
esac

AC_CHECK_SIZEOF(void *)
case "$ac_cv_sizeof_void_p" in
    4)
	ws=${ws-opt32}
	cvariant=32bits-inplace
	;;
    8)
	ws=${ws-opt64}
	cvariant=64bits
	;;
esac

AC_ARG_ENABLE([x64],
[  --enable-x64    Turn on compilation for amd64/x86_64],
[case "${enableval}" in
  yes) x64=true ;;
  no)  x64=false ;;
  *) AC_MSG_ERROR([bad value ${enableval} for --enable-x64]) ;;
esac],)
AM_CONDITIONAL([X64], [test x$x64 = xtrue])

AC_ARG_ENABLE([x86],
[  --enable-x86    Turn on compilation for amd64/x86_64],
[case "${enableval}" in
  yes) x86=true ;;
  no)  x86=false ;;
  *) AC_MSG_ERROR([bad value ${enableval} for --enable-x86]) ;;
esac],)
AM_CONDITIONAL([X86], [test x$x86 = xtrue])

AC_ARG_ENABLE([arm64],
[  --enable-arm64    Turn on compilation for amd64/x86_64],
[case "${enableval}" in
  yes) arm64=true ;;
  no)  arm64=false ;;
  *) AC_MSG_ERROR([bad value ${enableval} for --enable-arm64]) ;;
esac],)
AM_CONDITIONAL([ARM64], [test x$arm64 = xtrue])

AC_ARG_ENABLE([arm32],
[  --enable-arm32    Turn on compilation for amd64/x86_64],
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
