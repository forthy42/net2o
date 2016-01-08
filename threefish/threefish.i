%module threefish
%insert("include")
%{
#include <threefish.h>
%}

%apply long long { uint64_t };
%apply short { uint16_t };

%include <threefish.h>
