#!/bin/bash
set -exo pipefail

echo ""
echo "** Running Tests **"
echo ""
echo "TESTRESULT_PATH: $TESTRESULT_PATH"
rm -rf $TESTRESULT_PATH

source ./scripts/cicd/infra/setenv.sh

for projFile in $(find $LOCAL_PATH/app/test -name "*UnitTests.csproj"); do
(
    dirName=$(basename $(dirname $projFile))

    echo ""
    echo "Testing: $projFile"
    echo ""

    cd $(dirname $projFile)
    if [[ -f DO_NOT_AUTOTEST ]]; then exit 0; fi
    dotnet test $projFile --logger "trx" --results-directory $TESTRESULT_PATH --settings CodeCoverage.runsettings --collect "XPlat Code Coverage"
    echo "Results output to $TESTRESULT_PATH"
)
done

echo "Tests executed successfully."
