---
name: Ctrlx SDK Lookup
description: How to look up CtrlX SDK libraries and documentation. 
---

# Using Ctrlx SDK

Local reference (read-only — never edit these):
- Docs: `./libDocs/ctrlx`
- Examples: `./libExamples/ctrlx`

## Lookup procedure
1. **Search the docs first** for the class/method/concept:
   `grep -ril "<term>" ./libDocs/ctrlx/`
2. **Find a working example** that does something similar:
   `grep -rl "<ClassName>" ./libExamples/ctrlx --include=*.cs`
3. **Read the closest example in full** before writing code, and copy its patterns (setup, disposal, error handling, async usage).
4. **If the docs and an example disagree**, trust the documentation and mention the conflict.
5. **If nothing covers it**, say so plainly. Do not invent API names or signatures.
6. When you finish, list which doc/example files you relied on.

## Quick map — fill in as you learn the library
| Task | Doc file | Example file |
|---|---|---|
| Getting started / NuGet package (`ctrlX-DataLayer` 6.*), API version | `./libDocs/ctrlx/html/index.html`, `./libDocs/ctrlx/html/namespaceDatalayer.html` | `./libExamples/ctrlx/datalayer.client.simple/*.csproj`, `./libExamples/ctrlx/README.md` |
| Start/stop the Data Layer system (`new DatalayerSystem()`, `Start(startBroker: false)`, `Stop()`) | `./libDocs/ctrlx/html/classDatalayer_1_1DatalayerSystem.html`, `./libDocs/ctrlx/html/interfaceDatalayer_1_1IDatalayerSystem.html` | `./libExamples/ctrlx/datalayer.client.simple/Program.cs` |
| Build connection string to ctrlX CORE (`new Remote(ip:, sslPort:).ToString()`, user/password) | `./libDocs/ctrlx/html/classDatalayer_1_1Remote.html` | `./libExamples/ctrlx/datalayer.client.simple/Program.cs` |
| Create a client / check connection (`system.Factory.CreateClient(remote)`, `IsConnected`, `ConnectionStatus`, `Ping`, `SetTimeout`) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1IFactory.html`, `./libDocs/ctrlx/html/interfaceDatalayer_1_1IClient.html` | `./libExamples/ctrlx/datalayer.client.simple/Program.cs` |
| Read a single node (`client.Read(address)` → `(result, value)`; `ReadAsync`) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1IClient.html` | `./libExamples/ctrlx/datalayer.client.simple/Program.cs`, `./libExamples/ctrlx/hello.web.asp/services/DataLayerService.cs` |
| Read a node as JSON (`ReadJson(address, indent)`) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1IClient.html` | `./libExamples/ctrlx/datalayer.client.browse/ClientExtensions.cs` |
| Write a node (`Write`, `WriteJson`, `WriteAsync`, `WriteJsonAsync`) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1IClient.html` | none |
| Create / remove nodes, e.g. call-style or list entries (`Create`, `Remove`, `BulkCreate`) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1IClient.html` | none |
| Read several nodes at once (`BulkRead(addresses)` → `IBulkItem.Address/Value/Timestamp/Result`) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1IClient.html`, `./libDocs/ctrlx/html/interfaceDatalayer_1_1IBulkItem.html`, `./libDocs/ctrlx/html/interfaceDatalayer_1_1IBulk.html` | `./libExamples/ctrlx/datalayer.client.bulkread/Program.cs` |
| Browse the node tree / discover addresses (`Browse(address)` → `ToStringArray()`) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1IClient.html` | `./libExamples/ctrlx/datalayer.client.browse/ClientExtensions.cs` |
| Read node metadata (`ReadMetadata`, `BulkReadMetadata`) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1IClient.html` | none |
| Subscribe to value changes (`CreateSubscription(SubscriptionPropertiesT, userData)`, `Subscribe`, `DataChanged` event) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1ISubscription.html`, `./libDocs/ctrlx/html/interfaceDatalayer_1_1IDataChangedEventArgs.html`, `./libDocs/ctrlx/html/interfaceDatalayer_1_1INotifyItem.html` | `./libExamples/ctrlx/datalayer.client.simple/Program.cs` |
| Subscription properties (`SubscriptionPropertiesBuilder`: publish/keepalive/error/sampling interval, queueing, data-change filter) | `./libDocs/ctrlx/html/classDatalayer_1_1SubscriptionPropertiesBuilder.html` | `./libExamples/ctrlx/datalayer.client.simple/Program.cs` (commented alternative) |
| Async calls inside subscription callbacks (sync calls return `WOULD_BLOCK` there; use `*Async`) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1IClientAsyncResult.html`, `./libDocs/ctrlx/html/interfaceDatalayer_1_1ISubscriptionAsyncResult.html` | `./libExamples/ctrlx/datalayer.client.simple/Program.cs` |
| Variant conversion (`ToFloat`, `ToDouble`, `ToInt32`, `ToString`, `ToFloatArray`, `ToFlatbuffers`, `IsNull`, …) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1IVariant.html`, `./libDocs/ctrlx/html/classDatalayer_1_1Variant.html` | `./libExamples/ctrlx/datalayer.client.simple/Program.cs` |
| Error handling (`DLR_RESULT`, `result.IsGood()` / `IsBad()`) | `./libDocs/ctrlx/html/classDatalayer_1_1ResultExtensions.html`, `./libDocs/ctrlx/html/namespacemembers_enum.html` | `./libExamples/ctrlx/datalayer.client.browse/ClientExtensions.cs` |
| JSON ↔ Variant conversion (`system.Converter`) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1IConverter.html` | none |
| Provide nodes (`CreateProvider`, `RegisterNode`, `IProviderNodeHandler` OnRead/OnWrite/OnBrowse/…) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1IProvider.html`, `./libDocs/ctrlx/html/interfaceDatalayer_1_1IProviderNodeHandler.html`, `./libDocs/ctrlx/html/interfaceDatalayer_1_1IProviderNodeResult.html` | `./libExamples/ctrlx/datalayer.provider.simple/Program.cs`, `./libExamples/ctrlx/datalayer.provider.simple/ReadWriteNodeHandler.cs` |
| Provide nodes of all data types / flatbuffers types (`RegisterType`, bfbs) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1IProvider.html` | `./libExamples/ctrlx/datalayer.provider.alldata/Program.cs`, `./libExamples/ctrlx/datalayer.provider.alldata/Node.cs` |
| Provider-side subscriptions (`ISubscriptionProviderNodeHandler`, `OnSubscribe`, `IProviderSubscription.Publish`) | `./libDocs/ctrlx/html/interfaceDatalayer_1_1ISubscriptionProviderNodeHandler.html`, `./libDocs/ctrlx/html/interfaceDatalayer_1_1IProviderSubscription.html` | `./libExamples/ctrlx/datalayer.provider.subscription/SubscriptionNodeHandler.cs` |
| Virtual (dynamically created) provider nodes | `./libDocs/ctrlx/html/interfaceDatalayer_1_1IProvider.html` | `./libExamples/ctrlx/datalayer.provider.virtual/VirtualNodeHandler.cs`, `./libExamples/ctrlx/datalayer.provider.virtual/ProviderExtensions.cs` |
| Node metadata for providers (`MetadataBuilder`, `AllowedOperationFlags`, `ReferenceType`) | `./libDocs/ctrlx/html/classDatalayer_1_1MetadataBuilder.html`, `./libDocs/ctrlx/html/classDatalayer_1_1ReferenceType.html` | `./libExamples/ctrlx/datalayer.provider.simple/Node.cs` |
| Graceful shutdown on SIGTERM (`AppDomain.ProcessExit` + `TaskCompletionSource`) | none (not SDK API) | `./libExamples/ctrlx/datalayer.client.simple/Program.cs` |
| App data / persisted config in snap (`$SNAP_COMMON/solutions/activeConfiguration`) | none | `./libExamples/ctrlx/appdata/MyApplication.cs` |
| Logging to ctrlX logbook (journald / Microsoft logger) | none | `./libExamples/ctrlx/diagnostics.logbook/Program.cs`, `./libExamples/ctrlx/diagnostics.logbook/KernelLogger.cs` |
| Packaging as a snap | none | `./libExamples/ctrlx/datalayer.client.simple/snap/snapcraft.yaml` |
| Hosting Data Layer client in ASP.NET (DI service) | none | `./libExamples/ctrlx/hello.web.asp/services/DataLayerService.cs`, `./libExamples/ctrlx/hello.web.asp/Startup.cs` |
| Full API index (all classes/interfaces/members) | `./libDocs/ctrlx/html/annotated.html`, `./libDocs/ctrlx/html/classes.html`, `./libDocs/ctrlx/html/functions.html` | — |

"none" = no sample project in `./libExamples/ctrlx` covers it; use the doc page signature (check the `-members.html` page for overloads).
Note: the ctrlX docs are the generic Data Layer API only — drive/oscilloscope node addresses are NOT documented here. Find them by browsing the live Data Layer (`Browse` / `ReadJson`) or in `./designDocs/`.
