using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AutoLocalizer.Models;

public class Translation
{
    [JsonPropertyName( "text" )]
    public string? Text { get; set; }
    [JsonPropertyName( "to" )]
    public string? To { get; set; }
}
