// See https://aka.ms/new-console-template for more information
using AutoLocalizer;
using AutoLocalizer.Commands;
using AutoLocalizer.Extensions;
using AutoLocalizer.Models;
using AutoLocalizer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestSharp;
using Spectre.Console.Cli;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;


var hostBuilder = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddWritableJsonFile("appdynamicsettings.json");
    })
    .ConfigureServices(( context, services ) =>
    {
        services.Configure<WritableOptions>(context.Configuration.GetSection(nameof(WritableOptions)));
        services.AddSingleton<IOptionsSync<WritableOptions>>(new OptionsSync<WritableOptions>(
                context.Configuration.GetSection(nameof(WritableOptions)),
                ((IConfigurationRoot)context.Configuration).Providers.OfType<IWritableConfigurationProvider>()));
    });
using var registrar = new TypeRegistrar(hostBuilder);
var app = new CommandApp<DefaultCommand>(registrar);
app.Configure(config =>
{
    config.SetApplicationName("autolocalizer");
    config.SetExceptionHandler(ex =>
    {
        registrar.Host.Services.GetRequiredService<ILogger<Program>>().LogError(ex, message: "Critical exception");
    });
#if DEBUG
    config.ValidateExamples();
#endif
    config.AddBranch("set", c =>
    {
        c.AddCommand<SetConfigurationCommand>("configuration");
    });
});
return await app.RunAsync(args);