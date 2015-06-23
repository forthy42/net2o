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
INSTALL	= /usr/bin/install -c
INSTALL_DATA = ${INSTALL} -m 644
ARCH	  =
datadir   =

SOURCES = 64bit.fs alice-test.fs base64.fs base85.fs bob-test.fs	\
	  client-test.fs client-tests.fs crypto-api.fs curve25519.fs	\
	  debugging.fs ed25519-donna.fs ed25519-table-test.fs		\
	  eve-test.fs hash-table.fs keccak.fs keccak-small.fs		\
	  kregion.fs n2o.fs net2o-cmd.fs net2o-connected.fs		\
	  net2o-connect.fs net2o-crypt.fs net2o-dht.fs net2o-err.fs	\
	  net2o-file.fs net2o.fs net2o-keys.fs net2o-log.fs		\
	  net2o-msg.fs net2o-template.fs net2o-tools.fs			\
	  net2o-vault.fs rng.fs server-test.fs termclient.fs		\
	  terminal-test.fs test-keys.fs threefish.fs xtype.fs		\
	  tests/alice2-msg.fs tests/alice-msg.fs tests/bernd-msg.fs	\
	  tests/bob-msg.fs tests/copy.fs tests/cryptspeed.fs		\
	  tests/dht.fs tests/dht-pop.fs tests/ed25519.fs		\
	  tests/insdeltest.fs tests/keys.fs tests/msg.fs		\
	  tests/teststat.fs tests/vault.fs net2o-dhtroot.n2o

SRCDIRS = tests

all: configs no-config

no-config: libs install-libs libcc

configs:
	for i in $(LIBS); do (cd $$i; ./autogen.sh CFLAGS="$(subst O2,O3,$(CFLAGS))" --host=$(HOST) --prefix=$(PREFIX) && make clean); done

libs:
	for i in $(LIBS); do (cd $$i; make); done

install-libs:
	for i in $(LIBS); do (cd $$i; make install); done

install:
	for i in $(SRCDIRS); do \
		mkdir -p $(DESTDIR)$(datadir)/gforth$(ARCH)/site-forth/net2o/$$i; \
	done
	for i in $(SOURCES); do \
		$(INSTALL_DATA) ./$$i $(DESTDIR)$(datadir)/gforth$(ARCH)/site-forth/net2o/$$i; \
	done

libcc:	$(FORTHLIB)
	mkdir -p $(INCDIR)
	cp $(SRC)/engine/libcc.h $(SRC)/engine/config.h $(INCDIR)
	-for i in $(FORTHLIB); do \
		echo "generating library $$i"; \
		$(ENGINE) -p ".:~+:$(SRC)"  -e "also c-lib s\" $(LIBCCNAMED)\" >libcc-named-dir libcc-path clear-path libcc-named-dir libcc-path also-path :noname 2drop s\" $(LIBCCDEST)\" ; is replace-rpath previous" $$i -e bye; \
	done
