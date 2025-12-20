#!/bin/bash

rm -rf ./bin

dotnet publish -c Release stalker-gamma.cli/stalker-gamma.cli.csproj -o bin

codesign --sign - --force ./bin/stalker-gamma.cli

chmod +x ./bin/stalker-gamma.cli
chmod +x ./bin/resources/curl-impersonate/mac/curl-impersonate

xattr -dr com.apple.quarantine ./bin/*
