﻿using AutoLocalizer.Models;
using Microsoft.Extensions.Options;
using RestSharp;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        if ( !File.Exists( settings.SourcePath ) )
            throw new Exception( "File does not exist" );
        if ( string.IsNullOrEmpty( settings.SourceLanguage ) )
            throw new Exception( "Source language is not set" );
        if ( string.IsNullOrEmpty( settings.TargetLanguage ) )
            throw new Exception( "Target language is not set" );
        var sourceExtension = Path.GetExtension( settings.SourcePath )?.TrimStart( '.' );
        if ( !( sourceExtension?.Equals( "resx", StringComparison.InvariantCultureIgnoreCase ) ?? false ) )
            throw new Exception( "Extension is not supported" );
        if ( settings.TargetExtension == "resx" && settings.SourceLanguage == settings.TargetLanguage )
            throw new Exception( "Source and target are the same" );
        if ( settings.SourceLanguage != settings.TargetLanguage && ( string.IsNullOrEmpty( _options.Value.Key ) || string.IsNullOrEmpty( _options.Value.Region ) ) )
            throw new Exception( "Configuration is invalid" );
        var targetFolder = Path.GetDirectoryName(settings.SourcePath) ?? throw new Exception( "Unable to grab destination folder" );
        var targetName = Path.GetFileNameWithoutExtension( settings.SourcePath );
        if ( !targetName.EndsWith( $".{settings.TargetLanguage}" ) )
            targetName += $".{settings.TargetLanguage}";
        targetName += $".{settings.TargetExtension}";
        var targetPath = Path.Combine(targetFolder, targetName);
        Dictionary<string, string> oldData = new();
        if ( !settings.Override && File.Exists( targetPath ) )
        {
            switch ( settings.TargetExtension )
            {
                case "resx":
                    oldData = await GetTransaltionsFromResxFileAsync( targetPath );
                    break;
                case "po":
                    oldData = await GetTransaltionsFromPoFileAsync( targetPath );
                    break;
            }
        }   
        using var s = File.OpenRead( settings.SourcePath );
        var xdoc = await XDocument.LoadAsync( s, LoadOptions.None, CancellationToken.None );
        if ( settings.SourceLanguage != settings.TargetLanguage )
        {
            var (translatedCount, copiedCount) = await TranslateAsync( xdoc, oldData, settings );
            if ( settings.Verbose )
                AnsiConsole.MarkupLine( $"Number of copied/translated records: [blue]{copiedCount}/{translatedCount}[/]" );
        }
        switch ( settings.TargetExtension )
        {
            case "resx":
                await SaveAsResxFileAsync( xdoc, targetPath );
                break;
            case "po":
                await SaveAsPOFileAsync( xdoc, GetContext( settings ), targetPath );
                break;
            default:
                throw new Exception( $"Unable to save to {settings.TargetExtension} extension" );
        }
        AnsiConsole.MarkupLine( $"Saved in [blue]{targetPath}[/]" );
        return 0;
    }

    private async Task<(int translatedCount, int copiedCount)> TranslateAsync(XDocument document, Dictionary<string, string> oldData, DefaultCommandSettings settings )
    {
        if ( _options.Value.Key == null || _options.Value.Region == null )
            return (0, 0);
        var elements = document.XPathSelectElements( "//data" );
        string endpoint = "https://api.cognitive.microsofttranslator.com/";
        string route = $"/translate?api-version=3.0&from={settings.SourceLanguage}&to={settings.TargetLanguage}";
        var client = new RestClient( endpoint + route );
        var count = elements.Count();
        var translatedCount = 0;
        var copiedCount = 0;
        await AnsiConsole.Progress()
            .AutoClear( true )
            .StartAsync( async ctx =>
            {
                // Define tasks
                var task = ctx.AddTask( $"Translating" );
                task.MaxValue( count );
                foreach ( var element in elements )
                {
                    string name = element.Attribute( "name" )?.Value ?? throw new Exception( "Missing name attribute" );
                    XElement valueElement = element.Element( "value" ) ?? throw new Exception( "Missing value element" );
                    if ( string.IsNullOrWhiteSpace( valueElement.Value ) )
                        continue;
                    if ( oldData.TryGetValue( name, out var oldValue ) && !string.IsNullOrWhiteSpace( oldValue ) )
                    {
                        valueElement.Value = oldValue;
                        task.Increment( 1 );
                        copiedCount++;
                        continue;
                    }
                    var request = new RestRequest()
                        .AddHeader( "Ocp-Apim-Subscription-Key", _options.Value.Key )
                        .AddHeader( "Ocp-Apim-Subscription-Region", _options.Value.Region )
                        .AddJsonBody( new object[] { new { Text = valueElement.Value } } );
                    var response = await client.PostAsync<List<MicrosoftTranslatorRecord>>( request, CancellationToken.None );
                    valueElement?.SetValue( response?.FirstOrDefault()?.Translations?.FirstOrDefault()?.Text ?? string.Empty );
                    task.Increment( 1 );
                    translatedCount++;
                }
            } );
        return (translatedCount, copiedCount);
    }

    private async Task SaveAsResxFileAsync(XDocument document, string path )
    {
        using var xmlWriter = XmlWriter.Create( path, new XmlWriterSettings { Async = true, Indent = true } );
        await document.WriteToAsync( xmlWriter, CancellationToken.None );
    }

    private string? GetContext( DefaultCommandSettings settings )
    {
        if( settings.SkipContext )
            return null;
        var context = $"{settings.ContextPrefix}{Path.GetFileNameWithoutExtension( settings.SourcePath )}";
        if ( settings.SourceLanguage != null && context.EndsWith( $".{settings.SourceLanguage}" ) )
            return context[ ..( context.Length - settings.SourceLanguage.Length - 1 ) ];
        return context;
    }

    /// <summary>
    /// Save XDocument as po file
    /// Based on:
    /// OrchardCore.Localization.PortableObject.PortableObjectStringLocalizerFactory
    /// </summary>
    /// <param name="document"></param>
    /// <param name="context"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    private async Task SaveAsPOFileAsync( XDocument document, string? context, string path )
    {
        var elements = document.XPathSelectElements( "//data" );
        if ( elements == null )
            return;
        StringBuilder sb = new StringBuilder();
        foreach ( XElement element in elements )
        {
            var key = element.Attribute( "name" )?.Value.Replace( "\"", "\\\"" );
            var value = element.Element( "value" )?.Value.Replace( "\"", "\\\"" );
            if ( context != null )
                sb
                    .AppendFormat( "msgctxt \"{0}\"", context )
                    .AppendLine();
            sb
                .AppendFormat( "msgid \"{0}\"", key )
                .AppendLine()
                .AppendFormat( "msgstr \"{0}\"", value )
                .AppendLine()
                .AppendLine();
        }
        await File.WriteAllTextAsync(path, sb.ToString() );
    }

    private async Task<Dictionary<string,string>> GetTransaltionsFromResxFileAsync( string path )
    {
        using var stream = File.OpenRead(path);
        var xdoc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
        var elements = xdoc.XPathSelectElements("//data");
        return elements
            .Select( x => new { Key = x.Attribute( "name" )?.Value, x.Element( "value" )?.Value } )
            .Where( x => !string.IsNullOrEmpty( x.Key ) && !string.IsNullOrEmpty( x.Value ) )
            .ToDictionary( x => x.Key ?? string.Empty, x => x.Value ?? string.Empty );
    }

    private async Task<Dictionary<string, string>> GetTransaltionsFromPoFileAsync( string path )
    {
        var content = await File.ReadAllTextAsync( path );
        var regex = new Regex( "msgid \"(?<key>.+)\"(\n|\r|\r\n)msgstr \"(?<value>.+)\"(\n|\r|\r\n|$)", RegexOptions.None, TimeSpan.FromMilliseconds( 400 ) );
        return regex.Matches( content )
            .ToDictionary( x => x.Groups[ "key" ].Value, x => x.Groups[ "value" ].Value );
    }


    public class DefaultCommandSettings : CommandSettings
    {
        [CommandArgument(0, "<source>" )]
        [Description()]
        public string? SourcePath { get; set; }

        [CommandArgument(2, "<lang>" )]
        [Description()]
        public string? TargetLanguage { get; set; }

        [CommandOption("--override")]
        [DefaultValue(false)]
        public bool Override { get; set; }

        [CommandOption("--sourcelang")]
        [DefaultValue("en")]
        public string? SourceLanguage { get; set; }

        [CommandOption( "--extension" )]
        [DefaultValue( "resx" )]
        public string? TargetExtension { get; set; }

        [CommandOption( "--verbose" )]
        public bool Verbose { get; set; }

        [CommandOption( "--contextprefix" )]
        public string? ContextPrefix { get; set; }

        [CommandOption( "--skipcontext" )]
        public bool SkipContext { get; set; }
    }
}
