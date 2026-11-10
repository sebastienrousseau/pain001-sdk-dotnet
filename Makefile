# SPDX-License-Identifier: Apache-2.0 OR MIT
#
# `make check` runs every local gate CI runs: format, build (analysers,
# CA1502 complexity and warnings as errors), unit tests and the
# vulnerable-package audit. Tests that need a server skip unless
# PAIN001_BASE_URL is set; `make conformance` insists on it.

SOLUTION := Pain001.Sdk.sln
CONFIG   := Release

.PHONY: check format format-fix build test audit conformance example docs-lint

check: format build test audit

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

conformance: build
	@test -n "$$PAIN001_BASE_URL" || { echo "PAIN001_BASE_URL is unset: start pain001 serve first" >&2; exit 1; }
	dotnet test $(SOLUTION) -c $(CONFIG) --no-build

example: build
	dotnet run --project examples/Basic -c $(CONFIG) --no-build

docs-lint:
	npx --yes markdownlint-cli2@0.23.3 "**/*.md"
