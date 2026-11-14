#!/usr/bin/env bash
# SPDX-License-Identifier: Apache-2.0 OR MIT
#
# Runs every example in examples/ against the server at PAIN001_BASE_URL
# and fails on the first one that exits non-zero. Each example checks its
# own result. Build first (make build); CI and scripts/regression.sh call
# this after the build.
#
# When PAIN001_KEYED_BASE_URL and PAIN001_KEYED_API_KEY name a second
# server started with PAIN001_API_KEY set, the ApiKey example also runs
# against it, where a wrong key must be refused with HTTP 401.
set -euo pipefail

cd "$(dirname "$0")/.."
: "${PAIN001_BASE_URL:?PAIN001_BASE_URL is unset: start pain001 serve first}"
config="${CONFIG:-Release}"
out="$(mktemp -d)"
trap 'rm -rf "$out"' EXIT

run() {
  local name="$1"
  shift
  echo "::group::examples/$name"
  dotnet run --project "examples/$name" -c "$config" --no-build -- "$@"
  echo "::endgroup::"
}

run Basic conformance/fixtures/credit-transfer.pain.001.001.09.csv "$out/basic.xml"
grep -q '<NbOfTxs>5</NbOfTxs>' "$out/basic.xml"
run Health
run Validate
run Generate "$out/generate.xml"
run Check
run Faces
run ProjectFace berlin_group
run RawRequest
run ErrorHandling
run Timeouts
run ApiKey

if [ -n "${PAIN001_KEYED_BASE_URL:-}" ]; then
  echo "::group::examples/ApiKey against the keyed server"
  PAIN001_BASE_URL="$PAIN001_KEYED_BASE_URL" PAIN001_API_KEY="${PAIN001_KEYED_API_KEY:?}" \
    dotnet run --project examples/ApiKey -c "$config" --no-build \
    | tee "$out/apikey.txt"
  grep -q 'refused, The pain001 API returned HTTP 401' "$out/apikey.txt"
  echo "::endgroup::"
fi

echo "every example ran against $PAIN001_BASE_URL"
