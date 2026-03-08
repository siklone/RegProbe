using System;
using System.Windows.Input;
using WindowsOptimizer.App.Services;

namespace WindowsOptimizer.App.ViewModels;

public sealed class InstallRecommendationItemViewModel
{
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

    public ICommand PrimaryActionCommand { get; }
    public ICommand SourceActionCommand { get; }
}
