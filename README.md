# UM Event Processors

This repo houses the event processor Lambdas for the Utilization Management bounded context and the Infrastructure as Code (IaC) to deploy them to AWS via the `EventProcessorInfraStack` CDK stack.

**Processors:** TemplatedNoteProcessor, EmailProcessor, SendEmailProcessor, TcnOralNotificationProcessor, TcnPollingProcessor, DocumentMetadataProcessor

---

## Deployment Sequencing (IAM Role Name Conflict)

These processors were migrated from `um-api`'s `ApiInfraStack`. Both stacks use the same explicit IAM role name pattern:

```
{AppRoot}-{constructId}-role-access-appclient
```

CloudFormation will fail to create the new roles if the old stack still owns them. **For each environment**, the cutover must follow this order:

1. **Deploy `um-api`** (with processors removed) → CloudFormation destroys the old Lambda, SQS, EventBridge, and IAM role resources in that environment
2. **Verify** no events are being dropped during the gap (EventBridge discards unmatched events — confirm this is acceptable per environment)
3. **Deploy `um-event-processors`** → new resources are created and begin processing

Repeat for each environment in order: **sandbox → dev → test → stage → prod**

> Note: The `um-api` stack must remain deployed — `EventProcessorInfraStack` reads the `/{AppRoot}/api/{ApiVersion}/UMApiCustomBaseURL` SSM parameter that is written by `ApiInfraStack`.
