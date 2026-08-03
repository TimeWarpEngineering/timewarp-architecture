# TimeWarp.402 — library layout

**PackageId:** `TimeWarp.402` (NuGet / product name).  
**C# namespace:** `TimeWarp.X402` — `TimeWarp.402` is not a legal C# identifier (leading digit after the dot).

Namespaces do not track folders (everything is `TimeWarp.X402`); folders exist for reader
navigation once challenge/settle/ledger types land (tasks 104-008+).

Scaffold only (104-007): packable project + generated `IAssemblyMarker`. No host wiring yet.
