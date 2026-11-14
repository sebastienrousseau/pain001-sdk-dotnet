# SPDX-License-Identifier: Apache-2.0 OR MIT
#
# `make check` runs every local gate CI runs: format, build (analysers,
# CA1502 complexity and warnings as errors), unit tests and the
# vulnerable-package audit. Tests that need a server skip unless
# PAIN001_BASE_URL is set; `make conformance` insists on it.

SOLUTION := Pain001.Sdk.sln
CONFIG   := Release

.PHONY: check format format-fix build test audit verify-versions conformance example demo docs-lint

check: format build test audit verify-versions

format:
	dotnet format $(SOLUTION) --verify-no-changes

format-fix:
	dotnet format $(SOLUTION)

build:
	dotnet build $(SOLUTION) -c $(CONFIG)

test: build
	dotnet test $(SOLUTION) -c $(CONFIG) --no-build

# `dotnet list package --vulnerable` exits 0 even when it finds one, so
# the output is checked.
audit:
	@out="$$(dotnet list $(SOLUTION) package --vulnerable --include-transitive 2>&1)"; \
	echo "$$out"; \
	if echo "$$out" | grep -q "has the following vulnerable packages"; then exit 1; fi

# Every file that states the version agrees with the csproj.
verify-versions:
	scripts/verify-release-versions

conformance: build
	@test -n "$$PAIN001_BASE_URL" || { echo "PAIN001_BASE_URL is unset: start pain001 serve first" >&2; exit 1; }
	dotnet test $(SOLUTION) -c $(CONFIG) --no-build

example: build
	dotnet run --project examples/Basic -c $(CONFIG) --no-build

# Renders .github/demo.gif from .github/demo.tape against a real
# `pain001 serve` on 127.0.0.1. The server is $PAIN001_SERVER, else
# `pain001` on PATH; CONTRIBUTING.md shows how to install it.
demo: build
	@server="$${PAIN001_SERVER:-$$(command -v pain001 || true)}"; \
	if [ -z "$$server" ] || [ ! -x "$$server" ]; then \
	  echo "pain001 server not found: set PAIN001_SERVER or put pain001 on PATH (see CONTRIBUTING.md)" >&2; exit 1; \
	fi; \
	command -v vhs > /dev/null || { echo "vhs not found: https://github.com/charmbracelet/vhs" >&2; exit 1; }; \
	PAIN001_SERVER="$$server" vhs .github/demo.tape

docs-lint:
	npx --yes markdownlint-cli2@0.23.3 "**/*.md"
