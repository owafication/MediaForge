# Structure, naming and tools

**Purpose:** Canonical repository structure, naming and tool-use rules for the managed desktop expansion.  
**Read when:** Adding folders, projects, classes, dependencies, scripts or build tools.  
**Owner:** Technical maintainer.  
**Authority:** Structure/naming/tool owner.  
**Update trigger:** New component category, project, dependency, naming rule or build tool.  
**Linked IDs:** `PH-07`–`PH-19`, `DEC-015`, `REQ-025`, `REQ-054`.

## Existing structure

Keep the current solution/project and root paths. Do not create a parallel app. Preserve `Models/`, `Services/`, root WPF views, `scripts/`, `installer/` and `project_docs/` until an affected owner is migrated.

## Incremental target

```text
/MediaForge
  App.xaml(.cs)
  MainWindow.xaml(.cs)
  MediaEditorWindow.xaml(.cs)
  Models/
    Persistence/       project, preset, queue DTOs and migrations
    Processing/        immutable plan and stream action models
  Services/
    Persistence/       project, preset, history services
    Media/             capability, compatibility, preview, verification
    Runtime/           process runner, workspace, queue coordinator
  ViewModels/          added only as state/commands are extracted
  Views/               optional after a view is moved; avoid duplicate XAML
  Resources/           themes/localised strings when PH-18 begins
  tests/MediaForge.Tests/
  scripts/
  installer/
  project_docs/
```

Create a folder only when its first canonical owner is implemented. Move existing files only through a reviewed migration; do not leave active duplicate implementations.

## Naming

- `*ViewModel`: presentation state and commands
- `*Service`: reusable stateful/stateless capability boundary
- `*Dto`: persistence-only representation
- `*Snapshot`: immutable captured run/session state
- `*Plan`: immutable intended processing
- `*Request` / `*Result`: operation boundary
- `*Migration`: one schema transition
- `*Report`: user/support evidence model
- Async methods end in `Async`; cancellation token is explicit for I/O/process operations.
- Persisted enum/string identifiers are stable and locale-independent.

## Tools

- Windows 10/11 development environment
- Visual Studio with .NET desktop workload or supported .NET SDK
- Current source baseline: .NET 10 WPF (`net10.0-windows`); native validation pending
- Local FFmpeg and FFprobe with recorded version/configuration/hash
- PowerShell build scripts
- Inno Setup only for installer phases
- Git local repository; no remote required

Do not add a package, native binding, framework, database, updater library or test-mocking framework without a requirement, compatibility check and decision. Prefer platform/BCL features and explicit composition.

## Scaffolding rule

PH-08 seams, PH-09 persistence/recovery and PH-10 preset/queue workflow contracts are implemented in source. Preserve their ownership and require a clean native build/test plus project/preset/queue fixture run before dependent PH-11 work, unless an explicit risk exception is recorded.
