TOP        = $(HOME)/proj/swig-2.0.1-bernd
NDK        = $(HOME)/proj/android-toolchain/sysroot
CC         = arm-linux-androideabi-gcc
SWIG       = $(TOP)/preinst-swig
TARGETS    = gles.fsx egl.fsx glext.fsx
OPTIONS    = -forth -no-sectioncomments -stackcomments -I$(NDK)/usr/include -I$(NDK)/usr/include/GLES -I$(NDK)/usr/include/EGL

all: $(TARGETS)

# execute compiled file
%.fs: %.fsx
	./$< -gforth > $@

# compile fsi-file
%.fsx: %-fsi.c
	$(CC) -o $@ $<

# use swig to create the fsi file
%-fsi.c: 
	$(SWIG) $(OPTIONS) -o $@ $(patsubst %-fsi.c, %.i, $@)

run: $(DEMO)
	gforth $(DEMO)

.PHONY: clean

clean:
	rm -f $(TARGETS)
	rm -f $(patsubst %.fs, %-fsi.c, $(TARGETS))
	rm -f $(patsubst %.fs, %.fsx, $(TARGETS))

