%module bdelta
%insert("include")
%{
#include <stdint.h>
#include <bdelta.h>
%}

%apply long long { pos };

%include <bdelta.h>
