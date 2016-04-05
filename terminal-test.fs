\ test file for net2o terminal

require client-tests.fs \ test framework
require test-keys.fs \ we want the test keys - never use this in production!

+debug
%droprate

?nextarg [IF] s>number drop to net2o-port [THEN]

i'm test

init-server
event-loop-task
