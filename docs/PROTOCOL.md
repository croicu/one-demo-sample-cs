# PROTOCOL.md

CLI signature and file format schemas for `__project_name__`.

## CLI

<!-- Command name, arguments, flags, exit codes. -->

`src/__package_name__` accepts `--debug` (override `settings.json`'s `debug` flag), `--log <dir>`
(write a timestamped log file into `<dir>`, creating it if missing, in addition to whatever
console/debug sinks are already active; overrides `settings.json`'s `logDir` if both are given --
see `docs/ARCHITECTURE.md`'s `FileLog`/`Correlation` entries for the sink itself and CLAUDE.md's
Logging section for the file's line format), and `-h`/`--help` (print usage and exit 0). An
unrecognized argument exits 2.

## File formats

<!-- Schemas for any files this project reads or writes. -->

### settings.json / settings.local.json -- `"settings"` object

- `logDir` (string, directory, default unset -- no file logging) -- installs a `FileLog` sink
  writing a timestamped log file into this directory; overridable per-invocation by `--log <dir>`
  (see the CLI section above). See `docs/ARCHITECTURE.md`'s `FileLog` entry.

### settings.json / settings.local.json -- location and build wiring

The checked-in module-tier `settings.json` (and an optional, gitignored `settings.local.json`)
live at the **repo root**, not inside `src/__package_name__/` -- keeps the location stable even if
this instance later grows a second `Exe` project sharing `src/Directory.Build.props`'
`BaseOutputPath` (the way `croicu/desk-tools`' `Service`/`Hello` do), rather than one project's own
copy of the file silently clobbering the other's at the shared output path.
`src/Directory.Build.targets` copies both into `__package_name__`'s own `$(OutDir)` after `Build`
(imperative `Copy` tasks, not a declarative `CopyToOutputDirectory` item -- the latter propagates
through any `ProjectReference`, which would leak these files into `__package_name__.Tests` too) and
`settings.json` alone into `$(PublishDir)` after `Publish` -- `settings.local.json` is deliberately
never published, since `installer/Setup.wixproj` globs its entire publish output into the MSI and a
developer's personal override must never ship there.

### settings.json tiers and the per-user file

`Settings.Load()`/`Settings.Section()` (`src/Base/Settings.cs`) read these tiers, each overriding the
previous key-by-key (`Settings.Load()`, the core `"settings"` object, reads tiers 1, 2 and 4; the
per-user tier 3 is read by `Settings.Section()` only -- see below):

1. `settings.json` next to the running module (`AppContext.BaseDirectory`). The base tier.
2. `settings.json` in the current working directory -- a per-deployment/per-invocation override.
3. The per-user file, `%LOCALAPPDATA%\__project_name__\settings.json` (the platform's local
   application-data folder elsewhere). Written by the program itself -- `UserSettings.Set(section,
   key, value)` -- for values only the user's own machine can know and that the program discovers
   at run time (a path the user picked, a preference they set).
4. `settings.local.json` in the current working directory -- a personal, gitignored override of
   everything above, including the per-user file.

**The per-user file is honored only for non-core sections.** Any process running as the user can
write it, while the program may read it with more privilege than that, so it may supply sections
that sit beside the `"settings"` object (read with `Settings.Section(name)`), never the core
`"settings"` object itself (`logDir`, `debug`, `logLevel`, ...): `Settings.Load()` does not read it,
`Section("settings")` skips it, and `UserSettings.Set` refuses that section. A per-user file that is
not valid JSON, or a section in it that is not an object, is logged and ignored rather than thrown;
`UserSettings.Set` in turn never overwrites an existing file that is not valid JSON. Writes go to a
temporary file that is moved into place (a reader never sees a half-written file); concurrent
writers are last-one-wins. `Settings.Section()` merges the named section key-by-key across the
tiers.
