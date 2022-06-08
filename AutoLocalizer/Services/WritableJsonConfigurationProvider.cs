using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace AutoLocalizer.Services;

public class WritableJsonConfigurationProvider : JsonConfigurationProvider, IWritableConfigurationProvider
{
    public WritableJsonConfigurationProvider( JsonConfigurationSource source ) : base( source )
    {

    }

    private static Dictionary<string, object?> GetUnflattened( IDictionary<string, string> input )
    {
        var output = new Dictionary<string, object?>();
        foreach ( var row in input )
        {
            var currentDictionary = output;
            var keys = row.Key.Split( ConfigurationPath.KeyDelimiter ).AsEnumerable();
            while ( keys.Any() )
            {
                var key = keys.First();
                if ( keys.Count() == 1 )
                {
                    currentDictionary[ key ] = row.Value;
                    break;
                }
                if ( !currentDictionary.TryGetValue( key, out object? value ) || value is not Dictionary<string, object?> )
                {
                    Dictionary<string, object?> node = new();
                    currentDictionary[ key ] = node;
                    currentDictionary = node;
                }
                else
                    currentDictionary = (Dictionary<string, object?>)value;
                keys = keys.Skip( 1 );
            }
        }
        return output;
    }

    public async Task SaveAsync( CancellationToken cancelationToken )
    {
        var json = JsonSerializer.Serialize( GetUnflattened( Data ), new JsonSerializerOptions() { WriteIndented = true } );
        var root = ( base.Source.FileProvider as PhysicalFileProvider )?.Root;
        var path = root != null ? Path.Combine( root, base.Source.Path ) : base.Source.Path;
        await System.IO.File.WriteAllTextAsync( path, json, cancelationToken );
    }
}