## keccac Autoconf

AC_INIT([KECCAK], [3:3], [bernd@net2o.de], [keccak for Linux],
        [http://keccak.noekeon.org/])
AC_PREREQ([2.59])
AM_INIT_AUTOMAKE([1.10 -Wall no-define])
AC_CONFIG_MACRO_DIR([m4])
AC_CONFIG_HEADERS([config.h])

CFLAGS=${CFLAGS--O3}

AC_PROG_CC
AM_PROG_AS
m4_ifdef([AM_PROG_AR], [AM_PROG_AR])
LT_INIT

AC_CHECK_SIZEOF(void *)
case "$ac_cv_sizeof_void_p" in
  4)
    ws=${ws-opt32}
    ;;
  8)
    ws=${ws-opt64}
    ;;
esac

flags="$CFLAGS"
nomflags="`echo $CFLAGS | sed -e 's/-m[[^ ]]* //g'`"

# special optimizations for some plattforms
case "$host_alias" in
     arm-linux*)
	flags="$CFLAGS"
	asmflags="$CFLAGS"
	fastasmflags="-O3 -mfpu=neon $nomflags"
	fastflags="-O3 -mfpu=neon $nomflags"
	as="inplace-armgcc-ARMv7A-NEON"
	enable_asm=yes
	enable_fast=yes
	plattform=neon
	CFLAGS=""
	CCASFLAGS=""
	;;
     arm-*)
	asmflags="-O3 -mfpu=neon $nomflags"
	flags="-O3 -mfpu=neon $nomflags"
	as="inplace-armgcc-ARMv7A-NEON"
	enable_asm=yes
	plattform=neon
	CFLAGS=""
	CCASFLAGS=""
	;;
    i686-linux*|i686-*cygwin*)
	# if you don't want sse, specify i586-linux-gnu
	enable_fast=yes
        flags="$CFLAGS"
	fastflags="-O3 -m32 -mssse3 -DUseSSE $nomflags"
	ws=opt64
	CFLAGS=""
	;;
    i686-*)
	# if you don't want sse, specify i586-linux-gnu
        flags="-O3 -m32 -mssse3 -DUseSSE $nomflags"
	ws=opt64
	CFLAGS=""
	;;
esac

AC_ARG_ENABLE([asm],
[  --enable-asm    Turn on assembler sources],
[case "${enableval}" in
  yes) asm=true ;;
  no)  asm=false ;;
  *) AC_MSG_ERROR([bad value ${enableval} for --enable-asm]) ;;
esac],[asm=false])
AM_CONDITIONAL([ASM], [test x$asm = xtrue])
AC_ARG_ENABLE([fast],
[  --enable-fast    Turn on optional fast library with non-standard CPU features],
[case "${enableval}" in
  yes) fast=true ;;
  no)  fast=false ;;
  *) AC_MSG_ERROR([bad value ${enableval} for --enable-fast]) ;;
esac],[fast=false])
AM_CONDITIONAL([FAST], [test x$fast = xtrue])

#x86 on x86_64 plattforms:
#./configure --host=i686-linux-gnu CFLAGS="-O3 -m32 -mssse3 -DUseSSE" ws=opt64
#ARM with NEON
#./configure --enable-asm as="inplace-armgcc-ARMv7A-NEON" plattform=neon CC="arm-linux-androideabi-gcc -mfpu=neon-vfpv4" --host=arm-linux-androideabi

PRECC=${CC%gcc*}
PRECLANG=${PRECC%clang*}
dnl MacOS X has a libtool that does something else
AC_CHECK_PROGS(GNU_LIBTOOL,"glibtool --tag=CC" libtool)

LIBTOOL=${PRECLANG}${LIBTOOL}
echo "Libtool: $PRECC, $PRECLANG, $LIBTOOL"

AC_SUBST(nomflags)
AC_SUBST(fastflags)
AC_SUBST(fastasmflags)
AC_SUBST(flags)
AC_SUBST(asmflags)
AC_SUBST(ws)
AC_SUBST(as)
AC_SUBST(plattform)
AC_SUBST(LIBTOOL)
AC_CONFIG_FILES([Makefile])
AC_OUTPUT
