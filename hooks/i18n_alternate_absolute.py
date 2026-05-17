"""MkDocs hook: absolute URLs for Material language switcher (GitHub Pages subpath).

mkdocs-static-i18n writes relative alternate links into HTML. On a project site
(site_url with a path, e.g. /cascade-ide/) an extra ``../`` or a URL without a
trailing slash can send users to the org root (https://ai-guiders.github.io/).
Rewrite language-switcher hrefs after each page is rendered.
"""

from __future__ import annotations

import re
from urllib.parse import urljoin

from mkdocs.plugins import event_priority

_REL_HREFLANG_RE = re.compile(r'(href=")((?:\.\./)+[^"]+)(" hreflang="[^"]+")')


@event_priority(-200)
def on_post_page(output, page, config, **kwargs):
    abs_page = getattr(page, "abs_url", None)
    if not abs_page or not output:
        return output

    def _abs_href(match: re.Match[str]) -> str:
        rel = match.group(2)
        return f"{match.group(1)}{urljoin(abs_page, rel)}{match.group(3)}"

    return _REL_HREFLANG_RE.sub(_abs_href, output)
