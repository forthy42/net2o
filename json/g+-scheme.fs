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

object class
    scope: g+
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
    }scope
end-class g+-comments

object class
    scope{ g+
    cs-scope: author
    $value: displayName$
    $value: profilePageUrl$
    $value: avatarImageUrl$
    $value: resourceName$
    }scope
    synonym plusOner author
    synonym resharer author
    }scope
end-class g+-author
synonym g+-plusOner g+-author
synonym g+-resharer g+-author

object class
    scope{ g+
    cs-scope: link
    $value: title$
    $value: url$
    $value: imageUrl$
    }scope
    }scope
end-class g+-link

object class
    scope{ g+
    cs-scope: plusOnes
    value: plusOner{}
    }scope
    }scope
end-class g+-plusOnes

object class
    scope{ g+
    cs-scope: reshares
    value: resharer{}
    }scope
    }scope
end-class g+-reshares

object class
    scope{ g+
    cs-scope: postAcl
    value: collectionAcl{}
    }scope
    }scope
end-class g+-postAcl

object class
    scope{ g+
    cs-scope: collectionAcl
    value: collection{}
    field: users[]
    }scope
    }scope
end-class g+-collectionAcl

object class
    scope{ g+
    cs-scope: collection
    $value: resourceName$
    $value: displayName$
    }scope
    synonym users collection
    }scope
end-class g+-collection
synonym g+-users g+-collection
