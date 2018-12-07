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
	    [: ." -o g+:" author:resourceName$ basename type ." .png "
		type ;] $tmp avatars[] $+[]!
	THEN
    ELSE  2drop  THEN ;

: execute>file ( ... xt addr u -- )
    r/w create-file throw dup >r outfile-execute
    r> close-file throw ;

: add-post { dvcs -- }
    comments:content$ [: html-untag cr ;] "post.md" execute>file
    "post.md" dvcs .dvcs-add ;

: add-media { dvcs -- }
    media:url$ basename 2dup
    [: dir@ type ." /" 2dup type ;] $tmp symlink ?ior
    dvcs .dvcs-ref ;

: add-album { dvcs -- }
    comments:album{} ?dup-IF
	.album:media[] $@ bounds U+DO
	    dvcs I @ .add-media
	cell +LOOP
    THEN ;

also net2o-base

: add-message ( xt -- )
    project:project$ $@ ?msg-log
    [: sign[ msg-start execute ( ?chain, ) msg> ;] gen-cmd$ >msg-log ;

: add-plusones { dvcs -- }
    comments:plusOnes[] $@ bounds U+DO
	I @ .plusOnes:plusOner{} .author:mapped-key dvcs >o to my-key
	[: '👍' ulit, msg-like ;] add-message drop 2drop o>
    cell +LOOP ;

: add-reshares { dvcs -- }
    comments:reshares[] $@ bounds U+DO
	I @ >o reshares:resharer{} .author:mapped-key dvcs >o to my-key
	[: '🔃' ( '🐙' ) ulit, msg-like ;] add-message drop 2drop o>
    cell +LOOP ;

previous

: add-comment { dvcs -- }
    comments:content$ [: html-untag cr ;]
    comments:url$ basename [: type ." .md" ;] $tmp 2dup 2>r execute>file
    2r> dvcs .dvcs-add ;

: add-comments { dvcs -- }
    comments:comments[] $@ bounds U+DO
	I @ >o
	dvcs add-comment
	dvcs add-media
	comments:author{} .author:mapped-key dvcs >o to my-key o>
	"comment" dvcs .(dvcs-ci)
	dvcs add-plusones
	dvcs add-reshares
	o>
    cell +LOOP ;

: write-out-article ( o:comment -- )
    dvcs:new-dvcs { dvcsp }
    comments:resourceName$ basename [: ." posts/" type ." /.n2o" ;] $tmp
    .net2o-cache/ 2dup $1FF init-dir '/' -scan set-dir throw
    ".n2o/files" touch
    comments:url$ basename dvcsp >o project:project$ $! "master" project:branch$ $! o>
    dvcsp add-post
    dvcsp add-album
    dvcsp add-media
    "post" dvcsp .(dvcs-ci)
    dvcsp add-plusones
    dvcsp add-reshares
    dvcsp add-comments
    dvcsp .dvcs:dispose-dvcs ;
