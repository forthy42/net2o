# Generic makefile, for crosscompiling use make -f Makefile.<target>

INCLUDES   = -I$(NDK)/usr/include -I$(NDK)/usr/local/include/
TOP        = $(HOME)/proj/swig-2.0.1-bernd2
SWIG       = $(TOP)/preinst-swig
TARGETS    = keccak.fsx
LIBRARY    = libkeccak.fs
OPTIONS    = -forth -no-sectioncomments -stackcomments $(INCLUDES)
INSTALL	= /usr/bin/install -c
ARCH=
VERSION	= `gforth --version 2>&1 | cut -f2 -d' '`
SHELL	= /bin/sh
RMTREE	= rm -rf
prefix = 
exec_prefix = ${prefix}
libexecdir = $(package)${exec_prefix}/lib
libccdir = $(subst $(DESTDIR),,$(libexecdir)/gforth$(ARCH)/$(VERSION)/libcc-named/)
srcdir = .
DESTDIR = /home/bernd/proj/net2o

all: $(TARGETS)

build-libcc-named: $(LIBRARY) $(TARGETS)
		$(RMTREE) lib/gforth$(ARCH)/$(VERSION)/libcc-named/
		-for i in $(LIBRARY); do ./libforth$(SUFFIX) -e "s\" `pwd`/lib/gforth$(ARCH)/$(VERSION)/libcc-named/\" libcc-named-dir-v 2! libcc-path clear-path libcc-named-dir libcc-path also-path :noname 2drop s\" $(DESTDIR)$(libccdir)\" ; is replace-rpath" $(srcdir)/$$i -e bye; done

libs: build-libcc-named $(LIBRARY)
	for i in $(LIBRARY); do \
	    $(LIBTOOL) --silent --mode=install $(INSTALL) lib/gforth$(ARCH)/$(VERSION)/libcc-named/libgf`basename $$i .fs`.la $(DESTDIR)$(libccdir)libgf`basename $$i .fs`.la; \
	done

# execute compiled file
%.fs: %.fsx
	./$< -gforth > $@

# compile fsi-file
%.fsx: %-fsi.c
	$(CC) -o $@ $<

# use swig to create the fsi file
%-fsi.c: %.i
	$(SWIG) $(OPTIONS) -o $@ $(patsubst %-fsi.c, %.i, $@)

run: $(DEMO)
	gforth $(DEMO)

.PHONY: clean

clean:
	rm -f $(TARGETS)
	rm -f $(patsubst %.fsx, %-fsi.c, $(TARGETS))
	rm -f $(patsubst %.fsx, %.fs, $(TARGETS))

