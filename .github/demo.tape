# SPDX-License-Identifier: Apache-2.0 OR MIT
#
# Rendered by `make demo` (vhs, https://github.com/charmbracelet/vhs) from
# the repository root, after `make build`. Hidden: start the pain001
# server named by PAIN001_SERVER on 127.0.0.1:8765, wait for its health
# endpoint, and copy the built examples/Basic and the conformance kit's
# synthetic credit-transfer CSV into a throwaway directory. On screen: the
# CSV's amounts and creditors, the example checking health, validating
# and generating an XSD-valid pain.001.001.09 through the SDK, and the
# file's first lines. Nothing leaves the loopback interface.
Output .github/demo.gif

Set Shell "bash"
Set FontSize 14
Set Width 1200
Set Height 640
Set Padding 20
Set Theme "Catppuccin Mocha"

Hide
Type `R="$PWD" && cd "$(mktemp -d /tmp/pain001-demo.XXXX)"`
Enter
Type `"$PAIN001_SERVER" serve --host 127.0.0.1 --port 8765 > .serve.log 2>&1 & SRV=$!`
Enter
Type `for _ in $(seq 1 60); do curl -fsS http://127.0.0.1:8765/api/v1/health > /dev/null 2>&1 && break; sleep 1; done`
Enter
Type `cp -R "$R/examples/Basic/bin/Release/net10.0" basic && cp "$R/conformance/fixtures/credit-transfer.pain.001.001.09.csv" .`
Enter
Type `export PAIN001_BASE_URL=http://127.0.0.1:8765 PS1='$ ' && clear`
Enter
Sleep 8s
Show

Type "cut -d, -f16,17,19 credit-transfer.pain.001.001.09.csv"
Sleep 500ms
Enter
Sleep 3s

Type "basic/Basic credit-transfer.pain.001.001.09.csv payments.xml"
Sleep 500ms
Enter
Sleep 14s

Type "head -n 14 payments.xml"
Sleep 500ms
Enter
Sleep 7s

Hide
Type `kill "$SRV"`
Enter
Sleep 1s
