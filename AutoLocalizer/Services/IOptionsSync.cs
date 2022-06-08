using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLocalizer.Services;

public interface IOptionsSync<T> where T : class
{
	public Task SyncAsync( T options, CancellationToken cancelationToken = default );
	public Task SaveAsync( CancellationToken cancelationToken = default );
}