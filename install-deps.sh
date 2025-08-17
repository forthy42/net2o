#!/bin/sh
which sudo || alias sudo=eval

version=$1
arch=$2

setup_debian() {
wget -O - https://net2o.de/bernd@net2o.de-yubikey.pgp.asc | \
    sudo gpg --dearmor -o /usr/share/keyrings/net2o-archive-keyring.gpg
sudo sh -c "cat >/etc/apt/sources.list.d/net2o.list" <<EOF
deb [signed-by=/usr/share/keyrings/net2o-archive-keyring.gpg] https://net2o.de/debian $version main
EOF
}

install_debian() {
    sudo apt-get -y install wget git gpg
    setup_debian
    sudo apt-get -y update
    sudo apt-get -y install gforth gforth-minos2
}

case `uname` in
    Linux)
	OS=`. /etc/os-release; echo ${ID%-*}`
	;;
    Darwin)
	OS=osx
	;;
esac

install_${TRAVIS_OS_NAME:-$OS}
