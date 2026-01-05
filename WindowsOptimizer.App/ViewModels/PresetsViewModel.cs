using System.Collections.ObjectModel;
using System.Windows.Input;
using WindowsOptimizer.App.Models;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Utilities;

namespace WindowsOptimizer.App.ViewModels;

public sealed class PresetsViewModel : ViewModelBase
{
    private readonly PresetService _presetService;
    private string _statusMessage = "Select a preset to optimize your system.";
    private bool _isApplying;
    private PresetCardViewModel? _selectedPreset;

    public PresetsViewModel()
    {
        // TODO: Integrate with existing TweakService in future
        _presetService = new PresetService();

        LoadPresets();
    }

    public string Title => "Smart Presets";

    public ObservableCollection<PresetCardViewModel> Presets { get; } = new();

    public PresetCardViewModel? SelectedPreset
    {
        get => _selectedPreset;
        set => SetProperty(ref _selectedPreset, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsApplying
    {
        get => _isApplying;
        set => SetProperty(ref _isApplying, value);
    }

    private void LoadPresets()
    {
        var presets = _presetService.GetAllPresets();
        
        foreach (var preset in presets)
        {
            var cardVm = new PresetCardViewModel(preset, this)
            {
                ApplyCommand = new RelayCommand(
                    async _ => await ApplyPresetAsync(preset.Id),
                    _ => !IsApplying
                ),
                RevertCommand = new RelayCommand(
                    async _ => await RevertPresetAsync(preset.Id),
                    _ => !IsApplying
                )
            };
            
            Presets.Add(cardVm);
        }
    }

    private async Task ApplyPresetAsync(string presetId)
    {
        IsApplying = true;
        StatusMessage = "Applying preset...";

        try
        {
            var progressHandler = new Progress<int>(percent =>
            {
                StatusMessage = $"Applying preset: {percent}%";
            });

            var result = await _presetService.ApplyPresetAsync(presetId, progressHandler);

            if (result.Success)
            {
                StatusMessage = $"✓ {result.Message}";
            }
            else
            {
                StatusMessage = $"⚠ {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Failed to apply preset: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }

    private async Task RevertPresetAsync(string presetId)
    {
        IsApplying = true;
        StatusMessage = "Reverting preset...";

        try
        {
            var success = await _presetService.RevertPresetAsync(presetId);

            if (success)
            {
                StatusMessage = "✓ Preset reverted successfully";
            }
            else
            {
                StatusMessage = "⚠ Failed to revert preset";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Error: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }
}

/// <summary>
/// ViewModel for individual preset card in UI.
/// </summary>
public class PresetCardViewModel : ViewModelBase
{
    private readonly PresetModel _preset;
    private readonly PresetsViewModel _parentVm;

    public PresetCardViewModel(PresetModel preset, PresetsViewModel parentVm)
    {
        _preset = preset;
        _parentVm = parentVm;
    }

    public string Name => _preset.Name;
    public string Description => _preset.Description;
    public string IconPath => _preset.IconPath;
    public string Category => _preset.Category.ToString();
    public string Level => _preset.Level.ToString();
    public int TweakCount => _preset.TweakIds.Count;

    public string LevelColor => _preset.Level switch
    {
        PresetDifficulty.Beginner => "#4CAF50",  // Green
        PresetDifficulty.Advanced => "#FF9800",  // Orange
        PresetDifficulty.Expert => "#F44336",    // Red
        _ => "#9E9E9E"
    };

    public ICommand? ApplyCommand { get; set; }
    public ICommand? RevertCommand { get; set; }

    public bool IsApplying => _parentVm.IsApplying;
}
