#!/bin/bash
set -eo pipefail

if [ -z "$1" ]
  then
    echo "Branch type was not specified"
    exit 1
  else
    BRANCH_TYPE=$1
fi

if [ -z "$2" ]
  then
    echo "Version was not specified"
    exit 1
  else
    BRANCH_VERSION=$2
fi

case "$BRANCH_TYPE" in
  hotfix)
    SOURCE_BRANCH="main"
    ;;
  release)
    SOURCE_BRANCH="develop"
    ;;
  bugfix)
    read -p "Enter the release number to base the bugfix on: " SOURCE_VERSION
    SOURCE_BRANCH="release/$SOURCE_VERSION"
    ;;
  *)
    echo "Invalid BRANCH_TYPE: $BRANCH_TYPE"
    exit 1
    ;;
esac

# Ensure working directory in version branch clean
git update-index -q --refresh
if ! git diff-index --quiet HEAD --; then
  echo "Working directory not clean, please commit your changes first"
  git clean -f
  exit 1
fi

git clean -ffdx

echo "Starting $BRANCH_TYPE: $BRANCH_VERSION"

git fetch origin
git checkout main
git pull
git checkout develop
git pull
# git reset --hard
# git clean -df

git flow $BRANCH_TYPE start $BRANCH_VERSION $SOURCE_BRANCH
git flow $BRANCH_TYPE publish $BRANCH_VERSION
