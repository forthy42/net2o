require unix/socket.fs
require unix/filestat.fs
file-stat buffer: statbuf
s" net2o.fs" r/o open-file throw value fd0
fd0 fileno statbuf fstat . errno . cr
s" .cache/net2o.fs" r/w open-file throw value fd1
-100 ".cache/net2o.fs\0" drop statbuf st_mtime 0 .s utimensat . errno . cr
fd1 fileno 0 statbuf st_mtime $400 .s utimensat . errno . cr