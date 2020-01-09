/* KeccakF interface */

#ifndef _KeccakF_1600_h_
#define _KeccakF_1600_h_

typedef unsigned char UINT8;
typedef unsigned short UINT16;
typedef unsigned int UINT32;
typedef unsigned long long int UINT64;
#ifndef STATEI
# define STATEI long
#endif
typedef STATEI keccak_state[25*sizeof(UINT64)/sizeof(STATEI)];

void KeccakInitialize();
void KeccakF(keccak_state state, int round);
void KeccakInitializeState(keccak_state state);
void KeccakExtract(keccak_state state, UINT64 *data, int laneCount);
void KeccakAbsorb (keccak_state state, UINT64 *data, int laneCount);
void KeccakEncrypt(keccak_state state, UINT64 *data, int laneCount);
void KeccakDecrypt(keccak_state state, UINT64 *data, int laneCount);

#endif
