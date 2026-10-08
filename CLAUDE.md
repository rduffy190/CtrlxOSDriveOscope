# CLAUDE.md

## Project
Configure, Trigger and record O-Scope reading from Ctrlx Drive via Data Layer endpoint on CtrlX Core 

- Language: C# / .NET 10 
- Platform: Linux Ubuntu 24.04
- Main libraries used: Easy Automation Library, Ctrlx Automation SDK

## Layout
```
/                      
├── src/               
├── docs/              # user how-to (scope_capture_how_to.tex → .pdf)
├── designDocs/
├── libDocs/           # local only, git-ignored (Bosch Rexroth material)
└── libExamples/       # local only, git-ignored (Bosch Rexroth material)
```
## Design — read before writing any code
- Design: `./designDocs/`— interface and design description of application
## Library reference — read before writing library code
- Docs: `./libDocs/eal/` — the source of truth for the Easy Automation Library API. `./libDocs/ctrlx/` — the source for the Ctrlx SDK
- Examples: `./libExamples/eal/` — Easy Automation Library Examples, `./libExamples/ctrlx/` — CtrlX SDK Examples
- Never guess a LIBRARY method, class, or parameter name. Search the docs/examples first; if it isn't there, say so and ask.
- See the `eal-library-reference` and `ctrlxSdk-library-reference` skills for the lookup procedure.

## Commands
```bash
dotnet restore                 # restore packages (hangs on this machine unless DOTNET_SYSTEM_NET_DISABLEIPV6=1)
dotnet build                   # build — run after every change (no test project yet)
dotnet run --project src/DriveOscope         # Data Layer provider service (ctrlX CORE)
dotnet run --project src/DriveOscopeClient   # CLI: create capture, trigger, save, delete
src/DriveOscope/build-fbs.sh                 # regenerate FlatBuffers code from designDocs/interface/*.fbs
src/DriveOscope/build-snap-amd64.sh          # build the amd64 snap into src/
dotnet format                  # format before finishing
```

## Code style
- Use camelCase 
- Class variables start with _ 
- local Temp variables start with t_
- JSON files (config input and data output) use snake_case keys, e.g. `drive_ip_address`. In C#, use `JsonNamingPolicy.SnakeCaseLower` rather than per-property attributes.
- Follow the style of nearby code when in doubt.

## Rules
- Don't edit anything inside `/libDocs` or `/libExamples` — read-only reference, and never commit or copy them into the repo (repo is MIT; they're Bosch Rexroth's).
- Don't add NuGet packages without asking.
