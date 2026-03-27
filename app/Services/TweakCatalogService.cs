using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.App.Services.TweakProviders;
using RegProbe.App.Utilities;
using RegProbe.Core;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Services;
using RegProbe.Infrastructure;
using RegProbe.Infrastructure.Elevation;
using RegProbe.Infrastructure.Registry;

namespace RegProbe.App.Services;

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
        var paths = AppPaths.FromEnvironment();
        paths.EnsureDirectories();
        var logger = new FileAppLogger(paths);
        var logStore = new FileTweakLogStore(paths);
        var rollbackStore = new RollbackStateStore(paths);
        _pipeline = new TweakExecutionPipeline(logger, logStore, rollbackStore);

        IsElevated = ProcessElevation.IsElevated();
        ElevatedHostPath = ElevatedHostLocator.GetExecutablePath();
        IsElevatedHostAvailable = File.Exists(ElevatedHostPath);

        var elevatedHostClient = new ElevatedHostClient(new ElevatedHostClientOptions
        {
            HostExecutablePath = ElevatedHostPath,
            PipeName = ElevatedHostDefaults.GetPipeNameForProcess(Process.GetCurrentProcess().Id),
            ParentProcessId = Process.GetCurrentProcess().Id
        });

        var elevatedRegistryAccessor = new ElevatedRegistryAccessor(elevatedHostClient);
        _context = new TweakContext(
            new RoutingRegistryAccessor(new LocalRegistryAccessor(), elevatedRegistryAccessor),
            elevatedRegistryAccessor,
            new ElevatedServiceManager(elevatedHostClient),
            new ElevatedScheduledTaskManager(elevatedHostClient),
            new ElevatedFileSystemAccessor(elevatedHostClient),
            new ElevatedCommandRunner(elevatedHostClient));

        _providers = BuildProviders();
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

    private static IReadOnlyList<ITweakProvider> BuildProviders()
    {
        return new ITweakProvider[]
        {
            new SystemTweakProvider(),
            new SystemRegistryTweakProvider(),
            new PrivacyTweakProvider(),
            new SecurityTweakProvider(),
            new NetworkTweakProvider(),
            new PowerTweakProvider(),
            new PeripheralTweakProvider(),
            new VisibilityTweakProvider(),
            new PerformanceTweakProvider(),
            new AudioTweakProvider(),
            new MiscTweakProvider(),
            new DeveloperTweakProvider()
        };
    }
}
