#!/bin/bash

# Get the value of a key from the infraconfig JSON file

infraconfig_file="./infra/src/EventProcessorInfra/infraconfig.json"

export API_VERSION=$(jq -r '.ApiVersion' $infraconfig_file)
echo "API_VERSION: $API_VERSION"
