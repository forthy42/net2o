# Try it #

You can try net2o (see below) either by starting net2o with

    n2o cmd

and enter net2o command mode, or by running each command with

    n2o <command>

in the second case, you'll be asked again and again for your password
when needed.

You can execute the following net2o script commands either by entering

    help
    keygen <nick>

This will ask for your password and generate a key.  Write down the
base85 code it gives for revoking that key; this revocation key is not
stored anywhere else.  For some tests, you want to create a second key
(e.g. to test chat).  Give it a different pass phrase; you can create
as many IDs as you like; if they have different passphrases, only one
of them opens at a time.

Check what keys you have already importet:

    keylist

This should be your key and the net2o-dhtroot key.  Import my key

    keysearch kQusJ

At the moment, a 32 bit ID should do it...  Your own pubkeys have been
exported with the keygen command into a <nick>.n2o file.  You can
import that in your other id(s) with

    keyin <nick>.n2o

Try encrypt and decrypt a test file for yourself:

    <create a file>
    enc <file>
    cat <file>.v2o
    dec <file>.v2o

You can also create detached signatures (<file>.s2o):

    sign <file>
    verify <file>

You can try a group chat with several ids, start the group "test" with <id1>

    chat test

If you want to talk to someone else, you need to make sure they accept
your connection, so you first need to send an invitation

    invite @<id1>

And after acceptance of that id connect from the other ids with

    chat test@<id1>

Or do a 1:1 chat with

    chat @<id2>

from <id1> and

    chat @<id1>

from <id2> on several computers/terminals.  The chat mode works a bit like IRC,
you can use /help to list the commands, /peers to see the direct
neighbors, and /me <action> if you aren't actually talking.

You can copy small or large files, when the corresponding id has the
named file permission.  Set the permission with

    perm @<id2> +n

and start the server

    server

to supply things on <id1>, and e.g.

    get @<id1> data/2011-05-20_17-01-12.jpg

to copy one of the test images.