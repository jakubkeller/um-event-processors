#!/bin/bash
################################################################################
# Purpose: Deploy the application
################################################################################

set -eo pipefail

source ./scripts/cicd/infra/get_api_version.sh
source ./scripts/cicd/infra/setenv.sh

echo ""
echo "** Deploying Infra **"
echo ""

# Reuse the synthed cdk.out from the synth step
cdk_out_path=${1:-infra/cdk.out}

stack_prefix="um"
if [ -n "${LOCAL_APP_ROOT}" ]; then
  stack_prefix=$LOCAL_APP_ROOT
fi

function deploy_stack() {
    cdk deploy "$stack_prefix-$1" --app $cdk_out_path --ci --require-approval never
}

declare -a stacks=($API_VERSION-EventProcessorInfraStack)
for stack in ${stacks[@]}; do deploy_stack ${stack}; done

echo "Deploy completed"
