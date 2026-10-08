# CLAUDE.md

<!-- Claude Code reads this file at the start of every session in this folder.
     Keep it short: facts Claude can't guess. Replace every [FILL IN]. -->

## Project
Configure, Trigger and record O-Scope reading from Ctrlx Drive via Data Layer endpoint on CtrlX Core 

- Language: C# / .NET 10 
- Platform: Linux Ubuntu 24.04
- Main libraries used: Easy Automation Library, Ctrlx Automation SDK

## Layout
```
/                      
├── src/               
├── libDocs/        
├── libExamples/
└── designDocs/

```

## Library reference — read before writing library code
- Design: `./designDocs/`— interface and design description of application
- Docs: `./libDocs/eal/` — the source of truth for the Easy Automation Library API. `./libDocs/ctrlx/` — the source for the Ctrlx SDK
- Examples: `./libExamples/eal/` — Easy Automation Library Examples, `./libExamples/ctrlx/` — CtrlX SK Examples
- Never guess a LIBRARY method, class, or parameter name. Search the docs/examples first; if it isn't there, say so and ask.
- See the `library-reference` skill for the lookup procedure.

## Commands
```bash
dotnet restore                 # restore packages (hangs on this machine unless DOTNET_SYSTEM_NET_DISABLEIPV6=1)
dotnet build                   # build — run after every change
dotnet test                    # run tests
dotnet run --project src/DriveOscope         # Data Layer provider service (ctrlX CORE)
dotnet run --project src/DriveOscopeClient   # CLI: create capture, trigger, save, delete
src/DriveOscope/build-fbs.sh                 # regenerate FlatBuffers code from designDocs/interface/*.fbs
dotnet format                  # format before finishing
```

## Code style
- Use camelCase 
- Class variables start with _ 
- local Temp variables start with t_
- JSON files (config input and data output) use snake_case keys, e.g. `drive_ip_address`. In C#, use `JsonNamingPolicy.SnakeCaseLower` rather than per-property attributes.
- Follow the style of nearby code when in doubt.

## Rules
- Don't edit anything inside `/libDocs` — they're read-only reference.
- Don't edit anything inside `/libExamples` — they're read-only reference.
- Don't add NuGet packages without asking.
