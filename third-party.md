# Third-party software

Every dependency, its license and whether it ships in the game. Add a row before adding the dependency (user approval first, see `code-rules.md`).

| Name | Version | License | Ships in game | Use | Approved |
|---|---|---|---|---|---|
| Godot Engine (.NET) | 4.7.2 | MIT | yes (export templates) | engine | 2026-10-16 |
| Godot.NET.Sdk / GodotSharp | 4.7.2 | MIT | yes (GodotSharp) | C# build + bindings | 2026-10-16 |
| .NET runtime | 8 | MIT | yes (bundled by Godot export) | C# runtime | 2026-10-16 |
| xunit.v3 | 4.0.2 | Apache-2.0 | no (tests only) | unit tests for `Vlg.Core` | 2026-10-16 |

Godot's license requires its copyright notice in the shipped game (credits / about screen): https://godotengine.org/license
