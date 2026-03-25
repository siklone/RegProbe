using System;
using System.Windows.Input;
using System.Windows.Media;
using OpenTraceProject.App.Services;

namespace OpenTraceProject.App.ViewModels;

public sealed class InstallRecommendationItemViewModel
{
    private static readonly Brush NvidiaBrush = CreateBrush(0x68, 0xE5, 0x8F);
    private static readonly Brush AmdBrush = CreateBrush(0xFF, 0x63, 0x6F);
    private static readonly Brush UtilityBrush = CreateBrush(0x38, 0xD0, 0xFF);
    private static readonly Brush RuntimeBrush = CreateBrush(0xFF, 0xD7, 0x5A);

    public InstallRecommendationItemViewModel(
        InstallRecommendation model,
        Action<InstallRecommendationItemViewModel> primaryAction,
        Action<InstallRecommendationItemViewModel> sourceAction)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        PrimaryActionCommand = new RelayCommand(_ => primaryAction(this));
        SourceActionCommand = new RelayCommand(
            _ => sourceAction(this),
            _ => HasSourceAction);
    }

    public InstallRecommendation Model { get; }

    public string Id => Model.Id;
    public string Category => Model.Category;
    public string Title => Model.Title;
    public string Description => Model.Description;
    public string Reason => Model.Reason;
    public string CurrentState => Model.CurrentState;
    public string StatusLabel => Model.StatusLabel;
    public string SourceName => Model.SourceName;
    public string SourceUrl => Model.SourceUrl;
    public string? InstallCommand => Model.InstallCommand;
    public bool IsInstalled => Model.IsInstalled;
    public bool HasInstallAction => !string.IsNullOrWhiteSpace(InstallCommand);
    public bool HasSourceAction => !string.IsNullOrWhiteSpace(SourceUrl);
    public string PrimaryActionLabel => HasInstallAction ? "Install" : "Open page";
    public Brush IndicatorBrush => ResolveIndicatorBrush();

    public ICommand PrimaryActionCommand { get; }
    public ICommand SourceActionCommand { get; }

    private Brush ResolveIndicatorBrush()
    {
        if (Title.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
        {
            return NvidiaBrush;
        }

        if (Title.Contains("AMD", StringComparison.OrdinalIgnoreCase))
        {
            return AmdBrush;
        }

        return Category switch
        {
            "Runtime" => RuntimeBrush,
            "Utility" => UtilityBrush,
            _ => AmdBrush
        };
    }

    private static Brush CreateBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
