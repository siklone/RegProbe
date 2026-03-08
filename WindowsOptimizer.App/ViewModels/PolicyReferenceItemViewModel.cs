using System;
using System.Windows.Input;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Utilities;

namespace WindowsOptimizer.App.ViewModels;

public sealed class PolicyReferenceItemViewModel
{
    public PolicyReferenceItemViewModel(PolicyReferenceEntry entry, Action<PolicyReferenceEntry> openAction)
    {
        Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        OpenMatchingSettingsCommand = new RelayCommand(_ => openAction(Entry));
    }

    private PolicyReferenceEntry Entry { get; }

    public string ComponentName => Entry.ComponentName;
    public string PrimaryCategory => Entry.PrimaryCategory;
    public string ScopeLabel => Entry.ScopeLabel;
    public string SettingCountLabel => Entry.SettingCountLabel;
    public string StatusLabel => Entry.StatusLabel;
    public string RiskLabel => Entry.RiskLabel;
    public string Description => Entry.Description;
    public string ExampleSummary => Entry.ExampleSummary;
    public string SearchFragment => Entry.SearchFragment;
    public string ReadPathLabel => Entry.ReadPathLabel;
    public string ScopeDetail => Entry.ScopeDetail;
    public string ExpectedBehavior => Entry.ExpectedBehavior;
    public string RelatedSettingsLabel => Entry.RelatedSettingsLabel;

    public ICommand OpenMatchingSettingsCommand { get; }
}
