using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AutoLocalizer.Services;

public class OptionsSync<T> : IOptionsSync<T> where T : class
{
	private readonly IConfigurationSection _configurationSection;
	private readonly IEnumerable<IConfigurationProvider> _providers;
	private readonly System.Reflection.PropertyInfo[] _properties;

	public OptionsSync( IConfigurationSection configurationSection, IEnumerable<IConfigurationProvider> providers )
	{
		_configurationSection = configurationSection ?? throw new ArgumentNullException(nameof(configurationSection));
		_providers = providers ?? Enumerable.Empty<IConfigurationProvider>();
		_properties = typeof(T)
			.GetProperties()
			.Where(x => !Attribute.IsDefined(x, typeof(JsonIgnoreAttribute)) && !Attribute.IsDefined(x, typeof(IgnoreDataMemberAttribute)))
			.ToArray();
	}

	private IEnumerable<(string Path, JsonProperty P)> GetLeaves( string? path, JsonProperty p )
		=> p.Value.ValueKind != JsonValueKind.Object
			? new[] { (Path: path == null ? p.Name : ConfigurationPath.Combine(path, p.Name), p) }
			: p.Value.EnumerateObject().SelectMany(child => GetLeaves(path == null ? p.Name : ConfigurationPath.Combine(path, p.Name), child));

	private Dictionary<string, string?> GetFlattened( T options )
	{
		using var document = JsonSerializer.SerializeToDocument(options, typeof(T));
		return document.RootElement
			.EnumerateObject()
			.SelectMany(p => GetLeaves(null, p))
			.ToDictionary(k => k.Path, v => v.P.Value.ValueKind == JsonValueKind.Null ? null : v.P.Value.ToString());
	}

	public Task SyncAsync( T options, CancellationToken cancelationToken = default )
	{
		if (options == null)
			throw new ArgumentNullException(nameof(options));
		var dictionary = GetFlattened(options);
		if (!_providers.Any())
			foreach (var key in dictionary.Keys)
				_configurationSection[ key ] = dictionary[ key ];
		else
			foreach (var provider in _providers)
				foreach (var key in dictionary.Keys)
					provider.Set(ConfigurationPath.Combine(_configurationSection.Path, key), dictionary[ key ]);
		return Task.CompletedTask;
	}

	public async Task SaveAsync( CancellationToken cancelationToken = default )
	{
		foreach (var provider in _providers.OfType<IWritableConfigurationProvider>())
		{
			cancelationToken.ThrowIfCancellationRequested();
			await provider.SaveAsync(cancelationToken);
		}
	}
}