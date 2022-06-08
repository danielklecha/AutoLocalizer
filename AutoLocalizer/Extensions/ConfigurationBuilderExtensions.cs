using AutoLocalizer.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLocalizer.Extensions;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddWritableJsonFile( this IConfigurationBuilder builder, string fileName )
    {
        builder.Add<WritableJsonConfigurationSource>(s =>
        {
            s.FileProvider = builder.GetFileProvider();
            s.Path = fileName;
            s.Optional = true;
            s.ReloadOnChange = true;
            s.ResolveFileProvider();
        });
        return builder;
    }
}