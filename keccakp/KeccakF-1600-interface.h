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

#ifndef _KeccakPermutationInterface_h_
#define _KeccakPermutationInterface_h_

#include "KeccakF-1600-int-set.h"

void KeccakInitialize( void );
void KeccakInitializeState(unsigned char *state);
void KeccakPermutation(unsigned char *state);

void KeccakExtract(const unsigned char *state, unsigned char *data, unsigned int laneCount);

#endif
