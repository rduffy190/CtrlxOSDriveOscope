---
name: csharp-linux
description: Conventions and workflow for writing, building, and testing C#/.NET code on Linux. Use whenever creating or editing .cs or .csproj files, running dotnet commands, or fixing build/test errors in this project.
---

# C# on Linux

## Environment
- .NET SDK: 10 (check with `dotnet --list-sdks`)
- Target framework: net10.0
- Distro: Ubuntu 24.04
- IDE/editor (for context only): VS-code

## Workflow for every code change
1. Make the change.
2. `dotnet build` — fix all errors and new warnings before continuing.
3. `dotnet test` — if tests exist for the area you touched, they must pass.
4. `dotnet format` before reporting the task as done.
5. Report what changed and the build/test result.

## Linux gotchas — always follow
- **Paths are case-sensitive.** `Config.json` ≠ `config.json`. Match file names exactly, including in `<Content Include>` and `using` of embedded resources.
- **Never hardcode `\` separators or drive letters.** Use `Path.Combine`, `Path.DirectorySeparatorChar`, and `AppContext.BaseDirectory`.
- **Line endings are LF.** Don't introduce CRLF.
- **No Windows-only APIs**: no Registry, WinForms, WPF, `System.Drawing.Common` (unsupported on Linux since .NET 6), COM, or `Environment.SpecialFolder` values that don't map on Linux. If something needs one, stop and say so.
- **Environment variables are case-sensitive.**
- **Shell commands** from code go through `/bin/sh` or `/bin/bash`, not `cmd.exe`.
- **File permissions matter**: newly created scripts need `chmod +x`.

## Code conventions
- Nullable reference types: enable
- Namespaces: block
- Async: prefer `async`/`await` end to end; no `.Result` or `.Wait()`.
- Test framework: xUnit.net
- JSON keys: snake_case for all config and output files — set `PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower` on the `JsonSerializerOptions`.

## Common errors
| Symptom | Likely cause | Fix |
|---|---|---|
| `FileNotFoundException` that works on Windows | path case mismatch | match exact case |
| `PlatformNotSupportedException` | Windows-only API | use a cross-platform alternative |
| `NETSDK1045` | SDK older than target framework | install matching SDK or lower TFM |
