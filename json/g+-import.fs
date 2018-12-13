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

Variable pics#

".metadata.csv" nip Constant .mtcvs#

: get-pic-filename { d: mtcvs -- }
    mtcvs r/o open-file throw { fd }
    pad $100 + $1000 fd read-line throw 2drop
    pad $100 + $1000 fd read-line throw drop pad $100 + swap
    ',' $split 2drop
    over c@ '"' = negate /string
    2dup + 1- c@ '"' = +
    mtcvs .mtcvs# -
    2dup + 4 - 4 2dup ".png" str= >r ".jpg" str= r> or 0= IF
	[: type ." .jpg" ;] $tmp
    THEN
    2swap pics# #!
    fd close-file throw ;

: get-pic-filenames ( addr u -- )
    2dup open-dir throw { dd } fpath @ >r fpath off fpath also-path dd
    [: { dd } !time { | nn }
	BEGIN
	    pad $100 dd read-dir throw  WHILE
		pad swap 2dup "*.metadata.csv" filename-match IF
		    get-pic-filename
		ELSE  2drop  THEN
		1 +to nn
	REPEAT  drop
	nn [: ." read " . ." pics in " .time ;]
	success-color color-execute cr ;] catch
    fpath $free r> fpath !  dd close-dir throw  throw ;

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

: add-file { dvcs d: file -- }
    [:
	comments:content$ html-untag cr
	comments:link{} ?dup-IF cr >o
	    '[' emit link:title$ type ." ](" link:url$ type ')' emit cr
	    o>  THEN
    ;] file execute>file
    file dvcs .dvcs-add ;

: add-post ( dvcs -- ) "post.md" add-file ;

: add-media { dvcs -- }
    media:url$ basename 2dup delete-file drop
    2dup pics# #@ [: dir@ type ." /" type ;] $tmp 2over symlink ?ior
    dvcs .dvcs-ref ;

: add-album { dvcs -- }
    comments:album{} ?dup-IF
	.album:media[] $@ bounds U+DO
	    dvcs I @ .add-media
	cell +LOOP
    THEN ;

also net2o-base

2Variable post-ref

: add-message ( xt -- )
    project:project$ $@ ?msg-log
    [: sign[ msg-start execute post-ref 2@ chain, ]pksign ;] gen-cmd$
    +last-signed last-signed 2@ >msg-log ;

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
    [: ." comment-" 1 comment# +!@ 0 <# # # # #> type ." .md" ;] $tmp add-file ;

: create>never ( o:comments -- )
    comments:creationTime! comments:updateTime! 64umax 64#-1 sigdate le-128! ;

: add-comments { dvcs -- }
    comments:comments[] $@ bounds U+DO
	I @ >o
	dvcs add-comment
	comments:media{} ?dup-IF  >o dvcs add-media o>  THEN
	comments:author{} .author:mapped-key dvcs >o to my-key o>
	create>never
	"comment" dvcs .(dvcs-ci)  last-msg 2@ post-ref 2!
	dvcs add-plusones
	dvcs add-reshares
	o>
    cell +LOOP ;

: write-out-article ( o:comment -- )
    >dir redate-mode on  comment# off
    dvcs:new-dvcs { dvcsp }
    comments:url$ basename [: ." posts/" type ." /.n2o" ;] $tmp
    .net2o-cache/ 2dup $1FF init-dir drop dirname set-dir throw
    ".n2o/files" touch
    comments:url$ basename dvcsp >o project:project$ $!
    "master" project:branch$ $! save-project o>
    dvcsp add-post
    dvcsp add-album
    comments:media{} ?dup-IF  >o dvcsp add-media o>  THEN
    create>never
    "post" dvcsp .(dvcs-ci)  last-msg 2@ post-ref 2!
    dvcsp add-plusones
    dvcsp add-reshares
    dvcsp add-comments
    dvcsp .dvcs:dispose-dvcs
    dir> redate-mode off ;
