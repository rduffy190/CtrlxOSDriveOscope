#!/bin/bash
# Regenerates the C# FlatBuffers code (Generated/) and the binary schemas (bfbs/) from the
# design schemas in ../../designDocs/interface. The schemas are the source of truth; never edit the output.
set -e
cd "$(dirname "$0")"

flatc="${FLATC:-$HOME/ctrlx-automation-sdk/bin/oss.flatbuffers/ubuntu24-gcc-x64/flatc}"
schemas=(../../designDocs/interface/*.fbs)

if [ ! -x "$flatc" ]; then
  echo "flatbuffers compiler $flatc could not be found (set FLATC=...)"
  exit 1
fi

rm -rf Generated bfbs
mkdir -p Generated bfbs
"$flatc" --csharp --gen-object-api --gen-compare --suppress-validate-version --no-warnings -o Generated "${schemas[@]}"
"$flatc" --schema --binary --bfbs-comments --bfbs-builtins --no-warnings -o bfbs "${schemas[@]}"
