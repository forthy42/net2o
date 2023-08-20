\ test for threefish

require ../net2o.fs

threefish-o crypto-o !

: >skein ( addr u -- ) c:0key c:hash ;
: skein@ ( -- addr u ) pad c:key> pad $40 ;
: skein? ( addr u -- ) skein@ str= '+' '-' rot select emit ;

\ tests from Skein 1.3 NIST CD

hex" 00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" >skein hex" B1A2BBC6EF6025BC40EB3822161F36E375D1BB0AEE3186FBD19E47C5D479947B7BC2F8586E35F0CFF7E7F03084B0B7B1F1AB3961A580A3E97EB41EA14A6D7BBE" skein?
hex" 101112131415161718191A1B1C1D1E1F202122232425262728292A2B2C2D2E2F303132333435363738393A3B3C3D3E3F404142434445464748494A4B4C4D4E4F" >threefish
0x0706050403020100. d>64 0x0F0E0D0C0B0A0908. d>64 tf-tweak!
hex" FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0DFDEDDDCDBDAD9D8D7D6D5D4D3D2D1D0CFCECDCCCBCAC9C8C7C6C5C4C3C2C1C0" c:hash
hex" 1CFABE6ADD2EA3D443F73F2C25C4D56A8CD6DEE25B87AC356CD05CEA74C7A699F8F4D751429CECDCAF92F53ECB1B76F560DF12326ACBABFC4EE1A2F99FDE6EFD" skein?
cr

\ test cases from NIST submission package; test encrypt & decrypt
tf_ctx_256 buffer: key256

hex" 0000000000000000000000000000000000000000000000000000000000000000" drop
key256 over
pad $C tf_encrypt_256
key256 pad pad $20 + $0 tf_decrypt_256
$20 pad $20 + over str=
hex" 84da2a1f8beaee947066ae3e3103f1ad536db1f4a1192495116b9f3ce6133fd8"
pad over str= and
." threefish 256 " [IF] ." passed" [ELSE] ." didn't pass" [THEN] cr

hex" 101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f"
key256 tf_ctx_256-key swap move
hex" 000102030405060708090a0b0c0d0e0f"
key256 tf_ctx_256-tweak swap move

hex" fffefdfcfbfaf9f8f7f6f5f4f3f2f1f0efeeedecebeae9e8e7e6e5e4e3e2e1e0" drop
key256 over
pad $C tf_encrypt_256
key256 pad pad $20 + $0 tf_decrypt_256
$20 pad $20 + over str=
hex" e0d091ff0eea8fdfc98192e62ed80ad59d865d08588df476657056b5955e97df"
pad over str= and
." threefish 256 " [IF] ." passed" [ELSE] ." didn't pass" [THEN] cr

tf_ctx buffer: key512

hex" 00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" drop
key512 over
pad $C tf_encrypt
key512 pad pad $40 + $0 tf_decrypt
$40 pad $40 + over str=
hex" b1a2bbc6ef6025bc40eb3822161f36e375d1bb0aee3186fbd19e47c5d479947b7bc2f8586e35f0cff7e7f03084b0b7b1f1ab3961a580a3e97eb41ea14a6d7bbe"
pad over str= and
." threefish 512 " [IF] ." passed" [ELSE] ." didn't pass" [THEN] cr

hex" 101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f"
key512 tf_ctx-key swap move
hex" 000102030405060708090a0b0c0d0e0f"
key512 tf_ctx-tweak swap move

hex" fffefdfcfbfaf9f8f7f6f5f4f3f2f1f0efeeedecebeae9e8e7e6e5e4e3e2e1e0dfdedddcdbdad9d8d7d6d5d4d3d2d1d0cfcecdcccbcac9c8c7c6c5c4c3c2c1c0" drop
key512 over
pad $C tf_encrypt
key512 pad pad $40 + $0 tf_decrypt
$40 pad $40 + over str=
hex" e304439626d45a2cb401cad8d636249a6338330eb06d45dd8b36b90e97254779272a0a8d99463504784420ea18c9a725af11dffea10162348927673d5c1caf3d"
pad over str= and
." threefish 512 " [IF] ." passed" [ELSE] ." didn't pass" [THEN] cr

\ Benchmarking
10 0 [DO] c:0key pad $100000 !time c:encrypt .time ."  for 1MB" cr [LOOP]
script? [IF] bye [THEN]
