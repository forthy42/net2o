%module egl
%insert("fsiinclude")
%{
#include <EGL/egl.h>
%}
%apply int { EGLint };
%apply long { EGLNativeDisplayType, EGLNativeWindowType, EGLNativePixmapType };

#define __ANDROID__
#define ANDROID
#define EGLAPI
#define EGLAPIENTRY

%include <EGL/egl.h>
