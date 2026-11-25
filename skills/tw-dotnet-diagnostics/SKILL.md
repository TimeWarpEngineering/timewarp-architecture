---
name: tw-dotnet-diagnostics
description: "Find where CPU, allocations, or memory go in a running .NET service with Microsoft's diagnostics CLIs (dotnet-counters, dotnet-trace, dotnet-gcdump), pinned as local tools. Use when a service is slow, CPU-heavy, allocating or growing memory, contended, or when asked to profile an endpoint. Dev only: never add these as package references."
when-to-use: profile, performance, slow endpoint, hot path, CPU, allocations, allocation rate, GC, memory growth, memory leak, heap, lock contention, thread pool starvation, dotnet-counters, dotnet-trace, dotnet-gcdump, nettrace, gcdump, speedscope
---

# .NET runtime diagnostics

Three Microsoft CLIs answer "where does the time or memory go" in a live process with no code
change. They are dev tools; no product project references them. Run them as local tools, pinned
in the repository's `.config/dotnet-tools.json`. A generated app does not get that manifest, so
add these entries to it first (create it with `dotnet new tool-manifest` if it is missing):

```json
"dotnet-counters": { "version": "10.0.750501", "commands": [ "dotnet-counters" ], "rollForward": true },
"dotnet-trace":    { "version": "10.0.750501", "commands": [ "dotnet-trace" ],    "rollForward": true },
"dotnet-gcdump":   { "version": "10.0.750501", "commands": [ "dotnet-gcdump" ],   "rollForward": true }
```

`rollForward: true` is required: the packages target net8.0 and run on a newer runtime only by
rolling forward. Then run `dotnet tool restore` once and call each as `dotnet <tool>` from inside
the repository. Write every capture under `/tmp`, as the examples do.

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
dotnet dotnet-counters collect -p <pid> --duration 00:00:10 --format json -o /tmp/counters.json
```

`collect` writes a file and exits; `monitor` is a live screen and suits a human, not an agent.

The file is `{ "TargetProcess", "StartTime", "Events": [...] }`. Each event has `name`, `tags`,
`counterType`, and `value`. The runtime keeps `dotnet.process.cpu.time`,
`dotnet.gc.heap.total_allocated`, `dotnet.gc.pause.time`, and `dotnet.monitor.lock_contentions`
as cumulative totals. The 10.0.750501 exporter writes each of them as the delta over one refresh
interval (default 1 s): `counterType` is `Rate` and the unit in `name` ends in `/ 1 sec`, for
example `dotnet.gc.heap.total_allocated (By / 1 sec)`. Gauges such as
`dotnet.process.cpu.count ({cpu})` are `Metric` and carry the current value. Match on the name
before ` (`, and average the `Rate` events over the window instead of reading one:

- CPU: `dotnet.process.cpu.time (s / 1 sec)` comes as two series, `tags` `cpu.mode=user` and
  `cpu.mode=system`. Their sum is cores busy; divide by `dotnet.process.cpu.count` for share.
- Allocation rate: `dotnet.gc.heap.total_allocated (By / 1 sec)`, bytes per second.
- GC pause fraction: `dotnet.gc.pause.time (s / 1 sec)`, seconds paused per second (0.014 is 1.4 %).
- Contention and queueing: `dotnet.monitor.lock_contentions` per second,
  `dotnet.thread_pool.queue.length` (gauge).
- `dotnet.process.memory.working_set` (gauge; growth across runs points to retention, so use gcdump).

```bash
python3 -c "
import json,sys,collections
s=collections.defaultdict(list); last={}
for e in json.load(open(sys.argv[1]))['Events']:
  n=e['name'].split(' (')[0]
  if e['counterType']=='Rate': s[(n,e['tags'])].append(e['value'])
  else: last[(n,e['tags'])]=e['value']
for k,v in sorted(s.items()): print('rate ',k,sum(v)/len(v))
for k,v in sorted(last.items()): print('gauge',k,v)" /tmp/counters.json
```

Sum the two CPU rows for cores busy.

## 3. Trace: hot methods and allocating call sites

CPU (sampled thread stacks):

```bash
dotnet dotnet-trace collect -p <pid> --profile dotnet-sampled-thread-time --duration 00:00:00:10 -o /tmp/cpu.nettrace
dotnet dotnet-trace report /tmp/cpu.nettrace topN -n 25              # exclusive
dotnet dotnet-trace report /tmp/cpu.nettrace topN -n 25 --inclusive
```

The sampler sees waiting threads too. Thread-pool and socket wait frames
(`LowLevelLifoSemaphore.Block`, `WaitHandle.WaitOneNoCheck`, `WaitForSocketEvents`) top the list
on any web server. Skip them and read the first application, framework, or library frames.

Allocations (sampled about every 100 KB allocated):

```bash
dotnet dotnet-trace collect -p <pid> --profile gc-verbose --duration 00:00:00:10 -o /tmp/alloc.nettrace
dotnet dotnet-trace report /tmp/alloc.nettrace topN -n 25
```

On a `gc-verbose` trace, `topN` ranks the methods on the allocating stacks. It does not name the
allocated types; use gcdump for types.

`topN` cuts long generic names (`__Canon]].MoveNext()`), and `-v` does not restore them. For full
frame names convert the trace and list its frames:

```bash
dotnet dotnet-trace convert /tmp/alloc.nettrace --format speedscope -o /tmp/alloc
python3 -c "import json,sys; [print(f['name']) for f in json.load(open(sys.argv[1]))['shared']['frames']]" /tmp/alloc.speedscope.json | grep <Namespace>
```

The same file opens as a flame graph at speedscope.app.

## 4. Heap: what is alive, by type

```bash
dotnet dotnet-gcdump collect -p <pid> -o /tmp/heap.gcdump
dotnet dotnet-gcdump report /tmp/heap.gcdump | head -40
```

`collect` forces a full GC and pauses the target; use it on a dev process only. The report lists
retained objects by type and size. It shows what survives, not allocation churn. Two dumps a few
minutes apart under the same load show growth.

## Rules

- Capture for 10 seconds under load. Longer runs grow the file, not the signal.
- Keep `.nettrace`, `.etlx`, `.gcdump`, counters, and speedscope files out of the repository;
  write them under `/tmp` (a CPU trace of 10 s under load is about 11 MB).
- Report numbers with the tool output that backs them: counter values, `topN` rows, report lines.
- `dotnet-trace collect-linux` needs root and traces the whole machine. Use `collect`.
