\ This file has been generated using SWIG and fsi,
\ and is already platform dependent, search for the corresponding
\ fsi-file to compile it where no one has compiled it before ;)
\ GForth has its own dynamic loader and doesn't need addional C-Code.
\ That's why this file contains normal Gforth-code( version 0.6.9 or higher )
\ and could be used directly with include or require.
\ As all comments are stripped during the compilation, please
\ insert the copyright notice of the original file here.
1	constant GL_ES_VERSION_2_0
256	constant GL_DEPTH_BUFFER_BIT
1024	constant GL_STENCIL_BUFFER_BIT
16384	constant GL_COLOR_BUFFER_BIT
0	constant GL_FALSE
1	constant GL_TRUE
0	constant GL_POINTS
1	constant GL_LINES
2	constant GL_LINE_LOOP
3	constant GL_LINE_STRIP
4	constant GL_TRIANGLES
5	constant GL_TRIANGLE_STRIP
6	constant GL_TRIANGLE_FAN
0	constant GL_ZERO
1	constant GL_ONE
768	constant GL_SRC_COLOR
769	constant GL_ONE_MINUS_SRC_COLOR
770	constant GL_SRC_ALPHA
771	constant GL_ONE_MINUS_SRC_ALPHA
772	constant GL_DST_ALPHA
773	constant GL_ONE_MINUS_DST_ALPHA
774	constant GL_DST_COLOR
775	constant GL_ONE_MINUS_DST_COLOR
776	constant GL_SRC_ALPHA_SATURATE
32774	constant GL_FUNC_ADD
32777	constant GL_BLEND_EQUATION
32777	constant GL_BLEND_EQUATION_RGB
34877	constant GL_BLEND_EQUATION_ALPHA
32778	constant GL_FUNC_SUBTRACT
32779	constant GL_FUNC_REVERSE_SUBTRACT
32968	constant GL_BLEND_DST_RGB
32969	constant GL_BLEND_SRC_RGB
32970	constant GL_BLEND_DST_ALPHA
32971	constant GL_BLEND_SRC_ALPHA
32769	constant GL_CONSTANT_COLOR
32770	constant GL_ONE_MINUS_CONSTANT_COLOR
32771	constant GL_CONSTANT_ALPHA
32772	constant GL_ONE_MINUS_CONSTANT_ALPHA
32773	constant GL_BLEND_COLOR
34962	constant GL_ARRAY_BUFFER
34963	constant GL_ELEMENT_ARRAY_BUFFER
34964	constant GL_ARRAY_BUFFER_BINDING
34965	constant GL_ELEMENT_ARRAY_BUFFER_BINDING
35040	constant GL_STREAM_DRAW
35044	constant GL_STATIC_DRAW
35048	constant GL_DYNAMIC_DRAW
34660	constant GL_BUFFER_SIZE
34661	constant GL_BUFFER_USAGE
34342	constant GL_CURRENT_VERTEX_ATTRIB
1028	constant GL_FRONT
1029	constant GL_BACK
1032	constant GL_FRONT_AND_BACK
3553	constant GL_TEXTURE_2D
2884	constant GL_CULL_FACE
3042	constant GL_BLEND
3024	constant GL_DITHER
2960	constant GL_STENCIL_TEST
2929	constant GL_DEPTH_TEST
3089	constant GL_SCISSOR_TEST
32823	constant GL_POLYGON_OFFSET_FILL
32926	constant GL_SAMPLE_ALPHA_TO_COVERAGE
32928	constant GL_SAMPLE_COVERAGE
0	constant GL_NO_ERROR
1280	constant GL_INVALID_ENUM
1281	constant GL_INVALID_VALUE
1282	constant GL_INVALID_OPERATION
1285	constant GL_OUT_OF_MEMORY
2304	constant GL_CW
2305	constant GL_CCW
2849	constant GL_LINE_WIDTH
33901	constant GL_ALIASED_POINT_SIZE_RANGE
33902	constant GL_ALIASED_LINE_WIDTH_RANGE
2885	constant GL_CULL_FACE_MODE
2886	constant GL_FRONT_FACE
2928	constant GL_DEPTH_RANGE
2930	constant GL_DEPTH_WRITEMASK
2931	constant GL_DEPTH_CLEAR_VALUE
2932	constant GL_DEPTH_FUNC
2961	constant GL_STENCIL_CLEAR_VALUE
2962	constant GL_STENCIL_FUNC
2964	constant GL_STENCIL_FAIL
2965	constant GL_STENCIL_PASS_DEPTH_FAIL
2966	constant GL_STENCIL_PASS_DEPTH_PASS
2967	constant GL_STENCIL_REF
2963	constant GL_STENCIL_VALUE_MASK
2968	constant GL_STENCIL_WRITEMASK
34816	constant GL_STENCIL_BACK_FUNC
34817	constant GL_STENCIL_BACK_FAIL
34818	constant GL_STENCIL_BACK_PASS_DEPTH_FAIL
34819	constant GL_STENCIL_BACK_PASS_DEPTH_PASS
36003	constant GL_STENCIL_BACK_REF
36004	constant GL_STENCIL_BACK_VALUE_MASK
36005	constant GL_STENCIL_BACK_WRITEMASK
2978	constant GL_VIEWPORT
3088	constant GL_SCISSOR_BOX
3106	constant GL_COLOR_CLEAR_VALUE
3107	constant GL_COLOR_WRITEMASK
3317	constant GL_UNPACK_ALIGNMENT
3333	constant GL_PACK_ALIGNMENT
3379	constant GL_MAX_TEXTURE_SIZE
3386	constant GL_MAX_VIEWPORT_DIMS
3408	constant GL_SUBPIXEL_BITS
3410	constant GL_RED_BITS
3411	constant GL_GREEN_BITS
3412	constant GL_BLUE_BITS
3413	constant GL_ALPHA_BITS
3414	constant GL_DEPTH_BITS
3415	constant GL_STENCIL_BITS
10752	constant GL_POLYGON_OFFSET_UNITS
32824	constant GL_POLYGON_OFFSET_FACTOR
32873	constant GL_TEXTURE_BINDING_2D
32936	constant GL_SAMPLE_BUFFERS
32937	constant GL_SAMPLES
32938	constant GL_SAMPLE_COVERAGE_VALUE
32939	constant GL_SAMPLE_COVERAGE_INVERT
34466	constant GL_NUM_COMPRESSED_TEXTURE_FORMATS
34467	constant GL_COMPRESSED_TEXTURE_FORMATS
4352	constant GL_DONT_CARE
4353	constant GL_FASTEST
4354	constant GL_NICEST
33170	constant GL_GENERATE_MIPMAP_HINT
5120	constant GL_BYTE
5121	constant GL_UNSIGNED_BYTE
5122	constant GL_SHORT
5123	constant GL_UNSIGNED_SHORT
5124	constant GL_INT
5125	constant GL_UNSIGNED_INT
5126	constant GL_FLOAT
5132	constant GL_FIXED
6402	constant GL_DEPTH_COMPONENT
6406	constant GL_ALPHA
6407	constant GL_RGB
6408	constant GL_RGBA
6409	constant GL_LUMINANCE
6410	constant GL_LUMINANCE_ALPHA
32819	constant GL_UNSIGNED_SHORT_4_4_4_4
32820	constant GL_UNSIGNED_SHORT_5_5_5_1
33635	constant GL_UNSIGNED_SHORT_5_6_5
35632	constant GL_FRAGMENT_SHADER
35633	constant GL_VERTEX_SHADER
34921	constant GL_MAX_VERTEX_ATTRIBS
36347	constant GL_MAX_VERTEX_UNIFORM_VECTORS
36348	constant GL_MAX_VARYING_VECTORS
35661	constant GL_MAX_COMBINED_TEXTURE_IMAGE_UNITS
35660	constant GL_MAX_VERTEX_TEXTURE_IMAGE_UNITS
34930	constant GL_MAX_TEXTURE_IMAGE_UNITS
36349	constant GL_MAX_FRAGMENT_UNIFORM_VECTORS
35663	constant GL_SHADER_TYPE
35712	constant GL_DELETE_STATUS
35714	constant GL_LINK_STATUS
35715	constant GL_VALIDATE_STATUS
35717	constant GL_ATTACHED_SHADERS
35718	constant GL_ACTIVE_UNIFORMS
35719	constant GL_ACTIVE_UNIFORM_MAX_LENGTH
35721	constant GL_ACTIVE_ATTRIBUTES
35722	constant GL_ACTIVE_ATTRIBUTE_MAX_LENGTH
35724	constant GL_SHADING_LANGUAGE_VERSION
35725	constant GL_CURRENT_PROGRAM
512	constant GL_NEVER
513	constant GL_LESS
514	constant GL_EQUAL
515	constant GL_LEQUAL
516	constant GL_GREATER
517	constant GL_NOTEQUAL
518	constant GL_GEQUAL
519	constant GL_ALWAYS
7680	constant GL_KEEP
7681	constant GL_REPLACE
7682	constant GL_INCR
7683	constant GL_DECR
5386	constant GL_INVERT
34055	constant GL_INCR_WRAP
34056	constant GL_DECR_WRAP
7936	constant GL_VENDOR
7937	constant GL_RENDERER
7938	constant GL_VERSION
7939	constant GL_EXTENSIONS
9728	constant GL_NEAREST
9729	constant GL_LINEAR
9984	constant GL_NEAREST_MIPMAP_NEAREST
9985	constant GL_LINEAR_MIPMAP_NEAREST
9986	constant GL_NEAREST_MIPMAP_LINEAR
9987	constant GL_LINEAR_MIPMAP_LINEAR
10240	constant GL_TEXTURE_MAG_FILTER
10241	constant GL_TEXTURE_MIN_FILTER
10242	constant GL_TEXTURE_WRAP_S
10243	constant GL_TEXTURE_WRAP_T
5890	constant GL_TEXTURE
34067	constant GL_TEXTURE_CUBE_MAP
34068	constant GL_TEXTURE_BINDING_CUBE_MAP
34069	constant GL_TEXTURE_CUBE_MAP_POSITIVE_X
34070	constant GL_TEXTURE_CUBE_MAP_NEGATIVE_X
34071	constant GL_TEXTURE_CUBE_MAP_POSITIVE_Y
34072	constant GL_TEXTURE_CUBE_MAP_NEGATIVE_Y
34073	constant GL_TEXTURE_CUBE_MAP_POSITIVE_Z
34074	constant GL_TEXTURE_CUBE_MAP_NEGATIVE_Z
34076	constant GL_MAX_CUBE_MAP_TEXTURE_SIZE
33984	constant GL_TEXTURE0
33985	constant GL_TEXTURE1
33986	constant GL_TEXTURE2
33987	constant GL_TEXTURE3
33988	constant GL_TEXTURE4
33989	constant GL_TEXTURE5
33990	constant GL_TEXTURE6
33991	constant GL_TEXTURE7
33992	constant GL_TEXTURE8
33993	constant GL_TEXTURE9
33994	constant GL_TEXTURE10
33995	constant GL_TEXTURE11
33996	constant GL_TEXTURE12
33997	constant GL_TEXTURE13
33998	constant GL_TEXTURE14
33999	constant GL_TEXTURE15
34000	constant GL_TEXTURE16
34001	constant GL_TEXTURE17
34002	constant GL_TEXTURE18
34003	constant GL_TEXTURE19
34004	constant GL_TEXTURE20
34005	constant GL_TEXTURE21
34006	constant GL_TEXTURE22
34007	constant GL_TEXTURE23
34008	constant GL_TEXTURE24
34009	constant GL_TEXTURE25
34010	constant GL_TEXTURE26
34011	constant GL_TEXTURE27
34012	constant GL_TEXTURE28
34013	constant GL_TEXTURE29
34014	constant GL_TEXTURE30
34015	constant GL_TEXTURE31
34016	constant GL_ACTIVE_TEXTURE
10497	constant GL_REPEAT
33071	constant GL_CLAMP_TO_EDGE
33648	constant GL_MIRRORED_REPEAT
35664	constant GL_FLOAT_VEC2
35665	constant GL_FLOAT_VEC3
35666	constant GL_FLOAT_VEC4
35667	constant GL_INT_VEC2
35668	constant GL_INT_VEC3
35669	constant GL_INT_VEC4
35670	constant GL_BOOL
35671	constant GL_BOOL_VEC2
35672	constant GL_BOOL_VEC3
35673	constant GL_BOOL_VEC4
35674	constant GL_FLOAT_MAT2
35675	constant GL_FLOAT_MAT3
35676	constant GL_FLOAT_MAT4
35678	constant GL_SAMPLER_2D
35680	constant GL_SAMPLER_CUBE
34338	constant GL_VERTEX_ATTRIB_ARRAY_ENABLED
34339	constant GL_VERTEX_ATTRIB_ARRAY_SIZE
34340	constant GL_VERTEX_ATTRIB_ARRAY_STRIDE
34341	constant GL_VERTEX_ATTRIB_ARRAY_TYPE
34922	constant GL_VERTEX_ATTRIB_ARRAY_NORMALIZED
34373	constant GL_VERTEX_ATTRIB_ARRAY_POINTER
34975	constant GL_VERTEX_ATTRIB_ARRAY_BUFFER_BINDING
35738	constant GL_IMPLEMENTATION_COLOR_READ_TYPE
35739	constant GL_IMPLEMENTATION_COLOR_READ_FORMAT
35713	constant GL_COMPILE_STATUS
35716	constant GL_INFO_LOG_LENGTH
35720	constant GL_SHADER_SOURCE_LENGTH
36346	constant GL_SHADER_COMPILER
36344	constant GL_SHADER_BINARY_FORMATS
36345	constant GL_NUM_SHADER_BINARY_FORMATS
36336	constant GL_LOW_FLOAT
36337	constant GL_MEDIUM_FLOAT
36338	constant GL_HIGH_FLOAT
36339	constant GL_LOW_INT
36340	constant GL_MEDIUM_INT
36341	constant GL_HIGH_INT
36160	constant GL_FRAMEBUFFER
36161	constant GL_RENDERBUFFER
32854	constant GL_RGBA4
32855	constant GL_RGB5_A1
36194	constant GL_RGB565
33189	constant GL_DEPTH_COMPONENT16
6401	constant GL_STENCIL_INDEX
36168	constant GL_STENCIL_INDEX8
36162	constant GL_RENDERBUFFER_WIDTH
36163	constant GL_RENDERBUFFER_HEIGHT
36164	constant GL_RENDERBUFFER_INTERNAL_FORMAT
36176	constant GL_RENDERBUFFER_RED_SIZE
36177	constant GL_RENDERBUFFER_GREEN_SIZE
36178	constant GL_RENDERBUFFER_BLUE_SIZE
36179	constant GL_RENDERBUFFER_ALPHA_SIZE
36180	constant GL_RENDERBUFFER_DEPTH_SIZE
36181	constant GL_RENDERBUFFER_STENCIL_SIZE
36048	constant GL_FRAMEBUFFER_ATTACHMENT_OBJECT_TYPE
36049	constant GL_FRAMEBUFFER_ATTACHMENT_OBJECT_NAME
36050	constant GL_FRAMEBUFFER_ATTACHMENT_TEXTURE_LEVEL
36051	constant GL_FRAMEBUFFER_ATTACHMENT_TEXTURE_CUBE_MAP_FACE
36064	constant GL_COLOR_ATTACHMENT0
36096	constant GL_DEPTH_ATTACHMENT
36128	constant GL_STENCIL_ATTACHMENT
0	constant GL_NONE
36053	constant GL_FRAMEBUFFER_COMPLETE
36054	constant GL_FRAMEBUFFER_INCOMPLETE_ATTACHMENT
36055	constant GL_FRAMEBUFFER_INCOMPLETE_MISSING_ATTACHMENT
36057	constant GL_FRAMEBUFFER_INCOMPLETE_DIMENSIONS
36061	constant GL_FRAMEBUFFER_UNSUPPORTED
36006	constant GL_FRAMEBUFFER_BINDING
36007	constant GL_RENDERBUFFER_BINDING
34024	constant GL_MAX_RENDERBUFFER_SIZE
1286	constant GL_INVALID_FRAMEBUFFER_OPERATION
c-function glActiveTexture glActiveTexture n -- void	( texture -- )
c-function glAttachShader glAttachShader n n -- void	( program shader -- )
c-function glBindAttribLocation glBindAttribLocation n n a -- void	( program index name -- )
c-function glBindBuffer glBindBuffer n n -- void	( target buffer -- )
c-function glBindFramebuffer glBindFramebuffer n n -- void	( target framebuffer -- )
c-function glBindRenderbuffer glBindRenderbuffer n n -- void	( target renderbuffer -- )
c-function glBindTexture glBindTexture n n -- void	( target texture -- )
c-function glBlendColor glBlendColor r r r r -- void	( red green blue alpha -- )
c-function glBlendEquation glBlendEquation n -- void	( mode -- )
c-function glBlendEquationSeparate glBlendEquationSeparate n n -- void	( modeRGB modeAlpha -- )
c-function glBlendFunc glBlendFunc n n -- void	( sfactor dfactor -- )
c-function glBlendFuncSeparate glBlendFuncSeparate n n n n -- void	( srcRGB dstRGB srcAlpha dstAlpha -- )
c-function glBufferData glBufferData n n a n -- void	( target size data usage -- )
c-function glBufferSubData glBufferSubData n n n a -- void	( target offset size data -- )
c-function glCheckFramebufferStatus glCheckFramebufferStatus n -- n	( target -- )
c-function glClear glClear n -- void	( mask -- )
c-function glClearColor glClearColor r r r r -- void	( red green blue alpha -- )
c-function glClearDepthf glClearDepthf r -- void	( depth -- )
c-function glClearStencil glClearStencil n -- void	( s -- )
c-function glColorMask glColorMask n n n n -- void	( red green blue alpha -- )
c-function glCompileShader glCompileShader n -- void	( shader -- )
c-function glCompressedTexImage2D glCompressedTexImage2D n n n n n n n a -- void	( target level internalformat width height border imageSize data -- )
c-function glCompressedTexSubImage2D glCompressedTexSubImage2D n n n n n n n n a -- void	( target level xoffset yoffset width height format imageSize data -- )
c-function glCopyTexImage2D glCopyTexImage2D n n n n n n n n -- void	( target level internalformat x y width height border -- )
c-function glCopyTexSubImage2D glCopyTexSubImage2D n n n n n n n n -- void	( target level xoffset yoffset x y width height -- )
c-function glCreateProgram glCreateProgram  -- n	( -- )
c-function glCreateShader glCreateShader n -- n	( type -- )
c-function glCullFace glCullFace n -- void	( mode -- )
c-function glDeleteBuffers glDeleteBuffers n a -- void	( n buffers -- )
c-function glDeleteFramebuffers glDeleteFramebuffers n a -- void	( n framebuffers -- )
c-function glDeleteProgram glDeleteProgram n -- void	( program -- )
c-function glDeleteRenderbuffers glDeleteRenderbuffers n a -- void	( n renderbuffers -- )
c-function glDeleteShader glDeleteShader n -- void	( shader -- )
c-function glDeleteTextures glDeleteTextures n a -- void	( n textures -- )
c-function glDepthFunc glDepthFunc n -- void	( func -- )
c-function glDepthMask glDepthMask n -- void	( flag -- )
c-function glDepthRangef glDepthRangef r r -- void	( zNear zFar -- )
c-function glDetachShader glDetachShader n n -- void	( program shader -- )
c-function glDisable glDisable n -- void	( cap -- )
c-function glDisableVertexAttribArray glDisableVertexAttribArray n -- void	( index -- )
c-function glDrawArrays glDrawArrays n n n -- void	( mode first count -- )
c-function glDrawElements glDrawElements n n n a -- void	( mode count type indices -- )
c-function glEnable glEnable n -- void	( cap -- )
c-function glEnableVertexAttribArray glEnableVertexAttribArray n -- void	( index -- )
c-function glFinish glFinish  -- void	( -- )
c-function glFlush glFlush  -- void	( -- )
c-function glFramebufferRenderbuffer glFramebufferRenderbuffer n n n n -- void	( target attachment renderbuffertarget renderbuffer -- )
c-function glFramebufferTexture2D glFramebufferTexture2D n n n n n -- void	( target attachment textarget texture level -- )
c-function glFrontFace glFrontFace n -- void	( mode -- )
c-function glGenBuffers glGenBuffers n a -- void	( n buffers -- )
c-function glGenerateMipmap glGenerateMipmap n -- void	( target -- )
c-function glGenFramebuffers glGenFramebuffers n a -- void	( n framebuffers -- )
c-function glGenRenderbuffers glGenRenderbuffers n a -- void	( n renderbuffers -- )
c-function glGenTextures glGenTextures n a -- void	( n textures -- )
c-function glGetActiveAttrib glGetActiveAttrib n n n a a a a -- void	( program index bufsize length size type name -- )
c-function glGetActiveUniform glGetActiveUniform n n n a a a a -- void	( program index bufsize length size type name -- )
c-function glGetAttachedShaders glGetAttachedShaders n n a a -- void	( program maxcount count shaders -- )
c-function glGetAttribLocation glGetAttribLocation n a -- n	( program name -- )
c-function glGetBooleanv glGetBooleanv n a -- void	( pname params -- )
c-function glGetBufferParameteriv glGetBufferParameteriv n n a -- void	( target pname params -- )
c-function glGetError glGetError  -- n	( -- )
c-function glGetFloatv glGetFloatv n a -- void	( pname params -- )
c-function glGetFramebufferAttachmentParameteriv glGetFramebufferAttachmentParameteriv n n n a -- void	( target attachment pname params -- )
c-function glGetIntegerv glGetIntegerv n a -- void	( pname params -- )
c-function glGetProgramiv glGetProgramiv n n a -- void	( program pname params -- )
c-function glGetProgramInfoLog glGetProgramInfoLog n n a a -- void	( program bufsize length infolog -- )
c-function glGetRenderbufferParameteriv glGetRenderbufferParameteriv n n a -- void	( target pname params -- )
c-function glGetShaderiv glGetShaderiv n n a -- void	( shader pname params -- )
c-function glGetShaderInfoLog glGetShaderInfoLog n n a a -- void	( shader bufsize length infolog -- )
c-function glGetShaderPrecisionFormat glGetShaderPrecisionFormat n n a a -- void	( shadertype precisiontype range precision -- )
c-function glGetShaderSource glGetShaderSource n n a a -- void	( shader bufsize length source -- )
c-function glGetString glGetString n -- a	( name -- )
c-function glGetTexParameterfv glGetTexParameterfv n n a -- void	( target pname params -- )
c-function glGetTexParameteriv glGetTexParameteriv n n a -- void	( target pname params -- )
c-function glGetUniformfv glGetUniformfv n n a -- void	( program location params -- )
c-function glGetUniformiv glGetUniformiv n n a -- void	( program location params -- )
c-function glGetUniformLocation glGetUniformLocation n a -- n	( program name -- )
c-function glGetVertexAttribfv glGetVertexAttribfv n n a -- void	( index pname params -- )
c-function glGetVertexAttribiv glGetVertexAttribiv n n a -- void	( index pname params -- )
c-function glGetVertexAttribPointerv glGetVertexAttribPointerv n n a -- void	( index pname pointer -- )
c-function glHint glHint n n -- void	( target mode -- )
c-function glIsBuffer glIsBuffer n -- n	( buffer -- )
c-function glIsEnabled glIsEnabled n -- n	( cap -- )
c-function glIsFramebuffer glIsFramebuffer n -- n	( framebuffer -- )
c-function glIsProgram glIsProgram n -- n	( program -- )
c-function glIsRenderbuffer glIsRenderbuffer n -- n	( renderbuffer -- )
c-function glIsShader glIsShader n -- n	( shader -- )
c-function glIsTexture glIsTexture n -- n	( texture -- )
c-function glLineWidth glLineWidth r -- void	( width -- )
c-function glLinkProgram glLinkProgram n -- void	( program -- )
c-function glPixelStorei glPixelStorei n n -- void	( pname param -- )
c-function glPolygonOffset glPolygonOffset r r -- void	( factor units -- )
c-function glReadPixels glReadPixels n n n n n n a -- void	( x y width height format type pixels -- )
c-function glReleaseShaderCompiler glReleaseShaderCompiler  -- void	( -- )
c-function glRenderbufferStorage glRenderbufferStorage n n n n -- void	( target internalformat width height -- )
c-function glSampleCoverage glSampleCoverage r n -- void	( value invert -- )
c-function glScissor glScissor n n n n -- void	( x y width height -- )
c-function glShaderBinary glShaderBinary n a n a n -- void	( n shaders binaryformat binary length -- )
c-function glShaderSource glShaderSource n n a a -- void	( shader count string length -- )
c-function glStencilFunc glStencilFunc n n n -- void	( func ref mask -- )
c-function glStencilFuncSeparate glStencilFuncSeparate n n n n -- void	( face func ref mask -- )
c-function glStencilMask glStencilMask n -- void	( mask -- )
c-function glStencilMaskSeparate glStencilMaskSeparate n n -- void	( face mask -- )
c-function glStencilOp glStencilOp n n n -- void	( fail zfail zpass -- )
c-function glStencilOpSeparate glStencilOpSeparate n n n n -- void	( face fail zfail zpass -- )
c-function glTexImage2D glTexImage2D n n n n n n n n a -- void	( target level internalformat width height border format type pixels -- )
c-function glTexParameterf glTexParameterf n n r -- void	( target pname param -- )
c-function glTexParameterfv glTexParameterfv n n a -- void	( target pname params -- )
c-function glTexParameteri glTexParameteri n n n -- void	( target pname param -- )
c-function glTexParameteriv glTexParameteriv n n a -- void	( target pname params -- )
c-function glTexSubImage2D glTexSubImage2D n n n n n n n n a -- void	( target level xoffset yoffset width height format type pixels -- )
c-function glUniform1f glUniform1f n r -- void	( location x -- )
c-function glUniform1fv glUniform1fv n n a -- void	( location count v -- )
c-function glUniform1i glUniform1i n n -- void	( location x -- )
c-function glUniform1iv glUniform1iv n n a -- void	( location count v -- )
c-function glUniform2f glUniform2f n r r -- void	( location x y -- )
c-function glUniform2fv glUniform2fv n n a -- void	( location count v -- )
c-function glUniform2i glUniform2i n n n -- void	( location x y -- )
c-function glUniform2iv glUniform2iv n n a -- void	( location count v -- )
c-function glUniform3f glUniform3f n r r r -- void	( location x y z -- )
c-function glUniform3fv glUniform3fv n n a -- void	( location count v -- )
c-function glUniform3i glUniform3i n n n n -- void	( location x y z -- )
c-function glUniform3iv glUniform3iv n n a -- void	( location count v -- )
c-function glUniform4f glUniform4f n r r r r -- void	( location x y z w -- )
c-function glUniform4fv glUniform4fv n n a -- void	( location count v -- )
c-function glUniform4i glUniform4i n n n n n -- void	( location x y z w -- )
c-function glUniform4iv glUniform4iv n n a -- void	( location count v -- )
c-function glUniformMatrix2fv glUniformMatrix2fv n n n a -- void	( location count transpose value -- )
c-function glUniformMatrix3fv glUniformMatrix3fv n n n a -- void	( location count transpose value -- )
c-function glUniformMatrix4fv glUniformMatrix4fv n n n a -- void	( location count transpose value -- )
c-function glUseProgram glUseProgram n -- void	( program -- )
c-function glValidateProgram glValidateProgram n -- void	( program -- )
c-function glVertexAttrib1f glVertexAttrib1f n r -- void	( indx x -- )
c-function glVertexAttrib1fv glVertexAttrib1fv n a -- void	( indx values -- )
c-function glVertexAttrib2f glVertexAttrib2f n r r -- void	( indx x y -- )
c-function glVertexAttrib2fv glVertexAttrib2fv n a -- void	( indx values -- )
c-function glVertexAttrib3f glVertexAttrib3f n r r r -- void	( indx x y z -- )
c-function glVertexAttrib3fv glVertexAttrib3fv n a -- void	( indx values -- )
c-function glVertexAttrib4f glVertexAttrib4f n r r r r -- void	( indx x y z w -- )
c-function glVertexAttrib4fv glVertexAttrib4fv n a -- void	( indx values -- )
c-function glVertexAttribPointer glVertexAttribPointer n n n n n a -- void	( indx size type normalized stride ptr -- )
c-function glViewport glViewport n n n n -- void	( x y width height -- )
