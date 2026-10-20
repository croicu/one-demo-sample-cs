# ARCHITECTURE.md

Modules, data flow, and contracts for `__project_name__`.

## Modules

<!-- One entry per file under src/Base/ (reusable scaffold: Logger/Settings/Errors/Interfaces,
     compiled to its own Base.dll) and src/__package_name__/ (the CLI itself, references
     Base.csproj): what it owns, what it depends on. -->

Base.dll is designed to be safe inside a service hosting multiple heterogeneous clients in one
process, each "renting" its own `ExecutionContext` with independent settings/logging. See
CLAUDE.md's Architecture convention 10 -- all ambient state in `src/Base` (the active sink list,
`ConsoleLog`'s/`DebugLog`'s/`FileLog`'s instance guards, `Settings.Current`, `Context.Current`,
`Correlation.Current`) is scoped with `AsyncLocal<T>` rather than plain `static` fields, so one
client's context can't see or disturb another's. `Logger` (`Logger.cs`) fans every call out to all
currently-registered sinks (`Sinks/DiagnosticsLog.cs`, `Sinks/ConsoleLog.cs`, `Sinks/DebugLog.cs`,
`Sinks/FileLog.cs`) rather than routing to a single active one. `Context` (`Context.cs`) wires
`Settings` into `Logger` and owns a host's bootstrap ceremony (`Context.Start`): load settings,
translate a malformed settings.json or an unhandled `AppError` into a logged message and exit code,
and invoke the host's `Run` delegate.

`Correlation` (`src/Base/Correlation.cs`): a short, lazily-generated hex id for the current logical
operation, exposed via `Correlation.Current` -- flows to nested awaits/Tasks like any other
`AsyncLocal` state here, so one operation's own id stays stable across it, distinct from an
unrelated concurrent operation's. Its one consumer today is `FileLog` (below), which tags each line
with it so a log file's interleaved lines (from concurrent operations, in a future multi-client
host) can still be attributed. Deliberately its own leaf type rather than living on `Context`:
`Context.Create` already constructs sinks from `Croicu.__project_namespace__.Base.Sinks`, so putting
the id there instead would make `Sinks` depend back on `Context` -- a cycle worth avoiding even
within one project (Architecture convention 9).

`FileLog.cs` -- writes unconditionally (same reasoning as `DebugLog`: a persisted file is for later
post-mortem debugging, not real-time viewing) to a new file inside a caller-supplied directory,
named after the UTC timestamp the sink was created. Each line is prefixed
`[timestamp][correlationId][LEVEL][category]` (see `Correlation` above) rather than `ConsoleLog`'s
bare `[LEVEL][category]`, since a log file can interleave lines from concurrent operations in a way
the console (read in real time, one operation at a time) doesn't. `Context.Create`/`Context.Start`
resolve `Settings.LogDir` with a host's own `--log <dir>` CLI override (the override taking
precedence when both are given, same direction as the debug override) into this sink, installed
alongside whatever else is active when non-null.

`src/Base/UserSettings.cs`: the per-user settings file, `%LOCALAPPDATA%\__project_name__\settings.json`
(`Environment.SpecialFolder.LocalApplicationData`, so also `~/.local/share` on Linux; `DefaultPath` is null,
not a relative fallback, on a platform with no such folder). `Settings.Section()` merges it key-by-key between
the working-directory `settings.json` and `settings.local.json`; `UserSettings.Set(section, key, value)` is the
write side (read-modify-write that keeps every other section/key, temp file then move, refuses the core section
and never overwrites a file that is not valid JSON). The trust rule that shapes all of it: the file is writable
by any process running as the user but may be read with more privilege, so it is honored only for non-core
sections -- never `logDir`/`debug`/`logLevel` -- and a malformed file is ignored with a warning, not thrown.
Cross-platform (Architecture convention 11); no static mutable state (convention 10).

## Data flow

<!-- How data enters, gets transformed, and leaves the system. -->

## Contracts

<!-- Interfaces.cs: public contracts -- persisted/shared data (plain classes/records, no behavior)
     plus behavioral interfaces meant for a consumer to implement/inject (e.g. ILoggingSink,
     already scaffolded there). Contracts.cs: behavioral interfaces that wire this project's own
     internals together -- never referenced by external consumers, unlike Interfaces.cs's. -->
