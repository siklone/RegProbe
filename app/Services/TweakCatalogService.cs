using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Application.Services.TweakProviders;
using RegProbe.Core;
using RegProbe.Core.Services;

namespace RegProbe.Application.Services;

public sealed record TweakCatalogEntry(string Category, ITweak Tweak);

public interface ITweakCatalog
{
    IReadOnlyList<TweakCatalogEntry> GetAll();
    ITweak? FindById(string tweakId);
    Task<TweakExecutionReport> ExecuteAsync(
        ITweak tweak,
        TweakExecutionOptions options,
        IProgress<TweakExecutionUpdate>? progress = null,
        CancellationToken ct = default);
    Task<TweakExecutionStep> ExecuteStepAsync(
        ITweak tweak,
        TweakAction action,
        IProgress<TweakExecutionUpdate>? progress = null,
        CancellationToken ct = default);
    bool IsElevated { get; }
    bool IsElevatedHostAvailable { get; }
    string ElevatedHostPath { get; }
}

public sealed class TweakCatalogService : ITweakCatalog
{
    private readonly TweakExecutionPipeline _pipeline;
    private readonly IReadOnlyList<ITweakProvider> _providers;
    private readonly TweakContext _context;
    private readonly object _sync = new();
    private IReadOnlyList<TweakCatalogEntry>? _cache;
    private Dictionary<string, ITweak>? _byId;

    public TweakCatalogService()
    {
        var bootstrap = TweakCatalogBootstrap.Create();
        _pipeline = bootstrap.Pipeline;
        _context = bootstrap.Context;
        IsElevated = bootstrap.IsElevated;
        IsElevatedHostAvailable = bootstrap.IsElevatedHostAvailable;
        ElevatedHostPath = bootstrap.ElevatedHostPath;
        _providers = TweakProviderCatalog.CreateDefault();
    }

    public bool IsElevated { get; }
    public bool IsElevatedHostAvailable { get; }
    public string ElevatedHostPath { get; }

    public IReadOnlyList<TweakCatalogEntry> GetAll()
    {
        lock (_sync)
        {
            if (_cache is not null)
            {
                return _cache;
            }

            var entries = new List<TweakCatalogEntry>();
            foreach (var provider in _providers)
            {
                foreach (var tweak in provider.CreateTweaks(_pipeline, _context, IsElevated))
                {
                    entries.Add(new TweakCatalogEntry(provider.CategoryName, tweak));
                }
            }

            _cache = entries;
            _byId = entries
                .Select(entry => entry.Tweak)
                .Where(tweak => !string.IsNullOrWhiteSpace(tweak.Id))
                .GroupBy(tweak => tweak.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            return _cache;
        }
    }

    public ITweak? FindById(string tweakId)
    {
        if (string.IsNullOrWhiteSpace(tweakId))
        {
            return null;
        }

        GetAll();

        lock (_sync)
        {
            if (_byId != null && _byId.TryGetValue(tweakId, out var tweak))
            {
                return tweak;
            }
        }

        return null;
    }

    public Task<TweakExecutionReport> ExecuteAsync(
        ITweak tweak,
        TweakExecutionOptions options,
        IProgress<TweakExecutionUpdate>? progress = null,
        CancellationToken ct = default)
        => _pipeline.ExecuteAsync(tweak, options, progress, ct);

    public Task<TweakExecutionStep> ExecuteStepAsync(
        ITweak tweak,
        TweakAction action,
        IProgress<TweakExecutionUpdate>? progress = null,
        CancellationToken ct = default)
        => _pipeline.ExecuteStepAsync(tweak, action, progress, ct);
}
