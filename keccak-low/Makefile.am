# Automake file for keccak primitives

## Place generated object files (.o) into the same directory as their source
## files, in order to avoid collisions when non-recursive make is used.
AUTOMAKE_OPTIONS = subdir-objects

## Additional flags to pass to aclocal when it is invoked automatically at
## make time. The ${ACLOCAL_FLAGS} variable is picked up from the environment
## to provide a way for the user to supply additional arguments.
ACLOCAL_AMFLAGS = ${ACLOCAL_FLAGS} -I m4

if bit64
lib_LTLIBRARIES = libkeccak_64.la
libkeccak_64_la_SOURCES = KeccakP-1600/plain-64bits/KeccakP-1600-opt64.c
libkeccak_64_la_LDFLAGS = -version-info ${VERSION} -no-undefined
libkeccak_64_la_CFLAGS = -O3 -Icommon -IKeccakP-1600/common
else
lib_LTLIBRARIES = libkeccak_32.la
libkeccak_32_la_SOURCES = KeccakP-1600/plain-32bits-inplace/KeccakP-1600-inplace32BI.c
libkeccak_32_la_LDFLAGS = -version-info ${VERSION} -no-undefined
libkeccak_32_la_CFLAGS = -O3 -Icommon -IKeccakP-1600/common
endif

if X64
lib_LTLIBRARIES += libkeccak_x86_64.la libkeccak_AVX2.la libkeccak_AVX512.la
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
#if bit64
#libkeccak_64_la_CFLAGS += -mbmi -mbmi2
#endif
endif
if X86
lib_LTLIBRARIES += libkeccak_XOP.la
libkeccak_XOP_la_SOURCES = KeccakP-1600/XOP/KeccakP-1600-XOP.c
libkeccak_XOP_la_LDFLAGS = -version-info ${VERSION} -no-undefined
libkeccak_XOP_la_CFLAGS = -O3 -IKeccakP-1600/XOP/u6 -Icommon -IKeccakP-1600/common -msse4 -mxop
libkeccak_XOP_la_CCASFLAGS =
endif
if ARM32
lib_LTLIBRARIES += libkeccak_ARMv7A_NEON.la
libkeccak_ARMv7A_NEON_la_SOURCES = KeccakP-1600/ARMv7A_NEON/KeccakP-1600-armv7a-le-neon-gcc.s
libkeccak_ARMv7A_NEON_la_LDFLAGS = -version-info ${VERSION} -no-undefined
libkeccak_ARMv7A_NEON_la_CFLAGS = -O3
libkeccak_ARMv7A_NEON_la_CCASFLAGS =
endif
if ARM64
lib_LTLIBRARIES += libkeccak_ARMv8A.la
libkeccak_ARMv8A_la_SOURCES = KeccakP-1600/ARMv8A/KeccakP-1600-armv8a-neon.s
libkeccak_ARMv8A_la_LDFLAGS = -version-info ${VERSION} -no-undefined
libkeccak_ARMv8A_la_CFLAGS = -O3
libkeccak_ARMv8A_la_CCASFLAGS =
endif

include_HEADERS = KeccakP-1600/compact/KeccakP-1600-SnP.h

install-exec-hook:
	-/sbin/ldconfig $(libdir)
