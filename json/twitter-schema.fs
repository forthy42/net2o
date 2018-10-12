\ net2o template for new files

\ Copyright (C) 2016   Bernd Paysan

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
\ example of Twitter date: "Mon Oct 08 22:27:35 +0000 2018"

scope: weekdays
0 7 enums Sun Mon Tue Wed Thu Fri Sat
}scope
scope: months
1 12 enums Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec
}scope

: twitter-?date ( addr u -- flag )
    (( \( letter c? letter c? letter c? \) \s         \ \1=weekday
       \1 ['] weekdays >body find-name-in 0= ?LEAVE   \ yeah, we can do that!
       \( letter c? letter c? letter c? \) \s         \ \2=month
       \2 ['] months >body find-name-in 0= ?LEAVE     \ yeah, we can do that!
       \( \d \d \) \s                                 \ \3=day
       \( \d \d \) ` : \( \d \d \) ` : \( \d \d \) \s \ \4:\5:\6=time
       {{ ` + \( || \( ` - }} \d \d `? : \d \d \) \s  \ \7=timezone
       \( \d \d \d \d \)                              \ \8=year
    )) ;
: twitter-date>ticks ( -- ticks )
    \8 s>number drop \2 ['] months >body find-name-in name>int execute
    \3 s>number drop ymd2day unix-day0 -
    #24 *
    \4 s>number drop + #60 * \5 s>number drop +
    \7 2 umin s>number drop   #60 *
    \7 dup 2 - /string s>unumber? 2drop over 0< IF - ELSE + THEN -
    #60 * \6 s>number drop +
    #1000000000 um* d>64 ;
previous

: twitter-date ( -- )
    ['] twitter-?date is ?date
    ['] twitter-date>ticks is date>ticks ;

cs-scope: twitter

object class{ tweets
    field: tweet[]
}class

object class{ tweet
    value: retweeted?
    value: truncated?
    value: favorited?
    value: possibly_sensitive?
    $value: source$
    value: entities{}
    value: extended_entities{}
    field: display_text_range[]#
    value: favorite_count#
    value: retweet_count#
    2value: in_reply_to_status_id&
    synonym in_reply_to_status_id_str& in_reply_to_status_id&
    2value: id&
    synonym id_str& id&
    2value: in_reply_to_user_id&
    synonym in_reply_to_user_id_str& in_reply_to_user_id&
    $value: in_reply_to_screen_name$
    64value: created_at!
    $value: full_text$
    $value: lang$
}class

object class{ entities
    field: hashtags[]
    field: media[]
    field: symbols[]
    field: polls[]
    field: user_mentions[]
    field: urls[]
}class

synonym extended_entities entities
synonym extended_entities-class entities-class

object class{ user_mentions
    $value: name$
    $value: screen_name$
    field: indices[]#
    2value: id&
    synonym id_str& id&
}class

object class{ urls
    $value: url$
    $value: expanded_url$
    $value: display_url$
    field: indices[]#
}class

object class{ hashtags
    $value: text$
    field: indices[]#
}class

synonym symbols hashtags
synonym symbols-class hashtags-class

object class{ media
    $value: expanded_url$
    field: indices[]#
    $value: url$
    $value: media_url$
    $value: media_url_https$
    2value: id&
    synonym id_str& id&
    2value: source_status_id&
    synonym source_status_id_str& source_status_id&
    2value: source_user_id&
    synonym source_user_id_str& source_user_id&
    $value: type$
    $value: display_url$
    value: sizes{}
    value: video_info{}
    value: additional_media_info{}
}class

object class{ sizes
    value: thumb{}
    value: small{}
    value: medium{}
    value: large{}
}class

object class{ thumb
    $value: resize$
    value: w#
    value: h#
}class

synonym small thumb
synonym medium thumb
synonym large thumb
synonym small-class thumb-class
synonym medium-class thumb-class
synonym large-class thumb-class

object class{ video_info
    field: aspect_ratio[]#
    value: duration_millis#
    field: variants[]
}class

object class{ variants
    value: bitrate#
    $value: content_type$
    $value: url$
}class

object class{ additional_media_info
    value: monetizable?
}class

object class{ polls
    field: options[]
    64value: end_datetime!
    value: duration_minutes#
}class

object class{ options
    value: position#
    $value: text$
}class

}scope

: twitter-scan ( -- )  twitter-date
    ['] twitter >body to schema-scope
    twitter:tweets-class to outer-class
    ['] twitter:tweets >body to schema-wid
    [: (name) 2drop (name) 2drop s" tweet" key$ $!
	['] noop is before-line ;] is before-line ;

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
