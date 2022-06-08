using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLocalizer.Services;

public interface IWritableConfigurationProvider : IConfigurationProvider
{
    public Task SaveAsync( CancellationToken cancelationToken = default );
}