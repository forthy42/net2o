\ test keys

: test-keys ( -- ) \ yes, use these keys *only* for testing!
    \ revoke: 58AB8F52F46E73EFAB068F6337F371E14DD589BF0894D2F0AF51AE7EBB858A68
    x" A91158F2C560ACCDFEFC05104B922E49C9DD022D0163921DAE08E6C2148A7BEBC83C71FCB345D24400D866C7FD32092C2D1EC056FD17B9537037590BD021EEBF" key:new >o
    x" B2578B8766DB3A60F1F4F36B276924FDA6E7F559F629716BC78D95DB1CD8D400" ke-sk sec! +seckey
    "test" ke-nick $! nick! $1367B086A24E6B10. d>64 ke-first! 0 ke-type ! perm%myself ke-mask ! o>
    \ revoke: 5843E2DC055E1F8BE14570A37B0F81146040A2CEE1D6C01B97C3BB801CDED864
    x" 69D86C471E5FEED89478FB4260C898B6F69026BA4E78A9D815B53EB33CA9013A8E753EC381881FAAFFA66CD9DD47D3F2C0867E1A2B48067CA2188DF400C11074" key:new >o
    x" 5905350A6B4B5DE29C2CA4562BB105EF570713CE648E38F6FBBB6D076D141B0A" ke-sk sec! +seckey
    "anonymous" ke-nick $! nick! $1367B086A255C9C2. d>64 ke-first! 0 ke-type ! perm%myself ke-mask ! o>
    \ revoke: 38A6FB42FF41A690A108DCA460CC0D15AE3C1C23FFFA9E92583FFD9FB16AD276
    x" 7A0FFD3D31ED822D683D685EA5689C91CB170B54A82F0E53554D34584F90DB017750513CDC1F1DC7F8F61214ED4BC801CF70C3D5FC90F716F2363038ACEE58BD" key:new >o
    x" AAB952DD5D1850F1B468EEF84F72552148070C3F499600FE362934970329FE04" ke-sk sec! +seckey
    "alice" ke-nick $! nick! $1367B086A25CEF70. d>64 ke-first! 1 ke-type ! perm%myself ke-mask ! o>
    \ revoke: D82AF4AE7CD3DA7316CE6F26BC5792F4F5E6B36B4C14F7D60C49B421AE1D5468
    x" 1A20176C79D26402811945CFC241116BAFB52DD033492044DB5CFEECCA21E6E49F350B40A28D83B618361167D13B51A4EFCE919C7BB6BDCC570D9B7031A0428E" key:new >o
    x" 6B65577985D851753ACFFFFB00360C70C267420132204A17F4468D9CACDB010F" ke-sk sec! +seckey
    "bob" ke-nick $! nick! $1367B086A26436A9. d>64 ke-first! 1 ke-type ! perm%myself ke-mask ! o>
    \ revoke: 7821DA41AFBB8F7356E2EB7059BE70321D7ADCDAD8C504998627CBB9366AB752
    x" 9483FBBB98A5BFE792206519FB2BAF9EE21FE863ABE981AB1C209123D40E1969EA7C68162DF5340142524D6BE3E407B065824D1E3582E6209CA03876F406EBCA" key:new >o
    x" 693D7EF6BF0E0CEFB0654EB95AB7C729B8799F850CAB24B1211116ED72EA3602" ke-sk sec! +seckey
    "eve" ke-nick $! nick! $1367B086A26B4E42. d>64 ke-first! 1 ke-type ! perm%myself ke-mask ! o>
;

test-keys
