#!/bin/bash
##########################################################################
# Purpose: Ensure LOCAL_APP_ROOT is set for deploying personal stacks
##########################################################################

if [ -z "$LOCAL_APP_ROOT" ]; then
  echo "LOCAL_APP_ROOT is not set. Set it in your ~/.bashrc"
  exit 1
fi
echo "LOCAL_APP_ROOT=$LOCAL_APP_ROOT"
