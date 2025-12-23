#!/bin/bash

echo "Uninstalling stalker-gamma-cli"

rm -rf $HOME/.local/share/stalker-gamma-cli
rm $HOME/.local/bin/stalker-gamma-cli

echo "stalker-gamma-cli uninstalled"