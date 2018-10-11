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

object class
    cs-scope: comments
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
    }scope
    synonym resharedPost comments
end-class comments-class
synonym resharedPost-class comments-class

object class
    cs-scope: author
    $value: resourceName$
    $value: displayName$
    $value: profilePageUrl$
    $value: avatarImageUrl$
    }scope
end-class author-class
synonym plusOner author
synonym resharer author
synonym voter author
synonym owner author

synonym plusOner-class author-class
synonym resharer-class author-class
synonym voter-class author-class
synonym owner-class author-class

object class
    cs-scope: link
    $value: title$
    $value: url$
    $value: imageUrl$
    }scope
end-class link-class

object class
    cs-scope: plusOnes
    value: plusOner{}
    }scope
end-class plusOnes-class

object class
    cs-scope: reshares
    value: resharer{}
    }scope
end-class reshares-class

object class
    cs-scope: postAcl
    value: collectionAcl{}
    value: communityAcl{}
    value: visibleToStandardAcl{}
    value: isLegacyAcl?
    }scope
end-class postAcl-class

object class
    cs-scope: collectionAcl
    value: collection{}
    field: users[]
    }scope
end-class collectionAcl-class

object class
    cs-scope: communityAcl
    value: community{}
    field: users[]
    }scope
end-class communityAcl-class

object class
    cs-scope: visibleToStandardAcl
    field: users[]
    field: circles[]
    }scope
end-class visibleToStandardAcl-class

object class
    cs-scope: circles
    $value: resourceName$
    $value: displayName$
    $value: type$
    }scope
end-class circles-class

object class
    cs-scope: collection
    $value: resourceName$
    $value: displayName$
    }scope
    synonym users collection
    synonym community collection
end-class collection-class
synonym users-class collection-class
synonym community-class collection-class

object class
    cs-scope: album
    field: media[]
    }scope
end-class album-class

object class
    cs-scope: media
    $value: resourceName$
    $value: url$
    $value: contentType$
    $value: description$
    value: width#
    value: height#
    }scope
end-class media-class

object class
    cs-scope: location
    fvalue: latitude%
    fvalue: longitude%
    $value: displayName$
    $value: physicalAddress$
    }scope
end-class location-class

object class
    cs-scope: poll
    field: choices[]
    $value: totalVotes$
    $value: viewerPollChoiceResourceName$
    }scope
end-class poll-class

object class
    cs-scope: choices
    $value: resourceName$
    $value: description$
    $value: imageUrl$
    $value: voteCount$
    field: votes[]
    }scope
end-class choices-class

object class
    cs-scope: votes
    value: voter{}
    }scope
end-class votes-class

object class
    cs-scope: collectionAttachment
    $value: resourceName$
    $value: displayName$
    $value: permalink$
    $value: coverPhotoUrl$
    value: owner{}
    }scope
end-class collectionAttachment-class

synonym communityAttachment collectionAttachment
synonym communityAttachment-class collectionAttachment-class

}scope
