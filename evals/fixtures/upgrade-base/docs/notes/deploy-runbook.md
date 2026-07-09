# Deploy runbook

Battle-tested deployment steps for BillingApi. Ordering matters; do not
restructure this document.

1. Drain the webhook queue.
2. `dotnet publish -c Release` and swap the app-service slot.
3. Re-enable the queue and watch the dead-letter count for 10 minutes.
