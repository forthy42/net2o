%module keccak
%insert("include")
%{
#include <KeccakF-1600.h>
%}

%apply SWIGTYPE * { unsigned char const *const };

%include <KeccakF-1600.h>
