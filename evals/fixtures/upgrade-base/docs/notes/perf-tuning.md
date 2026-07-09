# GC tuning notes

Hand-tuned server GC settings that took a quarter to get right. Values are
load-tested against the 2025 Black Friday traffic capture.

- `ServerGarbageCollection: true`, `ConcurrentGarbageCollection: false`
- Gen0 budget pinned via `GCgen0size` — see commit history before changing.
