# SPDX-License-Identifier: Apache-2.0 OR MIT
"""MkDocs hook: point links that leave docs/ at the file on GitHub.

The root Markdown files (DEVELOPMENT.md, SECURITY.md, ...) are included
into the manual, and they link to repository files outside docs/
(DCO.txt, Makefile, examples/). Those links would break in the built
site, and strict mode would reject them. This rewrites each relative
link that resolves outside docs/ to its blob URL on GitHub, and leaves
every other link for MkDocs to validate.
"""

from __future__ import annotations

import posixpath
import re

REPO = "https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/"
LINK = re.compile(r"(\]\()([^)\s#]+)(#[^)\s]*)?(\))")


def on_page_markdown(markdown, page, config, files):  # noqa: ARG001
    """Rewrite relative links that resolve outside docs/."""
    page_dir = posixpath.dirname(page.file.src_uri)

    def rewrite(match: re.Match[str]) -> str:
        target = match.group(2)
        if re.match(r"^[a-z][a-z0-9+.-]*:", target) or target.startswith("/"):
            return match.group(0)
        resolved = posixpath.normpath(posixpath.join("docs", page_dir, target))
        if resolved.startswith("docs/") or resolved == "docs":
            return match.group(0)
        anchor = match.group(3) or ""
        return f"{match.group(1)}{REPO}{resolved}{anchor}{match.group(4)}"

    return LINK.sub(rewrite, markdown)
