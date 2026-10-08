#!/bin/bash
#######################################################
# Purpose: Synth the CDK
#######################################################

set -eo pipefail

source ./scripts/cicd/infra/setenv.sh

echo ""
echo "** Synthing Infra **"
echo ""

pushd infra
cdk synth --ci

echo "cdk synth complete"
popd
