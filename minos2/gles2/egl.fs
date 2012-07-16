\ This file has been generated using SWIG and fsi,
\ and is already platform dependent, search for the corresponding
\ fsi-file to compile it where no one has compiled it before ;)
\ GForth has its own dynamic loader and doesn't need addional C-Code.
\ That's why this file contains normal Gforth-code( version 0.6.9 or higher )
\ and could be used directly with include or require.
\ As all comments are stripped during the compilation, please
\ insert the copyright notice of the original file here.
1	constant EGL_VERSION_1_0
1	constant EGL_VERSION_1_1
1	constant EGL_VERSION_1_2
1	constant EGL_VERSION_1_3
1	constant EGL_VERSION_1_4
0	constant EGL_FALSE
1	constant EGL_TRUE
12288	constant EGL_SUCCESS
12289	constant EGL_NOT_INITIALIZED
12290	constant EGL_BAD_ACCESS
12291	constant EGL_BAD_ALLOC
12292	constant EGL_BAD_ATTRIBUTE
12293	constant EGL_BAD_CONFIG
12294	constant EGL_BAD_CONTEXT
12295	constant EGL_BAD_CURRENT_SURFACE
12296	constant EGL_BAD_DISPLAY
12297	constant EGL_BAD_MATCH
12298	constant EGL_BAD_NATIVE_PIXMAP
12299	constant EGL_BAD_NATIVE_WINDOW
12300	constant EGL_BAD_PARAMETER
12301	constant EGL_BAD_SURFACE
12302	constant EGL_CONTEXT_LOST
12320	constant EGL_BUFFER_SIZE
12321	constant EGL_ALPHA_SIZE
12322	constant EGL_BLUE_SIZE
12323	constant EGL_GREEN_SIZE
12324	constant EGL_RED_SIZE
12325	constant EGL_DEPTH_SIZE
12326	constant EGL_STENCIL_SIZE
12327	constant EGL_CONFIG_CAVEAT
12328	constant EGL_CONFIG_ID
12329	constant EGL_LEVEL
12330	constant EGL_MAX_PBUFFER_HEIGHT
12331	constant EGL_MAX_PBUFFER_PIXELS
12332	constant EGL_MAX_PBUFFER_WIDTH
12333	constant EGL_NATIVE_RENDERABLE
12334	constant EGL_NATIVE_VISUAL_ID
12335	constant EGL_NATIVE_VISUAL_TYPE
12337	constant EGL_SAMPLES
12338	constant EGL_SAMPLE_BUFFERS
12339	constant EGL_SURFACE_TYPE
12340	constant EGL_TRANSPARENT_TYPE
12341	constant EGL_TRANSPARENT_BLUE_VALUE
12342	constant EGL_TRANSPARENT_GREEN_VALUE
12343	constant EGL_TRANSPARENT_RED_VALUE
12344	constant EGL_NONE
12345	constant EGL_BIND_TO_TEXTURE_RGB
12346	constant EGL_BIND_TO_TEXTURE_RGBA
12347	constant EGL_MIN_SWAP_INTERVAL
12348	constant EGL_MAX_SWAP_INTERVAL
12349	constant EGL_LUMINANCE_SIZE
12350	constant EGL_ALPHA_MASK_SIZE
12351	constant EGL_COLOR_BUFFER_TYPE
12352	constant EGL_RENDERABLE_TYPE
12353	constant EGL_MATCH_NATIVE_PIXMAP
12354	constant EGL_CONFORMANT
12368	constant EGL_SLOW_CONFIG
12369	constant EGL_NON_CONFORMANT_CONFIG
12370	constant EGL_TRANSPARENT_RGB
12430	constant EGL_RGB_BUFFER
12431	constant EGL_LUMINANCE_BUFFER
12380	constant EGL_NO_TEXTURE
12381	constant EGL_TEXTURE_RGB
12382	constant EGL_TEXTURE_RGBA
12383	constant EGL_TEXTURE_2D
1	constant EGL_PBUFFER_BIT
2	constant EGL_PIXMAP_BIT
4	constant EGL_WINDOW_BIT
32	constant EGL_VG_COLORSPACE_LINEAR_BIT
64	constant EGL_VG_ALPHA_FORMAT_PRE_BIT
512	constant EGL_MULTISAMPLE_RESOLVE_BOX_BIT
1024	constant EGL_SWAP_BEHAVIOR_PRESERVED_BIT
1	constant EGL_OPENGL_ES_BIT
2	constant EGL_OPENVG_BIT
4	constant EGL_OPENGL_ES2_BIT
8	constant EGL_OPENGL_BIT
12371	constant EGL_VENDOR
12372	constant EGL_VERSION
12373	constant EGL_EXTENSIONS
12429	constant EGL_CLIENT_APIS
12374	constant EGL_HEIGHT
12375	constant EGL_WIDTH
12376	constant EGL_LARGEST_PBUFFER
12416	constant EGL_TEXTURE_FORMAT
12417	constant EGL_TEXTURE_TARGET
12418	constant EGL_MIPMAP_TEXTURE
12419	constant EGL_MIPMAP_LEVEL
12422	constant EGL_RENDER_BUFFER
12423	constant EGL_VG_COLORSPACE
12424	constant EGL_VG_ALPHA_FORMAT
12432	constant EGL_HORIZONTAL_RESOLUTION
12433	constant EGL_VERTICAL_RESOLUTION
12434	constant EGL_PIXEL_ASPECT_RATIO
12435	constant EGL_SWAP_BEHAVIOR
12441	constant EGL_MULTISAMPLE_RESOLVE
12420	constant EGL_BACK_BUFFER
12421	constant EGL_SINGLE_BUFFER
12425	constant EGL_VG_COLORSPACE_sRGB
12426	constant EGL_VG_COLORSPACE_LINEAR
12427	constant EGL_VG_ALPHA_FORMAT_NONPRE
12428	constant EGL_VG_ALPHA_FORMAT_PRE
10000	constant EGL_DISPLAY_SCALING
12436	constant EGL_BUFFER_PRESERVED
12437	constant EGL_BUFFER_DESTROYED
12438	constant EGL_OPENVG_IMAGE
12439	constant EGL_CONTEXT_CLIENT_TYPE
12440	constant EGL_CONTEXT_CLIENT_VERSION
12442	constant EGL_MULTISAMPLE_RESOLVE_DEFAULT
12443	constant EGL_MULTISAMPLE_RESOLVE_BOX
12448	constant EGL_OPENGL_ES_API
12449	constant EGL_OPENVG_API
12450	constant EGL_OPENGL_API
12377	constant EGL_DRAW
12378	constant EGL_READ
12379	constant EGL_CORE_NATIVE_ENGINE
12423	constant EGL_COLORSPACE
12424	constant EGL_ALPHA_FORMAT
12425	constant EGL_COLORSPACE_sRGB
12426	constant EGL_COLORSPACE_LINEAR
12427	constant EGL_ALPHA_FORMAT_NONPRE
12428	constant EGL_ALPHA_FORMAT_PRE
c-function eglGetError eglGetError  -- n	( -- )
c-function eglGetDisplay eglGetDisplay a -- a	( display_id -- )
c-function eglInitialize eglInitialize a a a -- n	( dpy major minor -- )
c-function eglTerminate eglTerminate a -- n	( dpy -- )
c-function eglQueryString eglQueryString a n -- a	( dpy name -- )
c-function eglGetConfigs eglGetConfigs a a n a -- n	( dpy configs config_size num_config -- )
c-function eglChooseConfig eglChooseConfig a a a n a -- n	( dpy attrib_list configs config_size num_config -- )
c-function eglGetConfigAttrib eglGetConfigAttrib a a n a -- n	( dpy config attribute value -- )
c-function eglCreateWindowSurface eglCreateWindowSurface a a a a -- a	( dpy config win attrib_list -- )
c-function eglCreatePbufferSurface eglCreatePbufferSurface a a a -- a	( dpy config attrib_list -- )
c-function eglCreatePixmapSurface eglCreatePixmapSurface a a a a -- a	( dpy config pixmap attrib_list -- )
c-function eglDestroySurface eglDestroySurface a a -- n	( dpy surface -- )
c-function eglQuerySurface eglQuerySurface a a n a -- n	( dpy surface attribute value -- )
c-function eglBindAPI eglBindAPI n -- n	( api -- )
c-function eglQueryAPI eglQueryAPI  -- n	( -- )
c-function eglWaitClient eglWaitClient  -- n	( -- )
c-function eglReleaseThread eglReleaseThread  -- n	( -- )
c-function eglCreatePbufferFromClientBuffer eglCreatePbufferFromClientBuffer a n a a a -- a	( dpy buftype buffer config attrib_list -- )
c-function eglSurfaceAttrib eglSurfaceAttrib a a n n -- n	( dpy surface attribute value -- )
c-function eglBindTexImage eglBindTexImage a a n -- n	( dpy surface buffer -- )
c-function eglReleaseTexImage eglReleaseTexImage a a n -- n	( dpy surface buffer -- )
c-function eglSwapInterval eglSwapInterval a n -- n	( dpy interval -- )
c-function eglCreateContext eglCreateContext a a a a -- a	( dpy config share_context attrib_list -- )
c-function eglDestroyContext eglDestroyContext a a -- n	( dpy ctx -- )
c-function eglMakeCurrent eglMakeCurrent a a a a -- n	( dpy draw read ctx -- )
c-function eglGetCurrentContext eglGetCurrentContext  -- a	( -- )
c-function eglGetCurrentSurface eglGetCurrentSurface n -- a	( readdraw -- )
c-function eglGetCurrentDisplay eglGetCurrentDisplay  -- a	( -- )
c-function eglQueryContext eglQueryContext a a n a -- n	( dpy ctx attribute value -- )
c-function eglWaitGL eglWaitGL  -- n	( -- )
c-function eglWaitNative eglWaitNative n -- n	( engine -- )
c-function eglSwapBuffers eglSwapBuffers a a -- n	( dpy surface -- )
c-function eglCopyBuffers eglCopyBuffers a a a -- n	( dpy surface target -- )
c-function eglGetProcAddress eglGetProcAddress a -- a	( procname -- )
