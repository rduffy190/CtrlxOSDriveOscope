# DriveOscope

A ctrlX OS service that sets up, triggers and records oscilloscope captures on a ctrlX DRIVE.
It runs on a ctrlX CORE and is controlled through the Data Layer. A client creates a capture with a
`ScopeConfig`, fires the trigger, reads back the recorded `ScopeCapture`, and deletes the capture
when it is finished. Every capture runs its own worker and connects to its drive through the
Easy Automation Library.

Built with C# / .NET 10, the Easy Automation Library and the ctrlX Automation SDK.

## Documentation

- **How to use the service:** [`docs/scope_capture_how_to.pdf`](docs/scope_capture_how_to.pdf)
  covers prerequisites, the Data Layer interface, the capture lifecycle, data formats, worked use cases,
  the command line client and result codes. The source is `docs/scope_capture_how_to.tex`.
- **Service design:** [`designDocs/description/scope_capture_service.md`](designDocs/description/scope_capture_service.md)
- **Interface schemas (FlatBuffers):** [`designDocs/interface/`](designDocs/interface/)

## Third-party dependencies

The Easy Automation Library and the ctrlX Automation SDK are Bosch Rexroth products. They are not
included in this repository. Get them from Bosch Rexroth:

- **Easy Automation Library:** the build expects `EAL.dll` and `SiP.dll` in
  `/usr/local/eal_sdk/02VRS/eal4DotNetStandard20/bin` (override with `-p:EalBinDir=...`).
- **ctrlX Automation SDK:** the `ctrlX-DataLayer` NuGet package is read from the local feed set in
  `src/nuget.config`.

The library docs and examples used during development (`libDocs/`, `libExamples/`) are kept
locally only and are git-ignored.

The snap packaging (`src/DriveOscope/snap/snapcraft.yaml`, `src/DriveOscope/build-snap-amd64.sh`)
is adapted from the ctrlX Automation SDK samples, © Bosch Rexroth AG, MIT License.

## Projects

| Path | Description |
|---|---|
| `src/DriveOscope` | Data Layer provider service (packaged as the `ctrlx-drive-oscope` snap) |
| `src/DriveOscopeClient` | Command line client: create a capture, trigger, save, delete |

## Quick start

```bash
export DOTNET_SYSTEM_NET_DISABLEIPV6=1        # dotnet restore can hang without this
dotnet build src/DriveOscopeApp.sln
dotnet run --project src/DriveOscope          # run the service
dotnet run --project src/DriveOscopeClient    # run the client
src/DriveOscope/build-snap-amd64.sh           # build the amd64 snap into src/
```

## License

MIT. See [`LICENSE`](LICENSE). This covers the code in this repository only; the Easy Automation Library
and the ctrlX Automation SDK are licensed separately by Bosch Rexroth.
