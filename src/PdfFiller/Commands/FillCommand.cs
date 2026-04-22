using PdfFiller.Models;
using PdfFiller.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PdfFiller.Commands;

public sealed class FillCommand : Command<FillSettings>
{
    private readonly IPdfFormService _pdfService;

    public FillCommand(IPdfFormService pdfService) => _pdfService = pdfService;

    protected override int Execute(CommandContext context, FillSettings settings, CancellationToken cancellationToken)
    {
        PrintBanner();

        var files = ResolveFiles(settings.Path);
        if (files.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No PDF files found.[/]");
            return 0;
        }

        PrintFileList(files);

        var totalFields = 0;
        var errors = 0;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(settings.DryRun
            ? "[dim]Dry-run mode — no files will be written.[/]"
            : "[dim]Filling forms...[/]");
        AnsiConsole.WriteLine();

        foreach (var file in files)
        {
            try
            {
                totalFields += ProcessFile(file, settings);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]  Error processing {Markup.Escape(Path.GetFileName(file))}: {Markup.Escape(ex.Message)}[/]");
                errors++;
            }
        }

        PrintSummary(files.Count - errors, errors, totalFields, settings.DryRun);
        return errors > 0 ? 1 : 0;
    }

    private int ProcessFile(string file, FillSettings settings)
    {
        var name = Path.GetFileName(file);
        AnsiConsole.MarkupLine($"[bold cyan]  {Markup.Escape(name)}[/]");

        var fields = _pdfService.GetFields(file);

        if (fields.Count == 0)
        {
            AnsiConsole.MarkupLine("    [dim]No fillable fields found.[/]");
            return 0;
        }

        var table = BuildFieldTable(fields);
        AnsiConsole.Write(table);

        if (!settings.DryRun)
        {
            var outputPath = BuildOutputPath(file);
            _pdfService.Fill(file, outputPath, fields);
            AnsiConsole.MarkupLine($"    [green]✓[/] Written → [link]{Markup.Escape(outputPath)}[/]");
        }

        AnsiConsole.WriteLine();
        return fields.Count;
    }

    private static Table BuildFieldTable(IReadOnlyList<PdfFieldInfo> fields)
    {
        var table = new Table()
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[grey]Field[/]"))
            .AddColumn(new TableColumn("[grey]Type[/]"))
            .AddColumn(new TableColumn("[grey]Generated Value[/]"));

        table.Expand = false;
        table.Border = TableBorder.Simple;

        foreach (var f in fields)
        {
            var typeMarkup = f.Type switch
            {
                PdfFieldType.Text => "[blue]Text[/]",
                PdfFieldType.Checkbox => "[yellow]Checkbox[/]",
                PdfFieldType.Radio => "[yellow]Radio[/]",
                PdfFieldType.Choice => "[magenta]Choice[/]",
                _ => "[grey]Unknown[/]"
            };

            table.AddRow(
                Markup.Escape(f.Name),
                typeMarkup,
                $"[green]{Markup.Escape(f.GeneratedValue)}[/]");
        }

        return table;
    }

    private static void PrintBanner()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new FigletText("PDF Filler")
            .LeftJustified()
            .Color(Color.CornflowerBlue));
        AnsiConsole.Write(new Rule("[dim]Fill PDF AcroForms with Swedish fake data[/]")
            .LeftJustified()
            .RuleStyle("grey"));
        AnsiConsole.WriteLine();
    }

    private static void PrintFileList(IReadOnlyList<string> files)
    {
        AnsiConsole.MarkupLine($"[bold]Found {files.Count} PDF file(s):[/]");
        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("#"))
            .AddColumn(new TableColumn("File"))
            .AddColumn(new TableColumn("Size"));

        for (var i = 0; i < files.Count; i++)
        {
            var info = new FileInfo(files[i]);
            table.AddRow(
                $"[dim]{i + 1}[/]",
                Markup.Escape(info.Name),
                $"[dim]{info.Length / 1024.0:F1} KB[/]");
        }

        AnsiConsole.Write(table);
    }

    private static void PrintSummary(int processed, int errors, int totalFields, bool dryRun)
    {
        AnsiConsole.Write(new Rule("[dim]Summary[/]").RuleStyle("grey"));

        if (errors == 0)
            AnsiConsole.MarkupLine($"[green]✓[/] {processed} file(s) processed, {totalFields} field(s) {(dryRun ? "discovered" : "filled")}.");
        else
            AnsiConsole.MarkupLine($"[yellow]![/] {processed} file(s) processed, [red]{errors} error(s)[/], {totalFields} field(s) {(dryRun ? "discovered" : "filled")}.");
    }

    private static List<string> ResolveFiles(string? path)
    {
        if (path is not null && File.Exists(path))
            return [path];

        var dir = path is not null && Directory.Exists(path)
            ? path
            : Directory.GetCurrentDirectory();

        return Directory.GetFiles(dir, "*.pdf", SearchOption.TopDirectoryOnly)
            .Where(f => !f.EndsWith(".filled.pdf", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToList();
    }

    private static string BuildOutputPath(string inputPath)
    {
        var dir = Path.GetDirectoryName(inputPath) ?? ".";
        var stem = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(dir, $"{stem}.filled.pdf");
    }
}
