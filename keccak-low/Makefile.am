# Automake file for keccak primitives

## Place generated object files (.o) into the same directory as their source
## files, in order to avoid collisions when non-recursive make is used.
AUTOMAKE_OPTIONS = subdir-objects

## Additional flags to pass to aclocal when it is invoked automatically at
## make time. The ${ACLOCAL_FLAGS} variable is picked up from the environment
## to provide a way for the user to supply additional arguments.
ACLOCAL_AMFLAGS = ${ACLOCAL_FLAGS} -I m4

if X64
lib_LTLIBRARIES = libkeccak_x86_64.la libkeccak_AVX2.la libkeccak_AVX512.la libkeccak_64.la
libkeccak_x86_64_la_SOURCES = KeccakP-1600/x86-64/KeccakP-1600-x86-64-gas.s
libkeccak_x86_64_la_LDFLAGS = -version-info ${VERSION} -no-undefined
libkeccak_x86_64_la_CFLAGS = -O3
libkeccak_x86_64_la_CCASFLAGS =
libkeccak_AVX2_la_SOURCES = KeccakP-1600/AVX2/KeccakP-1600-AVX2.s
libkeccak_AVX2_la_LDFLAGS = -version-info ${VERSION} -no-undefined
libkeccak_AVX2_la_CFLAGS = -O3
libkeccak_AVX2_la_CCASFLAGS = -mavx2
libkeccak_AVX512_la_SOURCES = KeccakP-1600/AVX512/KeccakP-1600-AVX512.s
libkeccak_AVX512_la_LDFLAGS = -version-info ${VERSION} -no-undefined
libkeccak_AVX512_la_CFLAGS = -O3
libkeccak_AVX512_la_CCASFLAGS = -mavx512f -mavx512vl
libkeccak_64_la_CFLAGS = -O3 -mbmi -mbmi2 -Icommon -IKeccakP-1600/common
else
libkeccak_64_la_CFLAGS = -O3 -Icommon -IKeccakP-1600/common
endif
if ARM64
lib_LTLIBRARIES = libkeccak_ARMv8A.la libkeccak_64.la
libkeccak_ARMv8A_la_SOURCES = KeccakP-1600/ARMv8A/KeccakP-1600-armv8a-neon.s
libkeccak_ARMv8A_la_LDFLAGS = -version-info ${VERSION} -no-undefined
libkeccak_ARMv8A_la_CFLAGS = -O3
libkeccak_ARMv8A_la_CCASFLAGS =
endif
libkeccak_64_la_SOURCES = KeccakP-1600/plain-64bits/KeccakP-1600-opt64.c
libkeccak_64_la_LDFLAGS = -version-info ${VERSION} -no-undefined
#endif
include_HEADERS = KeccakP-1600/compact/KeccakP-1600-SnP.h


install-exec-hook:
	-/sbin/ldconfig $(libdir)
