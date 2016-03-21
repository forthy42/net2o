# Automake file for keccak

## Place generated object files (.o) into the same directory as their source
## files, in order to avoid collisions when non-recursive make is used.
AUTOMAKE_OPTIONS = subdir-objects

## Additional flags to pass to aclocal when it is invoked automatically at
## make time. The ${ACLOCAL_FLAGS} variable is picked up from the environment
## to provide a way for the user to supply additional arguments.
ACLOCAL_AMFLAGS = ${ACLOCAL_FLAGS} -I m4

## Set the default command-line flags for the C preprocessor to the value
## obtained from pkg-config via PKG_CHECK_MODULES in configure.ac.  These
## flags are passed to the compiler for both C and C++, in addition to the
## language-specific options.
AM_CPPFLAGS = $(EXAMPLE_CFLAGS)

lib_LTLIBRARIES = libkeccak.la
if FAST
lib_LTLIBRARIES += libkeccakfast.la
if ASM
libkeccakfast_la_SOURCES = KeccakF-1600-@as@.s KeccakF-1600-@plattform@.c
else
libkeccakfast_la_SOURCES = KeccakF-1600-@ws@.c
endif
libkeccak_la_SOURCES = KeccakF-1600-@ws@.c
else
if ASM
libkeccak_la_SOURCES = KeccakF-1600-@as@.s KeccakF-1600-@plattform@.c
else
libkeccak_la_SOURCES = KeccakF-1600-@ws@.c
endif
endif
include_HEADERS = KeccakF-1600.h
libkeccak_la_LIBADD = 
libkeccak_la_LDFLAGS = -version-info ${VERSION} -no-undefined
libkeccak_la_CFLAGS = @flags@
libkeccak_la_CCASFLAGS = @asmflags@
libkeccakfast_la_LIBADD = 
libkeccakfast_la_CFLAGS = @fastflags@
libkeccakfast_la_CCASFLAGS = @fastasmflags@
libkeccakfast_la_LDFLAGS = -version-info ${VERSION} #-soname libkeccak.so

install-exec-hook:
	-/sbin/ldconfig $(libdir)
