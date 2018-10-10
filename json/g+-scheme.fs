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

scope: g+

object class
    cs-scope: comments
    $value: url$
    synonym postUrl$ url$ \ comment has postUrl$ instead of url$
    64value: creationTime!
    64value: updateTime!
    value: author{}
    value: media{}
    value: link{}
    $value: content$
    $value: resourceName$
    field: comments[]
    field: plusOnes[]
    field: reshares[]
    value: postAcl{} \ only for message, not for comment
    }scope
end-class comments-class

object class
    cs-scope: author
    $value: displayName$
    $value: profilePageUrl$
    $value: avatarImageUrl$
    $value: resourceName$
    }scope
    synonym plusOner author
    synonym resharer author
end-class author-class
synonym plusOner-class author-class
synonym resharer-class author-class

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
    }scope
end-class postAcl-class

object class
    cs-scope: collectionAcl
    value: collection{}
    field: users[]
    }scope
end-class collectionAcl-class

object class
    cs-scope: collection
    $value: resourceName$
    $value: displayName$
    }scope
    synonym users collection
end-class collection-class
synonym users-class collection-class

}scope
