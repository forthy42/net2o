/*
The Keccak sponge function, designed by Guido Bertoni, Joan Daemen,
Michaël Peeters and Gilles Van Assche. For more information, feedback or
questions, please refer to our website: http://keccak.noekeon.org/

Implementation by the designers,
hereby denoted as "the implementer".

To the extent possible under law, the implementer has waived all copyright
and related or neighboring rights to the source code in this file.
http://creativecommons.org/publicdomain/zero/1.0/
*/

#define STATEI unsigned long long int
#include <string.h>
#include "brg_endian.h"
#include "KeccakF-1600-opt64-settings.h"
#include "KeccakF-1600.h"

#if defined(__GNUC__)
#define ALIGN __attribute__ ((aligned(32)))
#elif defined(_MSC_VER)
#define ALIGN __declspec(align(32))
#else
#define ALIGN
#endif

#if defined(UseSSE)
    #include <x86intrin.h>
    typedef __m128i V64;
    typedef __m128i V128;
    typedef union {
        V128 v128;
        UINT64 v64[2];
    } V6464;

    #define ANDnu64(a, b)       _mm_andnot_si128(a, b)
    #define LOAD64(a)           _mm_loadl_epi64((const V64 *)&(a))
    #define CONST64(a)          _mm_loadl_epi64((const V64 *)&(a))
    #define ROL64(a, o)         _mm_or_si128(_mm_slli_epi64(a, o), _mm_srli_epi64(a, 64-(o)))
    #define STORE64(a, b)       _mm_storel_epi64((V64 *)&(a), b)
    #define XOR64(a, b)         _mm_xor_si128(a, b)
    #define XOReq64(a, b)       a = _mm_xor_si128(a, b)
    #define SHUFFLEBYTES128(a, b)   _mm_shuffle_epi8(a, b)

    #define ANDnu128(a, b)      _mm_andnot_si128(a, b)
    #define LOAD6464(a, b)      _mm_set_epi64((__m64)(a), (__m64)(b))
    #define CONST128(a)         _mm_load_si128((const V128 *)&(a))
    #define LOAD128(a)          _mm_load_si128((const V128 *)&(a))
    #define LOAD128u(a)         _mm_loadu_si128((const V128 *)&(a))
    #define ROL64in128(a, o)    _mm_or_si128(_mm_slli_epi64(a, o), _mm_srli_epi64(a, 64-(o)))
    #define STORE128(a, b)      _mm_store_si128((V128 *)&(a), b)
    #define XOR128(a, b)        _mm_xor_si128(a, b)
    #define XOReq128(a, b)      a = _mm_xor_si128(a, b)
    #define GET64LOLO(a, b)     _mm_unpacklo_epi64(a, b)
    #define GET64HIHI(a, b)     _mm_unpackhi_epi64(a, b)
    #define COPY64HI2LO(a)      _mm_shuffle_epi32(a, 0xEE)
    #define COPY64LO2HI(a)      _mm_shuffle_epi32(a, 0x44)
    #define ZERO128()           _mm_setzero_si128()

    #ifdef UseOnlySIMD64
    #include "KeccakF-1600-simd64.macros"
    #else
ALIGN const UINT64 rho8_56[2] = {0x0605040302010007, 0x080F0E0D0C0B0A09};
    #include "KeccakF-1600-simd128.macros"
    #endif

    #ifdef UseBebigokimisa
    #error "UseBebigokimisa cannot be used in combination with UseSSE"
    #endif
