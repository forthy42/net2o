\ g+ scheme

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

require parser.fs

cs-scope: g+

object class{ comments
    $value: resourceName$
    $value: url$
    $value: content$
    $value: activityId$
    $value: postKind$
    synonym commentActivityId$ activityId$
    synonym postUrl$ url$ \ comment has postUrl$ instead of url$
    64value: creationTime!
    64value: updateTime!
    value: author{}
    value: album{}
    value: media{}
    value: location{}
    value: link{}
    value: resharedPost{}
    value: poll{}
    value: collectionAttachment{}
    value: communityAttachment{}
    field: comments[]
    field: plusOnes[]
    field: reshares[]
    value: postAcl{} \ only for message, not for comment
}class

synonym resharedPost comments

object class{ author
    $value: resourceName$
    $value: displayName$
    $value: profilePageUrl$
    $value: avatarImageUrl$
    value: mapped-key
    : dispose ( o:author -- )
    addr resourceName$ $free
    addr displayName$ $free
    addr profilePageUrl$ $free
    addr avatarImageUrl$ $free
    dispose ;
}class

synonym plusOner author
synonym resharer author
synonym voter author
synonym owner author

object class{ link
    $value: title$
    $value: url$
    $value: imageUrl$
}class

object class{ plusOnes
    value: plusOner{}
}class

object class{ reshares
    value: resharer{}
}class

object class{ postAcl
    value: collectionAcl{}
    value: communityAcl{}
    value: visibleToStandardAcl{}
    value: isLegacyAcl?
    value: isPublic?
}class

object class{ collectionAcl
    value: collection{}
    field: users[]
}class

object class{ communityAcl
    value: community{}
    field: users[]
}class

object class{ visibleToStandardAcl
    field: users[]
    field: circles[]
}class

object class{ circles
    $value: resourceName$
    $value: displayName$
    $value: type$
}class

object class{ collection
    $value: resourceName$
    $value: displayName$
}class

synonym users collection
synonym community collection

object class{ album
    field: media[]
}class

object class{ media
    $value: resourceName$
    $value: url$
    $value: contentType$
    $value: description$
    $value: localFilePath$
    value: width#
    value: height#
}class

object class{ location
    fvalue: latitude%
    fvalue: longitude%
    $value: displayName$
    $value: physicalAddress$
}class

object class{ poll
    field: choices[]
    value: totalVotes#
    $value: viewerPollChoiceResourceName$
}class

object class{ choices
    $value: resourceName$
    $value: description$
    $value: imageUrl$
    $value: imageLocalFilePath$
    value: voteCount#
    field: votes[]
}class

object class{ votes
    value: voter{}
}class

object class{ collectionAttachment
    $value: resourceName$
    $value: displayName$
    $value: permalink$
    $value: coverPhotoUrl$
    value: owner{}
}class

synonym communityAttachment collectionAttachment

}scope

Variable authors#
Variable names#
Variable avatars[]

true Value first-key?

also g+

[IFDEF] dummy-key
    [IFUNDEF] entries[]
	$Variable entries[] \ list of google+ entries
    [THEN]
    require g+-import.fs
[THEN]

: dedup-author { a -- }
    a @ >o
    author:resourceName$ basename authors# #@ 0= IF
	drop  o { w^ x }
	x cell  author:resourceName$ basename authors# #!
	x cell  author:displayName$ names# #!
	[IFDEF] dummy-key
	    key-author +avatar-author
	[THEN]
    ELSE
	@ a !
	author:dispose
    THEN
    o> ;

: dedup-authors ( o:comment -- )
    addr comments:author{} dedup-author
    comments:resharedPost{} ?dup-IF  >o recurse o>  THEN
    comments:comments[] $@ bounds U+DO
	I @ >o recurse o>
    cell +LOOP
    comments:plusOnes[] $@ bounds U+DO
	I @ >o addr plusOnes:plusOner{} dedup-author o>
    cell +LOOP
    comments:reshares[] $@ bounds U+DO
	I @ >o addr reshares:resharer{} dedup-author o>
    cell +LOOP
    comments:poll{} ?dup-IF
	.poll:choices[] $@ bounds U+DO
	    I @ .choices:votes[] $@ bounds U+DO
		I @ >o addr votes:voter{} dedup-author o>
	    cell +LOOP
	cell +LOOP
    THEN
    comments:collectionAttachment{} ?dup-IF
	>o addr collectionAttachment:owner{} dedup-author o>
    THEN ;

: g+-scan ( -- )  iso-date
    ['] g+ >wordlist to schema-scope
    g+:comments:class to outer-class
    ['] g+:comments >wordlist to schema-wid
    ['] dedup-authors is process-element ;

g+-scan

previous

\\\
Local Variables:
forth-local-words:
    (
     (("class{") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
     (("}class") definition-ender (font-lock-keyword-face . 1))
    )
forth-local-indent-words:
    (
     (("class{") (0 . 2) (0 . 2))
     (("}class") (-2 . 0) (0 . -2))
    )
End:
[THEN]
