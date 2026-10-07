using LogDeck.Core.Models;

namespace LogDeck.Core.Modules;

/// <summary>
/// Every tool LogDeck knows. Each writes under %ProgramData%\Managed&lt;Bucket&gt;\logs; the
/// patterns below follow each tool's own log writer:
///
///   BootstrapMate  logs\yyyy-MM-dd\HHmmss\bootstrap.log, events.jsonl
///   ReportMate     logs\reportmate-yyyyMMdd.log (daily), logs\yyyy-MM-dd-HHmmss\*.json (sent payloads)
///   Cimian         logs\yyyy-MM-dd\HHmm\install.log, events.jsonl; logs\installs, packages, selfupdate
///   StartSet       logs\yyyy-MM-dd\HHmm-runtype\startset.log, events.jsonl
///   Crypt escrow   logs\yyyy-MM-dd\crypt-escrow.log, events.jsonl (one folder per day)
///   csharpDialog   logs\csharpdialog.log, rotated to .1 to .5
///   ManageUsers    logs\yyyy-MM-dd\manageusers.log, events.jsonl; logs\manageusers.audit.log
///   Intune         %ProgramData%\Microsoft\IntuneManagementExtension\Logs\*.log (CMTrace)
///
/// A second numbered session in the same minute or second gets a _2 to _9 suffix, which the
/// depth search picks up like any other folder.
/// </summary>
public static class ToolCatalog
{
    private const string ProgramData = "%ProgramData%";
    private const string ProgramFiles = "%ProgramFiles%";

