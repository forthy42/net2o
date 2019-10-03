\ Google+ import

\ Copyright © 2018   Bernd Paysan

\ This program is free software: you can redistribute it and/or modify
\ it under the terms of the GNU Affero General Public License as published by
\ the Free Software Foundation, either version 3 of the License, or
\ (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU Affero General Public License for more details.

\ You should have received a copy of the GNU Affero General Public License
\ along with this program.  If not, see <http://www.gnu.org/licenses/>.

require ../html/parser.fs

: key-pk@ ( o:key -- addr u )
    author:mapped-key .ke-pk $@ key| ;

: .key64 ( o:key -- )
    key-pk@ 64type ;

: replace-user ( addr u -- )
    2dup "https://plus.google.com/" string-prefix? IF
	2dup #24 /string authors# #@ IF
	    ." key:" @ ..key64
	    2drop EXIT  THEN
	drop  THEN
    type ;

' replace-user IS href-replace

Variable pics#
Variable picbase#
Variable picdesc#
Variable album#
Variable dir#

".metadata.csv" nip Constant .mtcsv#

: match-jpg/png ( key u addr u -- ) $make { w^ fn$ }
    fn$ $@ dir# #@ d0= IF
	".png" fn$ $+! fn$ $@ dir# #@ d0= IF
	    ".jpg" fn$ $@ + over - swap move
	    fn$ $@ dir# #@ d0= IF
		2drop fn$ $free  EXIT  THEN
	THEN
    THEN
    fn$ $@ 2swap pics# #!
    fn$ $free ;

: unquote ( addr u -- addr' u' )
    2dup + 1- c@ ',' = +
    over c@ '"' = negate safe/string ?dup-IF  2dup + 1- c@ '"' = +  THEN ;

: dir-skips ( addr u n -- addr' u' )
    0 ?DO '/' $split 2nip LOOP ;

: basedir+name ( addr u -- addr' u' )
    [: ( 2dup basename 2>r ) 3 dir-skips
	2dup 2 dir-skips drop nip over - type
	( 2r> type ) ;] $tmp ;

: next, ( addr u -- addr' u' )
    2dup "\"\"," string-prefix? IF  3 /string  EXIT  THEN
    BEGIN  1 /string "\","  search  WHILE
	over 1- c@ '"' <>  UNTIL  2 safe/string  THEN ;

: next-csv ( addr u -- addr' u' addr1 u1 )
    2dup next, 2tuck drop nip over - unquote ;

: un-dquote ( addr u -- )
    BEGIN
	2dup "\"\"" search  WHILE
	    2dup 2 /string 2>r drop 1+ nip over - type
	    2r>  REPEAT  2drop type ;

: +album ( addr u -- )
    2dup 5 umin album# #@ d0= IF
	$make { w^ fn } fn cell over $@ 5 umin album# #!
    ELSE
	last# cell+ $ins[] drop
    THEN ;

: get-pic-filename { d: mtcsv -- }
    mtcsv r/o open-file throw { fd }
    mtcsv .mtcsv# - to mtcsv \ for the rest, need only the basename of the file
    pad $100 + $10000 fd read-line throw 2drop
    pad $100 + $10000 fd read-file throw pad $100 + swap
    next-csv { d: basefn }
    next-csv { d: desc }
    next-csv 2nip basedir+name mtcsv match-jpg/png
    last# cell+ $@
    basefn 2over picbase# #!
    desc ['] un-dquote $tmp 2over picdesc# #!
    +album
    fd close-file throw ;

: get-pic-filenames ( addr u -- )
    2dup open-dir throw { dd } fpath @ >r fpath off fpath also-path dd
    [: { dd } !time
	BEGIN
	    pad $100 dd read-dir throw  WHILE
		pad swap s" " 2swap dir# #!
	REPEAT  drop
	0 dir# [: $@ 2dup "*.metadata.csv" filename-match IF
		get-pic-filename 1+
	    ELSE  2drop  THEN ;] #map
	[: ." read " . ." pics in " .time ;]
	success-color color-execute cr ;] catch
    fpath $free r> fpath !  dd close-dir throw  throw
    dir# #frees ;

: get-avatars ( -- )
    avatars[] $[]# 0= ?EXIT \ nothing to do
    "avatars" .net2o-cache/ { d: dir }
    dir $1FF init-dir drop
    dir [: ." cd " type ." ; sort -k 3 | split -l128 - avatars.sh." ;] $tmp
    w/o open-pipe throw >r
    avatars[] ['] $[]. r@ outfile-execute
    r> close-pipe throw to $?
    dir [: ." cd " type ." ;time eval '(for i in avatars.sh.*; do curl -s $(cat $i) & done; wait)'; #rm avatars.sh.*" ;] $tmp system ;

: .avatar-file ( o:author -- addr u )
    [: ." avatars/g+:" author:resourceName$ basename type ." .png" ;] $tmp
    .net2o-cache/ ;

: hash-in-avatars ( -- )
    authors# [: cell+ $@ drop @ >o
	author:avatarImageUrl$ nip IF
	    .avatar-file slurp-file over >r hash-in r> free throw
	    author:mapped-key >o ke-avatar $! this-key-sign o>
	THEN
	o> ;] #map ;

: key-author ( o:author -- )
    first-key? IF
	my-key-default  false to first-key?
    ELSE
	author:resourceName$ basename  author:displayName$
	0 .dummy-key
    THEN  to author:mapped-key ;

: .url ( addr u -- )
    2dup "http" string-prefix? 0= IF  ." https:"  THEN  type ;

: +avatar-author ( o:author -- )
    author:avatarImageUrl$ dup IF
	.avatar-file file-status 0= IF
	    drop 2drop
	ELSE
	    drop [: ." -o g+:" author:resourceName$ basename type ." .png "
		.url
	    ;] $tmp avatars[] $+[]!
	THEN
    ELSE  2drop  THEN ;

: execute>file ( ... xt addr u -- )
    r/w create-file throw dup >r outfile-execute
    r> close-file throw ;

$100 buffer: filter-out
filter-out bl 1- 1 fill
1 filter-out ']' + c!

: .simple-text ( addr u -- )
    bounds ?DO  i c@ filter-out + c@ 0= IF  I c@ emit  THEN  LOOP ;

: .html ( -- )
    comments:content$ html-untag cr ;
: .plain ( -- )
    comments:content$ html>text ;
: .link ( -- )
    comments:link{} ?dup-IF cr >o
	'[' emit link:title$ type-esc'd ." ](" link:url$ type ')' emit cr
	o>  THEN ;

