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

typedef unsigned int STATEI;
#include <string.h>
#include "brg_endian.h"
#include "KeccakF-1600-opt32-settings.h"
#include "KeccakF-1600.h"

#ifdef UseInterleaveTables
int interleaveTablesBuilt = 0;
UINT16 interleaveTable[65536];
UINT16 deinterleaveTable[65536];

void buildInterleaveTables()
{
    UINT32 i, j;
    UINT16 x;

    if (!interleaveTablesBuilt) {
        for(i=0; i<65536; i++) {
            x = 0;
            for(j=0; j<16; j++) {
                if (i & (1 << j))
                    x |= (1 << (j/2 + 8*(j%2)));
            }
            interleaveTable[i] = x;
            deinterleaveTable[x] = (UINT16)i;
        }
        interleaveTablesBuilt = 1;
    }
}

#if (PLATFORM_BYTE_ORDER == IS_LITTLE_ENDIAN)

#define xor2bytesIntoInterleavedWords(even, odd, source, j) \
    i##j = interleaveTable[((const UINT16*)source)[j]]; \
    ((UINT8*)even)[j] ^= i##j & 0xFF; \
    ((UINT8*)odd)[j] ^= i##j >> 8;

#define setInterleavedWordsInto2bytes(dest, even, odd, j) \
    d##j = deinterleaveTable[((even >> (j*8)) & 0xFF) ^ (((odd >> (j*8)) & 0xFF) << 8)]; \
    ((UINT16*)dest)[j] = d##j;

#else // (PLATFORM_BYTE_ORDER == IS_BIG_ENDIAN)

