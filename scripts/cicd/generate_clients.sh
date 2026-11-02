#!/bin/bash

run_nswag() {
  local folder=$1

  pushd "$folder" || exit
  npx --yes nswag@14.0.3 run
  popd || exit
}

run_nswag "./app/src/ServiceClients/Config"
