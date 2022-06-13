using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLocalizer.Services;

public sealed class TypeResolver : ITypeResolver
{
    private readonly IHost _host;

    public TypeResolver( IHost host )
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public object? Resolve( Type? type )
    {
        if (type == null)
        {
            return null;
        }
        return _host?.Services.GetService(type);
    }
}
