# Automake file for bdelta library

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

lib_LTLIBRARIES = libbdelta.la
include_HEADERS = bdelta.h compatibility.h checksum.h
libbdelta_la_SOURCES = libbdelta.cpp
libbdelta_la_LIBADD = 
libbdelta_la_LDFLAGS = -version-info ${VERSION} -no-undefined
libbdelta_la_CXXFLAGS = @flags@

install-exec-hook:
	-/sbin/ldconfig $(libdir)