#elif defined(UseXOP)
    #include <x86intrin.h>
    typedef __m128i V64;
    typedef __m128i V128;
   
    #define LOAD64(a)           _mm_loadl_epi64((const V64 *)&(a))
    #define CONST64(a)          _mm_loadl_epi64((const V64 *)&(a))
    #define STORE64(a, b)       _mm_storel_epi64((V64 *)&(a), b)
    #define XOR64(a, b)         _mm_xor_si128(a, b)
    #define XOReq64(a, b)       a = _mm_xor_si128(a, b)

    #define ANDnu128(a, b)      _mm_andnot_si128(a, b)
    #define LOAD6464(a, b)      _mm_set_epi64((__m64)(a), (__m64)(b))
    #define CONST128(a)         _mm_load_si128((const V128 *)&(a))
    #define LOAD128(a)          _mm_load_si128((const V128 *)&(a))
    #define LOAD128u(a)         _mm_loadu_si128((const V128 *)&(a))
    #define STORE128(a, b)      _mm_store_si128((V128 *)&(a), b)
    #define XOR128(a, b)        _mm_xor_si128(a, b)
    #define XOReq128(a, b)      a = _mm_xor_si128(a, b)
    #define ZERO128()           _mm_setzero_si128()

    #define SWAP64(a)           _mm_shuffle_epi32(a, 0x4E)
    #define GET64LOLO(a, b)     _mm_unpacklo_epi64(a, b)
    #define GET64HIHI(a, b)     _mm_unpackhi_epi64(a, b)
    #define GET64LOHI(a, b)     ((__m128i)_mm_blend_pd((__m128d)a, (__m128d)b, 2))
    #define GET64HILO(a, b)     SWAP64(GET64LOHI(b, a))
    #define COPY64HI2LO(a)      _mm_shuffle_epi32(a, 0xEE)
    #define COPY64LO2HI(a)      _mm_shuffle_epi32(a, 0x44)
 
    #define ROL6464same(a, o)   _mm_roti_epi64(a, o)
    #define ROL6464(a, r1, r2)  _mm_rot_epi64(a, CONST128( rot_##r1##_##r2 ))
ALIGN const UINT64 rot_0_20[2]  = { 0, 20};
ALIGN const UINT64 rot_44_3[2]  = {44,  3};
ALIGN const UINT64 rot_43_45[2] = {43, 45};
ALIGN const UINT64 rot_21_61[2] = {21, 61};
ALIGN const UINT64 rot_14_28[2] = {14, 28};
ALIGN const UINT64 rot_1_36[2]  = { 1, 36};
ALIGN const UINT64 rot_6_10[2]  = { 6, 10};
ALIGN const UINT64 rot_25_15[2] = {25, 15};
ALIGN const UINT64 rot_8_56[2]  = { 8, 56};
ALIGN const UINT64 rot_18_27[2] = {18, 27};
ALIGN const UINT64 rot_62_55[2] = {62, 55};
ALIGN const UINT64 rot_39_41[2] = {39, 41};

#if defined(UseSimulatedXOP)
    // For debugging purposes, when XOP is not available
    #undef ROL6464
    #undef ROL6464same
    #define ROL6464same(a, o)   _mm_or_si128(_mm_slli_epi64(a, o), _mm_srli_epi64(a, 64-(o)))
    V128 ROL6464(V128 a, int r0, int r1)
    {
        V128 a0 = ROL64(a, r0);
        V128 a1 = COPY64HI2LO(ROL64(a, r1));
        return GET64LOLO(a0, a1);
    }
#endif
    
    #include "KeccakF-1600-xop.macros"

    #ifdef UseBebigokimisa
    #error "UseBebigokimisa cannot be used in combination with UseXOP"
    #endif
#elif defined(UseMMX)
    #include <mmintrin.h>
    typedef __m64 V64;
    #define ANDnu64(a, b)       _mm_andnot_si64(a, b)

    #if (defined(_MSC_VER) || defined (__INTEL_COMPILER))
        #define LOAD64(a)       *(V64*)&(a)
        #define CONST64(a)      *(V64*)&(a)
        #define STORE64(a, b)   *(V64*)&(a) = b
    #else
        #define LOAD64(a)       (V64)a
        #define CONST64(a)      (V64)a
        #define STORE64(a, b)   a = (UINT64)b
    #endif
    #define ROL64(a, o)         _mm_or_si64(_mm_slli_si64(a, o), _mm_srli_si64(a, 64-(o)))
    #define XOR64(a, b)         _mm_xor_si64(a, b)
    #define XOReq64(a, b)       a = _mm_xor_si64(a, b)

    #include "KeccakF-1600-simd64.macros"

    #ifdef UseBebigokimisa
    #error "UseBebigokimisa cannot be used in combination with UseMMX"
    #endif
#else
    #if defined(_MSC_VER)
    #define ROL64(a, offset) _rotl64(a, offset)
    #elif defined(UseSHLD)
      #define ROL64(x,N) ({ \
        register UINT64 __out; \
        register UINT64 __in = x; \
        __asm__ ("shld %2,%0,%0" : "=r"(__out) : "0"(__in), "i"(N)); \
        __out; \
      })
    #else
    #define ROL64(a, offset) ((((UINT64)a) << offset) ^ (((UINT64)a) >> (64-offset)))
    #endif

    #include "KeccakF-1600-64.macros"
