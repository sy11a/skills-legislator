# OrderService

A brand-new ASP.NET Core Web API for handling orders. Just scaffolded with
`dotnet new webapi` — no domain code yet, but the domain is settled:

- An **order** groups one or more **line items** for a **customer**.
- Orders move through a **status lifecycle**: draft → placed → fulfilled
  (cancellation allowed any time before fulfilment).
- Every line item captures a **unit price snapshot** at order time, so
  later catalog changes never alter a placed order.
