#!/bin/bash
set -eo pipefail

echo ""
echo "** Publishing Event Processor Functions **"
echo ""

ROOT_DIR=$(pwd)

processors=(
    TemplatedNoteProcessor
    DocumentMetadataProcessor
    SendEmailProcessor
    EmailProcessor
    TcnOralNotificationProcessor
    TcnPollingProcessor
    RtpbReceiveProcessor
)

for processor in "${processors[@]}"; do
    pushd $ROOT_DIR/app/src/$processor

    echo ""
    echo "Publishing $processor"
    echo ""

    dotnet publish -c Release \
        -o $ROOT_DIR/dist/event-processors/$processor/release/publish \
        -r linux-x64 \
        --self-contained false \
        -p:PublishReadyToRun=true

    echo "$processor published to $ROOT_DIR/dist/event-processors/$processor/release/publish"
    popd
done

echo "Publish Success!!"
