# Legacy firmware version cache synchronization (#187)

The version value cache was already process-wide but its callers used different instance locks. Each individual legacy Firmware instance retains its original semaphore map/scope; capture the same-URI semaphore reference while under the one common lock, then wait and release that reference outside it. Cache reads and successful writes use the fixed lock, never logger, network, response read, wait or release. Key remains exact AbsoluteUri; no scope, eviction, URL ordering, URI formation, timeout or download behavior is changed. Simultaneous first lookups for the same URI across **different Firmware instances** remain allowed as before; the lock protects the map, not cross-instance network coalescing.

```sh
python3 MissionPlannerTests/FirmwareVersionCacheGuard/run-isolated.py
```

The runner extracts exact production field declarations and the method body, substitutes an in-memory HTTP client/stream and a failing-if-locked log. Twelve assertions cover three concurrent independently blocked keys across two instances (including two different per-instance semaphore entries), cross-instance exact key hits after success, same-instance same-key waits, and successful retry after an injected read failure. The old version fails the deterministic shared-lock structural assertion. No actual HTTP client, list loader, URIs, device, or firmware action is executed.

Audit: Firmware.options is an instance field used by a fresh loader per route. FirmwareHistory lists are populated by CLR static initialization and not mutated in the list/version fetch path. Download.CheckHTTPFileExists creates call-local client/request; Settings.UserAgent is read-only in these paths. Same-instance list-element updates under the existing Parallel.ForEach predate this change and are not a new cross-instance shared cache; no unrelated code paths are altered here.