    public static IReadOnlyList<ToolModule> All { get; } =
    [
        new ToolModule(
            Id: "bootstrapmate",
            Name: "BootstrapMate",
            Category: "Bootstrap",
            Glyph: "",
            DetectionPaths:
            [
                $@"{ProgramFiles}\BootstrapMate\managedbootstrapinstall.exe",
                $@"{ProgramFiles}\BootstrapMate\Managed Bootstrap Install.exe",
            ],
            Sources:
            [
                new LogSource("sessions", "Sessions", $@"{ProgramData}\ManagedBootstrap\logs", ["*.log"], Depth: 2),
                new LogSource("events", "Events", $@"{ProgramData}\ManagedBootstrap\logs", ["events.jsonl"], Depth: 2),
                new LogSource("state", "Run state", $@"{ProgramData}\ManagedBootstrap", ["last-run.json", "status.json", "installed.json"]),
            ],
            SupportPaths:
            [
                new SupportPath("Install folder", $@"{ProgramFiles}\BootstrapMate"),
                new SupportPath("Data folder", $@"{ProgramData}\ManagedBootstrap"),
            ]),

        new ToolModule(
            Id: "reportmate",
            Name: "ReportMate",
            Category: "Reports",
            Glyph: "",
            DetectionPaths:
            [
                $@"{ProgramFiles}\ReportMate\managedreportsrunner.exe",
                $@"{ProgramFiles}\ReportMate\Managed Reports Runner.exe",
            ],
            Sources:
            [
                new LogSource("runs", "Run log", $@"{ProgramData}\ManagedReports\logs", ["*.log"]),
                new LogSource("transmissions", "Transmissions", $@"{ProgramData}\ManagedReports\logs", ["transmission_*.json", "unified_payload_*.json"], Depth: 1),
                new LogSource("cache", "Collected data", $@"{ProgramData}\ManagedReports\cache", ["*.json"], Depth: 1),
            ],
            SupportPaths:
            [
                new SupportPath("Install folder", $@"{ProgramFiles}\ReportMate"),
                new SupportPath("Data folder", $@"{ProgramData}\ManagedReports"),
            ]),

        new ToolModule(
            Id: "cimian",
            Name: "Cimian",
            Category: "Installs",
            Glyph: "",
            DetectionPaths:
            [
                $@"{ProgramFiles}\Cimian\managedsoftwareupdate.exe",
                $@"{ProgramFiles}\Cimian\Managed Software Center.exe",
            ],
            Sources:
            [
                new LogSource("sessions", "Sessions", $@"{ProgramData}\ManagedInstalls\logs", ["install.log"], Depth: 2),
                new LogSource("events", "Events", $@"{ProgramData}\ManagedInstalls\logs", ["events.jsonl"], Depth: 2),
                new LogSource("installers", "Installer logs", $@"{ProgramData}\ManagedInstalls\logs\installs", ["*.log"]),
                new LogSource("scripts", "Package scripts", $@"{ProgramData}\ManagedInstalls\logs\packages", ["*.log"], Depth: 1),
                new LogSource("selfupdate", "Self-update", $@"{ProgramData}\ManagedInstalls\logs\selfupdate", ["*.log"]),
                new LogSource("watcher", "Watcher", $@"{ProgramData}\ManagedInstalls\logs", ["cimiwatcher*.log"]),
                new LogSource("reports", "Reports", $@"{ProgramData}\ManagedInstalls\reports", ["*.log", "*.json"]),
            ],
            SupportPaths:
            [
                new SupportPath("Install folder", $@"{ProgramFiles}\Cimian"),
                new SupportPath("Data folder", $@"{ProgramData}\ManagedInstalls"),
            ]),

        new ToolModule(
            Id: "startset",
            Name: "StartSet",
            Category: "State",
            Glyph: "",
            DetectionPaths:
            [
                $@"{ProgramFiles}\StartSet\managedstatekeeper.exe",
                $@"{ProgramFiles}\StartSet\StartSetService.exe",
                $@"{ProgramFiles}\StartSet\Managed State Keeper.exe",
            ],
            Sources:
            [
                new LogSource("sessions", "Sessions", $@"{ProgramData}\ManagedState\logs", ["startset.log"], Depth: 2),
                new LogSource("events", "Events", $@"{ProgramData}\ManagedState\logs", ["events.jsonl"], Depth: 2),
                new LogSource("installers", "Installer logs", $@"{ProgramData}\ManagedState\logs\installs", ["*.log"]),
                new LogSource("reports", "Reports", $@"{ProgramData}\ManagedState\reports", ["*.log", "*.json"]),
            ],
            SupportPaths:
            [
                new SupportPath("Install folder", $@"{ProgramFiles}\StartSet"),
                new SupportPath("Data folder", $@"{ProgramData}\ManagedState"),
            ]),

        new ToolModule(
            Id: "crypt",
            Name: "Crypt",
            Category: "Encryption",
            Glyph: "",
            DetectionPaths:
            [
                $@"{ProgramFiles}\Crypt\checkin.exe",
                $@"{ProgramFiles}\Crypt\Managed Encryption Escrow.exe",
            ],
            Sources:
            [
                new LogSource("daily", "Daily logs", $@"{ProgramData}\ManagedEncryption\logs", ["*.log"], Depth: 1),
                new LogSource("events", "Events", $@"{ProgramData}\ManagedEncryption\logs", ["events.jsonl"], Depth: 1),
            ],
            SupportPaths:
            [
                new SupportPath("Install folder", $@"{ProgramFiles}\Crypt"),
                new SupportPath("Data folder", $@"{ProgramData}\ManagedEncryption"),
            ]),

        new ToolModule(
            Id: "csharpdialog",
            Name: "csharpDialog",
            Category: "Notifications",
            Glyph: "",
            DetectionPaths:
            [
                $@"{ProgramFiles}\csharpDialog\dialog.exe",
                $@"{ProgramFiles}\csharpDialog\Managed Notifications Dialog.exe",
            ],
            Sources:
            [
                new LogSource("log", "Log", $@"{ProgramData}\ManagedNotifications\logs", ["csharpdialog.log", "csharpdialog.log.*"]),
            ],
            SupportPaths:
            [
                new SupportPath("Install folder", $@"{ProgramFiles}\csharpDialog"),
                new SupportPath("Data folder", $@"{ProgramData}\ManagedNotifications"),
            ]),

        new ToolModule(
            Id: "manageusers",
            Name: "ManageUsers",
            Category: "Users",
            Glyph: "",
            DetectionPaths:
            [
                $@"{ProgramFiles}\ManageUsers\manageusers.exe",
                $@"{ProgramFiles}\ManageUsers\Managed Users Cleanup.exe",
            ],
            Sources:
            [
                new LogSource("daily", "Daily logs", $@"{ProgramData}\ManagedUsers\logs", ["manageusers.log", "manageusers.log.*"], Depth: 1),
                new LogSource("audit", "Audit log", $@"{ProgramData}\ManagedUsers\logs", ["manageusers.audit.log", "manageusers.audit.log.*"]),
                new LogSource("events", "Events", $@"{ProgramData}\ManagedUsers\logs", ["events.jsonl"], Depth: 1),
            ],
            SupportPaths:
            [
                new SupportPath("Install folder", $@"{ProgramFiles}\ManageUsers"),
                new SupportPath("Data folder", $@"{ProgramData}\ManagedUsers"),
            ]),

        new ToolModule(
            Id: "intune",
            Name: "Intune",
            Category: "Management Extension",
            Glyph: "",
            DetectionPaths:
            [
                @"%ProgramFiles(x86)%\Microsoft Intune Management Extension\Microsoft.Management.Services.IntuneWindowsAgent.exe",
                $@"{ProgramData}\Microsoft\IntuneManagementExtension",
            ],
            Sources:
            [
                new LogSource("ime", "Management Extension", $@"{ProgramData}\Microsoft\IntuneManagementExtension\Logs", ["*.log"]),
            ],
            SupportPaths:
            [
                new SupportPath("Install folder", @"%ProgramFiles(x86)%\Microsoft Intune Management Extension"),
                new SupportPath("Data folder", $@"{ProgramData}\Microsoft\IntuneManagementExtension"),
            ]),
    ];

    public static ToolModule? Find(string? id) =>
        id is null ? null : All.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
}
