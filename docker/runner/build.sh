#!/bin/bash

eval $(n2o version | head -1 | while read NVERSION GFORTHVERSION; do GFORTHVERSION=${GFORTHVERSION%-*}; echo NVERSION=${NVERSION#*-} GFORTHVERSION=${GFORTHVERSION##*-}; done)
sed -e "s/@VERSION@/$NVERSION/g" <Dockerfile.in >Dockerfile

sudo docker build -t forthy42/net2o:latest .
sudo docker build -f Dockerfile.gui -t forthy42/net2o-gui:latest .
sudo docker build -f Dockerfile.gui+fonts -t forthy42/net2o-gui-fonts:latest .
if [ "$1" != "nopush" ]
then
    docker push forthy42/net2o:latest
    docker push forthy42/net2o-gui:latest
    docker push forthy42/net2o-gui-fonts:latest
fi
