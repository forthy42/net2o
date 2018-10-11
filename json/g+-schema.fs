\ g+ scheme

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

also regexps
Charclass [blT] bl +char 'T' +char
: ?date ( addr u -- flag )
    (( \( \d \d \d \d \) ` - \( \d \d \) ` - \( \d \d \) [blT] c?
    \( \d \d \) ` : \( \d \d \) ` : \( \d \d \)
    {{ ` . \( {++ \d \d \d ++} \) || \( \) }}
    {{ ` Z \( \) \( \) ||
       {{ ` + \( || \( ` - }} \d \d `? : \d \d \)
    }} )) ;
: date>ticks ( -- ticks )
    \1 s>number drop \2 s>number drop \3 s>number drop ymd2day unix-day0 -
    #24 *
    \4 s>number drop + #60 * \5 s>number drop +
    \8 2 umin s>number drop   #60 *
    \8 dup 2 - /string s>unumber? 2drop over 0< IF - ELSE + THEN -
    #60 * \6 s>number drop +
    #1000000000 um*
    \7 s>unumber? 2drop
    case \7 nip
	3 of  #1000000 um*  endof
	6 of  #1000    um*  endof
	0 swap
    endcase  d+
    d>64 ;
previous

cs-scope: g+

object class{ comments
    $value: resourceName$
    $value: url$
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
    $value: content$
    field: comments[]
    field: plusOnes[]
    field: reshares[]
    value: postAcl{} \ only for message, not for comment
}class

synonym resharedPost comments
synonym resharedPost-class comments-class

object class{ author
    $value: resourceName$
    $value: displayName$
    $value: profilePageUrl$
    $value: avatarImageUrl$
}class

synonym plusOner author
synonym resharer author
synonym voter author
synonym owner author

synonym plusOner-class author-class
synonym resharer-class author-class
synonym voter-class author-class
synonym owner-class author-class

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

synonym users-class collection-class
synonym community-class collection-class

object class{ album
    field: media[]
}class

object class{ media
    $value: resourceName$
    $value: url$
    $value: contentType$
    $value: description$
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
synonym communityAttachment-class collectionAttachment-class

}scope
