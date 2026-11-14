# SPDX-License-Identifier: Apache-2.0 OR MIT
"""Render the library's XML documentation comments as Markdown.

Reads the documentation file the build writes beside Pain001.Sdk.dll
(GenerateDocumentationFile) and writes docs/api/: an index of the public
types and one page per type with its members, summaries, parameters,
return values, exceptions and remarks. Only the standard library is
used, and nothing is loaded or reflected, so it runs on any Python 3.

    python3 scripts/api_docs.py <Pain001.Sdk.xml> <output-dir>
"""

from __future__ import annotations

import re
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

NAMESPACE = "Pain001.Sdk."
KINDS = {"M": "Methods", "P": "Properties", "F": "Fields", "E": "Events"}


def short(name: str) -> str:
    """Drop namespaces from a type name: System.Threading.CancellationToken -> CancellationToken."""
    name = name.replace("{", "<").replace("}", ">")
    return re.sub(r"(?:[A-Za-z_][A-Za-z0-9_]*\.)+([A-Za-z_][A-Za-z0-9_]*)", r"\1", name)


def cref_text(cref: str, types: set[str]) -> str:
    """A cref as Markdown: a link for this library's types, code otherwise."""
    kind, _, target = cref.partition(":")
    target = target.split("(")[0]
    if target.startswith(NAMESPACE):
        local = target[len(NAMESPACE):]
        type_name = local if kind == "T" else local.rsplit(".", 1)[0]
        label = local.replace("#ctor", type_name)
        if type_name in types:
            return f"[`{label}`]({type_name}.md)"
        return f"`{label}`"
    return f"`{short(target)}`"


def text(node: ET.Element | None, types: set[str]) -> str:
    """The inline Markdown of a documentation element."""
    if node is None:
        return ""
    parts = [node.text or ""]
    for child in node:
        if child.tag == "see" and "cref" in child.attrib:
            parts.append(cref_text(child.attrib["cref"], types))
        elif child.tag == "see" and "langword" in child.attrib:
            parts.append(f"`{child.attrib['langword']}`")
        elif child.tag in {"paramref", "typeparamref"}:
            parts.append(f"`{child.attrib.get('name', '')}`")
        elif child.tag == "c":
            parts.append(f"`{''.join(child.itertext())}`")
        elif child.tag == "para":
            parts.append("\n\n" + text(child, types) + "\n\n")
        else:
            parts.append(text(child, types))
        parts.append(child.tail or "")
    joined = "".join(parts)
    paragraphs = [re.sub(r"\s+", " ", p).strip() for p in joined.split("\n\n")]
    return "\n\n".join(p for p in paragraphs if p)


def signature(member_id: str) -> str:
    """A readable member name with its parameter types."""
    name = member_id.split(":", 1)[1]
    head, _, args = name.partition("(")
    member = head.rsplit(".", 1)[-1].replace("#ctor", head.rsplit(".", 2)[-2])
    if not args and member_id.startswith("M:"):
        return f"{member}()"
    if not args:
        return member
    params = [short(a) for a in split_args(args.rstrip(")"))]
    return f"{member}({', '.join(params)})"


def split_args(args: str) -> list[str]:
    """Split a parameter list on top-level commas."""
    out, depth, current = [], 0, ""
    for ch in args:
        depth += ch in "{(" and 1 or 0
        depth -= ch in "})" and 1 or 0
        if ch == "," and depth == 0:
            out.append(current)
            current = ""
        else:
            current += ch
    return out + [current] if current else out


def member_section(member: ET.Element, types: set[str]) -> list[str]:
    lines = [f"### `{signature(member.attrib['name'])}`", "", text(member.find("summary"), types), ""]
    params = member.findall("param")
    if params:
        lines += ["| Parameter | Description |", "| :--- | :--- |"]
        lines += [f"| `{p.attrib['name']}` | {text(p, types)} |" for p in params]
        lines.append("")
    if member.find("returns") is not None:
        lines += [f"**Returns:** {text(member.find('returns'), types)}", ""]
    exceptions = member.findall("exception")
    if exceptions:
        lines += ["| Exception | When |", "| :--- | :--- |"]
        lines += [f"| {cref_text(e.attrib['cref'], types)} | {text(e, types)} |" for e in exceptions]
        lines.append("")
    if member.find("remarks") is not None:
        lines += [text(member.find("remarks"), types), ""]
    return lines


def type_page(name: str, node: ET.Element, members: list[ET.Element], types: set[str]) -> str:
    lines = [f"# {name}", "", "Namespace `Pain001.Sdk`.", "", text(node.find("summary"), types), ""]
    record_params = node.findall("param")
    if record_params:
        lines += ["## Positional properties", "", "| Property | Description |", "| :--- | :--- |"]
        lines += [f"| `{p.attrib['name']}` | {text(p, types)} |" for p in record_params]
        lines.append("")
    if node.find("remarks") is not None:
        lines += ["## Remarks", "", text(node.find("remarks"), types), ""]
    by_kind: dict[str, list[ET.Element]] = defaultdict(list)
    for member in members:
        by_kind[member.attrib["name"][0]].append(member)
    for kind, title in KINDS.items():
        if by_kind.get(kind):
            lines += [f"## {title}", ""]
            for member in sorted(by_kind[kind], key=lambda m: m.attrib["name"]):
                lines += member_section(member, types)
    lines += ["[All types](index.md)", ""]
    return "\n".join(lines)


def main() -> int:
    source, out = Path(sys.argv[1]), Path(sys.argv[2])
    members = ET.parse(source).getroot().find("members")
    type_nodes, owned = {}, defaultdict(list)
    for member in members:
        member_id = member.attrib["name"]
        kind, target = member_id.split(":", 1)
        if not target.startswith(NAMESPACE) or ".Internal." in target:
            continue
        local = target[len(NAMESPACE):]
        if kind == "T":
            type_nodes[local] = member
        else:
            owned[local.split("(")[0].rsplit(".", 1)[0]].append(member)
    types = set(type_nodes)
    out.mkdir(parents=True, exist_ok=True)
    index = ["# API reference", "",
             "Every public type of `Pain001.Sdk`, rendered from the XML documentation",
             "comments by `scripts/api_docs.py`. The same comments ship in the",
             "package for IDE tooltips.", "",
             "| Type | Summary |", "| :--- | :--- |"]
    for name in sorted(types):
        (out / f"{name}.md").write_text(type_page(name, type_nodes[name], owned[name], types), encoding="utf-8")
        summary = text(type_nodes[name].find("summary"), types).replace("\n\n", " ")
        index.append(f"| [`{name}`]({name}.md) | {summary} |")
    (out / "index.md").write_text("\n".join(index) + "\n", encoding="utf-8")
    print(f"api_docs: wrote {len(types) + 1} pages to {out}/")
    return 0


if __name__ == "__main__":
    sys.exit(main())
