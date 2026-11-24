---
name: tw-dotnet-diagnostics
description: "Find where CPU, allocations, or memory go in a running .NET service with Microsoft's diagnostics CLIs (dotnet-counters, dotnet-trace, dotnet-gcdump), pinned as local tools. Use when a service is slow, CPU-heavy, allocating or growing memory, contended, or when asked to profile an endpoint. Dev only: never add these as package references."
when-to-use: profile, performance, slow endpoint, hot path, CPU, allocations, allocation rate, GC, memory growth, memory leak, heap, lock contention, thread pool starvation, dotnet-counters, dotnet-trace, dotnet-gcdump, nettrace, gcdump, speedscope
---

# .NET runtime diagnostics

Three Microsoft CLIs answer "where does the time or memory go" in a live process with no code
change. They are local tools in `.config/dotnet-tools.json`: run `dotnet tool restore` once, then
call them as `dotnet <tool>` from inside the repository. They are dev tools; no product project
references them.

| Question | Tool |
|----------|------|
| Is the process busy, allocating, collecting, contended, or queueing? | `dotnet-counters` |
| Which methods burn the time? Which call sites allocate? | `dotnet-trace` |
| What is alive on the heap right now, by type? | `dotnet-gcdump` |

Start with counters. They say which of the other two to run.

## 1. Find the process and drive load

```bash
dotnet dotnet-trace ps | grep <project-executable>    # e.g. api-server, web-server
```

Under Aspire each project resource is its own process, named after the project. A single service
also runs on its own: `ASPNETCORE_URLS=http://127.0.0.1:<port> dotnet bin/<config>/<tfm>/<project>.dll`.

Send steady requests to the endpoint for the whole capture window. An idle process shows only
waiting threads.

## 2. Counters: the health snapshot

```bash
dotnet dotnet-counters collect -p <pid> --duration 00:00:10 --format json -o counters.json
```

`collect` writes a file and exits; `monitor` is a live screen and suits a human, not an agent.
Read the last value of each counter:

- `dotnet.process.cpu.time` (seconds per second; divide by `dotnet.process.cpu.count` for share)
- `dotnet.gc.heap.total_allocated` (bytes per second) and `dotnet.gc.pause.time`
- `dotnet.monitor.lock_contentions` and `dotnet.thread_pool.queue.length`
- `dotnet.process.memory.working_set` (growth across runs points to retention, so use gcdump)

## 3. Trace: hot methods and allocating call sites

CPU (sampled thread stacks):

```bash
dotnet dotnet-trace collect -p <pid> --profile dotnet-sampled-thread-time --duration 00:00:00:10 -o cpu.nettrace
dotnet dotnet-trace report cpu.nettrace topN -n 25              # exclusive
dotnet dotnet-trace report cpu.nettrace topN -n 25 --inclusive
```

The sampler sees waiting threads too. Thread-pool and socket wait frames
(`LowLevelLifoSemaphore.Block`, `WaitHandle.WaitOneNoCheck`, `WaitForSocketEvents`) top the list
on any web server. Skip them and read the first application, framework, or library frames.

Allocations (sampled about every 100 KB allocated):

```bash
dotnet dotnet-trace collect -p <pid> --profile gc-verbose --duration 00:00:00:10 -o alloc.nettrace
dotnet dotnet-trace report alloc.nettrace topN -n 25
```

On a `gc-verbose` trace, `topN` ranks the methods on the allocating stacks. It does not name the
allocated types; use gcdump for types.

`topN` cuts long generic names (`__Canon]].MoveNext()`), and `-v` does not restore them. For full
frame names convert the trace and list its frames:

```bash
dotnet dotnet-trace convert alloc.nettrace --format speedscope -o alloc
python3 -c "import json,sys; [print(f['name']) for f in json.load(open(sys.argv[1]))['shared']['frames']]" alloc.speedscope.json | grep <Namespace>
```

The same file opens as a flame graph at speedscope.app.

## 4. Heap: what is alive, by type

```bash
dotnet dotnet-gcdump collect -p <pid> -o heap.gcdump
dotnet dotnet-gcdump report heap.gcdump | head -40
```

`collect` forces a full GC and pauses the target; use it on a dev process only. The report lists
retained objects by type and size. It shows what survives, not allocation churn. Two dumps a few
minutes apart under the same load show growth.

## Rules

- Capture for 10 seconds under load. Longer runs grow the file, not the signal.
- Keep `.nettrace`, `.etlx`, `.gcdump`, and speedscope files out of the repository; write them to a
  temp directory.
- Report numbers with the tool output that backs them: counter values, `topN` rows, report lines.
- `dotnet-trace collect-linux` needs root and traces the whole machine. Use `collect`.
