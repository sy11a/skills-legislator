#!/usr/bin/env bash
# BL-082: one entry point for the .NET substrate — build strict, test, AOT publish smoke.
set -euo pipefail
cd "$(dirname "$0")/../src"
dotnet build -warnaserror --nologo
dotnet test
dotnet publish Legislator.Cli -c Release -r linux-x64 -o ../artifacts/linux-x64 --nologo
