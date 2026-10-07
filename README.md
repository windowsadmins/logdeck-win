# LogDeck for Windows

One place to view the local logs of Windows management tools, for an admin troubleshooting on the endpoint. The Windows sibling of [LogDeck for macOS](https://github.com/rodchristiansen/logdeck-mac).

![Windows 10 1809+](https://img.shields.io/badge/Windows-10%201809%2B-blue)
![.NET 10](https://img.shields.io/badge/.NET-10-purple)
![License: MIT](https://img.shields.io/badge/License-MIT-green)

## What it does

- **Detects** each tool from its install folder and exe, and lists installed tools first in the sidebar. Tools that are gone but left logs behind are still listed; the filter button shows the rest.
- **Sessions newest first** for each of a tool's log sources: one entry per run for tools that write a session folder per run, one per file for the others.
- **Live tail**: new lines appear as the tool writes them, and a new run shows up in the list on its own.
- **Filter** by text and by level (all, warnings and errors, errors only).
- **Severity colours** for the formats these tools write: `[timestamp] LEVEL message`, Serilog's `[WRN]` tags, `events.jsonl` levels, and the CMTrace format of the Intune Management Extension, which is also shown as plain `date time component message`.
- **Reveal in File Explorer, Copy path, Open in editor** for the open log, and Reveal / Copy path for every folder on the Tools tab.
- **Read-only.** LogDeck writes no settings and runs without elevation. A log only administrators may read is marked "Run as administrator to read these", with a button that restarts LogDeck elevated.

## Supported tools

| Tool | Detected by | Logs |
|------|-------------|------|
| **BootstrapMate** | `C:\Program Files\BootstrapMate\managedbootstrapinstall.exe` | `C:\ProgramData\ManagedBootstrap\logs\<date>\<HHmmss>\bootstrap.log`, `events.jsonl` |
| **ReportMate** | `C:\Program Files\ReportMate\managedreportsrunner.exe` | `C:\ProgramData\ManagedReports\logs\reportmate-<yyyyMMdd>.log`, transmissions, `cache\<run>\*.json` |
| **Cimian** | `C:\Program Files\Cimian\managedsoftwareupdate.exe` | `C:\ProgramData\ManagedInstalls\logs\<date>\<HHmm>\install.log`, `events.jsonl`, `installs\`, `packages\`, `selfupdate\`, `cimiwatcher*.log`, `reports\` |
| **StartSet** | `C:\Program Files\StartSet\managedstatekeeper.exe` | `C:\ProgramData\ManagedState\logs\<date>\<HHmm-runtype>\startset.log`, `events.jsonl`, `installs\`, `reports\` |
| **Crypt** | `C:\Program Files\Crypt\checkin.exe` | `C:\ProgramData\ManagedEncryption\logs\<date>\crypt-escrow.log`, `events.jsonl` |
| **csharpDialog** | `C:\Program Files\csharpDialog\dialog.exe` | `C:\ProgramData\ManagedNotifications\logs\csharpdialog.log` and `.1`–`.5` |
| **ManageUsers** | `C:\Program Files\ManageUsers\manageusers.exe` | `C:\ProgramData\ManagedUsers\logs\<date>\manageusers.log`, `events.jsonl`, `manageusers.audit.log` |
| **Intune** | `C:\Program Files (x86)\Microsoft Intune Management Extension\` | `C:\ProgramData\Microsoft\IntuneManagementExtension\Logs\*.log` |

A tool module is data: detection paths, log sources (a folder, file masks and how many session-folder levels to search) and support folders. They live in [`src/Core/Modules/ToolCatalog.cs`](src/Core/Modules/ToolCatalog.cs); adding a tool is one more entry there.

## Install

Each [release](https://github.com/windowsadmins/logdeck-win/releases) carries:

- `logdeck-x64.zip`, `logdeck-arm64.zip`: the install folder, `C:\Program Files\LogDeck`, as the payload [cimipkg](https://github.com/windowsadmins/cimian-pkg) packages together with `build-info.yaml` and `scripts/`.
- `LogDeck-<arch>-<version>.msi` and `.nupkg`, built by cimipkg from the same files. The install adds an all-users Start menu shortcut; uninstall removes it.

Releases are unsigned; the release notes show how to sign them.

## Command line

```
LogDeck.exe [--tab logs|tools] [--tool <id>] [--source <id>] [--theme light|dark]
```

Tool ids: `bootstrapmate`, `reportmate`, `cimian`, `startset`, `crypt`, `csharpdialog`, `manageusers`, `intune`.

## Building

Windows with the .NET 10 SDK and the Windows SDK (for `makepri.exe`):

```powershell
dotnet test tests/LogDeck.Tests/LogDeck.Tests.csproj
.\build.ps1 -Arch x64 -Zip
```

`build.ps1` publishes the self-contained WinUI 3 app to `dist\<arch>\` and, with `-Zip`, writes `dist\logdeck-<arch>.zip`. The Core library and its tests also build on macOS and Linux.

Releases are cut by pushing a `vYYYY.MM.DD.HHMM` tag; the workflow stamps that version into every file.

## Architecture

- **`src/Core`**: tool modules, `ToolDetector`, `LogScanner` (sessions newest first; a folder this account may not read is reported, never thrown), `LogTail` (shared-read tail that survives truncation and rotation, UTF-8 and UTF-16), `LineClassifier` and `CmTrace`.
- **`src/App`**: the WinUI 3 app, `LogDeck.exe`: a Logs tab (tools, sources and sessions, viewer) and a Tools tab (what is installed and where everything is).
- **`tests`**: xUnit tests for Core.

## License

MIT
