#!/bin/bash
#######################################################
# Purpose: Set environment variables
#######################################################

set -eo pipefail

if [ -z $LOCAL_PATH ]; then
	echo "LOCAL_PATH is not set"
	export LOCAL_PATH=$(pwd)
	export TESTRESULT_PATH=$LOCAL_PATH/TestResults
fi

if [ -n "$NODE_VERSION" ]; then
    echo "Setting nvm to use $NODE_VERSION"
    source ~/bin/nvm/nvm.sh
    nvm use --silent $NODE_VERSION || nvm install $NODE_VERSION --latest-npm
fi

if [ -n "$CDK_VERSION" ]; then
    echo "Setting cdk to use $CDK_VERSION"
    npm install --location=global aws-cdk@$CDK_VERSION
else
    echo "Using the locally installed version of CDK CLI"
    cdk --version
fi
echo "aws cdk version being used: $(cdk --version)"