0 Value img-req-fid

: .mfile { d: fn -- }
    fn basedir+name pics# #@ 2dup d0= IF
	2drop fn .url
	fn [: .url cr ;] img-req-fid outfile-execute
    ELSE
	." file:" picbase# #@ type
    THEN ;
: .csv-link { d: fn -- }
    ." ![" fn picdesc# #@ .simple-text ." ](file:" fn picbase# #@ type ." )" cr ;
: .media-file ( -- )
    media:url$ basename nip $100 > IF
	." file:" media:localFilePath$ basename type
    ELSE
	media:localFilePath$ nip IF  ." file:" media:url$ basename type
	ELSE  media:url$ .mfile  THEN
    THEN ;
: inline-image ( -- )
    media:contentType$ "image/" string-prefix? IF
	." ![" media:description$ .simple-text ." ](" .media-file ')' emit cr
    ELSE
	media:url$ ." [" 2dup type ." ](" type ')' emit cr
    THEN ;
: .media ( -- )
    comments:media{} ?dup-IF cr .inline-image  THEN ;
: .album ( -- )
    comments:album{} ?dup-IF cr
	." ::album::" cr cr
	.album:media[] $@ over @ .media:url$
	basedir+name pics# #@ d0= IF
	    bounds U+DO
		I @ .inline-image
	    cell +LOOP
	ELSE
	    2drop
	    last# cell+ $@ 5 umin album# #@ bounds U+DO
		I $@ .csv-link
	    cell +LOOP
	THEN
    THEN ;
