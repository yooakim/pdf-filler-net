using Spectre.Console.Cli;
using System.ComponentModel;

namespace PdfFiller.Commands;

public sealed class FillSettings : CommandSettings
{
    [CommandArgument(0, "[PATH]")]
    [Description("PDF file or directory to process. Defaults to current directory.")]
    public string? Path { get; init; }

    [CommandOption("--locale")]
    [Description("Bogus locale for fake data generation (default: sv).")]
    [DefaultValue("sv")]
    public string Locale { get; init; } = "sv";

    [CommandOption("--dry-run")]
    [Description("Discover and display fields without writing output files.")]
    [DefaultValue(false)]
    public bool DryRun { get; init; }

    [CommandOption("-v|--verbose")]
    [Description("Enable verbose console logging.")]
    [DefaultValue(false)]
    public bool Verbose { get; init; }
}
