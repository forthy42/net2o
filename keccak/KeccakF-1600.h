/* KeccakF interface */

#ifndef _KeccakF_1600_h_
#define _KeccakF_1600_h_

typedef unsigned char UINT8;
typedef unsigned short UINT16;
typedef unsigned int UINT32;
typedef unsigned long long int UINT64;
typedef unsigned long keccak_state[25*sizeof(UINT64)/sizeof(unsigned long)];

void KeccakInitialize();
void KeccakF(keccak_state state);
void KeccakInitializeState(keccak_state state);
void KeccakExtract(keccak_state state, UINT64 *data, unsigned int laneCount);
void KeccakAbsorb(keccak_state state, UINT64 *data, unsigned int laneCount);
void KeccakEncrypt(keccak_state state, UINT64 *data, unsigned int laneCount);
void KeccakDecrypt(keccak_state state, UINT64 *data, unsigned int laneCount);

#endif
