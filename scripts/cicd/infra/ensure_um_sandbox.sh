#!/bin/bash
#########################################################
# Purpose: Ensure that the AWS account is UM Sandbox
#########################################################

aws_account_id=$(aws sts get-caller-identity --query Account --output text)

if [ "$aws_account_id" != "822596811112" ]; then
    echo "AWS Account ID is not UM Sandbox"
    exit 1
fi
echo "AWS Account is POD12 - UM Sandbox"
