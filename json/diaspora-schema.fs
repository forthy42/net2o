\ Diaspora scheme

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

require ../hash-table.fs

cs-scope: diaspora

object class{ takeout
    $value: version$
    value: user{}
    value: others_data{}
}class

object class{ user
    $value: username$
    $value: email$
    $value: language$
    $value: private_key$
    value: disable_mail?
    value: show_community_spotlight_in_stream?
    value: auto_follow_back?
    value: auto_follow_back_aspect?
    value: strip_exif?
    value: profile{}
    field: contact_groups[]
    field: contacts[]
    field: posts[]
    field: followed_tags[]
    field: post_subscriptions[]
    field: relayables[]
}class

object class{ profile
    $value: entity_type$
    value: entity_data{}
    field: property_order[]
}class

object class{ others_data
    field: relayables[]
}class

synonym relayables profile
synonym relayables-class profile-class
synonym photos profile
synonym photos-class profile-class

object class{ entity_data
    $value: author$
    $value: author_signature$
    $value: guid$
    $value: parent_guid$
    $value: parent_type$
    64value: edited_at!
    64value: created_at!
    $value: first_name$
    $value: last_name$
    $value: image_url$
    $value: image_url_medium$
    $value: image_url_small$
    $value: remote_photo_path$
    $value: remote_photo_name$
    $value: status_message_guid$
    $value: bio$
    64value: birthday!
    $value: gender$
    $value: location$
    $value: text$
    value: searchable?
    value: public?
    value: positive?
    value: nsfw?
    value: height#
    value: width#
    field: photos[]
}class

object class{ contact_groups
    $value: name$
    value: chat_enabled?
}class

object class{ contacts
    value: sharing?
    value: receiving?
    value: following?
    value: followed?
    $value: person_guid$ \ 128 bit in hex
    $value: person_name$
    $value: account_id$
    $value: public_key$
    field: contact_groups_membership[]
}class

object class{ posts
    field: subscribed_pods_uris[]
    field: subscribed_users_ids[]
    $value: entity_type$
    value: entity_data{}
}class

}scope

: diaspora-scan ( -- )  iso-date
    ['] diaspora >body to schema-scope
    diaspora:takeout-class to outer-class
    ['] diaspora:takeout >body to schema-wid
    ['] noop is process-element ;

0 [IF]
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