#endif

#include "KeccakF-1600-unrolling.macros"

void KeccakF(keccak_state state, int round)
{
    declareABCDE
#if (Unrolling != 24)
    unsigned int i;
#endif

    copyFromState(A, state)
    rounds(round)
#if defined(UseMMX)
    _mm_empty();
#endif
}

void KeccakInitializeState(keccak_state state)
{
    memset(state, 0, 200);
#ifdef UseBebigokimisa
    ((UINT64*)state)[ 1] = ~(UINT64)0;
    ((UINT64*)state)[ 2] = ~(UINT64)0;
    ((UINT64*)state)[ 8] = ~(UINT64)0;
    ((UINT64*)state)[12] = ~(UINT64)0;
    ((UINT64*)state)[17] = ~(UINT64)0;
    ((UINT64*)state)[20] = ~(UINT64)0;
#endif
}

void fromBytesToWord(UINT64 *word, const UINT8 *bytes)
{
    unsigned int i;

    *word = 0;
    for(i=0; i<(64/8); i++)
        *word |= (UINT64)(bytes[i]) << (8*i);
}

void fromWordToBytes(UINT8 *bytes, const UINT64 word)
{
    unsigned int i;

    for(i=0; i<(64/8); i++)
        bytes[i] = (word >> (8*i)) & 0xFF;
}

void KeccakInitialize()
{
}

void KeccakExtract(keccak_state state, UINT64 *data, int byteCount)
{
  UINT64 m = ~(UINT64)0;
#if (PLATFORM_BYTE_ORDER == IS_LITTLE_ENDIAN)
  memmove(data, state, byteCount);
#else
  int i;
  
  for(i=0; i<byteCount-7; i+=8)
    fromWordToBytes(data+(i>>3), ((const UINT64*)state)[i>>3]);
#endif
#ifdef UseBebigokimisa
#if (PLATFORM_BYTE_ORDER == IS_LITTLE_ENDIAN)
  m >>= ((8-byteCount) & 7)*8;
#else
  m <<= ((8-byteCount) & 7)*8;
#endif
  switch((byteCount+7)>>3) {
  case 25: case 24: case 23: case 22: m = ~(UINT64)0;
  case 21:
    data[20] ^= m; m = ~(UINT64)0;
  case 20: case 19: m = ~(UINT64)0; case 18:
    data[17] ^= m; m = ~(UINT64)0;
  case 17: case 16: case 15: case 14: m = ~(UINT64)0; case 13:
    data[12] ^= m; m = ~(UINT64)0;
  case 12: case 11: case 10: m = ~(UINT64)0; case 9:
    data[ 8] ^= m; m = ~(UINT64)0;
  case 8: case 7: case 6: case 5: case 4: m = ~(UINT64)0; case 3:
    data[ 2] ^= m; m = ~(UINT64)0;
  case 2:
    data[ 1] ^= m;
  }
#endif
}

void KeccakAbsorb(keccak_state state, UINT64 *data, int byteCount)
{
  int i;
  UINT64 m = ~(UINT64)0;
  for(i=0; i<byteCount-7; i+=8) {
#if (PLATFORM_BYTE_ORDER == IS_LITTLE_ENDIAN)
    state[i>>3] ^= data[i>>3];
#else
    UINT64 tmp;
    fromWordToBytes(&tmp, data[i>>3]);
    state[i>>3] ^= tmp;
#endif
  }
#if (PLATFORM_BYTE_ORDER == IS_LITTLE_ENDIAN)
  m >>= ((8-byteCount) & 7)*8;
  if(byteCount & 7)
    state[i>>3] ^= data[i>>3] & m;
#else
  m <<= ((8-byteCount) & 7)*8;
  if(byteCount & 7) {
    fromWordToBytes(&tmp, data[i>>3] & m);
    state[i>>3] ^= tmp;
  }
#endif
}

