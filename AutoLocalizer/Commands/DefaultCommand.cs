using AutoLocalizer.Models;
using Microsoft.Extensions.Options;
using RestSharp;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using static AutoLocalizer.Commands.DefaultCommand;

namespace AutoLocalizer.Commands;

public class DefaultCommand : AsyncCommand<DefaultCommandSettings>
{
    private readonly IOptions<WritableOptions> _options;

    public DefaultCommand(IOptions<WritableOptions> options)
    {
        _options = options;
    }
    public override async Task<int> ExecuteAsync( CommandContext context, DefaultCommandSettings settings )
    {
        if ( string.IsNullOrEmpty( _options.Value.Key ) || string.IsNullOrEmpty( _options.Value.Region ) )
        {
            AnsiConsole.WriteLine( "[red]Configuration is invalid[/]" );
            return -1;
        }
        if ( !File.Exists( settings.FilePath ) )
            throw new Exception( "File does not exist" );
        if (string.IsNullOrEmpty(settings.Language))
            throw new Exception("Language is not set");
        if ( !( Path.GetExtension( settings.FilePath )?.Equals( ".resx", StringComparison.InvariantCultureIgnoreCase ) ?? false ) )
            throw new Exception( "Extension is not supported" );
        var targetFolder = Path.GetDirectoryName(settings.FilePath);
        if (targetFolder == null)
            throw new Exception("Unable to grab destination folder");
        var targetName = $"{Path.GetFileNameWithoutExtension(settings.FilePath)}.{settings.Language}{Path.GetExtension(settings.FilePath)}";
        var targetPath = Path.Combine(targetFolder, targetName);
        Dictionary<string, string?> oldData = new();
        if (!settings.Override && File.Exists(targetPath))
        {
            oldData = await GetDictionaryAsync(targetPath);
        }
        using var s = File.OpenRead( settings.FilePath );
        var xdoc = await XDocument.LoadAsync( s, LoadOptions.None, CancellationToken.None );
        var elements = xdoc.XPathSelectElements( "//data" );
        string endpoint = "https://api.cognitive.microsofttranslator.com/";
        string route = $"/translate?api-version=3.0&from={settings.SourceLanguage}&to={settings.Language}";
        var client = new RestClient( endpoint + route );
        var count = elements.Count();
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                // Define tasks
                var task = ctx.AddTask($"Translating");
                task.MaxValue(count);
                foreach (var element in elements)
                {

                    string name = element.Attribute("name")?.Value ?? throw new Exception("Missing name attribute");
                    XElement valueElement = element.Element("value") ?? throw new Exception("Missing value element");
                    if (string.IsNullOrWhiteSpace(valueElement.Value))
                        continue;
                    if (oldData.TryGetValue(name, out var oldValue) && !string.IsNullOrWhiteSpace(oldValue))
                    {
                        valueElement.Value = oldValue;
                        task.Increment(1);
                        continue;
                    }
                    var request = new RestRequest()
                        .AddHeader("Ocp-Apim-Subscription-Key", _options.Value.Key)
                        .AddHeader("Ocp-Apim-Subscription-Region", _options.Value.Region)
                        .AddJsonBody(new object[] { new { Text = valueElement.Value } });
                    var response = await client.PostAsync<List<MicrosoftTranslatorRecord>>(request, CancellationToken.None);
                    valueElement?.SetValue(response?.FirstOrDefault()?.Translations?.FirstOrDefault()?.Text ?? string.Empty);
                    task.Increment(1);
                }
            });
        using var xmlWriter = XmlWriter.Create(targetPath, new XmlWriterSettings { Async = true, Indent = true });
        await xdoc.WriteToAsync( xmlWriter, CancellationToken.None );
        AnsiConsole.Write(new Markup($"[green]File saved in {targetPath}[/]"));
        return 0;
    }

    private async Task<Dictionary<string,string?>> GetDictionaryAsync( string path )
    {
        using var stream = File.OpenRead(path);
        var xdoc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
        var elements = xdoc.XPathSelectElements("//data");
        return elements
            .Select(x => new { Key = x.Attribute("name")?.Value ?? "", Value = x.Element("value")?.Value })
            .Where(x => !string.IsNullOrEmpty(x.Key))
            .ToDictionary(x => (string)x.Key, x => x.Value);
    }

    public class DefaultCommandSettings : CommandSettings
    {
        [CommandArgument(0, "<file>" )]
        [Description()]
        public string? FilePath { get; set; }
        
        [CommandArgument(1, "<lang>" )]
        [Description()]
        public string? Language { get; set; }

        [CommandOption("--override")]
        [DefaultValue(false)]
        public bool Override { get; set; }

        [CommandOption("--sourcelang")]
        [DefaultValue("en")]
        public string? SourceLanguage { get; set; }
    }
}
