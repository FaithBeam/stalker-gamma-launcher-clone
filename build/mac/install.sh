#!/bin/bash

SCRIPT_DIR=$(dirname "$0")

echo "Installing stalker-gamma-cli to $HOME/.local/bin/stalker-gamma-cli and $HOME/.local/share/stalker-gamma-cli"

mkdir $HOME/.local/share/stalker-gamma-cli
cp -R $SCRIPT_DIR/* $HOME/.local/share/stalker-gamma-cli
ln -s $HOME/.local/share/stalker-gamma-cli/stalker-gamma-cli $HOME/.local/bin/stalker-gamma-cli

echo "Installed!"
echo "Use stalker-gamma-cli"