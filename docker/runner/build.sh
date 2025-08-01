#!/bin/bash

eval $(n2o version | head -1 | while read NVERSION GFORTHVERSION; do GFORTHVERSION=${GFORTHVERSION%-*}; echo NVERSION=${NVERSION#*-} GFORTHVERSION=${GFORTHVERSION##*-}; done)
sed -e "s/@VERSION@/$NVERSION/g" <Dockerfile.in >Dockerfile

docker build --network host -t forthy42/net2o:latest .
docker build --network host -f Dockerfile.gui -t forthy42/net2o-gui:latest .
docker build --network host -f Dockerfile.gui+fonts -t forthy42/net2o-gui-fonts:latest .
if [ "$1" != "nopush" ]
then
    docker push forthy42/net2o:latest
    docker push forthy42/net2o-gui:latest
    docker push forthy42/net2o-gui-fonts:latest
fi
