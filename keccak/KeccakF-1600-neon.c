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

typedef unsigned long long int STATEI;
#include <string.h>
#include "brg_endian.h"
#include "KeccakF-1600-opt64-settings.h"
#include "KeccakF-1600.h"

void KeccakF(keccak_state state)
{
  KeccakF_armv7a_neon(state);
}

void KeccakInitializeState(keccak_state state)
{
    memset(state, 0, 200);
}

void KeccakInitialize()
{
}

void KeccakExtract(keccak_state state, UINT64 *data, unsigned int byteCount)
{
  memcpy(data, state, byteCount);
}

void KeccakAbsorb(keccak_state state, UINT64 *data, unsigned int byteCount)
{
  unsigned int i;
  UINT64 m = 0xffffffffffffffffull;
  for(i=0; i<byteCount-7; i+=8) {
    state[i>>3] ^= data[i>>3];
  }
  m >>= ((8-byteCount) & 7)*8;
  if(byteCount & 7)
    state[i>>3] ^= data[i>>3] & m;
}

void KeccakEncrypt(keccak_state state, UINT64 *data, unsigned int byteCount)
{
  unsigned int i;
  for(i=0; i<byteCount-7; i+=8) {
    data[i>>3] = state[i>>3] ^= data[i>>3];
  }
  if(byteCount & 7) {
    UINT64 m = 0xffffffffffffffffull >> ((8-byteCount) & 7)*8;
    data[i>>3] = (data[i>>3] & ~m) | (m & (state[i>>3] ^= data[i>>3] & m));
  }
}

void KeccakDecrypt(keccak_state state, UINT64 *data, unsigned int byteCount)
{
  unsigned int i;
  UINT64 tmp;
  for(i=0; i<byteCount-7; i+=8) {
    tmp = data[i>>3] ^ state[i>>3];
    state[i>>3] = data[i>>3];
    data[i>>3] = tmp;
  }
  if(byteCount & 7) {
    UINT64 m = 0xffffffffffffffffull >> ((8-byteCount) & 7)*8;
    tmp = data[i>>3] ^ state[i>>3];
    state[i>>3] = (data[i>>3] & m) | (state[i>>3] & ~m);
    data[i>>3] = (tmp & m) | (data[i>>3] & ~m);
  }
}
