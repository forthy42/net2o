# Gforth integration Makefile

# Copyright (C) 2015   Bernd Paysan

# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU Affero General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.

# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU Affero General Public License for more details.

# You should have received a copy of the GNU Affero General Public License
# along with this program.  If not, see <http://www.gnu.org/licenses/>.

LIBS 	 = keccak threefish ed25519-donna
GCC	 = gcc
LIBTOOL	 = libtool
CFLAGS	 = -O3
HOST     = 
FORTHLIB = ed25519-donna.fs keccak.fs threefish.fs
SRC      = .
ENGINE   = gforth
VERSION  = $(shell $(ENGINE) --version | tr ' ' /)
LIBCCNAMED =
LIBCCDEST =
KERNEL    = 
PREFIX    = /usr/local
INCDIR    = $(PREFIX)/include/$(VERSION)

all: configs no-config

no-config: libs install libcc

configs:
	for i in $(LIBS); do (cd $$i; ./autogen.sh CFLAGS="$(subst O2,O3,$(CFLAGS))" --host=$(HOST) --prefix=$(PREFIX) && make clean); done

libs:
	for i in $(LIBS); do (cd $$i; make); done

install:
	for i in $(LIBS); do (cd $$i; make install); done

libcc:	$(FORTHLIB)
	mkdir -p $(INCDIR)
	cp $(SRC)/engine/libcc.h $(SRC)/engine/config.h $(INCDIR)
	-for i in $(FORTHLIB); do (echo "generating library $$i"; \
		$(ENGINE) -p ".:~+:$(SRC)"  -e "also c-lib s\" $(LIBCCNAMED)\" >libcc-named-dir libcc-path clear-path libcc-named-dir libcc-path also-path :noname 2drop s\" $(LIBCCDEST)\" ; is replace-rpath previous" $$i -e bye); done
