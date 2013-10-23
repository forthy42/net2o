# Automake file for libed25519

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

lib_LTLIBRARIES = libed25519.la
libed25519_la_SOURCES = fe25519_getparity.c fe25519_invert.c fe25519_iseq.c fe25519_iszero.c fe25519_neg.c fe25519_pack.c fe25519_pow2523.c fe25519_setint.c fe25519_unpack.c ge25519_add.c ge25519_base.c ge25519_double.c ge25519_double_scalarmult.c ge25519_scalarmult.c ge25519_isneutral.c ge25519_multi_scalarmult.c ge25519_pack.c ge25519_scalarmult_base.c ge25519_unpackneg.c index_heap.c sc25519_from32bytes.c sc25519_from64bytes.c sc25519_from_shortsc.c sc25519_iszero.c sc25519_mul.c sc25519_mul_shortsc.c sc25519_slide.c sc25519_to32bytes.c sc25519_window4.c choose_t.s fe25519_add.s fe25519_freeze.s fe25519_mul.s fe25519_square.s fe25519_sub.s ge25519_add_p1p1.s ge25519_dbl_p1p1.s ge25519_nielsadd2.s ge25519_nielsadd_p1p1.s ge25519_p1p1_to_p2.s ge25519_p1p1_to_p3.s ge25519_pnielsadd_p1p1.s heap_rootreplaced_1limb.s heap_rootreplaced_2limbs.s heap_rootreplaced_3limbs.s heap_rootreplaced.s sc25519_add.s sc25519_barrett.s sc25519_lt.s sc25519_sub_nored.s ull4_mul.s
include_HEADERS = fe25519.h ge25519.h sc25519.h

