# UM Templated Note Processor

This solution houses the Templated Note Processor for the Utilization Management Context

**Purpose:** This is an event-driven application that acts responds to Utilization Management domain events to determine if a Call Tracking Note needs to be created. If a note is determined to be required, this lambda contains the business logic to handle the creation of the note via invocation of the Utilization Management API Microservice.

![Architecture Diagram](../../../../images/TemplatedNoteProcessor.drawio.png)

## Table of Contents

[[_TOC_]]

## Debugging Locally

This application can be debugged locally (including setting breakpoints) via the [AWS Lambda .NET Test tool](https://github.com/aws/aws-lambda-dotnet).

## Current UM Domain Events Processed

- EpisodeCreated
- EpisodeUpdatedV2
- GrievanceAndAppealsEpisodeCreated
- GrievanceAndAppealsEpisodeUpdated

## Service Client to Invoke UM API

The UM Service Client that is part of this repository is auto generated using the [NSwag CLI tool](https://github.com/RicoSuter/NSwag).

An easy-to-use just command is provided `just clients` to generate an updated Service Client as UM API evolves over time.
