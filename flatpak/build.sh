#!/bin/bash

OPTS="--gpg-sign=67007C30 --repo=repo --user --install --force-clean build"

flatpak-builder $OPTS net.net2o.net2o.yml

rsync -az repo/ root@net2o.de:/var/www/flathub.net2o.net/html/repo
