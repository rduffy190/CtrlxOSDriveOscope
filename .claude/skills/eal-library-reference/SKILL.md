---
name: Easy Automation Library Lookup
description: How to look up the Easy Automation Library API using the local docs and example code folders. Use whenever writing, changing, or debugging code that calls Easy Automation Library, or when unsure how a Easy Automation Library class or method works.
---

# Using Easy Automation Library

Local reference (read-only — never edit these):
- Docs: `./libDocs/eal`
- Examples: `./libExamples/eal`

## Lookup procedure
1. **Search the docs first** for the class/method/concept:
   `grep -ril "<term>" ./libDocs/eal/`
2. **Find a working example** that does something similar:
   `grep -rl "<ClassName>" ./libExamples/eal --include=*.cs`
3. **Read the closest example in full** before writing code, and copy its patterns (setup, disposal, error handling, async usage).
4. **If the docs and an example disagree**, trust the documentation and mention the conflict.
5. **If nothing covers it**, say so plainly. Do not invent API names or signatures.
6. When you finish, list which doc/example files you relied on.

## Quick map — fill in as you learn the library
| Task | Doc file | Example file |
|---|---|---|
| Getting started / setup (project refs: EAL.dll, SiP.dll) | `./libDocs/eal/fundamentals01.html` | none (all samples' `.csproj` show references, e.g. `./libExamples/eal/Sample - SaveParameters/SaveParameters/SaveParameters.csproj`) |
| Architecture overview | `./libDocs/eal/fundamentals02.html` | none |
| Sync vs async execution (`new EALConnection(true)`, `WaitUntilQueueComplete`) | `./libDocs/eal/fundamentals03.html` | `./libExamples/eal/Sample - SaveParameters/SaveParameters/Program.cs` |
| Connecting to a drive (`EALConnection`, `Connect`, `Disconnect`) | `./libDocs/eal/fundamentals05_02.html`, `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_i_e_a_l_connection.html` | `./libExamples/eal/Sample - SetFirmwareSample/SetFirmwareSample/Program.cs` |
| Ping a drive before connecting | `./libDocs/eal/fundamentals05_01.html`, `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_i_ping_e_a_l_connection.html` | none |
| Browse / discover devices on network | `./libDocs/eal/fundamentals05_05_07.html`, `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_browse_1_1_i_browse_devices.html` | none |
| Read/write a single parameter (`ReadData`/`WriteData`) | `./libDocs/eal/fundamentals05_03.html`, `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_parameter_1_1_i_parameter.html` | none |
| Save parameters to file | `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_parameter_1_1_i_parameter.html` | `./libExamples/eal/Sample - SaveParameters/SaveParameters/Program.cs` |
| Load parameters from file | `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_parameter_1_1_i_parameter.html` | `./libExamples/eal/Sample - LoadParameters/LoadParameters/Program.cs` |
| Axis condition / status (`GetCondition`/`SetCondition`) | `./libDocs/eal/fundamentals05_04.html`, `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_motion_1_1_i_axis.html` | `./libExamples/eal/Sample - SetFirmwareSample/SetFirmwareSample/Program.cs` |
| Motion: power, MoveAbsolute, MoveVelocity | `./libDocs/eal/fundamentals05_05_03.html`, `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_motion_1_1_i_movement.html` | none |
| Read actual velocity | `./libDocs/eal/fundamentals05_05_02.html` | none |
| Motion limit values | `./libDocs/eal/fundamentals05_05_01.html`, `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_axis_1_1_motion_limit_value_1_1_i_motion_limit_values.html` | none |
| **Oscilloscope: configure channels/trigger, start, read data** | `./libDocs/eal/fundamentals05_05_06.html`, `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_oscilloscope_1_1_i_oscilloscope.html`, `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_oscilloscope_1_1_i_trigger.html`, `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_oscilloscope_1_1_i_channel.html` | none |
| Trending data | `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_oscilloscope_1_1_i_trending.html` | none |
| EFC drive parameters | `./libDocs/eal/fundamentals05_05_04.html` | none |
| Multi-axis access (`Axes[n]`) | `./libDocs/eal/fundamentals05_05_08.html` | none |
| Firmware: read file from drive (`GetFirmwareFile`) | `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_system_1_1_i_firmware_management.html` | `./libExamples/eal/Sample - GetFirmwareFileSample/GetFirmwareFileSample/Program.cs` |
| Firmware: write to drive (`SetFirmware`; doc calls this "download") + reboot | `./libDocs/eal/fundamentals05_05_05.html`, `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_system_1_1_i_firmware_management.html`, `./libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_system_1_1_i_system.html` | `./libExamples/eal/Sample - SetFirmwareSample/SetFirmwareSample/Program.cs` |
| Error handling (exceptions + error events) | `./libDocs/eal/class_e_a_l_1_1_interfaces_1_1_parameter_1_1_parameter_read_write_error_event_args.html`, `./libDocs/eal/class_e_a_l_1_1_interfaces_1_1_system_1_1_transfer_error_event_args.html` | `./libExamples/eal/Sample - SetFirmwareSample/SetFirmwareSample/Program.cs` (`FwMngmtEALException`), `./libExamples/eal/Sample - LoadParameters/LoadParameters/Program.cs` (`ErrorOnUpdate`) |
| Full API index (all interfaces/classes) | `./libDocs/eal/annotated.html`, `./libDocs/eal/pages.html` | — |

"none" = no sample project in `./libExamples/eal` covers it; use the code snippet inside the doc page instead.