#define xor2bytesIntoInterleavedWords(even, odd, source, j) \
    i##j = interleaveTable[source[2*j] ^ ((UINT16)source[2*j+1] << 8)]; \
    *even ^= (i##j & 0xFF) << (j*8); \
    *odd ^= ((i##j >> 8) & 0xFF) << (j*8);

#define setInterleavedWordsInto2bytes(dest, even, odd, j) \
    d##j = deinterleaveTable[((even >> (j*8)) & 0xFF) ^ (((odd >> (j*8)) & 0xFF) << 8)]; \
    dest[2*j] = d##j & 0xFF; \
    dest[2*j+1] = d##j >> 8;

#endif // Endianness

void xor8bytesIntoInterleavedWords(UINT32 *even, UINT32 *odd, const UINT8* source)
{
    UINT16 i0, i1, i2, i3;

    xor2bytesIntoInterleavedWords(even, odd, source, 0)
    xor2bytesIntoInterleavedWords(even, odd, source, 1)
    xor2bytesIntoInterleavedWords(even, odd, source, 2)
    xor2bytesIntoInterleavedWords(even, odd, source, 3)
}

#define xorLanesIntoState(byteCount, state, input) {			\
  int i;								\
  UINT64 tmp=0;								\
  for(i=0; i<(byteCount-7); i+=8)					\
    xor8bytesIntoInterleavedWords(state+(i>>2), state+(i>>2)+1, input+i); \
  memcpy(&tmp, input+i, byteCount & 7);					\
  xor8bytesIntoInterleavedWords(state+(i>>2), state+(i>>2)+1, &tmp);	\
}

void setInterleavedWordsInto8bytes(UINT8* dest, UINT32 even, UINT32 odd)
{
    UINT16 d0, d1, d2, d3;

    setInterleavedWordsInto2bytes(dest, even, odd, 0)
    setInterleavedWordsInto2bytes(dest, even, odd, 1)
    setInterleavedWordsInto2bytes(dest, even, odd, 2)
    setInterleavedWordsInto2bytes(dest, even, odd, 3)
}

#define extractLanes(byteCount, state, data) \
  {					     \
    int i;				     \
    UINT64 tmp=0;			     \
    for(i=0; i<(byteCount-7); i+=8)					\
      setInterleavedWordsInto8bytes(data+i, ((UINT32*)state)[i>>2], ((UINT32*)state)[(i>>2)+1]); \
    setInterleavedWordsInto8bytes(&tmp, ((UINT32*)state)[i>>2], ((UINT32*)state)[(i>>2)+1]); \
    memcpy(data+i, &tmp, byteCount & 7);				\
  }

#else // No interleaving tables

#if (PLATFORM_BYTE_ORDER == IS_LITTLE_ENDIAN)

// Credit: Henry S. Warren, Hacker's Delight, Addison-Wesley, 2002
#define xorInterleavedLE(byteCount, state, input) \
  {							   \
    const UINT32 * pI = (const UINT32 *)input;		   \
    UINT32 * pS = state;				   \
    UINT32 t, x0, x1;					   \
    int i;						   \
    for (i = (byteCount)-8; i >= 0; i-=8)		   \
      {							   \
	x0 = *(pI++);							\
	t = (x0 ^ (x0 >>  1)) & 0x22222222UL;  x0 = x0 ^ t ^ (t <<  1); \
	t = (x0 ^ (x0 >>  2)) & 0x0C0C0C0CUL;  x0 = x0 ^ t ^ (t <<  2); \
	t = (x0 ^ (x0 >>  4)) & 0x00F000F0UL;  x0 = x0 ^ t ^ (t <<  4); \
	t = (x0 ^ (x0 >>  8)) & 0x0000FF00UL;  x0 = x0 ^ t ^ (t <<  8); \
	x1 = *(pI++);							\
	t = (x1 ^ (x1 >>  1)) & 0x22222222UL;  x1 = x1 ^ t ^ (t <<  1); \
	t = (x1 ^ (x1 >>  2)) & 0x0C0C0C0CUL;  x1 = x1 ^ t ^ (t <<  2); \
	t = (x1 ^ (x1 >>  4)) & 0x00F000F0UL;  x1 = x1 ^ t ^ (t <<  4); \
	t = (x1 ^ (x1 >>  8)) & 0x0000FF00UL;  x1 = x1 ^ t ^ (t <<  8); \
	*(pS++) ^= (UINT16)x0 | (x1 << 16);				\
	*(pS++) ^= (x0 >> 16) | (x1 & 0xFFFF0000);			\
      }									\
    x0 = byteCount >= 4 ? *(pI++) : *(pI++) & 0xffffffffu >> (8*((4-byteCount) & 3)); \
    t = (x0 ^ (x0 >>  1)) & 0x22222222UL;  x0 = x0 ^ t ^ (t <<  1);	\
    t = (x0 ^ (x0 >>  2)) & 0x0C0C0C0CUL;  x0 = x0 ^ t ^ (t <<  2);	\
    t = (x0 ^ (x0 >>  4)) & 0x00F000F0UL;  x0 = x0 ^ t ^ (t <<  4);	\
    t = (x0 ^ (x0 >>  8)) & 0x0000FF00UL;  x0 = x0 ^ t ^ (t <<  8);	\
    x1 = byteCount < 4 ? 0 : *(pI++) & 0xffffffffu >> (8*((4-byteCount) & 3)); \
    t = (x1 ^ (x1 >>  1)) & 0x22222222UL;  x1 = x1 ^ t ^ (t <<  1);	\
    t = (x1 ^ (x1 >>  2)) & 0x0C0C0C0CUL;  x1 = x1 ^ t ^ (t <<  2);	\
    t = (x1 ^ (x1 >>  4)) & 0x00F000F0UL;  x1 = x1 ^ t ^ (t <<  4);	\
    t = (x1 ^ (x1 >>  8)) & 0x0000FF00UL;  x1 = x1 ^ t ^ (t <<  8);	\
    *(pS++) ^= (UINT16)x0 | (x1 << 16);					\
    *(pS++) ^= (x0 >> 16) | (x1 & 0xFFFF0000);				\
  }

#define xorLanesIntoState(byteCount, state, input) \
    xorInterleavedLE(byteCount, state, input)

#else // (PLATFORM_BYTE_ORDER == IS_BIG_ENDIAN)

// Credit: Henry S. Warren, Hacker's Delight, Addison-Wesley, 2002
UINT64 toInterleaving(UINT64 x) 
{
   UINT64 t;

   t = (x ^ (x >>  1)) & 0x2222222222222222ULL;  x = x ^ t ^ (t <<  1);
   t = (x ^ (x >>  2)) & 0x0C0C0C0C0C0C0C0CULL;  x = x ^ t ^ (t <<  2);
   t = (x ^ (x >>  4)) & 0x00F000F000F000F0ULL;  x = x ^ t ^ (t <<  4);
   t = (x ^ (x >>  8)) & 0x0000FF000000FF00ULL;  x = x ^ t ^ (t <<  8);
   t = (x ^ (x >> 16)) & 0x00000000FFFF0000ULL;  x = x ^ t ^ (t << 16);

   return x;
}

void xor8bytesIntoInterleavedWords(UINT32* evenAndOdd, const UINT8* source)
{
    // This can be optimized
    UINT64 sourceWord =
        (UINT64)source[0]
        ^ (((UINT64)source[1]) <<  8)
        ^ (((UINT64)source[2]) << 16)
        ^ (((UINT64)source[3]) << 24)
        ^ (((UINT64)source[4]) << 32)
        ^ (((UINT64)source[5]) << 40)
        ^ (((UINT64)source[6]) << 48)
        ^ (((UINT64)source[7]) << 56);
    UINT64 evenAndOddWord = toInterleaving(sourceWord);
    evenAndOdd[0] ^= (UINT32)evenAndOddWord;
    evenAndOdd[1] ^= (UINT32)(evenAndOddWord >> 32);
}

#define xorLanesIntoState(byteCount, state, input)		 \
  {								 \
    int i; UINT64 tmp=0;					 \
    for(i=0; i<(byteCount-7); i+=8)				 \
      xor8bytesIntoInterleavedWords(((char*)state)+i, input+i);	\
    memcpy(state+i, &tmp, byteCount & 7);			 \
    xor8bytesIntoInterleavedWords(((char*)state)+i, &tmp);	 \
  }

#endif // Endianness

// Credit: Henry S. Warren, Hacker's Delight, Addison-Wesley, 2002
UINT64 fromInterleaving(UINT64 x)
{
   UINT64 t;

   t = (x ^ (x >> 16)) & 0x00000000FFFF0000ULL;  x = x ^ t ^ (t << 16);
   t = (x ^ (x >>  8)) & 0x0000FF000000FF00ULL;  x = x ^ t ^ (t <<  8);
   t = (x ^ (x >>  4)) & 0x00F000F000F000F0ULL;  x = x ^ t ^ (t <<  4);
   t = (x ^ (x >>  2)) & 0x0C0C0C0C0C0C0C0CULL;  x = x ^ t ^ (t <<  2);
   t = (x ^ (x >>  1)) & 0x2222222222222222ULL;  x = x ^ t ^ (t <<  1);

   return x;
}

void setInterleavedWordsInto8bytes(UINT8* dest, UINT32* evenAndOdd)
{
#if (PLATFORM_BYTE_ORDER == IS_LITTLE_ENDIAN)
    ((UINT64*)dest)[0] = fromInterleaving(*(UINT64*)evenAndOdd);
#else // (PLATFORM_BYTE_ORDER == IS_BIG_ENDIAN)
    // This can be optimized
    UINT64 evenAndOddWord = (UINT64)evenAndOdd[0] ^ ((UINT64)evenAndOdd[1] << 32);
    UINT64 destWord = fromInterleaving(evenAndOddWord);
    dest[0] = destWord & 0xFF;
    dest[1] = (destWord >> 8) & 0xFF;
    dest[2] = (destWord >> 16) & 0xFF;
    dest[3] = (destWord >> 24) & 0xFF;
    dest[4] = (destWord >> 32) & 0xFF;
    dest[5] = (destWord >> 40) & 0xFF;
    dest[6] = (destWord >> 48) & 0xFF;
    dest[7] = (destWord >> 56) & 0xFF;
#endif // Endianness
}

#define extractLanes(byteCount, state, data) \
    { \
      int i; UINT64 tmp=0;						\
      for(i=0; i<(byteCount-7); i+=8)					\
	setInterleavedWordsInto8bytes(data+i, (UINT32*)state+(i>>2));	\
      setInterleavedWordsInto8bytes(&tmp, (UINT32*)state+(i>>2));	\
      memcpy(data+i, &tmp, byteCount & 7);				\
    }

#endif // With or without interleaving tables

#if defined(_MSC_VER)
#define ROL32(a, offset) _rotl(a, offset)
#elif (defined (__arm__) && defined(__ARMCC_VERSION))
#define ROL32(a, offset) __ror(a, 32-(offset))
#else
#define ROL32(a, offset) ((((UINT32)a) << (offset)) ^ (((UINT32)a) >> (32-(offset))))
#endif

#include "KeccakF-1600-unrolling.macros"
#include "KeccakF-1600-32.macros"

#if (UseSchedule == 3)

#ifdef UseBebigokimisa
#error "No lane complementing with schedule 3."
#endif

#if (Unrolling != 2)
#error "Only unrolling 2 is supported by schedule 3."
#endif

#endif

void KeccakF(keccak_state state)
{
    declareABCDE
#if (Unrolling != 24)
    unsigned int i;
#endif

    copyFromState(A, state)
    rounds
}

void KeccakInitialize()
{
#ifdef UseInterleaveTables
    buildInterleaveTables();
#endif
}

void KeccakInitializeState(keccak_state state)
{
    memset(state, 0, 200);
}

void KeccakExtract(keccak_state state, UINT64 *data, unsigned int byteCount)
{
    extractLanes(byteCount, state, (char*)data)
}

void KeccakAbsorb(keccak_state state, UINT64 *data, unsigned int byteCount)
{
    xorLanesIntoState(byteCount, state, (char*)data)
}

void KeccakEncrypt(keccak_state state, UINT64 *data, unsigned int byteCount)
{
  xorLanesIntoState(byteCount, state, (char*)data);
  extractLanes(byteCount, state, (char*)data);
}

void KeccakDecrypt(keccak_state state, UINT64 *data, unsigned int byteCount)
{
  UINT64 tmp[(byteCount>>3)+1];
  int i;
  tmp[(byteCount>>3)]=0;

  extractLanes(byteCount, state, (char*)tmp);
  for(i=0; i<byteCount-7; i+=8) {
    data[i>>3] ^= tmp[i>>3];
  }
  if(byteCount & 7)
    data[i>>3] ^= tmp[i>>3];
  xorLanesIntoState(byteCount, state, (char*)data);
}
