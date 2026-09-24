#!/usr/bin/env bash
# AGENTS.md makes dotnet verification mandatory for every runnable C# snippet,
# but a fresh cloud container ships without a .NET SDK, so this installs it.
# Local machines that already have dotnet on PATH are left alone.
set -uo pipefail

if command -v dotnet >/dev/null 2>&1; then exit 0; fi

if [ -x "$HOME/.dotnet/dotnet" ]; then
  echo "dotnet found at ~/.dotnet — add it to PATH: export PATH=\"\$HOME/.dotnet:\$PATH\""
  exit 0
fi

echo "Installing .NET 10 SDK (needed by the snippet verification harness)..."
if curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh 2>/dev/null; then
  if bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet" >/tmp/dotnet-install.log 2>&1; then
    echo "dotnet $("$HOME/.dotnet/dotnet" --version) installed. Use: export PATH=\"\$HOME/.dotnet:\$PATH\""
  else
    echo "dotnet install failed; see /tmp/dotnet-install.log. C# snippets cannot be verified until it succeeds."
  fi
else
  echo "Could not download the dotnet installer (no network?). C# snippets cannot be verified."
fi
exit 0
