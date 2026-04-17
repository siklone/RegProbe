using System.Collections.ObjectModel;
using System.IO;
using RegProbe.App.Services;
using RegProbe.Core;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;
using RegProbe.Infrastructure;
using RegProbe.Infrastructure.Services;

namespace RegProbe.App.ViewModels;

internal sealed class WorkspacePluginTweakLoader
{
    private readonly TweakExecutionPipeline _pipeline;
    private readonly bool _isElevated;
    private readonly IAppLogger _appLogger;
    private readonly PluginLoader _pluginLoader = new();

    public WorkspacePluginTweakLoader(
        TweakExecutionPipeline pipeline,
        bool isElevated,
        IAppLogger appLogger)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _isElevated = isElevated;
        _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
    }

    public void Load(ObservableCollection<TweakItemViewModel> tweaks)
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _appLogger.Log(LogLevel.Info, $"Plugin discovery: baseDir='{baseDir}'");

            var existingIds = WorkspaceTweakIdSetBuilder.Build(tweaks);
            var pluginsPath = Path.Combine(baseDir, "Plugins");
            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
            }

            _appLogger.Log(LogLevel.Info, $"Plugin discovery: pluginsPath='{pluginsPath}'");

            var plugins = _pluginLoader.LoadPlugins(pluginsPath).ToList();
            _appLogger.Log(LogLevel.Info, $"Plugin discovery: loadedPlugins={plugins.Count}");

            foreach (var plugin in plugins)
            {
                _appLogger.Log(LogLevel.Info, $"Plugin loaded: name='{plugin.PluginName}' version='{plugin.Version}'");
                var pluginTweaks = plugin.GetTweaks()?.ToList() ?? new List<ITweak>();
                _appLogger.Log(LogLevel.Info, $"Plugin tweaks: plugin='{plugin.PluginName}' count={pluginTweaks.Count}");

                foreach (var tweak in pluginTweaks)
                {
                    if (string.IsNullOrWhiteSpace(tweak.Id) || !existingIds.Add(tweak.Id))
                    {
                        continue;
                    }

                    tweaks.Add(new TweakItemViewModel(tweak, _pipeline, _isElevated));
                }
            }
        }
        catch (Exception ex)
        {
            _appLogger.Log(LogLevel.Error, "Plugin system error", ex);
        }
    }
}
