#!/bin/bash

# install homebrew if not installed
if ! command -v brew &> /dev/null; then
  /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
  echo >> "${HOME}/.zprofile"
  echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> "${HOME}/.zprofile"
  eval "$(/opt/homebrew/bin/brew shellenv)"
fi

# install git if not in PATH
if ! command -v git &> /dev/null; then
  brew install git
fi

brew install libidn2
brew install zstd