void KeccakEncrypt(keccak_state state, UINT64 *data, int byteCount)
{
  int i;
  UINT64 m = ~(UINT64)0;
  for(i=0; i<byteCount-7; i+=8) {
#if (PLATFORM_BYTE_ORDER == IS_LITTLE_ENDIAN)
    data[i>>3] = state[i>>3] ^= data[i>>3];
#else
    UINT64 tmp;
    fromWordToBytes(&tmp, data[i>>3]);
    tmp = state[i>>3] ^= tmp;
    fromWordToBytes(data+(i>>3), tmp);
#endif
  }
#if (PLATFORM_BYTE_ORDER == IS_LITTLE_ENDIAN)
  m >>= ((8-byteCount) & 7)*8;
  if(byteCount & 7) {
    state[i>>3] ^= data[i>>3] & m;
    data[i>>3] = (data[i>>3] & ~m) | (state[i>>3] & m);
  }
#else
  m <<= ((8-byteCount) & 7)*8;
  if(byteCount & 7) {
    UINT64 tmp, tmp2;
    fromWordToBytes(&tmp, data[i>>3] & m);
    tmp = state[i>>3] ^= tmp;
    fromWordToBytes(&tmp2, tmp);
    data[i>>3] = (data[i>>3] & ~m) | (tmp2 & m);
  }
#endif
#ifdef UseBebigokimisa
  switch((byteCount+7)>>3) {
  case 25: case 24: case 23: case 22: m = ~(UINT64)0;
  case 21:
    data[20] ^= m; m = ~(UINT64)0;
  case 20: case 19: m = ~(UINT64)0; case 18:
    data[17] ^= m; m = ~(UINT64)0;
  case 17: case 16: case 15: case 14: m = ~(UINT64)0; case 13:
    data[12] ^= m; m = ~(UINT64)0;
  case 12: case 11: case 10: m = ~(UINT64)0; case 9:
    data[ 8] ^= m; m = ~(UINT64)0;
  case 8: case 7: case 6: case 5: case 4: m = ~(UINT64)0; case 3:
    data[ 2] ^= m; m = ~(UINT64)0;
  case 2:
    data[ 1] ^= m;
  }
#endif
}

void KeccakDecrypt(keccak_state state, UINT64 *data, int byteCount)
{
  int i;
  UINT64 m = ~(UINT64)0;
  UINT64 tmp;
  for(i=0; i<byteCount-7; i+=8) {
#if (PLATFORM_BYTE_ORDER == IS_LITTLE_ENDIAN)
    tmp = data[i>>3] ^ state[i>>3];
    state[i>>3] = data[i>>3];
    data[i>>3] = tmp;
#else
    UINT64 tmp1;
    fromWordToBytes(&tmp, data[i>>3]);
    tmp1 = tmp ^ state[i>>3];
    state[i>>3] = tmp;
    fromWordToBytes(data+(i>>3), tmp1);
#endif
  }
#if (PLATFORM_BYTE_ORDER == IS_LITTLE_ENDIAN)
  m >>= ((8-byteCount) & 7)*8;
  if(byteCount & 7) {
    tmp = data[i>>3] ^ state[i>>3];
    state[i>>3] = (data[i>>3] & m) | (state[i>>3] & ~m);
    data[i>>3] = (tmp & m) | (data[i>>3] & ~m);
  }
#else
  m <<= ((8-byteCount) & 7)*8;
  if(bytecount & 7) {
    UINT64 tmp1;
    fromWordToBytes(&tmp1, data[i>>3]);
    tmp = tmp1 ^ state[i>>3];
    state[i>>3] = (tmp & m) | (state[i>>3] & ~m);
    tmp1 = (tmp & m) | (tmp1 & ~m);
    fromWordToBytes(data+(i>>3), tmp1);
  }
#endif
#ifdef UseBebigokimisa
  switch((byteCount+7)>>3) {
  case 25: case 24: case 23: case 22: m = ~(UINT64)0;
  case 21:
    data[20] ^= m; state[20] ^= m; m = ~(UINT64)0;
  case 20: case 19: m = ~(UINT64)0; case 18:
    data[17] ^= m; state[17] ^= m; m = ~(UINT64)0;
  case 17: case 16: case 15: case 14: m = ~(UINT64)0; case 13:
    data[12] ^= m; state[12] ^= m; m = ~(UINT64)0;
  case 12: case 11: case 10: m = ~(UINT64)0; case 9:
    data[ 8] ^= m; state[ 8] ^= m; m = ~(UINT64)0;
  case 8: case 7: case 6: case 5: case 4: m = ~(UINT64)0; case 3:
    data[ 2] ^= m; state[ 2] ^= m; m = ~(UINT64)0;
  case 2:
    data[ 1] ^= m; state[ 1] ^= m;
  }
#endif
}
