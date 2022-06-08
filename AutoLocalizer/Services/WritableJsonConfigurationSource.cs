using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace AutoLocalizer.Services;

public class WritableJsonConfigurationSource : JsonConfigurationSource
{
    public override IConfigurationProvider Build( IConfigurationBuilder builder )
    {
        this.EnsureDefaults( builder );
        return new WritableJsonConfigurationProvider( this );
    }
}