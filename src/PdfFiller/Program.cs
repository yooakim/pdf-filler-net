using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PdfFiller.Commands;
using PdfFiller.Infrastructure;
using PdfFiller.Services;
using Serilog;
using Spectre.Console.Cli;

// Parse --verbose early so we can configure the logger before DI
var verbose = args.Contains("--verbose") || args.Contains("-v");

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

var logConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(config);

if (verbose)
    logConfig = logConfig.WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}");

Log.Logger = logConfig.CreateLogger();

try
{
    var services = new ServiceCollection()
        .AddSingleton<IConfiguration>(config)
        .AddSingleton<IFakeDataService, FakeDataService>()
        .AddSingleton<IPdfFormService, PdfFormService>();

    var registrar = new TypeRegistrar(services);
    var app = new CommandApp<FillCommand>(registrar);

    app.Configure(c =>
    {
        c.SetApplicationName("pdf-filler");
        c.SetApplicationVersion("1.0.0");
        c.SetExceptionHandler((ex, _) =>
        {
            Log.Error(ex, "Unhandled exception");
            Spectre.Console.AnsiConsole.MarkupLine($"[red]Error:[/] {Spectre.Console.Markup.Escape(ex.Message)}");
        });
    });

    return app.Run(args);
}
finally
{
    await Log.CloseAndFlushAsync();
}
