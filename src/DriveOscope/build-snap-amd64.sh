#!/usr/bin/env bash
# Builds the amd64 snap into src/ (based on libExamples/ctrlx/datalayer.provider.simple/build-snap-amd64.sh).
# Trimming is off: EAL.dll is a closed library and may load types by reflection.
# Snapcraft runs in destructive mode (no LXD needed): the part only dumps ./publish and the
# build host is Ubuntu 24.04, the same as the core24 base.
set -e
cd "$(dirname "$0")"
TARGET_DOTNET=linux-x64
PUBLISH_DIR=./publish

export DOTNET_SYSTEM_NET_DISABLEIPV6="${DOTNET_SYSTEM_NET_DISABLEIPV6:-1}"

rm -rf ${PUBLISH_DIR}
dotnet publish --configuration Release --runtime ${TARGET_DOTNET} --property:PublishDir=${PUBLISH_DIR} \
  --self-contained true /property:GenerateFullPaths=true /property:PublishSingleFile=true \
  /property:PublishTrimmed=false /property:DebugType=None /property:DebugSymbols=false

snapcraft clean --destructive-mode
# The finished snap goes to src/; the intermediate folders are removed afterwards.
snapcraft pack --destructive-mode --build-for=amd64 --output ..
snapcraft clean --destructive-mode
rm -rf ${PUBLISH_DIR} parts stage prime overlay .craft