: .post ( o:comments -- )
    ." post:" comments:author{} ..key64 '/' emit
    ." g+:" comments:url$ basename type ;
: .project ( o:comments -- )
    comments:author{} .key-pk@ type
    ." g+:" comments:url$ basename type ;
: .author ( o:author -- )
    ." +[" author:displayName$ type ." ](key:" .key64 ." )" ;
: .reshared ( o:comments -- )
    comments:resharedPost{} ?dup-IF  cr >o
	." > " comments:author{} ?dup-IF  ..author ." : " THEN
	'[' emit comments:content$ ['] html>text $tmp $40 umin .simple-text
	." ](" .post ') emit cr cr
	.html .link .media .album o>
    THEN ;
: .choice ( n o:choices -- )
    '1' + dup emit ." . ::votes#" emit ." :: " choices:description$ type
    '\' emit cr ."   ![" choices:description$ type ." ]("
    choices:imageLocalFilePath$ dup IF  basename type
    ELSE  2drop choices:imageUrl$ .mfile  THEN  ." ) " ;
: .polls ( o:comments -- )
    comments:poll{} ?dup-IF  cr >o
	0 { n }
	poll:choices[] $@ bounds U+DO
	    n I @ ..choice cr  1 +to n
	cell +LOOP o>
    THEN ;
: .com/col ( o:collection -- ) cr
    collectionAttachment:displayName$
    ." [!["	2dup type ." ](" collectionAttachment:coverPhotoUrl$ .mfile
    ." )](chat:g+:" type
    collectionAttachment:owner{} ?dup-IF  '@' emit ..key64  THEN
    ')' emit ;

: .collection ( o:comments -- )
    comments:collectionAttachment{} ?dup-IF  ..com/col  THEN ;
: .community ( o:comments -- )
    comments:communityAttachment{} ?dup-IF  ..com/col  THEN ;

: add-file { dvcs d: file -- }
    [: .html .link .media .reshared .album .polls .collection .community ;] file execute>file
    file dvcs .dvcs-add ;

Variable pfile$
: $pfile ( xt -- addr u )
    pfile$ $free  pfile$ $exec  pfile$ $@ ;
: post-file ( -- )
    [: ." post-" project:project$ $. ." .md" ;] $pfile ;
: comment-file ( -- addr u )
    [: ." comment-" comments:resourceName$ dup 11 - /string type ." .md" ;]
    $pfile ;

: add-post ( dvcs -- ) dup .post-file add-file ;

: /dirs ( addr u n -- addr' u' )
    0 ?DO  '/' scan 1 safe/string  LOOP ;

Variable photo-path

: +photo-paths ( -- )
    "../.." open-dir throw >r
    BEGIN  pad $100 r@ read-dir throw  WHILE
	    pad over "." str= >r pad over ".." str= r> or 0= IF
		[: dir@ type ." /../../" pad swap type ;] $tmp
		compact-filename photo-path also-path
	    ELSE  drop  THEN
    REPEAT  drop r> close-dir throw ;

: find-file { d: fn-orig d: fn -- fn-orig addr' u' flag }
    fn-orig
    fn [: dir@ forth:type ." /" forth:type ;]
    $tmp compact-filename 2dup file-status nip 0= dup ?EXIT  drop 2drop
    fn-orig fn [:
	3 /dirs	dirname forth:type '/' emit basename forth:type ;]
    $tmp photo-path open-path-file IF  0 0 false
    ELSE  rot close-file throw true  THEN ;

: local-media ( filename u basename u o:dsvc -- )
    2dup delete-file drop 2swap
    find-file IF  2over symlink ?ior  dvcs-ref
    ELSE  2drop 2drop  THEN ;

: csv-media ( filename u o:dsvc -- )
    2dup picbase# #@ local-media ;

: add-media { dvcs -- }
    media:localFilePath$ dup IF
	media:url$ basename dup $100 > IF
	    2drop media:localFilePath$ basename
	THEN  dvcs .local-media  EXIT
    THEN  2drop
    media:url$ basedir+name pics# #@ d0= IF
	media:contentType$ "image/*" str= IF
	    ." media unavailable: " media:url$ type cr  THEN
	EXIT  THEN
    media:contentType$ "video/*" str= IF
	media:url$ ." [" 2dup type ." ](" type ." )" cr
	EXIT  THEN
    last# cell+ $@ dvcs .csv-media ;

: add-album { dvcs -- }
    comments:album{} ?dup-IF
	.album:media[] $@
	over @ .media:url$
	basedir+name pics# #@ d0= IF
	    bounds U+DO
		dvcs I @ .add-media
	    cell +LOOP
	ELSE
	    2drop
	    last# cell+ $@ 5 umin album# #@ bounds U+DO
		I $@ dvcs .csv-media
	    cell +LOOP
	THEN
    THEN ;

: add-poll-photo { dvcs -- }
    choices:imageLocalFilePath$ 2dup basename dvcs .local-media ;

: add-poll-photos { dvcs -- }
    comments:poll{} ?dup-IF
	.poll:choices[] $@ bounds U+DO
	    dvcs I @ .add-poll-photo
	cell +LOOP
    THEN ;

also net2o-base

2Variable post-ref

: add-message ( xt -- )
    project:project$ $@ ?msg-log
    [: sign[ msg-start execute post-ref 2@ chain, ]pksign ;] gen-cmd$
    +last-signed last-signed 2@ >msg-log 2drop ;

: sigdate+ ( -- )
    sigdate le-64@ 64#1 64+ sigdate le-64! ;

: add-plusones { dvcs -- }
    comments:plusOnes[] $@ bounds U+DO
	sigdate+
	I @ .plusOnes:plusOner{} .author:mapped-key dvcs >o to my-key
	[: '👍' ulit, msg-like ;] add-message o>
    cell +LOOP ;

: add-reshares { dvcs -- }
    comments:reshares[] $@ bounds U+DO
	sigdate+
	I @ .reshares:resharer{} .author:mapped-key dvcs >o to my-key
	[: '' ( '🔃' or '🐙' ) ulit, msg-like ;] add-message o>
    cell +LOOP ;

: add-votes { dvcs -- }
    comments:poll{} ?dup-IF
	0 { n }
	.poll:choices[] $@ bounds U+DO
	    I @ .choices:votes[] $@ bounds U+DO
		sigdate+
		I @ .votes:voter{} .author:mapped-key dvcs >o to my-key
		n [{: n :}L n '1' + ulit, msg-like ;] add-message o>
	    cell +LOOP
	    1 +to n
	cell +LOOP
    THEN ;
    
previous

Variable comment#

: add-comment ( dvcs -- )
    comment-file add-file ;

: create>never ( o:comments -- )
    comments:creationTime! ( comments:updateTime! 64umax )
    64#-1 sigdate le-128! ;

: wrap-key ( ... xt key -- )
    my-key-default >r to my-key-default catch
    r> to my-key-default throw ;

: add-comments { dvcs-o -- }
    comments:comments[] $@ bounds U+DO
	I @ >o
	dvcs-o add-comment
	comments:media{} ?dup-IF  >o dvcs-o add-media o>  THEN
	comments:author{} .author:mapped-key { mkey }
	create>never
	['] .plain $tmp $80 umin -trailing-garbage
	dvcs-o >o pfile$ $@ dvcs:fileref[] $+[]!
	['] (dvcs-ci) mkey wrap-key o>
	last-msg 2@ post-ref 2!
	dvcs-o add-plusones \ plusones not exported at the moment
	\ dvcs-o add-reshares \ reshares of comments not possible
	o>
    cell +LOOP ;

: comments-base ( -- addr u )
    comments:comments[] $@len IF
	comments:comments[] $@ drop @ .comments:url$
    ELSE
	comments:url$
    THEN  basename ;

: ?make-group ( -- )
    msg-group$ $@ group# #@ d0= IF
	msg-group$ $@ make-group
    THEN ;

: add-collection { dvcso | w^ groups[] -- }
    comments:postAcl{} ?dup-IF >o
	postAcl:collectionAcl{} ?dup-IF
	    .collectionAcl:collection{} ?dup-IF
		[: ." g+:coll:" .collection:displayName$ type ;] $tmp
		groups[] $+[]!
	    THEN
	THEN
	postAcl:communityAcl{} ?dup-IF
	    .communityAcl:community{} ?dup-IF
		[: ." g+:comm:" .collection:displayName$ type ;] $tmp
		groups[] $+[]!
	    THEN
	THEN
	postAcl:visibleToStandardAcl{} ?dup-IF
	    .visibleToStandardAcl:circles[] $@ bounds U+DO
		I @ .circles:displayName$ dup IF
		    [: ." g+:circle:"  type ;] $tmp groups[] $+[]!
		ELSE  2drop  THEN
	    cell +LOOP
	THEN
	groups[] $[]# 0= IF
	    postAcl:isPublic?
	    IF  "g+:<public>"  ELSE  "g+:<private>"  THEN
	    groups[] $+[]!
	THEN
	o>
    THEN
    comments:postKind$ "EXTERNAL_SITE_COMMENT" str= IF
	"g+:external-site-comment" groups[] $+[]!
    THEN
    ['] .plain $tmp $80 umin -trailing-garbage
    dup 0= IF  2drop "<no text>"  THEN  ['] .project $tmp
    groups[] [: msg-group$ $! 0 .?make-group
	[ also net2o-base ]
	[: 2over $, msg-text 2dup $, msg:posting# ulit, msg-object ;]
	[ previous ]
	(send-avalanche) drop msg-group$ $. space
	2dup d0= IF  ." <dupe>" 2drop cr  ELSE  .chat  THEN ;] $[]map
    2drop 2drop  groups[] $[]free ;

: write-out-article ( o:comment -- )
    \ <info> ." write out: " comments:url$ type cr <default>
    >dir redate-mode on  comment# off
    dvcs:new-dvcs { dvcs-o }
    comments-base
    2dup [: ." posts/" type ." /.n2o" ;] $tmp ~net2o-cache/..
    ".n2o/files" touch
    dvcs-o >o "g+:" project:project$ $! project:project$ $+!
    "master" project:branch$ $!  save-project o>
    dvcs-o add-post
    dvcs-o add-album
    dvcs-o add-poll-photos
    comments:media{} ?dup-IF  >o dvcs-o add-media o>  THEN
    create>never
    ['] .plain $tmp $100 umin -trailing-garbage
    dup 0= IF  2drop "<no text>"  THEN
    comments:author{} .author:mapped-key { mkey }
    dvcs-o >o pfile$ $@ dvcs:fileref[] $+[]!
    ['] (dvcs-ci) mkey wrap-key o>
    last-msg 2@ post-ref 2!
    dvcs-o add-plusones
    dvcs-o add-reshares
    dvcs-o add-votes
    dvcs-o add-comments
    dvcs-o .dvcs:dispose-dvcs
    create>never
    dvcs-o ['] add-collection mkey wrap-key
    dir> redate-mode off
    dvcs-objects #frees ;

: write-articles ( -- ) { | nn }
    "img-req.lst" w/o create-file throw to img-req-fid
    entries[] $@ bounds ?DO
	nn [: ." write " 6 .r ."  postings" ;]
	warning-color color-execute
	#-21 0 at-deltaxy
	I @ .write-out-article
	1 +to nn
    cell +LOOP
    img-req-fid close-file throw  0 to img-req-fid
    nn [: ." write "  6 .r ."  postings in " .time ;]
    success-color color-execute cr ;

[IFUNDEF] json-load-dir
    forward json-load-dir
[THEN]

: g+-import { d: dir -- }
    ?get-me >dir +photo-paths dir>
    ." Read pics metadata" cr   dir get-pic-filenames \ legacy for old takeout
    ." Read JSON files" cr      dir json-load-dir
    ." Write entries" cr        write-articles
    ." Get avatars" cr          get-avatars hash-in-avatars
    !save-all-msgs save-keys
    save-chatgroups .chatgroups ;
