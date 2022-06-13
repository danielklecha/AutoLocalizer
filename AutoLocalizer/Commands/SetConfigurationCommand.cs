using AutoLocalizer.Models;
using AutoLocalizer.Services;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AutoLocalizer.Commands.SetConfigurationCommand;

namespace AutoLocalizer.Commands;

public class SetConfigurationCommand : AsyncCommand<SetConfigurationSettings>
{
    private readonly IOptions<WritableOptions> _options;
    private readonly IOptionsSync<WritableOptions> _optionsSync;

    public SetConfigurationCommand( IOptions<WritableOptions> options, IOptionsSync<WritableOptions> optionsSync )
    {
        _options = options;
        _optionsSync = optionsSync;
    }
    public override async Task<int> ExecuteAsync( CommandContext context, SetConfigurationSettings settings )
    {
        var value = _options.Value;
        value.Key = await new TextPrompt<string>("What's key?").Secret().ShowAsync(AnsiConsole.Console, CancellationToken.None);
        value.Region = await new TextPrompt<string>("What's region?").ShowAsync(AnsiConsole.Console, CancellationToken.None);
        await _optionsSync.SyncAsync(value);
        await _optionsSync.SaveAsync();
        return 0;
    }

    public class SetConfigurationSettings : CommandSettings
    {

    }
}
