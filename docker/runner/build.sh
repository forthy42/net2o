#!/bin/bash

eval $(n2o version | while read NVERSION GFORTHVERSION; do GFORTHVERSION=${GFORTHVERSION%-*}; echo NVERSION=${NVERSION#*-} GFORTHVERSION=${GFORTHVERSION##*-}; done)
sed -e "s/@VERSION@/$NVERSION/g" <Dockerfile.in >Dockerfile

sudo docker build -t forthy42/net2o:latest .
docker push forthy42/net2o:latest
