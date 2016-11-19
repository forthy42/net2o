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

void KeccakF(keccak_state state, int round)
{
  KeccakF_armv7a_neon(state, round);
}

void KeccakInitializeState(keccak_state state)
{
    memset(state, 0, 200);
}

void KeccakInitialize()
{
}

void KeccakExtract(keccak_state state, UINT64 *data, int byteCount)
{
  memcpy(data, state, byteCount);
}

void KeccakAbsorb(keccak_state state, UINT64 *data, int byteCount)
{
  int i;
  UINT64 m = 0xffffffffffffffffull;
  keccak_state datai;
  memcpy(datai, data, byteCount);
  for(i=0; i<byteCount-7; i+=8) {
    state[i>>3] ^= datai[i>>3];
  }
  m >>= ((8-byteCount) & 7)*8;
  if(byteCount & 7)
    state[i>>3] ^= datai[i>>3] & m;
}

void KeccakEncrypt(keccak_state state, UINT64 *data, int byteCount)
{
  int i;
  for(i=0; i<byteCount-7; i+=8) {
    data[i>>3] = state[i>>3] ^= data[i>>3];
  }
  if(byteCount & 7) {
    UINT64 m = 0xffffffffffffffffull >> ((8-byteCount) & 7)*8;
    state[i>>3] ^= data[i>>3] & m;
    data[i>>3] = (data[i>>3] & ~m) | (state[i>>3] & m);
  }
}

void KeccakDecrypt(keccak_state state, UINT64 *data, int byteCount)
{
  int i;
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
