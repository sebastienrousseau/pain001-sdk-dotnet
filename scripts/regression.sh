#!/usr/bin/env bash
# SPDX-License-Identifier: Apache-2.0 OR MIT
#
# The full gate against a real server, the same steps CI runs:
#
#   1. a pain001 server: $PAIN001_SERVER (a `pain001` executable), or the
#      CI pin from ci/server-requirements.txt installed hash-checked into
#      a temporary virtual environment;
#   2. that server on a free loopback port, and a second one with
#      PAIN001_API_KEY set for the ApiKey example;
#   3. make check (format, build, coverage, audit, versions, Halstead);
#   4. the conformance kit and typed tests against the server;
#   5. every example, and the benchmark smoke run.
#
# Both servers are stopped and the temporary files removed on exit,
# failure included. Nothing outside the temporary directory and the
# build output changes, so it is safe to run again.
#
#   scripts/regression.sh        (or: make regression)
#
# PAIN001_PORT and PAIN001_KEYED_PORT pick the ports instead of free ones.
set -euo pipefail

cd "$(dirname "$0")/.."
work="$(mktemp -d)"
pids=()

cleanup() {
  local pid
  for pid in "${pids[@]}"; do
    kill "$pid" 2> /dev/null || true
    wait "$pid" 2> /dev/null || true
  done
  rm -rf "$work"
}
trap cleanup EXIT

free_port() {
  python3 -c 'import socket; s = socket.socket(); s.bind(("127.0.0.1", 0)); print(s.getsockname()[1]); s.close()'
}

server="${PAIN001_SERVER:-}"
if [ -z "$server" ]; then
  echo "regression: installing the pinned server (ci/server-requirements.txt)"
  python3 -m venv "$work/srv"
  "$work/srv/bin/pip" install --quiet --require-hashes --no-deps -r ci/server-requirements.txt
  server="$work/srv/bin/pain001"
fi
echo "regression: server $("$server" --version)"

start() {
  local port="$1" log="$2"
  shift 2
  (cd "$work" && exec env "$@" "$server" serve --host 127.0.0.1 --port "$port") > "$log" 2>&1 &
  pids+=("$!")
  local _
  for _ in $(seq 1 60); do
    if curl -fsS "http://127.0.0.1:$port/api/v1/health" > /dev/null 2>&1; then
      return 0
    fi
    sleep 1
  done
  echo "regression: the server on port $port did not start:" >&2
  cat "$log" >&2
  return 1
}

port="${PAIN001_PORT:-$(free_port)}"
keyed_port="${PAIN001_KEYED_PORT:-$(free_port)}"
key="regression-$RANDOM$RANDOM"
start "$port" "$work/server.log" PAIN001_API_KEY=
start "$keyed_port" "$work/keyed.log" PAIN001_API_KEY="$key"

export PAIN001_BASE_URL="http://127.0.0.1:$port"
export PAIN001_KEYED_BASE_URL="http://127.0.0.1:$keyed_port"
export PAIN001_KEYED_API_KEY="$key"
echo "regression: servers on $PAIN001_BASE_URL and $PAIN001_KEYED_BASE_URL (with a key)"

make check
make conformance
make examples
make bench-smoke

echo "regression: every gate passed against $("$server" --version)"
