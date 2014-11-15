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

void KeccakExtract(keccak_state state, UINT64 *data, unsigned int laneCount)
{
  memcpy(data, state, laneCount*8);
}

void KeccakAbsorb(keccak_state state, UINT64 *data, unsigned int laneCount)
{
  unsigned int i;
  for(i=0; i<laneCount; i++) {
    state[i] ^= data[i];
  }
}

void KeccakEncrypt(keccak_state state, UINT64 *data, unsigned int laneCount)
{
  unsigned int i;
  for(i=0; i<laneCount; i++) {
    data[i] = state[i] ^= data[i];
  }
}

void KeccakDecrypt(keccak_state state, UINT64 *data, unsigned int laneCount)
{
  unsigned int i;
  for(i=0; i<laneCount; i++) {
    UINT64 tmp;
    tmp = data[i] ^ state[i];
    state[i] = data[i];
    data[i] = tmp;
  }
}
