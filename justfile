# Ensure that we are connected to um sandbox
sandbox:
    ./scripts/cicd/infra/ensure_um_sandbox.sh

personal:
    ./scripts/cicd/infra/ensure_personal_app_root.sh

# Grant execute permission to all shell scripts in scripts folder
chmod:
    find ./scripts -type f -name "*.sh" -exec chmod +x {} \;

synth:
    ./scripts/cicd/infra/synth.sh

clearcache:
    dotnet nuget locals all --clear

publish: sandbox
    rm -rf ./dist
    ./scripts/cicd/event-processors/publish_event_processor_functions.sh

deploy: sandbox personal
    ./scripts/cicd/infra/deploy.sh

infra: sandbox personal synth deploy
    @echo "Finished infra deployment."

cicd: sandbox personal
    #!/usr/bin/env bash
    set -euxo pipefail
    ./scripts/cicd/event-processors/publish_event_processor_functions.sh
    ./scripts/cicd/infra/synth.sh
    ./scripts/cicd/infra/deploy.sh

unittest:
    #!/usr/bin/env bash
    export LOCAL_PATH=$(pwd)
    export TESTRESULT_PATH=$(pwd)/TestResults
    ./scripts/cicd/tests.sh

# Run unit tests for a specific processor. Example: just test DocumentMetadataProcessor
test processor:
    #!/usr/bin/env bash
    echo "Testing {{processor}}"
    pushd app/test/UnitTests
    if [[ "{{processor}}" = "all" ]]; then
        dotnet test
    else
        dotnet test --filter "FullyQualifiedName~{{processor}}"
    fi
    popd

build:
    #!/usr/bin/env bash
    export LOCAL_PATH=$(pwd)
    dotnet build app/src/UMEventProcessors.sln -c Release

clients:
    ./scripts/cicd/generate_clients.sh
