# net2o in a container

see https://fossil.net2o.de for more information

Uses the directory `~/net2o` of the docker host to store
persistent data such as keys or chat logs. Inside the container
this directory is called `/net2o` regardless where it is on the
host -- see the `-v` option below.

```shell
$ mkdir ~/net2o
```

Put a config text file in the `~/net2o` directory. Note the
slightly different pathnames.

```shell
$ cat ~/net2o/config
date=2
chats="/net2o/chats"
keys="/net2o/keys"
.net2o="/net2o"
$
```
optionally copy *other* existing net2o files into this directory keeping the
directory structure intact.

Fetch your container

```shell
$ docker pull forthy42/net2o
```

Now run the container

```shell
$ docker run -ti --rm -v ~/net2o:/net2o --user $(id -u):$(id -g) forthy42/net2o keylist
Passphrase: ••••••  
==== opened: ....
$ docker run -ti --rm -v ~/net2o:/net2o --user $(id -u):$(id -g) forthy42/net2o chat groupname
Passphrase: ••••••  
==== opened: ....
...
/bye
$
```

Hint: use a shell alias to shorten the command line

```shell
$ alias n2o="docker run -ti --rm -v ~/net2o:/net2o --user $(id -u):$(id -g) forthy42/net2o"
$ n2o keylist
...
$ n2o chat group@user
....
```
