\ test file for net2o - server side

require client-tests.fs \ test framework
require test-keys.fs \ we want the test keys - never use this in production!

+debug
%droprate
\ bg( )else( debug-task )

+db no0key( \ )
perm%myself dup to perm%unknown to perm%default

cmd-args ?nextarg [IF] s>number drop to net2o-port [THEN]

i'm test

init-server
server-loop

