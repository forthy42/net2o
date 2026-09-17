#!/bin/bash

PLATFORMS=${PLATFORMS:-linux/amd64,linux/arm64}

eval $(n2o version | head -1 | while read NVERSION GFORTHVERSION; do GFORTHVERSION=${GFORTHVERSION%-*}; echo NVERSION=${NVERSION#*-} GFORTHVERSION=${GFORTHVERSION##*-}; done)

sed -e "s/@VERSION@/$NVERSION/g" <Dockerfile.in >Dockerfile

docker buildx build --platform $PLATFORMS --network host -t forthy42/net2o:latest . 2>net2o-build.log
docker buildx build --platform $PLATFORMS --network host -f Dockerfile.gui -t forthy42/net2o-gui:latest . 2>net2o-gui-build.log
docker buildx build --platform $PLATFORMS --network host -f Dockerfile.gui+fonts -t forthy42/net2o-gui-fonts:latest . 2>net2o-fonts-build.log
if [ "$1" != "nopush" ]
then
    docker push forthy42/net2o:latest
    docker push forthy42/net2o-gui:latest
    docker push forthy42/net2o-gui-fonts:latest
fi
