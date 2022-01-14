\ Blogger.com Atom feed

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

object class{ atom-tags
    value: ?xml{}
    object class{ ?xml-attrs
	$value: version$
	$value: encoding$
    }class
    xml:class class{ ?xml
    }class
    value: feed{}
    object class{ feed-attrs
	$value: xmlns$
	$value: xmlns:blogger$
    }class
    xml:class class{ feed
	$value: id$
	$value: title$
	field: entry[]
	xml:class class{ entry
	    $value: id$
	    $value: blogger:parent$
	    $value: blogger:type$
	    $value: blogger:status$
	    value: author{}
	    xml:class class{ author
		$value: name$
		$value: uri$
		$value: blogger:type$
	    }class
	    $value: title$
	    $value: content$
	    object class{ content-attrs
		$value: type$
	    }class
	    64value: blogger:created!
	    64value: published!
	    64value: updated!
	    value: blogger:location{}
	    xml:class class{ blogger:location
		$value: blogger:name$
		fvalue: blogger:latitude%
		fvalue: blogger:longitude%
		$value: blogger:span$
	    }class
	    $value: blogger:filename$
	}class
    }class
}class

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
