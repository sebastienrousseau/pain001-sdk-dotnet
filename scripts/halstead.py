# SPDX-License-Identifier: Apache-2.0 OR MIT
"""Halstead difficulty per C# method, against the ceiling of 30.

The .NET analysers enforce the other complexity ceilings (CA1502 for
cyclomatic complexity, SonarAnalyzer S3776, S138 and S104 for cognitive
complexity and length); none computes Halstead metrics, so this does.

A method is a member body: a block or an expression body (`=>`) that
follows a parameter list, an accessor (get, set, init, add, remove) or
a top-level program's statements. Tokens come from Pygments' C# lexer.
Operators are keywords, operators and punctuation; operands are
identifiers and literals. Difficulty is (n1 / 2) * (N2 / n2), with n1
distinct operators, n2 distinct operands and N2 total operands.

    python scripts/halstead.py [--max 30] [paths...]

With no path it scans every tracked *.cs file. Exits 1 when a method is
over the ceiling, printing each offender.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

from pygments.lexers import CSharpLexer
from pygments.token import Comment, Keyword, Literal, Name, Operator, Punctuation, Text, Token

TYPE_WORDS = {"class", "record", "struct", "interface", "namespace", "enum"}
ACCESSORS = {"get", "set", "init", "add", "remove"}


@dataclass
class Method:
    """The tokens of one member body."""

    name: str
    line: int
    operators: list[str] = field(default_factory=list)
    operands: list[str] = field(default_factory=list)

    def add(self, kind: str, value: str) -> None:
        (self.operators if kind == "op" else self.operands).append(value)

    @property
    def difficulty(self) -> float:
        distinct_operands = len(set(self.operands))
        if not distinct_operands:
            return 0.0
        return len(set(self.operators)) / 2 * len(self.operands) / distinct_operands


def classify(tokentype, value: str) -> str | None:
    """'op', 'arg' or None (whitespace and comments)."""
    if tokentype in Text or tokentype in Comment or not value.strip():
        return None
    if tokentype in Name or tokentype in Literal or tokentype in Token.Literal:
        return "arg"
    if tokentype in Keyword and value in {"true", "false", "null", "this", "base"}:
        return "arg"
    if tokentype in Keyword or tokentype in Operator or tokentype in Punctuation:
        return "op"
    return "op"


RAW_STRING = re.compile(r'\$*("{3,}).*?\1', re.DOTALL)


def without_raw_strings(source: str) -> str:
    """Replace each raw string literal by one ordinary literal.

    Pygments' C# lexer splits a raw string literal (\"\"\"...\"\"\") into
    pieces, so its JSON reads as code. Each one becomes a single operand,
    with its line breaks kept after it so line numbers stay right.
    """
    return RAW_STRING.sub(lambda m: '"raw"' + "\n" * m.group(0).count("\n"), source)


def significant(path: Path) -> list[tuple[str, str, int]]:
    """(kind, value, line) for every token that counts."""
    out = []
    line = 1
    for tokentype, value in CSharpLexer().get_tokens(without_raw_strings(path.read_text(encoding="utf-8"))):
        kind = classify(tokentype, value)
        if kind:
            out.append((kind, value.strip(), line))
        line += value.count("\n")
    return out


def methods(path: Path) -> list[Method]:
    """Every member body in the file, and its top-level statements."""
    tokens = significant(path)
    found: list[Method] = []
    top = Method("<top-level statements>", 1)
    stack: list[str] = []  # "type", "method" or "other" per open brace
    current: Method | None = None
    expression: Method | None = None
    since: list[str] = []  # tokens since the last ; { or }
    depth = 0  # braces open inside an expression body
    for kind, value, line in tokens:
        if expression is not None:
            if value == ";" and depth == 0:
                found.append(expression)
                expression = None
                since = []
                continue
            depth += {"{": 1, "}": -1}.get(value, 0)
            expression.add(kind, value)
            continue
        if value == "{":
            opener = block_kind(since, stack)
            if opener == "method" and current is None:
                current = Method(member_name(since), line)
                stack.append("method")
                since = []
                continue
            stack.append(opener if current is None else "other")
            since = []
            if current is not None:
                current.add(kind, value)
            continue
        if value == "}":
            closed = stack.pop() if stack else "other"
            if closed == "method" and current is not None:
                found.append(current)
                current = None
            elif current is not None:
                current.add(kind, value)
            since = []
            continue
        if current is not None:
            current.add(kind, value)
            continue
        if value == "=>" and in_type(stack) and since and (since[-1] == ")" or since[-1] in ACCESSORS):
            expression = Method(member_name(since), line)
            since = []
            continue
        if not in_type(stack) and not any(word in TYPE_WORDS for word in since + [value]) and value != "using":
            top.add(kind, value)
        since = [] if value == ";" else since + [value]
    if len(top.operands) > 1:
        found.append(top)
    return found


def in_type(stack: list[str]) -> bool:
    return bool(stack) and stack[-1] == "type"


def block_kind(since: list[str], stack: list[str]) -> str:
    if any(word in TYPE_WORDS for word in since):
        return "type"
    if in_type(stack) and since and (since[-1] == ")" or since[-1] in ACCESSORS or "where" in since):
        return "method"
    return "other"


def member_name(since: list[str]) -> str:
    if since and since[-1] in ACCESSORS:
        return since[-1]
    # The parameter list is the last "(" that follows a name.
    for index in range(len(since) - 1, 0, -1):
        if since[index] == "(" and since[index - 1].isidentifier():
            return since[index - 1]
    return since[-1] if since else "<anonymous>"


def tracked_sources() -> list[Path]:
    listed = subprocess.run(["git", "ls-files", "*.cs"], capture_output=True, text=True, check=True)
    return [Path(name) for name in listed.stdout.split()]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n", 1)[0])
    parser.add_argument("--max", type=float, default=30.0)
    parser.add_argument("--report", action="store_true", help="print every method, not only offenders")
    parser.add_argument("paths", nargs="*", type=Path)
    args = parser.parse_args()
    offenders = 0
    worst = (0.0, "")
    for path in args.paths or tracked_sources():
        for method in methods(path):
            where = f"{path}:{method.line} {method.name}"
            if method.difficulty > worst[0]:
                worst = (method.difficulty, where)
            if args.report or method.difficulty > args.max:
                print(f"{method.difficulty:6.1f}  {where}")
            if method.difficulty > args.max:
                offenders += 1
    print(f"halstead: worst difficulty {worst[0]:.1f} ({worst[1]}); ceiling {args.max:g}; {offenders} over")
    return 1 if offenders else 0


if __name__ == "__main__":
    sys.exit(main())
