#!/usr/bin/env bash
# SPDX-License-Identifier: Apache-2.0 OR MIT
#
# Writes the GitHub release notes for v<version> to stdout, in the
# suite's release shape:
#
#   ## Highlights ⭐️      from .github/release-highlights.md (hand-written)
#   ## What's Changed     the pull requests merged since the previous tag
#                         (or, with none, the commits), from the GitHub API
#   ## New Contributors   only when there are any
#   ## Checksums          every file in <dist>, SHA-256
#   **Full Changelog**    the compare URL (the tag's commits for a first tag)
#
#   scripts/release-notes.sh <version> <dist-dir>
#
# Needs git history with tags (fetch-depth 0) and gh with GH_TOKEN. Fails
# on an em dash anywhere in the notes.
set -euo pipefail

version="${1:?usage: release-notes.sh <version> <dist-dir>}"
dist="${2:?usage: release-notes.sh <version> <dist-dir>}"
repo="${GITHUB_REPOSITORY:-sebastienrousseau/pain001-sdk-dotnet}"
tag="v$version"
cd "$(git rev-parse --show-toplevel)"

highlights=$(grep -E '^\* \*\*[^*]+\*\*: ' .github/release-highlights.md || true)
count=$(printf '%s\n' "$highlights" | grep -c . || true)
if [ "$count" -lt 2 ] || [ "$count" -gt 4 ]; then
  echo "release-notes: .github/release-highlights.md needs 2 to 4 '* **Headline**: text' bullets, has $count" >&2
  exit 1
fi

ref="$tag"
git rev-parse -q --verify "refs/tags/$tag" > /dev/null || ref=HEAD
previous=$(git describe --tags --abbrev=0 --match 'v*' "$ref^" 2> /dev/null || true)
range="${previous:+$previous..}$ref"

changes=()
authors=()
while read -r number; do
  [ -n "$number" ] || continue
  line=$(gh pr view "$number" --repo "$repo" --json title,author,url \
    --jq '"* \(.title) by @\(.author.login) in \(.url)"')
  changes+=("$line")
  authors+=("$(gh pr view "$number" --repo "$repo" --json author --jq .author.login)")
done < <(git log --merges --reverse --format='%s' "$range" | sed -n 's/^Merge pull request #\([0-9][0-9]*\) .*/\1/p')

if [ "${#changes[@]}" -eq 0 ]; then
  while read -r sha; do
    login=$(gh api "repos/$repo/commits/$sha" --jq '.author.login // "unknown"')
    changes+=("* $(git log -1 --format=%s "$sha") by @$login in https://github.com/$repo/commit/$sha")
    authors+=("$login")
  done < <(git log --no-merges --reverse --format='%H' "$range")
fi

newcomers=()
if [ -n "$previous" ]; then
  for login in $(printf '%s\n' "${authors[@]}" | sort -u); do
    [ "$login" != unknown ] || continue
    if [ "$(gh api "repos/$repo/commits?sha=$previous&author=$login&per_page=1" --jq length)" = 0 ]; then
      newcomers+=("* @$login made their first contribution in $tag")
    fi
  done
fi

if [ -n "$previous" ]; then
  full="https://github.com/$repo/compare/$previous...$tag"
else
  full="https://github.com/$repo/commits/$tag"
fi

notes=$(
  echo "## Highlights ⭐️"
  echo
  printf '%s\n' "$highlights"
  echo
  echo "## What's Changed"
  echo
  printf '%s\n' "${changes[@]}"
  if [ "${#newcomers[@]}" -gt 0 ]; then
    echo
    echo "## New Contributors"
    echo
    printf '%s\n' "${newcomers[@]}"
  fi
  echo
  echo "## Checksums"
  echo
  echo "SHA-256 of every asset attached to this release:"
  echo
  echo '```text'
  (cd "$dist" && find . -maxdepth 1 -type f ! -name SHA256SUMS | sed 's|^\./||' | LC_ALL=C sort | xargs sha256sum)
  echo '```'
  echo
  echo "**Full Changelog**: $full"
)

if printf '%s' "$notes" | grep -q '—'; then
  echo "release-notes: the notes contain an em dash" >&2
  exit 1
fi
printf '%s\n' "$notes"
