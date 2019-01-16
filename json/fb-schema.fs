\ Facebook schema

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

cs-scope: fb

object class{ timeline
    field: status_updates[]
    synonym wall_posts_sent_to_you[] status_updates[]
}class

object class{ status_updates
    $value: title$
    64value: timestamp!
    field: attachments[]
    field: data[]
}class

synonym wall_posts_sent_to_you status_updates
synonym wall_posts_sent_to_you-class status_updates-class

object class{ attachments
    field: data[]
}class

object class{ data
    $value: post$
    64value: update_timestamp!
    value: external_context{}
    value: media{}
}class

object class{ external_context
    $value: url$
}class

object class{ media
    $value: uri$
    $value: description$
    64value: creation_timestamp!
    value: media_metadata{}
    value: thumbnail{}
    field: comments[]
}class

object class{ media_metadata
    value: photo_metadata{}
    value: video_metadata{}
}class

object class{ thumbnail
    $value: uri$
}class

object class{ comments
    64value: timestamp!
    $value: comment$
    $value: author$
    $value: group$
}class

object class{ photo_metadata
    value: iso_speed#
    value: orientation#
    value: original_width#
    value: original_height#
    $value: upload_ip$
}class

object class{ video_metadata
    value: upload_timestamp#
    $value: upload_ip$
}class

}scope

: fb-scan ( -- )
    fixed-width set-encoding \ UTF-8 fuckup for \u
    ['] fb >body to schema-scope
    fb:timeline-class to outer-class
    ['] fb:timeline >body to schema-wid ;

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
