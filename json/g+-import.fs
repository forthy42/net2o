\ Google+ import

\ Copyright (C) 2018   Bernd Paysan

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

: .key64 ( o:key -- )
    author:mapped-key .ke-pk $@ key| 64type ;

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
    dir# #offs ;

: get-avatars ( -- )
    avatars[] $[]# 0= ?EXIT \ nothing to do
    "avatars" .net2o-cache/ { d: dir }
    dir $1FF init-dir drop
    dir [: ." cd " type ." ; sort -k 3 | split -l128 - avatars.sh." ;] $tmp
    w/o open-pipe throw >r
    avatars[] ['] $[]. r@ outfile-execute
    r> close-pipe throw to $?
    dir [: ." cd " type ." ;time eval '(for i in avatars.sh.*; do curl -s $(cat $i) & done; wait)'; rm avatars.sh.*" ;] $tmp system ;

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

: +avatar-author ( o:author -- )
    author:avatarImageUrl$ dup IF
	.avatar-file file-status 0= IF
	    drop 2drop
	ELSE
	    drop [: ." -o g+:" author:resourceName$ basename type ." .png "
		type ;] $tmp avatars[] $+[]!
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
	'[' emit link:title$ type ." ](" link:url$ type ')' emit cr
	o>  THEN ;

: .mfile ( -- )
    media:url$ basedir+name pics# #@ 2dup d0= IF
	2drop media:url$ type
    ELSE
	." file:" picbase# #@ type
    THEN ;
: .csv-link { d: fn -- }
    ." ![" fn picdesc# #@ .simple-text ." ](file:" fn picbase# #@ type ." )" cr ;
: .media ( -- )
    comments:media{} ?dup-IF cr >o
	." ![" media:description$ .simple-text ." ](" .mfile ')' emit cr
	o>  THEN ;
: .album ( -- )
    comments:album{} ?dup-IF cr
	.album:media[] $@ over @ .media:url$
	basedir+name pics# #@ d0= IF
	    bounds U+DO
		I @ >o
		." ![" media:description$ .simple-text ." ](" .mfile ')' emit cr
		o>
	    cell +LOOP
	ELSE
	    2drop
	    last# cell+ $@ 5 umin album# #@ bounds U+DO
		I $@ .csv-link
	    cell +LOOP
	THEN
    THEN ;
: .reshared ( -- )
    comments:resharedPost{} ?dup-IF  cr >o
	." > " comments:author{} ?dup-IF >o
	    ." +[" author:displayName$ type ." ](key:" .key64 ." ): " o>
	THEN
	'[' emit comments:content$ ['] html>text $tmp $40 umin .simple-text
	." ](post:" comments:author{} ..key64 '/' emit
	comments:url$ basename type ') emit cr cr
	.html .link .media .album
    o> THEN ;

: add-file { dvcs d: file -- }
    [: .html .link .media .reshared .album ;] file execute>file
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

: csv-media ( filename u o:dsvc -- )
    2dup picbase# #@ 2dup delete-file drop 2swap
    [: dir@ forth:type ." /" forth:type ;] $tmp
    2over symlink ?ior
    dvcs-ref ;

: add-media { dvcs -- }
    media:url$ basedir+name pics# #@ d0= IF
	media:contentType$ "image/*" str= IF
	    ." media unavailable: " media:url$ type cr  THEN
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

also net2o-base

2Variable post-ref

: add-message ( xt -- )
    project:project$ $@ ?msg-log
    [: sign[ msg-start execute post-ref 2@ chain, ]pksign ;] gen-cmd$
    +last-signed last-signed 2@ >msg-log 2drop ;

: add-plusones { dvcs -- }
    comments:plusOnes[] $@ bounds U+DO
	I @ .plusOnes:plusOner{} .author:mapped-key dvcs >o to my-key
	[: '👍' ulit, msg-like ;] add-message o>
    cell +LOOP ;

: add-reshares { dvcs -- }
    comments:reshares[] $@ bounds U+DO
	I @ .reshares:resharer{} .author:mapped-key dvcs >o to my-key
	[: '🔃' ( '🐙' ) ulit, msg-like ;] add-message o>
    cell +LOOP ;

previous

Variable comment#

: add-comment ( dvcs -- )
    comment-file add-file ;

: create>never ( o:comments -- )
    comments:creationTime! comments:updateTime! 64umax 64#-1 sigdate le-128! ;

: wrap-key ( ... xt key -- )
    my-key-default >r to my-key-default catch r> to my-key-default throw ;

: add-comments { dvcs-o -- }
    comments:comments[] $@ bounds U+DO
	I @ >o
	dvcs-o add-comment
	comments:media{} ?dup-IF  >o dvcs-o add-media o>  THEN
	comments:author{} .author:mapped-key { mkey }
	create>never
	['] .plain $tmp $100 umin
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

: write-out-article ( o:comment -- )
    >dir redate-mode on  comment# off
    dvcs:new-dvcs { dvcs-o }
    comments-base
    2dup [: ." posts/" type ." /.n2o" ;] $tmp
    .net2o-cache/ 2dup $1FF init-dir drop dirname set-dir throw
    ".n2o/files" touch
    dvcs-o >o project:project$ $!
    "master" project:branch$ $! save-project o>
    dvcs-o add-post
    dvcs-o add-album
    comments:media{} ?dup-IF  >o dvcs-o add-media o>  THEN
    create>never
    ['] .plain $tmp $100 umin
    dvcs-o >o pfile$ $@ dvcs:fileref[] $+[]! (dvcs-ci) o>
    last-msg 2@ post-ref 2!
    dvcs-o add-plusones
    dvcs-o add-reshares
    dvcs-o add-comments
    dvcs-o .dvcs:dispose-dvcs
    dir> redate-mode off ;

: write-articles ( -- ) { | nn }
    entries[] $@ bounds ?DO
	I @ .write-out-article
	1 +to nn
	nn [: ." write " 6 .r ."  postings" ;]
	warning-color color-execute
	#-21 0 at-deltaxy
    cell +LOOP
    nn [: ." write "  6 .r ."  postings in " .time ;]
    success-color color-execute cr ;

: g+-import ( -- )
    ?get-me
    ." Read pics metadata" cr   "." get-pic-filenames
    ." Read JSON files" cr      "." json-load-dir
    ." Write entries" cr        write-articles
    ." Get avatars" cr          get-avatars hash-in-avatars
    !save-all-msgs save-keys ;
