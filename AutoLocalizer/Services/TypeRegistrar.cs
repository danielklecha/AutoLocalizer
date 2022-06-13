using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLocalizer.Services;

public sealed class TypeRegistrar : ITypeRegistrar, IDisposable
{
    private IHost? _host;
    private bool disposedValue;

    public IHost Host => _host ??= _builder.Build();

    private readonly IHostBuilder _builder;

    public TypeRegistrar( IHostBuilder builder )
    {
        _builder = builder;
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(Host);
    }

    public void Register( Type service, Type implementation )
    {
        _builder.ConfigureServices(services => services.AddTransient(service, implementation));
    }

    public void RegisterInstance( Type service, object implementation )
    {
        _builder.ConfigureServices(services => services.AddSingleton(service, implementation));
    }

    public void RegisterLazy( Type service, Func<object> func )
    {
        ArgumentNullException.ThrowIfNull(func);
        _builder.ConfigureServices(services => services.AddSingleton(service, ( provider ) => func()));
    }

    private void Dispose( bool disposing )
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _host?.Dispose();
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
