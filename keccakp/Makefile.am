# Automake file for keccak primitives

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

lib_LTLIBRARIES = libkeccakp.la
if FAST
lib_LTLIBRARIES += libkeccakpfast.la
if ASM
libkeccakpfast_la_SOURCES = KeccakF-1600-@as@.s KeccakF-1600-@plattform@.c
else
libkeccakpfast_la_SOURCES = KeccakF-1600-@ws@.c
endif
libkeccakp_la_SOURCES = KeccakF-1600-@ws@.c
else
if ASM
libkeccakp_la_SOURCES = KeccakF-1600-@as@.s KeccakF-1600-@plattform@.c
else
libkeccakp_la_SOURCES = KeccakF-1600-@ws@.c
endif
endif
include_HEADERS = KeccakF-1600.h
libkeccakp_la_LIBADD = 
libkeccakp_la_LDFLAGS = -version-info ${VERSION} -no-undefined
libkeccakp_la_CFLAGS = @flags@
libkeccakp_la_CCASFLAGS = @asmflags@
libkeccakpfast_la_LIBADD = 
libkeccakpfast_la_CFLAGS = @fastflags@
libkeccakpfast_la_CCASFLAGS = @fastasmflags@
libkeccakpfast_la_LDFLAGS = -version-info ${VERSION} -no-undefined

install-exec-hook:
	-/sbin/ldconfig $(libdir)
