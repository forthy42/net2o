%module keccaklow
%insert("include")
%{
#include <KeccakP-1600-SnP.h>
%}

%apply SWIGTYPE * { unsigned char const *const };

%include <KeccakP-1600/compact/KeccakP-1600-SnP.h>
